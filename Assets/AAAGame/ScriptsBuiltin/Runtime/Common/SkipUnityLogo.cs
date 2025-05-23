#if !UNITY_EDITOR
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

[Preserve]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class SkipUnityLogo
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
#if UNITY_WEBGL
        Application.focusChanged += Application_focusChanged;
#else
        UniTask.RunOnThreadPool(AsyncSkip).Forget();
#endif
    }

#if UNITY_WEBGL
    private static void Application_focusChanged(bool obj)
    {
        Application.focusChanged -= Application_focusChanged;
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
#else
    private static void AsyncSkip()
    {
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
#endif
}
#endif