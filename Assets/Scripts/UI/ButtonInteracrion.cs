using HoloToolkit.Unity.Buttons;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteracrion : Button
{
    public UnityEvent OnButtonPressed;

    private void Awake()
    {
        OnButtonClicked += ButtonClicked;    
    }

    private void ButtonClicked(GameObject obj)
    {
        OnButtonPressed.Invoke();
    }
}
