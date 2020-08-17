using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public TextMeshProUGUI textMP;
    public SpatialMappingObserver observer;

    public void HandleButton(ButtonType buttonType)
    {
        var currentResolution = observer.TrianglesPerCubicMeter;
        switch (buttonType)
        {
            case ButtonType.Add100:
                currentResolution += 100;
                break;
            case ButtonType.Add10:
                currentResolution += 10;
                break;
            case ButtonType.Add1:
                currentResolution += 1;
                break;
            case ButtonType.Sub100:
                currentResolution -= 100;
                break;
            case ButtonType.Sub10:
                currentResolution -= 10;
                break;
            case ButtonType.Sub1:
                currentResolution -= 1;
                break;
        }
        observer.TrianglesPerCubicMeter = currentResolution;
    }
    private void Update()
    {
        textMP.text = observer.TrianglesPerCubicMeter.ToString();
    }
}
