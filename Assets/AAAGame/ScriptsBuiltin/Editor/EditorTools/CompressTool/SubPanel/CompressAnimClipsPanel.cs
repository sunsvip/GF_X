using System;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("压缩动画", typeof(CompressToolEditor), 5)]
    public class CompressAnimClipsPanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:animationclip t:folder";

        public override string DragAreaTips => "拖拽到此处添加文件夹或动画";
        public override string ReadmeText => "降低动画文件中保存的浮点数精度";

        private Type[] mSupportAssetTypes = { typeof(AnimationClip) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        int floatPrecision = 3;//浮点型精度,默认保留3位小数
        public override void DrawSettingsPanel()
        {
            //floatPrecision
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("浮点型精度", GUILayout.Width(120));
                floatPrecision = EditorGUILayout.IntSlider(floatPrecision, 2, 5);
                EditorGUILayout.EndHorizontal();
            }
        }
        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    StartCompressAnimClip();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void StartCompressAnimClip()
        {
            var animClips = GetSelectedAssets();
            CompressTool.OptimizeAnimationClips(animClips, floatPrecision);
        }
    }
}

