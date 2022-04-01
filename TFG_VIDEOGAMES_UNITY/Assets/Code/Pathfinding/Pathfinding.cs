using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
public class Pathfinding : MonoBehaviour
{
    PathfinderRequestManager requestManager;
    void Awake()
    {
        requestManager = GetComponent<PathfinderRequestManager>();
    }

    public void StartFindPath(Node startNode, Node targetNode)
    {
        StartCoroutine(FindPath(startNode, targetNode));
    }
    public void StartFindPath(Vector3 startPos, Vector3 targetPos, Vector3 carForward)
    {
        StartCoroutine(FindPath(startPos, targetPos, carForward));
    }

    IEnumerator FindPath(Node startNode, Node targetNode)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        startNode.gCost = 0;
        Heap<Node> openSet = new Heap<Node>(WorldGrid.Instance.MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
            if (currentNode == targetNode)
            {
                pathSuccess = true;
                break;
            }

            foreach (Node neighbour in currentNode.neighbours)
            {

                if (closedSet.Contains(neighbour))
                {
                    continue;
                }

                float newMovementCostToNeighbour = currentNode.gCost + GetAlternativeHeuristic(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetAlternativeHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        yield return null;
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess, startNode, targetNode);

    }
    // I need to find the closest node from startPost to targetPos
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, Vector3 carForward)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = WorldGrid.Instance.FindStartNode(startPos, carForward);
        startNode.gCost = 0;
        Node targetNode = WorldGrid.Instance.FindEndNode(targetPos);


        Heap<Node> openSet = new Heap<Node>(WorldGrid.Instance.MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
            if (currentNode == targetNode)
            {
                pathSuccess = true;
                break;
            }

            foreach (Node neighbour in currentNode.neighbours)
            {

                if (closedSet.Contains(neighbour))
                {
                    continue;
                }

                float newMovementCostToNeighbour = currentNode.gCost + GetAlternativeHeuristic(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetAlternativeHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        yield return null;
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess, startNode, targetNode);

    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);

        //Vector3[] waypoints = SimplifyPath(path);
        Vector3[] waypoints = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            waypoints[i] = path[i].worldPosition;
        }
        Array.Reverse(waypoints);
        return waypoints;
    }

    //Vector3[] SimplifyPath(List<Node> path)
    //{
    //    List<Vector3> waypoints = new List<Vector3>();
    //    Vector2 directionOld = Vector2.zero;
    //    waypoints.Add(path[0].worldPosition);
    //    for (int i = 1; i < path.Count; i++)
    //    {
    //        Vector2 directionNew = new Vector2(path[i - 1].worldPosition.x - path[i].worldPosition.x, path[i - 1].worldPosition.z - path[i].worldPosition.z);
    //        if (directionNew != directionOld)
    //        {
    //            waypoints.Add(path[i].worldPosition);
    //        }
    //        directionOld = directionNew;
    //    }
    //    return waypoints.ToArray();
    //}

    float GetDistanceHeuristic(Node nodeA, Node nodeB)
    {
        if(nodeA == null || nodeB == null)
        {
            Debug.LogError("QUE SE ROMPE");
        }
        float dstX = Mathf.Abs(nodeA.worldPosition.x - nodeB.worldPosition.x);
        float dstY = Mathf.Abs(nodeA.worldPosition.z - nodeB.worldPosition.z);

        if (dstX > dstY)
            return 14f * dstY + 10f * (dstX - dstY);
        return 14f * dstX + 10f * (dstY - dstX);
    }

    int GetAlternativeHeuristic(Node nodeA, Node nodeB)
    {
        int Distance_X = (int)Mathf.Abs(nodeA.worldPosition.x - nodeB.worldPosition.x);
        int Distance_Y = (int)Mathf.Abs(nodeA.worldPosition.z - nodeB.worldPosition.z);
        return (int)Mathf.Sqrt(Distance_X * Distance_X + Distance_Y * Distance_Y);
    }

}
