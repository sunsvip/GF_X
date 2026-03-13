mergeInto(LibraryManager.library, {
  GF_InitFsSync: function () {
    try {
      var scope = typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this);
      if (scope.GFUnityPersistence && scope.GFUnityPersistence.initialize) {
        scope.GFUnityPersistence.initialize();
        return;
      }

      if (typeof scope.GF_FS_SYNC_READY === 'undefined') {
        scope.GF_FS_SYNC_READY = 0;
      }

      if (scope.GF_FS_SYNC_INITIALIZING) {
        return;
      }

      if (typeof FS !== 'undefined' && FS.syncfs) {
        scope.GF_FS_SYNC_INITIALIZING = 1;
        scope.GF_FS_SYNC_READY = 0;
        FS.syncfs(true, function (err) {
          if (err) {
            console.error('[GF_InitFsSync] syncfs populate failed:', err);
          }

          scope.GF_FS_SYNC_READY = 1;
          scope.GF_FS_SYNC_INITIALIZING = 0;
        });
      } else {
        scope.GF_FS_SYNC_READY = 1;
      }
    } catch (e) {
      var scope = typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this);
      console.error('[GF_InitFsSync] exception:', e);
      scope.GF_FS_SYNC_READY = 1;
      scope.GF_FS_SYNC_INITIALIZING = 0;
    }
  },
  GF_IsFsSyncReady: function () {
    try {
      var scope = typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this);
      if (scope.GFUnityPersistence && scope.GFUnityPersistence.isReady) {
        return scope.GFUnityPersistence.isReady();
      }

      return scope.GF_FS_SYNC_READY ? 1 : 0;
    } catch (e) {
      return 1;
    }
  },
  GF_SyncFs: function (path) {
    try {
      var scope = typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this);
      if (scope.GFUnityPersistence && scope.GFUnityPersistence.sync) {
        scope.GFUnityPersistence.sync(path ? UTF8ToString(path) : null);
        return;
      }

      if (typeof FS !== 'undefined' && FS.syncfs) {
        FS.syncfs(false, function (err) {
          if (err) {
            console.error('[GF_SyncFs] syncfs failed:', err);
          }
        });
      }
    } catch (e) {
      console.error('[GF_SyncFs] exception:', e);
    }
  }
});
