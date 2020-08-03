using HoloToolkit.Unity.Buttons;
//using System;
//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MyButton : Button
{
    public UnityEvent OnMyButtonClicked;

    private void Start()
    {
        OnButtonClicked += MyMethod;
    }

    private void MyMethod(GameObject obj)
    {
        
    }
}
