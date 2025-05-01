
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class StaticUIComponent : GameFrameworkComponent
{
    [Header("Waiting View:")]
    [SerializeField] GameObject waitingView = null;

    //[SerializeField] UltimateJoystick mJoystick;
    //public bool JoystickEnable
    //{
    //    get { return mJoystick.gameObject.activeSelf; }
    //    set
    //    {
    //        if (value)
    //        {
    //            mJoystick.EnableJoystick();
    //            mJoystick.UpdatePositioning();
    //        }
    //        else
    //        {
    //            mJoystick.DisableJoystick();
    //        }
    //    }
    //}
    //public UltimateJoystick Joystick { get { return mJoystick; } }
    [SerializeField] SimpleJoystick m_Joystick;
    public SimpleJoystick Joystick { get => m_Joystick; }
    private void Start()
    {
        UpdateCanvasScaler();
        waitingView.SetActive(false);
        Joystick.Enable = false;
    }

    public void UpdateCanvasScaler()
    {
        var uiRootCanvas = GFBuiltin.RootCanvas;
        var canvasRoot = this.GetComponent<Canvas>();
        canvasRoot.worldCamera = uiRootCanvas.worldCamera;
        canvasRoot.planeDistance = uiRootCanvas.planeDistance;
        canvasRoot.sortingLayerID = uiRootCanvas.sortingLayerID;
        canvasRoot.sortingOrder = uiRootCanvas.sortingOrder;

        var canvasScaler = this.GetComponent<CanvasScaler>();
        var uiRootScaler = uiRootCanvas.GetComponent<CanvasScaler>();

        canvasScaler.uiScaleMode = uiRootScaler.uiScaleMode;
        canvasScaler.screenMatchMode = uiRootScaler.screenMatchMode;
        canvasScaler.matchWidthOrHeight = uiRootScaler.matchWidthOrHeight;
        canvasScaler.referencePixelsPerUnit = uiRootScaler.referencePixelsPerUnit;
        canvasScaler.referenceResolution = uiRootScaler.referenceResolution;
    }
}
