using UnityEngine;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Globalization;

public class LunchProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.InitSettings();
        ChangeState(procedureOwner, GFBuiltin.Base.EditorResourceMode ? typeof(LoadHotfixDllProcedure) : typeof(CheckAndUpdateProcedure));
    }

    private void InitSettings()
    {
        CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
        GFBuiltin.Debugger.ActiveWindow = AppSettings.Instance.DebugMode;
        GFBuiltin.Debugger.WindowScale = 1.4f;
    }
}
