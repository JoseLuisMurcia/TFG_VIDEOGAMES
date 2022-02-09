using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CarSteeringAI AIController;
    public Waypoint currentWaypoint;
    private void Awake()
    {
        AIController = GetComponent<CarSteeringAI>();
    }
    void Start()
    {
        SetTargetToAI();
    }

    void Update()
    {
        if(AIController.GetTargetReached())
        {
            if (currentWaypoint.nextWaypoint == null)
                return;

            currentWaypoint = currentWaypoint.nextWaypoint;
            SetTargetToAI();
        }
    }

    private void SetTargetToAI()
    {
        Debug.Log("current Waypoint: " + currentWaypoint.gameObject.name);
        Debug.Log("next Waypoint: " + currentWaypoint.nextWaypoint.gameObject.name);
        if (currentWaypoint.nextWaypoint != null)
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), false);
        }
        else
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), true);
        }
    }
}
