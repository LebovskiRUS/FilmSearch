using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Services
{
    public interface IAdminMovieService
    {
        Task<List<Movie>> SearchAsync(string? query, int take = 50, CancellationToken cancellationToken = default);
        Task<MovieFormViewModel?> GetFormAsync(int id, CancellationToken cancellationToken = default);
        Task CreateAsync(MovieFormViewModel form, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(MovieFormViewModel form, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public class AdminMovieService : IAdminMovieService
    {
        private readonly ApplicationDbContext _context;

        public AdminMovieService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Movie>> SearchAsync(string? query, int take = 50, CancellationToken cancellationToken = default)
        {
            var movies = _context.Movies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var pattern = $"%{query.Trim()}%";
                movies = movies.Where(movie => EF.Functions.ILike(movie.Title, pattern));
            }

            return movies
                .OrderByDescending(movie => movie.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<MovieFormViewModel?> GetFormAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Movies
                .AsNoTracking()
                .Where(movie => movie.Id == id)
                .Select(movie => new MovieFormViewModel
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Year = movie.Year,
                    Genres = movie.Genres,
                    Description = movie.Description,
                    ImageUrl = movie.ImageUrl
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task CreateAsync(MovieFormViewModel form, CancellationToken cancellationToken = default)
        {
            _context.Movies.Add(new Movie
            {
                Title = form.Title.Trim(),
                Year = form.Year,
                Genres = form.Genres.Trim(),
                Description = NormalizeOptional(form.Description),
                ImageUrl = NormalizeOptional(form.ImageUrl)
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> UpdateAsync(MovieFormViewModel form, CancellationToken cancellationToken = default)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(item => item.Id == form.Id, cancellationToken);
            if (movie is null)
            {
                return false;
            }

            movie.Title = form.Title.Trim();
            movie.Year = form.Year;
            movie.Genres = form.Genres.Trim();
            movie.Description = NormalizeOptional(form.Description);
            movie.ImageUrl = NormalizeOptional(form.ImageUrl);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (movie is null)
            {
                return false;
            }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
