using FilmSearch.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IMovieLensImportService _movieLensImportService;
        private readonly IConfiguration _configuration;

        public AdminController(IMovieLensImportService movieLensImportService, IConfiguration configuration)
        {
            _movieLensImportService = movieLensImportService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewData["DefaultDatasetDirectory"] = _configuration["MovieLens:DatasetDirectory"] ?? string.Empty;
            return View();
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
    }
}
