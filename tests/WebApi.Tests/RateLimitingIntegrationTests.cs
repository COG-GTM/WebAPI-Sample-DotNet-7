using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApi.Tests
{
    public class RateLimitingIntegrationTests
    {
        private const int AnonymousLimit = 3;
        private const int AuthenticatedLimit = 5;

        private static WebApplicationFactory<Program> CreateFactory() =>
            new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimiting:AnonymousRequestsPerMinute", AnonymousLimit.ToString());
                builder.UseSetting("RateLimiting:AuthenticatedRequestsPerMinute", AuthenticatedLimit.ToString());
            });

        [Fact]
        public async Task Anonymous_Client_Gets_429_With_RetryAfter_And_Json_Body()
        {
            await using var factory = CreateFactory();
            var client = factory.CreateClient();

            for (var i = 0; i < AnonymousLimit; i++)
            {
                var ok = await client.GetAsync("/api/educations");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
            }

            var limited = await client.GetAsync("/api/educations");

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            var retryAfter = Assert.Single(limited.Headers.GetValues("Retry-After"));
            var retryAfterSeconds = int.Parse(retryAfter);
            Assert.InRange(retryAfterSeconds, 1, 60);

            Assert.Equal("application/json", limited.Content.Headers.ContentType?.MediaType);
            using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
            Assert.Equal("rate_limited", body.RootElement.GetProperty("error").GetString());
            Assert.Equal(retryAfterSeconds, body.RootElement.GetProperty("retry_after_seconds").GetInt32());
        }

        [Fact]
        public async Task Api_Key_Client_Uses_Authenticated_Limit()
        {
            await using var factory = CreateFactory();
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");

            for (var i = 0; i < AuthenticatedLimit; i++)
            {
                var ok = await client.GetAsync("/api/educations");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
            }

            var limited = await client.GetAsync("/api/educations");
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Fact]
        public async Task Health_Endpoint_Is_Not_Rate_Limited()
        {
            await using var factory = CreateFactory();
            var client = factory.CreateClient();

            for (var i = 0; i < AnonymousLimit + 2; i++)
            {
                var response = await client.GetAsync("/health");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }
}
