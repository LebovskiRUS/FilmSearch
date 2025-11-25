namespace FilmSearch.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public int Value { get; set; } // 1-5
        public DateTime RatedAt { get; set; }
    }
}
