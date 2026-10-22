#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PS4PKGTool.LinuxGui;
using PS4PKGTool.Shell;
using PS4PKGTool.Startup;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Settings;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Directory.CreateDirectory(Helper.UserDataDirectory);
            Directory.CreateDirectory(Helper.AppDataDirectory);
            SettingsManager.LoadSettings(SettingsManager.SettingFilePath);

            if (ShellCommandRouter.IsShellMode(args))
            {
                var shellReq = ShellCommandRouter.Parse(args);
                if (shellReq == null)
                {
                    Console.Error.WriteLine("PS4 PKG Tool: invalid --shell arguments.");
                    ShellCommands.NotifyUser("PS4 PKG Tool", "Invalid shell command arguments.", isError: true);
                    return 1;
                }
                string? pathErr = ShellCommandRouter.ValidatePaths(shellReq.PackagePaths);
                if (pathErr != null)
                {
                    Console.Error.WriteLine("PS4 PKG Tool: " + pathErr);
                    ShellCommands.NotifyUser("PS4 PKG Tool", pathErr, isError: true);
                    return 1;
                }
                ShellCommands.Run(shellReq);
                return 0;
            }

            var route = StartupArgumentRouter.Route(args);
            if (route.Kind == StartupRouteKind.InvalidArguments)
            {
                Console.Error.WriteLine("PS4 PKG Tool: " + route.ErrorMessage);
                ShellCommands.NotifyUser("PS4 PKG Tool", route.ErrorMessage ?? "Invalid arguments.", isError: true);
                return 1;
            }

            var backend = new AppBackendService(route);
            string wwwrootPath = FindWwwrootPath();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = wwwrootPath
            });

            builder.Logging.ClearProviders();
            builder.WebHost.UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0);
            });

            var app = builder.Build();

            if (Directory.Exists(wwwrootPath))
            {
                var fileProvider = new PhysicalFileProvider(wwwrootPath);
                app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
                app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
            }

            MapApiEndpoints(app, backend);

            app.StartAsync().GetAwaiter().GetResult();

            var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
            string serverUrl = addresses?.FirstOrDefault() ?? "http://127.0.0.1:5000";
            Logger.LogInformation("PS4 PKG Tool Linux UI bound to " + serverUrl);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "PackageIcon.png");
            string windowTitle = route.Kind == StartupRouteKind.MiniPkgViewer
                ? $"Mini PKG Viewer - {Path.GetFileName(route.PackagePath)}"
                : "PS4 PKG Tool v1.9.0.0";

            int width = route.Kind == StartupRouteKind.MiniPkgViewer ? 980 : 1440;
            int height = route.Kind == StartupRouteKind.MiniPkgViewer ? 720 : 900;

            try
            {
                LinuxDesktopWindow.RunWindow(serverUrl, windowTitle, width, height, iconPath);
            }
            finally
            {
                Helper.BGM.StopBgm();
                Helper.PKGSENDER.StopBuiltInServer();
                try { app.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); } catch { }
            }

            return 0;
        }

        private static string FindWwwrootPath()
        {
            string candidate1 = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (Directory.Exists(candidate1)) return candidate1;
            string candidate2 = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            if (Directory.Exists(candidate2)) return candidate2;
            Directory.CreateDirectory(candidate1);
            return candidate1;
        }

        private static void MapApiEndpoints(WebApplication app, AppBackendService backend)
        {
            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            app.MapGet("/api/bootstrap", () => Results.Json(backend.GetBootstrapState(), jsonOpts));
            app.MapGet("/api/notifications", () => Results.Json(backend.DrainNotifications(), jsonOpts));
            app.MapGet("/api/logs", () => Results.Json(new { log = backend.GetLogContents() }, jsonOpts));
            app.MapPost("/api/logs/clear", () => { backend.ClearLog(); return Results.Json(new { ok = true }, jsonOpts); });

            // Native GTK3 File/Folder Dialogs
            app.MapPost("/api/dialog/folder", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "Select Folder" : "Select Folder";
                string? initial = doc.RootElement.TryGetProperty("initialFolder", out var inf) ? inf.GetString() : null;
                string? folder = await LinuxDesktopWindow.ShowSelectFolderDialogAsync(title, initial);
                return Results.Json(new { path = folder }, jsonOpts);
            });

            app.MapPost("/api/dialog/open-file", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "Select File" : "Select File";
                string? filterName = doc.RootElement.TryGetProperty("filterName", out var fn) ? fn.GetString() : null;
                bool multi = doc.RootElement.TryGetProperty("multiSelect", out var m) && m.GetBoolean();
                string[]? patterns = null;
                if (doc.RootElement.TryGetProperty("patterns", out var p) && p.ValueKind == JsonValueKind.Array)
                    patterns = p.EnumerateArray().Select(x => x.GetString() ?? "*").ToArray();
                string[] paths = await LinuxDesktopWindow.ShowOpenFileDialogAsync(title, filterName, patterns, multi);
                return Results.Json(new { paths }, jsonOpts);
            });

            app.MapPost("/api/dialog/save-file", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "Save File" : "Save File";
                string defaultName = doc.RootElement.TryGetProperty("defaultName", out var dn) ? dn.GetString() ?? "output.bin" : "output.bin";
                string? initial = doc.RootElement.TryGetProperty("initialFolder", out var inf) ? inf.GetString() : null;
                string? path = await LinuxDesktopWindow.ShowSaveFileDialogAsync(title, defaultName, initial);
                return Results.Json(new { path }, jsonOpts);
            });

            // Library & Manifest
            app.MapGet("/api/library", () => Results.Json(backend.GetLibrary(), jsonOpts));
            app.MapPost("/api/library/load-manifest", () => Results.Json(backend.LoadFromManifestFile(), jsonOpts));
            app.MapPost("/api/library/scan", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var root = doc.RootElement;
                List<string>? dirs = null;
                if (root.TryGetProperty("directories", out var dEl) && dEl.ValueKind == JsonValueKind.Array)
                    dirs = dEl.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                bool? recursive = root.TryGetProperty("recursive", out var rEl) && (rEl.ValueKind == JsonValueKind.True || rEl.ValueKind == JsonValueKind.False) ? rEl.GetBoolean() : null;
                bool append = root.TryGetProperty("append", out var aEl) && aEl.GetBoolean();
                var list = await Task.Run(() => backend.ScanDirectories(dirs, recursive, append));
                return Results.Json(list, jsonOpts);
            });

            app.MapPost("/api/library/add-files", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                bool append = !doc.RootElement.TryGetProperty("append", out var aEl) || aEl.GetBoolean();
                var list = await Task.Run(() => backend.ScanPackageFiles(files, append));
                return Results.Json(list, jsonOpts);
            });

            app.MapPost("/api/library/backport", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                bool backported = doc.RootElement.GetProperty("backported").GetBoolean();
                return Results.Json(backend.SetBackportFlag(files, backported), jsonOpts);
            });

            app.MapGet("/api/library/duplicates", () => Results.Json(backend.FindDuplicates(), jsonOpts));
            app.MapGet("/api/library/missing-base", () => Results.Json(backend.FindMissingBasePkgs(), jsonOpts));

            app.MapPost("/api/library/rename/preview", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                int formatId = doc.RootElement.GetProperty("formatId").GetInt32();
                string? custom = doc.RootElement.TryGetProperty("customPattern", out var cp) ? cp.GetString() : null;
                return Results.Json(backend.PreviewRename(files, formatId, custom), jsonOpts);
            });

            app.MapPost("/api/library/rename/execute", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                int formatId = doc.RootElement.GetProperty("formatId").GetInt32();
                string? custom = doc.RootElement.TryGetProperty("customPattern", out var cp) ? cp.GetString() : null;
                var (renamed, errors, lib) = backend.ExecuteRename(files, formatId, custom);
                return Results.Json(new { renamed, errors, library = lib }, jsonOpts);
            });

            app.MapPost("/api/library/move", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                string dest = doc.RootElement.GetProperty("destinationRoot").GetString() ?? "";
                string mode = doc.RootElement.TryGetProperty("subfolderMode", out var m) ? m.GetString() ?? "none" : "none";
                var (moved, errors, lib) = backend.MovePackages(files, dest, mode);
                return Results.Json(new { moved, errors, library = lib }, jsonOpts);
            });

            app.MapPost("/api/library/delete", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var files = doc.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                bool disk = doc.RootElement.TryGetProperty("deleteFromDisk", out var d) && d.GetBoolean();
                var (deleted, errors, lib) = backend.DeletePackages(files, disk);
                return Results.Json(new { deleted, errors, library = lib }, jsonOpts);
            });

            app.MapPost("/api/library/export-excel", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string output = doc.RootElement.GetProperty("outputPath").GetString() ?? "";
                string saved = backend.ExportLibraryToExcel(output);
                return Results.Json(new { savedPath = saved }, jsonOpts);
            });

            // PKG Inspection, Images, BGM, Wallpaper, Digests, Validation
            app.MapGet("/api/pkg/inspect", async (string path, CancellationToken ct) =>
            {
                var res = await backend.InspectPkgFullAsync(path, ct);
                return Results.Json(res, jsonOpts);
            });

            app.MapGet("/api/pkg/image", async (string path, string type, CancellationToken ct) =>
            {
                var snap = await backend.GetCachedInspectionAsync(path, ct);
                byte[]? bytes = type.ToLowerInvariant() switch
                {
                    "pic0" => snap.Pic0Bytes,
                    "pic1" => snap.Pic1Bytes,
                    _ => snap.Icon0Bytes
                };
                if (bytes == null || bytes.Length == 0)
                    return Results.NotFound();
                return Results.File(bytes, "image/png");
            });

            app.MapPost("/api/pkg/image/save", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string type = doc.RootElement.GetProperty("type").GetString() ?? "icon0";
                string outputPath = doc.RootElement.GetProperty("outputPath").GetString() ?? "";
                var snap = await backend.GetCachedInspectionAsync(pkgPath);

                if (type == "all")
                {
                    Directory.CreateDirectory(outputPath);
                    string prefix = IllegalNameCheck.SanitizeFileName(snap.Title);
                    int count = 0;
                    if (snap.Icon0Bytes is { Length: > 0 }) { await File.WriteAllBytesAsync(Path.Combine(outputPath, $"{prefix}_ICON0.PNG"), snap.Icon0Bytes); count++; }
                    if (snap.Pic0Bytes is { Length: > 0 }) { await File.WriteAllBytesAsync(Path.Combine(outputPath, $"{prefix}_PIC0.PNG"), snap.Pic0Bytes); count++; }
                    if (snap.Pic1Bytes is { Length: > 0 }) { await File.WriteAllBytesAsync(Path.Combine(outputPath, $"{prefix}_PIC1.PNG"), snap.Pic1Bytes); count++; }
                    return Results.Json(new { saved = count, outputPath }, jsonOpts);
                }

                byte[]? bytes = type.ToLowerInvariant() switch
                {
                    "pic0" => snap.Pic0Bytes,
                    "pic1" => snap.Pic1Bytes,
                    _ => snap.Icon0Bytes
                };
                if (bytes == null || bytes.Length == 0)
                    return Results.BadRequest(new { error = "Image not present in PKG." });
                await File.WriteAllBytesAsync(outputPath, bytes);
                return Results.Json(new { saved = 1, outputPath }, jsonOpts);
            });

            app.MapPost("/api/pkg/wallpaper", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() ?? "pic1" : "pic1";
                string styleStr = doc.RootElement.TryGetProperty("style", out var st) ? st.GetString() ?? "Stretched" : "Stretched";
                Enum.TryParse<PS4PKGTool.Util.Wallpaper.Style>(styleStr, true, out var style);
                var snap = await backend.GetCachedInspectionAsync(pkgPath);
                byte[]? bytes = type.Equals("pic0", StringComparison.OrdinalIgnoreCase) ? snap.Pic0Bytes : (snap.Pic1Bytes ?? snap.Icon0Bytes);
                if (bytes == null || bytes.Length == 0)
                    return Results.BadRequest(new { error = "No background image found in PKG." });
                bool ok = PS4PKGTool.Util.Wallpaper.SetFromBytes(bytes, style, out string err);
                return ok ? Results.Json(new { ok = true }, jsonOpts) : Results.BadRequest(new { error = err });
            });

            app.MapGet("/api/pkg/bgm", (string path) =>
            {
                byte[]? wav = Helper.BGM.DecodePkgBgmToWav(path);
                if (wav == null || wav.Length == 0)
                    return Results.NotFound();
                return Results.File(wav, "audio/wav");
            });

            app.MapPost("/api/pkg/bgm/stop", () =>
            {
                Helper.BGM.StopBgm();
                return Results.Json(new { ok = true }, jsonOpts);
            });

            app.MapPost("/api/pkg/entries/extract", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string outputDir = doc.RootElement.GetProperty("outputDir").GetString() ?? "";
                var ids = doc.RootElement.GetProperty("entryIds").EnumerateArray().Select(x => x.GetUInt32()).ToList();
                var (extracted, dir) = backend.ExtractPkgEntries(pkgPath, ids, outputDir);
                return Results.Json(new { extracted, outputDir = dir }, jsonOpts);
            });

            app.MapPost("/api/pkg/validate", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                return Results.Json(backend.ValidatePkgStructure(pkgPath), jsonOpts);
            });

            app.MapPost("/api/pkg/digests", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                bool dryRun = !doc.RootElement.TryGetProperty("dryRun", out var dr) || dr.GetBoolean();
                return Results.Json(backend.FixPkgDigests(pkgPath, dryRun), jsonOpts);
            });

            // Trophies
            app.MapGet("/api/pkg/trophies", async (string path, CancellationToken ct) =>
            {
                var res = await backend.InspectTrophiesAsync(path, ct);
                return Results.Json(res, jsonOpts);
            });

            app.MapPost("/api/pkg/trophies/extract", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string outputDir = doc.RootElement.GetProperty("outputDir").GetString() ?? "";
                string message = await backend.ExtractTrophiesAsync(pkgPath, outputDir);
                return Results.Json(new { message }, jsonOpts);
            });

            app.MapPost("/api/trophies/build-cache", async (CancellationToken ct) =>
            {
                var res = await backend.BuildTrophyCacheAsync(ct);
                return Results.Json(res, jsonOpts);
            });

            // File Browser & Asset Viewer
            app.MapGet("/api/pkg/files", async (string path, string? passcode, CancellationToken ct) =>
            {
                var res = await backend.ListPkgFilesAsync(path, passcode, ct);
                return Results.Json(res, jsonOpts);
            });

            app.MapPost("/api/pkg/files/export-tree", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string outputPath = doc.RootElement.GetProperty("outputPath").GetString() ?? "";
                string? passcode = doc.RootElement.TryGetProperty("passcode", out var pc) ? pc.GetString() : null;
                string saved = await backend.ExportFileTreeTxtAsync(pkgPath, outputPath, passcode);
                return Results.Json(new { savedPath = saved }, jsonOpts);
            });

            app.MapPost("/api/pkg/files/extract", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string internalPath = doc.RootElement.TryGetProperty("internalPath", out var ip) ? ip.GetString() ?? "/" : "/";
                bool isDir = doc.RootElement.TryGetProperty("isDirectory", out var id) && id.GetBoolean();
                string dest = doc.RootElement.GetProperty("destinationFolder").GetString() ?? "";
                string? passcode = doc.RootElement.TryGetProperty("passcode", out var pc) ? pc.GetString() : null;
                var (ok, msg, outPath) = await backend.ExtractSelectedPkgPathsAsync(pkgPath, internalPath, isDir, dest, passcode);
                return Results.Json(new { succeeded = ok, message = msg, outputPath = outPath }, jsonOpts);
            });

            app.MapGet("/api/pkg/asset/inspect", async (string path, string entry, string? passcode, CancellationToken ct) =>
            {
                var res = await backend.InspectPkgAssetEntryAsync(path, entry, passcode, ct);
                return Results.Json(res, jsonOpts);
            });

            app.MapGet("/api/pkg/asset/audio", async (string path, string entry, string? passcode, CancellationToken ct) =>
            {
                var (data, mime) = await backend.GetAssetAudioStreamAsync(path, entry, passcode, ct);
                return Results.File(data, mime);
            });

            app.MapPost("/api/pkg/asset/export-png", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string internalPath = doc.RootElement.GetProperty("internalPath").GetString() ?? "";
                string outputPng = doc.RootElement.GetProperty("outputPngPath").GetString() ?? "";
                string? passcode = doc.RootElement.TryGetProperty("passcode", out var pc) ? pc.GetString() : null;
                string saved = await backend.ExportDecodedAssetPngAsync(pkgPath, internalPath, outputPng, passcode);
                return Results.Json(new { savedPath = saved }, jsonOpts);
            });

            // Task Queue
            app.MapGet("/api/tasks", () => Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts));
            app.MapPost("/api/tasks/auto-start", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                backend.SetTaskQueueAutoStart(doc.RootElement.GetProperty("autoStart").GetBoolean());
                return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts);
            });
            app.MapPost("/api/tasks/start-next", () => { backend.StartNextQueuedTask(); return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts); });
            app.MapPost("/api/tasks/clear-completed", () => { backend.ClearCompletedTasks(); return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts); });
            app.MapPost("/api/tasks/cancel", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                backend.CancelTask(Guid.Parse(doc.RootElement.GetProperty("id").GetString()!));
                return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts);
            });
            app.MapPost("/api/tasks/retry", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                backend.RetryTask(Guid.Parse(doc.RootElement.GetProperty("id").GetString()!));
                return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts);
            });
            app.MapPost("/api/tasks/remove", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                backend.RemoveTask(Guid.Parse(doc.RootElement.GetProperty("id").GetString()!));
                return Results.Json(backend.GetTaskQueueSnapshot(), jsonOpts);
            });

            app.MapPost("/api/tasks/enqueue/extract", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string outputDir = doc.RootElement.GetProperty("outputDir").GetString() ?? "";
                bool raw = doc.RootElement.TryGetProperty("rawLayout", out var rl) && rl.GetBoolean();
                Guid id = backend.EnqueueFullExtraction(pkgPath, outputDir, raw);
                return Results.Json(new { id, queue = backend.GetTaskQueueSnapshot() }, jsonOpts);
            });

            app.MapPost("/api/tasks/enqueue/merge", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string basePkg = doc.RootElement.GetProperty("basePkgPath").GetString() ?? "";
                string updatePkg = doc.RootElement.GetProperty("updatePkgPath").GetString() ?? "";
                string outputPkg = doc.RootElement.GetProperty("outputPkgPath").GetString() ?? "";
                bool validate = !doc.RootElement.TryGetProperty("validateAfterBuild", out var v) || v.GetBoolean();
                string pfscMode = doc.RootElement.TryGetProperty("pfscMode", out var pm) ? pm.GetString() ?? "Auto" : "Auto";
                int workers = doc.RootElement.TryGetProperty("workerCount", out var wc) ? wc.GetInt32() : 4;
                Guid id = backend.EnqueueMergePkg(basePkg, updatePkg, outputPkg, validate, pfscMode, workers);
                return Results.Json(new { id, queue = backend.GetTaskQueueSnapshot() }, jsonOpts);
            });

            app.MapPost("/api/tasks/enqueue/ffpfsc", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string outputPath = doc.RootElement.GetProperty("outputPath").GetString() ?? "";
                int zlib = doc.RootElement.TryGetProperty("zlibLevel", out var zl) ? zl.GetInt32() : 6;
                bool verify = !doc.RootElement.TryGetProperty("verify", out var vf) || vf.GetBoolean();
                bool sidecars = !doc.RootElement.TryGetProperty("generateSidecars", out var sc) || sc.GetBoolean();
                Guid id = backend.EnqueueFfpfscConvert(pkgPath, outputPath, zlib, verify, sidecars);
                return Results.Json(new { id, queue = backend.GetTaskQueueSnapshot() }, jsonOpts);
            });

            app.MapPost("/api/tasks/enqueue/shadps4-install", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string pkgPath = doc.RootElement.GetProperty("pkgPath").GetString() ?? "";
                string? libDir = doc.RootElement.TryGetProperty("libraryDir", out var ld) ? ld.GetString() : null;
                bool replace = !doc.RootElement.TryGetProperty("replace", out var rp) || rp.GetBoolean();
                Guid id = backend.EnqueueShadps4Install(pkgPath, libDir, replace);
                return Results.Json(new { id, queue = backend.GetTaskQueueSnapshot() }, jsonOpts);
            });

            // Official Update Downloader
            app.MapGet("/api/updates/check", async (string titleId) =>
            {
                var res = await Task.Run(() => backend.CheckOfficialUpdate(titleId));
                return Results.Json(res, jsonOpts);
            });
            app.MapGet("/api/updates/status", () => Results.Json(backend.GetUpdateDownloadState(), jsonOpts));
            app.MapPost("/api/updates/cancel", () => { backend.CancelOfficialUpdateDownload(); return Results.Json(backend.GetUpdateDownloadState(), jsonOpts); });
            app.MapPost("/api/updates/download", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string titleId = doc.RootElement.GetProperty("titleId").GetString() ?? "";
                string dir = doc.RootElement.GetProperty("downloadDirectory").GetString() ?? "";
                var pieces = JsonSerializer.Deserialize<List<OfficialUpdatePieceDto>>(doc.RootElement.GetProperty("pieces").GetRawText(), jsonOpts) ?? new();
                backend.StartOfficialUpdateDownload(titleId, dir, pieces);
                return Results.Json(backend.GetUpdateDownloadState(), jsonOpts);
            });

            // PS4 Remote Package Installer
            app.MapPost("/api/rpi/action", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string action = doc.RootElement.GetProperty("action").GetString() ?? "";
                string pkgPath = doc.RootElement.TryGetProperty("pkgPath", out var pp) ? pp.GetString() ?? "" : "";
                var res = await Task.Run(() => backend.ExecutePs4RpiAction(action, pkgPath));
                return Results.Json(res, jsonOpts);
            });

            // Databases (PS5 BC & shadPS4 Compatibility)
            app.MapPost("/api/databases/ps5bc", async () =>
            {
                var (count, err) = await backend.DownloadPs5BcDatabaseAsync();
                return Results.Json(new { count, error = err }, jsonOpts);
            });
            app.MapPost("/api/databases/shadps4-compat", async () =>
            {
                var (count, err) = await backend.DownloadShadps4CompatDatabaseAsync();
                return Results.Json(new { count, error = err }, jsonOpts);
            });

            // shadPS4 Manager
            app.MapGet("/api/shadps4/overview", () => Results.Json(backend.GetShadps4Overview(), jsonOpts));
            app.MapGet("/api/shadps4/games", () => Results.Json(backend.ListInstalledShadps4Games(), jsonOpts));
            app.MapGet("/api/shadps4/releases", async (CancellationToken ct) =>
            {
                var res = await backend.FetchShadps4ReleasesAsync(ct);
                return Results.Json(res, jsonOpts);
            });
            app.MapPost("/api/shadps4/builds/install", async (HttpContext ctx, CancellationToken ct) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ct);
                var r = doc.RootElement;
                string comp = r.GetProperty("component").GetString() ?? "Core";
                string tag = r.GetProperty("tag").GetString() ?? "";
                string name = r.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? tag : tag;
                string url = r.GetProperty("assetUrl").GetString() ?? "";
                long size = r.TryGetProperty("assetSize", out var sz) ? sz.GetInt64() : 0;
                bool active = !r.TryGetProperty("setAsActive", out var sa) || sa.GetBoolean();
                var res = await backend.DownloadAndInstallShadps4BuildAsync(comp, tag, name, url, size, active, ct);
                return Results.Json(res, jsonOpts);
            });
            app.MapPost("/api/shadps4/builds/activate", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string comp = doc.RootElement.GetProperty("component").GetString() ?? "Core";
                string val = doc.RootElement.GetProperty("value").GetString() ?? "";
                return Results.Json(backend.SetActiveShadps4Build(comp, val), jsonOpts);
            });
            app.MapPost("/api/shadps4/builds/delete", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string comp = doc.RootElement.GetProperty("component").GetString() ?? "Core";
                string buildId = doc.RootElement.GetProperty("buildId").GetString() ?? "";
                return Results.Json(backend.DeleteShadps4ManagedBuild(comp, buildId), jsonOpts);
            });
            app.MapPost("/api/shadps4/launch-game", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string titleId = doc.RootElement.GetProperty("titleId").GetString() ?? "";
                var (ok, msg) = backend.LaunchShadps4Game(titleId);
                return Results.Json(new { succeeded = ok, message = msg }, jsonOpts);
            });
            app.MapPost("/api/shadps4/open-launcher", () =>
            {
                var (ok, msg) = backend.OpenShadps4QtLauncher();
                return Results.Json(new { succeeded = ok, message = msg }, jsonOpts);
            });

            // shadPS4 Saves
            app.MapGet("/api/shadps4/saves", () => Results.Json(backend.ListShadps4Saves(), jsonOpts));
            app.MapPost("/api/shadps4/saves/backup", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string titleId = doc.RootElement.GetProperty("titleId").GetString() ?? "";
                string userId = doc.RootElement.GetProperty("userId").GetString() ?? "1";
                string? note = doc.RootElement.TryGetProperty("note", out var n) ? n.GetString() : null;
                return Results.Json(backend.BackupShadps4Save(titleId, userId, note), jsonOpts);
            });
            app.MapPost("/api/shadps4/saves/restore", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string zipPath = doc.RootElement.GetProperty("zipPath").GetString() ?? "";
                backend.RestoreShadps4SaveBackup(zipPath);
                return Results.Json(new { ok = true }, jsonOpts);
            });

            // shadPS4 Patches & Cheats
            app.MapGet("/api/shadps4/patches", (string titleId, string? title, string? appVersion) =>
            {
                return Results.Json(backend.GetShadps4PatchesAndCheats(titleId, title ?? titleId, appVersion ?? "01.00"), jsonOpts);
            });
            app.MapPost("/api/shadps4/patches/save", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var changes = JsonSerializer.Deserialize<List<Shadps4PatchStateChange>>(doc.RootElement.GetProperty("changes").GetRawText(), jsonOpts) ?? new();
                backend.SaveShadps4PatchStates(changes);
                return Results.Json(new { ok = true }, jsonOpts);
            });
            app.MapPost("/api/shadps4/patches/download", async (HttpContext ctx, CancellationToken ct) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ct);
                string kind = doc.RootElement.GetProperty("kind").GetString() ?? "patches";
                string titleId = doc.RootElement.GetProperty("titleId").GetString() ?? "";
                var res = await backend.DownloadOfficialPatchesOrCheatsAsync(kind, titleId, ct);
                return Results.Json(res, jsonOpts);
            });

            // Game Feedback & Reports
            app.MapGet("/api/feedback", (string? titleId) => Results.Json(backend.GetGameFeedbackEntries(titleId), jsonOpts));
            app.MapPost("/api/feedback/save", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var entry = JsonSerializer.Deserialize<GameFeedbackEntry>(doc.RootElement.GetRawText(), jsonOpts)!;
                return Results.Json(backend.SaveGameFeedback(entry), jsonOpts);
            });
            app.MapPost("/api/feedback/delete", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string id = doc.RootElement.GetProperty("id").GetString() ?? "";
                backend.DeleteGameFeedback(id);
                return Results.Json(new { ok = true }, jsonOpts);
            });
            app.MapPost("/api/feedback/report", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var entry = JsonSerializer.Deserialize<GameFeedbackEntry>(doc.RootElement.GetRawText(), jsonOpts)!;
                return Results.Json(backend.BuildCompatibilityReport(entry), jsonOpts);
            });

            // Settings & System Actions
            app.MapPost("/api/settings", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                return Results.Json(backend.UpdateSettings(doc.RootElement), jsonOpts);
            });

            app.MapPost("/api/system/open", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string target = doc.RootElement.GetProperty("target").GetString() ?? "";
                if (target.StartsWith("http://") || target.StartsWith("https://"))
                    Helper.Tool.OpenWebLink(target);
                else
                    Helper.Tool.OpenFolder(target);
                return Results.Json(new { ok = true }, jsonOpts);
            });

            app.MapPost("/api/system/copy", async (HttpContext ctx) =>
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                string text = doc.RootElement.GetProperty("text").GetString() ?? "";
                bool ok = ShellCommands.TrySetLinuxClipboard(text);
                return Results.Json(new { ok }, jsonOpts);
            });
        }
    }
}
