using HoloToolkit.UI.Keyboard;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIEventHandler : MonoBehaviour
{
    //public TCPClient newClient;
    public TCPClient client;
    public VertexCollector collector;
    public SpatialMappingObserver spatialMappingObserver;
    public Counter counter;

    public KeyboardInput keyboardInput;

    public void HandleButton(ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonType.Connect:
                client.Connect();                
                break;
            case ButtonType.SendHello:
                client.SendString("Pizza pepperroncino is ready, stronzi");
                break;
            case ButtonType.CollectVertices:
                collector.Collect();
                break;
            case ButtonType.SendPointcloud:
                break;
            case ButtonType.SetHost:
                keyboardInput.OnKeyboardCalled();
                if (keyboardInput.inputString.Length>0)
                {
                    client.host = keyboardInput.inputString;
                    client.OnHostSet.Invoke(client.host);
                }
                break;
        }
        counter.HandleButton(buttonType);
    }
}
[Serializable]
public class UIEvent : UnityEvent<string> { }
