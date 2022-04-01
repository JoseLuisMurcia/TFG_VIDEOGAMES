using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightEvents : MonoBehaviour
{

    public event Action<TrafficLightColor, bool> onLightChange;
    public void LightChange(TrafficLightColor newColor, bool subscription)
    {
        if(onLightChange != null)
        {
            onLightChange(newColor, subscription);
        }
    }
}
