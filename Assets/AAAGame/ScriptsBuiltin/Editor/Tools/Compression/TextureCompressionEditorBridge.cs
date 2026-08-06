using System;
using System.Reflection;
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class TextureCompressionEditorBridge
    {
        private static MethodInfo s_GetTextureFormatOptionsMethod;
        private static MethodInfo s_GetImportWarningsMethod;
        private static MethodInfo s_GetStorageMemorySizeLongMethod;

        internal static void InitializeTextureFormatOptions(out int[] formatValues, out string[] formatDisplayOptions)
        {
            var parameters = new object[] { TextureImporterType.Sprite, EditorUserBuildSettings.activeBuildTarget, null, null };
            var getTextureFormatOptionsMethod = GetTextureFormatOptionsMethod();
            if (getTextureFormatOptionsMethod == null)
            {
                UseFallbackTextureFormats(out formatValues, out formatDisplayOptions);
                return;
            }

            getTextureFormatOptionsMethod.Invoke(null, parameters);
            formatValues = parameters[2] as int[];
            formatDisplayOptions = parameters[3] as string[];
            if (formatValues == null || formatDisplayOptions == null)
            {
                UseFallbackTextureFormats(out formatValues, out formatDisplayOptions);
            }
        }

        internal static bool HasImportWarnings(TextureImporter textureImporter, out string warning)
        {
            var getImportWarningsMethod = GetImportWarningsMethod(textureImporter);
            if (getImportWarningsMethod == null)
            {
                warning = string.Empty;
                return false;
            }

            warning = getImportWarningsMethod.Invoke(textureImporter, null) as string;
            return !string.IsNullOrWhiteSpace(warning);
        }

        internal static bool TryGetStorageMemorySize(Texture texture, out long storageSize)
        {
            var getStorageMemorySizeLongMethod = GetStorageMemorySizeLongMethod();
            if (getStorageMemorySizeLongMethod == null)
            {
                storageSize = 0L;
                return false;
            }

            storageSize = (long)getStorageMemorySizeLongMethod.Invoke(null, new object[] { texture });
            return true;
        }

        private static MethodInfo GetTextureFormatOptionsMethod()
        {
            return s_GetTextureFormatOptionsMethod ??=
                Utility.Assembly.GetType("UnityEditor.TextureImportValidFormats")
                    ?.GetMethod("GetPlatformTextureFormatValuesAndStrings", BindingFlags.Static | BindingFlags.Public);
        }

        private static MethodInfo GetImportWarningsMethod(TextureImporter textureImporter)
        {
            return s_GetImportWarningsMethod ??=
                textureImporter.GetType().GetMethod("GetImportWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static MethodInfo GetStorageMemorySizeLongMethod()
        {
            return s_GetStorageMemorySizeLongMethod ??=
                Utility.Assembly.GetType("UnityEditor.TextureUtil")
                    ?.GetMethod("GetStorageMemorySizeLong", BindingFlags.Public | BindingFlags.Static);
        }

        private static void UseFallbackTextureFormats(out int[] formatValues, out string[] formatDisplayOptions)
        {
            var fallbackFormats = (TextureImporterFormat[])Enum.GetValues(typeof(TextureImporterFormat));
            formatValues = Array.ConvertAll(fallbackFormats, item => (int)item);
            formatDisplayOptions = Array.ConvertAll(fallbackFormats, item => item.ToString());
        }
    }
}
