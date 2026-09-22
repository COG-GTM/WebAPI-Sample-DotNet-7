using System.Collections.Concurrent;
using System.Diagnostics;

namespace WebApi.RateLimiting
{
    public readonly record struct RateLimitDecision(bool IsAllowed, int RetryAfterSeconds);

    public interface IRateLimiter
    {
        RateLimitDecision TryAcquire(string clientKey, int limit);
    }

    /// <summary>
    /// In-memory fixed-window (one minute) counter keyed by client identifier.
    /// Uses a monotonic clock so wall-clock adjustments cannot stretch or shrink a window.
    /// </summary>
    public class FixedWindowRateLimiter : IRateLimiter
    {
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<string, WindowCounter> _counters = new();
        private readonly Func<TimeSpan> _clock;

        public FixedWindowRateLimiter() : this(() => Stopwatch.GetElapsedTime(0)) { }

        public FixedWindowRateLimiter(Func<TimeSpan> clock)
        {
            _clock = clock;
        }

        public RateLimitDecision TryAcquire(string clientKey, int limit)
        {
            var now = _clock();
            var counter = _counters.GetOrAdd(clientKey, _ => new WindowCounter());

            lock (counter)
            {
                if (now >= counter.WindowStart + Window)
                {
                    counter.WindowStart = now;
                    counter.Count = 0;
                }

                if (counter.Count < limit)
                {
                    counter.Count++;
                    return new RateLimitDecision(true, 0);
                }

                var remaining = counter.WindowStart + Window - now;
                var retryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
                return new RateLimitDecision(false, retryAfter);
            }
        }

        private sealed class WindowCounter
        {
            public TimeSpan WindowStart = TimeSpan.MinValue;
            public int Count;
        }
    }
}
