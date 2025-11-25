using FilmSearch.Models;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class RecommendationController : Controller
    {
        public IActionResult Index()
        {
            // Заглушка для рекомендаций
            var model = new RecommendationViewModel
            {
                RecommendedMovies = new List<Movie>
            {
                new Movie { Id = 4, Title = "Крестный отец 2", Genre = "Криминал", Year = 1974, ImageUrl = "/images/godfather2.jpg" },
                new Movie { Id = 5, Title = "Славные парни", Genre = "Криминал", Year = 1990, ImageUrl = "/images/goodfellas.jpg" },
                new Movie { Id = 6, Title = "Лицо со шрамом", Genre = "Криминал", Year = 1983, ImageUrl = "/images/scarface.jpg" }
            },
                Explanation = "Вам могут понравиться эти фильмы, потому что вы высоко оценили 'Крестный отец'"
            };

            return View(model);
        }
    }
}
