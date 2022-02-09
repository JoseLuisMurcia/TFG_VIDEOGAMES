using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CarMovementAI AIController;
    public Waypoint currentWaypoint;
    private bool noMoreWaypoints = false;
    private void Awake()
    {
        AIController = GetComponent<CarMovementAI>();
    }
    void Start()
    {
        SetTargetToAI();
    }

    void Update()
    {
        if (noMoreWaypoints) return;

        if(AIController.GetTargetReached())
        {
            currentWaypoint = currentWaypoint.nextWaypoint;
            if (currentWaypoint == null)
            {
                noMoreWaypoints = true;
                return;
            }
                
            SetTargetToAI();
        }
    }

    private void SetTargetToAI()
    {
        //Debug.Log("current Waypoint: " + currentWaypoint.gameObject.name);
        //Debug.Log("next Waypoint: " + currentWaypoint.nextWaypoint.gameObject.name);
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
