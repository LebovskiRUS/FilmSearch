using FilmSearch.Models;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class MovieController : Controller
    {
        public IActionResult Index()
        {
            // Заглушка для списка фильмов 
            var movies = GetAllMovies();

            return View(movies);
        }

        public IActionResult Rate(int movieId)
        {
            // Заглушка для получения фильма по ID
            var movie = GetMovieById(movieId);

            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpPost]
        public IActionResult Rate(int movieId, int rating)
        {
            // Заглушка для оценки фильма
            TempData["Message"] = $"Спасибо! Вы поставили оценку {rating} фильму.";
            return RedirectToAction("Index");
        }

        private Movie GetMovieById(int movieId)
        {
            var movies = GetAllMovies();
            return movies.FirstOrDefault(m => m.Id == movieId);
        }

        private List<Movie> GetAllMovies()
        {
            return new List<Movie>
            {
                new Movie {
                    Id = 1,
                    Title = "Крестный отец",
                    Genre = "Криминал",
                    Year = 1972,
                    Description = "Эпическая сага о сицилийской мафиозной семье.",
                    ImageUrl = "/images/godfather.jpg"
                },
                new Movie {
                    Id = 2,
                    Title = "Форрест Гамп",
                    Genre = "Драма",
                    Year = 1994,
                    Description = "История человека с добрым сердцем.",
                    ImageUrl = "/images/forrestgump.jpg"
                },
                new Movie {
                    Id = 3,
                    Title = "Начало",
                    Genre = "Фантастика",
                    Year = 2010,
                    Description = "Проникновение в сны других людей.",
                    ImageUrl = "/images/inception.jpg"
                },
                new Movie {
                    Id = 4,
                    Title = "Крестный отец 2",
                    Genre = "Криминал",
                    Year = 1974,
                    Description = "Продолжение эпической саги о семье Корлеоне.",
                    ImageUrl = "/images/godfather2.jpg"
                },
                new Movie {
                    Id = 5,
                    Title = "Славные парни",
                    Genre = "Криминал",
                    Year = 1990,
                    Description = "Хроника жизни и падения преступной группировки.",
                    ImageUrl = "/images/goodfellas.jpg"
                },
                new Movie {
                    Id = 6,
                    Title = "Лицо со шрамом",
                    Genre = "Криминал",
                    Year = 1983,
                    Description = "Икона криминального кино о стремлении к власти.",
                    ImageUrl = "/images/scarface.jpg"
                }
            };
        }
    }
}
