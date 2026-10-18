using System;

namespace GameJam2026
{
    public sealed class CountdownClock
    {
        public double RemainingSeconds { get; private set; }
        public bool IsExpired => RemainingSeconds <= 0;
        public int DisplaySeconds => (int)Math.Ceiling(RemainingSeconds);

        public CountdownClock(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            RemainingSeconds = seconds;
        }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
        }
    }
}
