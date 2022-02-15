using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Pathfinding : MonoBehaviour
{
    public Transform sourceObject, targetObject;
    private Waypoint previousTargetWaypoint;
    Graph graph;
    void Awake()
    {
        graph = GetComponent<Graph>();
    }

    private void Start()
    {
        //FindPath(sourceObject.position, targetObject.position);

    }
    private void Update()
    {
        FindPath(sourceObject.position, targetObject.position);
    }

    // I need to find the nearest waypoint from startPost to targetPos
    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Waypoint targetWaypoint = graph.FindClosestWaypointFromWorldPoint(targetPos);
        //if (targetWaypoint == previousTargetWaypoint) return;
        Waypoint startWaypoint = graph.FindClosestWaypointFromWorldPoint(startPos);

        Heap<Waypoint> openSet = new Heap<Waypoint>(graph.MaxSize);
        HashSet<Waypoint> closedSet = new HashSet<Waypoint>();
        openSet.Add(startWaypoint);

        while(openSet.Count > 0)
        {
            Waypoint currentWaypoint = openSet.RemoveFirst();  
            closedSet.Add(currentWaypoint);

            // Target waypoint found
            if (currentWaypoint == targetWaypoint)
            {
                RetracePath(startWaypoint, targetWaypoint);
                return;
            }
                

            foreach (Connection connection in currentWaypoint.connections)
            {
                Waypoint neighbour = connection.destWaypoint;
                if (closedSet.Contains(neighbour))
                    continue;

                float newMovementCostToNeighbour = currentWaypoint.gCost + GetDistanceHeuristic(currentWaypoint, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistanceHeuristic(neighbour, targetWaypoint);
                    neighbour.pathfindingParent = currentWaypoint;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }

        }

    }

    void RetracePath(Waypoint startWaypoint, Waypoint targetWaypoint)
    {
        List<Waypoint> path = new List<Waypoint>();
        Waypoint currentWaypoint = targetWaypoint;

        while (currentWaypoint != startWaypoint)
        {
            path.Add(currentWaypoint);
            
            currentWaypoint = currentWaypoint.pathfindingParent;
        }
        path.Add(currentWaypoint);
        //path.Reverse();
        //graph.path = path;
        graph.SetCarPath(path);
        previousTargetWaypoint = targetWaypoint;
    }

    float GetDistanceHeuristic(Waypoint source, Waypoint dest)
    {
        float dstX = Mathf.Abs(source.GetPosition().x - dest.GetPosition().z);
        float dstY = Mathf.Abs(source.GetPosition().z - dest.GetPosition().z);

        if (dstX > dstY)
            return 14f * dstY + 10f * (dstX - dstY);
        return 14f * dstX + 10f * (dstY - dstX);
    }
}
