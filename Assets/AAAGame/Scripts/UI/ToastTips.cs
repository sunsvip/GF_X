using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public class ToastTips : UIFormBase
{
    const float maxWidth = 500;
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
        if (!Params.Has("content"))
        {
            GF.UI.CloseUIForm(this.UIForm);
            return;
        }

        if (Params.Has("duration"))
        {
            duration = Params.Get<VarFloat>("duration");
        }

        toastText.text = Params.Get<VarString>("content");
        StartCoroutine(InitLayout());
    }

    IEnumerator InitLayout()
    {
        yield return new WaitForEndOfFrame();
        toastEle.enabled = true;
        toastEle.preferredWidth = Mathf.Min(maxWidth, toastText.rectTransform.sizeDelta.x);
        //yield return new WaitForSeconds(duration);
        //GF.UI.HideUIForm(this.UIForm);
        var seqAct = DOTween.Sequence();
        seqAct.SetEase(Ease.Linear);
        seqAct.SetUpdate(true);
        seqAct.AppendInterval(duration);
        seqAct.onComplete = () =>
        {
            UIExtension.CloseUIFormWithAnim(GF.UI, this.UIForm);
        };
    }
}
