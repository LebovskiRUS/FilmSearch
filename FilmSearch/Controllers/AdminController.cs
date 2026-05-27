using FilmSearch.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IMovieLensImportService _movieLensImportService;

        public AdminController(IMovieLensImportService movieLensImportService)
        {
            _movieLensImportService = movieLensImportService;
        }

        [HttpPost]
        public async Task<IActionResult> ImportMovieLens(string datasetDirectory, CancellationToken cancellationToken)
        {
            var result = await _movieLensImportService.ImportAsync(datasetDirectory, cancellationToken);
            return Json(result);
        }
    }
}
