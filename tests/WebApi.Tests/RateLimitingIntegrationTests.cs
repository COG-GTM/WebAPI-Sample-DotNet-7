using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebApi.Tests
{
    public class RateLimitingIntegrationTests
    {
        private const int UnauthenticatedLimit = 2;
        private const int AuthenticatedLimit = 4;

        private static WebApplicationFactory<Program> CreateFactory() =>
            new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimiting:UnauthenticatedRequestsPerMinute", UnauthenticatedLimit.ToString());
                builder.UseSetting("RateLimiting:AuthenticatedRequestsPerMinute", AuthenticatedLimit.ToString());
            });

        [Fact]
        public async Task Unauthenticated_Client_Over_Limit_Gets_429_With_Retry_After_And_Json_Body()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < UnauthenticatedLimit; i++)
            {
                var ok = await client.GetAsync("/api/educations");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
            }

            var response = await client.GetAsync("/api/educations");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.NotNull(response.Headers.RetryAfter?.Delta);
            var retryAfterHeader = (int)response.Headers.RetryAfter!.Delta!.Value.TotalSeconds;
            Assert.InRange(retryAfterHeader, 1, 60);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("rate_limited", body.RootElement.GetProperty("error").GetString());
            Assert.Equal(retryAfterHeader, body.RootElement.GetProperty("retry_after_seconds").GetInt32());
        }

        [Fact]
        public async Task Authenticated_Client_Uses_Api_Key_Limit()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");

            for (var i = 0; i < AuthenticatedLimit; i++)
            {
                var ok = await client.GetAsync("/api/educations");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
            }

            var response = await client.GetAsync("/api/educations");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task Different_Api_Keys_Are_Limited_Independently()
        {
            using var factory = CreateFactory();
            using var first = factory.CreateClient();
            first.DefaultRequestHeaders.Add("X-Api-Key", "key-one");
            using var second = factory.CreateClient();
            second.DefaultRequestHeaders.Add("X-Api-Key", "key-two");

            for (var i = 0; i <= AuthenticatedLimit; i++)
            {
                await first.GetAsync("/api/educations");
            }

            var response = await second.GetAsync("/api/educations");

            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task Unknown_Api_Key_Falls_Back_To_Ip_Limit_When_Allowlist_Configured()
        {
            using var factory = CreateFactory().WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:ApiKeys:0", "known-key"));
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "forged-key");

            for (var i = 0; i < UnauthenticatedLimit; i++)
            {
                await client.GetAsync("/api/educations");
            }

            var response = await client.GetAsync("/api/educations");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public void Invalid_Limit_Fails_At_Startup()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:UnauthenticatedRequestsPerMinute", "0"));

            Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(() => factory.CreateClient());
        }

        [Fact]
        public async Task Health_Check_Is_Not_Rate_Limited()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < UnauthenticatedLimit * 3; i++)
            {
                var response = await client.GetAsync("/health");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        [Fact]
        public async Task Limits_Are_Read_From_Configuration()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:UnauthenticatedRequestsPerMinute", "1"));
            using var client = factory.CreateClient();

            await client.GetAsync("/api/educations");
            var response = await client.GetAsync("/api/educations");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
