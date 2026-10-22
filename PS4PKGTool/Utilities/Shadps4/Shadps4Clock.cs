using System;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>Test seam for time-dependent behavior (manifest timestamps, cache TTL).</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>The real clock.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
