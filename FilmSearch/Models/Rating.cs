namespace FilmSearch.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public int Value { get; set; }
        public DateTime RatedAt { get; set; }
        public long? SourceTimestamp { get; set; }

        public User? User { get; set; }
        public Movie? Movie { get; set; }
    }
}
