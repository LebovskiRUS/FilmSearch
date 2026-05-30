using System.Text.RegularExpressions;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace FilmSearch.Data
{
    public interface IMovieLensImportService
    {
        Task<MovieLensImportResult> ImportAsync(string datasetDirectory, CancellationToken cancellationToken = default);
    }

    public partial class MovieLensImportService : IMovieLensImportService
    {
        private static readonly Encoding MovieLensEncoding = Encoding.Latin1;

        private readonly string _connectionString;
        private readonly DatabaseInitializer _databaseInitializer;

        public MovieLensImportService(IConfiguration configuration, DatabaseInitializer databaseInitializer)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                CommandTimeout = 0
            };

            _connectionString = connectionStringBuilder.ConnectionString;
            _databaseInitializer = databaseInitializer;
        }

        public async Task<MovieLensImportResult> ImportAsync(string datasetDirectory, CancellationToken cancellationToken = default)
        {
            var usersPath = Path.Combine(datasetDirectory, "users.dat");
            var moviesPath = Path.Combine(datasetDirectory, "movies.dat");
            var ratingsPath = Path.Combine(datasetDirectory, "ratings.dat");

            if (!File.Exists(usersPath) || !File.Exists(moviesPath) || !File.Exists(ratingsPath))
            {
                throw new FileNotFoundException("MovieLens directory must contain users.dat, movies.dat and ratings.dat.");
            }

            await _databaseInitializer.InitializeAsync(cancellationToken);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var result = new MovieLensImportResult
            {
                UsersImported = await ImportUsersAsync(connection, usersPath, cancellationToken),
                MoviesImported = await ImportMoviesAsync(connection, moviesPath, cancellationToken),
                RatingsImported = await ImportRatingsAsync(connection, ratingsPath, cancellationToken)
            };

            await RefreshMovieStatsAsync(connection, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }

        private static async Task<int> ImportUsersAsync(NpgsqlConnection connection, string usersPath, CancellationToken cancellationToken)
        {
            await ExecuteAsync(connection, """
                CREATE TEMP TABLE tmp_movielens_users (
                    movielens_user_id INTEGER NOT NULL,
                    gender VARCHAR(1),
                    age INTEGER,
                    occupation INTEGER,
                    zip_code VARCHAR(20)
                ) ON COMMIT DROP;
                """, cancellationToken);

            var count = 0;
            await using (var importer = await connection.BeginBinaryImportAsync(
                "COPY tmp_movielens_users (movielens_user_id, gender, age, occupation, zip_code) FROM STDIN (FORMAT BINARY)",
                cancellationToken))
            {
                foreach (var line in File.ReadLines(usersPath, MovieLensEncoding))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var parts = line.Split("::");
                    if (parts.Length != 5)
                    {
                        continue;
                    }

                    await importer.StartRowAsync(cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[0]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(parts[1], NpgsqlDbType.Varchar, cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[2]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[3]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(parts[4], NpgsqlDbType.Varchar, cancellationToken);
                    count++;
                }

                await importer.CompleteAsync(cancellationToken);
            }

            await ExecuteAsync(connection, """
                INSERT INTO users (username, is_movielens_user, movielens_user_id, gender, age, occupation, zip_code)
                SELECT 'ml_' || movielens_user_id, TRUE, movielens_user_id, gender, age, occupation, zip_code
                FROM tmp_movielens_users
                ON CONFLICT (movielens_user_id) DO UPDATE SET
                    gender = EXCLUDED.gender,
                    age = EXCLUDED.age,
                    occupation = EXCLUDED.occupation,
                    zip_code = EXCLUDED.zip_code;
                """, cancellationToken);

            return count;
        }

        private static async Task<int> ImportMoviesAsync(NpgsqlConnection connection, string moviesPath, CancellationToken cancellationToken)
        {
            await ExecuteAsync(connection, """
                CREATE TEMP TABLE tmp_movielens_movies (
                    movielens_id INTEGER NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    year INTEGER,
                    genres VARCHAR(200) NOT NULL
                ) ON COMMIT DROP;
                """, cancellationToken);

            var count = 0;
            await using (var importer = await connection.BeginBinaryImportAsync(
                "COPY tmp_movielens_movies (movielens_id, title, year, genres) FROM STDIN (FORMAT BINARY)",
                cancellationToken))
            {
                foreach (var line in File.ReadLines(moviesPath, MovieLensEncoding))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var parts = line.Split("::");
                    if (parts.Length != 3)
                    {
                        continue;
                    }

                    var title = parts[1];
                    var year = TryReadYear(title);

                    await importer.StartRowAsync(cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[0]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(title, NpgsqlDbType.Varchar, cancellationToken);
                    if (year.HasValue)
                    {
                        await importer.WriteAsync(year.Value, NpgsqlDbType.Integer, cancellationToken);
                    }
                    else
                    {
                        await importer.WriteNullAsync(cancellationToken);
                    }

                    await importer.WriteAsync(parts[2], NpgsqlDbType.Varchar, cancellationToken);
                    count++;
                }

                await importer.CompleteAsync(cancellationToken);
            }

            await ExecuteAsync(connection, """
                INSERT INTO movies (movielens_id, title, year, genres)
                SELECT movielens_id, title, year, genres
                FROM tmp_movielens_movies
                ON CONFLICT (movielens_id) DO UPDATE SET
                    title = EXCLUDED.title,
                    year = EXCLUDED.year,
                    genres = EXCLUDED.genres;
                """, cancellationToken);

            return count;
        }

        private static async Task<int> ImportRatingsAsync(NpgsqlConnection connection, string ratingsPath, CancellationToken cancellationToken)
        {
            await ExecuteAsync(connection, """
                CREATE TEMP TABLE tmp_movielens_ratings (
                    movielens_user_id INTEGER NOT NULL,
                    movielens_movie_id INTEGER NOT NULL,
                    value INTEGER NOT NULL,
                    source_timestamp BIGINT NOT NULL
                ) ON COMMIT DROP;
                """, cancellationToken);

            var count = 0;
            await using (var importer = await connection.BeginBinaryImportAsync(
                "COPY tmp_movielens_ratings (movielens_user_id, movielens_movie_id, value, source_timestamp) FROM STDIN (FORMAT BINARY)",
                cancellationToken))
            {
                foreach (var line in File.ReadLines(ratingsPath, MovieLensEncoding))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var parts = line.Split("::");
                    if (parts.Length != 4)
                    {
                        continue;
                    }

                    await importer.StartRowAsync(cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[0]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[1]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(int.Parse(parts[2]), NpgsqlDbType.Integer, cancellationToken);
                    await importer.WriteAsync(long.Parse(parts[3]), NpgsqlDbType.Bigint, cancellationToken);
                    count++;
                }

                await importer.CompleteAsync(cancellationToken);
            }

            await ExecuteAsync(connection, """
                INSERT INTO ratings (user_id, movie_id, value, rated_at, source_timestamp)
                SELECT users.id,
                       movies.id,
                       tmp.value,
                       (to_timestamp(tmp.source_timestamp) AT TIME ZONE 'UTC'),
                       tmp.source_timestamp
                FROM tmp_movielens_ratings tmp
                JOIN users ON users.movielens_user_id = tmp.movielens_user_id
                JOIN movies ON movies.movielens_id = tmp.movielens_movie_id
                ON CONFLICT (user_id, movie_id) DO UPDATE SET
                    value = EXCLUDED.value,
                    rated_at = EXCLUDED.rated_at,
                    source_timestamp = EXCLUDED.source_timestamp;
                """, cancellationToken);

            return count;
        }

        private static async Task RefreshMovieStatsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            await ExecuteAsync(connection, """
                UPDATE movies
                SET average_rating = 0,
                    ratings_count = 0;

                UPDATE movies
                SET average_rating = stats.average_rating,
                    ratings_count = stats.ratings_count
                FROM (
                    SELECT movie_id,
                           ROUND(AVG(value)::numeric, 2) AS average_rating,
                           COUNT(*)::integer AS ratings_count
                    FROM ratings
                    GROUP BY movie_id
                ) stats
                WHERE movies.id = stats.movie_id;
                """, cancellationToken);
        }

        private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 0;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static int? TryReadYear(string title)
        {
            var match = YearRegex().Match(title);
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        [GeneratedRegex(@"\((\d{4})\)\s*$")]
        private static partial Regex YearRegex();
    }
}
