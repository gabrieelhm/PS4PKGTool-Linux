using System;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>Test seam for the WER-report discovery polling delay.</summary>
    public interface IDelay
    {
        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    /// <summary>The real delay.</summary>
    public sealed class TaskDelay : IDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            => Task.Delay(delay, cancellationToken);
    }
}
