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
        //Debug.Log("startNode x: " + startNode.gridX + ", y: " + startNode.gridY);
        //Debug.Log("targetNode x: " + targetNode.gridX + ", y: " + targetNode.gridY);

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                Debug.Log("Current node extracted from openSet: [" + currentNode.gridX + ", " + currentNode.gridY + "]");
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                // aqui el flujo se rompe, investigar dspues de que el 3,0 se mete en la open list
                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {
                    //if(currentNode.gridX == 3 && currentNode.gridY == 0)
                    //{
                    //    Debug.Log("Node[3,0] neighbour: [" +  + neighbour.gridX + "," + neighbour.gridY + "]");
                    //    if (!neighbour.walkable)
                    //    {
                    //        Debug.Log("Node: [" + neighbour.gridX + ", " + neighbour.gridY + "] not walkable");
                    //    }
                    //    if (closedSet.Contains(neighbour))
                    //    {
                    //        Debug.Log("Node: [" + neighbour.gridX + ", " + neighbour.gridY + "] contained in closed set");
                    //    }
                    //}
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    if (neighbour.isRoad)
                    {

                        if (currentNode.gridX == 3 && currentNode.gridY == 0)
                        {
                            //Debug.Log("Node[3,0] neighbour: [" + +neighbour.gridX + "," + neighbour.gridY + "]. " + "node[3,0] type: " + currentNode.typeOfRoad + ", neighbour type: " + neighbour.typeOfRoad);
                        }
                        bool compatibility = CanTravelV3(currentNode.typeOfRoad, neighbour.typeOfRoad, currentNode.worldPosition, neighbour.worldPosition, currentNode.gridX, currentNode.gridY, neighbour.gridX, neighbour.gridY);
                        if (compatibility == false) continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistanceHeuristic(currentNode, neighbour)/* + neighbour.movementPenalty*/;
                    //Debug.Log("node: " + currentNode.gridX + "," + currentNode.gridY + " . Exploring neighbour: " + neighbour.gridX + "," + neighbour.gridY +
                    //    ". currentNode new G Cost: " + newMovementCostToNeighbour + ", Neighbour G Cost: " + neighbour.gCost + ", currentNode G cost: " + currentNode.gCost 
                    //    + ", distanceHeuristic: " + GetDistanceHeuristic(currentNode, neighbour));

                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistanceHeuristic(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            //Debug.Log("neighbour: [" + neighbour.gridX + "," + neighbour.gridY + "] added to the open list");
                            openSet.Add(neighbour);
                        }
                        else
                        {
                            //Debug.Log("neighbour: [" + neighbour.gridX + "," + neighbour.gridY + "] updated in the open list");

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

        //path = ProcessTrafficLightsInPath(path);
        //Vector3[] waypoints = SimplifyPath(path);
        Vector3[] waypoints = new Vector3[path.Count]; 
        for(int i= 0; i < path.Count; i++)
        {
            waypoints[i] = path[i].worldPosition;
        }
        Array.Reverse(waypoints);
        return waypoints;
    }

    // Lo he dejado aquí, tratando de encontrar los mejores puntos de stop en caso de que el semaforo se ponga rojo/ambar.
    // Pero he descubierto un problema al darme cuenta de que semáforos en una orientación incorrecta tambien se estaban incluyendo,
    // lo cual me hace plantearme como puedo representar el espacio de forma que se distinga el sentido de unión de nodos en lugar de tener un grafo sin sentidos.
    List<Node> ProcessTrafficLightsInPath(List<Node> path)
    {
        List<TrafficLight> trafficLights = new List<TrafficLight>();
        // Find all the trafficLights in the path
        foreach (Node node in path)
        {
            if(node.hasTrafficLightClose)
            {
                if (!trafficLights.Contains(node.trafficLight))
                {
                    trafficLights.Add(node.trafficLight);
                }
            }
        }
        List<Node> nodesToStop = new List<Node>();
        // For each trafficLight in path, find all the nodes suitable to stop and select the best one
        foreach (TrafficLight trafficLight in trafficLights)
        {
            Node bestNode = trafficLight.nodesToStop[0];
            Vector3 trafficLightPosition = trafficLight.transform.position;
            float bestDistance = Vector3.Distance(bestNode.worldPosition, trafficLightPosition);
            for(int i=1; i < trafficLight.nodesToStop.Count; i++)
            {
                Node currentNode = trafficLight.nodesToStop[i];
                float currentNodeToTLDist = Vector3.Distance(currentNode.worldPosition, trafficLightPosition);
                if (currentNodeToTLDist < bestDistance)
                {
                    bestNode = currentNode;
                    bestDistance = currentNodeToTLDist;
                }
            }
            nodesToStop.Add(bestNode);
        }

        return path;
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

    bool CanTravelV2(TypeOfRoad srcType, TypeOfRoad dstType)
    {
        switch (srcType)
        {
            case TypeOfRoad.Right:
                if (dstType == TypeOfRoad.Right || dstType == TypeOfRoad.DownToRight || dstType == TypeOfRoad.UpToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Up:
                if (dstType == TypeOfRoad.Up || dstType == TypeOfRoad.UpToRight || dstType == TypeOfRoad.UpToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Left:
                if (dstType == TypeOfRoad.Left || dstType == TypeOfRoad.UpToLeft || dstType == TypeOfRoad.DownToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.Down:
                if (dstType == TypeOfRoad.Down || dstType == TypeOfRoad.DownToRight || dstType == TypeOfRoad.DownToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.DownToLeft:
                if (dstType == TypeOfRoad.Down || dstType == TypeOfRoad.Left || dstType == TypeOfRoad.DownToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.DownToRight:
                if (dstType == TypeOfRoad.Down || dstType == TypeOfRoad.Right || dstType == TypeOfRoad.UpToRight)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.UpToLeft:
                if (dstType == TypeOfRoad.Up || dstType == TypeOfRoad.Left || dstType == TypeOfRoad.DownToLeft)
                {
                    return true;
                }
                else return false;

            case TypeOfRoad.UpToRight:
                if (dstType == TypeOfRoad.Up || dstType == TypeOfRoad.Right || dstType == TypeOfRoad.UpToLeft)
                {
                    return true;
                }
                else return false;
        }
        return false;
    }
    bool CanTravelV3(TypeOfRoad srcType, TypeOfRoad dstType, Vector3 srcPos, Vector3 dstPos, int srcX, int srcY, int dstX, int dstY)
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
    bool CanTravel(Direction src, Vector3 srcPos, Direction dst, Vector3 dstPos)
    {
        switch (src)
        {
            case Direction.Right:
                if(dst == Direction.Right)
                {
                    return true;
                }
                else if(dst == Direction.Left)
                {
                    return false;
                }
                else if(dst == Direction.Up)
                {
                    // We want to go up but it is lower, trap
                    if(srcPos.z > dstPos.z)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if(dst == Direction.Down)
                {
                    // We want to go down but it is higher, trap
                    if (srcPos.z < dstPos.z)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                break;

            case Direction.Left:
                if (dst == Direction.Left)
                {
                    return true;
                }
                else if (dst == Direction.Right)
                {
                    return false;
                }
                else if (dst == Direction.Up)
                {
                    // We want to go up but it is lower, trap
                    if (srcPos.z > dstPos.z)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (dst == Direction.Down)
                {
                    // We want to go down but it is lower, trap
                    if (srcPos.z < dstPos.z)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                break;

            case Direction.Up:
                if (dst == Direction.Up)
                {
                    return true;
                }
                else if (dst == Direction.Down)
                {
                    return false;
                }
                else if (dst == Direction.Right)
                {
                    // We want to go right but it is lefter, trap
                    if (srcPos.x > dstPos.x)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (dst == Direction.Left)
                {
                    // We want to go left but it is righter, trap
                    if (srcPos.x < dstPos.x)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                break;

            case Direction.Down:
                if (dst == Direction.Down)
                {
                    return true;
                }
                else if (dst == Direction.Up)
                {
                    return false;
                }
                else if (dst == Direction.Right)
                {
                    // We want to go right but it is lefter, trap
                    if (srcPos.x > dstPos.x)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (dst == Direction.Left)
                {
                    // We want to go right but it is lefter, trap
                    if (srcPos.x > dstPos.x)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                break;


        }

        return false;
    }
}
