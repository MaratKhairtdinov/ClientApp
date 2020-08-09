using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    TouchScreenKeyboard keyboard;
    [SerializeField] public string inputString = string.Empty;
    [SerializeField] public NewTcpClient client;

    public void OnKeyboardCalled()
    {        
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);                 
    }

    
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
    }

}
