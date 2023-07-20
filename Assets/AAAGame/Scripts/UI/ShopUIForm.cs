using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public class ShopUIForm : UIFormBase
{
    //[SerializeField] RawImage carPreview;
    //[SerializeField] RenderTexture carRenderTex;
    //[SerializeField] ScrollRect listView;
    //[SerializeField] Button[] bottomBts;
    //[SerializeField] Button[] pageUpDownBts;
    //[SerializeField] Text moneyNumText;
    //[SerializeField] Transform hpNumNode;
    //[SerializeField] Slider[] carSliders;
    //private float maxEnginePower;
    //private float maxBrakePower;
    //private float maxSteerPower;
    //private List<CarTable> skinList;
    //private Dictionary<int, Button> skinItems;
    //private int curPageIdx;
    //private int maxPageIdx;
    //private bool isLoading;

    //private int mCurSelectId;
    //public int CurSelectId
    //{
    //    get { return mCurSelectId; }
    //    set
    //    {
    //        if (CurSelectId != value)
    //        {
    //            PlayItmeAnim(CurSelectId, false);
    //            SetPreviewCar(value);
    //        }
    //        else
    //        {
    //            if (carEntity == null) SetPreviewCar(value);
    //        }

    //        mCurSelectId = value;
    //        PlayItmeAnim(CurSelectId, true);
    //        RefreshCarInfo(GF.DataTable.GetDataTable<CarTable>().GetDataRow(CurSelectId));
    //    }
    //}

    //private Camera renderCam;
    //private int carEntityId;
    //private Entity carEntity;
    //float carRotateSpeed = 10f;
    //bool claimFreeSkin;
    //Vector3 previewPos = new Vector3(50, 0, 50);
    //private void PlayItmeAnim(int selectId, bool isSelected)
    //{
    //    if (!skinItems.ContainsKey(selectId))
    //    {
    //        return;
    //    }
    //    var item = skinItems[selectId];
    //    var img = item.transform.Find("Checkmark").GetComponent<Image>();
    //    var carIconNode = item.transform.Find("Icon");
    //    float tScale = isSelected ? 1.25f : 1;
    //    float duration = Mathf.Abs(tScale - carIconNode.localScale.x) * 0.5f;
    //    carIconNode.DOScale(tScale, duration);
    //    if (isSelected)
    //    {
    //        img.DOFade(1, (1 - img.color.a) * 0.4f);
    //    }
    //    else
    //    {
    //        img.DOFade(0, img.color.a * 0.4f);
    //    }
    //}
    //protected override void OnInit(object userData)
    //{
    //    base.OnInit(userData);
    //    skinList = new List<CarTable>();
    //    GF.DataTable.GetDataTable<CarTable>().GetAllDataRows(skinList);
    //    skinList.Sort((rowA, rowB) => { return rowA.ShopOrder.CompareTo(rowB.ShopOrder); });
    //    maxPageIdx = Mathf.CeilToInt(skinList.Count / 9f) - 1;
    //    skinItems = new Dictionary<int, Button>();
    //    //bottomBts[1].interactable = false;

    //    foreach (var item in skinList)
    //    {
    //        if (maxEnginePower < item.MotorPower)
    //        {
    //            maxEnginePower = item.MotorPower;
    //        }
    //        if (maxBrakePower < item.BrakePower)
    //        {
    //            maxBrakePower = item.BrakePower;
    //        }
    //        if (maxSteerPower < item.MaxSteerAngle)
    //        {
    //            maxSteerPower = item.MaxSteerAngle;
    //        }
    //    }
    //}
    //protected override void OnOpen(object userData)
    //{
    //    base.OnOpen(userData);
    //    GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    //    InitPreview();

    //    RefreshButtonText();
    //    SetMoneyText(GF.UserData.MONEY);
    //    claimFreeSkin = UIParms.ContainsKey("FREE_SKIN") && (bool)UIParms["FREE_SKIN"];
    //    RefreshPage(0, ()=> {
    //        StartCoroutine(WaitAndAutoBuySkin());
    //    });
    //}
    //IEnumerator WaitAndAutoBuySkin()
    //{
    //    if (!claimFreeSkin)
    //    {
    //        yield return null;
    //    }
    //    var waitFrame = new WaitForEndOfFrame();
    //    while (claimFreeSkin)
    //    {
    //        if (isLoading == false && GF.Entity.IsValidEntity(carEntity))
    //        {
    //            BuySkin(claimFreeSkin);
    //            claimFreeSkin = false;
    //        }
    //        yield return waitFrame;
    //    }
    //}
    //protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    //{
    //    base.OnUpdate(elapseSeconds, realElapseSeconds);
    //    if (carEntity != null)
    //    {
    //        carEntity.transform.Rotate(Vector3.up, Time.deltaTime * carRotateSpeed);
    //    }
    //}
    //protected override void OnClose(bool isShutdown, object userData)
    //{
    //    GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    //    if (!isShutdown)
    //    {
    //        if (renderCam != null)
    //        {
    //            Destroy(renderCam.gameObject);
    //        }
    //        GF.Entity.HideEntitySafe(carEntityId);
    //    }
    //    base.OnClose(isShutdown, userData);
    //}
    //private void InitPreview()
    //{
    //    renderCam = new GameObject("RenderCamera").AddComponent<Camera>();
    //    renderCam.orthographic = false;
    //    renderCam.transform.position = previewPos + new Vector3(0, 2.15f, 5);
    //    renderCam.transform.rotation = Quaternion.Euler(15, 180, 0);
    //    renderCam.clearFlags = CameraClearFlags.SolidColor;
    //    renderCam.cullingMask = LayerMask.GetMask("Shop");
    //    renderCam.targetTexture = carRenderTex;
    //    carPreview.texture = carRenderTex;
        
    //    StartCoroutine(RefreshPreviewImg());
    //}
    //IEnumerator RefreshPreviewImg()
    //{
    //    yield return new WaitForEndOfFrame();
    //    var layout = carPreview.GetComponent<LayoutElement>();
    //    layout.preferredWidth = carPreview.rectTransform.sizeDelta.y;
    //}
    //private void SetPreviewCar(int carId)
    //{
    //    //Vector3 rot = carEntity != null ? carEntity.transform.rotation.eulerAngles : Vector3.up * 45f;
    //    GF.Entity.HideEntitySafe(carEntityId);
    //    carEntity = null;
    //    isLoading = true;
    //    var carTb = GF.DataTable.GetDataTable<CarTable>();
    //    var carRow = carTb.GetDataRow(carId);
    //    var carParms = new Dictionary<string, object>
    //    {
    //        ["layer"] = "Shop",
    //        ["position"] = previewPos,
    //        ["rotation"] = Vector3.up * 45f,
    //        ["OnShow"] = new GameFrameworkAction<Entity>(entity =>
    //        {
    //            carEntity = entity;
    //            isLoading = false;
    //        })
    //    };
    //    carEntityId = GF.Entity.ShowEntity<SampleEntity>(UtilityBuiltin.ResPath.GetEntityPath(carRow.PfbName), Const.EntityGroup.UnRecycle, carParms);
    //}
    //private void OnUserDataChanged(object sender, GameEventArgs e)
    //{
    //    var args = e as UserDataChangedEventArgs;
    //    switch (args.Type)
    //    {
    //        case UserDataType.MONEY:
    //            SetMoneyText((int)args.Value);
    //            break;
    //        case UserDataType.AD2MONEY_LV:
    //            RefreshButtonText();
    //            break;
    //        case UserDataType.CAR_SKIN_ID:
    //            break;
    //        case UserDataType.OWN_CARS:
    //            RefreshButtonText();
    //            break;
    //    }
    //}
    //private void RefreshCarInfo(CarTable carRow)
    //{
    //    SetCarHp(carRow.Hp);
    //    float engine = carRow.MotorPower / maxEnginePower;
    //    carSliders[0].value = engine;
    //    carSliders[0].transform.Find("ProgressText").GetComponent<Text>().text = engine.ToString("0.00%");

    //    float brake = carRow.BrakePower / maxBrakePower;
    //    carSliders[1].value = brake;
    //    carSliders[1].transform.Find("ProgressText").GetComponent<Text>().text = brake.ToString("0.00%");

    //    float steering = carRow.MaxSteerAngle / maxSteerPower;
    //    carSliders[2].value = steering;
    //    carSliders[2].transform.Find("ProgressText").GetComponent<Text>().text = steering.ToString("0.00%");
    //}
    //private void SetCarHp(int num)
    //{
    //    var itemPfb = hpNumNode.GetChild(0).gameObject;
    //    GameObject item;
    //    for (int i = 0; i < num; i++)
    //    {
    //        if (i < hpNumNode.childCount)
    //        {
    //            item = hpNumNode.GetChild(i).gameObject;
    //        }
    //        else
    //        {
    //            item = Instantiate(itemPfb, hpNumNode);
    //        }
    //        item.SetActive(true);
    //    }
    //    for (int i = num; i < hpNumNode.childCount; i++)
    //    {
    //        hpNumNode.GetChild(i).gameObject.SetActive(false);
    //    }
    //}
    ///// <summary>
    ///// 上下翻页
    ///// </summary>
    ///// <param name="dir"></param>
    //public void ChangePage(int dir)
    //{
    //    RefreshPage(curPageIdx + dir);
    //}
    //private void RefreshPage(int idx, GameFrameworkAction onRefreshComplete = null)
    //{
    //    if (idx < 0 || idx > maxPageIdx || isLoading)
    //    {
    //        return;
    //    }
    //    isLoading = true;
    //    GF.UI.LoadSpriteAtlas(UtilityBuiltin.ResPath.GetTexturePath("carpreview.spriteatlas"), carIconAtlas =>
    //    {
    //        curPageIdx = idx;
    //        pageUpDownBts[0].interactable = curPageIdx > 0;
    //        pageUpDownBts[1].interactable = curPageIdx < maxPageIdx;
    //        RefreshPageDots(curPageIdx);
    //        skinItems.Clear();
    //        int startIdx = curPageIdx * 9;
    //        int endIdx = Mathf.Min(startIdx + 9, skinList.Count);
    //        var ownCars = GF.UserData.GetOwnCars();
    //        for (int i = 0; i < listView.content.childCount; i++)
    //        {
    //            var item = listView.content.GetChild(i);
    //            int curItemIdx = startIdx + i;
    //            bool isOut = curItemIdx >= endIdx;
    //            item.gameObject.SetActive(!isOut);
    //            if (!isOut)
    //            {
    //                var itemData = skinList[curItemIdx];
    //                SetItemData(item, itemData.Id, carIconAtlas.GetSprite(itemData.PreviewImg), ownCars.Contains(itemData.Id));
    //            }
    //        }
    //        isLoading = false;
    //        CurSelectId = GF.UserData.CAR_SKIN_ID;
    //        onRefreshComplete?.Invoke();
    //    });
    //}
    //private void RefreshPageDots(int pageIdx)
    //{
    //    var pageDotsNode = listView.transform.Find("PageDots");
    //    var dotItem = pageDotsNode.GetChild(0).gameObject;

    //    for (int i = pageDotsNode.childCount; i < maxPageIdx + 1; i++)
    //    {
    //        Instantiate(dotItem, pageDotsNode);
    //    }
    //    pageDotsNode.GetChild(pageIdx).GetComponent<Toggle>().isOn = true;
    //}
    //private void SetItemData(Transform item, int id, Sprite sprite, bool isUnlocked)
    //{
    //    var itemBt = item.GetComponent<Button>();
    //    var img = itemBt.transform.Find("Checkmark").GetComponent<Image>();
    //    var imgCol = img.color;
    //    imgCol.a = 0;
    //    img.color = imgCol;
    //    var itemGlow = itemBt.transform.Find("Glow").GetComponent<Image>();
    //    var glowCol = itemGlow.color;
    //    glowCol.a = 0;
    //    itemGlow.color = glowCol;
    //    //itemGlow.gameObject.SetActive(!GF.UserData.GetOwnCars().Contains(id));
    //    //bool isUnlocked = GF.UserData.GetOwnCars().Contains(id);
    //    itemBt.interactable = isUnlocked;
    //    var iconImg = item.Find("Icon").GetComponent<Image>();
    //    iconImg.transform.localScale = Vector3.one * (CurSelectId == id ? 1.25f : 1);
    //    iconImg.sprite = sprite;
    //    iconImg.SetNativeSize();
    //    iconImg.enabled = isUnlocked;
    //    skinItems.Add(id, itemBt);
    //    itemBt.onClick.RemoveAllListeners();
    //    itemBt.onClick.AddListener(() =>
    //    {
    //        OnItemClick(id);
    //    });
    //}
    //private void OnItemClick(int carId)
    //{
    //    if (isLoading)
    //    {
    //        return;
    //    }
    //    if (CurSelectId == carId || !GF.UserData.GetOwnCars().Contains(carId))
    //    {
    //        return;
    //    }
    //    GF.UserData.CAR_SKIN_ID = carId;
    //    CurSelectId = carId;
    //}
    //public override void OnButtonClick(object sender, string bt_tag)
    //{
    //    base.OnButtonClick(sender, bt_tag);
    //    if (isLoading)
    //    {
    //        return;
    //    }
    //    switch (bt_tag)
    //    {
    //        case "BUY":
    //            BuySkin();
    //            break;
    //        case "AD":
    //            ClaimAd2Money();
    //            break;
    //        case "BACK":
    //            GF.UI.HideUIForm(this.UIForm);
    //            break;
    //    }
    //}
    //private int GetCarInPage(int carId)
    //{
    //    for (int i = 0; i < skinList.Count; i++)
    //    {
    //        var carRow = skinList[i];
    //        if (carRow.Id == carId)
    //        {
    //            return Mathf.CeilToInt((i+1) / 9f) - 1;
    //        }
    //    }
    //    return -1;
    //}
    //private void BuySkin(bool isFree = false)
    //{
    //    var nextUnlockCar = GF.UserData.GetNextUnlockCarId();

    //    var skinTb = GF.DataTable.GetDataTable<CarTable>();
    //    int skinPrice = skinTb.GetDataRow(nextUnlockCar).PriceNum;
    //    if (!isFree && GF.UserData.MONEY < skinPrice)
    //    {
    //        var tips = Utility.Text.Format(GF.Localization.GetLocalString("You need {0} coins more to unlock the next one."), skinPrice - GF.UserData.MONEY);
    //        GF.AD.ShowToast(tips);
    //        return;
    //    }
    //    var pageIdx = GetCarInPage(nextUnlockCar);
    //    //Log.Info("NextUnlock car in Page:{0},nextCarId:{1}",pageIdx, nextUnlockCar);
        
    //    RefreshPage(pageIdx, ()=> {
    //        isLoading = true;
    //        int startIdx = curPageIdx * 9;
    //        int endIdx = Mathf.Min(startIdx + 9, skinList.Count);
    //        var remainSkins = new List<CarTable>();
    //        var ownSkins = GF.UserData.GetOwnCars();

    //        int targetIdx = -1;
    //        for (int i = startIdx; i < endIdx; i++)
    //        {
    //            var carRow = skinList[i];
    //            if (!ownSkins.Contains(carRow.Id))
    //            {
    //                remainSkins.Add(carRow);
    //                if (carRow.Id == nextUnlockCar)
    //                {
    //                    targetIdx = remainSkins.Count - 1;
    //                }
    //            }
    //        }
    //        GF.UserData.MONEY -= skinPrice;
    //        GF.UserData.AddOwnCar(nextUnlockCar);
    //        int toValue = 0;
    //        int endValue = (remainSkins.Count) * 2 + targetIdx;
    //        var animSeq = DOTween.Sequence();
    //        for (int i = toValue; i <= endValue; i++)
    //        {
    //            var skinRow = remainSkins[i % remainSkins.Count];
    //            var glowImg = skinItems[skinRow.Id].transform.Find("Glow").GetComponent<Image>();
    //            animSeq.Append(glowImg.DOFade(1f, 0.1f));
    //            animSeq.Append(glowImg.DOFade(0, 0.1f));
    //            //animSeq.AppendInterval(0.1f);
    //        }

    //        animSeq.onComplete = () =>
    //        {
    //            isLoading = false;
    //            GF.UserData.CAR_SKIN_ID = nextUnlockCar;
    //            RefreshPage(pageIdx);
    //            GF.AD.SendEvent("Purchase_Skins", "How many skins player bought.");
    //        };
    //    });
    //}
    //private void ClaimAd2Money()
    //{
    //    GF.AD.ShowVideoAd(() =>
    //    {
    //        GF.AD.SendEvent("Rewarded_click_ShopAdBt", "How many ShopAdBt player clicked");
    //        isLoading = true;
    //        int rewardCoin = GF.UserData.AD2MONEY;
    //        GF.UI.ShowRewardEffect(Vector3.zero, moneyNumText.transform.position - Vector3.right * 0.25f);
    //        var seqAct = DOTween.Sequence();
    //        seqAct.AppendInterval(0.5f);
    //        seqAct.onComplete = () =>
    //        {
    //            GF.UserData.MONEY += rewardCoin;
    //            GF.UserData.AD2MONEY_LV++;
    //            isLoading = false;
    //        };
    //    });
    //}


    //private void RefreshButtonText()
    //{
    //    var skinTb = GF.DataTable.GetDataTable<CarTable>();
    //    var buyBtText = bottomBts[0].transform.Find("PriceText").GetComponent<Text>();
    //    int nextCarId = GF.UserData.GetNextUnlockCarId();
    //    bool lvMax = GF.UserData.GetOwnCars().Contains(nextCarId);
    //    bottomBts[0].interactable = !lvMax;
    //    buyBtText.text = skinTb.GetDataRow(nextCarId).PriceNum.ToString();

    //    var adRewardText = bottomBts[1].GetComponentInChildren<Text>();
    //    adRewardText.text = Utility.Text.Format("+{0}", GF.UserData.AD2MONEY);
    //}
    //private void SetMoneyText(int coins)
    //{
    //    moneyNumText.text = UtilityBuiltin.Valuer.ToCoins(coins);
    //}
}
