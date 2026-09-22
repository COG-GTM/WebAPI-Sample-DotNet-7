using WebApi.RateLimiting;

namespace WebApi.Tests
{
    public class RateLimiterTests
    {
        private static readonly DateTimeOffset Start = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Allows_Requests_Up_To_The_Limit()
        {
            var limiter = new RateLimiter(() => Start);

            for (var i = 0; i < 3; i++)
            {
                Assert.True(limiter.TryAcquire("ip:1.1.1.1", 3).IsAllowed);
            }
        }

        [Fact]
        public void Rejects_Request_Over_The_Limit_With_Retry_After()
        {
            var now = Start;
            var limiter = new RateLimiter(() => now);

            limiter.TryAcquire("ip:1.1.1.1", 2);
            limiter.TryAcquire("ip:1.1.1.1", 2);
            now = Start.AddSeconds(15);

            var result = limiter.TryAcquire("ip:1.1.1.1", 2);

            Assert.False(result.IsAllowed);
            Assert.Equal(45, result.RetryAfterSeconds);
        }

        [Fact]
        public void Retry_After_Is_At_Least_One_Second()
        {
            var now = Start;
            var limiter = new RateLimiter(() => now);

            limiter.TryAcquire("ip:1.1.1.1", 1);
            now = Start.AddSeconds(59).AddMilliseconds(900);

            var result = limiter.TryAcquire("ip:1.1.1.1", 1);

            Assert.False(result.IsAllowed);
            Assert.Equal(1, result.RetryAfterSeconds);
        }

        [Fact]
        public void Resets_When_Window_Elapses()
        {
            var now = Start;
            var limiter = new RateLimiter(() => now);

            limiter.TryAcquire("ip:1.1.1.1", 1);
            Assert.False(limiter.TryAcquire("ip:1.1.1.1", 1).IsAllowed);

            now = Start.AddMinutes(1);

            Assert.True(limiter.TryAcquire("ip:1.1.1.1", 1).IsAllowed);
        }

        [Fact]
        public void Tracks_Clients_Independently()
        {
            var limiter = new RateLimiter(() => Start);

            limiter.TryAcquire("ip:1.1.1.1", 1);

            Assert.False(limiter.TryAcquire("ip:1.1.1.1", 1).IsAllowed);
            Assert.True(limiter.TryAcquire("ip:2.2.2.2", 1).IsAllowed);
            Assert.True(limiter.TryAcquire("key:abc", 1).IsAllowed);
        }

        [Fact]
        public void Evicts_Expired_Clients()
        {
            var now = Start;
            var limiter = new RateLimiter(() => now);

            limiter.TryAcquire("ip:1.1.1.1", 5);
            limiter.TryAcquire("ip:2.2.2.2", 5);
            Assert.Equal(2, limiter.TrackedClients);

            now = Start.AddMinutes(2);
            limiter.TryAcquire("ip:3.3.3.3", 5);

            Assert.Equal(1, limiter.TrackedClients);
        }

        [Fact]
        public async Task Is_Safe_Under_Concurrent_Access()
        {
            var limiter = new RateLimiter(() => Start);
            var allowed = 0;

            await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => Task.Run(() =>
            {
                if (limiter.TryAcquire("ip:1.1.1.1", 50).IsAllowed)
                {
                    Interlocked.Increment(ref allowed);
                }
            })));

            Assert.Equal(50, allowed);
        }
    }
}
