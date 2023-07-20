using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class UIParams : RefParams
{
    const string KeyAllowEscapeClose = "AllowEscapeClose";//UI支持返回操作
    const string KeySortOrder = "CanvasSortOrder";//UI支持返回操作
    const string KeyAnimationOpen = "AnimationOpen";
    const string KeyAnimationClose = "AnimationClose";
    const string KeyOpenCallback = "OpenCallback";
    const string KeyCloseCallback = "CloseCallback";
    const string KeyOnButtonClick = "OnButtonClick";
    public static UIParams Acquire(bool? allowEscape = null, int? sortOrder = null, UIFormAnimationType? animOpen = null, UIFormAnimationType? animClose = null)
    {
        var uiParms = ReferencePool.Acquire<UIParams>();
        uiParms.CreateRoot();
        if (allowEscape != null) uiParms.AllowEscapeClose = allowEscape;
        if (sortOrder != null) uiParms.SortOrder = sortOrder;
        if (animOpen != null) uiParms.AnimationOpen = animOpen;
        if (animClose != null) uiParms.AnimationClose = animClose;
        return uiParms;
    }
    public VarBoolean AllowEscapeClose
    {
        get => Get<VarBoolean>(KeyAllowEscapeClose);
        set
        {
            Set<VarBoolean>(KeyAllowEscapeClose, value);
        }
    }
    public VarInt32 SortOrder
    {
        get => Get<VarInt32>(KeySortOrder);
        set
        {
            Set<VarInt32>(KeySortOrder, value);
        }
    }
    public UIFormAnimationType? AnimationOpen
    {
        get
        {
            if (Has(KeyAnimationOpen))
                return (UIFormAnimationType)Get<VarInt32>(KeyAnimationOpen, (int)UIFormAnimationType.None).Value;
            else
                return null;
        }
        set => Set<VarInt32>(KeyAnimationOpen, (int)value);
    }
    public UIFormAnimationType? AnimationClose
    {
        get
        {
            if (Has(KeyAnimationClose))
                return (UIFormAnimationType)Get<VarInt32>(KeyAnimationClose, (int)UIFormAnimationType.None).Value;
            else
                return null;
        }
        set { Set<VarInt32>(KeyAnimationClose, (int)value); }
    }
    public GameFrameworkAction<UIFormLogic> OnOpenCallback
    {
        get
        {
            if (Has(KeyOpenCallback))
            {
                return Get<VarObject>(KeyOpenCallback).Value as GameFrameworkAction<UIFormLogic>;
            }
            return null;
        }
        set
        {
            var obj = ReferencePool.Acquire<VarObject>();
            obj.SetValue(value);
            Set<VarObject>(KeyOpenCallback, obj);
        }
    }
    public GameFrameworkAction<UIFormLogic> OnCloseCallback
    {
        get
        {
            if (Has(KeyCloseCallback))
            {
                return Get<VarObject>(KeyCloseCallback).Value as GameFrameworkAction<UIFormLogic>;
            }
            return null;
        }
        set
        {
            var obj = ReferencePool.Acquire<VarObject>();
            obj.SetValue(value);
            Set<VarObject>(KeyCloseCallback, obj);
        }
    }
    public GameFrameworkAction<object, string> OnButtonClick
    {
        get
        {
            if (Has(KeyOnButtonClick))
            {
                return Get<VarObject>(KeyOnButtonClick).Value as GameFrameworkAction<object, string>;
            }
            return null;
        }
        set
        {
            var obj = ReferencePool.Acquire<VarObject>();
            obj.SetValue(value);
            Set<VarObject>(KeyOnButtonClick, obj);
        }
    }
}
