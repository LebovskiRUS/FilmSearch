namespace FilmSearch.Services
{
    public class MlRecommendationOptions
    {
        public bool Enabled { get; set; } = true;
        public string BaseUrl { get; set; } = "http://127.0.0.1:8001";
        public int TimeoutSeconds { get; set; } = 5;
        public int MinimumRatings { get; set; } = 5;
        public int CandidateLimit { get; set; } = 1000;
    }
}
