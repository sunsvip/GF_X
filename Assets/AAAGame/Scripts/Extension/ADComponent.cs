
//using AppsFlyerSDK;
//using Facebook.Unity;
//using FlurrySDK;
using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;
using Log = UnityGameFramework.Runtime.Log;
using GameFramework.Event;
public enum ADResult
{
    Open,
    Close
}

public class ADComponent : GameFrameworkComponent//, IAppsFlyerConversionData
{
    [SerializeField] private bool m_SkipAd = false;
    [SerializeField] private bool m_ShowLoadingView = false;
    [SerializeField] private float m_LoadingTimeout = 5f;
    [SerializeField] private float m_ReloadingAdInterval = 10f;

    [SerializeField] private Color m_BannerBGColor = Color.white;
    [SerializeField] private AdSdkHelper m_CustomADHelper = null;

    [Header("[Sdk Config for Android]")]
    [SerializeField] private string m_AdKey = "";
    [SerializeField] private string m_AppsflyerKey = "";
    [SerializeField] private string m_FlurryKey = "";
    [SerializeField] private string m_InterAdUnitId = "";
    [SerializeField] private string m_RewardAdUnitId = "";
    [SerializeField] private string m_BannerAdUnitId = "";

    [Header("[Sdk Config for IOS]")]
    [SerializeField] private string m_AdKey_IOS = "";
    [SerializeField] private string m_AppsflyerKey_IOS = "";
    [SerializeField] private string m_FlurryKey_IOS = "";
    [SerializeField] private string m_InterAdUnitId_IOS = "";
    [SerializeField] private string m_RewardAdUnitId_IOS = "";
    [SerializeField] private string m_BannerAdUnitId_IOS = "";

    private AndroidJavaObject activity = null;
    /// <summary>
    /// 0等待结果 1非自然量 2自然量 3超时
    /// </summary>
    private int organic_state;
    public bool IsOrganic
    {
        get { return organic_state != 1; }
        set
        {
            organic_state = value ? 2 : 1;
            GF.Setting.SetBool("IsOrganic", value);
        }
    }


    private bool organicOutTime = false;
    private string targetAdKey;
    private string targetFlurryKey;
    private string targetInterUnitId;
    private string targetRewardUnitId;
    private string targetBannerUnitId;
    GameFrameworkAction showVideoAdCallback = null;
    ADTriggerPoint triggerPoint;
    GameFrameworkAction<ADResult> showInterAdCallback = null;
    Queue<Action> adEventsQueue;
    float interstitialAdInterval;
    float lastShowAdTime;

    public bool NoAD
    {
        get => GF.Setting.GetBool("NOAD", false); set
        {
            GF.Setting.SetBool("NOAD", value);
            if (NoAD)
            {
                HideBannerAd();
            }
        }
    }
    public bool AdViewShowing { get; private set; }
    protected override void Awake()
    {
        base.Awake();
#if UNITY_IOS
        targetFlurryKey = m_FlurryKey_IOS;
        targetInterUnitId = m_InterAdUnitId_IOS;
        targetRewardUnitId = m_RewardAdUnitId_IOS;
        targetBannerUnitId = m_BannerAdUnitId_IOS;
        targetAdKey = m_AdKey_IOS;
#else
        targetFlurryKey = m_FlurryKey;
        targetInterUnitId = m_InterAdUnitId;
        targetRewardUnitId = m_RewardAdUnitId;
        targetBannerUnitId = m_BannerAdUnitId;
        targetAdKey = m_AdKey;
#endif
        InitFacebook();
    }

    private void Start()
    {
        GameEntry.GetComponent<EventComponent>().Subscribe(GFEventArgs.EventId, OnGFEvent);
        adEventsQueue = new Queue<Action>();

        //m_CustomADHelper = Helper.CreateHelper(m_ADHelperTypeName, m_CustomADHelper);
        //if (m_CustomADHelper == null)
        //{
        //    Log.Error("Can not create AD helper.");
        //    return;
        //}
        AdViewShowing = false;
        m_CustomADHelper.name = "AD Helper";
        Transform helperNode = m_CustomADHelper.transform;
        helperNode.SetParent(this.transform);
        helperNode.localScale = Vector3.one;
        organic_state = 0;
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass act = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = act.GetStatic<AndroidJavaObject>("currentActivity");
        }

        SendEvent("LaunchGame", new Dictionary<string, string> { ["description"] = "How many times player launch this game" });
        InitAppsFlyer();
        InitFlurry();
        InitAdSdk();
    }
    public bool CheckAllInitiated()
    {
#if UNITY_EDITOR
        return true;

#else
        bool adInit = m_CustomADHelper != null ? m_CustomADHelper.SdkIsReady : false;
        return adInit;
        //return FB.IsInitialized && adInit && BuglyInitiated;
#endif
    }
    private void OnGFEvent(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.ResourceInitialized:
                {
                    interstitialAdInterval = GF.Config.GetFloat("InterstitialAdInterval", 60);
                    GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEvent);
                }
                break;
        }
    }

    private void Update()
    {
        lock (adEventsQueue)
        {
            while (adEventsQueue.Count > 0)
            {
                adEventsQueue.Dequeue().Invoke();
            }
        }
    }
    private void EnqueueAdEvent(Action act)
    {
        lock (adEventsQueue)
        {
            adEventsQueue.Enqueue(act);
        }
    }
    private void InitAdSdk()
    {
        m_CustomADHelper.mInterstitialAdLoadedEvent = OnInterstitialAdLoaded;
        m_CustomADHelper.mInterstitialAdOpenEvent = OnInterstitialAdOpen;
        m_CustomADHelper.mInterstitialAdClosedEvent = OnInterstitialAdClosed;
        m_CustomADHelper.mRewardedAdLoadedEvent = OnRewardedAdLoaded;
        m_CustomADHelper.mRewardedAdOpenEvent = OnRewardedAdOpen;
        m_CustomADHelper.mRewardedAdClosedEvent = OnRewardedAdClosed;
        m_CustomADHelper.InitSdk(targetAdKey, targetInterUnitId, targetRewardUnitId, targetBannerUnitId, initResult =>
        {
            Log.Info("Max Sdk init:{0}", initResult);
            if (initResult)
            {
                m_CustomADHelper.SetBannerBackgroundColor(m_BannerBGColor);
                LoadInterstitialAd();
                LoadRewardedAd();
            }
        });
    }

    internal void InitFacebook()
    {
        //if (!FB.IsInitialized)
        //{
        //    FB.Init(() =>
        //    {
        //        FB.Mobile.SetAdvertiserTrackingEnabled(true);
        //    });
        //}
        //else
        //{
        //    FB.ActivateApp();
        //}
    }

    internal void InitFlurry()
    {
        //new Flurry.Builder()
        //          .WithCrashReporting(true)
        //          .WithLogEnabled(true)
        //          .WithLogLevel(Flurry.LogLevel.VERBOSE)
        //          .WithMessaging(true)
        //          .WithPerformanceMetrics(Flurry.Performance.ALL)
        //          .WithAppVersion(Application.version)
        //          .Build(targetFlurryKey);
        //Flurry.SetReportLocation(true);
    }
    internal void InitAppsFlyer()
    {
        //AppsFlyer.setIsDebug(false);
        //AppsFlyer.initSDK(m_AppsflyerKey, m_AppsflyerKey_IOS, this);
        //AppsFlyer.startSDK();
    }

    public bool UserPrivacyAccepted()
    {
        return true;
//#if UNITY_EDITOR
//        return true;
//#elif UNITY_IPHONE
//        return true;
//#else
//        return m_CustomADHelper && m_CustomADHelper.UserPrivacyAccepted();
//#endif
    }
    /// <summary>
    /// 展示插屏广告
    /// </summary>
    /// <param name="callback"></param>
    public void ShowInterstitialAd(ADTriggerPoint adPoint, GameFrameworkAction<ADResult> callback = null)
    {
        this.showInterAdCallback = callback;
        this.triggerPoint = adPoint;
        if (m_SkipAd)
        {
            this.showInterAdCallback?.Invoke(ADResult.Close);
            return;
        }
        GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Interstitial"] = Utility.Text.Format("Trigger_{0}", adPoint.ToString()) });
        if (!IsInterstitialAdReady())
        {
            //GF.UserData.RecodEvent("missing_interAd");
            LoadInterstitialAd();
            return;
        }
        m_CustomADHelper.ShowInterstitialAd();
    }
    /// <summary>
    /// 展示激励广告
    /// </summary>
    /// <param name="callback"></param>
    //public void ShowRewardedAd(ADTriggerPoint adPoint, GameFrameworkAction callback = null)
    //{
    //    this.showVideoAdCallback = callback;
    //    this.triggerPoint = adPoint;
    //    if (m_SkipAd)
    //    {
    //        this.showVideoAdCallback?.Invoke();
    //        return;
    //    }
    //    GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Videos"] = Utility.Text.Format("Tap_{0}", adPoint.ToString()) });
    //    if (!IsRewardedAdReady())
    //    {
    //        LoadRewardedAd();
    //        if (m_ShowLoadingView)
    //        {
    //            GF.BuiltinView.WaitAndShowVideoAd(m_LoadingTimeout, () =>
    //            {
    //                m_CustomADHelper.ShowRewardedAd();
    //            });
    //        }
    //        else
    //        {
    //            ShowToast(GF.Localization.GetLocalString("Sponsor on the road~"));
    //        }
    //    }
    //    else
    //    {
    //        m_CustomADHelper.ShowRewardedAd();
    //    }
    //}
    internal void ShowBannerAd()
    {
        if (!NoAD) m_CustomADHelper.ShowBannerAd();
    }
    internal void HideBannerAd()
    {
        m_CustomADHelper.HideBannerAd();
    }
    internal bool IsInterstitialAdReady()
    {
        if (m_CustomADHelper == null) return false;

        return m_CustomADHelper.IsInterstitialReady();
    }
    internal bool IsRewardedAdReady()
    {
        if (m_CustomADHelper == null) return false;

        return m_CustomADHelper.IsRewardedAdReady();
    }
    /// <summary>
    /// 加载插屏广告
    /// </summary>
    /// <param name="delay"></param>
    void LoadInterstitialAd(float delay = 0)
    {
        if (m_CustomADHelper == null) return;

        if (delay < 0.1f)
        {
            m_CustomADHelper.LoadInterstitialAd();
        }
        else
        {
            StartCoroutine(DelayLoadInterstitialAd(delay));
            Log.Info("Reload Interstitial AD...");
        }
    }

    IEnumerator DelayLoadInterstitialAd(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_CustomADHelper.LoadInterstitialAd();
    }
    /// <summary>
    /// 加载激励广告
    /// </summary>
    /// <param name="delay"></param>
    void LoadRewardedAd(float delay = 0)
    {
        if (m_CustomADHelper == null) return;

        if (delay < 0.1f)
        {
            m_CustomADHelper.LoadRewardedAd();
        }
        else
        {
            StartCoroutine(DelayLoadRewardedAd(delay));
            Log.Info("Reload Reward AD...");
        }
    }
    IEnumerator DelayLoadRewardedAd(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        m_CustomADHelper.LoadRewardedAd();
    }


    private void OnInterstitialAdLoaded(bool success)
    {
        if (!success)
        {
            EnqueueAdEvent(delegate
            {
                //Log.Info("InterAdFailedToLoad");
                //GF.AD.SendEvent("AdFailedToLoad_InterAd", "Total number of InterAd Failed To Load.");
                LoadInterstitialAd(m_ReloadingAdInterval);
            });
        }
    }
    private void OnInterstitialAdOpen(bool openSuccess)
    {
        lastShowAdTime = Time.time;
        AdViewShowing = openSuccess;
        if (openSuccess)
        {
            EnqueueAdEvent(delegate
            {
                this.showInterAdCallback?.Invoke(ADResult.Open);
                GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Interstitial"] = Utility.Text.Format("Play_{0}", triggerPoint.ToString()) });
            });
        }
    }

    private void OnInterstitialAdClosed(bool obj)
    {
        lastShowAdTime = Time.time;
        AdViewShowing = false;
        EnqueueAdEvent(delegate
        {
            this.showInterAdCallback?.Invoke(ADResult.Close);
            GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Interstitial"] = Utility.Text.Format("Finish_{0}", triggerPoint.ToString()) });
            LoadInterstitialAd();
        });
    }

    private void OnRewardedAdClosed(bool rewardSuccess)
    {
        lastShowAdTime = Time.time;
        AdViewShowing = false;
        if (rewardSuccess)
        {
            EnqueueAdEvent(delegate
            {
                this.showVideoAdCallback?.Invoke();
                GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Videos"] = Utility.Text.Format("Success_{0}", triggerPoint.ToString()) });
            });
        }
        EnqueueAdEvent(delegate
        {
            LoadRewardedAd();
        });
    }

    private void OnRewardedAdOpen(bool openSuccess)
    {
        lastShowAdTime = Time.time;
        AdViewShowing = openSuccess;
        if (openSuccess)
        {
            EnqueueAdEvent(delegate
            {
                GF.AD.SendEvent("Ads", new Dictionary<string, string> { ["Videos"] = Utility.Text.Format("Play_{0}", triggerPoint.ToString()) });
            });
        }
    }

    private void OnRewardedAdLoaded(bool loadSuccess)
    {
        if (!loadSuccess)
        {
            EnqueueAdEvent(delegate
            {
                //GF.AD.SendEvent("AdFailedToLoad_RewardedAd", "Total number of RewardAd Failed To Load.");
                LoadRewardedAd(m_ReloadingAdInterval);
            });
        }
    }

    public void onConversionDataSuccess(string conversionData)
    {
        Log.Info("AppsFlyer onConversionDataSuccess:{0}", conversionData);
        //var jsonDt = AppsFlyer.CallbackStringToDictionary(conversionData);
    }

    public void onConversionDataFail(string error)
    {
        Log.Info("AppsFlyer onConversionDataFail:{0}", error);
    }

    public void onAppOpenAttribution(string attributionData)
    {

    }

    public void onAppOpenAttributionFailure(string error)
    {

    }


    /// <summary>
    /// 请求自然量/非自然量状态
    /// </summary>
    /// <param name="outTime"></param>
    /// <param name="onResult"></param>
    public void RequestOrganicState(float outTime, Action<float> onProgress, Action onComplete)
    {
        StartCoroutine(WaitingOrganicResult(outTime, onProgress, onComplete));
    }
    private IEnumerator WaitingOrganicResult(float outTime, Action<float> onProgress, Action onComplete)
    {
        float timer = 0;
        float interval = 0.2f;
        float progress;
        var hasOrganic = GF.Setting.HasSetting("IsOrganic");
        if (hasOrganic)
        {
            organic_state = GF.Setting.GetBool("IsOrganic") ? 2 : 1;
        }
        else
        {
            organic_state = Application.isEditor ? 1 : 0;
        }

        while (organic_state == 0)
        {
            yield return new WaitForSeconds(interval);
            timer += interval;
            if (timer >= outTime)
            {
                IsOrganic = true;
                //GF.UserData.RecodEvent("attribution_overtime", null, false);
                organicOutTime = true;
            }
            else
            {
                progress = Mathf.Clamp(timer / outTime, 0, 1);
                onProgress?.Invoke(progress);
            };
        }
        onComplete.Invoke();
    }

    internal void SendEvent(string eName, Dictionary<string, string> eParms = null)
    {
#if UNITY_EDITOR
        //Log.Info("SendEvent,Name:{0},Parms:{1}", eName, UtilityBuiltin.Json.ToJson(eParms));
#else
        //Flurry.LogEvent(eName, eParms);
#endif
    }

    /// <summary>
    /// 反馈
    /// </summary>
    internal void Feedback()
    {
        string email = GF.Config.GetString("FEEDBACK_EMAIL"); //这里是Email
        //Uri uri = new Uri(string.Format("mailto:{0}?subject={1}&body={2}", email, Utility.Text.Format("{0}-{1}-[ID-{2}]", Application.productName, OS_NAME, GF.UserData.User.UserId),
        //Utility.Text.Format("[ID]:{0}\n[OS]:{1}\n[VERSION]:{2}\n[Content]:\n", GF.UserData.User.UserId, OS_NAME, Application.version)));//第二个参数是邮件的标题

        //Application.OpenURL(uri.AbsoluteUri);
    }
    /// <summary>
    /// 跳转隐私条款
    /// </summary>
    internal void OpenPolicy()
    {
#if UNITY_ANDROID
        string policy_url = GF.Config.GetString("POLICY_ANDROID");
#else
        string policy_url = GF.Config.GetString("POLICY_IOS");
#endif
        Application.OpenURL(policy_url);
    }

    internal void OpenAppstore()
    {
#if UNITY_ANDROID
        Application.OpenURL(Utility.Text.Format("market://details?id={0}", Application.identifier));
#elif UNITY_IOS
        //Application.OpenURL(GF.UserData.Config.constTable.appStoreUrl);
#endif
    }
    /// <summary>
    /// ShowToast
    /// </summary>
    /// <param name="message">内容</param>
    /// <param name="duration">显示多久(秒)</param>
    public void ShowToast(string content, params object[] args)
    {
        string message = Utility.Text.Format(content, args);

        if (activity != null)
        {
            AndroidJavaClass jc = new AndroidJavaClass("android.widget.Toast");
            AndroidJavaObject toast = jc.CallStatic<AndroidJavaObject>("makeText", activity, message, 2000);
            toast.Call("show");
        }
        else
        {
            GF.UI.ShowToast(message);
        }
    }
    /// <summary>
    /// 切换到后台
    /// </summary>
    public void MoveTaskToBack()
    {
        if (activity != null)
        {
            activity.Call<bool>("moveTaskToBack", true);
        }
        else
        {
            GFBuiltin.Shutdown(ShutdownType.Quit);
        }
    }

    internal void TryTriggerInterAd(ADTriggerPoint triggerType)
    {
        if (AdViewShowing || NoAD) return;

        if (Time.time - lastShowAdTime < interstitialAdInterval)
        {
            Log.Info("插屏触发失败. 广告触发冷却中...");
            return;
        }


        GF.AD.ShowInterstitialAd(triggerType, result =>
        {
            if (result == ADResult.Close) GF.Event.Fire(this, ReferencePool.Acquire<PlayerEventArgs>().Fill(PlayerEventType.InterstitialAdClose));
        });
    }
}
/// <summary>
/// 广告触发点
/// </summary>
public enum ADTriggerPoint
{
    CloseUpgradeView,//升级操作后,关闭升级界面触发插屏
    CollectMoneyStack,//收集钱堆时触发插屏
    PlayerStayIdle, //玩家保持空闲触发插屏
    OfflineBonus,
    BuffItem,
    Upgrade
}