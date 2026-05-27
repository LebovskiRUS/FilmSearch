using System.Security.Claims;
using FilmSearch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IRatingService _ratingService;

        public MovieController(IMovieService movieService, IRatingService ratingService)
        {
            _movieService = movieService;
            _ratingService = ratingService;
        }

        public async Task<IActionResult> Index(string? query, string? genre, CancellationToken cancellationToken)
        {
            ViewData["Query"] = query;
            ViewData["Genre"] = genre;

            var movies = await _movieService.SearchAsync(query, genre, 100, cancellationToken);
            return View(movies);
        }

        [Authorize]
        public async Task<IActionResult> Rate(int movieId, CancellationToken cancellationToken)
        {
            var movie = await _movieService.GetByIdAsync(movieId, cancellationToken);
            if (movie is null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Rate(int movieId, int rating, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _ratingService.RateAsync(userId.Value, movieId, rating, cancellationToken);
            TempData[result.Success ? "Message" : "Error"] = result.Message;

            return RedirectToAction("Index");
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
