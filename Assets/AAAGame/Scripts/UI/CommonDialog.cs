using GameFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class CommonDialog : UIFormBase
{
    [SerializeField] Text title;
    [SerializeField] TextMeshProUGUI content;
    [SerializeField] Button closeBt;
    [SerializeField] Button[] buttons;
    [SerializeField] GameFrameworkAction positiveAction;
    [SerializeField] GameFrameworkAction negativeAction;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        for (int i = 0; i < buttons.Length; i++)
        {
            int btTag = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => { ClickButton(btTag); });
        }
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        var positiveData = Params.Get<VarObject>("PositiveAction");
        positiveAction = positiveData != null ? positiveData.Value as GameFrameworkAction : null;

        var negativeData = Params.Get<VarObject>("NegativeAction");
        negativeAction = negativeData != null ? negativeData.Value as GameFrameworkAction : null;

        bool showClose = Params.Get<VarBoolean>("ShowClose", true);

        closeBt.interactable = showClose;
        title.text = Params.Get<VarString>("Title");
        content.text = Params.Get<VarString>("Content");
        //buttons[1].gameObject.SetActive(positiveAction != null);
        buttons[0].gameObject.SetActive(negativeAction != null);
    }
    private void ClickButton(int btTag)
    {
        switch (btTag)
        {
            case 0:
                {
                    negativeAction?.Invoke();
                    OnClickClose();
                }
                break;
            case 1:
                {
                    positiveAction?.Invoke();
                    OnClickClose();
                }
                break;
        }
    }
}
