using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace UGF.EditorTools
{
    public class EditorUtilityExtension
    {
        public static string OpenRelativeFilePanel(string title, string relativeFilePath, string fileExt)
        {
            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            var curFullPath = !string.IsNullOrWhiteSpace(relativeFilePath) ? Path.Combine(rootPath, relativeFilePath) : rootPath;
            var selectPath = EditorUtility.OpenFilePanel(title, Path.GetDirectoryName(curFullPath), fileExt);

            return string.IsNullOrWhiteSpace(selectPath) ? selectPath : Path.GetRelativePath(rootPath, selectPath);
        }
        /// <summary>
        /// 选择相对工程路径文件夹
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="relativePath">默认打开的路径(相对路径)</param>
        /// <returns></returns>
        public static string OpenRelativeFolderPanel(string title, string relativePath)
        {
            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            var curFullPath = !string.IsNullOrWhiteSpace(relativePath) ? Path.Combine(rootPath, relativePath) : rootPath;
            var selectPath = EditorUtility.OpenFolderPanel(title, curFullPath, null);

            return string.IsNullOrWhiteSpace(selectPath) ? selectPath : Path.GetRelativePath(rootPath, selectPath);
        }

        /// <summary>
        /// 打开UnityEditor内置文件选择界面
        /// </summary>
        /// <param name="assetTp"></param>
        /// <param name="searchFilter"></param>
        /// <param name="onObjectSelectorClosed"></param>
        /// <param name="objectSelectorID"></param>
        /// <returns></returns>
        public static bool OpenAssetSelector(Type assetTp, string searchFilter = null, Action<UnityEngine.Object> onObjectSelectorClosed = null, int objectSelectorID = 0)
        {
            var objSelector = Utility.Assembly.GetType("UnityEditor.ObjectSelector");
            var objSelectorInst = objSelector?.GetProperty("get", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)?.GetValue(objSelector);
            if (objSelectorInst == null)
            {
                Debug.LogWarning("UnityEditor.ObjectSelector.get is null.");
                return false;
            }

            var objSelectorInstTp = objSelectorInst.GetType();
#if UNITY_2022_1_OR_NEWER
            var showFunc = objSelectorInstTp.GetMethod("Show", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[] { typeof(UnityObject), typeof(Type), typeof(UnityObject), typeof(bool), typeof(List<int>), typeof(Action<UnityObject>), typeof(Action<UnityObject>), typeof(bool) }, null);
#else
            var showFunc = objSelectorInstTp.GetMethod("Show", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[] { typeof(UnityObject), typeof(Type), typeof(UnityObject), typeof(bool), typeof(List<int>), typeof(Action<UnityObject>), typeof(Action<UnityObject>)}, null);
#endif
            if (showFunc == null)
            {
                Debug.LogWarning("UnityEditor.ObjectSelector.get.Show function is null.");
                return false;
            }
#if UNITY_2022_1_OR_NEWER
            showFunc.Invoke(objSelectorInst, new object[] { null, assetTp, null, false, null, onObjectSelectorClosed, null, false });
#else
            showFunc.Invoke(objSelectorInst, new object[] { null, assetTp, null, false, null, onObjectSelectorClosed, null });
#endif
            if (!string.IsNullOrEmpty(searchFilter))
            {
                objSelectorInstTp.GetProperty("searchFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(objSelectorInst, searchFilter);
            }

            objSelectorInstTp.GetField("objectSelectorID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(objSelectorInst, objectSelectorID);

            return true;
        }
    }

}
