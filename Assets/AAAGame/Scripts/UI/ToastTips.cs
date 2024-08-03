using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public class ToastTips : UIFormBase
{
    const float maxWidth = 500;
    const float defaultDuration = 2f;
    private Text toastText;
    private LayoutElement toastEle;
    private float duration;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        toastText = this.GetComponentInChildren<Text>();
        toastEle = toastText.gameObject.GetOrAddComponent<LayoutElement>();
        duration = 2.0f;
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        var canvasGp = this.UICanvas.GetComponent<CanvasGroup>();
        canvasGp.alpha = 0;
        canvasGp.DOFade(1, 0.4f);
        toastEle.enabled = false;

        if (Params.TryGet<VarFloat>("duration", out var tempDur))
        {
            duration = tempDur;
        }
        else
        {
            duration = defaultDuration;
        }

        toastText.text = Params.Get<VarString>("content");
        ScheduleStart();
    }

    private async void ScheduleStart()
    {
        await UniTask.DelayFrame(1);
        toastEle.enabled = true;
        toastEle.preferredWidth = Mathf.Min(maxWidth, toastText.rectTransform.sizeDelta.x);
        _ = UniTask.Delay((int)(duration * 1000)).ContinueWith(() =>
        {
            GF.UI.CloseUIFormWithAnim(this.UIForm);
        });
    }
}
