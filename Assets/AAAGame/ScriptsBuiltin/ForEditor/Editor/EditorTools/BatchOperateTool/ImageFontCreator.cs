using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UGF.EditorTools;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[EditorToolMenu("艺术字工具", typeof(BatchOperateToolEditor), 1)]
public class ImageFontCreator : UtilitySubToolBase
{
    enum ImageFontType
    {
        Font,
        TextMeshProFont
    }
    const string DefaultCharsFile = "Tools/FontMinify/CustomFontChars.txt";
    const string CharsFileKey = "ImageFontCreator.CharsFilePath";
    public override string AssetSelectorTypeFilter => "t:Texture2D";

    public override string DragAreaTips => "拖拽到此添加艺术字Sprite图集\nSpriteMode必须为Multiple";
    protected override Type[] SupportAssetTypes => new Type[] { typeof(Texture2D) };

    private string m_CharsString;
    private Vector2 m_ScrollPos;
    private string m_CharsFilePath;

    private IList<int> m_CacheUnicodes;
    private int m_FontSize;
    private int charTexInstanceId;
    private SpriteRect[] m_SpriteRects;
    ImageFontType m_FontType = ImageFontType.Font;
    bool m_NormalizeHeight = true;
    Font m_TMPBaseFont;
    public ImageFontCreator(UtilityToolEditorBase ownerEditor) : base(ownerEditor)
    {

    }
    public override void OnEnter()
    {
        base.OnEnter();
        m_CharsFilePath = EditorPrefs.GetString(CharsFileKey, DefaultCharsFile);
        if (m_CacheUnicodes == null) m_CacheUnicodes = new List<int>();
        RefreshCharsUnicodes();
        m_FontSize = 24;
    }
    public override void OnExit()
    {
        base.OnExit();
        charTexInstanceId = 0;
    }
    public override void DrawBottomButtonsPanel()
    {
        if (GUILayout.Button("生成字体", GUILayout.Height(30)))
        {
            GenerateCustomFont();
        }
    }

    public override void DrawSettingsPanel()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("字符文件(相对工程路径):", m_CharsFilePath, EditorStyles.selectionRect);
            if (GUILayout.Button("选择文件", GUILayout.Width(100)))
            {
                m_CharsFilePath = EditorUtilityExtension.OpenRelativeFilePanel("选择字符文件", m_CharsFilePath, "txt");
                if (!string.IsNullOrWhiteSpace(m_CharsFilePath))
                {
                    EditorPrefs.SetString(CharsFileKey, m_CharsFilePath);
                    RefreshCharsUnicodes();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        m_NormalizeHeight = EditorGUILayout.Toggle("统一字符高度:", m_NormalizeHeight);
        m_FontType = (ImageFontType)EditorGUILayout.EnumPopup("字体类型:", m_FontType);
        if (m_FontType == ImageFontType.TextMeshProFont)
        {
            m_TMPBaseFont = EditorGUILayout.ObjectField("Base Font:", m_TMPBaseFont, typeof(Font), false) as Font;
        }
        else
        {
            m_FontSize = EditorGUILayout.IntSlider("字体大小:", m_FontSize, 1, 512);
        }

        EditorGUILayout.LabelField("追加字符:");
        EditorGUI.BeginChangeCheck();
        {
            m_CharsString = EditorGUILayout.TextArea(m_CharsString, GUILayout.Height(50));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshCharsUnicodes();
            }
        }
        DrawSpriteMultiModeSettings();
    }

    private void DrawSpriteMultiModeSettings()
    {
        if (this.OwnerEditor.SelectObjectList != null && this.OwnerEditor.SelectObjectList.Count > 0 && this.OwnerEditor.SelectObjectList[0] is Texture2D tex)
        {
            EditorGUILayout.LabelField("预览:");
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            {
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    var btContent = EditorGUIUtility.TrIconContent(tex);
                    GUILayout.Box(btContent, GUIStyle.none, GUILayout.Width(tex.width), GUILayout.Height(tex.height));
                    var texRect = GUILayoutUtility.GetLastRect();
                    DrawSpritesRect(tex, texRect);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
                EditorGUILayout.EndScrollView();
            }

        }
    }

    private void DrawSpritesRect(Texture2D tex, Rect texRect)
    {
        Handles.BeginGUI();
        {
            var topRight = texRect.position + Vector2.right * tex.width;
            var bottomLeft = texRect.position + Vector2.up * tex.height;
            Handles.DrawLine(texRect.position, topRight);
            Handles.DrawLine(texRect.position, bottomLeft);
            Handles.DrawLine(topRight, topRight + Vector2.up * tex.height);
            Handles.DrawLine(bottomLeft, bottomLeft + Vector2.right * tex.width);
            Handles.EndGUI();
        }
        if (charTexInstanceId != tex.GetInstanceID() || m_SpriteRects == null)
        {
            charTexInstanceId = tex.GetInstanceID();
            var texFact = new SpriteDataProviderFactories();
            texFact.Init();
            var texDataProvider = texFact.GetSpriteEditorDataProviderFromObject(tex);
            texDataProvider.InitSpriteEditorDataProvider();
            m_SpriteRects = texDataProvider.GetSpriteRects();
        }
        if (m_SpriteRects == null || m_SpriteRects.Length < 1 || m_CacheUnicodes == null) return;
        for (int i = 0; i < m_SpriteRects.Length; i++)
        {
            var spRect = m_SpriteRects[i].rect;
            var pos = spRect.position;
            pos.y = tex.height - (pos.y + spRect.height);
            spRect.position = pos;
            spRect.position += texRect.position;
            GUI.Box(spRect, string.Empty, EditorStyles.selectionRect);
            var indexRect = spRect;
            indexRect.size = Vector2.one * 20;

            EditorGUI.DrawRect(indexRect, Color.green * 0.5f);
            GUI.Label(indexRect, $"{i}", EditorStyles.whiteLargeLabel);
            if (m_CacheUnicodes != null && i < m_CacheUnicodes.Count)
            {
                pos = indexRect.position;
                pos.x += spRect.width - 20;
                pos.y += spRect.height - 20;
                indexRect.position = pos;
                EditorGUI.DrawRect(indexRect, Color.black * 0.5f);
                GUI.Label(indexRect, $"'{(char)m_CacheUnicodes[i]}'", EditorStyles.whiteLargeLabel);
            }
        }
    }

    private IList<int> RefreshCharsUnicodes()
    {
        m_CacheUnicodes.Clear();
        string chars = "";
        if (System.IO.File.Exists(m_CharsFilePath))
        {
            chars = System.IO.File.ReadAllText(m_CharsFilePath, System.Text.Encoding.UTF8);
        }
        if (!string.IsNullOrEmpty(m_CharsString))
        {
            chars = Utility.Text.Format("{0}{1}", chars, m_CharsString);
        }
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsHighSurrogate(chars, i) && i + 1 < chars.Length && char.IsLowSurrogate(chars, i + 1))
            {
                m_CacheUnicodes.Add(char.ConvertToUtf32(chars[i], chars[i + 1]));
                i++;
            }
            else
            {
                m_CacheUnicodes.Add(chars[i]);
            }
        }

        return m_CacheUnicodes;
    }
    private void GenerateCustomFont()
    {
        if (this.OwnerEditor.SelectObjectList != null && this.OwnerEditor.SelectObjectList.Count > 0 && this.OwnerEditor.SelectObjectList[0] is Texture2D)
        {
            RefreshCharsUnicodes();
            if (m_CacheUnicodes.Count < 1)
            {
                Debug.LogWarning($"生成艺术字失败: 请先指定字符或字符文件");
                return;
            }


            if (!ParseCharsInfo(m_CacheUnicodes, out CharacterInfo[] charInfoArr, out Texture2D charsTexture, out int maxFontHeight))
            {
                return;
            }
            if (!charsTexture.isReadable)
            {
                var texImporter = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(charsTexture)) as TextureImporter;
                texImporter.isReadable = true;
                texImporter.alphaIsTransparency = true;
                texImporter.SaveAndReimport();
            }

            string outputDir = EditorUtility.SaveFolderPanel("保存到", Application.dataPath, null);
            if (!string.IsNullOrWhiteSpace(outputDir) && Directory.Exists(outputDir))
            {
                string relativePath = Path.GetRelativePath(Application.dataPath, outputDir);
                if (relativePath == ".") relativePath = string.Empty;
                outputDir = Path.Combine("Assets", relativePath);
                string outputFont;

                switch (m_FontType)
                {
                    case ImageFontType.Font:
                        {
                            outputFont = Path.Combine(outputDir, $"{charsTexture.name}_{m_FontSize}.fontsettings");
                            Font newFont;
                            if (!File.Exists(outputFont))
                            {
                                newFont = new Font(charsTexture.name);
                                AssetDatabase.CreateAsset(newFont, outputFont);
                            }
                            newFont = AssetDatabase.LoadAssetAtPath<Font>(outputFont);
                            string outputFontMat = Path.Combine(outputDir, $"{charsTexture.name}.mat");
                            if (!File.Exists(outputFontMat))
                            {
                                var tempFontMat = new Material(Shader.Find("UI/Default Font"));
                                AssetDatabase.CreateAsset(tempFontMat, outputFontMat);
                            }
                            var fontMat = AssetDatabase.LoadAssetAtPath<Material>(outputFontMat);
                            fontMat.shader = Shader.Find("UI/Default Font");
                            fontMat.SetTexture("_MainTex", charsTexture);
                            EditorUtility.SetDirty(fontMat);
                            AssetDatabase.SaveAssetIfDirty(fontMat);
                            newFont.material = fontMat;

                            newFont.characterInfo = charInfoArr;
                            EditorUtility.SetDirty(newFont);
                            AssetDatabase.SaveAssetIfDirty(newFont);
                            Selection.activeInstanceID = newFont.GetInstanceID();
                        }
                        break;
                    case ImageFontType.TextMeshProFont:
                        if (m_TMPBaseFont != null)
                        {
                            outputFont = Path.Combine(outputDir, $"{charsTexture.name}_{m_FontSize}.asset");
                            GenerateTextMeshProFont(charInfoArr, charsTexture, outputFont, maxFontHeight);
                        }
                        break;
                }
            }
        }
    }

    private void GenerateTextMeshProFont(CharacterInfo[] charInfoArr, Texture2D charsTexture, string outputFont, int maxFontHeight)
    {
        var fontAsset = TMP_FontAsset.CreateFontAsset(m_TMPBaseFont, maxFontHeight, 0, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SMOOTH, charsTexture.width, charsTexture.height, AtlasPopulationMode.Static, false);
        AssetDatabase.CreateAsset(fontAsset, outputFont);

        var tmpMat = new Material(Shader.Find("TextMeshPro/Bitmap Custom Atlas"));
        var charsAtlas = UnityEngine.Object.Instantiate<Texture2D>(charsTexture);
        charsAtlas.alphaIsTransparency = true;
        var fileName = Path.GetFileNameWithoutExtension(outputFont);
        tmpMat.name = Utility.Text.Format("{0}_mat", fileName);
        tmpMat.mainTexture = charsAtlas;
        charsAtlas.name = Utility.Text.Format("{0}_tex", fileName);
        fontAsset.atlas = charsAtlas;
        fontAsset.material = tmpMat;
        fontAsset.atlasTextures = new Texture2D[] { charsAtlas };
        fontAsset.characterTable.Clear();
        fontAsset.glyphTable.Clear();
        for (int i = 0; i < charInfoArr.Length; i++)
        {
            var charInfo = charInfoArr[i];
            var glyph = CharacterInfo2Glyph(i, charInfo, charsAtlas.width, charsAtlas.height);
            fontAsset.characterTable.Add(new TMP_Character((uint)charInfo.index, glyph));
            fontAsset.glyphTable.Add(glyph);
        }
        var faceInfo = fontAsset.faceInfo;
        faceInfo.familyName = fileName;
        faceInfo.lineHeight = faceInfo.ascentLine = maxFontHeight;
        faceInfo.baseline = faceInfo.descentLine = 0;
        fontAsset.faceInfo = faceInfo;
        var fontSettings = fontAsset.creationSettings;
        fontSettings.referencedFontAssetGUID = null;
        fontSettings.sourceFontFileGUID = null;
        fontSettings.sourceFontFileName = null;

        AssetDatabase.AddObjectToAsset(charsAtlas, fontAsset);
        AssetDatabase.AddObjectToAsset(tmpMat, fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssetIfDirty(fontAsset);
        Selection.activeInstanceID = fontAsset.GetInstanceID();
    }
    private UnityEngine.TextCore.Glyph CharacterInfo2Glyph(int i, CharacterInfo charInfo, int atlasWidth, int atlasHeight)
    {
        var glyph = new UnityEngine.TextCore.Glyph((uint)i, new UnityEngine.TextCore.GlyphMetrics(charInfo.glyphWidth, charInfo.glyphHeight, 0, charInfo.glyphHeight, charInfo.glyphWidth),
            new UnityEngine.TextCore.GlyphRect((int)(charInfo.uvBottomLeft.x * atlasWidth), (int)(charInfo.uvBottomLeft.y * atlasHeight), charInfo.glyphWidth, charInfo.glyphHeight));
        return glyph;
    }
    private bool ParseCharsInfo(IList<int> unicodes, out CharacterInfo[] charInfoArr, out Texture2D charsTexture, out int maxHeight)
    {
        charInfoArr = null;
        charsTexture = null;
        maxHeight = 0;
        if (unicodes == null || unicodes.Count < 1)
        {
            return false;
        }
        charsTexture = OwnerEditor.SelectObjectList[0] as Texture2D;
        var texSize = new Vector2Int(charsTexture.width, charsTexture.height);
        var texFact = new SpriteDataProviderFactories();
        texFact.Init();
        var texDataProvider = texFact.GetSpriteEditorDataProviderFromObject(charsTexture);
        texDataProvider.InitSpriteEditorDataProvider();
        var spRects = texDataProvider.GetSpriteRects();
        int count = Mathf.Min(unicodes.Count, spRects.Length);
        charInfoArr = new CharacterInfo[count];
        for (int i = 0; i < count; i++)
        {
            var spRect = spRects[i].rect;

            if (spRect.height > maxHeight)
            {
                maxHeight = (int)spRect.height;
            }
        }

        for (int i = 0; i < count; i++)
        {
            var spRect = spRects[i].rect;
            var spHeight = m_NormalizeHeight ? maxHeight : spRect.height;
            var spRectMax = spRect.max;
            if (m_NormalizeHeight) spRectMax.y = spRect.min.y + spHeight;
            var uvMin = spRect.min / texSize;
            var uvMax = spRectMax / texSize;
            float fontHeight = m_FontSize;
            float fontScale = m_FontSize / spHeight;
            int charBearing = 0;
            if (m_FontType == ImageFontType.TextMeshProFont)
            {
                fontHeight = spHeight;
                fontScale = 1;
                charBearing = Mathf.RoundToInt(spHeight * 1.5f);
            }
            var charInfo = new CharacterInfo
            {
                index = unicodes[i],
                uvBottomLeft = uvMin,
                uvBottomRight = new Vector2(uvMax.x, uvMin.y),
                uvTopLeft = new Vector2(uvMin.x, uvMax.y),
                uvTopRight = uvMax,
                minX = 0,
                minY = -(int)(fontHeight * 0.5f),//居中偏移量
                advance = (int)(spRect.width * fontScale),
                glyphWidth = (int)(spRect.width * fontScale),
                glyphHeight = (int)fontHeight,
                bearing = charBearing,
            };
            charInfoArr[i] = charInfo;
        }
        return true;
    }
}
