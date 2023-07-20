using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;

public static class SoundExtension
{
    private static Dictionary<string, float> lastPlayEffectTags = new Dictionary<string, float>();
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="soundCom"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static int PlayBGM(this SoundComponent soundCom, string name)
    {
        return soundCom.PlaySound(name, Const.SoundGroup.Music.ToString(), Vector3.zero, true);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundCom"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static int PlaySound(this SoundComponent soundCom, string name, string group, Vector3 worldPos, bool isLoop = false)
    {
        string assetName = UtilityBuiltin.ResPath.GetSoundPath(name);
        //TODO 临时资源存在判定
        if (GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist) return 0;
        var parms = ReferencePool.Acquire<GameFramework.Sound.PlaySoundParams>();
        parms.Clear();
        parms.Loop = isLoop;
        return soundCom.PlaySound(assetName, group, 0, parms, worldPos);
    }
    public static int PlayEffect(this SoundComponent soundCom, string name, bool isLoop = false)
    {
        return soundCom.PlaySound(name, Const.SoundGroup.Sound.ToString(), Vector3.zero, isLoop);
    }
    public static void PlayEffect(this SoundComponent soundCom, string name, float interval)
    {
        bool hasKey = lastPlayEffectTags.ContainsKey(name);
        if (hasKey && Time.time - lastPlayEffectTags[name] < interval)
        {
            return;
        }
        soundCom.PlaySound(name, Const.SoundGroup.Sound.ToString(), Vector3.zero, false);
        if (hasKey) lastPlayEffectTags[name] = Time.time;
        else lastPlayEffectTags.Add(name, Time.time);
    }

    public static void PlayVibrate(this SoundComponent soundCom, long time = Const.DefaultVibrateDuration)
    {
        if (soundCom.GetSoundGroup(Const.SoundGroup.Vibrate.ToString()).Mute)
        {
            return;
        }
#if UNITY_ANDROID || UNITY_IOS
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass act = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityAct = act.GetStatic<AndroidJavaObject>("currentActivity");
            var vibr = unityAct.Call<AndroidJavaObject>("getSystemService", "vibrator");
            vibr.Call("vibrate", time);
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Handheld.Vibrate();
        }
#endif
    }
}
