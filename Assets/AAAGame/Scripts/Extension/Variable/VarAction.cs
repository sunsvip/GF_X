using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VarAction : Variable<Action>
{
    /// <summary>
    /// 初始化 System.Float 变量类的新实例。
    /// </summary>
    public VarAction()
    {
    }

    /// <summary>
    /// 从 System.Float 到 System.Float 变量类的隐式转换。
    /// </summary>
    /// <param name="value">值。</param>
    public static implicit operator VarAction(Action value)
    {
        VarAction varValue = ReferencePool.Acquire<VarAction>();
        varValue.Value = value;
        return varValue;
    }

    /// <summary>
    /// 从 System.Float 变量类到 System.Float 的隐式转换。
    /// </summary>
    /// <param name="value">值。</param>
    public static implicit operator Action(VarAction value)
    {
        return value.Value;
    }
}
