using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataModelBase : IReference
{
    public int Id { get; private set; } = 0;

    protected virtual void OnInit() { }

    /// <summary>
    /// 当对象回收时自动调用OnClear,常用于重置变量属性,避免复用对象时带有默认数值(脏数据)
    /// </summary>
    protected virtual void OnClear() { }
    public void Init(int id)
    {
        this.Id = id;

        OnInit();
    }
    public void Clear()
    {
        this.Id = 0;
    }

    internal void Shutdown()
    {
        ReferencePool.Release(this);
    }
}
