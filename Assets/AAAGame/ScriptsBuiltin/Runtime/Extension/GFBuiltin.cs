using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class GFBuiltin : MonoBehaviour
{
    public static GFBuiltin Instance { get; private set; }
    public static BaseComponent Base { get; private set; }
    public static ConfigComponent Config { get; private set; }
    public static DataNodeComponent DataNode { get; private set; }
    public static DataTableComponent DataTable { get; private set; }
    public static DebuggerComponent Debugger { get; private set; }
    public static DownloadComponent Download { get; private set; }
    public static EntityComponent Entity { get; private set; }
    public static EventComponent Event { get; private set; }
    public static FsmComponent Fsm { get; private set; }
    public static FileSystemComponent FileSystem { get; private set; }
    public static LocalizationComponent Localization { get; private set; }
    public static NetworkComponent Network { get; private set; }
    public static ProcedureComponent Procedure { get; private set; }
    public static ResourceComponent Resource { get; private set; }
    public static SceneComponent Scene { get; private set; }
    public static SettingComponent Setting { get; private set; }
    public static SoundComponent Sound { get; private set; }
    public static UIComponent UI { get; private set; }
    public static ObjectPoolComponent ObjectPool { get; private set; }
    public static WebRequestComponent WebRequest { get; private set; }
    public static BuiltinViewComponent BuiltinView { get; private set; }
    public static Camera UICamera { get; private set; }

    public static Canvas RootCanvas { get; private set; } = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            var resCom = GameEntry.GetComponent<ResourceComponent>();
            if (resCom != null)
            {
                var resTp = resCom.GetType();
                var m_ResourceMode = resTp.GetField("m_ResourceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                m_ResourceMode.SetValue(resCom, AppSettings.Instance.ResourceMode);
                GFBuiltin.Log($"------------Set ResourceMode:{AppSettings.Instance.ResourceMode}------------");
            }
        }
    }

    private void Start()
    {
        GFBuiltin.Base = GameEntry.GetComponent<BaseComponent>();
        GFBuiltin.Config = GameEntry.GetComponent<ConfigComponent>();
        GFBuiltin.DataNode = GameEntry.GetComponent<DataNodeComponent>();
        GFBuiltin.DataTable = GameEntry.GetComponent<DataTableComponent>();
        GFBuiltin.Debugger = GameEntry.GetComponent<DebuggerComponent>();
        GFBuiltin.Download = GameEntry.GetComponent<DownloadComponent>();
        GFBuiltin.Entity = GameEntry.GetComponent<EntityComponent>();
        GFBuiltin.Event = GameEntry.GetComponent<EventComponent>();
        GFBuiltin.Fsm = GameEntry.GetComponent<FsmComponent>();
        GFBuiltin.Procedure = GameEntry.GetComponent<ProcedureComponent>();
        GFBuiltin.Localization = GameEntry.GetComponent<LocalizationComponent>();
        GFBuiltin.Network = GameEntry.GetComponent<NetworkComponent>();
        GFBuiltin.Resource = GameEntry.GetComponent<ResourceComponent>();
        GFBuiltin.FileSystem = GameEntry.GetComponent<FileSystemComponent>();
        GFBuiltin.Scene = GameEntry.GetComponent<SceneComponent>();
        GFBuiltin.Setting = GameEntry.GetComponent<SettingComponent>();
        GFBuiltin.Sound = GameEntry.GetComponent<SoundComponent>();
        GFBuiltin.UI = GameEntry.GetComponent<UIComponent>();
        GFBuiltin.ObjectPool = GameEntry.GetComponent<ObjectPoolComponent>();
        GFBuiltin.WebRequest = GameEntry.GetComponent<WebRequestComponent>();
        GFBuiltin.BuiltinView = GameEntry.GetComponent<BuiltinViewComponent>();

        RootCanvas = GFBuiltin.UI.GetComponentInChildren<Canvas>();
        GFBuiltin.UICamera = RootCanvas.worldCamera;

        UpdateCanvasScaler();
    }
    public void UpdateCanvasScaler()
    {
        CanvasScaler canvasScaler = RootCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = AppSettings.Instance.DesignResolution;
        var designRatio = canvasScaler.referenceResolution.x / (float)canvasScaler.referenceResolution.y;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = Screen.width / (float)Screen.height > designRatio ? 1 : 0;
        GFBuiltin.Log($"----------UI适配Match:{canvasScaler.matchWidthOrHeight}----------");
    }


    /// <summary>
    /// 退出或重启
    /// </summary>
    /// <param name="type"></param>
    public static void Shutdown(ShutdownType type)
    {
        GameEntry.Shutdown(type);
    }

    public static void Log(string format)
    {
        var colorfulFormat = $"<color=#2BD988>{format}</color>";
        Debug.Log(colorfulFormat);
    }
    public static void LogWarning(string format)
    {
        var colorfulFormat = $"<color=#F2A20C>{format}</color>";
        Debug.LogWarning(colorfulFormat);
    }
    public static void LogError(string format)
    {
        var colorfulFormat = $"<color=#F22E2E>{format}</color>";
        Debug.LogError(colorfulFormat);
    }
}