using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    public GameObject sliderNode;
    [SerializeField] private Slider slider;
    public bool sliderSeen = false;

    private void Awake()
    {
        slider = sliderNode.GetComponent<Slider>();
        slider.minValue = 0.0f; slider.maxValue = 1.0f;
    }

    public void DataLoadEvent(int fullLength, int chunksProcessed)
    {
        slider.normalizedValue = ((float)chunksProcessed) / ((float) fullLength);        
    }
}


