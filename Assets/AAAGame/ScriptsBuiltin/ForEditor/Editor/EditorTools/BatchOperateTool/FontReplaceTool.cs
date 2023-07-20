using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("替换字体", typeof(BatchOperateToolEditor), 0)]
    public class FontReplaceTool : UtilitySubToolBase
    {
        public override string AssetSelectorTypeFilter => "t:prefab t:folder";

        public override string DragAreaTips => "拖拽添加Prefab文件或文件夹";

        protected override Type[] SupportAssetTypes => new Type[] { typeof(GameObject) };


        UnityEngine.Font textFont;
        TMP_FontAsset tmpFont;
        TMP_SpriteAsset tmpFontSpriteAsset;
        TMP_StyleSheet tmpFontStyleSheet;

        public FontReplaceTool(BatchOperateToolEditor ownerEditor) : base(ownerEditor)
        {
        }

        public override void DrawBottomButtonsPanel()
        {
            if (GUILayout.Button("一键替换", GUILayout.Height(30)))
            {
                ReplaceFont();
            }
        }


        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                textFont = EditorGUILayout.ObjectField("Text字体替换:", textFont, typeof(UnityEngine.Font), false) as UnityEngine.Font;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginVertical("box");
            {
                tmpFont = EditorGUILayout.ObjectField("TextMeshPro字体替换:", tmpFont, typeof(TMP_FontAsset), false) as TMP_FontAsset;
                tmpFontSpriteAsset = EditorGUILayout.ObjectField("Sprite Asset替换:", tmpFontSpriteAsset, typeof(TMP_SpriteAsset), false) as TMP_SpriteAsset;
                tmpFontStyleSheet = EditorGUILayout.ObjectField("Style Sheet替换:", tmpFontStyleSheet, typeof(TMP_StyleSheet), false) as TMP_StyleSheet;

                EditorGUILayout.EndVertical();
            }
        }


        private void ReplaceFont()
        {
            var prefabs = OwnerEditor.GetSelectedAssets();
            if (prefabs == null || prefabs.Count < 1) return;

            int taskIdx = 0;
            int totalTaskCount = prefabs.Count;
            bool batTmpfont = tmpFont != null || tmpFontSpriteAsset != null || tmpFontStyleSheet != null;
            foreach (var item in prefabs)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(item); //PrefabUtility.LoadPrefabContents(item);
                if (pfb == null) continue;
                EditorUtility.DisplayProgressBar($"进度({taskIdx++}/{totalTaskCount})", item, taskIdx / (float)totalTaskCount);
                bool hasChanged = false;
                if (textFont != null)
                {
                    foreach (var textCom in pfb.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    {
                        textCom.font = textFont;
                        hasChanged = true;
                    }
                }
                if (batTmpfont)
                {
                    foreach (var tmpTextCom in pfb.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        if (tmpFont != null) tmpTextCom.font = tmpFont;
                        if (tmpFontSpriteAsset != null) tmpTextCom.spriteAsset = tmpFontSpriteAsset;
                        if (tmpFontStyleSheet != null) tmpTextCom.styleSheet = tmpFontStyleSheet;
                        hasChanged = true;
                    }
                }
                if (hasChanged)
                {
                    PrefabUtility.SavePrefabAsset(pfb);
                }
            }
            EditorUtility.ClearProgressBar();
        }
    }
}

