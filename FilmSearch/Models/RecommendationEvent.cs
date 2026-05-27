namespace FilmSearch.Models
{
    public class RecommendationEvent
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public decimal Score { get; set; }
        public string ModelVersion { get; set; } = "baseline";
        public DateTime ShownAt { get; set; }
        public DateTime? ClickedAt { get; set; }

        public User? User { get; set; }
        public Movie? Movie { get; set; }
    }
}
