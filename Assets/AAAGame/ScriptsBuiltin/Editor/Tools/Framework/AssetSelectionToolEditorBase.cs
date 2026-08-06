using GameFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    internal enum ItemType
    {
        NoSupport,
        File,
        Folder
    }

    public enum AssetSelectionScope
    {
        FilesAndFolders,
        FilesOnly,
        FoldersOnly
    }

    public abstract class AssetSelectionSubToolBase
    {
        private readonly List<UnityEngine.Object> _selectedObjects = new List<UnityEngine.Object>();

        public abstract string AssetSelectorTypeFilter { get; }
        public abstract string DragAreaTips { get; }
        public virtual string ReadmeText => string.Empty;
        public virtual AssetSelectionScope SelectionScope => AssetSelectionScope.FilesAndFolders;
        public virtual int MaxSelectedObjectCount => 0;
        protected abstract Type[] SupportAssetTypes { get; }
        protected List<UnityEngine.Object> SelectedObjects => _selectedObjects;
        internal List<UnityEngine.Object> SelectionObjects => _selectedObjects;

        public virtual void OnEnter() { }
        public virtual void OnExit() { SaveSettings(); }
        public virtual void DrawBeforeSettingsPanel() { }
        public abstract void DrawSettingsPanel();
        public abstract void DrawBottomButtonsPanel();

        public virtual bool IsSupportAsset(string assetPath)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return SupportAssetTypes.Contains(assetType);
        }

        public virtual bool TryGetSelectionObject(UnityEngine.Object sourceObject, out UnityEngine.Object selectionObject)
        {
            selectionObject = null;
            if (sourceObject == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(sourceObject);
            if (GetSelectedItemType(assetPath) == ItemType.NoSupport)
            {
                return false;
            }

            selectionObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return selectionObject != null;
        }

        public virtual List<string> FilterSelectedAssets(List<UnityEngine.Object> selectedObjects)
        {
            var assetPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in selectedObjects)
            {
                if (item == null)
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(item);
                var itemType = GetSelectedItemType(assetPath);
                if (itemType == ItemType.File)
                {
                    var selectedAssetPath = Utility.Path.GetRegularPath(assetPath);
                    if (IsSupportAsset(selectedAssetPath))
                    {
                        assetPaths.Add(selectedAssetPath);
                    }
                }
                else if (itemType == ItemType.Folder)
                {
                    var assets = AssetDatabase.FindAssets(GetFindAssetsFilter(), new[] { assetPath });
                    for (var i = 0; i < assets.Length; i++)
                    {
                        assetPaths.Add(AssetDatabase.GUIDToAssetPath(assets[i]));
                    }
                }
            }

            return assetPaths.ToList();
        }

        protected string GetFindAssetsFilter()
        {
            var filterParts = new string[SupportAssetTypes.Length];
            for (var i = 0; i < SupportAssetTypes.Length; i++)
            {
                filterParts[i] = $"t:{SupportAssetTypes[i].Name}";
            }

            return string.Join(" ", filterParts);
        }

        public virtual void SaveSettings()
        {
            if (EditorToolSettings.Instance)
            {
                EditorToolSettings.Save();
            }
        }

        internal ItemType GetSelectedItemType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return ItemType.NoSupport;
            }

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return SelectionScope == AssetSelectionScope.FilesOnly ? ItemType.NoSupport : ItemType.Folder;
            }

            return SelectionScope != AssetSelectionScope.FoldersOnly && IsSupportAsset(assetPath) ? ItemType.File : ItemType.NoSupport;
        }

        internal List<string> GetAllBackupFilesByDir(string assetFolder, string baseFolder)
        {
            var assetPaths = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(assetFolder) && System.IO.Directory.Exists(assetFolder))
            {
                var allFiles = System.IO.Directory.GetFiles(assetFolder, "*.*", System.IO.SearchOption.AllDirectories);
                foreach (var item in allFiles)
                {
                    var fileName = Utility.Path.GetRegularPath(System.IO.Path.GetRelativePath(baseFolder, item));
                    if (IsSupportAsset(fileName))
                    {
                        assetPaths.Add(fileName);
                    }
                }
            }

            return assetPaths.ToList();
        }
    }

    public abstract class OwnedAssetSelectionSubToolBase<TOwner> : AssetSelectionSubToolBase
        where TOwner : class
    {
        protected TOwner OwnerEditor { get; private set; }

        internal void Initialize(TOwner ownerEditor)
        {
            OwnerEditor = ownerEditor;
        }

        protected List<string> GetSelectedAssets()
        {
            return FilterSelectedAssets(SelectionObjects);
        }
    }

    public abstract class AssetSelectionToolEditorBase<TPanel> : EditorToolBase where TPanel : AssetSelectionSubToolBase
    {
        private GUIStyle _centerLabelStyle;
        private GUIStyle _readmeLabelStyle;
        private Texture _assetListIcon;
        private ReorderableList _srcScrollList;
        private Vector2 _srcScrollPos;
        private bool _settingFoldout = true;
        private readonly List<UnityEngine.Object> _emptySelection = new List<UnityEngine.Object>();
        private readonly AssetSelectionSubToolRegistry<TPanel> _subToolRegistry = new AssetSelectionSubToolRegistry<TPanel>();

        protected TPanel CurrentPanel { get; private set; }

        protected abstract int ActivePanelIndex { get; set; }
        protected virtual int SelectObjectWindowId => GetType().GetHashCode();
        protected virtual string ClearListButtonText => "清空列表";
        protected virtual string SettingsFoldoutLabel => "展开设置项:";
        protected virtual bool UseRootVerticalLayout => false;
        protected virtual bool WrapSelectionAreaInHelpBox => true;
        protected virtual bool WrapSettingsAreaInHelpBox => true;
        protected virtual GUIStyle DropAreaStyle => EditorStyles.helpBox;
        protected virtual float SelectionListMinHeight => 100f;
        protected virtual float CenterLabelFontSize => 13f;
        protected virtual Color CenterLabelColor => EditorGUIUtility.isProSkin
            ? new Color(0.7f, 0.7f, 0.7f)
            : new Color(0.35f, 0.35f, 0.35f);
        protected virtual float DragAreaTitleMinHeight => 44f;
        protected virtual bool ShowReadmeText => !string.IsNullOrEmpty(CurrentPanel?.ReadmeText);
        protected virtual float ReadmeFontSize => 11f;
        protected virtual Color ReadmeTextColor => EditorGUIUtility.isProSkin
            ? new Color(0.55f, 0.55f, 0.55f)
            : new Color(0.45f, 0.45f, 0.45f);

        protected abstract TPanel CreateSubPanelInstance(Type panelType);
        protected virtual void RestorePanelSelection(TPanel panel) { }

        private List<UnityEngine.Object> CurrentSelection => CurrentPanel == null ? _emptySelection : CurrentPanel.SelectionObjects;

        protected override void OnEnable()
        {
            base.OnEnable();

            _srcScrollList = new ReorderableList(_emptySelection, typeof(UnityEngine.Object), true, true, true, true);
            _srcScrollList.drawHeaderCallback = DrawScrollListHeader;
            _srcScrollList.onAddCallback = AddItem;
            _srcScrollList.drawElementCallback = DrawItems;
            _srcScrollList.multiSelect = true;

            _subToolRegistry.Reload(GetType());
            SwitchSubPanel(ActivePanelIndex);
        }

        protected virtual void OnDisable()
        {
            _subToolRegistry.ForEachCreatedPanel(panel => panel.OnExit());
        }

        protected virtual void OnGUI()
        {
            if (CurrentPanel == null)
            {
                return;
            }

            EnsureStyles();
            ObjectSelectorBridge.HandleObjectSelectorEvent(Event.current);

            if (UseRootVerticalLayout)
            {
                EditorGUILayout.BeginVertical();
            }

            DrawToolbar();
            DrawSelectionArea();

            CurrentPanel.DrawBeforeSettingsPanel();

            EditorGUILayout.Space(10);
            if (_settingFoldout = EditorGUILayout.Foldout(_settingFoldout, SettingsFoldoutLabel))
            {
                if (WrapSettingsAreaInHelpBox)
                {
                    EditorGUILayout.BeginVertical("helpbox");
                }

                CurrentPanel.DrawSettingsPanel();

                if (WrapSettingsAreaInHelpBox)
                {
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(10);
            CurrentPanel.DrawBottomButtonsPanel();

            if (UseRootVerticalLayout)
            {
                EditorGUILayout.EndVertical();
            }
        }

        protected void ForEachCreatedPanel(Action<TPanel> action)
        {
            _subToolRegistry.ForEachCreatedPanel(action);
        }

        private void EnsureStyles()
        {
            if (_centerLabelStyle != null && _readmeLabelStyle != null)
            {
                return;
            }

            InitializeStyles();
        }

        private void InitializeStyles()
        {
            _centerLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(CenterLabelFontSize)
            };
            _centerLabelStyle.normal.textColor = CenterLabelColor;

            _readmeLabelStyle = new GUIStyle(_centerLabelStyle)
            {
                fontSize = Mathf.RoundToInt(ReadmeFontSize)
            };
            _readmeLabelStyle.normal.textColor = ReadmeTextColor;
            _assetListIcon = EditorGUIUtility.FindTexture(EditorGUIUtility.isProSkin ? "d_Folder Icon" : "Folder Icon");
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUI.BeginChangeCheck();
            ActivePanelIndex = GUILayout.Toolbar(ActivePanelIndex, _subToolRegistry.Titles, GUILayout.Height(30));
            if (EditorGUI.EndChangeCheck())
            {
                SwitchSubPanel(ActivePanelIndex);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionArea()
        {
            if (WrapSelectionAreaInHelpBox)
            {
                EditorGUILayout.BeginVertical("helpbox");
            }

            _srcScrollPos = EditorGUILayout.BeginScrollView(_srcScrollPos, GUILayout.MinHeight(SelectionListMinHeight));
            _srcScrollList.list = CurrentSelection;
            _srcScrollList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            DrawDropArea();

            if (WrapSelectionAreaInHelpBox)
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDropArea()
        {
            var dragRect = EditorGUILayout.BeginVertical(DropAreaStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Space(10f);
            EditorGUILayout.LabelField(CurrentPanel.DragAreaTips, _centerLabelStyle, GUILayout.MinHeight(DragAreaTitleMinHeight));
            if (ShowReadmeText)
            {
                EditorGUILayout.LabelField(CurrentPanel.ReadmeText, _readmeLabelStyle);
            }

            var currentEvent = Event.current;
            if (dragRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = TryGetFirstSelectionObject(DragAndDrop.objectReferences, out _) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform && DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                {
                    if (TryGetFirstSelectionObject(DragAndDrop.objectReferences, out _))
                    {
                        DragAndDrop.AcceptDrag();
                        OnItemsDrop(DragAndDrop.objectReferences);
                    }

                    currentEvent.Use();
                }
            }

            GUILayout.Space(10f);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void OnItemsDrop(UnityEngine.Object[] objectReferences)
        {
            bool isSingleSelection = CurrentPanel.MaxSelectedObjectCount == 1;
            for (var i = 0; i < objectReferences.Length; i++)
            {
                if (!TryGetSelectionObject(objectReferences[i], out var selectionObject))
                {
                    LogUnsupportedAsset(objectReferences[i]);
                    continue;
                }

                AddSelectionObject(selectionObject);
                if (isSingleSelection)
                {
                    break;
                }
            }
        }

        private void AddItem(UnityEngine.Object obj)
        {
            if (!TryGetSelectionObject(obj, out var selectionObject))
            {
                LogUnsupportedAsset(obj);
                return;
            }

            AddSelectionObject(selectionObject);
        }

        private void AddSelectionObject(UnityEngine.Object selectionObject)
        {
            if (selectionObject == null || CurrentSelection.Contains(selectionObject))
            {
                return;
            }

            int maxCount = CurrentPanel.MaxSelectedObjectCount;
            if (maxCount == 1 && CurrentSelection.Count > 0)
            {
                ReplaceItem(0, selectionObject);
                return;
            }

            if (maxCount > 0 && CurrentSelection.Count >= maxCount)
            {
                Debug.LogWarningFormat("添加失败! 当前工具最多支持选择 {0} 个资源。", maxCount);
                return;
            }

            CurrentSelection.Add(selectionObject);
        }

        private void DrawItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            HandleItemDrop(rect, index);
            EditorGUI.BeginChangeCheck();
            var replacement = EditorGUI.ObjectField(rect, CurrentSelection[index], typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                ReplaceItem(index, replacement);
            }
        }

        private void DrawScrollListHeader(Rect rect)
        {
            var clearRect = rect;
            clearRect.xMin = clearRect.xMax - 80f;
            var labelRect = rect;
            labelRect.xMax = clearRect.xMin - 4f;
            var iconRect = labelRect;
            iconRect.width = 16f;
            iconRect.height = 16f;
            iconRect.y += (rect.height - iconRect.height) * 0.5f;
            labelRect.xMin = iconRect.xMax + 4f;
            GUI.DrawTexture(iconRect, _assetListIcon, ScaleMode.ScaleToFit, true);
            EditorGUI.LabelField(labelRect, $"已选资源 ({CurrentSelection.Count})");
            if (GUI.Button(clearRect, ClearListButtonText))
            {
                CurrentSelection.Clear();
            }
        }

        private void OnSelectAsset(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!TryGetSelectionObject(obj, out var selectionObject))
            {
                LogUnsupportedAsset(obj);
                return;
            }

            AddSelectionObject(selectionObject);
        }

        private void ReplaceItem(int index, UnityEngine.Object obj)
        {
            if (obj == null)
            {
                CurrentSelection.RemoveAt(index);
                return;
            }

            if (!TryGetSelectionObject(obj, out var selectionObject))
            {
                LogUnsupportedAsset(obj);
                return;
            }

            var currentObject = CurrentSelection[index];
            if (currentObject == selectionObject)
            {
                return;
            }

            int existingIndex = CurrentSelection.IndexOf(selectionObject);
            if (existingIndex >= 0)
            {
                CurrentSelection[existingIndex] = currentObject;
            }

            CurrentSelection[index] = selectionObject;
        }

        private void HandleItemDrop(Rect rect, int index)
        {
            var currentEvent = Event.current;
            if (!rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = TryGetFirstSelectionObject(DragAndDrop.objectReferences, out _) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform && TryGetFirstSelectionObject(DragAndDrop.objectReferences, out var selectionObject))
            {
                DragAndDrop.AcceptDrag();
                ReplaceItem(index, selectionObject);
                currentEvent.Use();
            }
        }

        private bool TryGetFirstSelectionObject(UnityEngine.Object[] objectReferences, out UnityEngine.Object selectionObject)
        {
            selectionObject = null;
            if (objectReferences == null)
            {
                return false;
            }

            for (var i = 0; i < objectReferences.Length; i++)
            {
                if (TryGetSelectionObject(objectReferences[i], out selectionObject))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSelectionObject(UnityEngine.Object sourceObject, out UnityEngine.Object selectionObject)
        {
            selectionObject = null;
            return CurrentPanel != null && CurrentPanel.TryGetSelectionObject(sourceObject, out selectionObject);
        }

        private static void LogUnsupportedAsset(UnityEngine.Object obj)
        {
            Debug.LogWarningFormat("添加失败! 不支持的文件格式:{0}", obj == null ? string.Empty : AssetDatabase.GetAssetPath(obj));
        }

        private void AddItem(ReorderableList list)
        {
            if (!ObjectSelectorBridge.Open(typeof(UnityEngine.Object), CurrentPanel.AssetSelectorTypeFilter, OnSelectAsset, SelectObjectWindowId))
            {
                Debug.LogWarning("打开资源选择界面失败!");
            }
        }

        private void SwitchSubPanel(int panelIndex)
        {
            if (_subToolRegistry.Count <= 0)
            {
                CurrentPanel = null;
                return;
            }

            panelIndex = Mathf.Clamp(panelIndex, 0, _subToolRegistry.Count - 1);
            ActivePanelIndex = panelIndex;
            SetWindowTitle(_subToolRegistry.GetTitle(panelIndex));

            CurrentPanel?.OnExit();
            CurrentPanel = _subToolRegistry.GetOrCreatePanel(panelIndex, CreateSubPanelInstance);
            RestorePanelSelection(CurrentPanel);
            _srcScrollList.list = CurrentSelection;
            CurrentPanel?.OnEnter();
        }
    }

    public abstract class PersistentAssetSelectionToolEditorBase<TPanel> : AssetSelectionToolEditorBase<TPanel>
        where TPanel : AssetSelectionSubToolBase
    {
        private int _activePanelIndex;
        private AssetSelectionToolSessionState _sessionState;
        private readonly HashSet<TPanel> _restoredPanels = new HashSet<TPanel>();

        protected override int ActivePanelIndex
        {
            get => _activePanelIndex;
            set => _activePanelIndex = value;
        }

        protected virtual string SessionStatePrefix => $"UGF.EditorTools.{GetType().FullName}";
        private AssetSelectionToolSessionState SessionStateCache => _sessionState ??= new AssetSelectionToolSessionState(SessionStatePrefix);

        protected override void OnEnable()
        {
            LoadSessionState();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveSessionState();
        }

        protected override void RestorePanelSelection(TPanel panel)
        {
            if (_restoredPanels.Add(panel))
            {
                SessionStateCache.LoadSelectedObjects(panel.GetType().FullName, panel.SelectionObjects);
            }
        }

        private void LoadSessionState()
        {
            _activePanelIndex = SessionStateCache.LoadActivePanelIndex();
        }

        private void SaveSessionState()
        {
            SessionStateCache.SaveActivePanelIndex(_activePanelIndex);
            ForEachCreatedPanel(panel => SessionStateCache.SaveSelectedObjects(panel.GetType().FullName, panel.SelectionObjects));
        }
    }
}
