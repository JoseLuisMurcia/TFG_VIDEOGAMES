using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
public class Pathfinding : MonoBehaviour
{
    PathfinderRequestManager requestManager;
    Grid grid;
    void Awake()
    {
        requestManager = GetComponent<PathfinderRequestManager>();
        grid = GetComponent<Grid>();
    }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    // I need to find the closest node from startPost to targetPos
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        startNode.gCost = 0;
        Node targetNode = grid.NodeFromWorldPoint(targetPos);


        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
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

                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {

                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    if (neighbour.isRoad)
                    {
                        bool compatibility = CanTravelV3(currentNode.typeOfRoad, neighbour.typeOfRoad);
                        if (compatibility == false) continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistanceHeuristic(currentNode, neighbour) + neighbour.movementPenalty;
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistanceHeuristic(neighbour, targetNode);
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
        }
        yield return null;
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);

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

        Vector3[] waypoints = SimplifyPath(path);
        //Vector3[] waypoints = new Vector3[path.Count]; 
        //for(int i= 0; i < path.Count; i++)
        //{
        //    waypoints[i] = path[i].worldPosition;
        //}
        Array.Reverse(waypoints);
        return waypoints;
    }

    Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;
        waypoints.Add(path[0].worldPosition);
        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }

    Vector3[] SimplifyPath(List<Node> path, Node startNode)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;
        waypoints.Add(path[0].worldPosition);
        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            directionOld = directionNew;

            if (i == path.Count - 1 && directionOld != new Vector2(path[i].gridX, path[i].gridY) - new Vector2(startNode.gridX, startNode.gridY))
                waypoints.Add(path[path.Count - 1].worldPosition);
        }
        return waypoints.ToArray();
    }

    int GetDistanceHeuristic(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    int GetAlternativeHeuristic(Node nodeA, Node nodeB)
    {
        int Distance_X = (int)Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int Distance_Y = (int)Mathf.Abs(nodeA.gridY - nodeB.gridY);
        return (int)Mathf.Sqrt(Distance_X * Distance_X + Distance_Y * Distance_Y);
    }

    bool CanTravelV3(TypeOfRoad srcType, TypeOfRoad dstType)
    {
        switch (srcType)
        {
            case TypeOfRoad.Right:
                if (dstType == TypeOfRoad.Right || dstType == TypeOfRoad.DownToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Up:
                if (dstType == TypeOfRoad.Up || dstType == TypeOfRoad.UpToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Left:
                if (dstType == TypeOfRoad.Left || dstType == TypeOfRoad.UpToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Down:
                if (dstType == TypeOfRoad.Down || dstType == TypeOfRoad.DownToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.DownToLeft:
                if (dstType == TypeOfRoad.Left || dstType == TypeOfRoad.DownToRight || dstType == TypeOfRoad.DownToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.DownToRight:
                if (dstType == TypeOfRoad.Down || dstType == TypeOfRoad.UpToRight || dstType == TypeOfRoad.DownToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.UpToLeft:
                if (dstType == TypeOfRoad.Up || dstType == TypeOfRoad.DownToLeft || dstType == TypeOfRoad.UpToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.UpToRight:
                if (dstType == TypeOfRoad.Right || dstType == TypeOfRoad.UpToLeft || dstType == TypeOfRoad.UpToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.None:
                return false;
        }
        return false;
    }
}
