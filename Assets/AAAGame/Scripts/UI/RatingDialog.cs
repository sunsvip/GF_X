using UnityEngine;
using UnityEngine.UI;

public partial class RatingDialog : UIFormBase
{
    const int MIN_STAR = 4;
    int m_Star;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        for (int i = 0; i < varStarToggleArr.Length; i++)
        {
            var tg = varStarToggleArr[i];
            tg.interactable = true;
            int clickedTgIndex = i;
            tg.onValueChanged.AddListener(isOn =>
            {
                SetStar(clickedTgIndex + 1);
            });
        }
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        SetStar();
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varButtonRating)
        {
            if (m_Star >= MIN_STAR)
            {
#if UNTIY_ANDROID
                Application.OpenURL(GF.Config.GetString("AppStoreAndroid"));
#elif UNITY_IOS
                Application.OpenURL(GF.Config.GetString("AppStoreIos"));
#else
                Application.OpenURL(GF.Config.GetString("AppStoreSteam"));
#endif
                GF.UI.ShowToast(GF.Localization.GetString("RatingDialog.HighRatingTips"));
            }
            else
            {
                GF.UI.ShowToast(GF.Localization.GetString("RatingDialog.LowRatingTips"));
            }
            GF.UI.Close(this.UIForm);
        }
    }

    private void SetStar(int num = 5)
    {
        m_Star = Mathf.Clamp(num, 1, 5);
        for (int i = 0; i < varStarToggleArr.Length; i++)
        {
            varStarToggleArr[i].SetIsOnWithoutNotify((i + 1) <= m_Star);
        }
    }
}
