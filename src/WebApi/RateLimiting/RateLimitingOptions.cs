namespace WebApi.RateLimiting
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        public int UnauthenticatedRequestsPerMinute { get; set; } = 60;

        public int AuthenticatedRequestsPerMinute { get; set; } = 600;

        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
    }
}
