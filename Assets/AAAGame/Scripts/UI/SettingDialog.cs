using GameFramework;
using GameFramework.Procedure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingDialog : UIFormBase
{
    [SerializeField] Toggle[] settingTgs;

    [SerializeField] Text versionText;

    int clickCount;
    float lastClickTime;
    readonly float clickInterval = 0.4f;

    [SerializeField] private GameObject rateBtnObject;
    [SerializeField] private GameObject restoreBtnObject;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        for (int i = 0; i < settingTgs.Length; i++)
        {
            var tg = settingTgs[i];
            tg.onValueChanged.RemoveAllListeners();
            int tgIndex = i;
            tg.onValueChanged.AddListener(isOn =>
            {
                OnSettingToggle(tgIndex, settingTgs[tgIndex].isOn);
            });
        }
        versionText.text = Utility.Text.Format("{0} v{1}", AppSettings.Instance.DebugMode ? "Debug" : string.Empty, GF.Base.EditorResourceMode ? Application.version : Utility.Text.Format("{0}({1})", Application.version, GF.Resource.InternalResourceVersion));
    }


    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        settingTgs[0].isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Sound);
        settingTgs[1].isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate);
        settingTgs[2].isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Joystick);
        clickCount = 0;
        lastClickTime = Time.time;
    }
    protected override void OnButtonClick(object sender, string bt_tag)
    {
        base.OnButtonClick(sender, bt_tag);
        switch (bt_tag)
        {
            case "RATE_US":
                GF.AD.OpenAppstore();
                break;
            case "HOME":
                Back2Home();
                GF.UI.CloseUIFormWithAnim(this.UIForm);
                break;
        }
    }
    public void OnClickVersionText()
    {
        if (Time.time - lastClickTime <= clickInterval)
        {
            clickCount++;
            if (clickCount > 5)
            {
                GF.Debugger.ActiveWindow = !GF.Debugger.ActiveWindow;
                clickCount = 0;
            }
        }
        else
        {
            clickCount = 0;
        }
        lastClickTime = Time.time;
    }

    private void Back2Home()
    {
        var curProcedure = GF.Procedure.CurrentProcedure;
        if (curProcedure is GameProcedure)
        {
            var gameProcedure = curProcedure as GameProcedure;
            gameProcedure.BackHome();
        }
    }
    public void OnSettingToggle(int tgIdx, bool isOn)
    {
        bool isMute = !isOn;
        settingTgs[tgIdx].targetGraphic.enabled = isMute;
        switch (tgIdx)
        {
            case 0:
                GF.Setting.SetMediaMute(Const.SoundGroup.Sound, isMute);
                GF.Setting.SetMediaMute(Const.SoundGroup.Music, isMute);
                //MaxSdk.SetMuted(isOn);
                break;
            case 1:
                GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, isMute);
                break;
            case 2:
                GF.Setting.SetMediaMute(Const.SoundGroup.Joystick, isMute);
                break;
        }
    }
}
