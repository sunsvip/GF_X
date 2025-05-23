[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class LanguagesDialog : UIFormBase
{
    public const string P_LangChangedCb = "LangChangedCb";
    VarAction m_VarAction;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_VarAction = Params.Get<VarAction>(P_LangChangedCb);
        RefreshList();
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        //UnspawnAll();
    }
    void RefreshList()
    {
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        foreach (var lang in langTb)
        {
            var item = this.SpawnItem<UIItemObject>(varLanguageToggle, varToggleGroup.transform);
            (item.itemLogic as LanguageItem).SetData(lang, varToggleGroup, m_VarAction);
        }
    }
}
