using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
[Serializable]
public enum PivotAxis
{
    // Rotate about all axes.
    Free,
    // Rotate about an individual axis.
    X,
    Y
}
public class BillboardEntity : SampleEntity
{
    PivotAxis PivotAxis = PivotAxis.Free;
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        if (Params.TryGet<VarInt32>("Axis", out var tempAxis))
        {
            PivotAxis = (PivotAxis)tempAxis.Value;
        }
        else
        {
            PivotAxis = PivotAxis.Free;
        }
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        Vector3 forward;
        Vector3 up;

        switch (PivotAxis)
        {
            case PivotAxis.X:
                Vector3 right = transform.right;
                forward = Vector3.ProjectOnPlane(CameraController.Instance.transform.forward, right).normalized;
                up = Vector3.Cross(forward, right);
                break;

            case PivotAxis.Y:
                up = transform.up;
                forward = Vector3.ProjectOnPlane(CameraController.Instance.transform.forward, up).normalized;
                break;
            case PivotAxis.Free:
            default:
                forward = CameraController.Instance.transform.forward;
                up = CameraController.Instance.transform.up;
                break;
        }
        transform.rotation = Quaternion.LookRotation(forward, up);
    }
}
