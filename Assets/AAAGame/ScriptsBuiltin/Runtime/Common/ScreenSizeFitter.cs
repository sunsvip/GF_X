using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScreenFitMode
{
    Width,
    Height
}
public class ScreenSizeFitter : MonoBehaviour
{
    public ScreenFitMode UIFitMode { get; private set; } = ScreenFitMode.Width;

    public int designWidth = 750;
    public int designHeight = 1334;
    public float Ratio
    {
        get { return designWidth / (float)designHeight; }
    }

    public void SetFilterMode(ScreenFitMode fitMode)
    {
        float aspectRatio = Screen.width / (float)Screen.height;
        float orthographicSize = 0;
        switch (fitMode)
        {
            case ScreenFitMode.Width:
                orthographicSize = designWidth / (2f * aspectRatio) * 0.01f;
                break;
            case ScreenFitMode.Height:
                orthographicSize = designHeight / 2f * 0.01f;
                break;
        }
        UIFitMode = fitMode;
        this.GetComponent<Camera>().orthographicSize = orthographicSize;
    }
}
