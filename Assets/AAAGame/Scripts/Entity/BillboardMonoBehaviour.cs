using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardMonoBehaviour : MonoBehaviour
{
    [SerializeField] PivotAxis PivotAxis = PivotAxis.Free;

    private void Update()
    {
        Vector3 forward;
        Vector3 up;

        switch (PivotAxis)
        {
            case PivotAxis.X:
                Vector3 right = transform.right;
                forward = Vector3.ProjectOnPlane(CameraFollower.Instance.transform.forward, right).normalized;
                up = Vector3.Cross(forward, right);
                break;

            case PivotAxis.Y:
                up = transform.up;
                forward = Vector3.ProjectOnPlane(CameraFollower.Instance.transform.forward, up).normalized;
                break;
            case PivotAxis.Free:
            default:
                forward = CameraFollower.Instance.transform.forward;
                up = CameraFollower.Instance.transform.up;
                break;
        }
        transform.rotation = Quaternion.LookRotation(forward, up);
    }
}
