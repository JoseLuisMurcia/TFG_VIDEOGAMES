using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLight : TrafficLight
{
    [HideInInspector]
    public PedestrianColorChanger colorChanger;
    private void Awake()
    {
        currentColor = TrafficLightState.Red;
        colorChanger = GetComponentInChildren<PedestrianColorChanger>();
    }

    void Start()
    {
        colorChanger.SetColor(currentColor);
    }
}

