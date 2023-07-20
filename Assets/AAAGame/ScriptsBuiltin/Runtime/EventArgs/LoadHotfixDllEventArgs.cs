using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadHotfixDllEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(LoadHotfixDllEventArgs).GetHashCode();
    public override int Id => EventId;
    public string DllName { get; private set; }
    public System.Reflection.Assembly Assembly { get; private set; }
    public object UserData { get; private set; }
    public LoadHotfixDllEventArgs Fill(string dllName, System.Reflection.Assembly dll, object userdata)
    {
        this.DllName = dllName;
        this.Assembly = dll;
        this.UserData = userdata;
        return this;
    }
    public override void Clear()
    {
        this.DllName = default;
        this.Assembly = null;
        this.UserData = null;
    }
}
