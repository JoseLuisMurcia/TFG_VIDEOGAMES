using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightTrigger : MonoBehaviour
{
    private TrafficLightScheduler scheduler;

    public TrafficLightScheduler GetScheduler()
    {
        return scheduler;
    }


    public void SetScheduler(TrafficLightScheduler _scheduler)
    {
        scheduler = _scheduler;
    }

}
