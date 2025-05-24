using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SettingDialog : UIFormBase
{
    int m_ClickCount;
    float m_LastClickTime;
    readonly float clickInterval = 0.4f;
    float m_ToggleHandleX;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_ToggleHandleX = Mathf.Abs(varToggleVibrate.transform.Find("Handle").localPosition.x);

        varToggleVibrate.onValueChanged.AddListener(isOn =>
        {
            OnToggleChanged(varToggleVibrate);
        });

        varMusicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        varSoundFxSlider.onValueChanged.AddListener(OnSoundFxSliderChanged);
    }
    public override void InitLocalization()
    {
        base.InitLocalization();
        varVersionTxt.text = Utility.Text.Format("{0}v{1}", AppSettings.Instance.DebugMode ? "Debug " : string.Empty, GF.Base.EditorResourceMode ? Application.version : Utility.Text.Format("{0}({1})", Application.version, GF.Resource.InternalResourceVersion));
        var handleText = varToggleVibrate.GetComponentInChildren<TextMeshProUGUI>();
        handleText.text = varToggleVibrate.isOn ? GF.Localization.GetString("ON") : GF.Localization.GetString("OFF");
    }
    private void OnSoundFxSliderChanged(float arg0)
    {
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, arg0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, arg0 == 0);
    }

    private void OnMusicSliderChanged(float arg0)
    {
        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, arg0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, arg0 == 0);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        m_ClickCount = 0;
        m_LastClickTime = Time.time;
        InitSettings();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);

        base.OnClose(isShutdown, userData);
    }
    private void InitSettings()
    {
        varMusicSlider.value = GF.Setting.GetMediaMute(Const.SoundGroup.Music) ? 0 : GF.Setting.GetMediaVolume(Const.SoundGroup.Music);
        varSoundFxSlider.value = GF.Setting.GetMediaMute(Const.SoundGroup.Sound) ? 0 : GF.Setting.GetMediaVolume(Const.SoundGroup.Sound);

        varToggleVibrate.isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate);
        RefreshLanguage();
    }

    private void OnToggleChanged(Toggle tg)
    {
        var handle = tg.transform.Find("Handle") as RectTransform;
        var handleText = handle.GetComponentInChildren<TextMeshProUGUI>();
        float targetX = tg.isOn ? m_ToggleHandleX : -m_ToggleHandleX;
        float duration = (Mathf.Abs(targetX - handle.anchoredPosition.x) / m_ToggleHandleX) * 0.2f;
        handle.DOAnchorPosX(targetX, duration).onComplete = () =>
        {
            handleText.text = tg.isOn ? GF.Localization.GetString("ON") : GF.Localization.GetString("OFF");
        };

        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, !varToggleVibrate.isOn);
    }

    private void RefreshLanguage()
    {
        var curLang = GF.Setting.GetLanguage();
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        var langRow = langTb.GetDataRow(row => row.LanguageKey == curLang.ToString());
        varIconFlag.SetSprite(langRow.LanguageIcon);
        varLanguageName.text = langRow.LanguageDisplay;
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varBtnLanguage)
        {
            var uiParms = UIParams.Create();
            VarAction action = ReferencePool.Acquire<VarAction>();
            action.Value = OnLanguageChanged;
            uiParms.Set<VarAction>(LanguagesDialog.P_LangChangedCb, action);
            GF.UI.OpenUIForm(UIViews.LanguagesDialog, uiParms);
        }
        else if (btSelf == varBtnHelp)
        {
            GF.UI.ShowToast(GF.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnPrivacy)
        {
            GF.UI.ShowToast(GF.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnTermsOfService)
        {
            GF.UI.ShowToast(GF.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnRating)
        {
            GF.UI.OpenUIForm(UIViews.RatingDialog);
        }
    }
    void OnLanguageChanged()
    {
        RefreshLanguage();
        GF.UI.CloseUIForms(UIViews.LanguagesDialog);
        ReloadLanguage();
    }
    private void ReloadLanguage()
    {
        GF.Localization.RemoveAllRawStrings();
        GF.Localization.LoadLanguage(GF.Localization.Language.ToString(), this);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        GF.UI.UpdateLocalizationTexts();
    }
    public void OnClickVersionText()
    {
        if (Time.time - m_LastClickTime <= clickInterval)
        {
            m_ClickCount++;
            if (m_ClickCount > 5)
            {
                GF.Debugger.ActiveWindow = !GF.Debugger.ActiveWindow;
                m_ClickCount = 0;
            }
        }
        else
        {
            m_ClickCount = 0;
        }
        m_LastClickTime = Time.time;
    }
}
