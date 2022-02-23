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

        path = ProcessTrafficLightsInPath(path);
        // waypoints = SimplifyPath(path, startNode);
        Vector3[] waypoints = SimplifyPath(path);

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
}
