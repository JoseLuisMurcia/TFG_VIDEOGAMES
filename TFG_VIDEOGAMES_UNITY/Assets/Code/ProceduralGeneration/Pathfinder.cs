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
        public List<Node> FindPath(Node startNode, Node targetNode)
        {
            bool pathSuccess = false;
            startNode.gCost = 0;
            Heap<Node> openSet = new Heap<Node>(Grid.instance.MaxSize);
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

                foreach (Node neighbour in Grid.instance.GetNeighboursInLine(currentNode))
                {
                    if (MovementInvalid(currentNode, neighbour, targetNode))
                        continue;

                    if (closedSet.Contains(neighbour) || !EnoughSpace(currentNode, neighbour))
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
        private bool MovementInvalid(Node currentNode, Node neighbour, Node targetNode)
        {
            int x = currentNode.gridX; int y = currentNode.gridY;
            int newX = neighbour.gridX; int newY = neighbour.gridY;
            if (x != newX && y != newY)
                return true;

            if (neighbour != targetNode && (neighbour.usage == Usage.road || neighbour.usage == Usage.point))
                return true;

            return false;
        }
        private bool EnoughSpace(Node currentNode, Node neighbour)
        {
            int startX = currentNode.gridX; int startY = currentNode.gridY;
            int endX = neighbour.gridX; int endY = neighbour.gridY;
            int dirX = 0, dirY = 0;

            if (startX - endX == 0) // Vertical Movement
            {
                if (endY > startY) // Subir
                {
                    dirY = 1;
                }
                else
                {

                    dirY = -1;
                }

            }
            else // HorizontalMovement
            {
                if (endX > startX) // Hacia derecha
                {
                    dirX = 1;
                }
                else
                {
                    dirX = -1;

                }
            }

            int[] neighbourIncrement = Visualizer.instance.GetLateralIncrementOnDirection(dirX, dirY);
            return Visualizer.instance.EnoughSpace(endX, endY, neighbourIncrement[0], neighbourIncrement[1]); 
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

