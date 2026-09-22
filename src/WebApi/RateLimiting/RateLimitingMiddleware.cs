using Microsoft.Extensions.Options;

namespace WebApi.RateLimiting
{
    public class RateLimitingMiddleware
    {
        public const string HealthCheckPath = "/health";

        private readonly RequestDelegate _next;
        private readonly IRateLimiter _limiter;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(RequestDelegate next, IRateLimiter limiter, IOptions<RateLimitOptions> options)
        {
            _next = next;
            _limiter = limiter;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(HealthCheckPath))
            {
                await _next(context);
                return;
            }

            var (clientKey, limit) = ResolveClient(context);
            var decision = _limiter.TryAcquire(clientKey, limit);

            if (decision.IsAllowed)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new RateLimitedResponse("rate_limited", decision.RetryAfterSeconds));
        }

        private (string ClientKey, int Limit) ResolveClient(HttpContext context)
        {
            var apiKey = context.Request.Headers[_options.ApiKeyHeaderName].ToString();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return ($"key:{apiKey}", _options.AuthenticatedRequestsPerMinute);
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return ($"ip:{ip}", _options.AnonymousRequestsPerMinute);
        }
    }

    public record RateLimitedResponse(string error, int retry_after_seconds);

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
            services.AddSingleton<IRateLimiter, FixedWindowRateLimiter>();
            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
            => app.UseMiddleware<RateLimitingMiddleware>();
    }
}
