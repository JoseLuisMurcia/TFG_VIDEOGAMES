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
        Node startNode = WorldGrid.Instance.FindStartNode(startPos, carForward);
        Node targetNode = WorldGrid.Instance.FindEndNode(targetPos);
        StartCoroutine(FindPath(startNode, targetNode));
    }

    public void StartFindPath(Vector3 startPos, Node targetNode, Vector3 carForward)
    {
        Node startNode = WorldGrid.Instance.FindStartNode(startPos, carForward);
        StartCoroutine(FindPath(startNode, targetNode));
    }

    IEnumerator FindPath(Node startNode, Node targetNode)
    {
        PathfindingResult result = new PathfindingResult();
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
        if (pathSuccess)
        {
            result = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(result, pathSuccess, startNode, targetNode);
    }

    PathfindingResult RetracePath(Node startNode, Node endNode)
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
        List<Vector3> waypoints = ModifyPathLateralOffset(nodes);
        PathfindingResult result = new PathfindingResult(nodes, waypoints);
        return result;
    }

    private List<Vector3> ModifyPathLateralOffset(List<Node> nodes)
    {
        float maxLeftOffset = .5f;
        float maxRightOffset = -.5f;

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
                lastNodeInSegment = nodesPerSegment * (i+1) - 1;
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


}

public enum PathModificator
{
    LeftSwing,
    RightSwing,
    LeftStraight,
    RightStraight
}

public struct PathfindingResult
{
    public List<Node> nodes;
    public List<Vector3> pathPositions;

    public PathfindingResult(List<Node> _nodes, List<Vector3> _pathPositions)
    {
        nodes = _nodes;
        pathPositions = _pathPositions;
    }
}