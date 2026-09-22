using System.Collections.Concurrent;

namespace WebApi.RateLimiting
{
    public readonly record struct RateLimitResult(bool IsAllowed, int RetryAfterSeconds);

    public interface IRateLimiter
    {
        RateLimitResult TryAcquire(string clientKey, int limitPerMinute);
    }

    /// <summary>
    /// In-memory fixed-window limiter: each client key gets at most <c>limitPerMinute</c>
    /// requests in the window that started when its first request arrived.
    /// </summary>
    public class RateLimiter : IRateLimiter
    {
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private readonly Func<DateTimeOffset> _clock;
        private readonly ConcurrentDictionary<string, WindowState> _windows = new();

        public RateLimiter() : this(() => DateTimeOffset.UtcNow) { }

        public RateLimiter(Func<DateTimeOffset> clock)
        {
            _clock = clock;
        }

        public RateLimitResult TryAcquire(string clientKey, int limitPerMinute)
        {
            var now = _clock();
            var state = _windows.GetOrAdd(clientKey, _ => new WindowState());

            lock (state)
            {
                if (now - state.WindowStart >= Window)
                {
                    state.WindowStart = now;
                    state.Count = 0;
                }

                if (state.Count < limitPerMinute)
                {
                    state.Count++;
                    return new RateLimitResult(true, 0);
                }

                var remaining = state.WindowStart + Window - now;
                var retryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
                return new RateLimitResult(false, retryAfter);
            }
        }

        private sealed class WindowState
        {
            public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.MinValue;
            public int Count { get; set; }
        }
    }
}
