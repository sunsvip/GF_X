using GameFramework;
using UnityGameFramework.Runtime;

public class UIParams : RefParams
{
    public bool? AllowEscapeClose { get; set; } = null;
    public int? SortOrder { get; set; } = null;
    public UIFormAnimationType AnimationOpen { get; set; } = UIFormAnimationType.None;
    public UIFormAnimationType AnimationClose { get; set; } = UIFormAnimationType.None;
    public GameFrameworkAction<UIFormLogic> OpenCallback { get; set; } = null;
    public GameFrameworkAction<UIFormLogic> CloseCallback { get; set; } = null;
    public GameFrameworkAction<object, string> ButtonClickCallback { get; set; } = null;
    public static UIParams Create(bool? allowEscape = null, int? sortOrder = null, UIFormAnimationType animOpen = UIFormAnimationType.Default, UIFormAnimationType animClose = UIFormAnimationType.Default)
    {
        var uiParms = ReferencePool.Acquire<UIParams>();
        uiParms.CreateRoot();
        uiParms.AllowEscapeClose = allowEscape;
        uiParms.SortOrder = sortOrder;
        uiParms.AnimationOpen = animOpen;
        uiParms.AnimationClose = animClose;
        return uiParms;
    }


    protected override void ClearDirtyData()
    {
        base.ClearDirtyData();
        AllowEscapeClose = null;
        SortOrder = null;
        AnimationOpen = UIFormAnimationType.Default;
        AnimationClose = UIFormAnimationType.Default;
        OpenCallback = null;
        CloseCallback = null;
        ButtonClickCallback = null;
    }
}
