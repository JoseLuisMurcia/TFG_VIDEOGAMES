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
            Heap<Node> openSet = new Heap<Node>(Grid.Instance.MaxSize);
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

                foreach (Node neighbour in Grid.Instance.GetNeighboursInLine(currentNode))
                {
                    if(neighbour != targetNode)
                    {
                        if (MovementInvalid(currentNode, neighbour, targetNode))
                            continue;

                        if (closedSet.Contains(neighbour) || !EnoughSpace(currentNode, neighbour, targetNode))
                        {
                            continue;
                        }
                    }
                   

                    float newMovementCostToNeighbour = currentNode.gCost + GetDistanceHeuristic(currentNode, neighbour) + AddCostIfDirectionChanges(currentNode, neighbour);
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
            if (neighbour != targetNode && (neighbour.usage == Usage.road || neighbour.usage == Usage.point))
                return true;

            return false;
        }
        private bool EnoughSpace(Node currentNode, Node neighbour, Node targetNode)
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
            return Visualizer.instance.EnoughSpace(endX, endY, neighbourIncrement[0], neighbourIncrement[1], targetNode); 
        }
        private int AddCostIfDirectionChanges(Node currentNode, Node neighbour)
        {
            // If direction changes, return an int, if not, 0
            // Get direction from parent to current
            Node parentNode = currentNode.parent;
            if (parentNode == null)
                return 0;

            Direction dirFromParentToCurrent = RoadPlacer.Instance.GetDirectionBasedOnPos(parentNode, currentNode);
            // Get direction from current to neighbour
            Direction dirFromCurrentToNeighbour = RoadPlacer.Instance.GetDirectionBasedOnPos(currentNode, neighbour);
            if(dirFromParentToCurrent == Direction.left || dirFromParentToCurrent == Direction.right) // Horizontal movement
            {
                if (dirFromCurrentToNeighbour == Direction.forward || dirFromCurrentToNeighbour == Direction.back) 
                    return 30;

                return 0;
            }
            else // Vertical movement
            {
                if (dirFromCurrentToNeighbour == Direction.left || dirFromCurrentToNeighbour == Direction.right)
                    return 30;

                return 0;
            }
                // If lateral changes to vertical, or viceversa, change
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
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

    }
}

