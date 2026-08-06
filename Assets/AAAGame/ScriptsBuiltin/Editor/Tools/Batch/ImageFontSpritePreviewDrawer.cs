using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class ImageFontSpritePreviewDrawer
    {
        public static void EnsureSpriteRects(Texture2D texture, ImageFontSpritePreviewState state)
        {
            if (texture == null || state == null)
            {
                return;
            }

            state.TextureInstanceId = texture.GetInstanceID();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var dataProvider = factories.GetSpriteEditorDataProviderFromObject(texture);
            if (dataProvider == null)
            {
                state.SpriteRects = Array.Empty<SpriteRect>();
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();
            state.SpriteRects = dataProvider.GetSpriteRects();
            Array.Sort(state.SpriteRects, (left, right) => string.CompareOrdinal(left.name, right.name));
        }

        public static void Draw(Texture2D texture, IList<int> unicodes, ImageFontSpritePreviewState state)
        {
            if (texture == null || state == null)
            {
                return;
            }

            EditorGUILayout.LabelField("预览:");
            state.ScrollPosition = EditorGUILayout.BeginScrollView(state.ScrollPosition);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GetSourceTextureSize(texture, out int sourceWidth, out int sourceHeight);
            float previewWidth = Mathf.Min(sourceWidth, Mathf.Max(1f, EditorGUIUtility.currentViewWidth - 40f));
            float previewHeight = previewWidth * sourceHeight / sourceWidth;
            var reservedRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));
            var textureRect = CalculateTextureRect(reservedRect, sourceWidth, sourceHeight);
            EditorGUI.DrawTextureTransparent(textureRect, texture, ScaleMode.StretchToFill);
            DrawSpriteRects(texture, textureRect, sourceWidth, sourceHeight, unicodes, state);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSpriteRects(Texture2D texture, Rect textureRect, int sourceWidth, int sourceHeight, IList<int> unicodes, ImageFontSpritePreviewState state)
        {
            Handles.BeginGUI();
            var topRight = textureRect.position + Vector2.right * textureRect.width;
            var bottomLeft = textureRect.position + Vector2.up * textureRect.height;
            Handles.DrawLine(textureRect.position, topRight);
            Handles.DrawLine(textureRect.position, bottomLeft);
            Handles.DrawLine(topRight, topRight + Vector2.up * textureRect.height);
            Handles.DrawLine(bottomLeft, bottomLeft + Vector2.right * textureRect.width);
            Handles.EndGUI();

            EnsureSpriteRects(texture, state);

            if (state.SpriteRects == null || state.SpriteRects.Length == 0 || unicodes == null)
            {
                return;
            }

            for (var i = 0; i < state.SpriteRects.Length; i++)
            {
                var spriteRect = ToPreviewRect(state.SpriteRects[i].rect, sourceWidth, sourceHeight, textureRect);
                GUI.Box(spriteRect, string.Empty, EditorStyles.selectionRect);

                var indexRect = spriteRect;
                indexRect.size = Vector2.one * 20;
                EditorGUI.DrawRect(indexRect, Color.green * 0.5f);
                GUI.Label(indexRect, $"{i}", EditorStyles.whiteLargeLabel);
                if (i >= unicodes.Count)
                {
                    continue;
                }

                var position = indexRect.position;
                position.x += spriteRect.width - 20;
                position.y += spriteRect.height - 20;
                indexRect.position = position;
                EditorGUI.DrawRect(indexRect, Color.black * 0.5f);
                GUI.Label(indexRect, $"'{char.ConvertFromUtf32(unicodes[i])}'", EditorStyles.whiteLargeLabel);
            }
        }

        private static Rect CalculateTextureRect(Rect reservedRect, int textureWidth, int textureHeight)
        {
            float scale = Mathf.Min(reservedRect.width / textureWidth, reservedRect.height / textureHeight);
            float width = textureWidth * scale;
            float height = textureHeight * scale;
            return new Rect(
                reservedRect.x + (reservedRect.width - width) * 0.5f,
                reservedRect.y + (reservedRect.height - height) * 0.5f,
                width,
                height);
        }

        private static Rect ToPreviewRect(Rect spriteRect, int textureWidth, int textureHeight, Rect textureRect)
        {
            float scaleX = textureRect.width / textureWidth;
            float scaleY = textureRect.height / textureHeight;
            return new Rect(
                textureRect.x + spriteRect.x * scaleX,
                textureRect.y + (textureHeight - spriteRect.y - spriteRect.height) * scaleY,
                spriteRect.width * scaleX,
                spriteRect.height * scaleY);
        }

        private static void GetSourceTextureSize(Texture2D texture, out int width, out int height)
        {
            width = texture.width;
            height = texture.height;
            var importer = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth > 0 && sourceHeight > 0)
            {
                width = sourceWidth;
                height = sourceHeight;
            }
        }
    }
}
