using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointPathFollower : MonoBehaviour
{
    CarMovementAI AIController;
    [SerializeField] Transform target;
    float speed = 2;
    [SerializeField] Vector3[] path;
    int targetIndex;

    private void Start()
    {
        //PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    private void Update()
    {
        PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            path = newPath;
            //StopCoroutine("FollowPath");
            //StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            // if the object has arrived
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    targetIndex = 0;
                    path = new Vector3[0];
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }
            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed*Time.deltaTime);
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
            for(int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawCube(path[i] + new Vector3(0,0.2f,0), new Vector3(0.5f,0.5f,0.5f));

                if(i == targetIndex)
                {
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
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
