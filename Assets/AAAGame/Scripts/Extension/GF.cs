using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class GF : GFBuiltin
{
    public static DataModelComponent DataModel { get; private set; }
    public static ADComponent AD { get; private set; }
    public static StaticUIComponent StaticUI { get; private set; } //无需异步加载的, 通用UI

    private void Start()
    {
        DataModel = GameEntry.GetComponent<DataModelComponent>();
        AD = GameEntry.GetComponent<ADComponent>();
        StaticUI = GameEntry.GetComponent<StaticUIComponent>();
    }

    private void OnApplicationQuit()
    {
        OnExitGame();
    }
    private void OnApplicationPause(bool pause)
    {
        //Log.Info("OnApplicationPause:{0}", pause);
        if (Application.isMobilePlatform && pause)
        {
            OnExitGame();
        }
    }
    public Vector2 GetCanvasSize()
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        return rect.sizeDelta;
    }
    public Vector2 World2ScreenPoint(Camera cam, Vector3 worldPoint)
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        Vector2 sPoint = cam.WorldToViewportPoint(worldPoint) * rect.sizeDelta;
        return sPoint - rect.sizeDelta * 0.5f;
    }
    private void OnExitGame()
    {
        GF.Event.FireNow(this, ReferencePool.Acquire<PlayerEventArgs>().Fill(PlayerEventType.ExitGame));
        var exit_time = DateTime.UtcNow.ToString();
        GF.Setting.SetString(ConstBuiltin.Setting.QuitAppTime, exit_time);
        GF.Setting.Save();
        Log.Info("Exit Time:{0}", exit_time);
    }
}
