using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public Transform sourceObject, targetobject;
    Graph graph;
    void Awake()
    {
        graph = GetComponent<Graph>();
    }

    private void Update()
    {
        FindPath(sourceObject.position, targetobject.position);
    }

    // I need to find the nearest waypoint from startPost to targetPos
    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Waypoint startWaypoint = graph.FindClosestWaypointFromWorldPoint(startPos);
        Waypoint targetWaypoint = graph.FindClosestWaypointFromWorldPoint(targetPos);

        List<Waypoint> openSet = new List<Waypoint>();
        HashSet<Waypoint> closedSet = new HashSet<Waypoint>();
        openSet.Add(startWaypoint);

        while(openSet.Count > 0)
        {
            Waypoint currentWaypoint = openSet[0];
            for (int i=1; i<openSet.Count; i++)
            {
                if(openSet[i].fCost < currentWaypoint.fCost || openSet[i].fCost == currentWaypoint.fCost && openSet[i].hCost < currentWaypoint.hCost)
                {
                    currentWaypoint = openSet[i];
                }
            }

            openSet.Remove(currentWaypoint);
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
        path.Reverse();

        graph.path = path;

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
