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
        if (WorldGrid.Instance != null)
            colorChanger.SetColor(currentColor);
    }

    public void StartSetColor()
    {
        colorChanger.SetColor(currentColor);
    }
}

