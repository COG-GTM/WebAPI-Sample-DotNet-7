using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace WebApi.RateLimiting
{
    public class RateLimitingMiddleware
    {
        public const string HealthCheckPath = "/health";

        private readonly RequestDelegate _next;
        private readonly IRateLimiter _rateLimiter;
        private readonly RateLimitingOptions _options;

        public RateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter, IOptions<RateLimitingOptions> options)
        {
            _next = next;
            _rateLimiter = rateLimiter;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(HealthCheckPath))
            {
                await _next(context);
                return;
            }

            var (clientKey, limit) = Identify(context);
            var result = _rateLimiter.TryAcquire(clientKey, limit);

            if (result.IsAllowed)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = result.RetryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new RateLimitedResponse(result.RetryAfterSeconds));
        }

        private (string ClientKey, int Limit) Identify(HttpContext context)
        {
            var apiKey = context.Request.Headers[_options.ApiKeyHeaderName].ToString();
            if (!string.IsNullOrWhiteSpace(apiKey) && IsKnownApiKey(apiKey))
            {
                return ($"key:{apiKey}", _options.AuthenticatedRequestsPerMinute);
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return ($"ip:{ip}", _options.UnauthenticatedRequestsPerMinute);
        }

        private bool IsKnownApiKey(string apiKey) =>
            _options.ApiKeys.Length == 0 || _options.ApiKeys.Contains(apiKey, StringComparer.Ordinal);

        private sealed record RateLimitedResponse([property: JsonPropertyName("retry_after_seconds")] int RetryAfterSeconds)
        {
            [JsonPropertyName("error")]
            public string Error { get; } = "rate_limited";
        }
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IRateLimiter, RateLimiter>();
            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
