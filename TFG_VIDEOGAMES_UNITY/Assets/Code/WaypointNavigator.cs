using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CarNavigationController controller;
    public Waypoint currentWaypoint;
    private void Awake()
    {
        controller = GetComponent<CarNavigationController>();
    }
    void Start()
    {
        controller.SetDestination(currentWaypoint.GetPosition());
    }

    void Update()
    {
        if(controller.reachedDestination)
        {
            currentWaypoint = currentWaypoint.nextWaypoint;
            controller.SetDestination(currentWaypoint.GetPosition());
        }
    }
}
