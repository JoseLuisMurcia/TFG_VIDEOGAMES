using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointPathFollower : MonoBehaviour
{
    CarMovementAI AIController;
    [SerializeField] Transform target;
    [SerializeField] float speed = 10;
    [SerializeField] float turnDst = 2;
    Path path;

    private void Start()
    {
        //PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    private void Update()
    {
        PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            //path = new Path(waypoints, transform.position, turnDst);
            //StopCoroutine("FollowPath");
            //StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath()
    {
        while (true)
        {
            yield return null;

        }
    }
    private void Awake()
    {
        AIController = GetComponent<CarMovementAI>();
    }

    public void OnDrawGizmos()
    {
        if(path != null)
        {
            path.DrawWithGizmos();
        }
    }

    // Called on pathfinding.cs start method
    //    public void SetPath(List<Waypoint> reversePath)
    //    {
    //        foreach (Waypoint point in reversePath)
    //            path.Push(point);

    //        currentWaypoint = path.Pop();
    //        SetTargetToAI();
    //        noMoreWaypoints = false;
    //    }

    //    void Update()
    //    {
    //        if (noMoreWaypoints) return;

    //        if (AIController.GetTargetReached())
    //        {
    //            currentWaypoint = path.Pop();
    //            if (path.Count == 0)
    //            {
    //                noMoreWaypoints = true;
    //            }
    //            SetTargetToAI();
    //        }
    //    }

    //    private void SetTargetToAI()
    //    {
    //        // If this is the last node, stop, all the waypoints have already been popped.
    //        if (path.Count > 0)
    //        {
    //            AIController.SetTargetPosition(currentWaypoint.GetPosition(), false);
    //        }
    //        else
    //        {
    //            AIController.SetTargetPosition(currentWaypoint.GetPosition(), true);
    //        }
    //    }
}
