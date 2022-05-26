using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Pathfinder : MonoBehaviour
    {
        public static Pathfinder instance;
        private void Awake()
        {
            instance = this;
        }
        public void StartFindPath(Node startNode, Node targetNode)
        {
            FindPath(startNode, targetNode);
        }
        private List<Node> FindPath(Node startNode, Node targetNode)
        {
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

                foreach (Node neighbour in Grid.instance.GetNeighbours(currentNode))
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

            if (pathSuccess)
            {
                return RetracePath(startNode, targetNode);
            }
            return null;
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
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
            return nodes;
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
}

