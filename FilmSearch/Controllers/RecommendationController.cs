using System.Security.Claims;
using FilmSearch.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _recommendationService.GetRecommendationsAsync(GetCurrentUserId(), 10, cancellationToken);
            return View(model);
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
