using System.Diagnostics;
using FilmSearch.Models;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Заглушка для популярных фильмов
            var popularMovies = new List<Movie>
        {
            new Movie { Id = 1, Title = "Крестный отец", Genre = "Криминал", Year = 1972, ImageUrl = "/images/godfather.jpg" },
            new Movie { Id = 2, Title = "Побег из Шоушенка", Genre = "Драма", Year = 1994, ImageUrl = "/images/shawshank.jpg" },
            new Movie { Id = 3, Title = "Темный рыцарь", Genre = "Боевик", Year = 2008, ImageUrl = "/images/darkknight.jpg" }
        };

            return View(popularMovies);
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
