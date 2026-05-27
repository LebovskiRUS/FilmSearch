namespace FilmSearch.Models
{
    public class RecommendationViewModel
    {
        public List<Movie> RecommendedMovies { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = "baseline";
    }
}
