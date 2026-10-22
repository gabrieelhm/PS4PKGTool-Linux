using System;
using System.IO;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// Small free-space helpers shared by the merge and FFPFSC option dialogs.
    /// Works for both local drives and mapped network (SMB) shares, since
    /// <see cref="DriveInfo"/> resolves the volume root of any path.
    /// </summary>
    internal static class DiskSpaceHelper
    {
        /// <summary>
        /// Returns the available free space (bytes) for the volume that holds
        /// <paramref name="path"/>, or -1 if it cannot be determined (empty
        /// path, nonexistent root, unmapped/offline drive, etc.).
        /// </summary>
        public static long GetAvailableFreeSpace(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return -1;

            try
            {
                string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? string.Empty;
                if (string.IsNullOrEmpty(root))
                    return -1;

                return new DriveInfo(root).AvailableFreeSpace;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
