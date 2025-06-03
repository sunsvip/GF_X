using GameFramework;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UGF.EditorTools
{
    public partial class ProjectPanelRightClickExtension
    {

        [MenuItem("Assets/GF Tools/Clear Prefabs Missing Scripts", priority = 2)]
        static void ClearMissingScripts()
        {
            var selectObjs = Selection.objects;
            int totalCount = selectObjs.Length;
            for (int i = 0; i < totalCount; i++)
            {
                var item = selectObjs[i];
                EditorUtility.DisplayProgressBar($"Clear missing scripts: [{i}/{totalCount}]", $"清理{item.name}丢失脚本:", i / (float)totalCount);
                var path = AssetDatabase.GetAssetPath(item);
                if (Directory.Exists(path))
                {
                    var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
                    foreach (var guid in prefabs)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        ClearPrefabMissingComponents(assetPath);
                    }
                }
                else if (File.Exists(path) && Path.GetExtension(path).ToLower().CompareTo(".prefab") == 0)
                {
                    ClearPrefabMissingComponents(path);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static void ClearPrefabMissingComponents(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab || type == PrefabAssetType.Variant)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabRoot.GetComponentsInChildren<Transform>(true);
            bool isDirty = false;
            foreach (var node in nodes)
            {
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject) > 0)
                {
                    isDirty = true;
                }
            }
            if (isDirty)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabPath, InteractionMode.AutomatedAction);
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        [MenuItem("Assets/GF Tools/Log Asset Dependencies", priority = 19)]
        static void LogAssetDependencies()
        {
            if (Selection.activeObject == null) return;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path)) return;

            var dependencies = AssetDatabase.GetDependencies(path);
            Debug.Log($"----------------{path} Dependencies---------------");
            foreach (var dependency in dependencies)
            {
                Debug.Log(dependency);
            }
            Debug.Log($"--------------------------------------------------");
        }
#if PSD2UGUI
        [MenuItem("Assets/GF Tools/2D/Auto Sprite Border", priority = 1004)]
        static void AutoSpriteSliceBorder()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                var assetName = AssetDatabase.GUIDToAssetPath(guid);
                var texImpt = AssetImporter.GetAtPath(assetName) as TextureImporter;
                if (texImpt == null || texImpt.textureType != TextureImporterType.Sprite) continue;
                var rawReadable = texImpt.isReadable;
                if (!rawReadable)
                {
                    texImpt.isReadable = true;
                    texImpt.SaveAndReimport();
                }
                var tex = AssetDatabase.LoadAssetAtPath<Sprite>(assetName);
                texImpt.spriteBorder = Psd2UGUI.UGUIParser.CalculateTexture9SliceBorder(tex.texture);
                texImpt.isReadable = rawReadable;
                texImpt.SaveAndReimport();
            }
        }
#endif

        [MenuItem("Assets/GF Tools/Copy Asset Path/Relative Path", priority = 1000)]
        static void CopyAssetRelativePath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 1);
        }
        [MenuItem("Assets/GF Tools/Copy Asset Path/Full Path", priority = 1001)]
        static void CopyAssetFullPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 0);
        }
        [MenuItem("Assets/GF Tools/Copy Asset Path/Assets Name", priority = 1002)]
        static void CopyAssetNameWithoutPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 2);
        }
        [MenuItem("Assets/GF Tools/Create/UIForm Prefab", priority = 1)]
        static void CreateUIFormMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateUIPrefabWithRename(ConstEditor.UIFormTemplate, savePath, "NewUIFormPrefab");
        }
        [MenuItem("Assets/GF Tools/Create/UIDialog Prefab", priority = 2)]
        static void CreateUIDialogMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateUIPrefabWithRename(ConstEditor.UIDialogTemplate, savePath, "NewUIDialogPrefab");
        }
        [MenuItem("Assets/GF Tools/Create/UIForm Prefab And Script", priority = 3)]
        static void CreateUIFormAndScriptMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateUIPrefabWithRename(ConstEditor.UIFormTemplate, savePath, "NewUIFormPrefab", true);
        }
        [MenuItem("Assets/GF Tools/Create/UIDialog Prefab And Script", priority = 4)]
        static void CreateUIDialogAndScriptMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateUIPrefabWithRename(ConstEditor.UIDialogTemplate, savePath, "NewUIDialogPrefab", true);
        }
        [MenuItem("Assets/GF Tools/Create/UIItem Script", priority = 5)]
        static void CreateUIItemScriptMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateUIItemScriptWithRename(ConstEditor.UIItemScriptFileTemplate, savePath, "NewUIItem");
        }
        static void CreateUIPrefabWithRename(string srcAsset, string savePath, string fileName, bool createUIScriptFile = false)
        {
            if (string.IsNullOrEmpty(savePath) || !AssetDatabase.IsValidFolder(savePath) || !File.Exists(srcAsset)) return;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                createUIScriptFile ? ScriptableObject.CreateInstance<DoCreateUIPrefabAndScriptFile>() : ScriptableObject.CreateInstance<DoCreatePrefab>(),
                Utility.Text.Format("{0}.prefab", fileName),
                EditorGUIUtility.FindTexture("Prefab Icon"),
                srcAsset);
        }
        static void CreateUIItemScriptWithRename(string srcAsset, string savePath, string fileName)
        {
            if (string.IsNullOrEmpty(savePath) || !AssetDatabase.IsValidFolder(savePath) || !File.Exists(srcAsset)) return;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateUIItemScriptFile>(),
                Utility.Text.Format("{0}.cs", fileName),
                EditorGUIUtility.FindTexture("cs Script Icon"),
                srcAsset);
        }
        /// <summary>
        /// 复制资源路径到剪贴板
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="copyFullPath"></param>
        private static void CopyAssetsPath2Clipboard(UnityEngine.Object[] assets, int pathMode)
        {
            if (assets == null || assets.Length < 1)
            {
                return;
            }
            StringBuilder strBuilder = new StringBuilder();
            switch (pathMode)
            {
                case 1: //Relative Path
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                case 2:
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrWhiteSpace(itemPath) || !Path.HasExtension(itemPath))
                        {
                            continue;
                        }
                        itemPath = Path.GetFileName(itemPath);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                default: //Full Path
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    foreach (var item in assets)
                    {
                        var itemPath = Path.GetFullPath(AssetDatabase.GetAssetPath(item), projectRoot);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
            }
            var result = strBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
    class DoCreatePrefab : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    class DoCreateUIPrefabAndScriptFile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        const string ADD_SCRIPT_TASK = "ADD_UISCRIPT_TASK";
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);

                    var uiPrefabName = Path.GetFileNameWithoutExtension(pathName);
                    var uiScriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIScriptsPath, uiPrefabName + ".cs");
                    if (File.Exists(uiScriptFile))
                    {
                        Debug.LogWarningFormat("创建UI脚本失败! 文件已存在:{0}", uiScriptFile);
                        return;
                    }
                    if (!File.Exists(ConstEditor.UIScriptFileTemplate))
                    {
                        Debug.LogErrorFormat("创建UI脚本失败! 文件模板不存在:{0}", ConstEditor.UIScriptFileTemplate);
                        return;
                    }
                    var text = File.ReadAllText(ConstEditor.UIScriptFileTemplate, UTF8Encoding.UTF8);
                    text = text.Replace("_CLASS_NAME_", uiPrefabName);
                    File.WriteAllText(uiScriptFile, text, UTF8Encoding.UTF8);
                    AssetDatabase.Refresh();
                    var taskInfo = Utility.Text.Format("{0}|{1}", pathName, uiScriptFile);
                    EditorPrefs.SetString(ADD_SCRIPT_TASK, taskInfo);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        [InitializeOnLoadMethod]
        private static void TaskRefresh()
        {
            if (EditorPrefs.HasKey(ADD_SCRIPT_TASK))
            {
                var infos = EditorPrefs.GetString(ADD_SCRIPT_TASK).Split('|');
                EditorPrefs.DeleteKey(ADD_SCRIPT_TASK);
                if (infos.Length != 2) return;
                var goAssetFile = infos[0];
                var monoScriptFile = infos[1];
                var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(goAssetFile);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(monoScriptFile);
                if (monoScript == null || targetPrefab == null) return;
                var monoType = monoScript.GetClass();
                targetPrefab.GetOrAddComponent(monoType);
            }
        }
    }
    class DoCreateUIItemScriptFile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(pathName);
                var uiScriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIItemScriptsPath, fileName + ".cs");
                if (File.Exists(uiScriptFile))
                {
                    Debug.LogWarningFormat("创建UI脚本失败! 文件已存在:{0}", uiScriptFile);
                    return;
                }
                if (!File.Exists(ConstEditor.UIScriptFileTemplate))
                {
                    Debug.LogErrorFormat("创建UI脚本失败! 文件模板不存在:{0}", ConstEditor.UIScriptFileTemplate);
                    return;
                }
                var text = File.ReadAllText(ConstEditor.UIItemScriptFileTemplate, UTF8Encoding.UTF8);
                text = text.Replace("_CLASS_NAME_", fileName);
                File.WriteAllText(uiScriptFile, text, UTF8Encoding.UTF8);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

