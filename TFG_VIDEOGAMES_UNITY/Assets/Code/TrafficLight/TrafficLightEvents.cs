using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightEvents : MonoBehaviour
{

    public event Action<TrafficLightColor> onLightChange;
    public void LightChange(TrafficLightColor newColor)
    {
        if(onLightChange != null)
        {
            onLightChange(newColor);
        }
    }
}
