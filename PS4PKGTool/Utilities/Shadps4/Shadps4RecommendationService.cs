using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// How confident we are that a core/launcher pair is compatible. v1 only
    /// ever emits Verified (an explicit upstream statement) or Unknown -
    /// compatibility is NEVER inferred from timestamps, dates or directory
    /// proximity (that inference caused a real crash incident).
    /// </summary>
    public enum Shadps4CompatibilityConfidence
    {
        Unknown,
        Verified,
        UpstreamBounded,
        SameGeneration,
        Incompatible,
    }

    /// <summary>What the "Install Recommended shadPS4 Setup" action would install.</summary>
    public sealed record Shadps4RecommendedSetup(
        Shadps4ReleaseInfo? Core,
        Shadps4ReleaseInfo? QtLauncher,
        Shadps4CompatibilityConfidence Compatibility);

    /// <summary>
    /// Upstream policy, kept OUT of the UI: the recommended core is the
    /// latest stable (non-prerelease) core release; the recommended launcher
    /// is the latest QtLauncher release, honestly labeled (upstream publishes
    /// it as a pre-release). No authoritative compatibility mapping is
    /// published, so the pairing is reported as Unknown.
    /// </summary>
    public sealed class Shadps4RecommendationService
    {
        private readonly IShadps4FeedClient _feed;

        public Shadps4RecommendationService(IShadps4FeedClient feed)
        {
            _feed = feed;
        }

        public async Task<Shadps4RecommendedSetup> GetRecommendedAsync(CancellationToken ct = default)
        {
            var coreTask = _feed.GetReleasesAsync(Shadps4FeedKind.CoreStable, ct);
            var launcherTask = _feed.GetReleasesAsync(Shadps4FeedKind.QtLauncher, ct);
            await Task.WhenAll(coreTask, launcherTask).ConfigureAwait(false);

            var core = coreTask.Result.Releases?
                .Where(r => !r.IsPrerelease)
                .OrderByDescending(r => r.PublishedUtc)
                .FirstOrDefault();
            var launcher = launcherTask.Result.Releases?
                .OrderByDescending(r => r.PublishedUtc)
                .FirstOrDefault();

            return new Shadps4RecommendedSetup(core, launcher, Shadps4CompatibilityConfidence.Unknown);
        }
    }
}
