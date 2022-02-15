using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Pathfinding : MonoBehaviour
{
    PathfinderRequestManager requestManager;
    public Transform sourceObject, targetObject;
    Graph graph;
    void Awake()
    {
        requestManager = GetComponent<PathfinderRequestManager>();
        graph = GetComponent<Graph>();
    }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    // I need to find the closest waypoint from startPost to targetPos
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Waypoint targetWaypoint = graph.FindClosestWaypointFromWorldPoint(targetPos);
        Waypoint startWaypoint = graph.FindClosestWaypointFromWorldPoint(startPos);

        //if (!startWaypoint.walkable || !targetWaypoint.walkable) return;

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
                pathSuccess = true;
                break;
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
        yield return null;
        if (pathSuccess)
        {
            waypoints = RetracePath(startWaypoint, targetWaypoint);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);
    }

    Vector3[] RetracePath(Waypoint startWaypoint, Waypoint targetWaypoint)
    {
        List<Waypoint> path = new List<Waypoint>();
        Waypoint currentWaypoint = targetWaypoint;

        while (currentWaypoint != startWaypoint)
        {
            path.Add(currentWaypoint);
            
            currentWaypoint = currentWaypoint.pathfindingParent;
        }
        path.Add(currentWaypoint);
        Vector3[] waypoints = SimplifyPath(path);
        //path.Reverse();
        return waypoints;
        //graph.path = path;
        //graph.SetCarPath(path);
    }

    Vector3[] SimplifyPath(List<Waypoint> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for(int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].GetPosition().x - path[i].GetPosition().x, path[i - 1].GetPosition().y - path[i].GetPosition().y);
            if(directionNew != directionOld)
            {
                waypoints.Add(path[i].GetPosition());
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray();
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
