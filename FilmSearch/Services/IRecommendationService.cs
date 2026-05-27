using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FilmSearch.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationViewModel> GetRecommendationsAsync(int? userId, int take = 10, CancellationToken cancellationToken = default);
    }

    public class BaselineRecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMlRecommendationClient _mlClient;
        private readonly MlRecommendationOptions _mlOptions;

        public BaselineRecommendationService(
            ApplicationDbContext context,
            IMlRecommendationClient mlClient,
            IOptions<MlRecommendationOptions> mlOptions)
        {
            _context = context;
            _mlClient = mlClient;
            _mlOptions = mlOptions.Value;
        }

        public async Task<RecommendationViewModel> GetRecommendationsAsync(
            int? userId,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            if (userId is null)
            {
                return new RecommendationViewModel
                {
                    RecommendedMovies = await GetPopularMoviesAsync(take, cancellationToken),
                    Explanation = "Войдите в систему и поставьте оценки, чтобы получить персональные рекомендации.",
                    ModelVersion = "baseline-popular"
                };
            }

            var userRatings = await GetUserRatingsAsync(userId.Value, cancellationToken);
            var mlResult = await TryGetMlRecommendationsAsync(userId.Value, userRatings, take, cancellationToken);
            if (mlResult is not null)
            {
                return mlResult;
            }

            return await GetBaselineRecommendationsAsync(userId.Value, userRatings, take, cancellationToken);
        }

        private async Task<RecommendationViewModel?> TryGetMlRecommendationsAsync(
            int userId,
            List<Rating> userRatings,
            int take,
            CancellationToken cancellationToken)
        {
            if (!_mlOptions.Enabled || userRatings.Count < _mlOptions.MinimumRatings)
            {
                return null;
            }

            var mlRatings = userRatings
                .Where(rating => rating.Movie?.MovieLensId is not null)
                .Select(rating => new MlRatingDto
                {
                    MovieLensId = rating.Movie!.MovieLensId!.Value,
                    Rating = rating.Value
                })
                .ToList();

            if (mlRatings.Count < _mlOptions.MinimumRatings)
            {
                return null;
            }

            var ratedMovieLensIds = mlRatings
                .Select(rating => rating.MovieLensId)
                .ToHashSet();

            var candidates = await _context.Movies
                .AsNoTracking()
                .Where(movie => movie.MovieLensId != null && !ratedMovieLensIds.Contains(movie.MovieLensId.Value))
                .OrderByDescending(movie => movie.RatingsCount)
                .ThenByDescending(movie => movie.AverageRating)
                .Take(_mlOptions.CandidateLimit)
                .Select(movie => movie.MovieLensId!.Value)
                .ToListAsync(cancellationToken);

            var response = await _mlClient.GetRecommendationsAsync(new MlRecommendationRequest
            {
                UserId = userId,
                Ratings = mlRatings,
                Candidates = candidates,
                Take = take
            }, cancellationToken);

            if (response?.Recommendations.Count is null or 0)
            {
                return null;
            }

            var movieLensIds = response.Recommendations
                .Select(item => item.MovieLensId)
                .ToList();

            var movies = await _context.Movies
                .AsNoTracking()
                .Where(movie => movie.MovieLensId != null && movieLensIds.Contains(movie.MovieLensId.Value))
                .ToListAsync(cancellationToken);

            var moviesByMovieLensId = movies
                .Where(movie => movie.MovieLensId is not null)
                .ToDictionary(movie => movie.MovieLensId!.Value);

            var orderedMovies = response.Recommendations
                .Where(item => moviesByMovieLensId.ContainsKey(item.MovieLensId))
                .Select(item => moviesByMovieLensId[item.MovieLensId])
                .ToList();

            if (orderedMovies.Count == 0)
            {
                return null;
            }

            await SaveRecommendationEventsAsync(userId, response.Recommendations, response.ModelVersion, cancellationToken);

            return new RecommendationViewModel
            {
                RecommendedMovies = orderedMovies,
                Explanation = "Рекомендации рассчитаны PyTorch-моделью на основе ваших оценок.",
                ModelVersion = response.ModelVersion
            };
        }

        private async Task<RecommendationViewModel> GetBaselineRecommendationsAsync(
            int userId,
            List<Rating> userRatings,
            int take,
            CancellationToken cancellationToken)
        {
            if (userRatings.Count < 5)
            {
                return new RecommendationViewModel
                {
                    RecommendedMovies = await GetPopularMoviesAsync(take, cancellationToken),
                    Explanation = "Поставьте хотя бы 5 оценок, чтобы рекомендации стали персональными. Пока показываем популярные фильмы.",
                    ModelVersion = "baseline-popular"
                };
            }

            var favoriteGenres = userRatings
                .Where(rating => rating.Value >= 4 && rating.Movie is not null)
                .SelectMany(rating => rating.Movie!.Genres.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .GroupBy(genre => genre)
                .OrderByDescending(group => group.Count())
                .Take(3)
                .Select(group => group.Key)
                .ToList();

            if (favoriteGenres.Count == 0)
            {
                return new RecommendationViewModel
                {
                    RecommendedMovies = await GetPopularMoviesAsync(take, cancellationToken),
                    Explanation = "Пока недостаточно высоких оценок для жанрового профиля, показываем популярные фильмы.",
                    ModelVersion = "baseline-popular"
                };
            }

            var ratedMovieIds = userRatings.Select(rating => rating.MovieId).ToHashSet();
            var candidates = await _context.Movies
                .AsNoTracking()
                .Where(movie => !ratedMovieIds.Contains(movie.Id))
                .OrderByDescending(movie => movie.RatingsCount)
                .ThenByDescending(movie => movie.AverageRating)
                .Take(500)
                .ToListAsync(cancellationToken);

            var recommended = candidates
                .Where(movie => favoriteGenres.Any(genre => movie.Genres.Contains(genre, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(movie => movie.AverageRating)
                .ThenByDescending(movie => movie.RatingsCount)
                .Take(take)
                .ToList();

            await SaveBaselineRecommendationEventsAsync(userId, recommended, cancellationToken);

            return new RecommendationViewModel
            {
                RecommendedMovies = recommended,
                Explanation = $"Вам могут понравиться эти фильмы, потому что вы высоко оценивали жанры: {string.Join(", ", favoriteGenres)}.",
                ModelVersion = "baseline-genre"
            };
        }

        private Task<List<Rating>> GetUserRatingsAsync(int userId, CancellationToken cancellationToken)
        {
            return _context.Ratings
                .AsNoTracking()
                .Include(rating => rating.Movie)
                .Where(rating => rating.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        private Task<List<Movie>> GetPopularMoviesAsync(int take, CancellationToken cancellationToken)
        {
            return _context.Movies
                .AsNoTracking()
                .OrderByDescending(movie => movie.RatingsCount)
                .ThenByDescending(movie => movie.AverageRating)
                .ThenBy(movie => movie.Title)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private async Task SaveRecommendationEventsAsync(
            int userId,
            List<MlRecommendationItem> recommendations,
            string modelVersion,
            CancellationToken cancellationToken)
        {
            var movieLensIds = recommendations.Select(item => item.MovieLensId).ToList();
            var movies = await _context.Movies
                .AsNoTracking()
                .Where(movie => movie.MovieLensId != null && movieLensIds.Contains(movie.MovieLensId.Value))
                .Select(movie => new { movie.Id, MovieLensId = movie.MovieLensId!.Value })
                .ToListAsync(cancellationToken);

            var movieIdByMovieLensId = movies.ToDictionary(movie => movie.MovieLensId, movie => movie.Id);
            var now = DateTime.UtcNow;
            var events = recommendations
                .Where(item => movieIdByMovieLensId.ContainsKey(item.MovieLensId))
                .Select(item => new RecommendationEvent
                {
                    UserId = userId,
                    MovieId = movieIdByMovieLensId[item.MovieLensId],
                    Score = item.Score,
                    ModelVersion = modelVersion,
                    ShownAt = now
                });

            _context.RecommendationEvents.AddRange(events);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SaveBaselineRecommendationEventsAsync(int userId, List<Movie> movies, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var events = movies.Select(movie => new RecommendationEvent
            {
                UserId = userId,
                MovieId = movie.Id,
                Score = movie.AverageRating,
                ModelVersion = "baseline-genre",
                ShownAt = now
            });

            _context.RecommendationEvents.AddRange(events);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
