using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
public abstract class AdSdkHelper : MonoBehaviour
{
    /// <summary>
    /// 插屏广告读取回调
    /// </summary>
    public GameFrameworkAction<bool> mInterstitialAdLoadedEvent;
    /// <summary>
    /// 激励广告读取回调
    /// </summary>
    public GameFrameworkAction<bool> mRewardedAdLoadedEvent;
    /// <summary>
    /// 差评广告关闭回调
    /// </summary>
    public GameFrameworkAction<bool> mInterstitialAdClosedEvent;
    /// <summary>
    /// 激励广告关闭回调
    /// </summary>
    public GameFrameworkAction<bool> mRewardedAdClosedEvent;
    /// <summary>
    /// 插屏广告打开回调
    /// </summary>
    public GameFrameworkAction<bool> mInterstitialAdOpenEvent;
    /// <summary>
    /// 激励广告打开回调
    /// </summary>
    public GameFrameworkAction<bool> mRewardedAdOpenEvent;

    public GameFrameworkAction<bool> mBannerAdLoadedEvent;
    public GameFrameworkAction<bool> mBannerAdOpenEvent;
    public GameFrameworkAction<bool> mBannerAdCloseEvent;

    public GameFrameworkAction<bool> mOnSdkInitialized;
    public bool SdkIsReady { get; private set; }

    public string SdkKey { get; private set; }
    public string SdkInterAdKey { get; private set; }
    public string SdkRewardAdKey { get; private set; }
    public string SdkBannerAdKey { get; private set; }
    public virtual void InitSdk(string key, string interAdKey, string rewardAdKey, string bannerAdKey, GameFrameworkAction<bool> sdkInitialized = null)
    {
        SdkIsReady = false;
        this.mOnSdkInitialized = sdkInitialized;
        this.SdkKey = key;
        this.SdkInterAdKey = interAdKey;
        this.SdkRewardAdKey = rewardAdKey;
        this.SdkBannerAdKey = bannerAdKey;
    }

    /// <summary>
    /// SDK初始化成功回调,需渠道类手动调用
    /// </summary>
    /// <param name="result">初始化是否成功</param>
    protected virtual void OnSdkInitialized(bool result)
    {
        SdkIsReady = result;
        mOnSdkInitialized?.Invoke(this);
    }

    public abstract void LoadInterstitialAd();
    public abstract void LoadRewardedAd();
    public abstract bool IsInterstitialReady();
    public abstract bool IsRewardedAdReady();
    public abstract void ShowInterstitialAd();
    public abstract void ShowRewardedAd();
    public abstract void ShowBannerAd();
    public abstract void HideBannerAd();
    public abstract void SetBannerBackgroundColor(Color col);
    public abstract bool UserPrivacyAccepted();
}
