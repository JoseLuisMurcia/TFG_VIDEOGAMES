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
                InvisiblePedestrianTrafficLightTrigger invisiblePedestrianTrigger = child.gameObject.GetComponent<InvisiblePedestrianTrafficLightTrigger>();
                PedestrianTrafficLightTrigger pedestrianTrigger = child.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
                if (invisiblePedestrianTrigger != null)
                {
                    invisiblePedestrianTrigger.SetIntersectionController(this);
                }
                else if (pedestrianTrigger != null)
                {
                    pedestrianTrigger.SetIntersectionController(this);
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
    public void UnsubscribeToLightChangeEvent(Action<TrafficLightState, bool> func)
    {
        trafficLightEvents.onLightChange -= func;
    }
    public float GetPedestrianTurnTimeLeft()
    {
        return trafficLightScheduler.GetPedestrianTurnTimeLeft();
    }
    public bool IsPedestrianState()
    {
        TrafficLightState state = trafficLightScheduler.GetState();
        return state == TrafficLightState.Pedestrian || state == TrafficLightState.PedestrianRush;
    }
    public TrafficLightState GetState()
    {
        return trafficLightScheduler.GetState();
    }
}
