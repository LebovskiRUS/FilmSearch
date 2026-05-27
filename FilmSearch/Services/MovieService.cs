using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Services
{
    public interface IMovieService
    {
        Task<List<Movie>> GetPopularAsync(int take = 12, CancellationToken cancellationToken = default);
        Task<List<Movie>> SearchAsync(string? query, string? genre, int take = 100, CancellationToken cancellationToken = default);
        Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }

    public class MovieService : IMovieService
    {
        private readonly ApplicationDbContext _context;

        public MovieService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Movie>> GetPopularAsync(int take = 12, CancellationToken cancellationToken = default)
        {
            return _context.Movies
                .AsNoTracking()
                .OrderByDescending(movie => movie.RatingsCount)
                .ThenByDescending(movie => movie.AverageRating)
                .ThenBy(movie => movie.Title)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Movie>> SearchAsync(string? query, string? genre, int take = 100, CancellationToken cancellationToken = default)
        {
            var movies = _context.Movies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var pattern = $"%{query.Trim()}%";
                movies = movies.Where(movie => EF.Functions.ILike(movie.Title, pattern));
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                var pattern = $"%{genre.Trim()}%";
                movies = movies.Where(movie => EF.Functions.ILike(movie.Genres, pattern));
            }

            return movies
                .OrderByDescending(movie => movie.RatingsCount)
                .ThenByDescending(movie => movie.AverageRating)
                .ThenBy(movie => movie.Title)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.Id == id, cancellationToken);
        }
    }
}
