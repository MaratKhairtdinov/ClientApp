using HoloToolkit.UI.Keyboard;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIEventHandler : MonoBehaviour
{
    public TCPClient client;
    public VertexCollector collector;
    public SpatialMappingObserver spatialMappingObserver;    

    public KeyboardInput keyboardInput;

    public void HandleButton(ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonType.Connect:
                client.Connect();                
                break;
            case ButtonType.SendMatrix:
                client.SendMatrix();
                break;
            case ButtonType.Refine:
                client.SendPointCloud(NetworkCommand.RefineRegistration);
                break;
            case ButtonType.Register:
                client.SendPointCloud(NetworkCommand.GlobalRegistration);
                break;
            case ButtonType.SetHost:
                keyboardInput.OnKeyboardCalled();
                if (keyboardInput.inputString.Length>0)
                {
                    client.host = keyboardInput.inputString;
                    client.OnHostSet.Invoke(client.host);
                }
                break;
            case ButtonType.SelectRoom:
                client.SelectRoom();
                break;
        }
        //resolution.HandleButton(buttonType);
        //roomSelector.HandleButton(buttonType);
    }
}
[Serializable]
public class UIEvent : UnityEvent<string> { }
