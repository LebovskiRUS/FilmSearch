using FilmSearch.Models;

namespace MovieRecommendationSystem.Services
{
    public interface IRecommendationService
    {
        RecommendationViewModel GetRecommendations(int userId);
    }

    public class MockRecommendationService : IRecommendationService
    {
        public RecommendationViewModel GetRecommendations(int userId)
        {
            // Заглушка для рекомендаций
            return new RecommendationViewModel
            {
                RecommendedMovies = new List<Movie>
                {
                    new Movie { Id = 4, Title = "Крестный отец 2", Genre = "Криминал", Year = 1974, ImageUrl = "/images/godfather2.jpg" },
                    new Movie { Id = 5, Title = "Славные парни", Genre = "Криминал", Year = 1990, ImageUrl = "/images/goodfellas.jpg" },
                    new Movie { Id = 6, Title = "Лицо со шрамом", Genre = "Криминал", Year = 1983, ImageUrl = "/images/scarface.jpg" }
                },
                Explanation = "Вам могут понравиться эти фильмы, потому что вы высоко оценили 'Крестный отец'"
            };
        }
    }
}