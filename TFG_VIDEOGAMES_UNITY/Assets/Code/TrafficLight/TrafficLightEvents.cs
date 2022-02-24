using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightEvents : MonoBehaviour
{
    public static TrafficLightEvents instance;

    private void Awake()
    {
        instance = this;
    }

    public event Action onLightChange;
    public event Action onRoadTriggerEnter;
    public event Action onRoadTriggerExit;
    public void LightChange()
    {
        if(onLightChange != null)
        {
            onLightChange();
        }
    }

    public void RoadTriggerEnter()
    {
        if (onRoadTriggerEnter != null)
        {
            onRoadTriggerEnter();
        }
    }

    public void RoadTriggerExit()
    {
        if (onRoadTriggerExit != null)
        {
            onRoadTriggerExit();
        }
    }
}
