using GameFramework.Resource;
using UnityEngine;

[CreateAssetMenu(fileName = "AppSettings", menuName = "ScriptableObject/AppSettings【App内置配置参数】")]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class AppSettings : ScriptableObject
{
    private static AppSettings mInstance = null;
    public static AppSettings Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load<AppSettings>("AppSettings");
            }
            return mInstance;
        }
    }
    [Tooltip("debug模式,默认显示debug窗口")]
    public bool DebugMode = false;
    [Tooltip("资源模式: 单机/全热更/需要时热更")]
    public ResourceMode ResourceMode = ResourceMode.Package;
    [Tooltip("屏幕设计分辨率:")]
    public Vector2Int DesignResolution = new Vector2Int(750, 1334);
    [Tooltip("需要加密的dll列表")]
    public string[] EncryptAOTDlls;
}
