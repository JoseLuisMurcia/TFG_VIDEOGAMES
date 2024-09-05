using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class GridPathfinder : MonoBehaviour
    {
        public static GridPathfinder instance;
        private void Awake()
        {
            instance = this;
        }
        public List<GridNode> FindPath(GridNode startNode, GridNode targetNode, List<GridNode> forbiddenNodes = null)
        {
            bool pathSuccess = false;
            startNode.gCost = 0;
            Heap<GridNode> openSet = new Heap<GridNode>(Grid.Instance.MaxSize);
            HashSet<GridNode> closedSet = new HashSet<GridNode>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                GridNode currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                foreach (GridNode neighbour in Grid.Instance.GetNeighboursInLine(currentNode))
                {
                    if (forbiddenNodes != null && forbiddenNodes.Contains(neighbour))
                    {
                        continue;
                    }

                    if (neighbour != targetNode)
                    {
                        if (MovementInvalid(currentNode, neighbour, targetNode))
                            continue;

                        if (closedSet.Contains(neighbour) || !EnoughSpace(currentNode, neighbour, targetNode))
                        {
                            continue;
                        }
                    }
                   

                    float newMovementCostToNeighbour = currentNode.gCost 
                        + GetDistanceHeuristic(currentNode, neighbour) 
                        + AddCostIfDirectionChanges(currentNode, neighbour) 
                        + AddCostIfIntersectionClose(targetNode, neighbour);
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
        private bool MovementInvalid(GridNode currentNode, GridNode neighbour, GridNode targetNode)
        {
            if (neighbour != targetNode && (neighbour.usage == Usage.road || neighbour.usage == Usage.point))
                return true;

            return false;
        }
        private bool EnoughSpace(GridNode currentNode, GridNode neighbour, GridNode targetNode)
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

            int[] neighbourIncrement = Visualizer.Instance.GetLateralIncrementOnDirection(dirX, dirY);
            return Visualizer.Instance.EnoughSpace(endX, endY, neighbourIncrement[0], neighbourIncrement[1], targetNode); 
        }
        private int AddCostIfIntersectionClose(GridNode targetNode, GridNode neighbour)
        {
            if (targetNode == neighbour) return 0;

            // Add cost if an intersection is detected near a certain distance from this neighbour
            int cost = 0;
            int distance = 4;
            foreach (Direction direction in RoadPlacer.Instance.GetAllDirections())
            {
                // From the neighbour, explore neighbours to find intersections
                int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
                for (int i = 1; i <= distance; i++)
                {
                    int newX = neighbour.gridX + offset[0] * i;
                    int newY = neighbour.gridY + offset[1] * i;

                    if (!Grid.Instance.OutOfGrid(newX, newY))
                    {
                        List<Direction> neighboursDir = RoadPlacer.Instance.GetNeighboursData(newX, newY).neighbours;
                        GridNode neighbourNode = Grid.Instance.nodesGrid[newX, newY];

                        if (neighboursDir.Count >= 3)
                            cost += 20;
                    }
                }
            }
            // If lateral changes to vertical, or viceversa, change
            return 0;
        }
        private int AddCostIfDirectionChanges(GridNode currentNode, GridNode neighbour)
        {
            // If direction changes, return an int, if not, 0
            // Get direction from parent to current
            GridNode parentNode = currentNode.parent;
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
        private List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
        {
            List<GridNode> nodes = new List<GridNode>();
            GridNode currentNode = endNode;

            while (currentNode != startNode)
            {
                nodes.Add(currentNode);
                currentNode = currentNode.parent;
            }
            nodes.Add(startNode);
            nodes.Reverse();
            return nodes;
        }

        float GetDistanceHeuristic(GridNode nodeA, GridNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

    }
}

