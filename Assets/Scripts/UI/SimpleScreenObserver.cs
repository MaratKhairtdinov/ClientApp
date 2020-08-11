using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleScreenObserver : MonoBehaviour
{
    Text text;
    private void OnValidate()
    {
        text = GetComponent<Text>();
    }
    private void Start()
    {
        text = GetComponent<Text>();
    }
    public void Display(string toDisplay)
    {
        text.text = toDisplay;
    }
}
