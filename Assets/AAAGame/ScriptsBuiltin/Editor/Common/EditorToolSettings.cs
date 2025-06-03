#if UNITY_EDITOR
using System.Collections.Generic;

namespace UGF.EditorTools
{
    [UGF.EditorTools.FilePath("ProjectSettings/EditorToolSettings.asset")]
    public class EditorToolSettings : EditorScriptableSignleton<EditorToolSettings>
    {
        //图片压缩工具设置项
        public string CompressImgToolBackupDir;
        public bool CompressImgToolCoverRaw = false;//压缩后的图片直接覆盖原文件
        public string CompressImgToolOutputDir;
        public List<string> CompressImgToolKeys = new List<string>() { "tinypngkey"};
        public List<UnityEngine.Object> CompressImgToolItemList = new List<UnityEngine.Object>();
        public bool CompressImgToolOffline = true;//离线模式; 使用本地压缩工具pngquant压缩(仅支持png,其它格式依然走tinypng在线压缩)
        public int CompressImgToolFastLv = 1;  //取值1-10, 数值越大压缩的速度越快,但压缩比会稍微降低
        public float CompressImgToolQualityLv = 80; //pngquant压缩质量等级,数值越小压缩后图片越小
        public float CompressImgToolQualityMinLv = 0;
        public int CompressImgMode = 0;//图片压缩模式。0:原图压缩模式; 1:Unity内置压缩模式
        public string FontCroppingCharSetsOutput;
        public string FontCroppingCharSetsFile;

        //AI助手
        public string ChatGPTKey = "";
        public float ChatGPTRandomness = 0;//结果随机性.取值0-2,官方默认1
        public int ChatGPTTimeout = 60;//ChatGPT请求超时,秒

        //语言国际化
        public List<int> LanguagesSupport = new List<int>();
        public string BaiduTransAppId = "";
        public string BaiduTransSecretKey = "";
        public int BaiduTransMaxLength = 2000;
    }
}
#endif