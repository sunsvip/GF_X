using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using Obfuz;
using System;
using UnityGameFramework.Runtime;
/// <summary>
/// 热更逻辑入口
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
public class HotfixEntry
{
    public static async void StartHotfixLogic(bool enableHotfix)
    {
        Log.Info<bool>("Hotfix Enable:{0}", enableHotfix);
        AwaitExtension.SubscribeEvent();


        GFBuiltin.Fsm.DestroyFsm<IProcedureManager>();
        var fsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
        var procManager = GameFrameworkEntry.GetModule<IProcedureManager>();
        var appConfig = await AppConfigs.GetInstanceSync();

        ProcedureBase[] procedures = new ProcedureBase[appConfig.Procedures.Length];
        for (int i = 0; i < appConfig.Procedures.Length; i++)
        {
            procedures[i] = Activator.CreateInstance(Type.GetType(appConfig.Procedures[i])) as ProcedureBase;
        }
        procManager.Initialize(fsmManager, procedures);
        procManager.StartProcedure<PreloadProcedure>();
    }
}
