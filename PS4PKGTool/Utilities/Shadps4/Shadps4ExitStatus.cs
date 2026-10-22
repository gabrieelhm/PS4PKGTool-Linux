using System;

namespace PS4PKGTool.Utilities.Shadps4
{
    public enum Shadps4ExitCategory
    {
        NormalExit,
        UserTermination,
        LoaderError,
        EntryPointError,
        InitializationError,
        AccessViolation,
        StackOverflow,
        Breakpoint,
        FailFast,
        OtherErrorStatus,
        Unknown,
    }

    /// <summary>Maps a process exit code (Windows NTSTATUS or Linux POSIX signal) to a category and status name.</summary>
    public static class Shadps4ExitStatus
    {
        public static Shadps4ExitCategory Classify(uint code) => code switch
        {
            0x00000000 => Shadps4ExitCategory.NormalExit,
            // POSIX signal exit codes (128 + sig or negative signal)
            130 or 143 or unchecked((uint)-2) or unchecked((uint)-15) => Shadps4ExitCategory.UserTermination,
            127 => Shadps4ExitCategory.LoaderError,
            139 or unchecked((uint)-11) => Shadps4ExitCategory.AccessViolation,   // SIGSEGV (11)
            134 or unchecked((uint)-6) => Shadps4ExitCategory.FailFast,           // SIGABRT (6)
            133 or unchecked((uint)-5) => Shadps4ExitCategory.Breakpoint,         // SIGTRAP (5)
            132 or unchecked((uint)-4) => Shadps4ExitCategory.OtherErrorStatus,   // SIGILL (4)
            135 or unchecked((uint)-7) => Shadps4ExitCategory.AccessViolation,    // SIGBUS (7)
            136 or unchecked((uint)-8) => Shadps4ExitCategory.OtherErrorStatus,   // SIGFPE (8)
            // Windows NTSTATUS codes
            0xC000013A => Shadps4ExitCategory.UserTermination,
            0xC0000135 => Shadps4ExitCategory.LoaderError,
            0xC0000139 => Shadps4ExitCategory.EntryPointError,
            0xC0000142 => Shadps4ExitCategory.InitializationError,
            0xC0000005 => Shadps4ExitCategory.AccessViolation,
            0xC00000FD => Shadps4ExitCategory.StackOverflow,
            0x80000003 => Shadps4ExitCategory.Breakpoint,
            0xC0000409 => Shadps4ExitCategory.FailFast,
            _ when code >= 0xC0000000 => Shadps4ExitCategory.OtherErrorStatus,
            _ => Shadps4ExitCategory.Unknown,
        };

        public static string StatusName(uint code) => code switch
        {
            0x00000000 => "STATUS_SUCCESS",
            130 or unchecked((uint)-2) => "SIGINT",
            143 or unchecked((uint)-15) => "SIGTERM",
            127 => "COMMAND_OR_LIB_NOT_FOUND",
            139 or unchecked((uint)-11) => "SIGSEGV (Segmentation Fault)",
            134 or unchecked((uint)-6) => "SIGABRT (Aborted)",
            133 or unchecked((uint)-5) => "SIGTRAP",
            132 or unchecked((uint)-4) => "SIGILL (Illegal Instruction)",
            135 or unchecked((uint)-7) => "SIGBUS (Bus Error)",
            136 or unchecked((uint)-8) => "SIGFPE (Floating Point Exception)",
            0xC000013A => "STATUS_CONTROL_C_EXIT",
            0xC0000135 => "STATUS_DLL_NOT_FOUND",
            0xC0000139 => "STATUS_ENTRYPOINT_NOT_FOUND",
            0xC0000142 => "STATUS_DLL_INIT_FAILED",
            0xC0000005 => "STATUS_ACCESS_VIOLATION",
            0xC00000FD => "STATUS_STACK_OVERFLOW",
            0x80000003 => "STATUS_BREAKPOINT",
            0xC0000409 => "STATUS_STACK_BUFFER_OVERRUN",
            _ => "",
        };

        public static bool IsErrorStatus(Shadps4ExitCategory category)
            => category is Shadps4ExitCategory.LoaderError
                or Shadps4ExitCategory.EntryPointError
                or Shadps4ExitCategory.InitializationError
                or Shadps4ExitCategory.AccessViolation
                or Shadps4ExitCategory.StackOverflow
                or Shadps4ExitCategory.Breakpoint
                or Shadps4ExitCategory.FailFast
                or Shadps4ExitCategory.OtherErrorStatus;
    }
}
