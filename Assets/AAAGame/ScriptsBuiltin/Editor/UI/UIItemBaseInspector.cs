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

namespace UGF.EditorTools
{
    [CustomEditor(typeof(UIItemBase), true)]
    public class UIItemBaseInspector : UnityEditor.Editor
    {
        readonly static string[] varPrefixArr = { "private", "protected", "public" };
        const float fieldPrefixWidth = 80;
        const float fieldTypeWidth = 220;
        SerializedProperty mFields;
        ReorderableList[] mReorderableList;
        UIItemBase uiForm;
        int mCurFieldIdx;

        int mCurFoldoutItemIdx = -1;
        GUIContent prefixContent;
        GUIContent typeContent;
        private GUIContent bindVarBtTitle;
        private GUIContent generateVarBtTitle;
        private GUIContent openVarCodeBtTitle;
        private GUIContent openUiLogicBtTitle;
        private GUIStyle highlightBtStyle;

        #region #右键菜单

        const string REFRESH_BIND = "UI_REFRESH_BIND";

        private static string GetVarPrefix(int idx)
        {
            return varPrefixArr[idx];
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
            prefixContent = new GUIContent();
            typeContent = new GUIContent();
            uiForm = (target as UIItemBase);
            if (uiForm != null && uiForm.SerializeFieldArr == null)
            {
                uiForm.SerializeFieldArr = (new SerializeFieldData[0]);
            }
            mFields = serializedObject.FindProperty("_fields");
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
                string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIItemSerializeFiledDir, Utility.Text.Format("{0}.Variables.cs", uiFormClassName));
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
            if (EditorGUILayout.LinkButton(UIFormBaseInspector.helpTitle))
            {
                EditorUtility.DisplayDialog(UIFormBaseInspector.helpTitle, UIFormBaseInspector.helpDoc, "OK");
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
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        /// <summary>
        /// 生成UI脚本.cs
        /// </summary>
        /// <param name="uiForm"></param>
        private void GenerateUIFormVariables(UIItemBase uiForm)
        {
            if (uiForm == null) return;

            var monoScript = MonoScript.FromMonoBehaviour(uiForm);
            var uiFormClassName = monoScript.GetClass().Name;
            string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIItemSerializeFiledDir, Utility.Text.Format("{0}.Variables.cs", uiFormClassName));
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
                if (string.IsNullOrWhiteSpace(field.VarType) || string.IsNullOrWhiteSpace(field.VarName))
                {
                    continue;
                }
                var varType = GetSampleType(field.VarType);
                if (varType == null)
                {
                    continue;
                }
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
            if (!Directory.Exists(ConstEditor.UIItemSerializeFiledDir))
                Directory.CreateDirectory(ConstEditor.UIItemSerializeFiledDir);
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
    }

}
#endif