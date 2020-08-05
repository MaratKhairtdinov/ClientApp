using UnityEngine;
using UnityEngine.UI;
using System;

public class TextSetter : MonoBehaviour
{
    Text text;
    private void OnValidate()
    {
        text = GetComponent<Text>();
    }
    private void Awake()
    {
        text = GetComponent<Text>();
        text.text = "";
    }

    public void SetText(string text)
    {
        this.text.text = (text + Environment.NewLine);
    }
}