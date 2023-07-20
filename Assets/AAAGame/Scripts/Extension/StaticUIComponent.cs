using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using UnityEngine.UI;

public class StaticUIComponent : GameFrameworkComponent
{
    [Header("Waiting View:")]
    [SerializeField] GameObject waitingView = null;

    [SerializeField] UltimateJoystick mJoystick;
    public bool JoystickEnable
    {
        get { return mJoystick.gameObject.activeSelf; }
        set
        {
            if (value)
            {
                mJoystick.EnableJoystick();
                StartCoroutine(RefreshJoystickPosition());
            }
            else
            {
                mJoystick.DisableJoystick();
            }
            mJoystick.disableVisuals = !value;
        }
    }
    public UltimateJoystick Joystick { get { return mJoystick; } }

    private void OnEnable()
    {
        waitingView.SetActive(false);
    }
    private void Start()
    {
        mJoystick.disableVisuals = true;
        UpdateCanvasScaler();
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
    }
    IEnumerator RefreshJoystickPosition() { yield return new WaitForEndOfFrame(); mJoystick.UpdatePositioning(); }
}
