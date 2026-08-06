using UnityEditor;
using UnityEngine;

namespace AnimationCompressor
{
    public partial class Core
    {
        private Option option = null;
        private AnimationClip originClip = null;
        private AnimationClip compressClip = null;

        private readonly int TotalStep = 4;

        private void UpdateProgressBar(string desc, int step)
        {
            EditorUtility.DisplayProgressBar(nameof(AnimationCompressor), desc, (float)step / TotalStep);
        }

        private void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        public void Compress(AnimationClip originClip, Option option)
        {
            if (originClip == null)
            {
                Debug.Log($"{nameof(AnimationCompressor)} AnimationClip is null");
                return;
            }

            this.option = option;
            this.originClip = originClip;

            ProcessCompress();
        }

        private void ProcessCompress()
        {
            compressClip = Object.Instantiate(originClip);

            EditorUtility.CopySerialized(originClip, compressClip);
            compressClip.ClearCurves();

            PreCompress();
            Compress();
            AssetDatabase.CreateAsset(compressClip, AssetDatabase.GetAssetPath(originClip));

            AssetDatabase.Refresh();
            ClearProgressBar();
            Object.DestroyImmediate(compressClip);
        }

        private void PreCompress()
        {
            UpdateProgressBar(nameof(GenerateOriginalAnimationBoneMap), 1);
            GenerateOriginalAnimationBoneMap();
        }

        private void Compress()
        {
            UpdateProgressBar(nameof(GenerateKeyFrameByCurveFittingPass), 2);
            GenerateKeyFrameByCurveFittingPass();

            // 집어치자 - 거지같음 ㅇㅇㅇㅇㅇㅇㅇㅇ
            //UpdateProgressBar(nameof(KeyFrameReductionPass), 3);
            //KeyFrameReductionPass();

            if (option.EnableAccurateEndPointNodes)
            {
                UpdateProgressBar(nameof(CalculateEndPointNode), 4);
                CalculateEndPointNodePass();
            }
        }
    }
}