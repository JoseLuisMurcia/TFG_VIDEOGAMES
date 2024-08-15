using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightEvents : MonoBehaviour
{

    public event Action<TrafficLightState, bool, float> onLightChange;
    public void LightChange(TrafficLightState newColor, bool subscription, float lightChangeTime)
    {
        if(onLightChange != null)
        {
            onLightChange(newColor, subscription, lightChangeTime);
        }
    }
}
