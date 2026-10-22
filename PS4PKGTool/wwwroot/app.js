(() => {
  "use strict";

  const state = {
    bootstrap: null,
    library: [],
    filteredLibrary: [],
    selectedPaths: new Set(),
    activePkgPath: null,
    activeInspection: null,
    activeTrophies: null,
    pfsFiles: [],
    selectedPfsEntry: null,
    activeAssetInspection: null,
    assetPreviewMode: "native",
    sortField: "title",
    sortAsc: true,
    highlightPaths: new Set(),
    updPieces: [],
    currentReportUrl: ""
  };

  // ── API Helper ─────────────────────────────────────────────────────────────
  async function apiGet(url) {
    const res = await fetch(url);
    if (!res.ok) {
      const txt = await res.text();
      throw new Error(txt || `HTTP ${res.status}`);
    }
    return res.json();
  }

  async function apiPost(url, body = {}) {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    if (!res.ok) {
      let msg = `HTTP ${res.status}`;
      try {
        const j = await res.json();
        msg = j.error || j.message || msg;
      } catch {
        msg = (await res.text()) || msg;
      }
      throw new Error(msg);
    }
    return res.json();
  }

  function showToast(title, message, type = "Info") {
    const container = document.getElementById("toast-container");
    if (!container) return;
    const el = document.createElement("div");
    el.className = `toast ${type}`;
    el.innerHTML = `<div style="font-weight:700;margin-bottom:3px;">${escapeHtml(title)}</div><div style="font-size:11.5px;white-space:pre-wrap;">${escapeHtml(message)}</div>`;
    container.appendChild(el);
    setTimeout(() => {
      el.style.opacity = "0";
      setTimeout(() => el.remove(), 300);
    }, 5500);
  }

  function setStatus(msg) {
    const el = document.getElementById("status-message");
    if (el) el.textContent = msg;
  }

  function escapeHtml(str) {
    if (str == null) return "";
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function formatBytes(bytes) {
    if (!bytes || bytes <= 0) return "0 B";
    const units = ["B", "KB", "MB", "GB", "TB"];
    let idx = 0;
    let val = Number(bytes);
    while (val >= 1024 && idx < units.length - 1) {
      val /= 1024;
      idx++;
    }
    return `${val.toFixed(idx === 0 ? 0 : 2)} ${units[idx]}`;
  }

  // ── Native GTK3 Dialog Helpers ─────────────────────────────────────────────
  async function pickFolder(title = "Select Folder", initialFolder = null) {
    const r = await apiPost("/api/dialog/folder", { title, initialFolder });
    return r.path || null;
  }

  async function pickOpenFiles(title = "Select File", filterName = "PKG Files", patterns = ["*.pkg"], multiSelect = false) {
    const r = await apiPost("/api/dialog/open-file", { title, filterName, patterns, multiSelect });
    return r.paths || [];
  }

  async function pickSaveFile(title = "Save File", defaultName = "output.pkg", initialFolder = null) {
    const r = await apiPost("/api/dialog/save-file", { title, defaultName, initialFolder });
    return r.path || null;
  }

  // ── Theme Application ──────────────────────────────────────────────────────
  function applyTheme(t, settings) {
    if (!t) return;
    const root = document.documentElement.style;
    root.setProperty("--bg-main", t.greyBackground || "#2d2d30");
    root.setProperty("--bg-header", t.headerBackground || "#393c3e");
    root.setProperty("--bg-dark", t.darkBackground || "#252526");
    root.setProperty("--bg-medium", t.mediumBackground || "#313335");
    root.setProperty("--bg-light", t.lightBackground || "#45494a");
    root.setProperty("--bg-lighter", t.lighterBackground || "#5f6566");
    root.setProperty("--border-light", t.lightBorder || "#515151");
    root.setProperty("--border-dark", t.darkBorder || "#333333");
    root.setProperty("--text-light", t.lightText || "#dcdcdc");
    root.setProperty("--text-disabled", t.disabledText || "#999999");
    root.setProperty("--accent-bg", t.blueBackground || "#3c709e");
    root.setProperty("--accent-dark", t.darkBlueBackground || "#34434c");
    root.setProperty("--accent-highlight", t.blueHighlight || "#6a97b8");
    root.setProperty("--accent-selection", t.blueSelection || "#4b6eaf");

    if (settings) {
      root.setProperty("--row-game-bg", settings.gamePkgBackColor || "#2e7d32");
      root.setProperty("--row-patch-bg", settings.patchPkgBackColor || "#1565c0");
      root.setProperty("--row-addon-bg", settings.addonPkgBackColor || "#8e24aa");
      root.setProperty("--row-app-bg", settings.appPkgBackColor || "#e65100");
    }
  }

  // ── Modal Management ───────────────────────────────────────────────────────
  function openModal(id) {
    const m = document.getElementById(id);
    if (m) m.classList.add("open");
  }

  function closeModal(id) {
    const m = document.getElementById(id);
    if (m) m.classList.remove("open");
  }

  document.querySelectorAll("[data-close]").forEach(btn => {
    btn.addEventListener("click", () => closeModal(btn.getAttribute("data-close")));
  });

  // ── Lower Tabs & shadPS4 Tabs ──────────────────────────────────────────────
  function activateLowerTab(tabId) {
    document.querySelectorAll(".lower-inspector-pane .tab-btn").forEach(b => {
      b.classList.toggle("active", b.getAttribute("data-tab") === tabId);
    });
    document.querySelectorAll(".lower-inspector-pane > .tab-panel").forEach(p => {
      p.classList.toggle("active", p.id === tabId);
    });
  }

  document.querySelectorAll(".lower-inspector-pane .tab-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      const tabId = btn.getAttribute("data-tab");
      activateLowerTab(tabId);
      if (tabId === "tab-trophies" && state.activePkgPath) loadTrophiesForPkg(state.activePkgPath);
      if (tabId === "tab-files" && state.activePkgPath && state.pfsFiles.length === 0) loadPfsFileTree(state.activePkgPath);
      if (tabId === "tab-logs") refreshLogs();
    });
  });

  document.querySelectorAll("#shadps4-tab-bar .tab-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      const stab = btn.getAttribute("data-stab");
      document.querySelectorAll("#shadps4-tab-bar .tab-btn").forEach(b => b.classList.toggle("active", b === btn));
      document.querySelectorAll("#modal-shadps4 .tab-panel").forEach(p => p.classList.toggle("active", p.id === stab));
      if (stab === "stab-overview") loadShadps4Overview();
      if (stab === "stab-games") loadShadps4Games();
      if (stab === "stab-builds") loadShadps4Overview();
      if (stab === "stab-saves") loadShadps4Saves();
      if (stab === "stab-feedback") loadFeedbackEntries();
    });
  });

  // ── Library Grid Rendering & Filtering ─────────────────────────────────────
  function setLibrary(items) {
    state.library = Array.isArray(items) ? items : [];
    state.highlightPaths.clear();
    populateRegionFilter();
    applyFiltersAndRender();
  }

  function populateRegionFilter() {
    const sel = document.getElementById("filter-region");
    const cur = sel.value;
    const regions = Array.from(new Set(state.library.map(x => x.region).filter(Boolean))).sort();
    sel.innerHTML = `<option value="ALL">All Regions</option>` + regions.map(r => `<option value="${escapeHtml(r)}">${escapeHtml(r)}</option>`).join("");
    if (regions.includes(cur)) sel.value = cur;
  }

  function normalizeCategoryKey(cat) {
    const c = (cat || "").toLowerCase();
    if (c.includes("game")) return "GAME";
    if (c.includes("patch") || c.includes("update")) return "PATCH";
    if (c.includes("addon") || c.includes("add-on") || c.includes("dlc")) return "ADDON";
    return "APP";
  }

  function applyFiltersAndRender() {
    const q = (document.getElementById("filter-search").value || "").trim().toLowerCase();
    const cat = document.getElementById("filter-category").value;
    const reg = document.getElementById("filter-region").value;
    const ps5 = document.getElementById("filter-ps5bc").value;
    const shad = document.getElementById("filter-shadps4").value;
    const instOnly = document.getElementById("filter-installed-only").checked;
    const groupBy = document.getElementById("filter-groupby").value;

    let rows = state.library.filter(item => {
      if (state.highlightPaths.size > 0 && !state.highlightPaths.has(item.filePath)) return false;
      if (cat !== "ALL" && normalizeCategoryKey(item.category) !== cat) return false;
      if (reg !== "ALL" && item.region !== reg) return false;
      if (ps5 !== "ALL" && !(item.ps5Bc || "").toLowerCase().includes(ps5.toLowerCase())) return false;
      if (shad !== "ALL" && !(item.shadps4Status || "").toLowerCase().includes(shad.toLowerCase())) return false;
      if (instOnly && !item.shadps4Installed) return false;
      if (q) {
        const hay = `${item.title} ${item.titleId} ${item.contentId} ${item.fileName} ${item.region} ${item.category} ${item.systemFirmware}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });

    const fieldMap = {
      PkgTitle: "title",
      PkgTitleId: "titleId",
      PkgContentId: "contentId",
      PkgRegion: "region",
      PkgType: "type",
      PkgCategory: "category",
      PkgVersion: "version",
      PkgFirmware: "systemFirmware",
      PkgBackport: "backport",
      PkgSizeBytes: "rawSize",
      Ps5BcStatus: "ps5Bc",
      Shadps4Status: "shadps4Status",
      PkgLocation: "fileName",
      PkgDirectory: "directory"
    };

    const key = fieldMap[state.sortField] || state.sortField || "title";
    rows.sort((a, b) => {
      const va = a[key] ?? "";
      const vb = b[key] ?? "";
      let cmp = typeof va === "number" && typeof vb === "number"
        ? va - vb
        : String(va).localeCompare(String(vb), undefined, { numeric: true, sensitivity: "base" });
      return state.sortAsc ? cmp : -cmp;
    });

    state.filteredLibrary = rows;
    renderLibraryTable(rows, groupBy, fieldMap[groupBy]);
    updateStatusBar(rows);
  }

  function getRowCategoryStyle(item) {
    const s = state.bootstrap?.settings;
    if (!s || !s.pkgColorLabel) return "";
    const k = normalizeCategoryKey(item.category);
    if (k === "GAME") return `background:${s.gamePkgBackColor || "#2e7d32"}44;`;
    if (k === "PATCH") return `background:${s.patchPkgBackColor || "#1565c0"}44;`;
    if (k === "ADDON") return `background:${s.addonPkgBackColor || "#8e24aa"}44;`;
    return `background:${s.appPkgBackColor || "#e65100"}44;`;
  }

  function renderLibraryTable(rows, groupBy, groupProp) {
    const tbody = document.getElementById("pkg-grid-body");
    if (!rows.length) {
      tbody.innerHTML = `<tr><td colspan="14" style="text-align:center;padding:26px;color:var(--text-disabled);">No PKG files match the current view.</td></tr>`;
      return;
    }

    let html = "";
    let lastGroup = null;

    if (groupBy && groupBy !== "NONE" && groupProp) {
      rows = [...rows].sort((a, b) => String(a[groupProp] || "").localeCompare(String(b[groupProp] || "")));
    }

    for (const item of rows) {
      if (groupBy && groupBy !== "NONE" && groupProp) {
        const gVal = item[groupProp] || "Unspecified";
        if (gVal !== lastGroup) {
          lastGroup = gVal;
          html += `<tr style="background:var(--bg-header);font-weight:700;color:var(--accent-highlight);"><td colspan="14">${escapeHtml(groupBy)}: ${escapeHtml(gVal)}</td></tr>`;
        }
      }

      const isSelected = state.selectedPaths.has(item.filePath) || state.activePkgPath === item.filePath;
      const rowStyle = getRowCategoryStyle(item);
      const shadBadge = item.shadps4Status
        ? `<span class="badge-pill">${escapeHtml(item.shadps4Status)}${item.shadps4Installed ? " ✓" : ""}</span>`
        : (item.shadps4Installed ? `<span class="badge-pill">Installed ✓</span>` : "-");

      html += `<tr data-path="${escapeHtml(item.filePath)}" class="${isSelected ? "selected" : ""}" style="${rowStyle}">
        <td><input type="checkbox" class="row-chk" data-path="${escapeHtml(item.filePath)}" ${state.selectedPaths.has(item.filePath) ? "checked" : ""} /></td>
        <td title="${escapeHtml(item.title)}"><b>${escapeHtml(item.title)}</b></td>
        <td>${escapeHtml(item.titleId)}</td>
        <td title="${escapeHtml(item.contentId)}">${escapeHtml(item.contentId)}</td>
        <td>${escapeHtml(item.region)}</td>
        <td>${escapeHtml(item.type)}</td>
        <td>${escapeHtml(item.category)}</td>
        <td>${escapeHtml(item.version)}</td>
        <td>${escapeHtml(item.systemFirmware)}</td>
        <td>${escapeHtml(item.backport)}</td>
        <td>${escapeHtml(item.size)}</td>
        <td title="${escapeHtml(item.ps5BcNote || "")}">${escapeHtml(item.ps5Bc || "-")}</td>
        <td>${shadBadge}</td>
        <td title="${escapeHtml(item.filePath)}">${escapeHtml(item.fileName)}</td>
      </tr>`;
    }

    tbody.innerHTML = html;

    tbody.querySelectorAll("tr[data-path]").forEach(tr => {
      const path = tr.getAttribute("data-path");
      tr.addEventListener("click", (ev) => {
        if (ev.target.classList.contains("row-chk")) return;
        if (!ev.ctrlKey && !ev.shiftKey) state.selectedPaths.clear();
        state.selectedPaths.add(path);
        selectPkgRow(path);
      });
      tr.addEventListener("contextmenu", (ev) => {
        ev.preventDefault();
        if (!state.selectedPaths.has(path)) {
          state.selectedPaths.clear();
          state.selectedPaths.add(path);
          selectPkgRow(path);
        }
        openContextMenu(ev.clientX, ev.clientY);
      });
    });

    tbody.querySelectorAll(".row-chk").forEach(chk => {
      chk.addEventListener("change", () => {
        const p = chk.getAttribute("data-path");
        if (chk.checked) state.selectedPaths.add(p);
        else state.selectedPaths.delete(p);
      });
    });
  }

  function updateStatusBar(rows) {
    let games = 0, patches = 0, addons = 0, apps = 0, bytes = 0;
    for (const r of rows) {
      bytes += r.rawSize || 0;
      const k = normalizeCategoryKey(r.category);
      if (k === "GAME") games++;
      else if (k === "PATCH") patches++;
      else if (k === "ADDON") addons++;
      else apps++;
    }
    document.getElementById("status-total").textContent = `Total PKGs: ${rows.length}`;
    document.getElementById("status-games").textContent = `Games: ${games}`;
    document.getElementById("status-patches").textContent = `Patches: ${patches}`;
    document.getElementById("status-addons").textContent = `Add-Ons: ${addons}`;
    document.getElementById("status-apps").textContent = `Apps: ${apps}`;
    document.getElementById("status-size").textContent = `Total Size: ${formatBytes(bytes)}`;
  }

  function getTargetPaths() {
    if (state.selectedPaths.size > 0) return Array.from(state.selectedPaths);
    if (state.activePkgPath) return [state.activePkgPath];
    return [];
  }

  // ── Inspect Selected PKG ───────────────────────────────────────────────────
  async function selectPkgRow(pkgPath) {
    state.activePkgPath = pkgPath;
    state.pfsFiles = [];
    document.querySelectorAll("#pkg-grid-body tr[data-path]").forEach(tr => {
      const p = tr.getAttribute("data-path");
      tr.classList.toggle("selected", p === pkgPath || state.selectedPaths.has(p));
    });

    const rowItem = state.library.find(x => x.filePath === pkgPath);
    if (rowItem) {
      document.getElementById("meta-title").textContent = rowItem.title || "-";
      document.getElementById("meta-titleid").textContent = rowItem.titleId || "-";
      document.getElementById("meta-contentid").textContent = rowItem.contentId || "-";
      document.getElementById("meta-category").textContent = rowItem.category || "-";
      document.getElementById("meta-version").textContent = rowItem.version || "-";
      document.getElementById("meta-fw").textContent = rowItem.systemFirmware || "-";
      document.getElementById("meta-region").textContent = rowItem.region || "-";
      document.getElementById("meta-size").textContent = rowItem.size || "-";
      document.getElementById("meta-backport").textContent = rowItem.backport || "No";
      document.getElementById("meta-ps5bc").textContent = rowItem.ps5Bc || "-";
      document.getElementById("meta-shadps4").textContent = rowItem.shadps4Status || "-";
    }

    try {
      setStatus(`Inspecting ${pkgPath.split("/").pop()}...`);
      const info = await apiGet(`/api/pkg/inspect?path=${encodeURIComponent(pkgPath)}`);
      state.activeInspection = info;

      // Update Side Card Artwork
      showArtworkType(info.hasIcon0 ? "icon0" : (info.hasPic1 ? "pic1" : "pic0"));

      // BGM Audio
      const bgmWrap = document.getElementById("bgm-player-wrap");
      const bgmAudio = document.getElementById("bgm-audio");
      if (info.hasBgm) {
        bgmWrap.style.display = "flex";
        bgmAudio.src = `/api/pkg/bgm?path=${encodeURIComponent(pkgPath)}`;
        if (state.bootstrap?.settings?.playBgm) {
          bgmAudio.play().catch(() => {});
        }
      } else {
        bgmWrap.style.display = "none";
        bgmAudio.pause();
        bgmAudio.removeAttribute("src");
      }

      renderSfoTable(info.sfoEntries || []);
      renderChangeInfo(info.changeInfo || []);
      renderHeaderAndEntries(info);

      const activeTab = document.querySelector(".lower-inspector-pane .tab-btn.active")?.getAttribute("data-tab");
      if (activeTab === "tab-trophies") loadTrophiesForPkg(pkgPath);
      if (activeTab === "tab-files") loadPfsFileTree(pkgPath);

      setStatus(`Loaded ${info.title} (${info.titleId})`);
    } catch (err) {
      showToast("PKG Inspection Error", err.message, "Error");
      setStatus("Inspection failed");
    }
  }

  function showArtworkType(type) {
    if (!state.activePkgPath) return;
    const img = document.getElementById("side-artwork-img");
    const ph = document.getElementById("side-artwork-placeholder");
    img.src = `/api/pkg/image?path=${encodeURIComponent(state.activePkgPath)}&type=${type}&t=${Date.now()}`;
    img.onload = () => {
      img.style.display = "block";
      ph.style.display = "none";
    };
    img.onerror = () => {
      img.style.display = "none";
      ph.style.display = "block";
      ph.textContent = `No ${type.toUpperCase()} in PKG`;
    };
  }

  function renderSfoTable(entries) {
    const q = (document.getElementById("sfo-filter").value || "").trim().toLowerCase();
    const tbody = document.getElementById("sfo-table-body");
    const filtered = entries.filter(e => !q || `${e.name} ${e.value}`.toLowerCase().includes(q));
    if (!filtered.length) {
      tbody.innerHTML = `<tr><td colspan="4" style="text-align:center;padding:16px;color:var(--text-disabled);">No PARAM.SFO entries.</td></tr>`;
      return;
    }
    tbody.innerHTML = filtered.map(e => `<tr>
      <td><b>${escapeHtml(e.name)}</b></td>
      <td style="white-space:normal;">${escapeHtml(e.value)}</td>
      <td>${escapeHtml(e.type || "Utf8")}</td>
      <td>${escapeHtml(e.length || "")}</td>
    </tr>`).join("");
  }

  function renderChangeInfo(changes) {
    const vBody = document.getElementById("changeinfo-versions-body");
    const txtEl = document.getElementById("changeinfo-text");
    if (!changes || !changes.length) {
      vBody.innerHTML = `<tr><td style="padding:12px;color:var(--text-disabled);">No changeinfo.xml found</td></tr>`;
      txtEl.textContent = "This package does not contain patch changelog notes (changeinfo.xml).";
      return;
    }
    vBody.innerHTML = changes.map((c, idx) => `<tr data-cidx="${idx}" class="${idx === 0 ? "selected" : ""}"><td><b>v${escapeHtml(c.appVer)}</b></td></tr>`).join("");
    txtEl.textContent = changes[0].text || "";
    vBody.querySelectorAll("tr[data-cidx]").forEach(tr => {
      tr.addEventListener("click", () => {
        vBody.querySelectorAll("tr").forEach(r => r.classList.remove("selected"));
        tr.classList.add("selected");
        const c = changes[Number(tr.getAttribute("data-cidx"))];
        txtEl.textContent = c ? c.text : "";
      });
    });
  }

  function renderHeaderAndEntries(info) {
    const hBody = document.getElementById("header-fields-body");
    const fields = [...(info.headerFields || [])];
    for (const p of (info.pubtoolEntries || [])) {
      fields.push({ name: `PUBTOOL: ${p.key}`, value: p.value });
    }
    hBody.innerHTML = fields.map(f => `<tr><td><b>${escapeHtml(f.name)}</b></td><td>${escapeHtml(f.value)}</td></tr>`).join("");

    const entries = info.entries || [];
    document.getElementById("pkg-entries-count").textContent = entries.length;
    const eBody = document.getElementById("pkg-entries-body");
    eBody.innerHTML = entries.map(e => `<tr>
      <td><input type="checkbox" class="pkg-entry-chk" data-id="${e.rawId}" /></td>
      <td><code>${escapeHtml(e.id)}</code></td>
      <td><b>${escapeHtml(e.name)}</b></td>
      <td><code>${escapeHtml(e.offset)}</code></td>
      <td>${escapeHtml(e.size)}</td>
      <td>${e.isEncrypted ? `<span style="color:var(--status-warn)">Yes (Key ${e.keyIndex})</span>` : "No"}</td>
    </tr>`).join("");
  }

  // ── Trophy Viewer ──────────────────────────────────────────────────────────
  async function loadTrophiesForPkg(pkgPath) {
    try {
      setStatus("Loading Trophy archive...");
      const t = await apiGet(`/api/pkg/trophies?path=${encodeURIComponent(pkgPath)}`);
      state.activeTrophies = t;
      document.getElementById("trophy-title-lbl").textContent = t.title || "Trophy Archive";
      document.getElementById("trophy-summary-badge").textContent = `${(t.trophies || []).length} Trophies`;
      document.getElementById("trophy-encryption-badge").textContent = t.statusMessage || (t.isEncrypted ? "Encrypted TRP" : "Decrypted TRP");

      const tbody = document.getElementById("trophy-table-body");
      if (!t.trophies || !t.trophies.length) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;padding:18px;color:var(--text-disabled);">${escapeHtml(t.statusMessage || "No trophies in this PKG.")}</td></tr>`;
        return;
      }
      tbody.innerHTML = t.trophies.map(tr => `<tr>
        <td>${tr.iconBase64 ? `<img src="data:image/png;base64,${tr.iconBase64}" style="width:36px;height:36px;object-fit:contain;" />` : "-"}</td>
        <td>#${tr.id}</td>
        <td><b>${escapeHtml(tr.grade)}</b></td>
        <td><b>${escapeHtml(tr.name)}</b></td>
        <td style="white-space:normal;">${escapeHtml(tr.detail)}</td>
        <td>${tr.hidden ? "Hidden" : "Visible"}</td>
        <td><code>${escapeHtml(tr.iconFileName)}</code></td>
      </tr>`).join("");
      setStatus("Trophies loaded.");
    } catch (err) {
      showToast("Trophy Viewer", err.message, "Warning");
    }
  }

  // ── 3-Pane File Browser & Game Asset Viewer ────────────────────────────────
  async function loadPfsFileTree(pkgPath) {
    if (!pkgPath) return;
    try {
      setStatus("Mounting & listing internal PFS image...");
      const passcode = document.getElementById("files-passcode").value.trim() || null;
      const res = await apiGet(`/api/pkg/files?path=${encodeURIComponent(pkgPath)}${passcode ? `&passcode=${encodeURIComponent(passcode)}` : ""}`);
      if (!res.succeeded) {
        document.getElementById("pfs-encrypted-badge").textContent = res.requiresPasscode ? "Passcode Required" : "Error";
        document.getElementById("pfs-files-body").innerHTML = `<tr><td colspan="2" style="padding:14px;color:var(--status-err);white-space:normal;">${escapeHtml(res.errorMessage)}</td></tr>`;
        return;
      }
      state.pfsFiles = res.entries || [];
      document.getElementById("pfs-encrypted-badge").textContent = "Mounted OK";
      renderPfsFileTree();
      setStatus(`Listed ${state.pfsFiles.length} internal entries.`);
    } catch (err) {
      showToast("PFS File Browser", err.message, "Error");
    }
  }

  function renderPfsFileTree() {
    const q = (document.getElementById("files-filter").value || "").trim().toLowerCase();
    const rows = state.pfsFiles.filter(f => !q || f.path.toLowerCase().includes(q));
    document.getElementById("pfs-files-count").textContent = rows.length;
    const tbody = document.getElementById("pfs-files-body");
    if (!rows.length) {
      tbody.innerHTML = `<tr><td colspan="2" style="padding:14px;color:var(--text-disabled);text-align:center;">No files match filter.</td></tr>`;
      return;
    }
    tbody.innerHTML = rows.slice(0, 1500).map(f => `<tr data-entry="${escapeHtml(f.path)}" data-isdir="${f.isDirectory}">
      <td title="${escapeHtml(f.path)}">${f.isDirectory ? "📁 " : "📄 "}${escapeHtml(f.path)}</td>
      <td>${escapeHtml(f.sizeFormatted || "")}</td>
    </tr>`).join("");

    tbody.querySelectorAll("tr[data-entry]").forEach(tr => {
      tr.addEventListener("click", () => {
        tbody.querySelectorAll("tr").forEach(r => r.classList.remove("selected"));
        tr.classList.add("selected");
        const entry = tr.getAttribute("data-entry");
        const isDir = tr.getAttribute("data-isdir") === "true";
        state.selectedPfsEntry = { path: entry, isDirectory: isDir };
        if (!isDir) inspectPfsAsset(entry);
      });
    });
  }

  async function inspectPfsAsset(internalPath) {
    if (!state.activePkgPath) return;
    const previewBody = document.getElementById("asset-preview-body");
    const metaBody = document.getElementById("asset-meta-body");
    const exportPngBtn = document.getElementById("btn-asset-export-png");
    previewBody.innerHTML = `<span style="color:var(--text-disabled);">Decoding ${escapeHtml(internalPath)}...</span>`;

    try {
      const passcode = document.getElementById("files-passcode").value.trim() || null;
      const url = `/api/pkg/asset/inspect?path=${encodeURIComponent(state.activePkgPath)}&entry=${encodeURIComponent(internalPath)}${passcode ? `&passcode=${encodeURIComponent(passcode)}` : ""}`;
      const asset = await apiGet(url);
      state.activeAssetInspection = asset;
      document.getElementById("asset-preview-title").textContent = `${asset.fileName} (${asset.format} / ${asset.engine})`;
      exportPngBtn.style.display = asset.imageBase64 ? "inline-flex" : "none";

      const metaRows = [
        ["File Name", asset.fileName],
        ["Internal Path", asset.internalPath],
        ["Size", asset.fileSizeFormatted],
        ["Format", asset.format],
        ["Engine", asset.engine],
        ["Capabilities", asset.capabilities]
      ];
      if (asset.metadata) {
        for (const [k, v] of Object.entries(asset.metadata)) metaRows.push([k, v]);
      }
      metaBody.innerHTML = metaRows.map(([k, v]) => `<tr><td><b>${escapeHtml(k)}</b></td><td style="white-space:normal;">${escapeHtml(v)}</td></tr>`).join("");

      renderActiveAssetPreview();
    } catch (err) {
      previewBody.innerHTML = `<span style="color:var(--status-err);">Preview error: ${escapeHtml(err.message)}</span>`;
    }
  }

  function renderActiveAssetPreview() {
    const asset = state.activeAssetInspection;
    const previewBody = document.getElementById("asset-preview-body");
    if (!asset) return;

    if (state.assetPreviewMode === "hex") {
      previewBody.style.alignItems = "stretch";
      previewBody.style.justifyContent = "flex-start";
      previewBody.innerHTML = `<pre class="code-view">${escapeHtml(asset.hexPreview || "No hex data.")}</pre>`;
      return;
    }

    if (asset.previewKind === "image" && asset.imageBase64) {
      previewBody.style.alignItems = "center";
      previewBody.style.justifyContent = "center";
      previewBody.innerHTML = `<img src="data:image/png;base64,${asset.imageBase64}" style="max-width:100%;max-height:100%;object-fit:contain;border:1px solid var(--border-light);" />`;
    } else if (asset.previewKind === "audio" && asset.audioUrl) {
      previewBody.style.alignItems = "center";
      previewBody.style.justifyContent = "center";
      previewBody.innerHTML = `<div style="text-align:center;display:flex;flex-direction:column;gap:10px;width:85%;">
        <div><b>Audio Stream:</b> ${escapeHtml(asset.fileName)}</div>
        <audio controls autoplay src="${asset.audioUrl}" style="width:100%;"></audio>
      </div>`;
    } else if (asset.previewKind === "text" && asset.textPreview != null) {
      previewBody.style.alignItems = "stretch";
      previewBody.style.justifyContent = "flex-start";
      previewBody.innerHTML = `<pre class="code-view" style="white-space:pre-wrap;">${escapeHtml(asset.textPreview)}</pre>`;
    } else if (asset.children && asset.children.length > 0) {
      previewBody.style.alignItems = "stretch";
      previewBody.style.justifyContent = "flex-start";
      previewBody.innerHTML = `<div style="margin-bottom:6px;font-weight:700;">Container Sub-Entries (${asset.children.length})</div>
        <table class="data-grid"><thead><tr><th>Sub-Entry</th><th>Size</th><th>Info</th></tr></thead>
        <tbody>${asset.children.map(c => `<tr><td>${escapeHtml(c.name)}</td><td>${escapeHtml(c.sizeFormatted)}</td><td>${escapeHtml(c.description)}</td></tr>`).join("")}</tbody></table>`;
    } else {
      previewBody.style.alignItems = "stretch";
      previewBody.style.justifyContent = "flex-start";
      previewBody.innerHTML = `<pre class="code-view">${escapeHtml(asset.hexPreview || "Binary file")}</pre>`;
    }
  }

  // ── Context Menu Commands ──────────────────────────────────────────────────
  function openContextMenu(x, y) {
    const menu = document.getElementById("pkg-context-menu");
    menu.classList.add("open");
    const rect = menu.getBoundingClientRect();
    const maxX = window.innerWidth - rect.width - 8;
    const maxY = window.innerHeight - rect.height - 8;
    menu.style.left = `${Math.max(8, Math.min(x, maxX))}px`;
    menu.style.top = `${Math.max(8, Math.min(y, maxY))}px`;
  }

  window.addEventListener("click", () => {
    document.getElementById("pkg-context-menu")?.classList.remove("open");
  });

  document.querySelectorAll("#pkg-context-menu .ctx-item").forEach(item => {
    item.addEventListener("click", async () => {
      const cmd = item.getAttribute("data-cmd");
      const targets = getTargetPaths();
      if (!targets.length) return;
      const first = targets[0];

      switch (cmd) {
        case "inspect":
          await selectPkgRow(first);
          break;
        case "install-shadps4":
          for (const p of targets) {
            await apiPost("/api/tasks/enqueue/shadps4-install", { pkgPath: p, replace: true });
          }
          activateLowerTab("tab-tasks");
          refreshTasks();
          showToast("shadPS4 Install", `Queued ${targets.length} PKG(s) for shadPS4 installation.`);
          break;
        case "extract-full": {
          const outDir = await pickFolder("Select Destination Folder for PKG Extraction");
          if (!outDir) return;
          for (const p of targets) {
            await apiPost("/api/tasks/enqueue/extract", { pkgPath: p, outputDir: outDir, rawLayout: false });
          }
          activateLowerTab("tab-tasks");
          refreshTasks();
          showToast("PKG Extraction", `Queued ${targets.length} PKG(s) for extraction.`);
          break;
        }
        case "browse-files":
          await selectPkgRow(first);
          activateLowerTab("tab-files");
          await loadPfsFileTree(first);
          break;
        case "rename":
          openRenameModal();
          break;
        case "move":
          openModal("modal-move");
          break;
        case "toggle-backport": {
          const row = state.library.find(x => x.filePath === first);
          const next = !(row && row.backport === "Yes");
          const lib = await apiPost("/api/library/backport", { files: targets, backported: next });
          setLibrary(lib);
          showToast("Backport Flag", `Updated backport flag to ${next ? "Yes" : "No"}.`);
          break;
        }
        case "merge":
          openMergeModalWithSelection();
          break;
        case "ffpfsc":
          document.getElementById("ffpfsc-src-pkg").value = first;
          document.getElementById("ffpfsc-out-pkg").value = first.replace(/\.pkg$/i, "_ffpfsc.pkg");
          openModal("modal-ffpfsc");
          break;
        case "check-updates": {
          const row = state.library.find(x => x.filePath === first);
          if (row?.titleId) document.getElementById("upd-titleid").value = row.titleId;
          openModal("modal-updates");
          if (row?.titleId) document.getElementById("btn-upd-check").click();
          break;
        }
        case "rpi-send":
          document.getElementById("rpi-pkg-path").value = first;
          openModal("modal-rpi");
          break;
        case "validate":
          document.getElementById("val-pkg-path").value = first;
          openModal("modal-validate");
          document.getElementById("btn-run-validate").click();
          break;
        case "save-artwork": {
          const outDir = await pickFolder("Select Folder to Save PKG Artwork (ICON0/PIC0/PIC1)");
          if (!outDir) return;
          const r = await apiPost("/api/pkg/image/save", { pkgPath: first, type: "all", outputPath: outDir });
          showToast("Artwork Saved", `Saved ${r.saved} PNG image(s) to ${r.outputPath}`);
          break;
        }
        case "set-wallpaper":
          await apiPost("/api/pkg/wallpaper", { pkgPath: first, type: "pic1", style: "Stretched" });
          showToast("Desktop Wallpaper", "Desktop wallpaper updated from PKG artwork!");
          break;
        case "open-folder":
          await apiPost("/api/system/open", { target: first });
          break;
        case "copy-titleid": {
          const row = state.library.find(x => x.filePath === first);
          if (row) {
            await apiPost("/api/system/copy", { text: `${row.title} | ${row.titleId} | ${row.contentId}` });
            showToast("Copied", "Copied Title, Title ID & Content ID to clipboard.");
          }
          break;
        }
        case "remove-list": {
          const r = await apiPost("/api/library/delete", { files: targets, deleteFromDisk: false });
          setLibrary(r.library);
          break;
        }
        case "delete-disk":
          if (confirm(`Are you sure you want to permanently DELETE ${targets.length} PKG file(s) from disk?`)) {
            const r = await apiPost("/api/library/delete", { files: targets, deleteFromDisk: true });
            setLibrary(r.library);
            showToast("Deleted", `Deleted ${r.deleted} file(s) from disk.`);
          }
          break;
      }
    });
  });

  // ── Toolbar & Button Handlers ──────────────────────────────────────────────
  document.getElementById("btn-scan-dirs").addEventListener("click", async () => {
    try {
      setStatus("Scanning configured PKG directories...");
      const lib = await apiPost("/api/library/scan", { append: false });
      setLibrary(lib);
      setStatus(`Scan complete (${lib.length} PKGs).`);
    } catch (err) {
      showToast("Scan Error", err.message, "Error");
    }
  });

  document.getElementById("btn-add-folder").addEventListener("click", async () => {
    const dir = await pickFolder("Select Folder Containing PS4 PKG Files");
    if (!dir) return;
    setStatus(`Scanning ${dir}...`);
    const lib = await apiPost("/api/library/scan", { directories: [dir], append: true });
    setLibrary(lib);
    setStatus(`Added folder ${dir} (${lib.length} total PKGs).`);
  });

  document.getElementById("btn-add-files").addEventListener("click", async () => {
    const files = await pickOpenFiles("Select PS4 PKG File(s)", "PS4 Package (*.pkg)", ["*.pkg", "*.PKG"], true);
    if (!files.length) return;
    const lib = await apiPost("/api/library/add-files", { files, append: true });
    setLibrary(lib);
    if (files.length === 1) await selectPkgRow(files[0]);
  });

  document.getElementById("btn-load-manifest").addEventListener("click", async () => {
    const lib = await apiPost("/api/library/load-manifest");
    setLibrary(lib);
    showToast("Manifest Loaded", `Loaded ${lib.length} PKG(s) from cached manifest.`);
  });

  document.getElementById("btn-find-dups").addEventListener("click", async () => {
    const dups = await apiGet("/api/library/duplicates");
    if (!dups.length) {
      showToast("Find Duplicates", "No duplicate Content IDs found in library.");
      return;
    }
    state.highlightPaths = new Set(dups);
    applyFiltersAndRender();
    showToast("Duplicates Found", `Filtering library to ${dups.length} duplicate PKG file(s). Click Reset Filters to show all.`);
  });

  document.getElementById("btn-find-missing-base").addEventListener("click", async () => {
    const missing = await apiGet("/api/library/missing-base");
    if (!missing.length) {
      showToast("Missing Base Game", "All patches and DLCs have a matching Base Game PKG in the library.");
      return;
    }
    state.highlightPaths = new Set(missing);
    applyFiltersAndRender();
    showToast("Missing Base Game", `Showing ${missing.length} Patch/DLC PKG(s) without a matching Base Game PKG.`);
  });

  document.getElementById("btn-export-excel").addEventListener("click", async () => {
    const out = await pickSaveFile("Export PKG Library to Excel", "PS4PKGTool_Library.xlsx");
    if (!out) return;
    const r = await apiPost("/api/library/export-excel", { outputPath: out });
    showToast("Excel Exported", `Saved library spreadsheet to ${r.savedPath}`);
  });

  // Filters
  ["filter-search", "filter-category", "filter-region", "filter-ps5bc", "filter-shadps4", "filter-installed-only", "filter-groupby"].forEach(id => {
    const el = document.getElementById(id);
    el.addEventListener("input", applyFiltersAndRender);
    el.addEventListener("change", applyFiltersAndRender);
  });

  document.getElementById("btn-reset-filters").addEventListener("click", () => {
    document.getElementById("filter-search").value = "";
    document.getElementById("filter-category").value = "ALL";
    document.getElementById("filter-region").value = "ALL";
    document.getElementById("filter-ps5bc").value = "ALL";
    document.getElementById("filter-shadps4").value = "ALL";
    document.getElementById("filter-installed-only").checked = false;
    document.getElementById("filter-groupby").value = "NONE";
    state.highlightPaths.clear();
    applyFiltersAndRender();
  });

  document.getElementById("chk-select-all").addEventListener("change", (ev) => {
    state.selectedPaths.clear();
    if (ev.target.checked) {
      for (const r of state.filteredLibrary) state.selectedPaths.add(r.filePath);
    }
    applyFiltersAndRender();
  });

  document.querySelectorAll("#pkg-grid thead th[data-sort]").forEach(th => {
    th.addEventListener("click", () => {
      const f = th.getAttribute("data-sort");
      if (state.sortField === f) state.sortAsc = !state.sortAsc;
      else { state.sortField = f; state.sortAsc = true; }
      applyFiltersAndRender();
    });
  });

  // Side Card Actions
  document.getElementById("btn-art-icon0").addEventListener("click", () => showArtworkType("icon0"));
  document.getElementById("btn-art-pic1").addEventListener("click", () => showArtworkType("pic1"));
  document.getElementById("btn-art-pic0").addEventListener("click", () => showArtworkType("pic0"));

  document.getElementById("btn-save-art").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const outDir = await pickFolder("Select Folder to Save PKG Artwork");
    if (!outDir) return;
    const r = await apiPost("/api/pkg/image/save", { pkgPath: state.activePkgPath, type: "all", outputPath: outDir });
    showToast("Saved Artwork", `Exported ${r.saved} PNG image(s) to ${r.outputPath}`);
  });

  document.getElementById("btn-set-wallpaper").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    await apiPost("/api/pkg/wallpaper", { pkgPath: state.activePkgPath, type: "pic1", style: "Stretched" });
    showToast("Wallpaper Set", "Applied PKG background as Linux desktop wallpaper.");
  });

  document.getElementById("btn-side-install-shadps4").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    await apiPost("/api/tasks/enqueue/shadps4-install", { pkgPath: state.activePkgPath, replace: true });
    activateLowerTab("tab-tasks");
    refreshTasks();
    showToast("Queued", "Queued PKG installation to shadPS4.");
  });

  document.getElementById("btn-side-extract").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const outDir = await pickFolder("Select Extraction Folder");
    if (!outDir) return;
    await apiPost("/api/tasks/enqueue/extract", { pkgPath: state.activePkgPath, outputDir: outDir, rawLayout: false });
    activateLowerTab("tab-tasks");
    refreshTasks();
  });

  document.getElementById("btn-side-validate").addEventListener("click", () => {
    if (!state.activePkgPath) return;
    document.getElementById("val-pkg-path").value = state.activePkgPath;
    openModal("modal-validate");
    document.getElementById("btn-run-validate").click();
  });

  // Lower Tab 1: SFO
  document.getElementById("sfo-filter").addEventListener("input", () => {
    renderSfoTable(state.activeInspection?.sfoEntries || []);
  });

  document.getElementById("btn-copy-sfo").addEventListener("click", async () => {
    const entries = state.activeInspection?.sfoEntries || [];
    const txt = entries.map(e => `${e.name} = ${e.value}`).join("\n");
    await apiPost("/api/system/copy", { text: txt });
    showToast("Copied", "PARAM.SFO table copied to clipboard.");
  });

  document.getElementById("btn-ps5bc-detail").addEventListener("click", () => {
    const row = state.library.find(x => x.filePath === state.activePkgPath);
    showToast("PS5 Backward Compatibility", row?.ps5BcNote || row?.ps5Bc || "No PS5 BC notes for this title.");
  });

  // Lower Tab 3: Trophies
  document.getElementById("btn-load-trophies").addEventListener("click", () => {
    if (state.activePkgPath) loadTrophiesForPkg(state.activePkgPath);
  });

  document.getElementById("btn-extract-trophies").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const outDir = await pickFolder("Select Folder to Extract Trophy Icons & JSON");
    if (!outDir) return;
    const r = await apiPost("/api/pkg/trophies/extract", { pkgPath: state.activePkgPath, outputDir: outDir });
    showToast("Trophies Extracted", r.message);
  });

  document.getElementById("btn-build-trophy-cache").addEventListener("click", async () => {
    setStatus("Building Trophy NPWR cache across library...");
    const r = await apiPost("/api/trophies/build-cache");
    showToast("Trophy Cache Built", `Scanned ${r.scannedPackages || 0} PKGs, indexed ${r.mappedTitles || 0} trophy sets.`);
    setStatus("Trophy cache updated.");
  });

  // Lower Tab 4: Header & Entries
  document.getElementById("btn-open-digest-tool").addEventListener("click", () => {
    if (!state.activePkgPath) return;
    document.getElementById("val-pkg-path").value = state.activePkgPath;
    openModal("modal-validate");
    document.getElementById("btn-run-validate").click();
  });

  document.getElementById("chk-all-entries").addEventListener("change", (ev) => {
    document.querySelectorAll(".pkg-entry-chk").forEach(c => { c.checked = ev.target.checked; });
  });

  document.getElementById("btn-extract-selected-entries").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const ids = Array.from(document.querySelectorAll(".pkg-entry-chk:checked")).map(c => Number(c.getAttribute("data-id")));
    if (!ids.length) {
      showToast("Select Entries", "Check one or more entries first.", "Warning");
      return;
    }
    const outDir = await pickFolder("Select Output Folder for Raw PKG Entries");
    if (!outDir) return;
    const r = await apiPost("/api/pkg/entries/extract", { pkgPath: state.activePkgPath, outputDir: outDir, entryIds: ids });
    showToast("Entries Extracted", `Extracted ${r.extracted} entry file(s) to ${r.outputDir}`);
  });

  document.getElementById("btn-extract-all-entries").addEventListener("click", async () => {
    if (!state.activePkgPath || !state.activeInspection?.entries) return;
    const ids = state.activeInspection.entries.map(e => e.rawId);
    const outDir = await pickFolder("Select Output Folder for All Raw PKG Entries");
    if (!outDir) return;
    const r = await apiPost("/api/pkg/entries/extract", { pkgPath: state.activePkgPath, outputDir: outDir, entryIds: ids });
    showToast("Entries Extracted", `Extracted ${r.extracted} entry file(s) to ${r.outputDir}`);
  });

  // Lower Tab 5: File Browser & Asset Viewer
  document.getElementById("btn-load-pfs-files").addEventListener("click", () => {
    if (state.activePkgPath) loadPfsFileTree(state.activePkgPath);
  });
  document.getElementById("files-filter").addEventListener("input", renderPfsFileTree);

  document.getElementById("btn-asset-view-native").addEventListener("click", () => {
    state.assetPreviewMode = "native";
    renderActiveAssetPreview();
  });
  document.getElementById("btn-asset-view-hex").addEventListener("click", () => {
    state.assetPreviewMode = "hex";
    renderActiveAssetPreview();
  });

  document.getElementById("btn-asset-export-png").addEventListener("click", async () => {
    if (!state.activePkgPath || !state.selectedPfsEntry) return;
    const out = await pickSaveFile("Export Decoded Texture as PNG", `${state.selectedPfsEntry.path.split("/").pop()}.png`);
    if (!out) return;
    const passcode = document.getElementById("files-passcode").value.trim() || null;
    const r = await apiPost("/api/pkg/asset/export-png", {
      pkgPath: state.activePkgPath,
      internalPath: state.selectedPfsEntry.path,
      outputPngPath: out,
      passcode
    });
    showToast("PNG Exported", `Saved texture to ${r.savedPath}`);
  });

  document.getElementById("btn-extract-pfs-selected").addEventListener("click", async () => {
    if (!state.activePkgPath || !state.selectedPfsEntry) {
      showToast("Select File", "Click a file or folder in the PFS tree first.", "Warning");
      return;
    }
    const dest = await pickFolder("Select Destination Folder");
    if (!dest) return;
    const passcode = document.getElementById("files-passcode").value.trim() || null;
    const r = await apiPost("/api/pkg/files/extract", {
      pkgPath: state.activePkgPath,
      internalPath: state.selectedPfsEntry.path,
      isDirectory: state.selectedPfsEntry.isDirectory,
      destinationFolder: dest,
      passcode
    });
    showToast("Extraction", r.message, r.succeeded ? "Info" : "Error");
  });

  document.getElementById("btn-extract-pfs-root").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const dest = await pickFolder("Select Destination Folder for Full PFS Extraction");
    if (!dest) return;
    await apiPost("/api/tasks/enqueue/extract", { pkgPath: state.activePkgPath, outputDir: dest, rawLayout: false });
    activateLowerTab("tab-tasks");
    refreshTasks();
  });

  document.getElementById("btn-export-file-tree").addEventListener("click", async () => {
    if (!state.activePkgPath) return;
    const out = await pickSaveFile("Export PFS File Tree (.txt)", "file_tree.txt");
    if (!out) return;
    const passcode = document.getElementById("files-passcode").value.trim() || null;
    const r = await apiPost("/api/pkg/files/export-tree", { pkgPath: state.activePkgPath, outputPath: out, passcode });
    showToast("File Tree Exported", `Saved to ${r.savedPath}`);
  });

  // ── Rename Modal ───────────────────────────────────────────────────────────
  function openRenameModal() {
    openModal("modal-rename");
    refreshRenamePreview();
  }

  document.getElementById("rename-format").addEventListener("change", (ev) => {
    document.getElementById("rename-custom-row").style.display = ev.target.value === "12" ? "flex" : "none";
    refreshRenamePreview();
  });
  document.getElementById("rename-custom").addEventListener("input", refreshRenamePreview);
  document.getElementById("btn-rename-preview").addEventListener("click", refreshRenamePreview);

  async function refreshRenamePreview() {
    const files = getTargetPaths();
    if (!files.length) return;
    const formatId = Number(document.getElementById("rename-format").value);
    const customPattern = document.getElementById("rename-custom").value;
    const list = await apiPost("/api/library/rename/preview", { files, formatId, customPattern });
    document.getElementById("rename-preview-body").innerHTML = list.map(x => `<tr>
      <td>${escapeHtml(x.originalFileName)}</td>
      <td><b>${escapeHtml(x.newFileName)}</b></td>
      <td>${x.willChange ? `<span style="color:var(--status-ok)">Ready</span>` : "Unchanged"}</td>
    </tr>`).join("");
  }

  document.getElementById("btn-rename-apply").addEventListener("click", async () => {
    const files = getTargetPaths();
    if (!files.length) return;
    const formatId = Number(document.getElementById("rename-format").value);
    const customPattern = document.getElementById("rename-custom").value;
    const r = await apiPost("/api/library/rename/execute", { files, formatId, customPattern });
    setLibrary(r.library);
    closeModal("modal-rename");
    showToast("Rename Complete", `Renamed ${r.renamed} file(s).`);
  });

  // ── Move Modal ─────────────────────────────────────────────────────────────
  document.getElementById("btn-browse-move-dir").addEventListener("click", async () => {
    const d = await pickFolder("Select Destination Root Folder");
    if (d) document.getElementById("move-dest-dir").value = d;
  });

  document.getElementById("btn-move-execute").addEventListener("click", async () => {
    const files = getTargetPaths();
    const destinationRoot = document.getElementById("move-dest-dir").value.trim();
    const subfolderMode = document.getElementById("move-subfolder-mode").value;
    if (!destinationRoot) return;
    const r = await apiPost("/api/library/move", { files, destinationRoot, subfolderMode });
    setLibrary(r.library);
    closeModal("modal-move");
    showToast("Move Complete", `Moved ${r.moved} PKG file(s).`);
  });

  // ── Merge Modal ────────────────────────────────────────────────────────────
  function openMergeModalWithSelection() {
    const targets = getTargetPaths();
    for (const p of targets) {
      const row = state.library.find(x => x.filePath === p);
      if (!row) continue;
      if (normalizeCategoryKey(row.category) === "GAME") document.getElementById("merge-base-pkg").value = p;
      else if (normalizeCategoryKey(row.category) === "PATCH") document.getElementById("merge-update-pkg").value = p;
    }
    openModal("modal-merge");
  }

  document.getElementById("btn-open-merge").addEventListener("click", openMergeModalWithSelection);
  document.getElementById("btn-browse-merge-base").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select Base Game PKG", "PKG", ["*.pkg"], false);
    if (f[0]) document.getElementById("merge-base-pkg").value = f[0];
  });
  document.getElementById("btn-browse-merge-update").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select Update Patch PKG", "PKG", ["*.pkg"], false);
    if (f[0]) document.getElementById("merge-update-pkg").value = f[0];
  });
  document.getElementById("btn-browse-merge-output").addEventListener("click", async () => {
    const f = await pickSaveFile("Save Merged PKG As", "merged_game.pkg");
    if (f) document.getElementById("merge-output-pkg").value = f;
  });
  document.getElementById("btn-merge-enqueue").addEventListener("click", async () => {
    const basePkgPath = document.getElementById("merge-base-pkg").value.trim();
    const updatePkgPath = document.getElementById("merge-update-pkg").value.trim();
    const outputPkgPath = document.getElementById("merge-output-pkg").value.trim();
    if (!basePkgPath || !updatePkgPath || !outputPkgPath) {
      showToast("Merge PKG", "Please select Base PKG, Update PKG, and Output PKG path.", "Warning");
      return;
    }
    await apiPost("/api/tasks/enqueue/merge", {
      basePkgPath,
      updatePkgPath,
      outputPkgPath,
      pfscMode: document.getElementById("merge-pfsc-mode").value,
      workerCount: Number(document.getElementById("merge-workers").value) || 4,
      validateAfterBuild: document.getElementById("merge-validate").checked
    });
    closeModal("modal-merge");
    activateLowerTab("tab-tasks");
    refreshTasks();
    showToast("Merge Task Queued", "Base + Update PKG merge queued in Task Queue.");
  });

  // ── FFPFSC Modal ───────────────────────────────────────────────────────────
  document.getElementById("btn-open-ffpfsc").addEventListener("click", () => {
    if (state.activePkgPath) {
      document.getElementById("ffpfsc-src-pkg").value = state.activePkgPath;
      document.getElementById("ffpfsc-out-pkg").value = state.activePkgPath.replace(/\.pkg$/i, "_ffpfsc.pkg");
    }
    openModal("modal-ffpfsc");
  });
  document.getElementById("btn-browse-ffpfsc-src").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select Source PKG", "PKG", ["*.pkg"], false);
    if (f[0]) document.getElementById("ffpfsc-src-pkg").value = f[0];
  });
  document.getElementById("btn-browse-ffpfsc-out").addEventListener("click", async () => {
    const f = await pickSaveFile("Save FFPFSC PKG As", "output_ffpfsc.pkg");
    if (f) document.getElementById("ffpfsc-out-pkg").value = f;
  });
  document.getElementById("btn-ffpfsc-enqueue").addEventListener("click", async () => {
    const pkgPath = document.getElementById("ffpfsc-src-pkg").value.trim();
    const outputPath = document.getElementById("ffpfsc-out-pkg").value.trim();
    if (!pkgPath || !outputPath) return;
    await apiPost("/api/tasks/enqueue/ffpfsc", {
      pkgPath,
      outputPath,
      zlibLevel: Number(document.getElementById("ffpfsc-zlib").value) || 6,
      verify: document.getElementById("ffpfsc-verify").checked,
      generateSidecars: document.getElementById("ffpfsc-sidecars").checked
    });
    closeModal("modal-ffpfsc");
    activateLowerTab("tab-tasks");
    refreshTasks();
    showToast("FFPFSC Conversion Queued", "Added FFPFSC conversion to Task Queue.");
  });

  // ── Official Sony Update Downloader Modal ──────────────────────────────────
  document.getElementById("btn-open-updates").addEventListener("click", () => {
    const row = state.library.find(x => x.filePath === state.activePkgPath);
    if (row?.titleId) document.getElementById("upd-titleid").value = row.titleId;
    openModal("modal-updates");
  });

  document.getElementById("btn-browse-upd-dir").addEventListener("click", async () => {
    const d = await pickFolder("Select Update Download Folder");
    if (d) document.getElementById("upd-download-dir").value = d;
  });

  document.getElementById("btn-upd-check").addEventListener("click", async () => {
    const tid = document.getElementById("upd-titleid").value.trim();
    if (!tid) return;
    try {
      const r = await apiGet(`/api/updates/check?titleId=${encodeURIComponent(tid)}`);
      if (!r.available) {
        document.getElementById("upd-summary-lbl").textContent = r.message || "No updates found.";
        document.getElementById("upd-pieces-body").innerHTML = `<tr><td colspan="4" style="text-align:center;padding:14px;">${escapeHtml(r.message)}</td></tr>`;
        document.getElementById("btn-upd-download").disabled = true;
        return;
      }
      state.updPieces = r.pieces || [];
      document.getElementById("upd-summary-lbl").textContent = `Latest: v${r.version} (FW ${r.systemVer}) • ${r.totalSizeFormatted}`;
      document.getElementById("upd-pieces-body").innerHTML = state.updPieces.map(p => `<tr>
        <td><b>${escapeHtml(p.partLabel)}</b></td>
        <td>${escapeHtml(p.fileSize)}</td>
        <td><code>${escapeHtml((p.sha256 || "").slice(0, 16))}...</code></td>
        <td title="${escapeHtml(p.url)}"><a href="#" onclick="return false;" style="color:var(--accent-highlight);">${escapeHtml(p.url.split("/").pop())}</a></td>
      </tr>`).join("");
      document.getElementById("btn-upd-download").disabled = state.updPieces.length === 0;
    } catch (err) {
      showToast("Update Check Failed", err.message, "Error");
    }
  });

  document.getElementById("btn-upd-download").addEventListener("click", async () => {
    let dir = document.getElementById("upd-download-dir").value.trim();
    if (!dir) {
      dir = await pickFolder("Select Folder to Download Official Sony Update PKGs");
      if (!dir) return;
      document.getElementById("upd-download-dir").value = dir;
    }
    const titleId = document.getElementById("upd-titleid").value.trim();
    await apiPost("/api/updates/download", { titleId, downloadDirectory: dir, pieces: state.updPieces });
    showToast("Download Started", `Downloading ${state.updPieces.length} piece(s) for ${titleId}...`);
  });

  document.getElementById("btn-upd-cancel").addEventListener("click", async () => {
    await apiPost("/api/updates/cancel");
  });

  // ── PS4 Remote Package Installer (RPI) ─────────────────────────────────────
  document.getElementById("btn-open-rpi").addEventListener("click", () => {
    if (state.activePkgPath) document.getElementById("rpi-pkg-path").value = state.activePkgPath;
    openModal("modal-rpi");
  });

  document.getElementById("btn-rpi-browse-pkg").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select PKG to Send to PS4", "PKG", ["*.pkg"], false);
    if (f[0]) document.getElementById("rpi-pkg-path").value = f[0];
  });

  document.getElementById("btn-rpi-save-net").addEventListener("click", async () => {
    const localServerIp = document.getElementById("rpi-local-ip").value.trim();
    const ps4Ip = document.getElementById("rpi-ps4-ip").value.trim();
    await apiPost("/api/settings", { localServerIp, ps4Ip });
    showToast("Network Saved", "Saved Local PC IP and PS4 Console IP.");
  });

  document.querySelectorAll("[data-rpi]").forEach(btn => {
    btn.addEventListener("click", async () => {
      const actionMap = {
        send: "send",
        exists: "check_exists",
        progress: "progress",
        pause: "stop",
        resume: "send",
        cancel: "stop",
        uninstall_game: "uninstall_game",
        uninstall_patch: "uninstall_patch"
      };
      const act = actionMap[btn.getAttribute("data-rpi")] || btn.getAttribute("data-rpi");
      const pkgPath = document.getElementById("rpi-pkg-path").value.trim();
      const view = document.getElementById("rpi-response-view");
      view.textContent = `Executing RPI '${act}'...`;
      try {
        const r = await apiPost("/api/rpi/action", { action: act, pkgPath });
        view.textContent = JSON.stringify(r, null, 2);
      } catch (err) {
        view.textContent = `Error: ${err.message}`;
      }
    });
  });

  // ── Validate & Fix Digests Modal ───────────────────────────────────────────
  document.getElementById("btn-run-validate").addEventListener("click", async () => {
    const pkgPath = document.getElementById("val-pkg-path").value.trim();
    if (!pkgPath) return;
    const r = await apiPost("/api/pkg/validate", { pkgPath });
    const tbody = document.getElementById("val-results-body");
    tbody.innerHTML = (r.issues || []).map(i => `<tr>
      <td><b>${escapeHtml(i.code || "Structure")}</b></td>
      <td style="color:${i.severity === "Error" ? "var(--status-err)" : "var(--status-ok)"}">${escapeHtml(i.severity)}</td>
      <td style="white-space:normal;">${escapeHtml(i.message)}</td>
    </tr>`).join("") || `<tr><td colspan="3" style="text-align:center;padding:14px;color:var(--status-ok);">✓ PKG container structure passed all validation checks!</td></tr>`;
  });

  async function runDigestTool(dryRun) {
    const pkgPath = document.getElementById("val-pkg-path").value.trim();
    if (!pkgPath) return;
    const r = await apiPost("/api/pkg/digests", { pkgPath, dryRun });
    const tbody = document.getElementById("val-results-body");
    const items = r.items || [];
    tbody.innerHTML = items.map(d => `<tr>
      <td><b>${escapeHtml(d.name)}</b></td>
      <td style="color:${d.status === "OK" || d.status === "Fixed" ? "var(--status-ok)" : "var(--status-warn)"}">${escapeHtml(d.status)}</td>
      <td style="white-space:normal;"><code>Expected: ${escapeHtml(d.expected || "-")} | Actual: ${escapeHtml(d.actual || "-")}</code></td>
    </tr>`).join("");
    showToast(dryRun ? "Digest Verification" : "Digest Repair", r.summary || "Completed.");
  }

  document.getElementById("btn-run-digest-dry").addEventListener("click", () => runDigestTool(true));
  document.getElementById("btn-run-digest-fix").addEventListener("click", () => runDigestTool(false));

  // ── shadPS4 Manager Modal ──────────────────────────────────────────────────
  document.getElementById("btn-open-shadps4").addEventListener("click", () => {
    openModal("modal-shadps4");
    loadShadps4Overview();
  });

  async function loadShadps4Overview() {
    const ov = await apiGet("/api/shadps4/overview");
    document.getElementById("shadps4-cfg-gamedir").value = ov.installDirectory || "";
    document.getElementById("shadps4-cfg-corepath").value = ov.coreExePath || "";
    document.getElementById("shadps4-cfg-launcherpath").value = ov.launcherExePath || "";

    document.getElementById("shadps4-overview-cards").innerHTML = `
      <div class="info-card">
        <h4>Active Emulator Core (Linux SDL)</h4>
        <div><b>Selection:</b> ${escapeHtml(ov.activeCore || "Auto")}</div>
        <div><b>Binary:</b> <span style="color:${ov.coreExists ? "var(--status-ok)" : "var(--status-warn)"}">${escapeHtml(ov.coreExePath || "Not installed yet (Go to Managed Builds tab)")}</span></div>
        <div><b>Running:</b> ${ov.isEmulatorRunning ? "Yes (Active Session)" : "Idle"}</div>
      </div>
      <div class="info-card">
        <h4>Active Qt Launcher</h4>
        <div><b>Selection:</b> ${escapeHtml(ov.activeLauncher || "Auto")}</div>
        <div><b>Binary:</b> <span style="color:${ov.launcherExists ? "var(--status-ok)" : "var(--status-warn)"}">${escapeHtml(ov.launcherExePath || "Not installed yet")}</span></div>
        <div><b>Installed Games:</b> ${ov.installedGamesCount} Title(s) &bull; ${ov.saveSlotsCount} Save(s)</div>
      </div>
      <div class="info-card">
        <h4>Linux Host Hardware</h4>
        <div><b>CPU:</b> ${escapeHtml(ov.hostInfo?.cpu || "-")}</div>
        <div><b>GPU:</b> ${escapeHtml(ov.hostInfo?.gpu || "-")}</div>
        <div><b>RAM:</b> ${ov.hostInfo?.ramGb || "-"} GB &bull; ${escapeHtml(ov.hostInfo?.os || "Linux")}</div>
      </div>
    `;

    // Render Installed Managed Builds table
    const allBuilds = [
      ...(ov.coreBuilds || []).map(b => ({ ...b, comp: "Core" })),
      ...(ov.launcherBuilds || []).map(b => ({ ...b, comp: "QtLauncher" }))
    ];
    const bBody = document.getElementById("shadps4-installed-builds-body");
    if (!allBuilds.length) {
      bBody.innerHTML = `<tr><td colspan="5" style="text-align:center;padding:14px;color:var(--text-disabled);">No managed builds installed yet. Click <b>Fetch Latest Releases</b> on the right to download Linux builds!</td></tr>`;
    } else {
      bBody.innerHTML = allBuilds.map(b => `<tr>
        <td><b>${escapeHtml(b.comp)}</b></td>
        <td>${escapeHtml(b.displayName || b.buildId)}</td>
        <td><code>${escapeHtml(b.tag || b.version || "")}</code></td>
        <td title="${escapeHtml(b.executablePath || "")}">${escapeHtml((b.executablePath || "").split("/").pop())}</td>
        <td>
          <button class="btn-sm btn-primary" data-act-comp="${b.comp}" data-act-id="managed:${b.buildId}">Set Active</button>
          <button class="btn-sm btn-danger" data-del-comp="${b.comp}" data-del-id="${b.buildId}">Delete</button>
        </td>
      </tr>`).join("");

      bBody.querySelectorAll("[data-act-id]").forEach(btn => {
        btn.addEventListener("click", async () => {
          await apiPost("/api/shadps4/builds/activate", {
            component: btn.getAttribute("data-act-comp"),
            value: btn.getAttribute("data-act-id")
          });
          loadShadps4Overview();
          showToast("Active Build Updated", "Updated active shadPS4 build.");
        });
      });
      bBody.querySelectorAll("[data-del-id]").forEach(btn => {
        btn.addEventListener("click", async () => {
          await apiPost("/api/shadps4/builds/delete", {
            component: btn.getAttribute("data-del-comp"),
            buildId: btn.getAttribute("data-del-id")
          });
          loadShadps4Overview();
        });
      });
    }
  }

  document.getElementById("btn-shadps4-open-qt").addEventListener("click", async () => {
    const r = await apiPost("/api/shadps4/open-launcher");
    showToast("shadPS4 Qt Launcher", r.message, r.succeeded ? "Info" : "Warning");
  });

  document.getElementById("btn-shadps4-refresh-overview").addEventListener("click", loadShadps4Overview);

  document.getElementById("btn-shadps4-sync-compat").addEventListener("click", async () => {
    setStatus("Syncing shadPS4 compatibility database...");
    const r = await apiPost("/api/databases/shadps4-compat");
    showToast("shadPS4 Compatibility DB", r.error ? r.error : `Synced ${r.count} title compatibility statuses.`);
    const lib = await apiGet("/api/library");
    setLibrary(lib);
  });

  document.getElementById("btn-shadps4-open-appdata").addEventListener("click", async () => {
    const ov = await apiGet("/api/shadps4/overview");
    if (ov.userDir) await apiPost("/api/system/open", { target: ov.userDir });
  });

  document.getElementById("btn-browse-shadps4-gamedir").addEventListener("click", async () => {
    const d = await pickFolder("Select shadPS4 Game Library Folder");
    if (d) document.getElementById("shadps4-cfg-gamedir").value = d;
  });
  document.getElementById("btn-browse-shadps4-core").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select shadPS4 Core Binary / AppImage", "Executable", ["*"], false);
    if (f[0]) document.getElementById("shadps4-cfg-corepath").value = f[0];
  });
  document.getElementById("btn-browse-shadps4-launcher").addEventListener("click", async () => {
    const f = await pickOpenFiles("Select shadPS4 Qt Launcher Binary / AppImage", "Executable", ["*"], false);
    if (f[0]) document.getElementById("shadps4-cfg-launcherpath").value = f[0];
  });

  document.getElementById("btn-save-shadps4-paths").addEventListener("click", async () => {
    const shadps4InstallDirectory = document.getElementById("shadps4-cfg-gamedir").value.trim();
    const core = document.getElementById("shadps4-cfg-corepath").value.trim();
    const launcher = document.getElementById("shadps4-cfg-launcherpath").value.trim();
    const payload = { shadps4InstallDirectory };
    if (core) payload.shadps4ActiveCore = `custom:${core}`;
    if (launcher) payload.shadps4ActiveLauncher = `custom:${launcher}`;
    await apiPost("/api/settings", payload);
    loadShadps4Overview();
    showToast("Saved", "Saved shadPS4 environment paths.");
  });

  async function loadShadps4Games() {
    const games = await apiGet("/api/shadps4/games");
    const tbody = document.getElementById("shadps4-games-body");
    if (!games.length) {
      tbody.innerHTML = `<tr><td colspan="10" style="text-align:center;padding:18px;color:var(--text-disabled);">No installed PS4 games found in the shadPS4 game library folder. Right-click any PKG in your library and select <b>Install to shadPS4 Emulator...</b></td></tr>`;
      return;
    }
    tbody.innerHTML = games.map(g => `<tr>
      <td><b>${escapeHtml(g.title)}</b></td>
      <td><code>${escapeHtml(g.titleId)}</code></td>
      <td>${escapeHtml(g.category)}</td>
      <td>${escapeHtml(g.appVersion)}</td>
      <td>-</td>
      <td>-</td>
      <td>-</td>
      <td><span class="badge-pill">${escapeHtml(g.compatStatus || "Unknown")}</span></td>
      <td>${g.hasEboot ? "eboot.bin ✓" : "Missing eboot"}</td>
      <td>
        <button class="btn-sm btn-primary" data-launch="${escapeHtml(g.titleId)}">Play</button>
        <button class="btn-sm" data-patches="${escapeHtml(g.titleId)}" data-ver="${escapeHtml(g.appVersion)}">Patches</button>
        <button class="btn-sm" data-open-dir="${escapeHtml(g.installedPath)}">Folder</button>
      </td>
    </tr>`).join("");

    tbody.querySelectorAll("[data-launch]").forEach(btn => {
      btn.addEventListener("click", async () => {
        const r = await apiPost("/api/shadps4/launch-game", { titleId: btn.getAttribute("data-launch") });
        showToast("Launch Game", r.message, r.succeeded ? "Info" : "Error");
      });
    });
    tbody.querySelectorAll("[data-patches]").forEach(btn => {
      btn.addEventListener("click", () => {
        document.getElementById("patch-titleid").value = btn.getAttribute("data-patches");
        document.getElementById("patch-version").value = btn.getAttribute("data-ver") || "01.00";
        document.querySelector('[data-stab="stab-patches"]').click();
        document.getElementById("btn-load-patches").click();
      });
    });
    tbody.querySelectorAll("[data-open-dir]").forEach(btn => {
      btn.addEventListener("click", () => apiPost("/api/system/open", { target: btn.getAttribute("data-open-dir") }));
    });
  }

  document.getElementById("btn-shadps4-refresh-games").addEventListener("click", loadShadps4Games);

  document.getElementById("btn-shadps4-fetch-releases").addEventListener("click", async () => {
    const tbody = document.getElementById("shadps4-remote-releases-body");
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:16px;">Fetching latest Linux builds from GitHub...</td></tr>`;
    try {
      const r = await apiGet("/api/shadps4/releases");
      const items = [
        ...(r.coreReleases || []).map(x => ({ ...x, comp: "Core" })),
        ...(r.launcherReleases || []).map(x => ({ ...x, comp: "QtLauncher" }))
      ];
      tbody.innerHTML = items.map(x => `<tr>
        <td><b>${escapeHtml(x.comp)}</b></td>
        <td><code>${escapeHtml(x.tag)}</code></td>
        <td>${x.isPrerelease ? "Nightly / Pre" : "Stable"}</td>
        <td>${escapeHtml(x.assetName)}</td>
        <td>${formatBytes(x.sizeBytes)}</td>
        <td><button class="btn-sm btn-primary" data-dl-comp="${x.comp}" data-dl-tag="${escapeHtml(x.tag)}" data-dl-url="${escapeHtml(x.assetUrl)}" data-dl-size="${x.sizeBytes}">Install</button></td>
      </tr>`).join("");

      tbody.querySelectorAll("[data-dl-url]").forEach(btn => {
        btn.addEventListener("click", async () => {
          btn.disabled = true;
          btn.textContent = "Installing...";
          try {
            await apiPost("/api/shadps4/builds/install", {
              component: btn.getAttribute("data-dl-comp"),
              tag: btn.getAttribute("data-dl-tag"),
              displayName: `${btn.getAttribute("data-dl-comp")} ${btn.getAttribute("data-dl-tag")}`,
              assetUrl: btn.getAttribute("data-dl-url"),
              assetSize: Number(btn.getAttribute("data-dl-size")) || 0,
              setAsActive: true
            });
            showToast("Build Installed", `Installed and activated ${btn.getAttribute("data-dl-tag")}`);
            loadShadps4Overview();
          } catch (err) {
            showToast("Install Error", err.message, "Error");
          } finally {
            btn.disabled = false;
            btn.textContent = "Install";
          }
        });
      });
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="6" style="color:var(--status-err);padding:14px;">${escapeHtml(err.message)}</td></tr>`;
    }
  });

  // Saves
  async function loadShadps4Saves() {
    const saves = await apiGet("/api/shadps4/saves");
    const sBody = document.getElementById("shadps4-saves-body");
    const bBody = document.getElementById("shadps4-backups-body");
    if (!saves.length) {
      sBody.innerHTML = `<tr><td colspan="7" style="text-align:center;padding:16px;color:var(--text-disabled);">No shadPS4 save data slots found.</td></tr>`;
      bBody.innerHTML = `<tr><td colspan="4" style="text-align:center;padding:16px;color:var(--text-disabled);">No save backups.</td></tr>`;
      return;
    }

    const allBackups = [];
    sBody.innerHTML = saves.map(s => {
      for (const bk of (s.backups || [])) allBackups.push(bk);
      return `<tr>
        <td><b>${escapeHtml(s.title)}</b></td>
        <td><code>${escapeHtml(s.titleId)}</code></td>
        <td>${escapeHtml(s.userId)}</td>
        <td>${s.fileCount} slot(s)</td>
        <td>${escapeHtml(s.sizeFormatted)}</td>
        <td>${escapeHtml((s.lastModifiedUtc || "").slice(0, 19))}</td>
        <td>
          <button class="btn-sm btn-primary" data-bk-tid="${escapeHtml(s.titleId)}" data-bk-uid="${escapeHtml(s.userId)}">Backup</button>
          <button class="btn-sm" data-open-save="${escapeHtml(s.slotDirectory)}">Folder</button>
        </td>
      </tr>`;
    }).join("");

    bBody.innerHTML = allBackups.map(b => `<tr>
      <td><b>${escapeHtml(b.titleId)}</b> (${escapeHtml(b.snapshotId)})</td>
      <td>${escapeHtml((b.backedUpAtUtc || "").slice(0, 19))}</td>
      <td>User ${escapeHtml(b.userId)}</td>
      <td><button class="btn-sm btn-primary" data-restore-path="${escapeHtml(b.backupPath)}">Restore</button></td>
    </tr>`).join("") || `<tr><td colspan="4" style="text-align:center;padding:16px;color:var(--text-disabled);">No backups created yet.</td></tr>`;

    sBody.querySelectorAll("[data-bk-tid]").forEach(btn => {
      btn.addEventListener("click", async () => {
        await apiPost("/api/shadps4/saves/backup", {
          titleId: btn.getAttribute("data-bk-tid"),
          userId: btn.getAttribute("data-bk-uid")
        });
        showToast("Save Backed Up", "Created snapshot backup of shadPS4 save data.");
        loadShadps4Saves();
      });
    });
    sBody.querySelectorAll("[data-open-save]").forEach(btn => {
      btn.addEventListener("click", () => apiPost("/api/system/open", { target: btn.getAttribute("data-open-save") }));
    });
    bBody.querySelectorAll("[data-restore-path]").forEach(btn => {
      btn.addEventListener("click", async () => {
        await apiPost("/api/shadps4/saves/restore", { zipPath: btn.getAttribute("data-restore-path") });
        showToast("Save Restored", "Restored shadPS4 save backup.");
        loadShadps4Saves();
      });
    });
  }

  document.getElementById("btn-shadps4-refresh-saves").addEventListener("click", loadShadps4Saves);
  document.getElementById("btn-shadps4-restore-save").addEventListener("click", async () => {
    const dir = await pickFolder("Select Save Snapshot Folder to Restore");
    if (!dir) return;
    await apiPost("/api/shadps4/saves/restore", { zipPath: dir });
    showToast("Save Restored", "Restored shadPS4 save backup.");
    loadShadps4Saves();
  });

  // Patches & Cheats
  document.getElementById("btn-load-patches").addEventListener("click", async () => {
    const titleId = document.getElementById("patch-titleid").value.trim();
    const appVersion = document.getElementById("patch-version").value.trim() || "01.00";
    if (!titleId) return;
    const r = await apiGet(`/api/shadps4/patches?titleId=${encodeURIComponent(titleId)}&appVersion=${encodeURIComponent(appVersion)}`);
    const pBody = document.getElementById("shadps4-patches-body");
    const cBody = document.getElementById("shadps4-cheats-body");

    pBody.innerHTML = (r.patches || []).map(p => `<tr>
      <td><input type="checkbox" class="patch-toggle-chk" data-file="${escapeHtml(p.sourceFilePath)}" data-name="${escapeHtml(p.name)}" ${p.isEnabled ? "checked" : ""} /></td>
      <td><b>${escapeHtml(p.name)}</b></td>
      <td>${escapeHtml(p.author || "-")}</td>
      <td>${escapeHtml(p.appVersion || "All")}</td>
      <td>${escapeHtml(p.repository || "Local")}</td>
    </tr>`).join("") || `<tr><td colspan="5" style="text-align:center;padding:16px;color:var(--text-disabled);">No patches found for ${escapeHtml(titleId)}. Click <b>Download Official Patches Repo</b>.</td></tr>`;

    cBody.innerHTML = (r.cheats || []).map(c => `<tr>
      <td><b>${escapeHtml(c.name)}</b></td>
      <td>${escapeHtml(c.version || "-")}</td>
      <td>${(c.mods || []).length} offset(s)</td>
      <td>${escapeHtml(c.filePath ? c.filePath.split("/").pop() : "Local")}</td>
    </tr>`).join("") || `<tr><td colspan="4" style="text-align:center;padding:16px;color:var(--text-disabled);">No cheats found for ${escapeHtml(titleId)}.</td></tr>`;
  });

  document.getElementById("btn-dl-official-patches").addEventListener("click", async () => {
    const titleId = document.getElementById("patch-titleid").value.trim() || "CUSA00000";
    const r = await apiPost("/api/shadps4/patches/download", { kind: "patches", titleId });
    showToast("Patches Downloaded", r.message || "Downloaded official shadPS4 patches/cheats.");
    if (document.getElementById("patch-titleid").value.trim()) document.getElementById("btn-load-patches").click();
  });

  document.getElementById("btn-dl-official-cheats").addEventListener("click", async () => {
    const titleId = document.getElementById("patch-titleid").value.trim() || "CUSA00000";
    const r = await apiPost("/api/shadps4/patches/download", { kind: "cheats", titleId });
    showToast("Cheats Downloaded", r.message || "Downloaded official shadPS4 cheats.");
    if (document.getElementById("patch-titleid").value.trim()) document.getElementById("btn-load-patches").click();
  });

  document.getElementById("btn-save-patch-states").addEventListener("click", async () => {
    const changes = Array.from(document.querySelectorAll(".patch-toggle-chk")).map(chk => ({
      sourceFilePath: chk.getAttribute("data-file"),
      patchName: chk.getAttribute("data-name"),
      isEnabled: chk.checked
    }));
    await apiPost("/api/shadps4/patches/save", { changes });
    showToast("Patches Saved", "Saved enabled patch states to XML.");
  });

  // Feedback & Compatibility Reports
  async function loadFeedbackEntries() {
    const list = await apiGet("/api/feedback");
    const tbody = document.getElementById("fb-entries-body");
    tbody.innerHTML = list.map(e => `<tr>
      <td><b>${escapeHtml(e.title)}</b></td>
      <td><code>${escapeHtml(e.titleId)}</code></td>
      <td>${escapeHtml(e.gameVersion)}</td>
      <td><span class="badge-pill">${escapeHtml(e.overallRating)}</span></td>
      <td>${escapeHtml((e.timestampUtc || "").slice(0, 10))}</td>
      <td>
        <button class="btn-sm" data-fb-preview='${escapeHtml(JSON.stringify(e))}'>Report</button>
        <button class="btn-sm btn-danger" data-fb-del="${escapeHtml(e.resultId || e.timestampUtc)}">✕</button>
      </td>
    </tr>`).join("") || `<tr><td colspan="6" style="text-align:center;padding:14px;color:var(--text-disabled);">No compatibility reports saved yet.</td></tr>`;

    tbody.querySelectorAll("[data-fb-preview]").forEach(btn => {
      btn.addEventListener("click", async () => {
        const entry = JSON.parse(btn.getAttribute("data-fb-preview"));
        const rep = await apiPost("/api/feedback/report", entry);
        document.getElementById("fb-report-preview").textContent = rep.markdownBody;
        state.currentReportUrl = rep.trackerUrl;
      });
    });
    tbody.querySelectorAll("[data-fb-del]").forEach(btn => {
      btn.addEventListener("click", async () => {
        await apiPost("/api/feedback/delete", { id: btn.getAttribute("data-fb-del") });
        loadFeedbackEntries();
      });
    });
  }

  function buildCurrentFeedbackFormEntry() {
    return {
      titleId: document.getElementById("fb-titleid").value.trim(),
      title: document.getElementById("fb-title").value.trim(),
      gameVersion: document.getElementById("fb-version").value.trim() || "01.00",
      overallRating: document.getElementById("fb-status").value,
      notes: document.getElementById("fb-notes").value.trim(),
      errorSummary: document.getElementById("fb-error").value.trim()
    };
  }

  document.getElementById("btn-fb-save").addEventListener("click", async () => {
    const entry = buildCurrentFeedbackFormEntry();
    if (!entry.titleId) return;
    const saved = await apiPost("/api/feedback/save", entry);
    const rep = await apiPost("/api/feedback/report", saved);
    document.getElementById("fb-report-preview").textContent = rep.markdownBody;
    state.currentReportUrl = rep.trackerUrl;
    loadFeedbackEntries();
    showToast("Saved", "Saved game compatibility feedback.");
  });

  document.getElementById("btn-fb-generate-report").addEventListener("click", async () => {
    const entry = buildCurrentFeedbackFormEntry();
    const rep = await apiPost("/api/feedback/report", entry);
    document.getElementById("fb-report-preview").textContent = rep.markdownBody;
    state.currentReportUrl = rep.trackerUrl;
  });

  document.getElementById("btn-fb-copy-report").addEventListener("click", async () => {
    const txt = document.getElementById("fb-report-preview").textContent;
    if (!txt) return;
    await apiPost("/api/system/copy", { text: txt });
    showToast("Copied", "Compatibility report Markdown copied to clipboard.");
  });

  document.getElementById("btn-fb-open-github").addEventListener("click", async () => {
    if (state.currentReportUrl) await apiPost("/api/system/open", { target: state.currentReportUrl });
  });

  // ── Settings Modal ─────────────────────────────────────────────────────────
  document.getElementById("btn-open-settings").addEventListener("click", () => {
    populateSettingsModal();
    openModal("modal-settings");
  });

  function populateSettingsModal() {
    const b = state.bootstrap;
    if (!b) return;
    const s = b.settings;

    const sel = document.getElementById("cfg-theme");
    sel.innerHTML = (b.themes || []).map(t => `<option value="${escapeHtml(t.name)}">${escapeHtml(t.category)} — ${escapeHtml(t.name)}</option>`).join("");
    sel.value = s.themeName;

    document.getElementById("cfg-color-label").checked = !!s.pkgColorLabel;
    document.getElementById("cfg-col-game").value = s.gamePkgBackColor || "#2e7d32";
    document.getElementById("cfg-col-patch").value = s.patchPkgBackColor || "#1565c0";
    document.getElementById("cfg-col-addon").value = s.addonPkgBackColor || "#8e24aa";
    document.getElementById("cfg-col-app").value = s.appPkgBackColor || "#e65100";

    document.getElementById("cfg-play-bgm").checked = !!s.playBgm;
    document.getElementById("cfg-recursive-scan").checked = !!s.scanRecursive;
    document.getElementById("cfg-auto-sort").checked = !!s.autoSortRow;
    document.getElementById("cfg-ps5bc-check").checked = !!s.psvrNeoPs5BcCheck;
    document.getElementById("cfg-shadps4-check").checked = !!s.shadps4Check;
    document.getElementById("cfg-shell-ext").checked = !!s.shellIntegrationInstalled;
    document.getElementById("cfg-patch-dl-dir").value = s.officialUpdateDownloadDirectory || "";

    renderSettingsDirsList(s.pkgDirectories || []);
  }

  function renderSettingsDirsList(dirs) {
    const wrap = document.getElementById("cfg-dirs-list");
    wrap.innerHTML = dirs.map((d, i) => `<div style="display:flex;justify-content:space-between;align-items:center;background:var(--bg-medium);padding:4px 8px;border-radius:3px;">
      <span>${escapeHtml(d)}</span>
      <button class="btn-sm btn-danger" data-rm-dir="${i}">Remove</button>
    </div>`).join("") || `<span style="color:var(--text-disabled);">No folders configured yet.</span>`;

    wrap.querySelectorAll("[data-rm-dir]").forEach(btn => {
      btn.addEventListener("click", () => {
        const idx = Number(btn.getAttribute("data-rm-dir"));
        state.bootstrap.settings.pkgDirectories.splice(idx, 1);
        renderSettingsDirsList(state.bootstrap.settings.pkgDirectories);
      });
    });
  }

  document.getElementById("btn-cfg-browse-dir").addEventListener("click", async () => {
    const d = await pickFolder("Select PKG Directory");
    if (d) document.getElementById("cfg-new-scan-dir").value = d;
  });

  document.getElementById("btn-cfg-add-dir").addEventListener("click", () => {
    const v = document.getElementById("cfg-new-scan-dir").value.trim();
    if (!v) return;
    const dirs = state.bootstrap.settings.pkgDirectories || [];
    if (!dirs.includes(v)) dirs.push(v);
    state.bootstrap.settings.pkgDirectories = dirs;
    document.getElementById("cfg-new-scan-dir").value = "";
    renderSettingsDirsList(dirs);
  });

  document.getElementById("btn-cfg-browse-patch-dir").addEventListener("click", async () => {
    const d = await pickFolder("Select Default Patch Download Folder");
    if (d) document.getElementById("cfg-patch-dl-dir").value = d;
  });

  document.getElementById("cfg-theme").addEventListener("change", async (ev) => {
    const b = await apiPost("/api/settings", { themeName: ev.target.value });
    state.bootstrap = b;
    applyTheme(b.activeTheme, b.settings);
  });

  document.getElementById("btn-save-settings").addEventListener("click", async () => {
    const b = await apiPost("/api/settings", {
      themeName: document.getElementById("cfg-theme").value,
      pkgColorLabel: document.getElementById("cfg-color-label").checked,
      playBgm: document.getElementById("cfg-play-bgm").checked,
      scanRecursive: document.getElementById("cfg-recursive-scan").checked,
      autoSortRow: document.getElementById("cfg-auto-sort").checked,
      psvrNeoPs5BcCheck: document.getElementById("cfg-ps5bc-check").checked,
      shadps4Check: document.getElementById("cfg-shadps4-check").checked,
      shellIntegrationInstalled: document.getElementById("cfg-shell-ext").checked,
      officialUpdateDownloadDirectory: document.getElementById("cfg-patch-dl-dir").value.trim(),
      pkgDirectories: state.bootstrap.settings.pkgDirectories || []
    });
    state.bootstrap = b;
    applyTheme(b.activeTheme, b.settings);
    applyFiltersAndRender();
    closeModal("modal-settings");
    showToast("Settings Saved", "Configuration and Linux integration updated.");
  });

  document.getElementById("btn-sync-ps5bc").addEventListener("click", async () => {
    const r = await apiPost("/api/databases/ps5bc");
    showToast("PS5 BC Database", r.error ? r.error : `Downloaded ${r.count} PS5 BC entries.`);
    setLibrary(await apiGet("/api/library"));
  });

  document.getElementById("btn-sync-shadps4").addEventListener("click", async () => {
    const r = await apiPost("/api/databases/shadps4-compat");
    showToast("shadPS4 Database", r.error ? r.error : `Downloaded ${r.count} shadPS4 compatibility entries.`);
    setLibrary(await apiGet("/api/library"));
  });

  // ── About & Logs ───────────────────────────────────────────────────────────
  document.getElementById("btn-open-about").addEventListener("click", () => openModal("modal-about"));
  document.getElementById("btn-about-github").addEventListener("click", () => apiPost("/api/system/open", { target: "https://github.com/pearlxcore/PS4PKGTool" }));
  document.getElementById("btn-about-appdata").addEventListener("click", () => {
    if (state.bootstrap?.userDataDir) apiPost("/api/system/open", { target: state.bootstrap.userDataDir });
  });
  document.getElementById("btn-open-appdata").addEventListener("click", () => {
    if (state.bootstrap?.userDataDir) apiPost("/api/system/open", { target: state.bootstrap.userDataDir });
  });

  async function refreshLogs() {
    const r = await apiGet("/api/logs");
    document.getElementById("app-log-view").textContent = r.log || "Log is empty.";
  }

  document.getElementById("btn-refresh-log").addEventListener("click", refreshLogs);
  document.getElementById("btn-clear-log").addEventListener("click", async () => {
    await apiPost("/api/logs/clear");
    refreshLogs();
  });

  // ── Task Queue & Background Polling ────────────────────────────────────────
  async function refreshTasks() {
    try {
      const snap = await apiGet("/api/tasks");
      document.getElementById("chk-tasks-autostart").checked = !!snap.autoStart;
      const tasks = snap.tasks || [];
      const activeCount = tasks.filter(t => t.status === "Running" || t.status === "Queued").length;
      document.getElementById("tab-tasks-badge").textContent = activeCount;

      const tbody = document.getElementById("tasks-table-body");
      if (!tasks.length) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;padding:18px;color:var(--text-disabled);">No queued or active tasks.</td></tr>`;
        return;
      }

      tbody.innerHTML = tasks.map(t => `<tr>
        <td><b>${escapeHtml(t.type)}</b></td>
        <td title="${escapeHtml(t.sourcePath)}">${escapeHtml(t.displayName)}</td>
        <td title="${escapeHtml(t.outputPath)}">${escapeHtml(t.outputPath)}</td>
        <td><span class="badge-pill">${escapeHtml(t.status)}</span></td>
        <td>
          <div class="progress-track">
            <div class="progress-fill" style="width:${t.overallProgress || 0}%"></div>
            <div class="progress-label">${t.overallProgress || 0}%</div>
          </div>
        </td>
        <td style="white-space:normal;">${escapeHtml(t.message || t.stage || "")}</td>
        <td>
          ${t.status === "Running" || t.status === "Queued" ? `<button class="btn-sm btn-danger" data-t-cancel="${t.id}">Cancel</button>` : ""}
          ${t.status === "Failed" || t.status === "Cancelled" ? `<button class="btn-sm" data-t-retry="${t.id}">Retry</button>` : ""}
          ${t.status !== "Running" ? `<button class="btn-sm" data-t-rm="${t.id}">Remove</button>` : ""}
        </td>
      </tr>`).join("");

      tbody.querySelectorAll("[data-t-cancel]").forEach(b => b.addEventListener("click", async () => {
        await apiPost("/api/tasks/cancel", { id: b.getAttribute("data-t-cancel") });
        refreshTasks();
      }));
      tbody.querySelectorAll("[data-t-retry]").forEach(b => b.addEventListener("click", async () => {
        await apiPost("/api/tasks/retry", { id: b.getAttribute("data-t-retry") });
        refreshTasks();
      }));
      tbody.querySelectorAll("[data-t-rm]").forEach(b => b.addEventListener("click", async () => {
        await apiPost("/api/tasks/remove", { id: b.getAttribute("data-t-rm") });
        refreshTasks();
      }));
    } catch {}
  }

  document.getElementById("chk-tasks-autostart").addEventListener("change", async (ev) => {
    await apiPost("/api/tasks/auto-start", { autoStart: ev.target.checked });
  });
  document.getElementById("btn-tasks-start-next").addEventListener("click", async () => {
    await apiPost("/api/tasks/start-next");
    refreshTasks();
  });
  document.getElementById("btn-tasks-clear-done").addEventListener("click", async () => {
    await apiPost("/api/tasks/clear-completed");
    refreshTasks();
  });

  async function pollBackgroundState() {
    try {
      const notes = await apiGet("/api/notifications");
      for (const n of notes) {
        if (n.kind === "message") {
          showToast(n.title || "Notification", n.message || "", n.type || "Info");
        } else if (n.kind === "shadps4_exit") {
          showToast("shadPS4 Session Ended", n.dialogMessage || "Emulator process exited.", n.dialogType || "Warning");
          if (n.feedback) {
            document.getElementById("fb-titleid").value = n.feedback.titleId || "";
            document.getElementById("fb-title").value = n.feedback.title || "";
            document.getElementById("fb-version").value = n.feedback.gameVersion || "01.00";
            document.getElementById("fb-error").value = n.feedback.errorPrefill || "";
          }
        }
      }

      await refreshTasks();

      if (document.getElementById("modal-updates").classList.contains("open")) {
        const u = await apiGet("/api/updates/status");
        document.getElementById("upd-progress-fill").style.width = `${u.percent || 0}%`;
        document.getElementById("upd-progress-label").textContent = `${u.statusMessage} (${u.percent || 0}%)`;
      }
    } catch {}
  }

  // ── Startup Bootstrap ──────────────────────────────────────────────────────
  async function initApp() {
    try {
      const b = await apiGet("/api/bootstrap");
      state.bootstrap = b;
      applyTheme(b.activeTheme, b.settings);

      document.getElementById("rpi-local-ip").value = b.settings.localServerIp || "";
      document.getElementById("rpi-ps4-ip").value = b.settings.ps4Ip || "";
      document.getElementById("upd-download-dir").value = b.settings.officialUpdateDownloadDirectory || "";

      if (b.directViewerPkgPath) {
        const lib = await apiPost("/api/library/add-files", { files: [b.directViewerPkgPath], append: false });
        setLibrary(lib);
        await selectPkgRow(b.directViewerPkgPath);
      } else if (b.manifestExists) {
        openModal("modal-startup");
      } else if (b.hasConfiguredDirs) {
        const lib = await apiPost("/api/library/scan", { append: false });
        setLibrary(lib);
      }

      setInterval(pollBackgroundState, 1500);
    } catch (err) {
      showToast("Startup Error", err.message, "Error");
    }
  }

  document.getElementById("btn-startup-manifest").addEventListener("click", async () => {
    closeModal("modal-startup");
    const lib = await apiPost("/api/library/load-manifest");
    setLibrary(lib);
  });

  document.getElementById("btn-startup-scan").addEventListener("click", async () => {
    closeModal("modal-startup");
    const lib = await apiPost("/api/library/scan", { append: false });
    setLibrary(lib);
  });

  document.getElementById("btn-startup-empty").addEventListener("click", () => {
    closeModal("modal-startup");
  });

  window.addEventListener("DOMContentLoaded", initApp);
})();
