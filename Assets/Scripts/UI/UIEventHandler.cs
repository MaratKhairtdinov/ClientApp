using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEventHandler : MonoBehaviour
{
    public NewTcpClient client;
    public VertexCollector collector;
    public SpatialMappingObserver spatialMappingObserver;
    public Counter counter;

    public KeyboardInput keyboardInput;

    public void HandleEvent(ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonType.Connect:
                client.Connect();
                break;
            case ButtonType.SendHello:
                client.SendMessage();
                break;
            case ButtonType.CollectVertices:
                collector.Collect();
                break;
            case ButtonType.SendPointcloud:
                client.SendPCD();
                break;
            case ButtonType.SetHost:
                keyboardInput.OnKeyboardCalled();
                if (keyboardInput.inputString.Length>0) 
                {
                    client.host = keyboardInput.inputString;
                }
                break;
        }
        counter.HandleEvent(buttonType);
    }
}
