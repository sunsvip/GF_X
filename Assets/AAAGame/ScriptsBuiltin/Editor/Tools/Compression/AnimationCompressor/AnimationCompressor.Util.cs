using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AnimationCompressor
{
    public static class Util
    {
        /// <summary>
        /// Get animation curve's path depth
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static int GetDepth(string path)
        {
            var matches = Regex.Matches(path, "/");
            return matches.Count;
        }

        /// <summary>
        /// Get upper depth
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetUpperDepth(string path)
        {
            if (path.Contains("/") == false)
                return null;

            var idx = path.LastIndexOf('/');
            var cnt = path.Length - idx;
            return path.Remove(idx, cnt);
        }

        /// <summary>
        /// Get destination output path
        /// </summary>
        /// <param name="originClip"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static string GetOutputPath(AnimationClip originClip)
        {
            if (originClip == null)
                return string.Empty;

            // From asdf/asdf/asdf@atk.FBX
            var originPath = AssetDatabase.GetAssetPath(originClip);
            var split = originPath.Split('.');

            // To asdf/asdf/asdf@atk
            var builder = new StringBuilder();
            for (var i = 0; i < split.Length - 1; i++)
                builder.Append(split[i]);

            // To asdf/asdf/asdf@atk_Compressed.anim
            builder.Append("_Compressed.anim");
            return builder.ToString();
        }

        /// <summary>
        /// Return true, if property is transform key (positio, rotation, scale)
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static bool IsTransformKey(string propertyName)
        {
            switch (propertyName)
            {
                case "m_LocalPosition":
                case "m_LocalPosition.x":
                case "m_LocalPosition.y":
                case "m_LocalPosition.z":

                case "m_LocalRotation":
                case "m_LocalRotation.x":
                case "m_LocalRotation.y":
                case "m_LocalRotation.z":
                case "m_LocalRotation.w":

                case "m_LocalScale":
                case "m_LocalScale.y":
                case "m_LocalScale.z":
                case "m_LocalScale.x":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Return true, if property is rotation key
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static bool IsRotaitonKey(string propertyName)
        {
            switch (propertyName)
            {
                case "m_LocalRotation":
                case "m_LocalRotation.x":
                case "m_LocalRotation.y":
                case "m_LocalRotation.z":
                case "m_LocalRotation.w":
                    return true;

                default:
                    return false;
            }
        }
    }
}