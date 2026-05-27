using System.ComponentModel.DataAnnotations.Schema;

namespace FilmSearch.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public int? MovieLensId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string Genres { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int RatingsCount { get; set; }

        [NotMapped]
        public string Genre
        {
            get => string.IsNullOrWhiteSpace(Genres) ? "Не указан" : Genres.Replace("|", ", ");
            set => Genres = value ?? string.Empty;
        }

        public List<Rating> Ratings { get; set; } = new();
    }
}
