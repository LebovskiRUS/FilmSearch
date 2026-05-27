using FilmSearch.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieService _movieService;

        public HomeController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var popularMovies = await _movieService.GetPopularAsync(6, cancellationToken);
            return View(popularMovies);
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
