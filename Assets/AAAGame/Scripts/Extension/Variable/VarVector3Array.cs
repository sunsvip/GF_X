using GameFramework;
using UnityEngine;
/// <summary>
/// UnityEngine.Vector3 数组变量类。
/// </summary>
public sealed class VarVector3Array : Variable<Vector3[]>
{
    public VarVector3Array()
    {
    }


    public static implicit operator VarVector3Array(Vector3[] value)
    {
        VarVector3Array varValue = ReferencePool.Acquire<VarVector3Array>();
        varValue.Value = value;
        return varValue;
    }

    public static implicit operator Vector3[](VarVector3Array value)
    {
        return value.Value;
    }
}