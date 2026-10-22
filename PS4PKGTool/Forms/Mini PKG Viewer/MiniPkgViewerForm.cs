using GitHubUpdate;
using PS4PKGTool.Util;
using PS4PKGTool.Util.Constants;
using PS4PKGTool.Utilities.PkgInspection;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.TrophyMetadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS4PKGTool
{
    public partial class MiniPkgViewerForm : DarkUI.Forms.DarkForm
    {
        private readonly string _packagePath;

        // The on-disk path of the package. File operations (rename) change it,
        // so the lazy sessions are rebuilt against the current path afterwards.
        private string _currentPackagePath;
        private readonly PkgInspectionService _inspectionService;
        private readonly IPkgEntryLoader _entryLoader;
        private readonly ITrophyInspectionLoader _trophyLoader;
        private readonly IPkgFileListingLoader _fileListingLoader;

        // The passcode used to list package files. Defaults to the standard
        // all-zero passcode; the user can supply a custom one when the
        // default is rejected by orbis-pub-cmd.
        private string _fileListingPasscode = PkgFileListingService.DefaultPasscode;
        private PkgEntryInspectionSession _entrySession;
        private TrophyInspectionSession _trophySession;
        private PkgFileListingSession _fileListingSession;
        private PkgPathMap _pathMap = new();
        private List<PkgFileEntry> _pkgEntries = new();
        private readonly CancellationTokenSource _loadCancellation = new CancellationTokenSource();
        // Set on the UI thread when Stop Extract is clicked; read by the
        // extraction worker after orbis-pub-cmd dies. volatile so the flag
        // crosses the thread boundary reliably (mirror of Main).
        private volatile bool _extractionStopRequested;
        private CancellationTokenSource _extractionCancellation;
        private bool _extracting;
        private PkgInspectionSnapshot _snapshot;
        private bool _loadStarted;
        private bool _resourcesReleased;

        // The mini viewer intentionally creates this pane in code so its
        // hand-maintained file-browser designer layout remains stable.
        private Assets.Abstractions.IAssetSource _filePreviewContainerSource;
        private Assets.Models.AssetDetectionResult _filePreviewContainerDetection;
        private readonly List<Assets.Abstractions.IAssetSource> _filePreviewContainerChildren = new();
        private string _filePreviewContainerTempDir;
        private int _filePreviewVersion;
        private int _filePreviewBusy;
        private static readonly Assets.AssetInspectionService AssetService = Assets.GenericAssetRegistryBuilder.Build();

        public MiniPkgViewerForm()
            : this(
                string.Empty,
                new PkgInspectionService(),
                new PkgEntryInspectionService(),
                new TrophyInspectionService())
        {
            // Required by the WinForms designer. Production startup always uses
            // the package-path constructor after StartupArgumentRouter validation.
        }

        public MiniPkgViewerForm(string packagePath)
            : this(
                packagePath,
                new PkgInspectionService(),
                new PkgEntryInspectionService(),
                new TrophyInspectionService())
        {
        }

        internal MiniPkgViewerForm(string packagePath, PkgInspectionService inspectionService)
            : this(
                packagePath,
                inspectionService,
                new PkgEntryInspectionService(),
                new TrophyInspectionService())
        {
        }

        internal MiniPkgViewerForm(
            string packagePath,
            PkgInspectionService inspectionService,
            IPkgEntryLoader entryLoader)
            : this(
                packagePath,
                inspectionService,
                entryLoader,
                new TrophyInspectionService())
        {
        }

        internal MiniPkgViewerForm(
            string packagePath,
            PkgInspectionService inspectionService,
            IPkgEntryLoader entryLoader,
            ITrophyInspectionLoader trophyLoader)
            : this(
                packagePath,
                inspectionService,
                entryLoader,
                trophyLoader,
                new PkgFileListingService())
        {
        }

        internal MiniPkgViewerForm(
            string packagePath,
            PkgInspectionService inspectionService,
            IPkgEntryLoader entryLoader,
            ITrophyInspectionLoader trophyLoader,
            IPkgFileListingLoader fileListingLoader)
        {
            _packagePath = packagePath ?? throw new ArgumentNullException(nameof(packagePath));
            _currentPackagePath = _packagePath;
            _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));
            _entryLoader = entryLoader ?? throw new ArgumentNullException(nameof(entryLoader));
            _trophyLoader = trophyLoader ?? throw new ArgumentNullException(nameof(trophyLoader));
            _fileListingLoader = fileListingLoader ?? throw new ArgumentNullException(nameof(fileListingLoader));
            _entrySession = new PkgEntryInspectionSession(_currentPackagePath, _entryLoader);
            _trophySession = new TrophyInspectionSession(_currentPackagePath, _trophyLoader);
            _fileListingSession = new PkgFileListingSession(
                _currentPackagePath, _fileListingPasscode, _fileListingLoader);

            InitializeComponent();
            Text = "PS4 PKG Tool - PKG Viewer";
            toolStripStatusLabel2.Text = _currentPackagePath;

            // File browser icons from embedded resources, same set and
            // ordering as the main app (imageList1) so the tree and list
            // share the same iconography.
            try
            {
                var icons = new (string key, Bitmap bmp)[]
                {
                    ("folder",       Properties.Resources.tv_folder),
                    ("document",     Properties.Resources.tv_document),
                    ("image",        Properties.Resources.tv_image),
                    ("config",       Properties.Resources.tv_config),
                    ("binary",       Properties.Resources.tv_binary),
                    ("folder-open",  Properties.Resources.tv_folder_open),
                    ("audio",        Properties.Resources.tv_audio),
                    ("file-unknown", Properties.Resources.tv_file_unknown),
                    ("package",      Properties.Resources.tv_package),
                    ("video",        Properties.Resources.tv_video),
                    ("code",         Properties.Resources.tv_code),
                };
                foreach (var (key, bmp) in icons)
                    imageListFiles.Images.Add(key, new Bitmap(bmp));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("File browser icon load failed: " + ex.Message);
            }

        }

        private async void MiniPkgViewerForm_Shown(object sender, EventArgs e)
        {
            if (_loadStarted)
                return;

            _loadStarted = true;
            try
            {
                await LoadPackageAsync();
            }
            catch (Exception ex)
            {
                // LoadPackageAsync handles its own failures; this is the last
                // resort so a broken package can never crash the app.
                Logger.LogError("Mini viewer initial load failed", ex);
                if (!IsDisposed && !Disposing)
                {
                    AppMessageBox.Show(
                        "PKG Viewer",
                        "The package could not be opened.\n\n" + ex.Message,
                        AppMessageType.Error,
                        AppMessageButtons.OK);
                    Close();
                }
            }
        }

        private async Task LoadPackageAsync()
        {
            SetLoadingState(true, "Reading package metadata...");

            try
            {
                PkgInspectionSnapshot snapshot = await _inspectionService
                    .InspectAsync(_packagePath, _loadCancellation.Token);

                if (_resourcesReleased || IsDisposed)
                {
                    snapshot.Dispose();
                    return;
                }

                _snapshot = snapshot;
                Populate(snapshot);
                SetLoadingState(false, "Ready");

                // The file browser is the most requested view: populate it
                // right after the metadata instead of waiting for the tab.
                await LoadFilesAsync();

                // Trophy metadata needs the NP Communication ID (NPWRxxxxx_00).
                // Extract it into the same cache the main app reads so the
                // Trophy tab can decrypt the trophy data.
                await EnsureNpCommunicationIdExtractedAsync(_loadCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Closing the viewer while it is loading is expected.
            }
            catch (Exception ex)
            {
                SetLoadingState(false, "Unable to read package");
                if (!IsDisposed && !Disposing)
                {
                    AppMessageBox.Show(
                    "PKG Viewer",
                        "The supplied file could not be read as a PS4 package.\n\n" + ex.Message,
                        AppMessageType.Error,
                        AppMessageButtons.OK);
                    Close();
                }
            }
        }

        private void Populate(PkgInspectionSnapshot snapshot)
        {
            string displayTitle = ValueOrFallback(snapshot.Title, snapshot.FileName);
            Text = displayTitle + " - PKG Viewer";
            lblTitle.Text = displayTitle;
            lblSubtitle.Text = string.Join("  •  ", new[]
            {
                snapshot.TitleId,
                snapshot.PackageCategory,
                snapshot.PackageState,
                string.IsNullOrWhiteSpace(snapshot.ApplicationVersion) ? string.Empty : "v" + snapshot.ApplicationVersion,
                snapshot.PackageSize
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
            lblContentId.Text = snapshot.ContentId;
            picIcon.Image = snapshot.Icon0;

            SetOverviewValue(lblOverviewTitle, snapshot.Title);
            SetOverviewValue(lblOverviewTitleId, snapshot.TitleId);
            SetOverviewValue(lblOverviewContentId, snapshot.ContentId);
            SetOverviewValue(lblOverviewCategory, snapshot.PackageCategory);
            SetOverviewValue(lblOverviewState, snapshot.PackageState);
            SetOverviewValue(lblOverviewAppVersion, snapshot.ApplicationVersion);
            SetOverviewValue(lblOverviewPkgVersion, snapshot.PackageVersion);
            SetOverviewValue(lblOverviewFirmware, snapshot.RequiredFirmware);
            SetOverviewValue(lblOverviewSize, snapshot.PackageSize);

            dgvSfo.Rows.Clear();
            foreach (PkgSfoEntry entry in snapshot.SfoEntries)
                dgvSfo.Rows.Add(entry.Name, entry.Value);

            PopulateInspectionGrid(dgvPackageHeader, snapshot.HeaderFields);
            PopulateInspectionGrid(dgvBuildInfo, snapshot.BuildInfoFields);
            if (snapshot.HeaderFields.Count == 0)
                dgvPackageHeader.Rows.Add("Header information", "Not available");
            if (snapshot.BuildInfoFields.Count == 0)
                dgvBuildInfo.Rows.Add("PUBTOOLINFO", "Not available");

            picPic0.Image = snapshot.Pic0;
            picPic1.Image = snapshot.Pic1;
            picPic0.Visible = snapshot.Pic0 != null;
            picPic1.Visible = snapshot.Pic1 != null;
            lblNoPic0.Visible = snapshot.Pic0 == null;
            lblNoPic1.Visible = snapshot.Pic1 == null;
        }

        private static string ValueOrFallback(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static void SetOverviewValue(Control label, string value) =>
            label.Text = string.IsNullOrWhiteSpace(value) ? "Not available" : value;

        private static void PopulateInspectionGrid(
            DarkUI.Controls.DarkDataGridView grid,
            System.Collections.Generic.IEnumerable<PkgInspectionField> fields)
        {
            grid.Rows.Clear();
            foreach (PkgInspectionField field in fields)
                grid.Rows.Add(field.Name, field.Value);
        }

        private async void packageTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (packageTabs.SelectedTab != tabPackageEntries || _resourcesReleased)
                return;

            try
            {
                await LoadEntriesAsync();
            }
            catch (Exception ex)
            {
                // LoadEntriesAsync handles cancellation; anything else must
                // never take the app down with the viewer.
                Logger.LogError("Mini viewer entries load failed", ex);
                labelDisplayTotalPKG.Text = "Ready (Entries unavailable)";
            }
        }

        private async Task LoadEntriesAsync()
        {
            labelDisplayTotalPKG.Text = "Reading package entries...";
            try
            {
                PkgEntryLoadResult result = await _entrySession.LoadAsync(_loadCancellation.Token);
                if (_resourcesReleased || IsDisposed)
                    return;

                dgvEntries.Rows.Clear();
                if (!result.Succeeded)
                {
                    labelDisplayTotalPKG.Text = "Ready (Entries unavailable)";
                    return;
                }

                foreach (PkgEntryInfo entry in result.Entries)
                {
                    dgvEntries.Rows.Add(
                        entry.Name, entry.Offset, entry.Size,
                        entry.Flags1, entry.Flags2, entry.Encrypted);
                }
            }
            catch (OperationCanceledException)
            {
                // Viewer lifetime cancellation is expected during close.
            }
        }

        private async void tabsViewer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_resourcesReleased)
                return;

            try
            {
                if (tabsViewer.SelectedTab == tabTrophy)
                    await LoadTrophiesAsync();
            }
            catch (Exception ex)
            {
                // Tab loads handle cancellation; anything else must never
                // take the app down with the viewer.
                Logger.LogError("Mini viewer tab load failed", ex);
                labelDisplayTotalPKG.Text = "Ready";
            }
        }

        /// <summary>
        /// Ensures the package's NP Communication ID sits in the shared cache
        /// the Trophy tab reads (same file the main app writes). Extraction
        /// uses the main app's method: NpbindExtractor reads Sc0/npbind.dat
        /// in-process and scans it for the NPWRxxxxx_00 token.
        /// </summary>
        private async Task EnsureNpCommunicationIdExtractedAsync(CancellationToken cancellationToken)
        {
            string contentId = _snapshot?.ContentId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            var cache = new NpCommunicationIdCache(Path.Combine(
                Helper.AppDataDirectory, "TrophyMetadata", "np-communication-ids.json"));
            if (cache.TryGet(contentId, out _))
                return;

            try
            {
                NpbindExtractionResult extraction = await new NpbindExtractor()
                    .ExtractAsync(_currentPackagePath, cancellationToken);
                if (extraction.Succeeded)
                {
                    cache.Set(contentId, extraction.NpCommunicationId);
                    Logger.LogInformation(
                        "NP Communication ID extracted for " + contentId + ": " + extraction.NpCommunicationId);
                }
                else
                {
                    Logger.LogWarning(
                        "NP Communication ID auto extraction failed: " + extraction.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                // Closing the viewer while extraction runs is expected.
            }
            catch (Exception ex)
            {
                Logger.LogWarning("NP Communication ID auto extraction failed: " + ex.Message);
            }
        }

        private async Task LoadTrophiesAsync()
        {
            lblTrophyState.Text = "Loading trophy information...";
            labelDisplayTotalPKG.Text = "Loading trophy information...";
            try
            {
                string contentId = _snapshot?.ContentId ?? string.Empty;
                TrophyInspectionResult result = await _trophySession.LoadAsync(
                    contentId, _loadCancellation.Token);
                if (_resourcesReleased || IsDisposed)
                    return;

                dgvTrophies.Rows.Clear();
                switch (result.Status)
                {
                    case TrophyInspectionStatus.Loaded:
                        foreach (TrophyInspectionItem trophy in result.Trophies)
                        {
                            dgvTrophies.Rows.Add(
                                trophy.Icon,
                                trophy.Id.ToString("000"),
                                trophy.Name,
                                trophy.Description,
                                trophy.Grade,
                                trophy.Hidden ? "Yes" : "No");
                        }
                        lblTrophyState.Text = BuildTrophySummary(result);
                        labelDisplayTotalPKG.Text = "Ready";
                        break;

                    case TrophyInspectionStatus.NoTrophyResource:
                        lblTrophyState.Text = "No trophy data was found in this package.";
                        labelDisplayTotalPKG.Text = "Ready (No trophy data)";
                        break;

                    case TrophyInspectionStatus.Inaccessible:
                        lblTrophyState.Text = result.Message;
                        labelDisplayTotalPKG.Text = "Ready (Trophy data inaccessible)";
                        break;

                    default:
                        lblTrophyState.Text = string.IsNullOrWhiteSpace(result.Message)
                            ? "Trophy information could not be read."
                            : result.Message;
                        labelDisplayTotalPKG.Text = "Ready (Trophy unavailable)";
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Viewer lifetime cancellation is expected during close.
            }
        }

        private static string BuildTrophySummary(TrophyInspectionResult result)
        {
            string counts = string.Join("  •  ", result.Trophies
                .GroupBy(trophy => trophy.Grade)
                .OrderBy(group => TrophyGradeOrder(group.Key))
                .Select(group => $"{group.Count():N0} {group.Key}"));
            string summary = $"{result.Trophies.Count:N0} trophies";
            if (!string.IsNullOrWhiteSpace(counts))
                summary += "  •  " + counts;
            if (result.IconDecodeFailures > 0)
                summary += $"  •  {result.IconDecodeFailures:N0} icon(s) unavailable";
            return summary;
        }

        private static int TrophyGradeOrder(string grade) => grade switch
        {
            "Bronze" => 0,
            "Silver" => 1,
            "Gold" => 2,
            "Platinum" => 3,
            _ => 4
        };

        private async Task LoadFilesAsync()
        {
            labelDisplayTotalPKG.Text = "Loading package file list...";
            try
            {
                PkgFileListingResult result = await _fileListingSession.LoadAsync(
                    _loadCancellation.Token);
                if (_resourcesReleased || IsDisposed)
                    return;

                _pathMap = result.PathMap;
                _pkgEntries = result.Entries.ToList();
                tvFiles.BeginUpdate();
                try
                {
                    tvFiles.Nodes.Clear();
                    // One game-folder root (like a dump / shadPS4 library folder).
                    string rootName = !string.IsNullOrWhiteSpace(_snapshot?.TitleId)
                        ? _snapshot!.TitleId
                        : Path.GetFileNameWithoutExtension(_currentPackagePath);
                    var wrapper = new TreeNode(rootName)
                    {
                        Tag = new PkgFileNode { Name = rootName, FullPath = string.Empty, IsDirectory = true },
                        ImageIndex = 0,
                        SelectedImageIndex = 0
                    };
                    tvFiles.Nodes.Add(wrapper);
                    foreach (PkgFileNode root in result.Roots)
                        AddViewerFileNode(wrapper.Nodes, root);
                    wrapper.Expand();
                }
                finally
                {
                    tvFiles.EndUpdate();
                }

                lvFiles.BeginUpdate();
                try { lvFiles.Items.Clear(); }
                finally { lvFiles.EndUpdate(); }

                if (!result.Succeeded)
                {
                    if (!result.RestoreFailed && IsPasscodeFailure(result.ErrorMessage))
                    {
                        labelDisplayTotalPKG.Text = "Ready (Passcode required)";
                        if (PromptForPasscode(out string passcode))
                        {
                            try { _fileListingSession.Dispose(); } catch { /* best-effort: session already disposed */ }
                            _fileListingPasscode = passcode;
                            _fileListingSession = new PkgFileListingSession(
                                _currentPackagePath, _fileListingPasscode, _fileListingLoader);
                            await LoadFilesAsync();
                            return;
                        }
                        return;
                    }

                    labelDisplayTotalPKG.Text = result.RestoreFailed
                        ? "Package restoration requires attention"
                        : "Ready (Files unavailable)";
                    return;
                }

               
                labelDisplayTotalPKG.Text = "Ready";
                if (tvFiles.Nodes.Count > 0)
                    tvFiles.SelectedNode = tvFiles.Nodes.Cast<TreeNode>()
                        .FirstOrDefault(n => n.Nodes.Count > 0) ?? tvFiles.Nodes[0];
            }
            catch (OperationCanceledException)
            {
                // Viewer lifetime cancellation is expected during close.
            }
        }

        private static void AddViewerFileNode(
            TreeNodeCollection collection, PkgFileNode model)
        {
            var node = new TreeNode(model.Name) { Tag = model };
            int icon = model.IsDirectory ? 0 : IconFor(model.Name);
            node.ImageIndex = icon;
            node.SelectedImageIndex = icon;
            collection.Add(node);
            foreach (PkgFileNode child in model.Children)
                AddViewerFileNode(node.Nodes, child);
        }

        /// <summary>
        /// Same extension-to-icon mapping as the main app (Main.IconFor):
        /// image list indexes into imageListFiles.
        /// </summary>
        private static int IconFor(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return 4; // no extension -> binary
            return ext switch
            {
                ".png" or ".jpg" or ".dds" => 2,                          // image
                ".txt" => 1,                                              // document
                ".xml" or ".json" or ".sfo" or ".ini" => 3,              // config
                ".at9" or ".ogg" or ".mp3" or ".wav" => 6,              // audio
                ".mp4" or ".avi" => 9,                                    // video
                ".pkg" => 8,                                              // package
                _ => 4,                                                   // binary (default)
            };
        }

        private void tvFiles_AfterSelect(object sender, TreeViewEventArgs e) => RefreshFileList();

        /// <summary>
        /// Rebuilds the list view: the selected folder's contents normally,
        /// or every package match across the whole tree when the filter box
        /// has text (same behavior as the main app's file browser).
        /// </summary>
        private void RefreshFileList()
        {
            string query = tbFilterFiles.SearchText.Trim();
            bool filtering = !string.IsNullOrEmpty(query);

            lvFiles.BeginUpdate();
            try
            {
                lvFiles.Items.Clear();
                if (!filtering)
                {
                    if (tvFiles.SelectedNode != null)
                        PopulateFileList(tvFiles.SelectedNode);
                    return;
                }

                foreach (TreeNode root in tvFiles.Nodes)
                    CollectFileMatches(root, query);
            }
            finally
            {
                lvFiles.EndUpdate();
            }
        }

        private void PopulateFileList(TreeNode selected)
        {
            if (selected?.Tag is not PkgFileNode)
                return;

            // "..." parent navigation item (same as the main app's file
            // browser); clicking it selects the parent folder.
            if (selected.Parent != null)
            {
                var upItem = new ListViewItem("...") { ImageIndex = 5 };
                upItem.Tag = selected.Parent;
                upItem.SubItems.Add(""); upItem.SubItems.Add(""); upItem.SubItems.Add("");
                lvFiles.Items.Add(upItem);
            }

            foreach (TreeNode child in selected.Nodes)
            {
                if (child.Tag is not PkgFileNode model)
                    continue;

                bool isDirectory = model.IsDirectory;
                string type = isDirectory
                    ? "Directory"
                    : string.IsNullOrWhiteSpace(Path.GetExtension(model.Name))
                        ? "File"
                        : Path.GetExtension(model.Name).TrimStart('.');

                var item = new ListViewItem(model.Name) { Tag = child };
                item.SubItems.Add(type);
                item.SubItems.Add(isDirectory ? "" : model.FullPath);
                item.SubItems.Add(isDirectory ? "" : Helper.RoundBytes(model.Size));
                item.ImageIndex = isDirectory ? 0 : IconFor(model.Name);
                lvFiles.Items.Add(item);
            }
        }

        private void tbFilterFiles_SearchTextChanged(object sender, EventArgs e) => RefreshFileList();

        /// <summary>
        /// Collects every tree entry matching the query (name, type or full
        /// path) and expands the tree so the matches are visible. Mirrors the
        /// main app's CollectMatches.
        /// </summary>
        private void CollectFileMatches(TreeNode node, string query)
        {
            foreach (TreeNode child in node.Nodes)
            {
                bool isDirectory = child.Nodes.Count > 0;
                string type = isDirectory
                    ? "Directory"
                    : string.IsNullOrWhiteSpace(Path.GetExtension(child.Text))
                        ? "File"
                        : Path.GetExtension(child.Text).TrimStart('.');

                bool matches = child.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || type.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || child.FullPath.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (matches && child.Tag is PkgFileNode model)
                {
                    var item = new ListViewItem(child.Text) { Tag = child };
                    item.SubItems.Add(type);
                    item.SubItems.Add(isDirectory ? "" : model.FullPath);
                    item.SubItems.Add(isDirectory ? "" : Helper.RoundBytes(model.Size));
                    item.ImageIndex = isDirectory ? 0 : IconFor(child.Text);
                    lvFiles.Items.Add(item);
                    ExpandFileAncestors(child);
                }

                if (isDirectory)
                    CollectFileMatches(child, query);
            }
        }

        private static void ExpandFileAncestors(TreeNode node)
        {
            TreeNode parent = node.Parent;
            while (parent != null)
            {
                parent.Expand();
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Double-click navigation: "..." moves to the parent folder, a
        /// directory entry opens it (same flow as the main app's file browser).
        /// </summary>
        private void lvFiles_ItemActivate(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0)
                return;
            if (lvFiles.SelectedItems[0].Tag is not TreeNode node)
                return;

            if (node.Tag is not PkgFileNode model)
                return;

            if (model.IsDirectory || lvFiles.SelectedItems[0].Text == "...")
            {
                tvFiles.SelectedNode = node;
                node.EnsureVisible();
                return;
            }

            PreviewPackageFileAsync(model);
        }

        private async void PreviewPackageFileAsync(PkgFileNode file)
        {
            if (_resourcesReleased || _extracting || !File.Exists(_currentPackagePath))
                return;
            if (Interlocked.CompareExchange(ref _filePreviewBusy, 1, 0) != 0)
            {
                _filePreviewInfo.Text = "Preview already in progress...";
                return;
            }

            int version = Interlocked.Increment(ref _filePreviewVersion);
            CleanupFilePreviewContainer();
            ClearFilePreviewImage();
            _filePreviewImage.Visible = false;
            _filePreviewText.Visible = false;
            _filePreviewAssetList.Visible = false;
            _filePreviewBackButton.Visible = false;
            _filePreviewInfo.Text = "Previewing " + file.Name + "...";

            try
            {
                PreviewResult result = await Task.Run(() => BuildFilePreview(file));
                if (version != _filePreviewVersion || IsDisposed || Disposing)
                {
                    result.Cleanup();
                    return;
                }

                _filePreviewInfo.Text = result.Info;
                if (result.ContainerSource != null && result.ContainerDetection != null)
                {
                    _filePreviewContainerSource = result.ContainerSource;
                    _filePreviewContainerDetection = result.ContainerDetection;
                    _filePreviewContainerTempDir = result.ContainerTempDir;
                    _filePreviewContainerChildren.AddRange(result.Children);
                    PopulateFilePreviewAssetList();
                }
                else if (result.Texture != null)
                {
                    _filePreviewImage.Image?.Dispose();
                    _filePreviewImage.Image = TextureToBitmap(result.Texture);
                    _filePreviewImage.Visible = true;
                }
                else
                {
                    _filePreviewText.Text = result.Text;
                    _filePreviewText.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer preview failed: " + ex.Message);
                if (version == _filePreviewVersion && !IsDisposed && !Disposing)
                {
                    _filePreviewInfo.Text = file.Name + " - preview unavailable: " + ex.Message;
                    _filePreviewText.Text = string.Empty;
                    _filePreviewText.Visible = true;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _filePreviewBusy, 0);
            }
        }

        private PreviewResult BuildFilePreview(PkgFileNode file)
        {
            string previewDir = Helper.CreateOrbisTempDir("v");
            try
            {
                string extracted = Path.Combine(previewDir, Path.GetFileName(file.FullPath));
                string realPath = _pathMap.TryResolveFile(file.FullPath, out string resolved) ? resolved : file.FullPath;
                using (var reader = new OrbisPkgTool.PkgReader(_currentPackagePath, _fileListingPasscode))
                {
                    reader.ExtractFileTo(realPath, extracted);
                    ExtractUnityStreamCompanions(reader, file.FullPath, previewDir);
                }

                var source = new Assets.IO.FileAssetSource(extracted, "PKG entry",
                    rel => ResolveUnityStreamCompanion(previewDir, rel));
                var detection = AssetService.Detect(source);
                if (detection == null)
                    return new PreviewResult(file.Name + " (" + Helper.RoundBytes(file.Size) + ") - hex preview", BuildHexDump(extracted, 1 << 20), null);

                var descriptor = AssetService.InspectAsync(source, detection).GetAwaiter().GetResult();
                if (AssetService.IsContainer(detection))
                {
                    var children = AssetService.GetChildrenAsync(source, detection, 0).GetAwaiter().GetResult();
                    string containerInfo = file.Name + ": " + children.Count + " entries (double-click an entry to preview)";
                    var result = new PreviewResult(containerInfo, string.Empty, null, source, detection, children, previewDir);
                    previewDir = null;
                    return result;
                }
                var preview = descriptor.Capabilities.HasFlag(Assets.Abstractions.AssetCapabilities.Preview)
                    ? AssetService.TryPreviewAsync(source, detection).GetAwaiter().GetResult()
                    : null;
                string info = BuildPreviewInfo(file.Name, descriptor);
                if (preview?.Texture != null)
                    return new PreviewResult(preview.Info ?? info, string.Empty, preview.Texture);
                return new PreviewResult(preview?.Info ?? info, preview?.Text ?? BuildHexDump(extracted, 1 << 20), null);
            }
            finally
            {
                if (previewDir != null)
                    try { Directory.Delete(previewDir, true); } catch { }
            }
        }

        private void PopulateFilePreviewAssetList()
        {
            _filePreviewAssetList.BeginUpdate();
            try
            {
                _filePreviewAssetList.Items.Clear();
                const int maxEntries = 2000;
                for (int i = 0; i < _filePreviewContainerChildren.Count && i < maxEntries; i++)
                {
                    var child = _filePreviewContainerChildren[i];
                    string type = child is Assets.Containers.UnityObjectAssetSource unity
                        ? "class " + unity.Object.ClassId
                        : child.SourceDescription ?? "file";
                    _filePreviewAssetList.Items.Add(new ListViewItem(new[]
                        { child.Name, type, Helper.RoundBytes(child.Length) }));
                }
                if (_filePreviewContainerChildren.Count > maxEntries)
                    _filePreviewAssetList.Items.Add(new ListViewItem(new[] { "...", "", "" }));
            }
            finally { _filePreviewAssetList.EndUpdate(); }

            _filePreviewImage.Visible = false;
            _filePreviewText.Visible = false;
            _filePreviewAssetList.Visible = true;
            _filePreviewBackButton.Visible = false;
        }

        private void filePreviewAssetList_ItemActivate(object sender, EventArgs e)
        {
            if (_filePreviewAssetList.SelectedItems.Count == 0)
                return;
            int index = _filePreviewAssetList.SelectedItems[0].Index;
            if (index < 0 || index >= _filePreviewContainerChildren.Count)
                return;
            PreviewPackageAssetAsync(_filePreviewContainerChildren[index]);
        }

        private async void PreviewPackageAssetAsync(Assets.Abstractions.IAssetSource child)
        {
            if (Interlocked.CompareExchange(ref _filePreviewBusy, 1, 0) != 0)
            {
                _filePreviewInfo.Text = "Preview already in progress...";
                return;
            }

            int version = Interlocked.Increment(ref _filePreviewVersion);
            ClearFilePreviewImage();
            _filePreviewAssetList.Visible = false;
            _filePreviewImage.Visible = false;
            _filePreviewText.Visible = false;
            _filePreviewBackButton.Visible = false;
            _filePreviewInfo.Text = "Previewing " + child.Name + "...";
            try
            {
                PreviewResult result = await Task.Run(() => BuildPackageAssetPreview(child));
                if (version != _filePreviewVersion || IsDisposed || Disposing)
                    return;
                _filePreviewInfo.Text = result.Info;
                if (result.Texture != null)
                {
                    _filePreviewImage.Image?.Dispose();
                    _filePreviewImage.Image = TextureToBitmap(result.Texture);
                    _filePreviewImage.Visible = true;
                }
                else
                {
                    _filePreviewText.Text = result.Text;
                    _filePreviewText.Visible = true;
                }
                _filePreviewBackButton.Visible = true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer asset preview failed: " + ex.Message);
                if (version == _filePreviewVersion && !IsDisposed && !Disposing)
                {
                    _filePreviewInfo.Text = child.Name + " - preview unavailable: " + ex.Message;
                    _filePreviewText.Text = string.Empty;
                    _filePreviewText.Visible = true;
                    _filePreviewBackButton.Visible = true;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _filePreviewBusy, 0);
            }
        }

        private PreviewResult BuildPackageAssetPreview(Assets.Abstractions.IAssetSource child)
        {
            if (child is Assets.Containers.UnityObjectAssetSource unityChild)
            {
                var handler = new Assets.Handlers.UnitySerializedFileHandler();
                var preview = handler.PreviewAsync(unityChild, _filePreviewContainerDetection).GetAwaiter().GetResult();
                return new PreviewResult(preview?.Info ?? child.Name, preview?.Text ?? string.Empty, preview?.Texture);
            }

            string extension = Path.GetExtension(child.Name);
            string temp = Path.Combine(_filePreviewContainerTempDir, "p4t_child_" + Guid.NewGuid().ToString("N") + extension);
            using (var input = child.OpenRead())
            using (var output = File.Create(temp))
                input.CopyTo(output);
            try
            {
                var source = new Assets.IO.FileAssetSource(temp, "container member");
                var detection = AssetService.Detect(source);
                if (detection == null)
                    return new PreviewResult(child.Name + " - hex preview", BuildHexDump(temp, 1 << 20), null);
                var descriptor = AssetService.InspectAsync(source, detection).GetAwaiter().GetResult();
                var preview = descriptor.Capabilities.HasFlag(Assets.Abstractions.AssetCapabilities.Preview)
                    ? AssetService.TryPreviewAsync(source, detection).GetAwaiter().GetResult()
                    : null;
                return new PreviewResult(preview?.Info ?? BuildPreviewInfo(child.Name, descriptor),
                    preview?.Text ?? BuildHexDump(temp, 1 << 20), preview?.Texture);
            }
            finally { try { File.Delete(temp); } catch { } }
        }

        private void filePreviewBackButton_Click(object sender, EventArgs e)
        {
            ClearFilePreviewImage();
            PopulateFilePreviewAssetList();
        }

        private void ClearFilePreviewImage()
        {
            if (_filePreviewImage?.Image == null) return;
            _filePreviewImage.Image.Dispose();
            _filePreviewImage.Image = null;
        }

        private void CleanupFilePreviewContainer()
        {
            _filePreviewContainerSource = null;
            _filePreviewContainerDetection = null;
            _filePreviewContainerChildren.Clear();
            if (_filePreviewContainerTempDir != null)
            {
                try { Directory.Delete(_filePreviewContainerTempDir, true); } catch { }
                _filePreviewContainerTempDir = null;
            }
        }

        private static void ExtractUnityStreamCompanions(OrbisPkgTool.PkgReader reader, string entryPath, string previewDir)
        {
            string folder = (Path.GetDirectoryName(entryPath) ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            foreach (var candidate in reader.ListFiles())
            {
                if (candidate.IsDirectory || !candidate.Path.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
                    continue;
                string candidateLogical = PS4PKGTool.Utilities.PkgInspection.DumpLayout.MapToLogical(candidate.Path);
                string candidateFolder = (Path.GetDirectoryName(candidateLogical) ?? string.Empty).Replace('\\', '/').TrimEnd('/');
                if (!string.Equals(folder, candidateFolder, StringComparison.OrdinalIgnoreCase))
                    continue;
                try { reader.ExtractFileTo(candidate.Path, Path.Combine(previewDir, Path.GetFileName(candidate.Path))); }
                catch { }
            }
        }

        private static Assets.Abstractions.IAssetSource ResolveUnityStreamCompanion(string previewDir, string relativePath)
        {
            string requested = Path.Combine(previewDir, Path.GetFileName(relativePath));
            if (File.Exists(requested))
                return new Assets.IO.FileAssetSource(requested, "Unity .resS stream");
            string[] candidates = Directory.GetFiles(previewDir, "*.resS");
            return candidates.Length == 1
                ? new Assets.IO.FileAssetSource(candidates[0], "Unity .resS stream")
                : null;
        }

        private static string BuildPreviewInfo(string name, Assets.Models.AssetDescriptor descriptor)
            => name + " (" + Helper.RoundBytes(descriptor.Size) + ") - " + descriptor.Format.ToUpperInvariant();

        private static string BuildHexDump(string path, int maxBytes)
        {
            byte[] bytes;
            using (var stream = File.OpenRead(path))
            {
                bytes = new byte[Math.Min((int)Math.Min(stream.Length, maxBytes), maxBytes)];
                stream.ReadExactly(bytes);
            }
            var text = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += 16)
            {
                text.Append(i.ToString("X8")).Append("  ");
                for (int j = 0; j < 16; j++)
                    text.Append(i + j < bytes.Length ? bytes[i + j].ToString("X2") + " " : "   ");
                text.Append(' ');
                for (int j = 0; j < 16 && i + j < bytes.Length; j++)
                    text.Append(bytes[i + j] is >= 32 and < 127 ? (char)bytes[i + j] : '.');
                text.AppendLine();
            }
            return bytes.Length == 0 ? "(empty file)" : text.ToString();
        }

        private static Bitmap TextureToBitmap(Assets.Models.TextureData texture)
        {
            var bitmap = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, texture.Width, texture.Height);
            var bits = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                var bgra = new byte[texture.Rgba8.Length];
                for (int i = 0; i + 3 < texture.Rgba8.Length; i += 4)
                {
                    bgra[i] = texture.Rgba8[i + 2];
                    bgra[i + 1] = texture.Rgba8[i + 1];
                    bgra[i + 2] = texture.Rgba8[i];
                    bgra[i + 3] = texture.Rgba8[i + 3];
                }
                System.Runtime.InteropServices.Marshal.Copy(bgra, 0, bits.Scan0, bgra.Length);
            }
            finally { bitmap.UnlockBits(bits); }
            return bitmap;
        }

        private sealed record PreviewResult(
            string Info,
            string Text,
            Assets.Models.TextureData? Texture,
            Assets.Abstractions.IAssetSource ContainerSource = null,
            Assets.Models.AssetDetectionResult ContainerDetection = null,
            IReadOnlyList<Assets.Abstractions.IAssetSource> Children = null,
            string ContainerTempDir = null)
        {
            public void Cleanup()
            {
                if (ContainerTempDir != null)
                    try { Directory.Delete(ContainerTempDir, true); } catch { }
            }
        }

        /// <summary>
        /// Right-click on a file entry opens the extraction/copy menu.
        /// The "..." navigation item has no context menu (same as Main).
        /// </summary>
        private void lvFiles_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ListViewItem clickedItem = lvFiles.GetItemAt(e.X, e.Y);
            if (clickedItem != null && clickedItem.Text != "...")
                ctxFileList.Show(lvFiles, e.Location);
        }

        private void CtxExtractItem_Click(object sender, EventArgs e) =>
            ExtractSelectedItemsAsync(preserveStructure: false);

        private void CtxExtractFolderItem_Click(object sender, EventArgs e) =>
            ExtractSelectedItemsAsync(preserveStructure: true);

        private void CtxCopyPathItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0)
                return;
            if (lvFiles.SelectedItems[0].Tag is not TreeNode node || node.Tag is not PkgFileNode model)
                return;

            Clipboard.SetText(model.FullPath);
        }

        private void CtxCopyNameItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0)
                return;

            Clipboard.SetText(lvFiles.SelectedItems[0].Text);
        }

        /// <summary>
        /// Extracts the selected file/folder entry from the package, with or
        /// without its folder structure (mirror of Main's extract-selected).
        /// </summary>
        private async void ExtractSelectedItemsAsync(bool preserveStructure)
        {
            if (_extracting)
            {
                ShowInformation("Extraction already in progress.", false);
                return;
            }
            if (lvFiles.SelectedItems.Count == 0)
                return;
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }
            if (lvFiles.SelectedItems[0].Tag is not TreeNode node || node.Tag is not PkgFileNode model)
                return;
            if (!ShowFolderBrowserDialog(out FolderBrowserDialog fbd))
                return;

            string target = model.IsDirectory ? model.FullPath + "/" : model.FullPath;
            string sourcePath = _currentPackagePath;
            string extractLocation = fbd.SelectedPath;

            BeginExtractionUi();
            labelDisplayTotalPKG.Text = "Extracting selected data...";

            bool succeeded;
            string message;
            try
            {
                (succeeded, message) = await Task.Run(() =>
                    ExtractSelectedCore(sourcePath, target, extractLocation, preserveStructure));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer selected extract failed: " + ex.Message);
                succeeded = false;
                message = "Extraction failed: " + ex.Message;
            }
            finally
            {
                EndExtractionUi();
            }

            if (_extractionStopRequested)
            {
                ShowInformation("Extraction cancelled.", true);
                return;
            }

            if (succeeded)
                ShowInformation("Selected data extracted.", true);
            else
                ShowError(message, true);
        }

        /// <summary>
        /// Single-entry extraction: OrbisPkgTool.PkgReader in-process (mirror
        /// of Main's ExtractSelectedPKGData per-entry flow). The PKG is opened
        /// read-only and the entry is decrypted straight into the chosen
        /// folder - no spawn, no ASCII temp staging, no move step.
        /// </summary>
        private (bool Succeeded, string Message) ExtractSelectedCore(
            string sourcePath, string targetPath, string extractLocation, bool preserveStructure)
        {
            try
            {
                var (files, dirs) = PkgSelection.Resolve(_pkgEntries, new[] { targetPath }, preserveStructure);
                foreach (string dirRel in dirs)
                    Directory.CreateDirectory(PkgSelection.SafeDestination(extractLocation, dirRel));

                using var reader = new OrbisPkgTool.PkgReader(sourcePath, _fileListingPasscode);
                int count = 0;
                foreach (var target in files)
                {
                    if (_extractionStopRequested)
                        return (false, "Cancelled");

                    string outPath = PkgSelection.SafeDestination(extractLocation, target.DestRel);
                    string? outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir))
                        Directory.CreateDirectory(outDir);

                    // Exact destination (no Image0/Sc0 prefix); logical path already
                    // encodes the flattened structure.
                    reader.ExtractFileTo(target.Real, outPath);
                    count++;
                }

                Logger.LogInformation($"Mini viewer extracted {count} file(s): {targetPath}");
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, "Extraction failed: " + ex.Message);
            }
        }

        private void SetLoadingState(bool loading, string message)
        {
            // The designer defines the bar's at-rest look (always visible,
            // static). Animating only while work runs needs a runtime flip,
            // same as the main app (Main.cs toggles Style at runtime too).
            if (loading)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripProgressBar1.MarqueeAnimationSpeed = 30;
            }
            else
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
                toolStripProgressBar1.Value = 0;
            }

            labelDisplayTotalPKG.Text = message;
            tabsViewer.Enabled = !loading;
        }

        /// <summary>
        /// Stop Extract flags the cancellation so the extraction worker
        /// reports the cancellation once the current entry finishes.
        /// </summary>
        private void BtnStopExtract_Click(object sender, EventArgs e)
        {
            if (!_extracting)
                return;

            _extractionStopRequested = true;
            _extractionCancellation?.Cancel();
            btnStopExtract.Enabled = false;
            labelDisplayTotalPKG.Text = "Stopping extraction...";
        }

        /// <summary>
        /// During extraction only the status strip (progress bar, state label,
        /// Stop Extract) stays enabled - everything else is disabled so no
        /// other operation can run while the package is temp-renamed (mirror
        /// of Main's SetExtractionUiEnabled).
        /// </summary>
        private void BeginExtractionUi()
        {
            _extracting = true;
            _extractionStopRequested = false;
            _extractionCancellation?.Dispose();
            _extractionCancellation = new CancellationTokenSource();
            btnStopExtract.Visible = true;
            btnStopExtract.Enabled = true;
            toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = 1;
            toolStripProgressBar1.Value = 0;
            SetExtractionUiEnabled(false);
        }

        private void EndExtractionUi()
        {
            toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            toolStripProgressBar1.Value = 0;
            btnStopExtract.Visible = false;
            SetExtractionUiEnabled(true);
            _extracting = false;
            labelDisplayTotalPKG.Text = "Ready";
            _extractionCancellation?.Dispose();
            _extractionCancellation = null;
        }

        private void SetExtractionUiEnabled(bool enabled)
        {
            foreach (Control c in Controls)
            {
                if (c is StatusStrip) continue; // keep the progress bar + Stop Extract live
                if (c is TabControl tabs)
                {
                    foreach (TabPage page in tabs.TabPages)
                        page.Enabled = enabled;
                    continue;
                }
                c.Enabled = enabled;
            }
        }

        private void MiniPkgViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_extracting)
            {
                e.Cancel = true;
                ShowInformation("Extraction in progress. Stop it before closing.", false);
                return;
            }
            ReleaseViewerResources();
        }

        private void ReleaseViewerResources()
        {
            if (_resourcesReleased)
                return;

            _resourcesReleased = true;
            _entrySession.Dispose();
            dgvTrophies.Rows.Clear();
            _trophySession.Dispose();
            _fileListingSession.Dispose();
            CleanupFilePreviewContainer();
            ClearFilePreviewImage();
            _loadCancellation.Cancel();
            picIcon.Image = null;
            picPic0.Image = null;
            picPic1.Image = null;
            _snapshot?.Dispose();
            _snapshot = null;
            _loadCancellation.Dispose();
        }

        #region Exit / Help (toolbar)

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private OfficialUpdateForm _officialUpdateForm;

        /// <summary>
        /// Opens the official update downloader for this package (mirror of
        /// Main's OpenOfficialUpdateForm). Only Game and Patch PKGs have
        /// official updates.
        /// </summary>
        private void DownloadUpdateItem_Click(object sender, EventArgs e)
        {
            try
            {
                string titleId = _snapshot?.TitleId ?? string.Empty;
                string category = _snapshot?.PackageCategory ?? string.Empty;
                if (string.IsNullOrWhiteSpace(titleId))
                {
                    ShowError("Could not read PKG title ID.", false);
                    return;
                }
                if (category != PKGCategory.GAME && category != PKGCategory.PATCH)
                {
                    ShowInformation("Official updates are only available for Game and Patch PKGs.", false);
                    return;
                }

                if (_officialUpdateForm == null || _officialUpdateForm.IsDisposed)
                    _officialUpdateForm = new OfficialUpdateForm();

                _officialUpdateForm.SetLogCallback(Logger.LogInformation);
                _officialUpdateForm.LoadUpdate(
                    titleId, category, appSettings_.OfficialUpdateDownloadDirectory);
                _officialUpdateForm.Show();
                _officialUpdateForm.BringToFront();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error opening official update form: " + ex.Message);
                ShowError("Error opening official update form: " + ex.Message, true);
            }
        }

        private void HelpAboutItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutForm(GetApplicationVersion()))
                about.ShowDialog(this);
        }

        private void HelpCoffeeItem_Click(object sender, EventArgs e) =>
            Helper.Tool.OpenWebLink("https://ko-fi.com/pearlxcore");

        private async void HelpUpdateItem_Click(object sender, EventArgs e)
        {
            if (!await Task.Run(() => Helper.Tool.CheckForInternetConnection()))
            {
                ShowWarning("No internet connection detected. Cannot check for updates.", false);
                return;
            }

            try
            {
                Logger.LogInformation("Checking for latest PS4 PKG Tool..");
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var checker = new UpdateChecker("pearlxcore", "PS4-PKG-Tool", "v" + GetApplicationVersion());

                UpdateType update = await checker.CheckUpdate();
                if (update == UpdateType.None)
                {
                    ShowInformation("The program is up to date.", true);
                }
                else
                {
                    var result = new UpdateNotifyDialog(checker).ShowDialog();
                    if (result == DialogResult.Yes)
                        Helper.Tool.OpenWebLink("https://github.com/pearlxcore/PS4-PKG-Tool/releases");
                }
            }
            catch (Exception ex)
            {
                // GitHubUpdate throws on API errors, rate limits (60 req/hr
                // unauthenticated), or malformed release tags - never crash.
                Logger.LogError("Update check failed: " + ex.Message);
                ShowWarning("Could not check for updates. Please try again later.\n\n" + ex.Message, false);
            }
        }

        private static string GetApplicationVersion()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            AssemblyInformationalVersionAttribute versionAttribute =
                entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return versionAttribute?.InformationalVersion ?? "Version information not available";
        }

        #endregion

        #region Single package operations (dgv context menu, File / Tools)

        private void CopyInfoItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem item) || item.Tag is not string kind)
                return;

            string value = kind switch
            {
                "title_id" => _snapshot?.TitleId,
                "content_id" => _snapshot?.ContentId,
                "title" => _snapshot?.Title,
                "filename" => Path.GetFileName(_currentPackagePath),
                _ => null
            };
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowWarning("Package information is not available yet.", false);
                return;
            }

            Clipboard.SetText(value);
            string what = kind switch
            {
                "title_id" => "Title ID",
                "content_id" => "Content ID",
                "title" => "Title",
                _ => "Filename"
            };
            ShowInformation(what + " copied to clipboard.", true);
        }

        private void RenamePkgItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem item) || item.Tag is not int fmtNum)
                return;
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }

            // Same shared format source as the main app and the Explorer
            // shell integration - one list, no per-UI copy.
            string format = PS4PKGTool.Utilities.Constants.PkgRenameFormats.GetFormat(fmtNum, appSettings_.RenameCustomName);
            if (string.IsNullOrWhiteSpace(format))
            {
                if (fmtNum == 11)
                    ShowError("Set custom name format in settings.", true);
                return;
            }

            // Preview example, same placeholders as the main app.
            string previewName = format
                .Replace("{TITLE}", "{Title}")
                .Replace("{TITLE_ID}", "CUSA00000")
                .Replace("{APP_VERSION}", "1.00")
                .Replace("{VERSION}", "1.00")
                .Replace("{CATEGORY}", "Game")
                .Replace("{CONTENT_ID}", "xxxxx")
                .Replace("{CONTENT_ID2}", "xxxxx")
                .Replace("{REGION}", "EU")
                .Replace("{SYSTEM_VERSION}", "9.00");

            if (DialogResultYesNo(
                $"Rename the PKG file?\n\nFormat: {format}\nExample: {previewName}.pkg") != DialogResult.Yes)
            {
                return;
            }

            labelDisplayTotalPKG.Text = "Renaming PKG...";
            try
            {
                string destinationFolder = Path.GetDirectoryName(_currentPackagePath) + @"\";
                (_, _, string targetPkg) =
                    PS4PKGTool.Utilities.PkgRename.PkgRenameService.GetNewPKGName(_currentPackagePath, destinationFolder, format);

                if (File.Exists(targetPkg))
                {
                    ShowWarning("A file with the target name already exists. The PKG was not renamed.", false);
                    labelDisplayTotalPKG.Text = "Ready";
                    return;
                }

                File.Move(_currentPackagePath, targetPkg);
                Logger.LogInformation(
                    $"Mini viewer renamed: {Path.GetFileName(_currentPackagePath)} -> {Path.GetFileName(targetPkg)}");
                _currentPackagePath = targetPkg;
                toolStripStatusLabel2.Text = targetPkg;
                Text = Path.GetFileName(targetPkg) + " - PKG Viewer";
                RebuildLazySessions();
                _ = LoadFilesAsync();
                ShowInformation("PKG renamed.", true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer rename failed: " + ex.Message);
                ShowError("The PKG could not be renamed: " + ex.Message, true);
            }
            finally
            {
                labelDisplayTotalPKG.Text = "Ready";
            }
        }

        private void DeletePkgItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }

            if (DialogResultYesNo(
                "PKG file will be permanently deleted. This operation cannot be undone. Are you sure you want to continue?") != DialogResult.Yes)
            {
                return;
            }

            try
            {
                File.Delete(_currentPackagePath);
                Logger.LogInformation("Mini viewer deleted: " + _currentPackagePath);
                ShowInformation("PKG file deleted.", true);
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer delete failed: " + ex.Message);
                ShowError("The PKG could not be deleted: " + ex.Message, true);
            }
        }

        private void SaveArtworkItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem item) || item.Tag is not string kind)
                return;
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }

            if (!ShowFolderBrowserDialog(out FolderBrowserDialog fbd))
                return;

            labelDisplayTotalPKG.Text = "Saving artwork...";
            string pkgName = Path.GetFileNameWithoutExtension(_currentPackagePath);
            var failures = new List<string>();
            try
            {
                byte[] icon = null, pic0 = null, pic1 = null;
                icon = PkgImageReader.ReadIcon0Png(_currentPackagePath);
                pic0 = PkgImageReader.ReadPic0Png(_currentPackagePath);
                pic1 = PkgImageReader.ReadPic1Png(_currentPackagePath);

                if (icon != null) SavePngFile(icon, Path.Combine(fbd.SelectedPath, $"{pkgName}_ICON.PNG"));
                if (pic0 != null) SavePngFile(pic0, Path.Combine(fbd.SelectedPath, $"{pkgName}_PIC0.PNG"));
                if (pic1 != null) SavePngFile(pic1, Path.Combine(fbd.SelectedPath, $"{pkgName}_PIC1.PNG"));
            }
            catch (Exception ex)
            {
                failures.Add(ex.Message);
            }

            labelDisplayTotalPKG.Text = "Ready";
            if (failures.Count > 0)
                ShowWarning("Artwork could not be saved: " + string.Join(" ", failures), true);
            else
                ShowInformation("Artwork saved.", true);
        }

        private static void SavePngFile(byte[] imageBytes, string filePath)
        {
            using var ms = new MemoryStream(imageBytes);
            using var bmp = Image.FromStream(ms);
            bmp.Save(filePath, ImageFormat.Png);
        }

        private void ViewChangeInfoItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }

            labelDisplayTotalPKG.Text = "Reading package change info...";
            try
            {
                // In-process extraction of Sc0/changeinfo/changeinfo.xml -
                // no spawn, no ASCII staging, package opened read-only.
                byte[] changeInfoBytes;
                using (var reader = new OrbisPkgTool.PkgReader(_currentPackagePath, PkgFileListingService.DefaultPasscode))
                {
                    changeInfoBytes = reader.ExtractEntryBytes("Sc0/changeinfo/changeinfo.xml");
                }

                string changeInfoData = System.Text.Encoding.UTF8.GetString(changeInfoBytes);
                using (var viewer = new PKGChangeInfoViewer(changeInfoData))
                    viewer.ShowDialog(this);
            }
            catch (FileNotFoundException)
            {
                ShowInformation("Change info not available.", true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer change info failed: " + ex.Message);
                ShowError("An error occurred while viewing the update changelog: " + ex.Message, true);
            }
            finally
            {
                labelDisplayTotalPKG.Text = "Ready";
            }
        }

        /// <summary>
        /// The package was renamed on disk: point the lazy sessions at the new
        /// path and drop their cached results so the tabs reload from there.
        /// </summary>
        private void RebuildLazySessions()
        {
            // The lazy sessions are rebuilt immediately after, so a Dispose
            // failure on a dead handle is harmless.
            try { _entrySession.Dispose(); } catch { /* best-effort */ }
            try { _trophySession.Dispose(); } catch { /* best-effort */ }
            try { _fileListingSession.Dispose(); } catch { /* best-effort */ }

            _entrySession = new PkgEntryInspectionSession(_currentPackagePath, _entryLoader);
            _trophySession = new TrophyInspectionSession(_currentPackagePath, _trophyLoader);
            _fileListingSession = new PkgFileListingSession(
                _currentPackagePath, _fileListingPasscode, _fileListingLoader);

            dgvEntries.Rows.Clear();
            dgvTrophies.Rows.Clear();
            tvFiles.Nodes.Clear();
            lvFiles.Items.Clear();
            lblTrophyState.Text = "Trophy information loads when this page is selected.";
        }

        private async void ExtractFullPkgItem_Click(object sender, EventArgs e)
        {
            if (_extracting)
            {
                ShowInformation("Extraction already in progress.", false);
                return;
            }
            if (!File.Exists(_currentPackagePath))
            {
                ShowError("PKG file not found.", false);
                return;
            }
            if (!ShowFolderBrowserDialog(out FolderBrowserDialog fbd))
                return;

            string sourcePath = _currentPackagePath;
            string extractLocation = Path.Combine(
                fbd.SelectedPath,
                SanitizeFolderName(_snapshot?.Title ?? Path.GetFileNameWithoutExtension(sourcePath)));

            // Lock the viewer (the status strip with Stop Extract stays live)
            // so no other operation touches the file.
            BeginExtractionUi();
            labelDisplayTotalPKG.Text = "Preparing extraction...";
            CancellationToken extractionToken = _extractionCancellation?.Token ?? CancellationToken.None;

            using var progress = new UiProgress<(int Current, int Total, string CurrentFile)>(p =>
            {
                if (_resourcesReleased || _extractionStopRequested || p.Total <= 0)
                    return;

                int completed = Math.Min(p.Current + 1, p.Total);
                toolStripProgressBar1.Minimum = 0;
                toolStripProgressBar1.Maximum = p.Total;
                toolStripProgressBar1.Value = completed;
                labelDisplayTotalPKG.Text = $"Extracting {completed}/{p.Total}: {p.CurrentFile}";
            });

            bool succeeded;
            string message;
            try
            {
                (succeeded, message) = await Task.Run(() => ExtractFullPkgCore(sourcePath, extractLocation, progress, extractionToken));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Mini viewer extract failed: " + ex.Message);
                succeeded = false;
                message = "Extraction failed: " + ex.Message;
            }
            finally
            {
                EndExtractionUi();
            }

            if (_extractionStopRequested)
            {
                ShowInformation("Extraction cancelled.", true);
                return;
            }

            if (succeeded)
            {
                string text = string.IsNullOrWhiteSpace(message)
                    ? $"PKG extracted to:\n{extractLocation}"
                    : $"PKG extracted to:\n{extractLocation}\n\n{message}";
                ShowInformation(text, true);
            }
            else
            {
                ShowError(message, true);
            }
        }

        /// <summary>
        /// Delegates to the shared PkgExtractionService - the exact same
        /// in-process extraction pipeline the Explorer shell integration
        /// uses.
        /// </summary>
        private (bool Succeeded, string Message) ExtractFullPkgCore(
            string sourcePath, string extractLocation,
            IProgress<(int Current, int Total, string CurrentFile)> progress,
            CancellationToken cancellationToken)
        {
            var service = new PkgExtractionService(_fileListingPasscode);
            return service.ExtractFullAsync(sourcePath, extractLocation, ct: cancellationToken,
                fileProgress: progress, logCompletion: true,
                layout: ExtractLayout.Merged).GetAwaiter().GetResult();
        }

        /// <summary>Shared folder-name sanitization (same rules as the main app's extract flows).</summary>
        private static string SanitizeFolderName(string name)
            => PkgExtractionService.SanitizeFolderName(name);

        // A rejected passcode surfaces in the failure message.
        private static bool IsPasscodeFailure(string errorMessage) =>
            !string.IsNullOrWhiteSpace(errorMessage) &&
            errorMessage.IndexOf("passcode", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Asks the user for a custom package passcode. Returns true and the
        /// entered value when confirmed; false when cancelled. The no-passcode
        /// option maps to PkgFileListingService.NoPasscode (--no_passcode).
        /// </summary>
        private bool PromptForPasscode(out string passcode)
        {
            using var prompt = new PasscodePromptForm();
            if (prompt.ShowDialog(this) != DialogResult.OK)
            {
                passcode = null;
                return false;
            }

            if (prompt.IsNoPasscode)
            {
                passcode = PkgFileListingService.NoPasscode;
                return true;
            }

            if (string.IsNullOrWhiteSpace(prompt.Passcode))
            {
                passcode = null;
                return false;
            }

            passcode = prompt.Passcode;
            return true;
        }

        #endregion
    }
}
