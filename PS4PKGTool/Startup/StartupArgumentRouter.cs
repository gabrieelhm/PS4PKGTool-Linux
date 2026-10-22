using System;
using System.IO;

namespace PS4PKGTool.Startup
{
    internal enum StartupRouteKind
    {
        NormalApplication,
        MiniPkgViewer,
        InvalidArguments
    }

    internal sealed class StartupRoute
    {
        private StartupRoute(StartupRouteKind kind, string packagePath, string errorMessage)
        {
            Kind = kind;
            PackagePath = packagePath;
            ErrorMessage = errorMessage;
        }

        public StartupRouteKind Kind { get; }
        public string PackagePath { get; }
        public string ErrorMessage { get; }

        public static StartupRoute NormalApplication() =>
            new StartupRoute(StartupRouteKind.NormalApplication, null, null);

        public static StartupRoute MiniPkgViewer(string packagePath) =>
            new StartupRoute(StartupRouteKind.MiniPkgViewer, packagePath, null);

        public static StartupRoute Invalid(string message) =>
            new StartupRoute(StartupRouteKind.InvalidArguments, null, message);
    }

    internal static class StartupArgumentRouter
    {
        public static StartupRoute Route(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return StartupRoute.NormalApplication();

            if (arguments.Length != 1)
            {
                return StartupRoute.Invalid(
                    "PS4 PKG Tool accepts either no arguments or one .pkg file path.");
            }

            string suppliedPath = arguments[0];
            if (string.IsNullOrWhiteSpace(suppliedPath))
                return StartupRoute.Invalid("The supplied package path is empty.");

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(suppliedPath);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return StartupRoute.Invalid("The supplied package path is invalid.");
            }

            if (!string.Equals(Path.GetExtension(fullPath), ".pkg", StringComparison.OrdinalIgnoreCase))
                return StartupRoute.Invalid("The supplied file must have a .pkg extension.");

            if (!File.Exists(fullPath))
                return StartupRoute.Invalid("The supplied package file does not exist:\n\n" + fullPath);

            try
            {
                if (new FileInfo(fullPath).Length == 0)
                    return StartupRoute.Invalid("The supplied package file is empty:\n\n" + fullPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return StartupRoute.Invalid("The supplied package file cannot be accessed:\n\n" + fullPath);
            }

            return StartupRoute.MiniPkgViewer(fullPath);
        }
    }
}
