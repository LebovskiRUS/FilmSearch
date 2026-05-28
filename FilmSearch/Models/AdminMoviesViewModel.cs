using System.ComponentModel.DataAnnotations;

namespace FilmSearch.Models
{
    public class AdminMoviesViewModel
    {
        public string? Query { get; set; }
        public string DefaultDatasetDirectory { get; set; } = string.Empty;
        public MovieFormViewModel NewMovie { get; set; } = new();
        public List<Movie> Movies { get; set; } = new();
    }

    public class MovieFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите название фильма.")]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Range(1888, 2100, ErrorMessage = "Укажите корректный год.")]
        public int? Year { get; set; }

        [Required(ErrorMessage = "Укажите жанры фильма.")]
        [StringLength(200)]
        public string Genres { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(500)]
        [Url(ErrorMessage = "Укажите корректный URL обложки.")]
        public string? ImageUrl { get; set; }
    }
}
