using System;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UGF.EditorTools
{
    internal static class SceneQuickOpenService
    {
        internal readonly struct SceneEntry
        {
            public SceneEntry(string assetPath, string displayName)
            {
                AssetPath = assetPath;
                DisplayName = displayName;
            }

            public string AssetPath { get; }
            public string DisplayName { get; }
        }

        public static List<SceneEntry> GetSceneEntries()
        {
            var sceneEntries = new List<SceneEntry>();
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ConstEditor.ScenePath });
            var rootPath = Utility.Path.GetRegularPath(ConstEditor.ScenePath).TrimEnd('/');
            for (var i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                var sceneDirectory = Path.GetDirectoryName(scenePath) ?? string.Empty;
                var fileDir = Utility.Path.GetRegularPath(sceneDirectory).TrimEnd('/');
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var displayName = sceneName;
                if (!string.Equals(rootPath, fileDir, StringComparison.Ordinal))
                {
                    var sceneDir = fileDir.Length > rootPath.Length
                        ? fileDir.Substring(rootPath.Length).TrimStart('/')
                        : fileDir;
                    displayName = $"{sceneDir}/{sceneName}";
                }

                sceneEntries.Add(new SceneEntry(scenePath, displayName));
            }

            return sceneEntries;
        }

        public static bool TryOpenScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.IsValid() && currentScene.isDirty)
            {
                var optionIndex = EditorUtility.DisplayDialogComplex(
                    "警告",
                    $"当前场景{currentScene.name}未保存。切换场景将以单场景模式打开目标场景，并关闭当前已加载的其它场景。是否保存?",
                    "保存",
                    "取消",
                    "不保存");
                switch (optionIndex)
                {
                    case 0:
                        if (!EditorSceneManager.SaveOpenScenes())
                        {
                            return false;
                        }
                        break;
                    case 1:
                        return false;
                }
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return true;
        }
    }
}
