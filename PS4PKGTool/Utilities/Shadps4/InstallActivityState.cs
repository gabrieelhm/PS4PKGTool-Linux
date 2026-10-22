namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>Explicit states of a game installation owned by the shadPS4 Manager.</summary>
    public enum InstallActivityState
    {
        Idle,
        Preparing,
        Running,
        Cancelling,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Legal state transitions for an installation. Terminal states
    /// (Completed / Cancelled / Failed) are final and can only move back to
    /// Idle (dismissal). A Cancelling state may still resolve to Completed
    /// when the cancellation request crossed the non-cancellable commit
    /// boundary and the operation genuinely finished - the real result wins
    /// over the requested cancel.
    /// </summary>
    public static class InstallActivityStateMachine
    {
        public static bool IsValidTransition(InstallActivityState from, InstallActivityState to)
        {
            switch (from)
            {
                case InstallActivityState.Idle:
                    return to == InstallActivityState.Preparing;
                case InstallActivityState.Preparing:
                    return to is InstallActivityState.Running
                        or InstallActivityState.Cancelled
                        or InstallActivityState.Failed;
                case InstallActivityState.Running:
                    return to is InstallActivityState.Cancelling
                        or InstallActivityState.Completed
                        or InstallActivityState.Cancelled
                        or InstallActivityState.Failed;
                case InstallActivityState.Cancelling:
                    return to is InstallActivityState.Cancelled
                        or InstallActivityState.Completed
                        or InstallActivityState.Failed;
                case InstallActivityState.Completed:
                case InstallActivityState.Cancelled:
                case InstallActivityState.Failed:
                    return to == InstallActivityState.Idle;
                default:
                    return false;
            }
        }
    }
}
