using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightEvents : MonoBehaviour
{
    public event Action<TrafficLightState, bool> onLightChange;
    public void LightChange(TrafficLightState newColor, bool subscription)
    {
        if (onLightChange != null)
        {
            onLightChange(newColor, subscription);
        }
    }
}
