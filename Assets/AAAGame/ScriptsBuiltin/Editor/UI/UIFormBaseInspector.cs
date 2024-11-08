#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using System;
using GameFramework;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    [CustomEditor(typeof(UIFormBase), true)]
    public class UIFormBaseInspector : UnityEditor.Editor
    {
        public const string helpTitle = "使用说明";
        public const string helpDoc = "1.打开UI界面预制体.\n2.右键节点'UIForm Tools'子菜单,添加/移除变量.\n3.在Inspector面板点击功能按钮生成变量代码.";
        const string KEY_BUTTON_ONCLICK = "ClickUIButton";
        const string KEY_BUTTON_ONCLOSE = "OnClickClose";
        readonly static string[] varPrefixArr = { "private", "protected", "public" };
        const string arrFlag = "Arr";
        const float fieldPrefixWidth = 80;
        const float fieldTypeWidth = 220;
        SerializedProperty mFields;
        static bool addToFieldToggle;
        static bool removeToFieldToggle;
        ReorderableList[] mReorderableList;
        UIFormBase uiForm;
        int mCurFieldIdx;

        int mCurFoldoutItemIdx = -1;
        static int varPrefixIndex = -1;
        static bool mShowSelectTypeMenu;

        GUIContent prefixContent;
        GUIContent typeContent;
        static GUIStyle varLabelGUIStyle;
        private GUIContent bindVarBtTitle;
        private GUIContent generateVarBtTitle;
        private GUIContent openVarCodeBtTitle;
        private GUIContent openUiLogicBtTitle;
        private GUIContent dropdownBtContent;

        private GUIStyle highlightBtStyle;

        SerializedProperty m_UIOpenAnim;
        SerializedProperty m_UICloseAnim;
        #region #右键菜单

        const string REFRESH_BIND = "UI_REFRESH_BIND";

        [InitializeOnLoadMethod]
        static void InitEditor()
        {
            Selection.selectionChanged = () =>
            {
                addToFieldToggle = false;
                removeToFieldToggle = false;
            };

            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemOnGUI;
        }
        private static void OnHierarchyItemOnGUI(int instanceID, Rect rect)
        {
            OpenSelectComponentMenuListener(rect);
            if (varLabelGUIStyle == null)
            {
                varLabelGUIStyle = new GUIStyle(EditorStyles.helpBox);
                varLabelGUIStyle.stretchWidth = false;
                varLabelGUIStyle.stretchHeight = false;
                varLabelGUIStyle.normal.textColor = Color.white * 0.88f;
                varLabelGUIStyle.fontStyle = FontStyle.Bold;
                varLabelGUIStyle.hover.textColor = Color.cyan;
            }
            var curDrawNode = EditorUtility.InstanceIDToObject(instanceID);
            if (curDrawNode == null) return;
            var uiForm = GetSerializeFieldTool(curDrawNode as GameObject);
            if (uiForm == null) return;
            var fields = uiForm.SerializeFieldArr;

            foreach (var item in fields)
            {
                if (item == null) continue;
                if (ArrayUtility.Contains(item.Targets, curDrawNode))
                {
                    var displayContent = EditorGUIUtility.TrTextContent(Utility.Text.Format("{0} {1} {2}", GetVarPrefix(item.VarPrefix), GetDisplayVarTypeName(item.VarType), item.VarName));
                    var itemLabelRect = GUILayoutUtility.GetRect(displayContent, varLabelGUIStyle);
                    itemLabelRect.y = rect.y;
                    itemLabelRect.width = Mathf.Min(rect.width * 0.4f, itemLabelRect.width);
                    itemLabelRect.x = rect.xMax - itemLabelRect.width;
                    if (itemLabelRect.width > 100)
                    {
                        GUI.Label(itemLabelRect, displayContent, varLabelGUIStyle);
                    }
                    break;
                }
            }
        }

        private static string GetVarPrefix(int idx)
        {
            return varPrefixArr[idx];
        }
        private static string GetDisplayVarTypeName(string varFullTypeName)
        {
            if (string.IsNullOrWhiteSpace(varFullTypeName)) return string.Empty;

            if (Path.HasExtension(varFullTypeName))
            {
                return Path.GetExtension(varFullTypeName).Substring(1);
            }
            return varFullTypeName;
        }
        [MenuItem("GameObject/UIForm Tools/Add private", false, priority = 1002)]
        private static void AddPrivateVariable2UIForm()
        {
            varPrefixIndex = 0;
            mShowSelectTypeMenu = true;
        }
        [MenuItem("GameObject/UIForm Tools/Add protected", false, priority = 1003)]
        private static void AddProtectedVariable2UIForm()
        {
            varPrefixIndex = 1;
            mShowSelectTypeMenu = true;
        }
        [MenuItem("GameObject/UIForm Tools/Add multiple", false, priority = 1004)]
        private static void AddPublicVariable2UIForm()
        {
            varPrefixIndex = 2;
            mShowSelectTypeMenu = true;
        }

        [MenuItem("GameObject/UIForm Tools/Remove", false, priority = 1005)]
        private static void RemoveUIFormVariable()
        {
            if (removeToFieldToggle)
            {
                return;
            }
            if (Selection.count <= 0) return;
            var selectGos = Selection.gameObjects;
            for (int i = 0; i < selectGos.Length; i++)
            {
                var itm = selectGos[i];
                if (itm == null) continue;
                var serializeTool = GetSerializeFieldTool(itm);
                if (serializeTool == null) continue;
                var fieldsProperties = serializeTool.SerializeFieldArr;
                if (fieldsProperties == null) return;
                for (int j = fieldsProperties.Length - 1; j >= 0; j--)
                {
                    var fields = fieldsProperties[j];
                    if (fields == null || fields.Targets == null || fields.Targets.Length <= 0) continue;
                    for (int k = fields.Targets.Length - 1; k >= 0; k--)
                    {
                        if (fields.Targets[k] == itm)
                        {
                            if (fields.Targets.Length <= 1)
                            {
                                ArrayUtility.RemoveAt(ref fieldsProperties, j);
                            }
                            else
                            {
                                ArrayUtility.RemoveAt(ref fields.Targets, k);
                            }
                        }
                    }
                }
                serializeTool.SerializeFieldArr = fieldsProperties;
                EditorUtility.SetDirty(serializeTool as MonoBehaviour);
            }
            removeToFieldToggle = true;
            addToFieldToggle = false;
        }
        [MenuItem("GameObject/UIForm Tools/Add Button OnClick(string)", false, priority = 1101)]
        static void AddClickButtonEventString()
        {
            //添加参数为string的按钮事件
            AddClickButtonEvent<string>();
        }
        [MenuItem("GameObject/UIForm Tools/Add Button OnClick(Button)", false, priority = 1102)]
        static void AddClickButtonEventButton()
        {
            //添加参数为按钮本身的按钮事件
            AddClickButtonEvent<UnityEngine.UI.Button>();
        }
        [MenuItem("GameObject/UIForm Tools/Add Localization Key", false, priority = 1103)]
        static void AddLocalizationKey()
        {
            if (Selection.count == 0) return;
            var selectGos = Selection.gameObjects;
            foreach (var item in selectGos)
            {
                if (item == null) continue;
                if (item.GetComponent<TMPro.TextMeshProUGUI>() != null || item.GetComponent<UnityEngine.UI.Text>() != null || item.GetComponent<TMPro.TextMeshPro>() != null)
                {
                    var serializeTool = GetSerializeFieldTool(item);
                    if (serializeTool == null) continue;
                    var goLogic = serializeTool as MonoBehaviour;
                    item.GetOrAddComponent<UIStringKey>().Key = Utility.Text.Format("{0}.{1}", goLogic.name, item.name);
                    EditorUtility.SetDirty(goLogic);
                }
            }
        }

        private static void AddClickButtonEvent<T>()
        {
            if (Selection.count == 0) return;

            var paramsType = typeof(T);
            var selectGos = Selection.gameObjects;
            foreach (var item in selectGos)
            {
                if (item == null || !item.TryGetComponent<Button>(out var buttonCom)) continue;
                var serializeTool = GetSerializeFieldTool(item);
                if (serializeTool == null) continue;
                var goLogic = serializeTool as MonoBehaviour;
                var m_OnClick = buttonCom.GetType().GetField("m_OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(buttonCom) as UnityEvent;
                var btnEvent = UnityEngine.Events.UnityAction.CreateDelegate(typeof(UnityAction<T>), goLogic, KEY_BUTTON_ONCLICK) as UnityAction<T>;
                for (int i = m_OnClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(m_OnClick, i);
                }
                if (paramsType == typeof(string))
                {
                    UnityEventTools.AddStringPersistentListener(m_OnClick, btnEvent as UnityAction<string>, buttonCom.name);
                }
                else if (paramsType == typeof(UnityEngine.UI.Button))
                {
                    UnityEventTools.AddObjectPersistentListener<UnityEngine.UI.Button>(m_OnClick, btnEvent as UnityAction<UnityEngine.UI.Button>, buttonCom);
                }
                EditorUtility.SetDirty(goLogic);
            }
        }
        [MenuItem("GameObject/UIForm Tools/Add Close Button Event", false, priority = 1102)]
        private static void AddCloseButtonEvent()
        {
            if (Selection.count <= 0) return;

            foreach (var item in Selection.gameObjects)
            {
                if (item == null || !item.TryGetComponent<Button>(out var buttonCom)) continue;
                var serializeTool = GetSerializeFieldTool(item);
                if (serializeTool == null) continue;
                var goLogic = serializeTool as MonoBehaviour;
                var m_OnClick = buttonCom.GetType().GetField("m_OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(buttonCom) as UnityEvent;
                var btnEvent = UnityEngine.Events.UnityAction.CreateDelegate(typeof(UnityAction), goLogic, KEY_BUTTON_ONCLOSE) as UnityAction;
                for (int i = m_OnClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(m_OnClick, i);
                }
                UnityEventTools.AddVoidPersistentListener(m_OnClick, btnEvent);
                EditorUtility.SetDirty(goLogic);
            }
        }
        /// <summary>
        /// 不带名字空间的类型名
        /// </summary>
        /// <returns></returns>
        private static Type GetSampleType(string fullName)
        {
            var result = Utility.Assembly.GetType(fullName);
            return result;
        }

        private static ISerializeFieldTool GetSerializeFieldTool(GameObject go)
        {
            var parent = go.transform.parent;
            while (parent != null)
            {
                var mono = parent.GetComponents<MonoBehaviour>().FirstOrDefault(item => item is ISerializeFieldTool);
                if (mono != null)
                {
                    return mono as ISerializeFieldTool;
                }
                parent = parent.parent;
            }
            return null;
        }
        private static void AddToFields(int varPrefix, string varType)
        {
            if (addToFieldToggle)
            {
                return;
            }
            if (Selection.count <= 0) return;
            var targets = GetTargetsFromSelectedNodes(Selection.gameObjects);

            if (varPrefix != 2)
            {
                Dictionary<ISerializeFieldTool, List<GameObject>> dic = new Dictionary<ISerializeFieldTool, List<GameObject>>();
                foreach (var item in targets)
                {
                    var serializeTool = GetSerializeFieldTool(item);
                    if (serializeTool == null) continue;
                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    if (fieldsProperties == null) return;
                    if (dic.ContainsKey(serializeTool))
                    {
                        dic[serializeTool].Add(item);
                    }
                    else
                    {
                        dic[serializeTool] = new List<GameObject>() { item };
                    }
                }
                foreach (var item in dic)
                {
                    var serializeTool = item.Key;
                    var goLogic = serializeTool as MonoBehaviour;
                    Undo.RecordObject(goLogic, goLogic.name);
                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    var gos = item.Value.ToArray();
                    var field = new SerializeFieldData(GenerateFieldName(fieldsProperties, gos), gos);
                    field.VarPrefix = varPrefix;
                    field.VarType = varType;
                    ArrayUtility.Add(ref fieldsProperties, field);
                    serializeTool.SerializeFieldArr = fieldsProperties;
                    EditorUtility.SetDirty(goLogic);
                }

            }
            else
            {
                HashSet<UnityEngine.Object> recorded = new HashSet<UnityEngine.Object>();
                foreach (var item in targets)
                {
                    var serializeTool = GetSerializeFieldTool(item);
                    if (serializeTool == null) continue;
                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    if (fieldsProperties == null) return;
                    var goLogic = serializeTool as MonoBehaviour;
                    if (!recorded.Contains(goLogic))
                        Undo.RecordObject(goLogic, goLogic.name);
                    GameObject[] elements = new GameObject[] { item };
                    var field = new SerializeFieldData(GenerateFieldName(fieldsProperties, elements), elements);
                    field.VarPrefix = 1; //默认protect
                    field.VarType = varType;
                    ArrayUtility.Add(ref fieldsProperties, field);
                    serializeTool.SerializeFieldArr = fieldsProperties;
                    EditorUtility.SetDirty(goLogic);
                }
            }
            addToFieldToggle = true;
            removeToFieldToggle = false;
        }
        private static GameObject[] GetTargetsFromSelectedNodes(GameObject[] selectedList)
        {
            GameObject[] targets = new GameObject[selectedList.Length];
            for (int i = 0; i < selectedList.Length; i++)
            {
                targets[i] = selectedList[i];
            }
            targets = targets.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();
            return targets;
        }
        #endregion

        private void OnEnable()
        {
            highlightBtStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).button);
            highlightBtStyle.normal.background = EditorGUIUtility.FindTexture("sv_label_3");
            highlightBtStyle.hover.background = EditorGUIUtility.FindTexture("sv_label_2");
            highlightBtStyle.active.background = EditorGUIUtility.FindTexture("sv_label_1");
            highlightBtStyle.fontStyle = FontStyle.Bold;
            highlightBtStyle.fontSize += 2;
            bindVarBtTitle = new GUIContent("绑定变量", "bind components to variables");
            generateVarBtTitle = new GUIContent("生成变量代码", "generate or update variables code");
            openVarCodeBtTitle = new GUIContent("查看变量代码", "open variables code in editor");
            openUiLogicBtTitle = new GUIContent("编辑UI代码", "open ui logic code in editor");
            dropdownBtContent = new GUIContent("选择动效", "select dotween sequence component");
            prefixContent = new GUIContent();
            typeContent = new GUIContent();
            varPrefixIndex = 0;
            mShowSelectTypeMenu = false;
            uiForm = (target as UIFormBase);
            if (uiForm.SerializeFieldArr == null)
            {
                uiForm.SerializeFieldArr = new SerializeFieldData[0];
            }
            mFields = serializedObject.FindProperty("_fields");
            m_UIOpenAnim = serializedObject.FindProperty("m_OpenAnimation");
            m_UICloseAnim = serializedObject.FindProperty("m_CloseAnimation");
            mReorderableList = new ReorderableList[mFields.arraySize];
            EditorApplication.update += OnEditorUpdate;
        }
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        private void OnEditorUpdate()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;
            if (EditorPrefs.GetBool(REFRESH_BIND, false))
            {
                SerializeFieldProperties(serializedObject, uiForm.SerializeFieldArr);
            }
        }

        private void OnDestroy()
        {
            EditorToolSettings.Save();
        }
        private static void OpenSelectComponentMenuListener(Rect rect)
        {
            if (mShowSelectTypeMenu)
            {
                int idx = -1;
                var strArr = GetPopupContents(GetTargetsFromSelectedNodes(Selection.gameObjects));
                var contents = new GUIContent[strArr.Length];
                for (int i = 0; i < strArr.Length; i++)
                {
                    contents[i] = new GUIContent(strArr[i]);
                }
                rect.width = 200;
                rect.height = MathF.Max(100, contents.Length * rect.height);
                EditorUtility.DisplayCustomMenu(rect, contents, idx, (userData, contents, selected) =>
                {
                    AddToFields(varPrefixIndex, contents[selected]);
                }, null);
                mShowSelectTypeMenu = false;
            }
        }

        public override void OnInspectorGUI()
        {
            CheckAndInitFields();
            serializedObject.Update();
            EditorGUILayout.BeginVertical();

            bool disableAct = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying;
            if (disableAct)
            {
                EditorGUILayout.HelpBox("Wiatting for compiling or updating...", MessageType.Warning);
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(disableAct);
            var btnHeight = GUILayout.Height(30);
            if (GUILayout.Button(generateVarBtTitle, highlightBtStyle, btnHeight)) //生成脚本
            {
                GenerateUIFormVariables(uiForm);
            }

            if (GUILayout.Button(bindVarBtTitle, btnHeight)) //绑定变量
            {
                SerializeFieldProperties(serializedObject, uiForm.SerializeFieldArr);
            }

            if (GUILayout.Button(openVarCodeBtTitle, btnHeight))
            {
                var uiFormClassName = uiForm.GetType().Name;
                string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UISerializeFieldDir, Utility.Text.Format("{0}.Variables.cs", uiFormClassName));
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }
            if (GUILayout.Button(openUiLogicBtTitle, highlightBtStyle, btnHeight))
            {
                var monoScript = MonoScript.FromMonoBehaviour(uiForm);
                string scriptFile = AssetDatabase.GetAssetPath(monoScript);
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("box");
            if (EditorGUILayout.LinkButton(helpTitle))
            {
                EditorUtility.DisplayDialog(helpTitle, helpDoc, "OK");
                GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All"))
            {
                mFields.ClearArray();
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < mFields.arraySize; i++)
            {
                var itemRect = EditorGUILayout.BeginHorizontal();
                var item = mFields.GetArrayElementAtIndex(i);
                var varNameProperty = item.FindPropertyRelative("VarName");
                var varTypeProperty = item.FindPropertyRelative("VarType");
                var targetsProperty = item.FindPropertyRelative("Targets");
                var varPrefixProperty = item.FindPropertyRelative("VarPrefix");

                int targetsCount = targetsProperty != null ? targetsProperty.arraySize : 0;
                bool foldoutItem = (i == mCurFoldoutItemIdx);
                if (GUILayout.Button(Utility.Text.Format(foldoutItem ? "▼{0} [{1}]" : "▶{0} [{1}]", i, targetsCount), EditorStyles.label, GUILayout.Width(50)))
                {
                    mCurFoldoutItemIdx = mCurFoldoutItemIdx == i ? -1 : i;
                }
                prefixContent.text = GetVarPrefix(varPrefixProperty.intValue);
                if (EditorGUILayout.DropdownButton(prefixContent, FocusType.Passive, GUILayout.Width(fieldPrefixWidth)))
                {
                    GenericMenu popMenu = new GenericMenu();
                    for (int varPrefixIdx = 0; varPrefixIdx < varPrefixArr.Length; varPrefixIdx++)
                    {
                        var varPrefix = varPrefixArr[varPrefixIdx];
                        popMenu.AddItem(new GUIContent(varPrefix), varPrefixIdx == varPrefixProperty.intValue, selectObj =>
                        {
                            varPrefixProperty.intValue = (int)selectObj;
                            serializedObject.ApplyModifiedProperties();
                        }, varPrefixIdx);
                    }
                    popMenu.ShowAsContext();
                }
                typeContent.text = varTypeProperty.stringValue;
                if (EditorGUILayout.DropdownButton(typeContent, FocusType.Passive, GUILayout.MaxWidth(fieldTypeWidth)))
                {
                    GenericMenu popMenu = new GenericMenu();
                    var popContens = GetPopupContents(targetsProperty);
                    foreach (var tpName in popContens)
                    {
                        popMenu.AddItem(new GUIContent(tpName), tpName.CompareTo(varTypeProperty.stringValue) == 0, selectObj =>
                        {
                            varTypeProperty.stringValue = selectObj.ToString();
                            serializedObject.ApplyModifiedProperties();
                        }, tpName);
                    }
                    popMenu.ShowAsContext();
                }

                varNameProperty.stringValue = GUILayout.TextField(varNameProperty.stringValue, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("+", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    InsertField(i + 1);
                }
                if (GUILayout.Button("-", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    RemoveField(i);
                }

                EditorGUILayout.EndHorizontal();
                if (foldoutItem && i < mFields.arraySize)
                {
                    item = mFields.GetArrayElementAtIndex(i);
                    targetsProperty = item.FindPropertyRelative("Targets");

                    mCurFieldIdx = i;
                    ReorderableList reorderableList = mReorderableList[i];
                    if (reorderableList == null)
                    {
                        reorderableList = new ReorderableList(serializedObject, targetsProperty, true, false, true, true);
                        reorderableList.drawElementCallback = DrawVariableTargets;
                        mReorderableList[i] = reorderableList;
                    }
                    else
                    {
                        reorderableList.serializedProperty = targetsProperty;
                    }
                    reorderableList.DoLayoutList();
                }
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.PrefixLabel("UI Animations:");
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(m_UIOpenAnim);
                if (EditorGUILayout.DropdownButton(dropdownBtContent, FocusType.Passive, GUILayout.Width(100)))
                {
                    ShowDOTweenSequenceDP(m_UIOpenAnim);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(m_UICloseAnim);
                if (EditorGUILayout.DropdownButton(dropdownBtContent, FocusType.Passive, GUILayout.Width(100)))
                {
                    ShowDOTweenSequenceDP(m_UICloseAnim);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
        private void ShowDOTweenSequenceDP(SerializedProperty property)
        {
            if (property.objectReferenceValue == null) return;
            var currentSeq = (property.objectReferenceValue as DOTweenSequence);
            var seqArr = currentSeq.GetComponents<DOTweenSequence>();
            var dropdownMenu = new GenericMenu();
            for (var menuIndex = 0; menuIndex < seqArr.Length; menuIndex++)
            {
                var item = seqArr[menuIndex];
                dropdownMenu.AddItem(new GUIContent(Utility.Text.Format("DOTweenSequence {0}", menuIndex)), currentSeq == item, () =>
                {
                    property.objectReferenceValue = item;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            dropdownMenu.ShowAsContext();
        }
        /// <summary>
        /// 生成UI脚本.cs
        /// </summary>
        /// <param name="uiForm"></param>
        private void GenerateUIFormVariables(UIFormBase uiForm)
        {
            if (uiForm == null) return;

            var monoScript = MonoScript.FromMonoBehaviour(uiForm);
            var uiFormClassName = monoScript.GetClass().Name;
            string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UISerializeFieldDir, Utility.Text.Format("{0}.Variables.cs", uiFormClassName));
            var fields = uiForm.SerializeFieldArr;
            if (fields == null || fields.Length <= 0)
            {
                AssetDatabase.DeleteAsset(scriptFile);
                return;
            }

            var matchResult = Regex.Match(monoScript.text, Utility.Text.Format("partial[\\s]+class[\\s]+{0}", uiFormClassName));
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            if (!matchResult.Success)
            {
                EditorUtility.DisplayDialog("生成UI变量失败!", Utility.Text.Format("请先手动为{0}类添加'partial'修饰符!\n{1}", uiFormClassName, scriptPath), "OK");
                return;
            }
            List<string> nameSpaceList = new List<string> { "UnityEngine" };//默认自带的名字空间
            List<string> fieldList = new List<string>();
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.VarType) || string.IsNullOrWhiteSpace(field.VarName)) continue;
                var varType = GetSampleType(field.VarType);
                if (varType == null) continue;

                if (!string.IsNullOrEmpty(varType.Namespace) && !nameSpaceList.Contains(varType.Namespace))
                {
                    nameSpaceList.Add(varType.Namespace);
                }
                bool isArray = field.Targets.Length > 1;

                var varPrefix = GetVarPrefix(field.VarPrefix);
                string serializeFieldPrefix = "[SerializeField] ";
                string fieldLine;
                if (isArray)
                {
                    fieldLine = Utility.Text.Format("{0}{1} {2}[] {3} = null;", serializeFieldPrefix, varPrefix, varType.Name, field.VarName);
                }
                else
                {
                    fieldLine = Utility.Text.Format("{0}{1} {2} {3} = null;", serializeFieldPrefix, varPrefix, varType.Name, field.VarName);
                }
                fieldList.Add(fieldLine);
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("//---------------------------------");
            stringBuilder.AppendLine("//此文件由工具自动生成,请勿手动修改");
            stringBuilder.AppendLine($"//更新自:{UnityEngine.SystemInfo.deviceName}");
            //stringBuilder.AppendLine($"//更新时间:{DateTime.Now}");
            stringBuilder.AppendLine("//---------------------------------");
            foreach (var item in nameSpaceList)
            {
                stringBuilder.AppendLine(Utility.Text.Format("using {0};", item));
            }
            string uiFormClassNameSpace = monoScript.GetClass().Namespace;
            bool hasNameSpace = !string.IsNullOrWhiteSpace(uiFormClassNameSpace);
            if (hasNameSpace)
            {
                stringBuilder.AppendLine(Utility.Text.Format("namespace {0}", uiFormClassNameSpace));
                stringBuilder.AppendLine("{");
            }
            stringBuilder.AppendLine(Utility.Text.Format("public partial class {0}", uiFormClassName));
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("\t[Space(10)]");
            stringBuilder.AppendLine("\t[Header(\"UI Variables:\")]");
            foreach (var item in fieldList)
            {
                stringBuilder.AppendLine("\t" + item);
            }

            stringBuilder.AppendLine("}");
            if (hasNameSpace) stringBuilder.AppendLine("}");
            if (!Directory.Exists(ConstEditor.UISerializeFieldDir))
                Directory.CreateDirectory(ConstEditor.UISerializeFieldDir);
            File.WriteAllText(scriptFile, stringBuilder.ToString());
            EditorPrefs.SetBool(REFRESH_BIND, true);
            AssetDatabase.Refresh();
        }
        private void SerializeFieldProperties(SerializedObject serializedObject, SerializeFieldData[] fields)
        {
            EditorPrefs.SetBool(REFRESH_BIND, false);
            if (serializedObject == null)
            {
                Debug.LogError("生成UI SerializedField失败, serializedObject为null");
                return;
            }

            foreach (var item in fields)
            {
                string varName = item.VarName;
                string varType = item.VarType;
                bool isGameObject = varType.CompareTo(typeof(GameObject).FullName) == 0;
                var property = serializedObject.FindProperty(varName);
                if (property == null) continue;
                if (item.Targets.Length <= 1)
                {
                    var itemGo = item.Targets[0];
                    if (itemGo == null)
                    {
                        GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0}, GameObject引用丢失!########", varName));
                        continue;
                    }
                    property.objectReferenceValue = isGameObject ? itemGo : itemGo.GetComponent(GetSampleType(varType));
                }
                else if (property.isArray)
                {
                    property.ClearArray();
                    for (int i = 0; i < item.Targets.Length; i++)
                    {
                        if (i >= property.arraySize)
                        {
                            property.InsertArrayElementAtIndex(i);
                        }
                        var itemGo = item.Targets[i];
                        if (itemGo == null)
                        {
                            GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0},索引为{1}的GameObject引用丢失!########", varName, i));
                            continue;
                        }
                        property.GetArrayElementAtIndex(i).objectReferenceValue = isGameObject ? itemGo : itemGo.GetComponent(GetSampleType(varType));
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void CheckAndInitFields()
        {
            if (uiForm.SerializeFieldArr == null)
            {
                uiForm.SerializeFieldArr = new SerializeFieldData[0];
                mFields = serializedObject.FindProperty("_fields");
            }

            if (mFields.arraySize != mReorderableList.Length)
            {
                if (mFields.arraySize > mReorderableList.Length)
                {
                    for (int i = mReorderableList.Length; i < mFields.arraySize; i++)
                    {
                        ArrayUtility.Insert(ref mReorderableList, mReorderableList.Length, null);
                    }
                }
                else
                {
                    for (int i = mFields.arraySize; i < mReorderableList.Length; i++)
                    {
                        ArrayUtility.RemoveAt(ref mReorderableList, mReorderableList.Length - 1);
                    }
                }
            }
        }

        private void InsertField(int idx)
        {
            Undo.RecordObject(uiForm, uiForm.name);
            mFields.InsertArrayElementAtIndex(idx);
            ArrayUtility.Insert(ref mReorderableList, idx, null);
            var lastField = mFields.GetArrayElementAtIndex(idx);
            if (lastField != null)
            {
                var lastVarName = lastField.FindPropertyRelative("VarName");
                if (!string.IsNullOrEmpty(lastVarName.stringValue))
                {
                    lastVarName.stringValue += idx.ToString();
                }
            }
        }
        private void RemoveField(int idx)
        {
            Undo.RecordObject(uiForm, uiForm.name);
            mFields.DeleteArrayElementAtIndex(idx);
            ArrayUtility.RemoveAt(ref mReorderableList, idx);
        }
        private void DrawVariableTargets(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying);
            var field = mFields.GetArrayElementAtIndex(mCurFieldIdx);
            var targetsProperty = field.FindPropertyRelative("Targets");
            var targetProperty = targetsProperty.GetArrayElementAtIndex(index);
            EditorGUI.LabelField(rect, index.ToString());
            rect.xMin += 50;
            EditorGUI.ObjectField(rect, targetProperty, GUIContent.none);
            EditorGUI.EndDisabledGroup();
        }
        private static string[] GetPopupContents(GameObject[] targets)
        {
            if (targets == null || targets.Length <= 0)
            {
                return new string[0];
            }
            var typeNames = GetIntersectionComponents(targets);
            if (typeNames == null || typeNames.Length <= 0)
            {
                return new string[0];
            }
            ArrayUtility.Insert(ref typeNames, 0, typeof(GameObject).FullName);
            return typeNames;
        }
        private static string[] GetPopupContents(SerializedProperty targets)
        {
            var goArr = new GameObject[targets.arraySize];
            for (int i = 0; i < targets.arraySize; i++)
            {
                var pp = targets.GetArrayElementAtIndex(i);
                goArr[i] = (pp != null && pp.objectReferenceValue != null) ? (pp.objectReferenceValue as GameObject) : null;
            }
            return GetPopupContents(goArr);
        }
        private static string[] GetIntersectionComponents(GameObject[] targets)
        {
            var firstItm = targets[0];
            if (firstItm == null)
            {
                return new string[0];
            }
            var coms = firstItm.GetComponents(typeof(Component));
            coms = coms.Distinct().ToArray();//去重

            for (int i = coms.Length - 1; i >= 1; i--)
            {
                var comType = coms[i].GetType().FullName;
                bool allContains = true;
                for (int j = 1; j < targets.Length; j++)
                {
                    var target = targets[j];
                    if (target == null) return new string[0];
                    var tComs = target.GetComponents(typeof(Component));
                    bool containsType = false;
                    for (int k = 0; k < tComs.Length; k++)
                    {
                        if (tComs[k].GetType().FullName.CompareTo(comType) == 0)
                        {
                            containsType = true;
                            break;
                        }
                    }
                    allContains &= containsType;
                    if (!allContains) break;
                }
                if (!allContains)
                {
                    ArrayUtility.RemoveAt(ref coms, i);
                }
            }
            string[] typesArr = new string[coms.Length];
            for (int i = 0; i < coms.Length; i++)
            {
                typesArr[i] = coms[i].GetType().FullName;
            }
            return typesArr;
        }

        /// <summary>
        /// 生成一个与变量列表里不重名的变量名
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GenerateFieldName(SerializeFieldData[] fields, GameObject[] targets)
        {
            var go = targets[0];
            string varName = Regex.Replace(go.name, "[^\\w]", string.Empty);
            if (targets.Length > 1) varName += arrFlag;

            if (fields == null || fields.Length == 0)
            {
                return GetFieldVarName(varName);
            }
            bool contains = false;
            foreach (SerializeFieldData item in fields)
            {
                if (item != null && item.VarName.CompareTo(varName) == 0)
                {
                    contains = true;
                    break;
                }
            }

            if (contains)
            {
                varName = Utility.Text.Format("{0}_{1}", varName, go.transform.GetSiblingIndex());
            }

            return GetFieldVarName(varName);
        }
        private static string GetFieldVarName(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            return Utility.Text.Format("var{0}{1}", str[..1].ToUpper(), str[1..]);
        }
    }

}
#endif