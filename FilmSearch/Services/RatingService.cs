using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Services
{
    public interface IRatingService
    {
        Task<(bool Success, string Message)> RateAsync(int userId, int movieId, int value, CancellationToken cancellationToken = default);
        Task<int> CountUserRatingsAsync(int userId, CancellationToken cancellationToken = default);
    }

    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;

        public RatingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> CountUserRatingsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _context.Ratings.CountAsync(rating => rating.UserId == userId, cancellationToken);
        }

        public async Task<(bool Success, string Message)> RateAsync(
            int userId,
            int movieId,
            int value,
            CancellationToken cancellationToken = default)
        {
            if (value is < 1 or > 5)
            {
                return (false, "Оценка должна быть от 1 до 5.");
            }

            var movieExists = await _context.Movies.AnyAsync(movie => movie.Id == movieId, cancellationToken);
            if (!movieExists)
            {
                return (false, "Фильм не найден.");
            }

            var rating = await _context.Ratings
                .FirstOrDefaultAsync(item => item.UserId == userId && item.MovieId == movieId, cancellationToken);

            if (rating is null)
            {
                _context.Ratings.Add(new Rating
                {
                    UserId = userId,
                    MovieId = movieId,
                    Value = value,
                    RatedAt = DateTime.UtcNow
                });
            }
            else
            {
                rating.Value = value;
                rating.RatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await RefreshMovieStatsAsync(movieId, cancellationToken);

            return (true, $"Спасибо! Вы поставили оценку {value} фильму.");
        }

        private async Task RefreshMovieStatsAsync(int movieId, CancellationToken cancellationToken)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(item => item.Id == movieId, cancellationToken);
            if (movie is null)
            {
                return;
            }

            var stats = await _context.Ratings
                .Where(rating => rating.MovieId == movieId)
                .GroupBy(rating => rating.MovieId)
                .Select(group => new
                {
                    Average = group.Average(rating => rating.Value),
                    Count = group.Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            movie.AverageRating = stats is null ? 0 : Math.Round((decimal)stats.Average, 2);
            movie.RatingsCount = stats?.Count ?? 0;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
