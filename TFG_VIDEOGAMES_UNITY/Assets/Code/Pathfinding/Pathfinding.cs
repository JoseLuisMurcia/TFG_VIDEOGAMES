using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
public class Pathfinding : MonoBehaviour
{
    PathfinderRequestManager requestManager;
    [SerializeField] private bool isProcedural = false;
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

        Node startNode = isProcedural ? RoadConnecter.Instance.FindStartNode(startPos, carForward) : WorldGrid.Instance.FindStartNode(startPos, carForward);
        Node targetNode = isProcedural ? RoadConnecter.Instance.FindStartNode(startPos, carForward) : WorldGrid.Instance.FindEndNode(targetPos);
        StartCoroutine(FindPath(startNode, targetNode));
    }
    public void StartFindPath(Vector3 startPos, Node targetNode, Vector3 carForward)
    {
        Node startNode = isProcedural ? RoadConnecter.Instance.FindStartNode(startPos, carForward) : WorldGrid.Instance.FindStartNode(startPos, carForward);
        StartCoroutine(FindPath(startNode, targetNode));
    }
    private IEnumerator FindPath(Node startNode, Node targetNode)
    {
        bool pathSuccess = false;
        startNode.gCost = 0;
        Heap<Node> openSet = isProcedural ? new Heap<Node>(RoadConnecter.Instance.MaxSize) : new Heap<Node>(WorldGrid.Instance.MaxSize);
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

                if (CheckLaneRestriction(currentNode, neighbour))
                    continue;

                float newMovementCostToNeighbour = currentNode.gCost + GetDistanceHeuristic(currentNode, neighbour);
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

        yield return null;
        PathfindingResult result = new PathfindingResult();

        if (pathSuccess)
        {
            result = RetracePath(startNode, targetNode);
            requestManager.FinishedProcessingPath(result, pathSuccess, startNode, result.endNode);
        }
        else
        {
            requestManager.FinishedProcessingPath(result, pathSuccess, startNode, targetNode);
        }
    }
    private bool CheckLaneRestriction(Node currentNode, Node neighbour)
    {
        if (currentNode.laneSide == LaneSide.Left && neighbour.laneSide == LaneSide.Right)
        {
            return true;
        }
        else if (currentNode.laneSide == LaneSide.Right && neighbour.laneSide == LaneSide.Left)
        {
            return true;
        }
        return false;
    }
    private PathfindingResult RetracePath(Node startNode, Node endNode)
    {
        List<Node> nodes = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            nodes.Add(currentNode);
            currentNode = currentNode.parent;
        }
        nodes.Add(startNode);
        nodes.Reverse();
        endNode = CorrectPathInRoundabout(nodes);
        List<Vector3> waypoints = ModifyPathLateralOffset(nodes);
        PathfindingResult result = new PathfindingResult(nodes, waypoints, endNode);
        return result;
    }
    public void StartLaneSwap(Node startNode)
    {
        StartCoroutine(SwapLane(startNode));
    }
    Node CorrectPathInRoundabout(List<Node> nodes)
    {
        int entryId = -1;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].road.typeOfRoad == TypeOfRoad.Roundabout)
            {
                Road roundabout = nodes[i].road;
                if (roundabout.entryNodes.Contains(nodes[i]))
                {
                    entryId = i;
                }
                else if (roundabout.exitNodes.Contains(nodes[i]))
                {
                    break;
                }
            }
        }
        if (entryId == -1)
            return nodes[nodes.Count - 1];

        List<Node> pathInRoundabout = GetPathInRoundabout(nodes[entryId]);
        nodes.RemoveRange(entryId, nodes.Count - entryId);
        nodes.AddRange(pathInRoundabout);
        return nodes[nodes.Count - 1];
    }
    private IEnumerator SwapLane(Node startNode)
    {
        //SpawnSphere(startNode.worldPosition, startNode.neighbours[0].worldPosition);
        int i = 0;
        while (i < 5)
        {
            if (startNode.neighbours.Count < 2)
            {
                startNode = startNode.neighbours[0];
            }
            else
            {
                i = 1000;
            }
        }

        if (startNode.neighbours.Count < 2)
        {
            Debug.LogWarning("start node has no neighbour 1");
            PathfindingResult result = new PathfindingResult();
            requestManager.FinishedProcessingPath(result, false, null, null);
        }
        else
        {
            Node realStartNode = startNode.neighbours[1]; // El nodo por el que queremos comenzar es el vecino de la otra linea //TO FIX ESTO SIGUE FALLANDO
            List<Node> nodes = new List<Node>();
            // Como se hasta donde tengo que estar devolviendo el camino? Cuando son suficientes nodos?
            // Devolver 75 neighbours[0] por los loles xd

            int numNodesToOvertake = 75;
            nodes.Add(realStartNode);


            for (int j = 0; j < numNodesToOvertake - 1; j++)
            {
                nodes.Add(nodes[j].neighbours[0]);
            }
            Node targetNode = nodes[numNodesToOvertake - 1].neighbours[0];
            yield return null;
            PathfindingResult result = ReturnLaneSwap(nodes);
            requestManager.FinishedProcessingPath(result, true, realStartNode, result.endNode);
        }
    }
    private PathfindingResult ReturnLaneSwap(List<Node> nodes)
    {
        Node endNode = CorrectPathInRoundabout(nodes);
        List<Vector3> waypoints = ModifyPathLateralOffset(nodes);
        PathfindingResult result = new PathfindingResult(nodes, waypoints, endNode);
        return result;
    }
    private List<Vector3> ModifyPathLateralOffset(List<Node> nodes)
    {
        float maxLeftOffset = .16f;
        float maxRightOffset = -.16f;

        int numNodesInPath = nodes.Count;
        const int nodesPerSegment = 50;
        int numSegments = numNodesInPath / nodesPerSegment;
        if (numSegments == 0)
            numSegments = 1;

        List<Vector3> waypoints = new List<Vector3>();
        waypoints.Add(nodes[0].worldPosition);
        int firstNodeInSegment;
        int lastNodeInSegment = -1;

        for (int i = 0; i < numSegments; i++)
        {
            int randomInt = UnityEngine.Random.Range(0, 2);
            // If to check if this is the last segment, in this case, merge all the points from the incomplete one in front,
            firstNodeInSegment = lastNodeInSegment + 1;
            if (i == numSegments - 1)
            {
                lastNodeInSegment = numNodesInPath - 2;
            }
            else
            {
                lastNodeInSegment = nodesPerSegment * (i + 1) - 1;
            }

            int halfPoint = (firstNodeInSegment + lastNodeInSegment) / 2;
            float offsetIncrement;

            if (randomInt == 0) // Go right
            {
                offsetIncrement = maxRightOffset / halfPoint;
            }
            else // Go left
            {
                offsetIncrement = maxLeftOffset / halfPoint;
            }

            float currentOffset = 0f;
            for (int j = firstNodeInSegment; j <= lastNodeInSegment; j++)
            {
                if (j < halfPoint)
                {
                    currentOffset += offsetIncrement;
                }
                else
                {
                    currentOffset -= offsetIncrement;
                }

                Vector3 dirToNextNode = (nodes[j + 1].worldPosition - nodes[j].worldPosition).normalized;
                Vector2 perpDir = Vector2.Perpendicular(new Vector2(dirToNextNode.x, dirToNextNode.z));
                Vector3 perpendicularDirection = new Vector3(perpDir.x, 0, perpDir.y).normalized;
                waypoints.Add(nodes[j + 1].worldPosition + perpendicularDirection * currentOffset);
            }
        }
        return waypoints;
    }
    float GetDistanceHeuristic(Node nodeA, Node nodeB)
    {
        float dstX = Mathf.Abs(nodeA.worldPosition.x - nodeB.worldPosition.x);
        float dstY = Mathf.Abs(nodeA.worldPosition.z - nodeB.worldPosition.z);

        if (dstX > dstY)
            return 14f * dstY + 10f * (dstX - dstY);
        return 14f * dstX + 10f * (dstY - dstX);
    }
    public List<Node> GetPathInRoundabout(Node entryNode)
    {
        List<Node> path = new List<Node>();
        // Get road
        Road originRoad = entryNode.previousNode.previousNode.road;
        Road roundabout = entryNode.road;
        bool useInnerLane;

        int numExits = roundabout.exitNodes.Count;
        int selectedExit = UnityEngine.Random.Range(0, numExits);
        Node exitNode = roundabout.exitNodes[selectedExit];
        Node newRoadNode = exitNode.neighbours[0].neighbours[0];
        Road newRoad = newRoadNode.road;

        if (originRoad.numberOfLanes > 1 && originRoad.numDirection == NumDirection.OneDirectional)
        {
            // Special case where you are forced to use one lane or the other
            if (entryNode.previousNode.previousNode.laneSide == LaneSide.Left)
            {
                // Use the inner lane
                useInnerLane = true;
            }
            else
            {
                // Use the outter lane
                useInnerLane = false;
            }
            // Aqui hay que hacer un check para que el exit node, en caso de ser perteneciente a una carretera de dos carriles
            // y unidireccional, sea el del carril interno y no el externo
            if (newRoad.numDirection == NumDirection.OneDirectional && newRoad.numberOfLanes > 1)
            {
                if (useInnerLane)
                {
                    exitNode = newRoad.entryNodes[0].previousNode.previousNode;
                }
                else
                {
                    exitNode = newRoad.entryNodes[1].previousNode.previousNode;
                }
            }
        }
        else
        {
            // Coming fron a twoDirectionalRoad or a OneDirectionalRoad with one lane
            // Use a lane matching the destiny. Only one entry in this side
            Vector3 dirToExitNode = (exitNode.worldPosition - entryNode.worldPosition).normalized;
            Vector3 forward = (entryNode.worldPosition - entryNode.previousNode.worldPosition).normalized;

            float angle = Vector3.SignedAngle(forward, dirToExitNode, Vector3.up);
            if (newRoad.numDirection == NumDirection.OneDirectional)
            {
                if (newRoad.numberOfLanes > 1)
                {
                    if (newRoadNode.laneSide == LaneSide.Left)
                    {
                        // We must use the inner lane
                        useInnerLane = true;
                    }
                    else
                    {
                        // We must use the outter lane
                        useInnerLane = false;
                    }
                }
                else
                {
                    // Use the lane depending on the angle to it, if it is in front or
                    // We can go right, forward or left
                    if (angle > -5f && angle < 5f)
                    {
                        // Going forward, use the outter lane or the inner
                        int choice = UnityEngine.Random.Range(0, 2);
                        if (choice == 0)
                        {
                            useInnerLane = true;
                        }
                        else
                        {
                            useInnerLane = false;
                        }
                    }
                    else if (angle > 10f)
                    {
                        // Going right, use the outter
                        useInnerLane = false;
                    }
                    else // Negative angle
                    {
                        // Going left, use the inner
                        useInnerLane = true;
                    }
                }
            }
            else // The new road is two directional
            {
                if (angle > -5f && angle < 5f)
                {
                    // Going forward, use the outter lane or the inner
                    useInnerLane = false;
                }
                else if (angle > 10f)
                {
                    // Going right, use the outter
                    useInnerLane = false;
                }
                else if (angle < -5f && angle > -60f) // Negative angle
                {
                    // Going left, use the inner
                    useInnerLane = true;
                }
                else // Direction change -- Angle = -90f
                {
                    useInnerLane = true;
                }
            }

        }

        path.Add(entryNode);

        Node neighbor1 = entryNode.neighbours[0];
        Node neighbor2 = entryNode.neighbours[1];
        Node firstLaneNode;
        Node lastLaneNode = null;
        if (useInnerLane)
        {
            if (neighbor1.laneSide == LaneSide.Left)
            {
                firstLaneNode = neighbor1;
            }
            else
            {
                firstLaneNode = neighbor2;
            }

        }
        else
        {
            if (neighbor1.laneSide == LaneSide.Right)
            {
                firstLaneNode = neighbor1;
            }
            else
            {
                firstLaneNode = neighbor2;
            }
        }

        Node currentNode = firstLaneNode;
        while (lastLaneNode == null)
        {
            foreach (Node neighbor in currentNode.neighbours)
            {
                if (neighbor == exitNode)
                {
                    lastLaneNode = currentNode;
                }
            }
            path.Add(currentNode);
            currentNode = currentNode.neighbours[0];
        }
        path.Add(exitNode);
        int nodesToAddAfterExit = 10;
        Node currentNeighbour = exitNode.neighbours[0];
        for (int i = 0; i < nodesToAddAfterExit; i++)
        {
            path.Add(currentNeighbour);
            currentNeighbour = currentNeighbour.neighbours[0];
        }
        return path;
    }

}

public struct PathfindingResult
{
    public List<Node> nodes;
    public List<Vector3> pathPositions;
    public Node endNode;
    public PathfindingResult(List<Node> _nodes, List<Vector3> _pathPositions, Node _endNode)
    {
        nodes = _nodes;
        pathPositions = _pathPositions;
        endNode = _endNode;
    }
}