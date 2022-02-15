using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointPathFollower : MonoBehaviour
{
    CarMovementAI AIController;
    private Waypoint currentWaypoint;
    [SerializeField] Stack<Waypoint> path = new Stack<Waypoint>();

    private bool noMoreWaypoints = true;
    private void Awake()
    {
        AIController = GetComponent<CarMovementAI>();
    }

    // Called on pathfinding.cs start method
    public void SetPath(List<Waypoint> reversePath)
    {
        foreach (Waypoint point in reversePath)
            path.Push(point);

        currentWaypoint = path.Pop();
        SetTargetToAI();
        noMoreWaypoints = false;
    }

    void Update()
    {
        if (noMoreWaypoints) return;

        if (AIController.GetTargetReached())
        {
            currentWaypoint = path.Pop();
            if (path.Count == 0)
            {
                noMoreWaypoints = true;
            }
            SetTargetToAI();
        }
    }

    private void SetTargetToAI()
    {
        // If this is the last node, stop, all the waypoints have already been popped.
        if (path.Count > 0)
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), false);
        }
        else
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), true);
        }
    }
}
