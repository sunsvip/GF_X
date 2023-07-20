using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 批处理操作工具
    /// </summary>
    public abstract class UtilityToolEditorBase : EditorToolBase
    {
        //public override string ToolName => "批处理工具集";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        GUIStyle centerLabelStyle;
        ReorderableList srcScrollList;
        Vector2 srcScrollPos;

        private int SelectOjbWinId => this.GetType().GetHashCode();
        private bool settingFoldout = true;

        List<Type> subPanelsClass;
        string[] subPanelTitles;
        UtilitySubToolBase[] subPanels;
        UtilitySubToolBase curPanel;
        private int mCompressMode;
        private List<UnityEngine.Object> selectList;

        private void OnEnable()
        {
            selectList = new List<UnityEngine.Object>();
            subPanelsClass = new List<Type>();
            centerLabelStyle = new GUIStyle();
            centerLabelStyle.alignment = TextAnchor.MiddleCenter;
            centerLabelStyle.fontSize = 25;
            centerLabelStyle.normal.textColor = Color.gray;

            srcScrollList = new ReorderableList(selectList, typeof(UnityEngine.Object), true, true, true, true);
            srcScrollList.drawHeaderCallback = DrawScrollListHeader;
            srcScrollList.onAddCallback = AddItem;
            srcScrollList.drawElementCallback = DrawItems;
            srcScrollList.multiSelect = true;
            ScanSubPanelClass();

            SwitchSubPanel(0);
        }


        private void ScanSubPanelClass()
        {
            subPanelsClass.Clear();
            var editorDll = Utility.Assembly.GetAssemblies().First(dll => dll.GetName().Name.CompareTo("Assembly-CSharp-Editor") == 0);
            var allEditorTool = editorDll.GetTypes().Where(tp => (tp.IsSubclassOf(typeof(UtilitySubToolBase)) && tp.HasAttribute<EditorToolMenuAttribute>() && tp.GetCustomAttribute<EditorToolMenuAttribute>().OwnerType == this.GetType()));

            subPanelsClass.AddRange(allEditorTool);
            subPanelsClass.Sort((x, y) =>
            {
                int xOrder = x.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                int yOrder = y.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                return xOrder.CompareTo(yOrder);
            });

            subPanels = new UtilitySubToolBase[subPanelsClass.Count];
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
            if (curPanel == null) return;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUI.BeginChangeCheck();
                mCompressMode = GUILayout.Toolbar(mCompressMode, subPanelTitles, GUILayout.Height(30));
                if (EditorGUI.EndChangeCheck())
                {
                    SwitchSubPanel(mCompressMode);
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
                EditorGUILayout.LabelField(curPanel.DragAreaTips, centerLabelStyle, GUILayout.MinHeight(200));
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
            if (obj == null || selectList.Contains(obj)) return;

            selectList.Add(obj);
        }

        private void DrawItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = selectList[index];
            EditorGUI.ObjectField(rect, item, typeof(UnityEngine.Object), false);
        }

        private void DrawScrollListHeader(Rect rect)
        {
            if (GUI.Button(rect, "清除列表"))
            {
                selectList?.Clear();
            }
        }
        private void OnSelectAsset(UnityEngine.Object obj)
        {
            AddItem(obj);
        }

        private void AddItem(ReorderableList list)
        {
            if (!EditorUtilityExtension.OpenAssetSelector(typeof(UnityEngine.Object), curPanel.AssetSelectorTypeFilter, OnSelectAsset, SelectOjbWinId))
            {
                Debug.LogWarning("打开资源选择界面失败!");
            }
        }

        private void SwitchSubPanel(int panelIdx)
        {
            if (subPanelsClass.Count <= 0) return;
            mCompressMode = Mathf.Clamp(panelIdx, 0, subPanelsClass.Count);
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
                curPanel = subPanels[mCompressMode] = Activator.CreateInstance(subPanelsClass[mCompressMode], new object[] { this }) as UtilitySubToolBase;
            }

            curPanel.OnEnter();
        }

        /// <summary>
        /// 获取当前选择的资源文件列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedAssets()
        {
            return curPanel.FilterSelectedAssets(selectList);
        }
    }
}
