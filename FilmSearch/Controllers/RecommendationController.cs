using System.Security.Claims;
using FilmSearch.Data;
using FilmSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(ApplicationDbContext context, IRecommendationService recommendationService)
        {
            _context = context;
            _recommendationService = recommendationService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _recommendationService.GetRecommendationsAsync(GetCurrentUserId(), 10, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> OpenMovie(int movieId, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId is not null)
            {
                var recommendationEvent = await _context.RecommendationEvents
                    .Where(item => item.UserId == userId.Value && item.MovieId == movieId)
                    .OrderByDescending(item => item.ShownAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (recommendationEvent is not null && recommendationEvent.ClickedAt is null)
                {
                    recommendationEvent.ClickedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return RedirectToAction("Rate", "Movie", new { movieId });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
