using HoloToolkit.Unity.Buttons;
using System;
using UnityEngine;
using UnityEngine.Events;

public enum ButtonType
{
    Connect = 0,
    SendMatrix = 1,
    Refine = 2,
    Register = 3,
    SetHost = 4,
    Add100=5,
    Add10=6,
    Add1=7,
    Sub100=8,
    Sub10=9,
    Sub1=10,
    SelectRoom = 11
}

public class ButtonInteracrion : Button
{
    public ButtonType ButtonType;

    public ButtonEvent OnButtonPressed;


    private void Awake()
    {
        OnButtonClicked += ButtonClicked;    
    }

    private void ButtonClicked(GameObject obj)
    {
        OnButtonPressed.Invoke(ButtonType);
    }
}

[Serializable]
public class ButtonEvent : UnityEvent<ButtonType> { }
