using FilmSearch.Data;
using FilmSearch.Models;
using FilmSearch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminMovieService _adminMovieService;
        private readonly IMovieLensImportService _movieLensImportService;
        private readonly IConfiguration _configuration;

        public AdminController(
            IAdminMovieService adminMovieService,
            IMovieLensImportService movieLensImportService,
            IConfiguration configuration)
        {
            _adminMovieService = adminMovieService;
            _movieLensImportService = movieLensImportService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
        {
            return View(new AdminMoviesViewModel
            {
                Query = query,
                DefaultDatasetDirectory = _configuration["MovieLens:DatasetDirectory"] ?? string.Empty,
                Movies = await _adminMovieService.SearchAsync(query, cancellationToken: cancellationToken)
            });
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
        public async Task<IActionResult> CreateMovie(MovieFormViewModel form, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Проверьте данные фильма.";
                return RedirectToAction(nameof(Index));
            }

            await _adminMovieService.CreateAsync(form, cancellationToken);
            TempData["Message"] = "Фильм добавлен.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditMovie(int id, CancellationToken cancellationToken)
        {
            var form = await _adminMovieService.GetFormAsync(id, cancellationToken);
            return form is null ? NotFound() : View(form);
        }

        [HttpPost]
        public async Task<IActionResult> EditMovie(MovieFormViewModel form, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Проверьте данные фильма.";
                return View(form);
            }

            if (!await _adminMovieService.UpdateAsync(form, cancellationToken))
            {
                return NotFound();
            }

            TempData["Message"] = "Фильм обновлен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMovie(int id, CancellationToken cancellationToken)
        {
            if (!await _adminMovieService.DeleteAsync(id, cancellationToken))
            {
                return NotFound();
            }

            TempData["Message"] = "Фильм удален.";
            return RedirectToAction(nameof(Index));
        }
    }
}
