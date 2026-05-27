using Npgsql;

namespace FilmSearch.Data
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(100) NOT NULL UNIQUE,
                    email VARCHAR(255) UNIQUE,
                    password_hash TEXT,
                    role VARCHAR(50) NOT NULL DEFAULT 'User',
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    is_movielens_user BOOLEAN NOT NULL DEFAULT FALSE,
                    movielens_user_id INTEGER UNIQUE,
                    gender VARCHAR(1),
                    age INTEGER,
                    occupation INTEGER,
                    zip_code VARCHAR(20)
                );

                CREATE TABLE IF NOT EXISTS movies (
                    id SERIAL PRIMARY KEY,
                    movielens_id INTEGER UNIQUE,
                    title VARCHAR(300) NOT NULL,
                    year INTEGER,
                    genres VARCHAR(200) NOT NULL DEFAULT '',
                    description TEXT,
                    image_url VARCHAR(500),
                    average_rating NUMERIC(4, 2) NOT NULL DEFAULT 0,
                    ratings_count INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS ratings (
                    id SERIAL PRIMARY KEY,
                    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    movie_id INTEGER NOT NULL REFERENCES movies(id) ON DELETE CASCADE,
                    value INTEGER NOT NULL CHECK (value >= 1 AND value <= 5),
                    rated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    source_timestamp BIGINT,
                    UNIQUE(user_id, movie_id)
                );

                CREATE TABLE IF NOT EXISTS recommendation_events (
                    id BIGSERIAL PRIMARY KEY,
                    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    movie_id INTEGER NOT NULL REFERENCES movies(id) ON DELETE CASCADE,
                    score NUMERIC(5, 3) NOT NULL DEFAULT 0,
                    model_version VARCHAR(100) NOT NULL DEFAULT 'baseline',
                    shown_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    clicked_at TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS ix_movies_title ON movies(title);
                CREATE INDEX IF NOT EXISTS ix_movies_year ON movies(year);
                CREATE INDEX IF NOT EXISTS ix_ratings_movie_id ON ratings(movie_id);
                CREATE INDEX IF NOT EXISTS ix_recommendation_events_user_shown ON recommendation_events(user_id, shown_at);
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
