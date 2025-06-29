using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;

namespace UGF.EditorTools
{
    [CustomEditor(typeof(AppSettings))]
    public class AppSettinsInspector : Editor
    {
        GUIContent designResolutionContent;
        GUIContent designResolutionBtnContent;
        private void OnEnable()
        {
            designResolutionContent = new GUIContent("UI Design Resolution", "UI设计分辨率:");
            designResolutionBtnContent = new GUIContent("Conform", "确认修改");
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.BeginHorizontal();
            {
                AppSettings.Instance.DesignResolution = EditorGUILayout.Vector2IntField(designResolutionContent, AppSettings.Instance.DesignResolution);
                if (GUILayout.Button(designResolutionBtnContent, GUILayout.Width(100)))
                {
                    SetDesignResolution(AppSettings.Instance.DesignResolution);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
        }
        private static void SetDesignResolution(Vector2Int designResolution)
        {
            EditorUtility.SetDirty(AppSettings.Instance);
            var launchSceneName = UtilityBuiltin.AssetsPath.GetScenePath("Launch");
            var currentOpenScene = EditorSceneManager.GetActiveScene();
            if (currentOpenScene != null && currentOpenScene.isDirty)
            {
                int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{currentOpenScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                switch (opIndex)
                {
                    case 0:
                        if (!EditorSceneManager.SaveOpenScenes())
                            return;
                        break;
                    case 1:
                        return;
                }
            }
            var launchScene = EditorSceneManager.OpenScene(launchSceneName, OpenSceneMode.Single);
            UIComponent uiCom = null;
            foreach (var item in launchScene.GetRootGameObjects())
            {
                uiCom = item.GetComponentInChildren<UIComponent>();
                if (uiCom != null) break;
            }
            if (uiCom != null)
            {
                var instanceRoot = uiCom.GetType().GetField("m_InstanceRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(uiCom) as RectTransform;
                if (instanceRoot != null)
                {
                    var canvasScaler = instanceRoot.GetComponent<CanvasScaler>();
                    if (canvasScaler != null && canvasScaler.referenceResolution != designResolution)
                    {
                        canvasScaler.referenceResolution = designResolution;
                        EditorSceneManager.SaveScene(launchScene);
                    }
                }
            }

            var gfExtension = UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension");
            var gfExtensionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gfExtension);
            if (gfExtensionPrefab != null)
            {
                var canvasScaler = gfExtensionPrefab.GetComponentInChildren<CanvasScaler>();
                if (canvasScaler != null && canvasScaler.referenceResolution != designResolution)
                {
                    canvasScaler.referenceResolution = designResolution;
                    EditorUtility.SetDirty(gfExtensionPrefab);
                    AssetDatabase.SaveAssetIfDirty(gfExtensionPrefab);
                }
            }
        }
    }
}

#endif
[CreateAssetMenu(fileName = "AppSettings", menuName = "GF/AppSettings [App内置配置参数]")]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class AppSettings : ScriptableObject
{
    private static AppSettings mInstance = null;
    public static AppSettings Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load<AppSettings>("AppSettings");
            }
            return mInstance;
        }
    }
    [Tooltip("debug模式,默认显示debug窗口")]
    public bool DebugMode = false;
    [Tooltip("资源模式: 单机/全热更/需要时热更")]
    public ResourceMode ResourceMode = ResourceMode.Package;
    [Tooltip("热更版本检测URL:")]
    public string CheckVersionUrl = "http://localhost/1_0_0_1/";
    [Tooltip("屏幕设计分辨率:")]
    [HideInInspector] public Vector2Int DesignResolution = new Vector2Int(750, 1334);
    [Tooltip("需要加密的dll列表")]
    public string[] EncryptAOTDlls;
}
