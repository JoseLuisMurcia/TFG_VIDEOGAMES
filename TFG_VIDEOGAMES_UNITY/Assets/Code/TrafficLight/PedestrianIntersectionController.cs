using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianIntersectionController : MonoBehaviour
{
    [SerializeField] List<GameObject> triggers;
    private PedestrianTrafficLightEvents trafficLightEvents;
    private TrafficLightScheduler trafficLightScheduler;
    void Start()
    {
        trafficLightEvents = GetComponent<PedestrianTrafficLightEvents>();
        trafficLightScheduler = GetComponent<TrafficLightScheduler>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                PedestrianTrafficLightTrigger trigger = child.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
                if (trigger != null)
                {
                    trigger.SetIntersectionController(this);
                }
            }
        }
    }

    public void ThrowLightChangeEvent(TrafficLightState state, bool subscription)
    {
        trafficLightEvents.LightChange(state, subscription);
    }

    public void SubscribeToLightChangeEvent(Action<TrafficLightState, bool> func)
    {
        trafficLightEvents.onLightChange += func;
    }

    public TrafficLightState GetState()
    {
        return trafficLightScheduler.GetState();
    }
}
