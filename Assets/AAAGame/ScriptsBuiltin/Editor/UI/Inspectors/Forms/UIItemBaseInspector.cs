#if UNITY_EDITOR
using GameFramework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    [CustomEditor(typeof(UIItemBase), true)]
    public sealed class UIItemBaseInspector : UnityEditor.Editor
    {
        private const string RefreshBindKey = "UIITEM_REFRESH_BIND";

        private SerializedProperty _fieldsProperty;
        private UIItemBase _uiItem;
        private GUIContent _bindVariablesButtonContent;
        private GUIContent _generateVariablesButtonContent;
        private GUIContent _openVariableCodeButtonContent;
        private GUIContent _openUiLogicButtonContent;
        private GUIStyle _highlightButtonStyle;
        private UISerializeFieldListView _serializeFieldListView;
        private static UIItemBaseInspector _instance;

        private void OnEnable()
        {
            _instance = this;
            _highlightButtonStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).button);
            _highlightButtonStyle.normal.background = EditorGUIUtility.FindTexture("sv_label_3");
            _highlightButtonStyle.hover.background = EditorGUIUtility.FindTexture("sv_label_2");
            _highlightButtonStyle.active.background = EditorGUIUtility.FindTexture("sv_label_1");
            _highlightButtonStyle.fontStyle = FontStyle.Bold;
            _highlightButtonStyle.fontSize += 2;

            _bindVariablesButtonContent = new GUIContent("绑定变量", "bind components to variables");
            _generateVariablesButtonContent = new GUIContent("生成变量代码", "generate or update variables code");
            _openVariableCodeButtonContent = new GUIContent("查看变量代码", "open variables code in editor");
            _openUiLogicButtonContent = new GUIContent("编辑UI代码", "open ui logic code in editor");

            _serializeFieldListView = new UISerializeFieldListView();
            _uiItem = target as UIItemBase;
            if (_uiItem != null && _uiItem.SerializeFieldArr == null)
            {
                _uiItem.SerializeFieldArr = new SerializeFieldData[0];
            }

            _fieldsProperty = serializedObject.FindProperty("_fields");
            _serializeFieldListView.Initialize(_fieldsProperty);
        }

        [InitializeOnLoadMethod]
        private static void RebindPropertiesDelay()
        {
            if (!EditorPrefs.HasKey(RefreshBindKey))
            {
                return;
            }

            EditorApplication.delayCall += RebindProperties;
        }

        private static void RebindProperties()
        {
            EditorApplication.delayCall -= RebindProperties;
            if (_instance != null && EditorPrefs.HasKey(RefreshBindKey))
            {
                SerializeFieldProperties(_instance.serializedObject, _instance._uiItem.SerializeFieldArr);
            }
        }

        public override void OnInspectorGUI()
        {
            EnsureFieldsInitialized();
            serializedObject.Update();
            EditorGUILayout.BeginVertical();

            bool disableActions = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying;
            if (disableActions)
            {
                EditorGUILayout.HelpBox("Waiting for compiling or updating...", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(disableActions);
            var buttonHeight = GUILayout.Height(30);
            if (GUILayout.Button(_generateVariablesButtonContent, _highlightButtonStyle, buttonHeight))
            {
                GenerateUIItemVariables(_uiItem);
            }

            if (GUILayout.Button(_bindVariablesButtonContent, buttonHeight))
            {
                SerializeFieldProperties(serializedObject, _uiItem.SerializeFieldArr);
            }

            if (GUILayout.Button(_openVariableCodeButtonContent, buttonHeight))
            {
                string uiItemClassName = _uiItem.GetType().Name;
                string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIItemSerializeFiledDir, Utility.Text.Format("{0}.Variables.cs", uiItemClassName));
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }

            if (GUILayout.Button(_openUiLogicButtonContent, _highlightButtonStyle, buttonHeight))
            {
                var monoScript = MonoScript.FromMonoBehaviour(_uiItem);
                string scriptFile = AssetDatabase.GetAssetPath(monoScript);
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal("box");
            if (EditorGUILayout.LinkButton(UIFormBaseInspector.HelpTitle))
            {
                EditorUtility.DisplayDialog(UIFormBaseInspector.HelpTitle, UIFormBaseInspector.HelpDoc, "OK");
                GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All"))
            {
                _fieldsProperty.ClearArray();
            }
            EditorGUILayout.EndHorizontal();

            _serializeFieldListView.Draw(serializedObject, _fieldsProperty, _uiItem);
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void GenerateUIItemVariables(UIItemBase item)
        {
            UIVariableCodeGenerator.Generate(item, ConstEditor.UIItemSerializeFiledDir, RefreshBindKey);
        }

        private static void SerializeFieldProperties(SerializedObject serializedObject, SerializeFieldData[] fields)
        {
            UISerializeFieldBindingService.SerializeFieldProperties(serializedObject, fields, RefreshBindKey);
        }

        private void EnsureFieldsInitialized()
        {
            UISerializeFieldBindingService.EnsureFieldsInitialized(serializedObject, _uiItem, ref _fieldsProperty);
            _serializeFieldListView.Initialize(_fieldsProperty);
        }
    }
}
#endif
