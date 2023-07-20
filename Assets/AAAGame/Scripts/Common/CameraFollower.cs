using UnityEngine;
using DG.Tweening;
using Cinemachine;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;

public class CameraFollower : MonoBehaviour
{
    public static CameraFollower Instance { get; private set; }
    internal Vector3 GetTargetPosition()
    {
        if (target == null)
        {
            return Vector3.zero;
        }
        return target.position;
    }

    Transform target;
    [SerializeField] CinemachineVirtualCamera followerVCamera;
    Vector3 initOffset = Vector3.zero;
    public Camera mainCam { get; private set; }


    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }
    private void OnEnable()
    {
        //mainCam.cullingMask = ~LayerMask.GetMask("UI");
        InitURP();
    }

    private void Start()
    {
        
    }

    private void InitURP()
    {
        //var curRenderMode = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name;
        var urpCam = mainCam.GetUniversalAdditionalCameraData();
        if (GFBuiltin.UICamera.GetUniversalAdditionalCameraData().renderType != CameraRenderType.Overlay)
        {
            GFBuiltin.UICamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
        }
        urpCam.cameraStack.Add(GFBuiltin.UICamera);

    }
    public void SetViewZoom(float height)
    {
        float offset = Mathf.Max(initOffset.y, height + height * Mathf.Tan(15 * Mathf.Deg2Rad));
        SwitchCameraView(new Vector3(0, offset, -offset), Vector3.zero);
    }
    public void SetFollowTarget(Transform target)
    {
        this.target = target;
        followerVCamera.gameObject.SetActive(true);
        followerVCamera.LookAt = target;
        followerVCamera.Follow = target;
        mainCam.orthographic = false;
        SetCameraView(1, false);
    }

    internal void SetCameraView(int viewId, bool smooth = true)
    {
        var camTb = GF.DataTable.GetDataTable<CameraViewTable>();
        if (!camTb.HasDataRow(viewId))
        {
            return;
        }
        var camRow = camTb.GetDataRow(viewId);
        initOffset = camRow.FollowOffset;
        SwitchCameraView(camRow.FollowOffset, camRow.AimOffset, smooth);
    }
    internal void ShakeCamera(float power = 1f)
    {
        var imp = followerVCamera.GetComponent<CinemachineImpulseSource>();
        imp.GenerateImpulse(power);
    }
    internal void SwitchCameraView(Vector3 offset, Vector3 aimOffset, bool smooth = true)
    {
        var transposer = followerVCamera.GetCinemachineComponent<CinemachineTransposer>();
        var aimCom = followerVCamera.GetCinemachineComponent<CinemachineComposer>();
        if (!smooth)
        {
            transposer.m_FollowOffset = offset;
            aimCom.m_TrackedObjectOffset = aimOffset;
            return;
        }
        float duration = Mathf.Clamp(Vector3.Distance(transposer.m_FollowOffset, offset) * 0.5f, 0f, 1f);
        DOTween.To(() => transposer.m_FollowOffset, x => transposer.m_FollowOffset = x, offset, duration);
        DOTween.To(() => aimCom.m_TrackedObjectOffset, x => aimCom.m_TrackedObjectOffset = x, aimOffset, duration);
    }
}
