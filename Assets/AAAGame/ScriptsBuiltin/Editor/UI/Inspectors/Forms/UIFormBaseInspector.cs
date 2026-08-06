#if UNITY_EDITOR
using GameFramework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    [CustomEditor(typeof(UIFormBase), true)]
    internal sealed class UIFormBaseInspector : UnityEditor.Editor
    {
        internal const string HelpTitle = "使用说明";
        internal const string HelpDoc = "1.打开UI界面预制体.\n2.右键节点'UIForm Tools'子菜单,添加/移除变量.\n3.在Inspector面板点击功能按钮生成变量代码.";
        private const string RefreshBindKey = "UI_REFRESH_BIND";

        private SerializedProperty _fieldsProperty;
        private UIFormBase _uiForm;
        private GUIContent _bindVariablesButtonContent;
        private GUIContent _generateVariablesButtonContent;
        private GUIContent _openVariableCodeButtonContent;
        private GUIContent _openUiLogicButtonContent;
        private GUIContent _animationSelectorButtonContent;
        private GUIContent _animationNameButtonContent;
        private GUIStyle _highlightButtonStyle;
        private UISerializeFieldListView _serializeFieldListView;

        private SerializedProperty _uiAnimationTypeProperty;
        private SerializedProperty _openAnimationProperty;
        private SerializedProperty _closeAnimationProperty;
        private SerializedProperty _reverseOpenAnimationAsCloseProperty;
        private SerializedProperty _openAnimationNameProperty;
        private SerializedProperty _closeAnimationNameProperty;

        private static UIFormBaseInspector _instance;

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
            _animationSelectorButtonContent = new GUIContent("选择动效", "select dotween sequence component");
            _animationNameButtonContent = new GUIContent(string.Empty, "select animation name");

            _serializeFieldListView = new UISerializeFieldListView();
            _uiForm = target as UIFormBase;
            _uiForm.SerializeFieldArr ??= new SerializeFieldData[0];

            _fieldsProperty = serializedObject.FindProperty("_fields");
            _uiAnimationTypeProperty = serializedObject.FindProperty("m_UIAnimationType");
            _openAnimationProperty = serializedObject.FindProperty("m_OpenAnimation");
            _closeAnimationProperty = serializedObject.FindProperty("m_CloseAnimation");
            _reverseOpenAnimationAsCloseProperty = serializedObject.FindProperty("m_ReverseOpenAnimAsClose");
            _openAnimationNameProperty = serializedObject.FindProperty("m_OpenAnimationName");
            _closeAnimationNameProperty = serializedObject.FindProperty("m_CloseAnimationName");
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
                SerializeFieldProperties(_instance.serializedObject, _instance._uiForm.SerializeFieldArr);
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
                GenerateUIFormVariables(_uiForm);
            }

            if (GUILayout.Button(_bindVariablesButtonContent, buttonHeight))
            {
                SerializeFieldProperties(serializedObject, _uiForm.SerializeFieldArr);
            }

            if (GUILayout.Button(_openVariableCodeButtonContent, buttonHeight))
            {
                string uiFormClassName = _uiForm.GetType().Name;
                string scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UISerializeFieldDir, Utility.Text.Format("{0}.Variables.cs", uiFormClassName));
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }

            if (GUILayout.Button(_openUiLogicButtonContent, _highlightButtonStyle, buttonHeight))
            {
                var monoScript = MonoScript.FromMonoBehaviour(_uiForm);
                string scriptFile = AssetDatabase.GetAssetPath(monoScript);
                InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal("box");
            if (EditorGUILayout.LinkButton(HelpTitle))
            {
                EditorUtility.DisplayDialog(HelpTitle, HelpDoc, "OK");
                GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All"))
            {
                _fieldsProperty.ClearArray();
            }
            EditorGUILayout.EndHorizontal();

            _serializeFieldListView.Draw(serializedObject, _fieldsProperty, _uiForm);

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_uiAnimationTypeProperty);
            if (_uiAnimationTypeProperty.intValue == (int)UIFormAnimationType.Tween)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(_openAnimationProperty);
                    if (EditorGUILayout.DropdownButton(_animationSelectorButtonContent, FocusType.Passive, GUILayout.Width(100)))
                    {
                        UIFormAnimationSelectionMenu.ShowUIAnimation(serializedObject, _openAnimationProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(_closeAnimationProperty);
                    if (EditorGUILayout.DropdownButton(_animationSelectorButtonContent, FocusType.Passive, GUILayout.Width(100)))
                    {
                        UIFormAnimationSelectionMenu.ShowUIAnimation(serializedObject, _closeAnimationProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else if (_uiAnimationTypeProperty.intValue == (int)UIFormAnimationType.Animation)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Open Animation Name");
                    _animationNameButtonContent.text = _openAnimationNameProperty.stringValue;
                    if (EditorGUILayout.DropdownButton(_animationNameButtonContent, FocusType.Passive))
                    {
                        UIFormAnimationSelectionMenu.ShowAnimationNames(serializedObject, _uiForm, _openAnimationNameProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Close Animation Name");
                    _animationNameButtonContent.text = _closeAnimationNameProperty.stringValue;
                    if (EditorGUILayout.DropdownButton(_animationNameButtonContent, FocusType.Passive))
                    {
                        UIFormAnimationSelectionMenu.ShowAnimationNames(serializedObject, _uiForm, _closeAnimationNameProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.PropertyField(_reverseOpenAnimationAsCloseProperty);
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void GenerateUIFormVariables(UIFormBase form)
        {
            UIVariableCodeGenerator.Generate(form, ConstEditor.UISerializeFieldDir, RefreshBindKey);
        }

        private static void SerializeFieldProperties(SerializedObject serializedObject, SerializeFieldData[] fields)
        {
            UISerializeFieldBindingService.SerializeFieldProperties(serializedObject, fields, RefreshBindKey);
        }

        private void EnsureFieldsInitialized()
        {
            UISerializeFieldBindingService.EnsureFieldsInitialized(serializedObject, _uiForm, ref _fieldsProperty);
            _serializeFieldListView.Initialize(_fieldsProperty);
        }
    }
}
#endif
