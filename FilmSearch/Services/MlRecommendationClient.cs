using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace FilmSearch.Services
{
    public interface IMlRecommendationClient
    {
        Task<MlRecommendationResponse?> GetRecommendationsAsync(
            MlRecommendationRequest request,
            CancellationToken cancellationToken = default);
    }

    public class MlRecommendationClient : IMlRecommendationClient
    {
        private readonly HttpClient _httpClient;
        private readonly MlRecommendationOptions _options;
        private readonly ILogger<MlRecommendationClient> _logger;

        public MlRecommendationClient(
            HttpClient httpClient,
            IOptions<MlRecommendationOptions> options,
            ILogger<MlRecommendationClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<MlRecommendationResponse?> GetRecommendationsAsync(
            MlRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return null;
            }

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync("/recommendations", request, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ML service returned {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<MlRecommendationResponse>(timeout.Token);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(exception, "ML service is unavailable, falling back to baseline recommendations.");
                return null;
            }
        }
    }

    public class MlRecommendationRequest
    {
        public int? UserId { get; set; }
        public List<MlRatingDto> Ratings { get; set; } = new();
        public List<int> Candidates { get; set; } = new();
        public int Take { get; set; } = 10;
    }

    public class MlRatingDto
    {
        public int MovieLensId { get; set; }
        public int Rating { get; set; }
    }

    public class MlRecommendationResponse
    {
        public string ModelVersion { get; set; } = "pytorch-mf";
        public List<MlRecommendationItem> Recommendations { get; set; } = new();
    }

    public class MlRecommendationItem
    {
        public int MovieLensId { get; set; }
        public decimal Score { get; set; }
    }
}
