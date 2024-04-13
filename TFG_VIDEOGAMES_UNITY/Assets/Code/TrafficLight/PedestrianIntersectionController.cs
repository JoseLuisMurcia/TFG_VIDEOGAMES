using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianIntersectionController : MonoBehaviour
{
    [SerializeField] List<GameObject> triggers;
    TrafficLightScheduler scheduler;
    void Start()
    {
        scheduler = GetComponent<TrafficLightScheduler>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                PedestrianTrafficLightTrigger trigger = child.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
                if (trigger != null)
                {
                    trigger.SetScheduler(scheduler);
                }
            }
        }
    }

}
