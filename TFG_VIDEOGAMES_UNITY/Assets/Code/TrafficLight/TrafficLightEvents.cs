using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightEvents : MonoBehaviour
{

    public event Action<TrafficLightState, bool> onLightChange;
    public void LightChange(TrafficLightState newColor, bool subscription)
    {
        if(onLightChange != null)
        {
            onLightChange(newColor, subscription);
        }
    }
}
