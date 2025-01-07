using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
public class ApplovinMaxSdkHelper : AdSdkHelper
{
    private bool isReceiveReward;
    public override void InitSdk(string key, string interAdKey, string rewardAdKey, string bannerAdKey, GameFrameworkAction<bool> sdkInitialized = null)
    {
        base.InitSdk(key, interAdKey, rewardAdKey, bannerAdKey, sdkInitialized);
        isReceiveReward = false;
        //Log.Error("IsUserConsentSet:{0}", MaxSdk.IsUserConsentSet());

        //MaxSdkCallbacks.OnSdkConsentDialogDismissedEvent += () =>
        //{
        //    //if (!MaxSdk.IsUserConsentSet())
        //    //{
        //    //    GF.Shutdown(ShutdownType.Quit);
        //    //}
        //};
        //MaxSdkCallbacks.OnSdkInitializedEvent += (sdkConfig) =>
        //{
        //    this.OnSdkInitialized(true);

        //    //if (!UserPrivacyAccepted())
        //    //{
        //    //    MaxSdk.UserService.ShowConsentDialog();
        //    //}
        //};
        //MaxSdk.SetSdkKey(key);
        //MaxSdk.InitializeSdk();
    }
    protected override void OnSdkInitialized(bool result)
    {
        //MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (infoStr, adInfo) => { this.mInterstitialAdLoadedEvent?.Invoke(true); };
        //MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (infoStr, errInfo) => { this.mInterstitialAdLoadedEvent?.Invoke(false); };
        //MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (infoStr, adInfo) => { this.mInterstitialAdOpenEvent?.Invoke(true); };
        //MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (infoStr, errInfo, adInfo) => { this.mInterstitialAdOpenEvent?.Invoke(false); };
        //MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (infoStr, adInfo) => { this.mInterstitialAdClosedEvent?.Invoke(true); };

        //MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (infoStr, adInfo) => { this.mRewardedAdLoadedEvent?.Invoke(true); };
        //MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (infoStr, errInfo) => { this.mRewardedAdLoadedEvent?.Invoke(false); };
        //MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (infoStr, adInfo) => { this.mRewardedAdOpenEvent?.Invoke(true); };
        //MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (infoStr, errInfo, adInfo) => { this.mRewardedAdOpenEvent?.Invoke(false); };
        //MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (infoStr, adInfo) => { this.mRewardedAdClosedEvent?.Invoke(isReceiveReward); };
        //MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (infoStr, rewardInfo, adInfo) => { isReceiveReward = true; };

        //MaxSdkCallbacks.Banner.OnAdLoadedEvent += (infoStr, adInfo) => { this.mBannerAdLoadedEvent?.Invoke(true); };
        //MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (infoStr, errInfo) => { this.mBannerAdLoadedEvent?.Invoke(false); };
        //MaxSdkCallbacks.Banner.OnAdCollapsedEvent += (infoStr, adInfo) => { this.mBannerAdCloseEvent?.Invoke(true); };
        //MaxSdkCallbacks.Banner.OnAdExpandedEvent += (infoStr, adInfo) => { this.mBannerAdOpenEvent?.Invoke(true); };

        //MaxSdk.CreateBanner(this.SdkBannerAdKey, MaxSdkBase.BannerPosition.BottomCenter);
        //MaxSdk.StartBannerAutoRefresh(this.SdkBannerAdKey);
        base.OnSdkInitialized(result);
    }
    public override bool IsInterstitialReady()
    {
        return true;
        //return MaxSdk.IsInterstitialReady(this.SdkInterAdKey);
    }

    public override bool IsRewardedAdReady()
    {
        return true;
        //return MaxSdk.IsRewardedAdReady(this.SdkRewardAdKey);
    }

    public override void LoadInterstitialAd()
    {
        //MaxSdk.LoadInterstitial(this.SdkInterAdKey);
    }

    public override void LoadRewardedAd()
    {
        isReceiveReward = false;
        //MaxSdk.LoadRewardedAd(this.SdkRewardAdKey);
    }

    public override void ShowInterstitialAd()
    {
        //MaxSdk.ShowInterstitial(this.SdkInterAdKey);
    }

    public override void ShowRewardedAd()
    {
        //MaxSdk.ShowRewardedAd(this.SdkRewardAdKey);
    }
    public override void ShowBannerAd()
    {
        //MaxSdk.ShowBanner(this.SdkBannerAdKey);
    }
    public override void HideBannerAd()
    {
        //MaxSdk.HideBanner(this.SdkBannerAdKey);
    }
    public override void SetBannerBackgroundColor(Color col)
    {
        //MaxSdk.SetBannerBackgroundColor(this.SdkBannerAdKey, col);
    }

    public override bool UserPrivacyAccepted()
    {
        return true;
        //return MaxSdk.IsUserConsentSet();
    }
}
