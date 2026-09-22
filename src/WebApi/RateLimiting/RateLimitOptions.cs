namespace WebApi.RateLimiting
{
    public class RateLimitOptions
    {
        public const string SectionName = "RateLimiting";

        public int AnonymousRequestsPerMinute { get; set; } = 60;

        public int AuthenticatedRequestsPerMinute { get; set; } = 600;

        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
    }
}
