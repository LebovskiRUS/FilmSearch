using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationViewModel> GetRecommendationsAsync(int? userId, int take = 10, CancellationToken cancellationToken = default);
    }

    public class BaselineRecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;

        public BaselineRecommendationService(ApplicationDbContext context)
        {
            _context = context;
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

            var userRatings = await _context.Ratings
                .AsNoTracking()
                .Include(rating => rating.Movie)
                .Where(rating => rating.UserId == userId.Value)
                .ToListAsync(cancellationToken);

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

            await SaveRecommendationEventsAsync(userId.Value, recommended, cancellationToken);

            return new RecommendationViewModel
            {
                RecommendedMovies = recommended,
                Explanation = $"Вам могут понравиться эти фильмы, потому что вы высоко оценивали жанры: {string.Join(", ", favoriteGenres)}.",
                ModelVersion = "baseline-genre"
            };
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

        private async Task SaveRecommendationEventsAsync(int userId, List<Movie> movies, CancellationToken cancellationToken)
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
