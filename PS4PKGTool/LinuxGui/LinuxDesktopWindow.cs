using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PS4PKGTool.Utilities;

namespace PS4PKGTool.LinuxGui
{
    /// <summary>
    /// Native Linux GTK3 + WebKit2GTK desktop window and native GTK3 FileChooser dialogs.
    /// Uses P/Invoke against system libgtk-3.so.0 and libwebkit2gtk-4.1.so.0 (or 4.0).
    /// </summary>
    public static class LinuxDesktopWindow
    {
        private const string LibGtk = "libgtk-3.so.0";
        private const string LibGObject = "libgobject-2.0.so.0";
        private const string LibGLib = "libglib-2.0.so.0";

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_init_check(ref int argc, IntPtr argv);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_window_new(int type);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_window_set_title(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_window_set_default_size(IntPtr window, int width, int height);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_window_set_position(IntPtr window, int position);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_window_set_icon_from_file(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr err);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_container_add(IntPtr container, IntPtr widget);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_widget_show_all(IntPtr widget);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_widget_destroy(IntPtr widget);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_main();

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_main_quit();

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_dialog_new(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
            IntPtr parent,
            int action,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string firstButtonText,
            int firstButtonResponse,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string secondButtonText,
            int secondButtonResponse,
            IntPtr terminator);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern int gtk_dialog_run(IntPtr dialog);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_get_filenames(IntPtr chooser);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_select_multiple(IntPtr chooser, bool selectMultiple);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_current_name(IntPtr chooser, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_file_chooser_set_current_folder(IntPtr chooser, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, bool doOverwriteConfirmation);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_filter_new();

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_filter_set_name(IntPtr filter, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_filter_add_pattern(IntPtr filter, [MarshalAs(UnmanagedType.LPUTF8Str)] string pattern);

        [DllImport(LibGtk, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong g_signal_connect_data(
            IntPtr instance,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string detailedSignal,
            IntPtr cHandler,
            IntPtr data,
            IntPtr destroyData,
            int connectFlags);

        [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint g_idle_add(GSourceFunc function, IntPtr data);

        [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void g_free(IntPtr mem);

        [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void g_slist_free(IntPtr list);

        [StructLayout(LayoutKind.Sequential)]
        private struct GSList
        {
            public IntPtr data;
            public IntPtr next;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DestroySignalHandler(IntPtr widget, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GSourceFunc(IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr WebKitWebViewNewDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WebKitWebViewLoadUriDelegate(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string uri);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr WebKitWebViewGetSettingsDelegate(IntPtr webView);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WebKitSettingsSetBoolDelegate(IntPtr settings, bool enabled);

        private const int GTK_WINDOW_TOPLEVEL = 0;
        private const int GTK_WIN_POS_CENTER = 1;

        private const int GTK_FILE_CHOOSER_ACTION_OPEN = 0;
        private const int GTK_FILE_CHOOSER_ACTION_SAVE = 1;
        private const int GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER = 2;

        private const int GTK_RESPONSE_ACCEPT = -3;
        private const int GTK_RESPONSE_CANCEL = -6;

        private static IntPtr _mainWindow = IntPtr.Zero;
        private static IntPtr _webView = IntPtr.Zero;
        private static DestroySignalHandler? _destroyHandler;
        private static WebKitWebViewLoadUriDelegate? _webkitLoadUri;
        private static bool _gtkRunning;

        public static bool IsGtkRunning => _gtkRunning;

        private static bool TryLoadWebKit(out WebKitWebViewNewDelegate? newView, out WebKitWebViewLoadUriDelegate? loadUri, out WebKitWebViewGetSettingsDelegate? getSettings, out IntPtr libHandle)
        {
            newView = null;
            loadUri = null;
            getSettings = null;
            libHandle = IntPtr.Zero;

            string[] candidates =
            {
                "libwebkit2gtk-4.1.so.0",
                "/usr/lib/libwebkit2gtk-4.1.so.0",
                "/usr/lib/x86_64-linux-gnu/libwebkit2gtk-4.1.so.0",
                "libwebkit2gtk-4.0.so.37",
                "/usr/lib/libwebkit2gtk-4.0.so.37",
                "/usr/lib/x86_64-linux-gnu/libwebkit2gtk-4.0.so.37"
            };

            foreach (string candidate in candidates)
            {
                if (NativeLibrary.TryLoad(candidate, out libHandle))
                {
                    if (NativeLibrary.TryGetExport(libHandle, "webkit_web_view_new", out IntPtr pNew) &&
                        NativeLibrary.TryGetExport(libHandle, "webkit_web_view_load_uri", out IntPtr pLoad))
                    {
                        newView = Marshal.GetDelegateForFunctionPointer<WebKitWebViewNewDelegate>(pNew);
                        loadUri = Marshal.GetDelegateForFunctionPointer<WebKitWebViewLoadUriDelegate>(pLoad);
                        if (NativeLibrary.TryGetExport(libHandle, "webkit_web_view_get_settings", out IntPtr pSettings))
                            getSettings = Marshal.GetDelegateForFunctionPointer<WebKitWebViewGetSettingsDelegate>(pSettings);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void RunWindow(string url, string title, int width, int height, string? iconPath)
        {
            // Disable WebKit2GTK DMABUF renderer issue on some Wayland compositors/NVIDIA/Mesa setups
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER")))
                Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");

            int argc = 0;
            bool gtkOk = false;
            try
            {
                gtkOk = gtk_init_check(ref argc, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("GTK3 initialization failed: " + ex.Message);
            }

            if (!gtkOk || !TryLoadWebKit(out var webkitNew, out var webkitLoad, out var webkitGetSettings, out var webkitLib))
            {
                Logger.LogWarning("Native GTK3/WebKit2GTK unavailable; opening URL in default browser: " + url);
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = $"\"{url}\"", UseShellExecute = false });
                }
                catch { }
                Thread.Sleep(Timeout.Infinite);
                return;
            }

            _webkitLoadUri = webkitLoad;
            _mainWindow = gtk_window_new(GTK_WINDOW_TOPLEVEL);
            gtk_window_set_title(_mainWindow, title);
            gtk_window_set_default_size(_mainWindow, width, height);
            gtk_window_set_position(_mainWindow, GTK_WIN_POS_CENTER);

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                try { gtk_window_set_icon_from_file(_mainWindow, iconPath, out _); }
                catch { }
            }

            _webView = webkitNew!();
            if (webkitGetSettings != null)
            {
                try
                {
                    IntPtr settings = webkitGetSettings(_webView);
                    if (settings != IntPtr.Zero)
                    {
                        EnableWebKitSetting(webkitLib, settings, "webkit_settings_set_enable_webaudio", true);
                        EnableWebKitSetting(webkitLib, settings, "webkit_settings_set_enable_webgl", true);
                        EnableWebKitSetting(webkitLib, settings, "webkit_settings_set_javascript_can_access_clipboard", true);
                        EnableWebKitSetting(webkitLib, settings, "webkit_settings_set_enable_developer_extras", true);
                    }
                }
                catch { }
            }

            gtk_container_add(_mainWindow, _webView);
            _webkitLoadUri!(_webView, url);

            _destroyHandler = OnWindowDestroy;
            IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(_destroyHandler);
            g_signal_connect_data(_mainWindow, "destroy", fnPtr, IntPtr.Zero, IntPtr.Zero, 0);

            gtk_widget_show_all(_mainWindow);
            _gtkRunning = true;
            gtk_main();
            _gtkRunning = false;
        }

        private static void EnableWebKitSetting(IntPtr webkitLib, IntPtr settings, string funcName, bool value)
        {
            if (NativeLibrary.TryGetExport(webkitLib, funcName, out IntPtr pFunc))
            {
                var fn = Marshal.GetDelegateForFunctionPointer<WebKitSettingsSetBoolDelegate>(pFunc);
                fn(settings, value);
            }
        }

        private static void OnWindowDestroy(IntPtr widget, IntPtr data)
        {
            _gtkRunning = false;
            gtk_main_quit();
        }

        public static Task<string?> ShowSelectFolderDialogAsync(string title, string? initialFolder = null)
        {
            if (!_gtkRunning)
                return RunZenityFallbackAsync($"--file-selection --directory --title=\"{EscapeArg(title)}\"");

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            GSourceFunc? callback = null;
            callback = _ =>
            {
                try
                {
                    IntPtr dlg = gtk_file_chooser_dialog_new(
                        title,
                        _mainWindow,
                        GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
                        "_Cancel", GTK_RESPONSE_CANCEL,
                        "_Select", GTK_RESPONSE_ACCEPT,
                        IntPtr.Zero);

                    if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                        gtk_file_chooser_set_current_folder(dlg, initialFolder);

                    int res = gtk_dialog_run(dlg);
                    string? selected = null;
                    if (res == GTK_RESPONSE_ACCEPT)
                    {
                        IntPtr ptr = gtk_file_chooser_get_filename(dlg);
                        if (ptr != IntPtr.Zero)
                        {
                            selected = Marshal.PtrToStringUTF8(ptr);
                            g_free(ptr);
                        }
                    }
                    gtk_widget_destroy(dlg);
                    tcs.TrySetResult(selected);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                GC.KeepAlive(callback);
                return false;
            };
            g_idle_add(callback, IntPtr.Zero);
            return tcs.Task;
        }

        public static Task<string[]> ShowOpenFileDialogAsync(string title, string? filterName = null, string[]? patterns = null, bool multiSelect = false, string? initialFolder = null)
        {
            if (!_gtkRunning)
            {
                return Task.Run(async () =>
                {
                    string? single = await RunZenityFallbackAsync($"--file-selection {(multiSelect ? "--multiple --separator=\"|\"" : "")} --title=\"{EscapeArg(title)}\"").ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(single)) return Array.Empty<string>();
                    return single.Split('|', StringSplitOptions.RemoveEmptyEntries);
                });
            }

            var tcs = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            GSourceFunc? callback = null;
            callback = _ =>
            {
                try
                {
                    IntPtr dlg = gtk_file_chooser_dialog_new(
                        title,
                        _mainWindow,
                        GTK_FILE_CHOOSER_ACTION_OPEN,
                        "_Cancel", GTK_RESPONSE_CANCEL,
                        "_Open", GTK_RESPONSE_ACCEPT,
                        IntPtr.Zero);

                    gtk_file_chooser_set_select_multiple(dlg, multiSelect);
                    if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                        gtk_file_chooser_set_current_folder(dlg, initialFolder);

                    if (patterns != null && patterns.Length > 0)
                    {
                        IntPtr filter = gtk_file_filter_new();
                        gtk_file_filter_set_name(filter, filterName ?? "Supported Files");
                        foreach (var pat in patterns)
                            gtk_file_filter_add_pattern(filter, pat);
                        gtk_file_chooser_add_filter(dlg, filter);

                        IntPtr allFilter = gtk_file_filter_new();
                        gtk_file_filter_set_name(allFilter, "All Files (*)");
                        gtk_file_filter_add_pattern(allFilter, "*");
                        gtk_file_chooser_add_filter(dlg, allFilter);
                    }

                    int res = gtk_dialog_run(dlg);
                    var results = new List<string>();
                    if (res == GTK_RESPONSE_ACCEPT)
                    {
                        if (multiSelect)
                        {
                            IntPtr listPtr = gtk_file_chooser_get_filenames(dlg);
                            IntPtr current = listPtr;
                            while (current != IntPtr.Zero)
                            {
                                GSList node = Marshal.PtrToStructure<GSList>(current);
                                if (node.data != IntPtr.Zero)
                                {
                                    string? path = Marshal.PtrToStringUTF8(node.data);
                                    if (!string.IsNullOrEmpty(path))
                                        results.Add(path);
                                    g_free(node.data);
                                }
                                current = node.next;
                            }
                            if (listPtr != IntPtr.Zero)
                                g_slist_free(listPtr);
                        }
                        else
                        {
                            IntPtr ptr = gtk_file_chooser_get_filename(dlg);
                            if (ptr != IntPtr.Zero)
                            {
                                string? path = Marshal.PtrToStringUTF8(ptr);
                                if (!string.IsNullOrEmpty(path))
                                    results.Add(path);
                                g_free(ptr);
                            }
                        }
                    }
                    gtk_widget_destroy(dlg);
                    tcs.TrySetResult(results.ToArray());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                GC.KeepAlive(callback);
                return false;
            };
            g_idle_add(callback, IntPtr.Zero);
            return tcs.Task;
        }

        public static Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string? initialFolder = null)
        {
            if (!_gtkRunning)
                return RunZenityFallbackAsync($"--file-selection --save --confirm-overwrite --filename=\"{EscapeArg(defaultFileName)}\" --title=\"{EscapeArg(title)}\"");

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            GSourceFunc? callback = null;
            callback = _ =>
            {
                try
                {
                    IntPtr dlg = gtk_file_chooser_dialog_new(
                        title,
                        _mainWindow,
                        GTK_FILE_CHOOSER_ACTION_SAVE,
                        "_Cancel", GTK_RESPONSE_CANCEL,
                        "_Save", GTK_RESPONSE_ACCEPT,
                        IntPtr.Zero);

                    gtk_file_chooser_set_do_overwrite_confirmation(dlg, true);
                    if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                        gtk_file_chooser_set_current_folder(dlg, initialFolder);
                    if (!string.IsNullOrWhiteSpace(defaultFileName))
                        gtk_file_chooser_set_current_name(dlg, defaultFileName);

                    int res = gtk_dialog_run(dlg);
                    string? selected = null;
                    if (res == GTK_RESPONSE_ACCEPT)
                    {
                        IntPtr ptr = gtk_file_chooser_get_filename(dlg);
                        if (ptr != IntPtr.Zero)
                        {
                            selected = Marshal.PtrToStringUTF8(ptr);
                            g_free(ptr);
                        }
                    }
                    gtk_widget_destroy(dlg);
                    tcs.TrySetResult(selected);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                GC.KeepAlive(callback);
                return false;
            };
            g_idle_add(callback, IntPtr.Zero);
            return tcs.Task;
        }

        private static async Task<string?> RunZenityFallbackAsync(string args)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                if (p == null) return null;
                string output = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await p.WaitForExitAsync().ConfigureAwait(false);
                return p.ExitCode == 0 ? output.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string EscapeArg(string s) => s.Replace("\"", "\\\"");
    }
}
