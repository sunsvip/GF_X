using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Cinemachine.dll",
		"DOTween.dll",
		"GameFramework.dll",
		"System.Core.dll",
		"UniTask.dll",
		"UnityEngine.AndroidJNIModule.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.UI.dll",
		"UnityGameFramework.Runtime.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<byte>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Cysharp.Threading.Tasks.UniTask<byte>
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSource<byte>
	// Cysharp.Threading.Tasks.UniTaskCompletionSource<object>
	// DG.Tweening.Core.DOGetter<UnityEngine.Vector3>
	// DG.Tweening.Core.DOGetter<float>
	// DG.Tweening.Core.DOGetter<int>
	// DG.Tweening.Core.DOSetter<UnityEngine.Vector3>
	// DG.Tweening.Core.DOSetter<float>
	// DG.Tweening.Core.DOSetter<int>
	// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>
	// GameFramework.DataTable.IDataTable<object>
	// GameFramework.Fsm.FsmState<object>
	// GameFramework.Fsm.IFsm<object>
	// GameFramework.GameFrameworkAction<ADResult>
	// GameFramework.GameFrameworkAction<byte>
	// GameFramework.GameFrameworkAction<object,object>
	// GameFramework.GameFrameworkAction<object>
	// GameFramework.Variable<UnityEngine.Vector3>
	// GameFramework.Variable<float>
	// GameFramework.Variable<int>
	// GameFramework.Variable<object>
	// System.Action<float>
	// System.Collections.Generic.Dictionary.Enumerator<TypeIdPair,object>
	// System.Collections.Generic.Dictionary<TypeIdPair,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,float>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.HashSet<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<TypeIdPair,object>
	// System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.Queue<object>
	// System.EventHandler<object>
	// System.Func<Cysharp.Threading.Tasks.UniTask>
	// System.IEquatable<TypeIdPair>
	// System.Nullable<UIFormAnimationType>
	// System.Nullable<UnityEngine.Vector3>
	// System.Nullable<byte>
	// System.Nullable<int>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.Task<object>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityEvent<byte>
	// }}

	public void RefMethods()
	{
		// object Cinemachine.CinemachineVirtualCamera.GetCinemachineComponent<object>()
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,AwaitExtension.<LoadSceneAsync>d__21>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,AwaitExtension.<LoadSceneAsync>d__21&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,AwaitExtension.<UnLoadSceneAsync>d__24>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,AwaitExtension.<UnLoadSceneAsync>d__24&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<RefParams.<>c__DisplayClass12_0.<<Get>b__0>d<object>>(RefParams.<>c__DisplayClass12_0.<<Get>b__0>d<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.Start<AwaitExtension.<LoadSceneAsync>d__21>(AwaitExtension.<LoadSceneAsync>d__21&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.Start<AwaitExtension.<UnLoadSceneAsync>d__24>(AwaitExtension.<UnLoadSceneAsync>d__24&)
		// Cysharp.Threading.Tasks.UniTask<object> Cysharp.Threading.Tasks.UniTask.FromResult<object>(object)
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions> DG.Tweening.DOTween.To<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>(DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>,DG.Tweening.Core.DOGetter<UnityEngine.Vector3>,DG.Tweening.Core.DOSetter<UnityEngine.Vector3>,object,float)
		// object DG.Tweening.TweenSettingsExtensions.SetAutoKill<object>(object)
		// object DG.Tweening.TweenSettingsExtensions.SetDelay<object>(object,float)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,DG.Tweening.Ease)
		// object DG.Tweening.TweenSettingsExtensions.SetTarget<object>(object,object)
		// object DG.Tweening.TweenSettingsExtensions.SetUpdate<object>(object,bool)
		// object GameFramework.DataNode.IDataNode.GetData<object>()
		// System.Void GameFramework.DataNode.IDataNode.SetData<object>(object)
		// System.Void GameFramework.Fsm.FsmState<object>.ChangeState<object>(GameFramework.Fsm.IFsm<object>)
		// object GameFramework.Fsm.IFsm<object>.GetData<object>(string)
		// System.Void GameFramework.Fsm.IFsm<object>.SetData<object>(string,object)
		// object GameFramework.GameFrameworkEntry.GetModule<object>()
		// System.Void GameFramework.Procedure.IProcedureManager.StartProcedure<object>()
		// object GameFramework.ReferencePool.Acquire<object>()
		// string GameFramework.Utility.Text.Format<TypeIdPair>(string,TypeIdPair)
		// string GameFramework.Utility.Text.Format<int>(string,int)
		// string GameFramework.Utility.Text.Format<object,int>(string,object,int)
		// string GameFramework.Utility.Text.Format<object,object>(string,object,object)
		// string GameFramework.Utility.Text.Format<object,ushort,object>(string,object,ushort,object)
		// string GameFramework.Utility.Text.Format<object>(string,object)
		// object[] System.Array.Empty<object>()
		// UIFormAnimationType System.Enum.Parse<UIFormAnimationType>(string)
		// bool System.Enum.TryParse<GameFramework.Localization.Language>(string,GameFramework.Localization.Language&)
		// TypeIdPair[] System.Linq.Enumerable.ToArray<TypeIdPair>(System.Collections.Generic.IEnumerable<TypeIdPair>)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,AppConfigs.<GetInstanceSync>d__14>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,AppConfigs.<GetInstanceSync>d__14&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<AppConfigs.<GetInstanceSync>d__14>(AppConfigs.<GetInstanceSync>d__14&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,HotfixEntry.<StartHotfixLogic>d__0>(System.Runtime.CompilerServices.TaskAwaiter<object>&,HotfixEntry.<StartHotfixLogic>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,PreloadProcedure.<LoadOthers>d__11>(System.Runtime.CompilerServices.TaskAwaiter<object>&,PreloadProcedure.<LoadOthers>d__11&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,PreloadProcedure.<PreloadAndInitData>d__10>(System.Runtime.CompilerServices.TaskAwaiter<object>&,PreloadProcedure.<PreloadAndInitData>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<HotfixEntry.<StartHotfixLogic>d__0>(HotfixEntry.<StartHotfixLogic>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<PreloadProcedure.<LoadOthers>d__11>(PreloadProcedure.<LoadOthers>d__11&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<PreloadProcedure.<PreloadAndInitData>d__10>(PreloadProcedure.<PreloadAndInitData>d__10&)
		// object System.Threading.Interlocked.CompareExchange<object>(object&,object,object)
		// byte UnityEngine.AndroidJavaObject.Call<byte>(string,object[])
		// object UnityEngine.AndroidJavaObject.Call<object>(string,object[])
		// object UnityEngine.AndroidJavaObject.CallStatic<object>(string,object[])
		// object UnityEngine.AndroidJavaObject.GetStatic<object>(string)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// System.Void UnityEngine.Component.GetComponentsInChildren<object>(System.Collections.Generic.List<object>)
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// bool UnityEngine.Component.TryGetComponent<object>(object&)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Vector3,UnityEngine.Quaternion,UnityEngine.Transform)
		// System.Void UnityEngine.UI.LayoutGroup.SetProperty<UnityEngine.UI.FlowLayoutGroup.Axis>(UnityEngine.UI.FlowLayoutGroup.Axis&,UnityEngine.UI.FlowLayoutGroup.Axis)
		// object UnityExtension.GetOrAddComponent<object>(UnityEngine.GameObject)
		// GameFramework.DataTable.IDataTable<object> UnityGameFramework.Runtime.DataTableComponent.GetDataTable<object>()
		// bool UnityGameFramework.Runtime.DataTableComponent.HasDataTable<object>()
		// System.Void UnityGameFramework.Runtime.EntityComponent.ShowEntity<object>(int,string,string,int,object)
		// bool UnityGameFramework.Runtime.FsmComponent.DestroyFsm<object>()
		// object UnityGameFramework.Runtime.GameEntry.GetComponent<object>()
		// System.Void UnityGameFramework.Runtime.Log.Error<int>(string,int)
		// System.Void UnityGameFramework.Runtime.Log.Error<object,object,object>(string,object,object,object)
		// System.Void UnityGameFramework.Runtime.Log.Error<object,object>(string,object,object)
		// System.Void UnityGameFramework.Runtime.Log.Error<object>(string,object)
		// System.Void UnityGameFramework.Runtime.Log.Info<byte>(string,byte)
		// System.Void UnityGameFramework.Runtime.Log.Info<object>(string,object)
		// System.Void UnityGameFramework.Runtime.Log.Warning<object>(string,object)
	}
}