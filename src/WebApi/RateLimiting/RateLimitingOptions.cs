using System.ComponentModel.DataAnnotations;

namespace WebApi.RateLimiting
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        [Range(1, int.MaxValue)]
        public int UnauthenticatedRequestsPerMinute { get; set; } = 60;

        [Range(1, int.MaxValue)]
        public int AuthenticatedRequestsPerMinute { get; set; } = 600;

        [Required(AllowEmptyStrings = false)]
        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

        /// <summary>
        /// Keys that qualify for the authenticated limit. When empty, any non-empty
        /// header value is treated as an API key.
        /// </summary>
        public string[] ApiKeys { get; set; } = Array.Empty<string>();
    }
}
