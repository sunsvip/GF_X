using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using UnityGameFramework.Runtime;
using System;

public class StarRatingDialog : UIFormBase
{
    [SerializeField] Button[] star_bts;
    [SerializeField] Text tipText;
    private int mStar;
    public int Star
    {
        get { return mStar; }
        set
        {
            mStar = value;
            for (int i = 0; i < star_bts.Length; i++)
            {
                star_bts[i].transform.Find("Checkmark").gameObject.SetActive((i + 1) <= mStar);
            }
        }
    }
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        for (int i = 0; i < star_bts.Length; i++)
        {
            var tg = star_bts[i];
            tg.onClick.RemoveAllListeners();
            int bt_index = i;
            tg.onClick.AddListener(()=> { Star = bt_index + 1; });
        }

        tipText.text = Utility.Text.Format("Enjoying {0}?\n<size=30>Tap a star to rate it.</size>", Application.productName);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Star = 5;
    }
    protected override void OnButtonClick(object sender, string bt_tag)
    {
        base.OnButtonClick(sender, bt_tag);
        if (bt_tag != "RATE")
        {
            return;
        }
        GF.Setting.SetBool("RATED_FIVE", true);//评过五星的不再弹出评星界面
        //统计 评星
        //GF.UserData.RecodEvent("star_rating", Star.ToString());
        
        if (Star < 5)
        {
            OnClickClose();
            return;
        }
        GF.AD.OpenAppstore();
        OnClickClose();
    }
}
