using System.Runtime.InteropServices;

namespace OrbisPkgTool.Pfs;

/// <summary>
/// Raw-deflate compressor with a 4 KiB history window, backed by native zlib
/// (libz.so.1 on Linux, zlib1.dll on Windows).
/// </summary>
public static class PfscDeflate
{
    // Windows LLP64: unsigned long = 32 bits
    [StructLayout(LayoutKind.Sequential)]
    private struct ZStreamWin
    {
        public IntPtr next_in;
        public uint avail_in;
        public uint total_in;
        public IntPtr next_out;
        public uint avail_out;
        public uint total_out;
        public IntPtr msg;
        public IntPtr state;
        public IntPtr zalloc;
        public IntPtr zfree;
        public IntPtr opaque;
        public int data_type;
        public uint adler;
        public uint reserved;
    }

    // Linux/macOS LP64: unsigned long = 64 bits (nuint)
    [StructLayout(LayoutKind.Sequential)]
    private struct ZStreamUnix
    {
        public IntPtr next_in;
        public uint avail_in;
        public nuint total_in;
        public IntPtr next_out;
        public uint avail_out;
        public nuint total_out;
        public IntPtr msg;
        public IntPtr state;
        public IntPtr zalloc;
        public IntPtr zfree;
        public IntPtr opaque;
        public int data_type;
        public nuint adler;
        public nuint reserved;
    }

    private const int ZOk = 0;
    private const int ZStreamEnd = 1;
    private const int ZFinish = 4;
    private const int ZDeflated = 8;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateInit2Win(ref ZStreamWin strm, int level, int method, int windowBits,
        int memLevel, int strategy, [MarshalAs(UnmanagedType.LPStr)] string version, int streamSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateWin(ref ZStreamWin strm, int flush);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateEndWin(ref ZStreamWin strm);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateInit2Unix(ref ZStreamUnix strm, int level, int method, int windowBits,
        int memLevel, int strategy, [MarshalAs(UnmanagedType.LPStr)] string version, int streamSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateUnix(ref ZStreamUnix strm, int flush);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeflateEndUnix(ref ZStreamUnix strm);

    private static readonly Lazy<bool> _loaded = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    private static bool _isUnixLayout;
    private static DeflateInit2Win? _deflateInit2Win;
    private static DeflateWin? _deflateWin;
    private static DeflateEndWin? _deflateEndWin;
    private static DeflateInit2Unix? _deflateInit2Unix;
    private static DeflateUnix? _deflateUnix;
    private static DeflateEndUnix? _deflateEndUnix;

    public static bool IsAvailable => _loaded.Value;

    private static bool Load()
    {
        try
        {
            IntPtr lib = IntPtr.Zero;
            string? env = Environment.GetEnvironmentVariable("ORBISPKG_ZLIB");
            if (!string.IsNullOrEmpty(env) && File.Exists(env))
                NativeLibrary.TryLoad(env, out lib);
            if (lib == IntPtr.Zero && !OperatingSystem.IsWindows())
            {
                NativeLibrary.TryLoad("libz.so.1", out lib);
                if (lib == IntPtr.Zero) NativeLibrary.TryLoad("libz.so", out lib);
                if (lib == IntPtr.Zero) NativeLibrary.TryLoad("libz.dylib", out lib);
            }
            if (lib == IntPtr.Zero)
                NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory, "zlib1.dll"), out lib);
            if (lib == IntPtr.Zero)
                NativeLibrary.TryLoad(@"C:\Program Files\Git\mingw64\bin\zlib1.dll", out lib);
            if (lib == IntPtr.Zero)
                NativeLibrary.TryLoad("zlib1", out lib);
            if (lib == IntPtr.Zero) return false;

            IntPtr pInit = NativeLibrary.GetExport(lib, "deflateInit2_");
            IntPtr pDeflate = NativeLibrary.GetExport(lib, "deflate");
            IntPtr pEnd = NativeLibrary.GetExport(lib, "deflateEnd");

            _isUnixLayout = !OperatingSystem.IsWindows();
            if (_isUnixLayout)
            {
                _deflateInit2Unix = Marshal.GetDelegateForFunctionPointer<DeflateInit2Unix>(pInit);
                _deflateUnix = Marshal.GetDelegateForFunctionPointer<DeflateUnix>(pDeflate);
                _deflateEndUnix = Marshal.GetDelegateForFunctionPointer<DeflateEndUnix>(pEnd);
            }
            else
            {
                _deflateInit2Win = Marshal.GetDelegateForFunctionPointer<DeflateInit2Win>(pInit);
                _deflateWin = Marshal.GetDelegateForFunctionPointer<DeflateWin>(pDeflate);
                _deflateEndWin = Marshal.GetDelegateForFunctionPointer<DeflateEndWin>(pEnd);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static int TryDeflate4K(byte[] input, int offset, int count, byte[] output)
    {
        if (!_loaded.Value) return -1;
        if (offset < 0 || count < 0 || offset + count > input.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (_isUnixLayout)
        {
            if (_deflateInit2Unix == null || _deflateUnix == null || _deflateEndUnix == null) return -1;
            var z = new ZStreamUnix();
            if (_deflateInit2Unix(ref z, PfsFormat.PfscDeflateLevel, ZDeflated, -12, 8, 0,
                    "1.2.13", Marshal.SizeOf<ZStreamUnix>()) != ZOk)
                return -1;
            var inPin = GCHandle.Alloc(input, GCHandleType.Pinned);
            var outPin = GCHandle.Alloc(output, GCHandleType.Pinned);
            try
            {
                z.next_in = IntPtr.Add(inPin.AddrOfPinnedObject(), offset);
                z.avail_in = (uint)count;
                z.next_out = outPin.AddrOfPinnedObject();
                z.avail_out = (uint)output.Length;
                int rc = _deflateUnix(ref z, ZFinish);
                while (rc == ZOk && z.avail_out > 0)
                    rc = _deflateUnix(ref z, ZFinish);
                if (rc != ZStreamEnd)
                    return -1;
                return (int)z.total_out;
            }
            finally
            {
                outPin.Free();
                inPin.Free();
                _deflateEndUnix(ref z);
            }
        }
        else
        {
            if (_deflateInit2Win == null || _deflateWin == null || _deflateEndWin == null) return -1;
            var z = new ZStreamWin();
            if (_deflateInit2Win(ref z, PfsFormat.PfscDeflateLevel, ZDeflated, -12, 8, 0,
                    "1.2.13", Marshal.SizeOf<ZStreamWin>()) != ZOk)
                return -1;
            var inPin = GCHandle.Alloc(input, GCHandleType.Pinned);
            var outPin = GCHandle.Alloc(output, GCHandleType.Pinned);
            try
            {
                z.next_in = IntPtr.Add(inPin.AddrOfPinnedObject(), offset);
                z.avail_in = (uint)count;
                z.next_out = outPin.AddrOfPinnedObject();
                z.avail_out = (uint)output.Length;
                int rc = _deflateWin(ref z, ZFinish);
                while (rc == ZOk && z.avail_out > 0)
                    rc = _deflateWin(ref z, ZFinish);
                if (rc != ZStreamEnd)
                    return -1;
                return (int)z.total_out;
            }
            finally
            {
                outPin.Free();
                inPin.Free();
                _deflateEndWin(ref z);
            }
        }
    }
}
