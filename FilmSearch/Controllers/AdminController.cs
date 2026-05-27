using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieLensImportService _movieLensImportService;
        private readonly IConfiguration _configuration;

        public AdminController(
            ApplicationDbContext context,
            IMovieLensImportService movieLensImportService,
            IConfiguration configuration)
        {
            _context = context;
            _movieLensImportService = movieLensImportService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
        {
            ViewData["Query"] = query;
            ViewData["DefaultDatasetDirectory"] = _configuration["MovieLens:DatasetDirectory"] ?? string.Empty;

            var movies = _context.Movies.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var pattern = $"%{query.Trim()}%";
                movies = movies.Where(movie => EF.Functions.ILike(movie.Title, pattern));
            }

            var model = await movies
                .OrderByDescending(movie => movie.Id)
                .Take(50)
                .ToListAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ImportMovieLens(string datasetDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(datasetDirectory))
            {
                TempData["Error"] = "Укажите папку с файлами MovieLens.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _movieLensImportService.ImportAsync(datasetDirectory, cancellationToken);
                TempData["Message"] = $"Импорт завершен: users={result.UsersImported}, movies={result.MoviesImported}, ratings={result.RatingsImported}.";
            }
            catch (Exception exception)
            {
                TempData["Error"] = $"Ошибка импорта: {exception.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie(
            string title,
            int? year,
            string genres,
            string? description,
            string? imageUrl,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(genres))
            {
                TempData["Error"] = "Укажите название и жанры фильма.";
                return RedirectToAction(nameof(Index));
            }

            _context.Movies.Add(new Movie
            {
                Title = title.Trim(),
                Year = year,
                Genres = genres.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
            });

            await _context.SaveChangesAsync(cancellationToken);
            TempData["Message"] = "Фильм добавлен.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditMovie(int id, CancellationToken cancellationToken)
        {
            var movie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (movie is null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpPost]
        public async Task<IActionResult> EditMovie(
            int id,
            string title,
            int? year,
            string genres,
            string? description,
            string? imageUrl,
            CancellationToken cancellationToken)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (movie is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(genres))
            {
                TempData["Error"] = "Укажите название и жанры фильма.";
                return View(movie);
            }

            movie.Title = title.Trim();
            movie.Year = year;
            movie.Genres = genres.Trim();
            movie.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            movie.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            await _context.SaveChangesAsync(cancellationToken);
            TempData["Message"] = "Фильм обновлен.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMovie(int id, CancellationToken cancellationToken)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (movie is null)
            {
                return NotFound();
            }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync(cancellationToken);
            TempData["Message"] = "Фильм удален.";

            return RedirectToAction(nameof(Index));
        }
    }
}
