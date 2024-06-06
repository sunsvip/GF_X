using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.Linq;
using GameFramework;
using System.Reflection;

namespace UGF.EditorTools
{
    internal enum ItemType
    {
        NoSupport,
        File,//文件
        Folder//文件夹
    }
    [EditorToolMenu("资源/压缩(优化)工具", null, 2)]
    public class CompressToolEditor : EditorToolBase
    {
        public override string ToolName => "压缩(优化)工具";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        GUIStyle centerLabelStyle;
        GUIStyle readmeLabelStyle;
        ReorderableList srcScrollList;
        Vector2 srcScrollPos;

        readonly int selectOjbWinId = "CompressToolEditor".GetHashCode();
        private bool settingFoldout = true;

        List<Type> subPanelsClass;
        string[] subPanelTitles;
        CompressToolSubPanel[] subPanels;
        CompressToolSubPanel curPanel;
        private void OnEnable()
        {
            subPanelsClass = new List<Type>();
            centerLabelStyle = new GUIStyle();
            centerLabelStyle.alignment = TextAnchor.MiddleCenter;
            centerLabelStyle.fontSize = 50;
            centerLabelStyle.normal.textColor = new Color(1, 1, 1, 0.25f);

            readmeLabelStyle = new GUIStyle(centerLabelStyle);
            readmeLabelStyle.fontSize = 18;

            srcScrollList = new ReorderableList(EditorToolSettings.Instance.CompressImgToolItemList, typeof(UnityEngine.Object), true, true, true, true);
            srcScrollList.drawHeaderCallback = DrawScrollListHeader;
            srcScrollList.onAddCallback = AddItem;
            srcScrollList.drawElementCallback = DrawItems;
            srcScrollList.multiSelect = true;
            ScanSubPanelClass();

            SwitchSubPanel(EditorToolSettings.Instance.CompressImgMode);
        }


        private void ScanSubPanelClass()
        {
            subPanelsClass.Clear();
            var editorDll = Utility.Assembly.GetAssemblies().First(dll => dll.GetName().Name.CompareTo("Assembly-CSharp-Editor") == 0);
            var allEditorTool = editorDll.GetTypes().Where(tp => (tp.IsClass && !tp.IsAbstract && tp.IsSubclassOf(typeof(CompressToolSubPanel)) && tp.GetCustomAttribute<EditorToolMenuAttribute>() != null));

            subPanelsClass.AddRange(allEditorTool);
            subPanelsClass.Sort((x, y) =>
            {
                int xOrder = x.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                int yOrder = y.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                return xOrder.CompareTo(yOrder);
            });

            subPanels = new CompressToolSubPanel[subPanelsClass.Count];
            subPanelTitles = new string[subPanelsClass.Count];
            for (int i = 0; i < subPanelsClass.Count; i++)
            {
                var toolAttr = subPanelsClass[i].GetCustomAttribute<EditorToolMenuAttribute>();
                subPanelTitles[i] = toolAttr.ToolMenuPath;
            }
        }
        private void OnDisable()
        {
            foreach (var panel in subPanels)
            {
                panel?.OnExit();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUI.BeginChangeCheck();
                EditorToolSettings.Instance.CompressImgMode = GUILayout.Toolbar(EditorToolSettings.Instance.CompressImgMode, subPanelTitles, GUILayout.Height(30));
                if (EditorGUI.EndChangeCheck())
                {
                    SwitchSubPanel(EditorToolSettings.Instance.CompressImgMode);
                }
                EditorGUILayout.EndHorizontal();
            }
            srcScrollPos = EditorGUILayout.BeginScrollView(srcScrollPos);
            srcScrollList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            DrawDropArea();
            EditorGUILayout.Space(10);
            if (settingFoldout = EditorGUILayout.Foldout(settingFoldout, "展开设置项:"))
            {
                curPanel.DrawSettingsPanel();
            }
            curPanel.DrawBottomButtonsPanel();
            EditorGUILayout.EndVertical();
        }


        /// <summary>
        /// 绘制拖拽添加文件区域
        /// </summary>
        private void DrawDropArea()
        {
            var dragRect = EditorGUILayout.BeginVertical("box");
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(curPanel.DragAreaTips, centerLabelStyle, GUILayout.Height(centerLabelStyle.fontSize + 10));
                EditorGUILayout.LabelField(curPanel.ReadmeText, readmeLabelStyle);
                if (dragRect.Contains(UnityEngine.Event.current.mousePosition))
                {
                    if (UnityEngine.Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    }
                    else if (UnityEngine.Event.current.type == EventType.DragExited)
                    {
                        if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                        {
                            OnItemsDrop(DragAndDrop.objectReferences);
                        }

                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 拖拽松手
        /// </summary>
        /// <param name="objectReferences"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnItemsDrop(UnityEngine.Object[] objectReferences)
        {
            foreach (var item in objectReferences)
            {
                var itemPath = AssetDatabase.GetAssetPath(item);
                if (curPanel.GetSelectedItemType(itemPath) == ItemType.NoSupport)
                {
                    Debug.LogWarningFormat("添加失败! 不支持的文件格式:{0}", itemPath);
                    continue;
                }
                AddItem(item);
            }
        }
        private void AddItem(UnityEngine.Object obj)
        {
            if (obj == null || EditorToolSettings.Instance.CompressImgToolItemList.Contains(obj)) return;

            EditorToolSettings.Instance.CompressImgToolItemList.Add(obj);
        }

        private void DrawItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = EditorToolSettings.Instance.CompressImgToolItemList[index];
            EditorGUI.ObjectField(rect, item, typeof(UnityEngine.Object), false);
        }

        private void DrawScrollListHeader(Rect rect)
        {
            if (GUI.Button(rect, "清除列表"))
            {
                EditorToolSettings.Instance.CompressImgToolItemList?.Clear();
            }
        }
        private void OnSelectAsset(UnityEngine.Object obj)
        {
            AddItem(obj);
        }

        private void AddItem(ReorderableList list)
        {
            if (!EditorUtilityExtension.OpenAssetSelector(typeof(UnityEngine.Object), curPanel.AssetSelectorTypeFilter, OnSelectAsset, selectOjbWinId))
            {
                Debug.LogWarning("打开资源选择界面失败!");
            }
        }

        private void SwitchSubPanel(int mCompressMode)
        {
            mCompressMode = Mathf.Clamp(mCompressMode, 0, subPanelsClass.Count - 1);
            this.titleContent.text = subPanelTitles[mCompressMode];
            if (curPanel != null)
            {
                curPanel.OnExit();
            }

            if (subPanels[mCompressMode] != null)
            {
                curPanel = subPanels[mCompressMode];
            }
            else
            {
                curPanel = subPanels[mCompressMode] = Activator.CreateInstance(subPanelsClass[mCompressMode]) as CompressToolSubPanel;
            }

            curPanel.OnEnter();
        }
    }
}

