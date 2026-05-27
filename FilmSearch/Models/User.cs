namespace FilmSearch.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }

        public bool IsMovieLensUser { get; set; }
        public int? MovieLensUserId { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public int? Occupation { get; set; }
        public string? ZipCode { get; set; }

        public List<Rating> Ratings { get; set; } = new();
    }
}
