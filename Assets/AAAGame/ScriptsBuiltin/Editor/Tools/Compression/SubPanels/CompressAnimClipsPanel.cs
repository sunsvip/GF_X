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

        private readonly Type[] _supportAssetTypes = { typeof(AnimationClip) };
        protected override Type[] SupportAssetTypes => _supportAssetTypes;

        private int _precision = 3;
        private float _positionAllowError = 0.02f;
        private float _rotationAllowError = 0.01f;
        private float _scaleAllowError = 0.05f;
        private bool _enableAccurate;

        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Float precision", GUILayout.Width(120));
                _precision = EditorGUILayout.IntSlider(_precision, 1, 6);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Position Allow Error", GUILayout.Width(120));
                _positionAllowError = EditorGUILayout.Slider(_positionAllowError, 0.001f, 0.5f);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Rotation Allow Error", GUILayout.Width(120));
                _rotationAllowError = EditorGUILayout.Slider(_rotationAllowError, 0.001f, 0.5f);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Scale Allow Error", GUILayout.Width(120));
                _scaleAllowError = EditorGUILayout.Slider(_scaleAllowError, 0.001f, 0.5f);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                _enableAccurate = EditorGUILayout.Toggle("Accurate end point nodes", _enableAccurate);
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
            AnimationClipOptimizeService.Optimize(animClips, _precision, _positionAllowError, _rotationAllowError, _scaleAllowError, _enableAccurate);
        }
    }
}

