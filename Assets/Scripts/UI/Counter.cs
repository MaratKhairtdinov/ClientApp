using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public TextMeshProUGUI textMP;
    public CounterTarget target;    
    public UIEventHandler handler;
    
    public void HandleButton(ButtonType buttonType)
    {
        float number = 0;
        if (target == CounterTarget.Resolution)
        {
            number = handler.spatialMappingObserver.TrianglesPerCubicMeter;
        }
        else if(target== CounterTarget.Selector)
        {
            number = handler.client.roomNumber;
        }

        switch (buttonType)
        {
            case ButtonType.Add100:
                number += 100;
                break;
            case ButtonType.Add10:
                number += 10;
                break;
            case ButtonType.Add1:
                number += 1;
                break;
            case ButtonType.Sub100:
                number -= 100;
                break;
            case ButtonType.Sub10:
                number -= 10;
                break;
            case ButtonType.Sub1:
                number -= 1;
                break;
        }
        if (target == CounterTarget.Resolution)
        {
            handler.spatialMappingObserver.TrianglesPerCubicMeter = number;
        }
        else if (target == CounterTarget.Selector)
        {
            handler.client.roomNumber = (int)number;
        }
    }
    private void Update()
    {
        if (target == CounterTarget.Resolution)
        {
            textMP.text = handler.spatialMappingObserver.TrianglesPerCubicMeter.ToString();
        }
        else if (target == CounterTarget.Selector)
        {
            textMP.text = handler.client.roomNumber.ToString();
        }
    }
}
public enum CounterTarget
{
    Resolution,
    Selector
}