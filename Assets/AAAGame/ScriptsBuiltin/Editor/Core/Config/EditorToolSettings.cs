#if UNITY_EDITOR
using System.Collections.Generic;

namespace UGF.EditorTools
{
    [UGF.EditorTools.FilePath("ProjectSettings/EditorToolSettings.asset")]
    public class EditorToolSettings : EditorScriptableSingleton<EditorToolSettings>
    {
        //图片压缩工具设置项
        public string CompressImgToolBackupDir;
        public bool CompressImgToolCoverRaw = false;//压缩后的图片直接覆盖原文件
        public string CompressImgToolOutputDir;
        // 图片压缩仅使用本地 pngquant，支持 PNG。
        public int CompressImgToolFastLv = 1;  //取值1-10, 数值越大压缩的速度越快,但压缩比会稍微降低
        public float CompressImgToolQualityLv = 80; //pngquant压缩质量等级,数值越小压缩后图片越小
        public float CompressImgToolQualityMinLv = 0;
        //语言国际化
        public List<int> LanguagesSupport = new List<int>();
        public int LocalizationTranslationProvider = (int)UGF.EditorTools.LocalizationTranslationProvider.Baidu;
        public string LocalizationAiPromptTemplatePath = "Assets/AAAGame/ScriptsBuiltin/Editor/Data/Localization/AIPrompts/LocalizationTranslatePrompt.md";
        public bool LocalizationAiShowDebugCommandWindow = false;
        public string BaiduTransAppId = "";
        public string BaiduTransSecretKey = "";
        public int BaiduTransMaxLength = 2000;
    }
}
#endif
