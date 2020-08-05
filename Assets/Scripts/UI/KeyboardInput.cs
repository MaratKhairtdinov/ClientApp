using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    TouchScreenKeyboard keyboard;
    [SerializeField] public static string inputString = string.Empty;
    [SerializeField] public NewTcpClient client;

    public void OnKeyboardCalled()
    {
        if (client.host.Length==0)
        {
            keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);            
        }
        else
        {
            client.Connect();
        }
    }

    bool connected = false;
    private void Update()
    {
        if (TouchScreenKeyboard.visible == false && keyboard != null)
        {
            if (keyboard.done == true)
            {
                inputString = keyboard.text;
                keyboard = null;
            }
        }
        if (!connected && inputString.Length>2)
        {
            connected = true;
            client.host = inputString;
            client.Connect();
        }
    }

}
