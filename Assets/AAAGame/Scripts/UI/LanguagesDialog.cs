using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LanguagesDialog : UIFormBase
{
    public const string P_LangChangedCb = "LangChangedCb";
    VarAction m_VarAction;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_VarAction = Params.Get<VarAction>(P_LangChangedCb);
        varLanguageToggle.SetActive(false);
        RefreshList();
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        //UnspawnAll();
    }
    void RefreshList()
    {
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        foreach (var lang in langTb)
        {
            var item = this.SpawnItem<LanguageItemObject>(varLanguageToggle, varToggleGroup.transform);
            item.SetLanguage(lang);
            item.onLanguageChanged = m_VarAction;
        }
    }

    private class LanguageItemObject : UIItemObject
    {
        public Toggle Toggle { get; private set; }
        public Image LanguageIcon { get; private set; }
        public TextMeshProUGUI LanguageName { get; private set; }

        string m_LanguageKey;
        public Action onLanguageChanged = null;
        protected override void OnCreate(GameObject itemInstance)
        {
            Toggle = itemInstance.GetComponent<Toggle>();
            LanguageIcon = itemInstance.transform.Find("Icon_Flag").GetComponent<Image>();
            LanguageName = itemInstance.GetComponentInChildren<TextMeshProUGUI>();
            Toggle.onValueChanged.RemoveAllListeners();
            Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool arg0)
        {
            Toggle.graphic.gameObject.SetActive(arg0);
            if (arg0 && m_LanguageKey != GF.Setting.GetLanguage().ToString())
            {
                if (Enum.TryParse<GameFramework.Localization.Language>(m_LanguageKey, out var clickLanguage))
                {
                    GF.Setting.SetLanguage(clickLanguage);
                    onLanguageChanged?.Invoke();
                }
            }
        }

        public void SetLanguage(LanguagesTable row)
        {
            m_LanguageKey = row.LanguageKey;
            LanguageIcon.SetSprite(row.LanguageIcon);
            LanguageName.text = row.LanguageDisplay;
            Toggle.isOn = row.LanguageKey == GF.Setting.GetLanguage().ToString();
            Toggle.graphic.gameObject.SetActive(Toggle.isOn);
        }
    }
}
