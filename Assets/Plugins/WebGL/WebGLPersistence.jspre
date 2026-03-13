(function () {
  var globalScope = typeof globalThis !== "undefined" ? globalThis : (typeof self !== "undefined" ? self : this);
  if (globalScope.GFUnityPersistence) {
    if (typeof Module !== "undefined" && Module && globalScope.GFUnityPersistence.installModuleHook) {
      globalScope.GFUnityPersistence.installModuleHook(Module);
    }
    return;
  }

  var MEM_ROOT = "/idbfs";
  var STORE_DIR_NAME = "__GF_IDBFS__";
  var MANIFEST_NAME = "__manifest__.json";

  var state = {
    ready: false,
    initializing: false,
    syncing: false,
    pendingFull: false,
    pendingPath: null,
    snapshot: {}
  };

  function setStatus(ready, initializing) {
    state.ready = !!ready;
    state.initializing = !!initializing;
    globalScope.GF_FS_SYNC_READY = state.ready ? 1 : 0;
    globalScope.GF_FS_SYNC_INITIALIZING = state.initializing ? 1 : 0;
  }

  function logError(prefix, error) {
    if (typeof console !== "undefined" && console.error) {
      console.error(prefix, error);
    }
  }

  function isWeChat() {
    return typeof wx !== "undefined"
      && !!wx.getFileSystemManager
      && !!wx.env
      && typeof wx.env.USER_DATA_PATH === "string";
  }

  function normalizeMemPath(path) {
    path = (path || "").replace(/\\/g, "/");
    if (!path) {
      return "";
    }

    if (path.charAt(0) !== "/") {
      path = "/" + path;
    }

    path = path.replace(/\/+/g, "/");
    while (path.length > 1 && path.charAt(path.length - 1) === "/") {
      path = path.slice(0, -1);
    }

    return path;
  }

  function normalizeWxPath(path) {
    path = (path || "").replace(/\\/g, "/");
    while (path.length > 1 && path.charAt(path.length - 1) === "/") {
      path = path.slice(0, -1);
    }

    return path;
  }

  function getFsManager() {
    return wx.getFileSystemManager();
  }

  function getWxRoot() {
    return normalizeWxPath(wx.env.USER_DATA_PATH + "/" + STORE_DIR_NAME);
  }

  function getManifestPath() {
    return normalizeWxPath(getWxRoot() + "/" + MANIFEST_NAME);
  }

  function isPersistentMemPath(path) {
    path = normalizeMemPath(path);
    return path === MEM_ROOT || path.indexOf(MEM_ROOT + "/") === 0;
  }

  function isSameOrChild(path, parent) {
    path = normalizeMemPath(path);
    parent = normalizeMemPath(parent);
    return path === parent || path.indexOf(parent + "/") === 0;
  }

  function getParentMemPath(path) {
    path = normalizeMemPath(path);
    if (!path || path === "/") {
      return "";
    }

    var index = path.lastIndexOf("/");
    if (index <= 0) {
      return "/";
    }

    return path.substring(0, index);
  }

  function getWxPathFromMem(path) {
    path = normalizeMemPath(path);
    if (!isPersistentMemPath(path)) {
      return null;
    }

    return normalizeWxPath(getWxRoot() + path.substring(MEM_ROOT.length));
  }

  function ensureMemDir(path) {
    if (typeof FS === "undefined") {
      return;
    }

    path = normalizeMemPath(path);
    if (!path || path === "/") {
      return;
    }

    var parts = path.split("/");
    var current = "";
    for (var i = 1; i < parts.length; i++) {
      if (!parts[i]) {
        continue;
      }

      current += "/" + parts[i];
      try {
        var stat = FS.stat(current);
        if (!FS.isDir(stat.mode)) {
          throw new Error(current + " is not a directory.");
        }
      } catch (error) {
        FS.mkdir(current);
      }
    }
  }

  function ensureWxDir(path) {
    if (!isWeChat()) {
      return;
    }

    try {
      getFsManager().mkdirSync(normalizeWxPath(path), true);
    } catch (error) {
    }
  }

  function removeWxPath(path) {
    if (!isWeChat()) {
      return;
    }

    var fs = getFsManager();
    path = normalizeWxPath(path);
    try {
      fs.unlinkSync(path);
      return;
    } catch (error) {
    }

    try {
      fs.rmdirSync(path, true);
    } catch (error2) {
    }
  }

  function toUint8Array(data) {
    if (data instanceof Uint8Array) {
      return data;
    }

    if (data instanceof ArrayBuffer) {
      return new Uint8Array(data);
    }

    if (data && data.buffer instanceof ArrayBuffer) {
      return new Uint8Array(data.buffer, data.byteOffset || 0, data.byteLength || data.length || 0);
    }

    return new Uint8Array(0);
  }

  function getStatTime(stat) {
    if (!stat) {
      return 0;
    }

    if (stat.mtime && typeof stat.mtime.getTime === "function") {
      return stat.mtime.getTime();
    }

    if (typeof stat.mtime === "number") {
      return stat.mtime;
    }

    if (typeof stat.timestamp === "number") {
      return stat.timestamp;
    }

    return 0;
  }

  function createEntryFromStat(path, stat) {
    var isDir = FS.isDir(stat.mode);
    return {
      path: normalizeMemPath(path),
      type: isDir ? "dir" : "file",
      size: isDir ? 0 : (stat.size || 0),
      mtimeMs: getStatTime(stat)
    };
  }

  function collectMemEntries(rootPath) {
    var entries = {};
    if (typeof FS === "undefined") {
      return entries;
    }

    rootPath = normalizeMemPath(rootPath);
    if (!rootPath) {
      return entries;
    }

    var analyze = FS.analyzePath(rootPath);
    if (!analyze.exists) {
      return entries;
    }

    function walk(currentPath) {
      var stat = FS.stat(currentPath);
      var entry = createEntryFromStat(currentPath, stat);
      entries[currentPath] = entry;
      if (entry.type !== "dir") {
        return;
      }

      var children = FS.readdir(currentPath);
      for (var i = 0; i < children.length; i++) {
        var child = children[i];
        if (child === "." || child === "..") {
          continue;
        }

        var childPath = currentPath === "/" ? "/" + child : currentPath + "/" + child;
        walk(childPath);
      }
    }

    walk(rootPath);
    return entries;
  }

  function readWxManifest() {
    if (!isWeChat()) {
      return null;
    }

    try {
      var text = getFsManager().readFileSync(getManifestPath(), "utf8");
      if (!text) {
        return null;
      }

      var manifest = JSON.parse(text);
      return manifest && Array.isArray(manifest.entries) ? manifest : null;
    } catch (error) {
      return null;
    }
  }

  function writeWxManifest(snapshot) {
    if (!isWeChat()) {
      return;
    }

    var entries = [];
    var paths = Object.keys(snapshot).sort();
    for (var i = 0; i < paths.length; i++) {
      var entry = snapshot[paths[i]];
      if (!entry || !isPersistentMemPath(entry.path)) {
        continue;
      }

      entries.push({
        path: entry.path,
        type: entry.type,
        size: entry.size || 0,
        mtimeMs: entry.mtimeMs || 0
      });
    }

    ensureWxDir(getWxRoot());
    getFsManager().writeFileSync(
      getManifestPath(),
      JSON.stringify({ version: 1, entries: entries }),
      "utf8"
    );
  }

  function restoreFromManifest(manifest) {
    ensureWxDir(getWxRoot());
    if (!manifest || !Array.isArray(manifest.entries)) {
      state.snapshot = collectMemEntries(MEM_ROOT);
      return;
    }

    var entries = manifest.entries.slice().sort(function (left, right) {
      if (left.type !== right.type) {
        return left.type === "dir" ? -1 : 1;
      }

      return left.path.localeCompare(right.path);
    });

    for (var i = 0; i < entries.length; i++) {
      var entry = entries[i];
      var memPath = normalizeMemPath(entry.path);
      if (!isPersistentMemPath(memPath) || memPath === MEM_ROOT) {
        continue;
      }

      if (entry.type === "dir") {
        ensureMemDir(memPath);
        continue;
      }

      try {
        ensureMemDir(getParentMemPath(memPath));
        var bytes = toUint8Array(getFsManager().readFileSync(getWxPathFromMem(memPath)));
        FS.writeFile(memPath, bytes);
      } catch (error) {
        logError("[GFUnityPersistence] restore file failed:", error);
      }
    }

    state.snapshot = collectMemEntries(MEM_ROOT);
  }

  function mountPersistentFileSystem(module) {
    if (typeof FS === "undefined") {
      setStatus(true, false);
      return;
    }

    setStatus(false, true);

    try {
      if (!FS.analyzePath(MEM_ROOT).exists) {
        FS.mkdir(MEM_ROOT);
      }
    } catch (error) {
    }

    if (isWeChat()) {
      module.addRunDependency("JS_FileSystem_Mount");
      try {
        module.__unityIdbfsMount = FS.mount(MEMFS, {}, MEM_ROOT);
        restoreFromManifest(readWxManifest());
      } catch (error) {
        logError("[GFUnityPersistence] mount failed:", error);
        state.snapshot = collectMemEntries(MEM_ROOT);
      }

      setStatus(true, false);
      module.removeRunDependency("JS_FileSystem_Mount");
      return;
    }

    module.__unityIdbfsMount = FS.mount(IDBFS, { autoPersist: !!module.autoSyncPersistentDataPath }, MEM_ROOT);
    module.addRunDependency("JS_FileSystem_Mount");
    FS.syncfs(true, function (error) {
      if (error && typeof console !== "undefined" && console.log) {
        console.log("IndexedDB is not available. Data will not persist in cache and PlayerPrefs will not be saved.");
      }

      state.snapshot = collectMemEntries(MEM_ROOT);
      setStatus(true, false);
      module.removeRunDependency("JS_FileSystem_Mount");
    });
  }

  function ensureParentSnapshotDirs(path, snapshot) {
    var dirs = [];
    var current = getParentMemPath(path);
    while (current && current !== "/" && isPersistentMemPath(current)) {
      if (snapshot[current]) {
        break;
      }

      dirs.unshift(current);
      if (current === MEM_ROOT) {
        break;
      }

      current = getParentMemPath(current);
    }

    if (!snapshot[MEM_ROOT]) {
      snapshot[MEM_ROOT] = { path: MEM_ROOT, type: "dir", size: 0, mtimeMs: 0 };
    }

    for (var i = 0; i < dirs.length; i++) {
      var dirPath = dirs[i];
      ensureWxDir(getWxPathFromMem(dirPath));
      snapshot[dirPath] = { path: dirPath, type: "dir", size: 0, mtimeMs: 0 };
    }
  }

  function entryEquals(left, right) {
    return !!left
      && !!right
      && left.type === right.type
      && left.size === right.size
      && left.mtimeMs === right.mtimeMs;
  }

  function writeMemFileToWx(memPath, snapshot) {
    var wxPath = getWxPathFromMem(memPath);
    if (!wxPath) {
      return;
    }

    var separator = wxPath.lastIndexOf("/");
    if (separator > 0) {
      ensureWxDir(wxPath.substring(0, separator));
    }

    var bytes = FS.readFile(memPath, { encoding: "binary" });
    var view = toUint8Array(bytes);
    var buffer = view.byteOffset === 0 && view.byteLength === view.buffer.byteLength
      ? view.buffer
      : view.buffer.slice(view.byteOffset, view.byteOffset + view.byteLength);

    getFsManager().writeFileSync(wxPath, buffer);
    snapshot[memPath] = createEntryFromStat(memPath, FS.stat(memPath));
  }

  function applySnapshotDiff(currentEntries, scopePath) {
    var snapshot = state.snapshot;
    if (!snapshot[MEM_ROOT]) {
      snapshot[MEM_ROOT] = { path: MEM_ROOT, type: "dir", size: 0, mtimeMs: 0 };
    }

    var currentPaths = Object.keys(currentEntries).sort(function (left, right) {
      var leftDir = currentEntries[left].type === "dir";
      var rightDir = currentEntries[right].type === "dir";
      if (leftDir !== rightDir) {
        return leftDir ? -1 : 1;
      }

      return left.localeCompare(right);
    });

    for (var i = 0; i < currentPaths.length; i++) {
      var currentPath = currentPaths[i];
      var currentEntry = currentEntries[currentPath];
      if (currentEntry.type === "dir") {
        ensureParentSnapshotDirs(currentPath, snapshot);
        ensureWxDir(getWxPathFromMem(currentPath));
        snapshot[currentPath] = currentEntry;
        continue;
      }

      ensureParentSnapshotDirs(currentPath, snapshot);
      if (!entryEquals(snapshot[currentPath], currentEntry)) {
        writeMemFileToWx(currentPath, snapshot);
      } else {
        snapshot[currentPath] = currentEntry;
      }
    }

    var stalePaths = [];
    var snapshotPaths = Object.keys(snapshot);
    for (var j = 0; j < snapshotPaths.length; j++) {
      var snapshotPath = snapshotPaths[j];
      if (snapshotPath === MEM_ROOT) {
        continue;
      }

      if (scopePath && !isSameOrChild(snapshotPath, scopePath)) {
        continue;
      }

      if (!currentEntries[snapshotPath]) {
        stalePaths.push(snapshotPath);
      }
    }

    stalePaths.sort(function (left, right) {
      return right.length - left.length;
    });

    for (var k = 0; k < stalePaths.length; k++) {
      var stalePath = stalePaths[k];
      removeWxPath(getWxPathFromMem(stalePath));
      delete snapshot[stalePath];
    }
  }

  function syncWeChat(syncPath) {
    if (!isWeChat() || typeof FS === "undefined") {
      setStatus(true, false);
      return;
    }

    var scopePath = syncPath ? normalizeMemPath(syncPath) : "";
    if (scopePath && !isPersistentMemPath(scopePath)) {
      scopePath = "";
    }

    var currentEntries = scopePath ? collectMemEntries(scopePath) : collectMemEntries(MEM_ROOT);
    applySnapshotDiff(currentEntries, scopePath);
    writeWxManifest(state.snapshot);
    setStatus(true, false);
  }

  function initialize() {
    if (state.ready || state.initializing) {
      return;
    }

    if (isWeChat()) {
      setStatus(false, true);
      try {
        restoreFromManifest(readWxManifest());
      } catch (error) {
        logError("[GFUnityPersistence] initialize failed:", error);
        state.snapshot = collectMemEntries(MEM_ROOT);
      }

      setStatus(true, false);
      return;
    }

    if (typeof FS !== "undefined" && FS.syncfs) {
      setStatus(false, true);
      FS.syncfs(true, function (error) {
        if (error) {
          logError("[GFUnityPersistence] syncfs populate failed:", error);
        }

        state.snapshot = collectMemEntries(MEM_ROOT);
        setStatus(true, false);
      });
      return;
    }

    setStatus(true, false);
  }

  function sync(syncPath) {
    if (!isWeChat()) {
      if (typeof FS !== "undefined" && FS.syncfs) {
        FS.syncfs(false, function (error) {
          if (error) {
            logError("[GFUnityPersistence] syncfs failed:", error);
          }
        });
      }

      return;
    }

    if (state.initializing) {
      state.pendingFull = true;
      return;
    }

    if (state.syncing) {
      if (!syncPath) {
        state.pendingFull = true;
        state.pendingPath = null;
      } else if (!state.pendingFull) {
        state.pendingPath = syncPath;
      }

      return;
    }

    state.syncing = true;
    try {
      var nextFull = !syncPath;
      var nextPath = syncPath;
      do {
        state.pendingFull = false;
        state.pendingPath = null;
        syncWeChat(nextFull ? null : nextPath);
        nextFull = state.pendingFull;
        nextPath = state.pendingPath;
      } while (nextFull || nextPath);
    } catch (error) {
      logError("[GFUnityPersistence] sync failed:", error);
    } finally {
      state.syncing = false;
    }
  }

  var api = {
    installModuleHook: function (module) {
      if (!module) {
        return;
      }

      module.unityFileSystemInit = function () {
        mountPersistentFileSystem(module);
      };
    },
    initialize: initialize,
    sync: sync,
    isReady: function () {
      return state.ready ? 1 : 0;
    }
  };

  globalScope.GFUnityPersistence = api;
  setStatus(false, false);

  if (typeof Module !== "undefined" && Module) {
    api.installModuleHook(Module);
  }
})();
