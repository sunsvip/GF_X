using UnityEngine;
using System.Collections;
using GameFramework;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

using UnityEngine.U2D;
using DG.Tweening;
using System;

public static class UIExtension
{
    public static bool IsPointerOverUIObject(this UIComponent uiCom, Vector3 mousePosition)
    {
        return UtilityExt.IsPointerOverUIObject(mousePosition);
    }
    public static void LoadSpriteAtlas(this UIComponent uiCom, string atlasName, GameFrameworkAction<SpriteAtlas> onSpriteAtlasLoaded)
    {
        if (GF.Resource.HasAsset(atlasName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("LoadSpriteAtlas失败, 资源不存在:{0}", atlasName);
            return;
        }

        GF.Resource.LoadAsset(atlasName, new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            var spAtlas = asset as SpriteAtlas;
            onSpriteAtlasLoaded.Invoke(spAtlas);
        }));
    }

    public static void LoadSprite(this UIComponent uiCom, string spriteName, GameFrameworkAction<Sprite> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.SetSprite()失败, 资源不存在:{0}", spriteName);
            return;
        }
        GF.Resource.LoadAsset(spriteName, new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Sprite resultSp = asset as Sprite;
            if (resultSp == null && asset != null && asset.GetType() == typeof(Texture2D))
            {
                var tex2d = asset as Texture2D;
                resultSp = Sprite.Create(asset as Texture2D, new Rect(0, 0, tex2d.width, tex2d.height), Vector2.one * 0.5f);
            }
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    public static void LoadTexture(this UIComponent uiCom, string spriteName, GameFrameworkAction<Texture2D> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.SetSprite()失败, 资源不存在:{0}", spriteName);
            return;
        }
        GF.Resource.LoadAsset(spriteName, new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Texture2D resultSp = asset as Texture2D;
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    public static void RemoveAllChildren(this UIComponent ui, Transform parent)
    {
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    public static void ShowToast(this UIComponent ui, string content, float duration = 2)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }
        var uiParams = UIParams.Acquire();
        uiParams.Set<VarString>("content", content);
        uiParams.Set<VarFloat>("duration", duration);

        ui.OpenUIForm(UIViews.ToastTips, uiParams);
    }
    public static void ShowTips(this UIComponent ui, string title, string content)
    {
        var uiParams = UIParams.Acquire();
        uiParams.Set<VarString>("title", title);
        uiParams.Set<VarString>("content", content);
        ui.OpenUIForm(UIViews.TipsDialog, uiParams);
    }

    public static int OpenUIForm(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
        var uiTb = GF.DataTable.GetDataTable<UITable>();
        int uiId = (int)viewId;
        if (!uiTb.HasDataRow(uiId))
        {
            Log.Error("UI表不存在id:{0}", uiId);
            return -1;
        }
        var uiRow = uiTb.GetDataRow(uiId);
        string uiName = UtilityBuiltin.ResPath.GetUIFormPath(uiRow.UIPrefab);
        if (uiCom.IsLoadingUIForm(uiName))
        {
            return -1;
        }
        parms ??= UIParams.Acquire();
        parms.AllowEscapeClose ??= uiRow.EscapeClose;
        parms.SortOrder ??= uiRow.SortOrder;
        parms.AnimationOpen ??= Enum.Parse<UIFormAnimationType>(uiRow.OpenAnimType);
        parms.AnimationClose ??= Enum.Parse<UIFormAnimationType>(uiRow.CloseAnimType);
        return uiCom.OpenUIForm(uiName, uiRow.UIGroup, uiRow.PauseCoveredUI, parms);
    }

    //public static int ShowDialog(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    //{
    //    return uiCom.OpenUIForm(viewId, parms);
    //}

    public static void CloseUIFormWithAnim(this UIComponent uiCom, UIForm ui)
    {
        CloseUIFormWithAnim(uiCom, ui.SerialId);
    }
    public static void CloseUIFormWithAnim(this UIComponent uiCom, int uiFormId)
    {
        if (uiCom.IsLoadingUIForm(uiFormId))
        {
            GF.UI.CloseUIForm(uiFormId);
            return;
        }
        if (!uiCom.HasUIForm(uiFormId))
        {
            return;
        }
        var uiForm = uiCom.GetUIForm(uiFormId);
        UIFormBase logic = uiForm.Logic as UIFormBase;
        logic.CloseUIWithAnim();
    }
    public static void CloseUIForms(this UIComponent uiCom, string groupName)
    {
        var group = uiCom.GetUIGroup(groupName);
        var all = group.GetAllUIForms();
        foreach (var item in all)
        {
            uiCom.CloseUIForm(item.SerialId);
        }
    }
    public static bool IsLoadingUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        return uiCom.IsLoadingUIForm(assetName);
    }
    public static bool HasUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        return uiCom.HasUIForm(assetName);
    }
    public static string GetUIFormAssetName(this UIComponent uiCom, UIViews view)
    {
        if (GF.DataTable == null || !GF.DataTable.HasDataTable<UITable>())
        {
            Log.Warning("GetUIFormAssetName is empty.");
            return string.Empty;
        }

        var uiTb = GF.DataTable.GetDataTable<UITable>();
        if (!uiTb.HasDataRow((int)view))
        {
            return string.Empty;
        }
        string uiName = UtilityBuiltin.ResPath.GetUIFormPath(uiTb.GetDataRow((int)view).UIPrefab);
        return uiName;
    }
    public static void CloseUIForms(this UIComponent uiCom, UIViews view, string uiGroup = null)
    {
        string uiAssetName = uiCom.GetUIFormAssetName(view);
        GameFramework.UI.IUIForm[] uIForms;
        if (string.IsNullOrEmpty(uiGroup))
        {
            uIForms = uiCom.GetUIForms(uiAssetName);
        }
        else
        {
            if (!uiCom.HasUIGroup(uiGroup))
            {
                return;
            }
            uIForms = uiCom.GetUIGroup(uiGroup).GetUIForms(uiAssetName);
        }

        foreach (var item in uIForms)
        {
            uiCom.CloseUIForm(item.SerialId);
        }
    }
    public static int GetTopUIFormId(this UIComponent uiCom)
    {
        var dialogGp = uiCom.GetUIGroup(Const.UIGroup.Dialog.ToString());
        if (dialogGp.CurrentUIForm != null)
        {
            return dialogGp.CurrentUIForm.SerialId;
        }
        var uiFormGp = uiCom.GetUIGroup(Const.UIGroup.UIForm.ToString());
        if (uiFormGp.CurrentUIForm != null)
        {
            return uiFormGp.CurrentUIForm.SerialId;
        }
        return -1;
    }
    public static void ShowRewardEffect(this UIComponent uiCom, Vector3 centerPos, Vector3 fly2Pos, float flyDelay = 0.5f, GameFrameworkAction onAnimComplete = null, int num = 30)
    {
        int coinNum = num;
        DOVirtual.DelayedCall(flyDelay, () =>
        {
            GF.Sound.PlayEffect("add_money.wav");
        });
        var richText = "<sprite name=USD_0>";
        for (int i = 0; i < num; i++)
        {
            var animPrams = EntityParams.Acquire(centerPos, Vector3.zero, Vector3.one);
            VarObject onShowCb = new VarObject();
            onShowCb.SetValue(new GameFrameworkAction<EntityLogic>(moneyEntity =>
            {
                moneyEntity.GetComponent<TMPro.TextMeshPro>().text = richText;
                var spawnPos = UnityEngine.Random.insideUnitCircle * 3;
                var expPos = centerPos;
                expPos.x += spawnPos.x;
                expPos.y += spawnPos.y;
                var targetPos = fly2Pos;
                int moneyEntityId = moneyEntity.Entity.Id;
                var expDuration = Vector2.Distance(moneyEntity.transform.position, expPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(moneyEntity.transform.position, expPos)*0.01f, 0.1f, 0.4f);
                var animSeq = DOTween.Sequence();
                animSeq.Append(moneyEntity.transform.DOMove(expPos, expDuration));
                animSeq.AppendInterval(0.25f);
                var moveDuration = Vector2.Distance(expPos, targetPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(expPos, targetPos)*0.01f, 0.1f, 0.8f);
                animSeq.Append(moneyEntity.transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear));
                animSeq.onComplete = () =>
                {
                    GF.Entity.HideEntitySafe(moneyEntityId);
                    coinNum--;
                    if (coinNum <= 0)
                    {
                        onAnimComplete?.Invoke();
                    }
                    GF.Sound.PlayEffect("Collect_Gem_2.wav");
                    GF.Sound.PlayVibrate();
                };
            }));
            animPrams.Set<VarObject>("OnShow", onShowCb);
            GF.Entity.ShowEntity<SampleEntity>("Effect/EffectMoney", Const.EntityGroup.Effect, animPrams);
        }
    }
}
