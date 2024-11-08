using System;
using UnityEngine.UI;

public partial class LanguageItem : UIItemBase
{
    string m_LanguageKey;
    Toggle m_Toggle;
    Action m_Action;
    protected override void OnInit()
    {
        base.OnInit();
        m_Toggle = GetComponent<Toggle>();
        m_Toggle.onValueChanged.RemoveAllListeners();
        m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }
    public void SetData(LanguagesTable row, ToggleGroup group, Action action)
    {
        m_Toggle.group = group;
        m_LanguageKey = row.LanguageKey;
        m_Action = action;
        varIcon.SetSprite(row.LanguageIcon);
        varName.text = row.LanguageDisplay;
        var isOn = (row.LanguageKey == GF.Setting.GetLanguage().ToString());
        m_Toggle.isOn = isOn;
        m_Toggle.graphic.gameObject.SetActive(isOn);
    }
    private void OnToggleValueChanged(bool arg0)
    {
        m_Toggle.graphic.gameObject.SetActive(arg0);
        if (arg0 && m_LanguageKey != GF.Setting.GetLanguage().ToString())
        {
            if (Enum.TryParse<GameFramework.Localization.Language>(m_LanguageKey, out var clickLanguage))
            {
                GF.Setting.SetLanguage(clickLanguage);
                m_Action?.Invoke();
            }
        }
    }
}
