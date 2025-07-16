using System.Collections;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 内置的UI界面(热更之前)
/// </summary>
public class BuiltinViewComponent : GameFrameworkComponent
{
    [Header("Loading Progress:")]
    [SerializeField] GameObject loadingProgressNode = null;
    [SerializeField] private TextMeshProUGUI loadSliderText;
    [SerializeField] private Slider loadSlider;

    [Space(20)]
    [Header("Tips Dialog:")]
    [SerializeField] GameObject tipsDialog = null;
    [SerializeField] TextMeshProUGUI tipsTitleText;
    [SerializeField] TextMeshProUGUI tipsContentText;
    [SerializeField] Button tipsPositiveBtn;
    [SerializeField] Button tipsNegativeBtn;
    
    private void Start()
    {
        ShowLoadingProgress();
    }
    public void ShowLoadingProgress(float defaultProgress = 0)
    {
        loadingProgressNode.SetActive(true);
        SetLoadingProgress(defaultProgress);
    }
    public void SetLoadingProgress(float progress)
    {
        loadSlider.value = progress;
        loadSliderText.text = Utility.Text.Format("{0:N0}%", loadSlider.value * 100);
    }

    public void HideLoadingProgress()
    {
        loadingProgressNode.SetActive(false);
    }

    public void ShowDialog(string title, string content, string yes_btn_title = "YES", string no_btn_title = "NO", UnityEngine.Events.UnityAction yes_cb = null, UnityEngine.Events.UnityAction no_cb = null)
    {
        tipsDialog.SetActive(true);
        if (yes_cb == null && no_cb == null)
        {
            yes_cb = HideDialog;
        }
        tipsNegativeBtn.gameObject.SetActive(no_cb != null);
        tipsNegativeBtn.GetComponentInChildren<TextMeshProUGUI>().text = no_btn_title;

        tipsPositiveBtn.gameObject.SetActive(yes_cb != null);
        tipsPositiveBtn.GetComponentInChildren<TextMeshProUGUI>().text = yes_btn_title;
        tipsTitleText.text = title.ToUpper();
        tipsContentText.text = content;
        tipsNegativeBtn.onClick.RemoveAllListeners();
        tipsPositiveBtn.onClick.RemoveAllListeners();
        if (no_cb != null) tipsNegativeBtn.onClick.AddListener(() => { no_cb.Invoke(); HideDialog(); });
        if (yes_cb != null) tipsPositiveBtn.onClick.AddListener(() => { yes_cb.Invoke(); HideDialog(); });
    }

    public void HideDialog()
    {
        tipsDialog.SetActive(false);
    }
}
