#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UGF.EditorTools
{
    public partial class MyGameTools : EditorWindow
    {
        #region 通用方法
        public static void FindChildByName(Transform root, string name, ref Transform result)
        {
            if (root.name.StartsWith(name))
            {
                result = root;
                return;
            }

            foreach (Transform child in root)
            {
                FindChildByName(child, name, ref result);
            }
        }
        public static void FindChildrenByName(Transform root, string name, ref List<Transform> result)
        {
            if (root.name.StartsWith(name))
            {
                result.Add(root);

            }

            foreach (Transform child in root)
            {
                FindChildrenByName(child, name, ref result);
            }
        }
        public static string GetNodePath(Transform node, Transform root = null)
        {
            if (node == null)
            {
                return string.Empty;
            }
            Transform curNode = node;
            string path = curNode.name;
            while (curNode.parent != root)
            {
                curNode = curNode.parent;
                path = string.Format("{0}/{1}", curNode.name, path);
            }
            return path;
        }
        #endregion
    }

}
#endif