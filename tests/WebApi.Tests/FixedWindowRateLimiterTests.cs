using WebApi.RateLimiting;

namespace WebApi.Tests
{
    public class FixedWindowRateLimiterTests
    {
        [Fact]
        public void Allows_Requests_Up_To_Limit_Then_Rejects()
        {
            var limiter = new FixedWindowRateLimiter(() => TimeSpan.FromHours(1));

            for (var i = 0; i < 3; i++)
            {
                Assert.True(limiter.TryAcquire("client", 3).IsAllowed);
            }

            var rejected = limiter.TryAcquire("client", 3);
            Assert.False(rejected.IsAllowed);
            Assert.Equal(60, rejected.RetryAfterSeconds);
        }

        [Fact]
        public void RetryAfter_Reflects_Remaining_Window()
        {
            var now = TimeSpan.FromHours(1);
            var limiter = new FixedWindowRateLimiter(() => now);

            limiter.TryAcquire("client", 1);
            now += TimeSpan.FromSeconds(45.2);

            var rejected = limiter.TryAcquire("client", 1);
            Assert.False(rejected.IsAllowed);
            Assert.Equal(15, rejected.RetryAfterSeconds);
        }

        [Fact]
        public void Window_Resets_After_One_Minute()
        {
            var now = TimeSpan.FromHours(1);
            var limiter = new FixedWindowRateLimiter(() => now);

            limiter.TryAcquire("client", 1);
            Assert.False(limiter.TryAcquire("client", 1).IsAllowed);

            now += TimeSpan.FromMinutes(1);
            Assert.True(limiter.TryAcquire("client", 1).IsAllowed);
        }

        [Fact]
        public void Clients_Are_Tracked_Independently()
        {
            var limiter = new FixedWindowRateLimiter(() => TimeSpan.FromHours(1));

            limiter.TryAcquire("a", 1);
            Assert.False(limiter.TryAcquire("a", 1).IsAllowed);
            Assert.True(limiter.TryAcquire("b", 1).IsAllowed);
        }

        [Fact]
        public void Options_Default_To_Required_Limits()
        {
            var options = new RateLimitOptions();

            Assert.Equal(60, options.AnonymousRequestsPerMinute);
            Assert.Equal(600, options.AuthenticatedRequestsPerMinute);
            Assert.Equal("X-Api-Key", options.ApiKeyHeaderName);
        }
    }
}
