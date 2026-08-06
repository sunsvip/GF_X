using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationCompressor
{
    public partial class Core
    {
        private int maxDepth;
        private Dictionary<string, Bone> originBoneMap;
        private Dictionary<string, Bone> compressBoneMap;

        private void GenerateOriginalAnimationBoneMap()
        {
            originBoneMap = GenerateBoneMap(originClip);

            maxDepth = -1;
            foreach (var key in originBoneMap.Keys)
            {
                var depth = Util.GetDepth(key);

                if (depth > maxDepth)
                    maxDepth = depth;
            }
        }

        private void GenerateCurrentCompressedAnimationBoneMap()
        {
            compressBoneMap = GenerateBoneMap(compressClip);
        }

        private Dictionary<string, Bone> GenerateBoneMap(AnimationClip clip)
        {
            var boneMap = new Dictionary<string, Bone>();

            // Create bone map
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var path = binding.path;
                var propertyName = binding.propertyName;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                Bone bone;

                if (boneMap.ContainsKey(path) == false)
                    boneMap[path] = new Bone(path);

                bone = boneMap[path];
                bone.SetCurve(propertyName, curve);
            }

            // Set parent
            foreach (var bone in boneMap.Values)
            {
                var upperPath = Util.GetUpperDepth(bone.Path);

                while (string.IsNullOrEmpty(upperPath) == false)
                {
                    if (boneMap.ContainsKey(upperPath))
                    {
                        bone.SetParent(boneMap[upperPath]);
                        break;
                    }

                    upperPath = Util.GetUpperDepth(bone.Path);
                }
            }

            return boneMap;
        }
    }
}