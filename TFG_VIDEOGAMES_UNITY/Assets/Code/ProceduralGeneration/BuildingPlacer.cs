using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class BuildingPlacer : MonoBehaviour
    {
        private Grid grid = null;
        public void PlaceBuildings(Grid _grid)
        {
            grid = _grid;
            FindBuildingNodes();
        }

        private void FindBuildingNodes()
        {
            int gridSizeX = grid.gridSizeX;
            int gridSizeY = grid.gridSizeY;

            for (int i = 0; i < gridSizeX; i++)
            {
                for (int j = 0; j < gridSizeY; j++)
                {
                    GridNode currentNode = grid.nodesGrid[i, j];
                    if (currentNode.usage != Usage.empty)
                        continue;

                    // Check if it has a decorationNeighbour
                    if (HasDecorationNeighbour(currentNode))
                    {
                        currentNode.usage = Usage.building;
                        continue;
                    }

                    // If no decoration neighbour, check directions
                    List<Direction> neighbours = GetNeighboursData(i, j).neighbours;

                    if (neighbours.Count < 4)
                        continue;

                    bool nodeMeetsRoadInAllDirections = true;
                    // Explore all the neighbours that are empty
                    foreach (Direction direction in neighbours)
                    {
                        // See if they are between roads (should spawn building)
                        // Or if they are at the extreme roads of the map (should spawn building) only one decoration neighbour
                        bool nodeMeetsRoad = AdvanceUntilRoad(direction, i, j);
                        if (!nodeMeetsRoad)
                        {
                            nodeMeetsRoadInAllDirections = false;
                        }
                    }
                    if (nodeMeetsRoadInAllDirections)
                    {
                        currentNode.usage = Usage.building;
                    }

                }
            }
        }
        private bool HasDecorationNeighbour(GridNode node)
        {
            return grid.GetNeighbours(node).Any(x => x.usage == Usage.decoration);
        }
        private NeighboursData GetNeighboursData(int posX, int posY)
        {
            NeighboursData data = new NeighboursData();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (CanPlaceBuilding(grid.nodesGrid[posX + 1, posY])) // Right
                    data.neighbours.Add(Direction.right);
            }
            if (posX - 1 >= 0)
            {
                if (CanPlaceBuilding(grid.nodesGrid[posX - 1, posY])) // Left
                    data.neighbours.Add(Direction.left);
            }

            if (posY + 1 < limitY)
            {
                if (CanPlaceBuilding(grid.nodesGrid[posX, posY + 1])) // Up
                    data.neighbours.Add(Direction.forward);
            }

            if (posY - 1 >= 0)
            {
                if (CanPlaceBuilding(grid.nodesGrid[posX, posY - 1])) // Down
                    data.neighbours.Add(Direction.back);
            }
            return data;
        }
        private bool CanPlaceBuilding(GridNode node)
        {
            return node.usage == Usage.empty || node.usage == Usage.building || node.usage == Usage.decoration;
        }
        private bool AdvanceUntilRoad(Direction direction, int startX, int startY)
        {
            int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

            int i = 1;
            while (true)
            {
                int currentPosX = startX + dir[0] * i;
                int currentPosY = startY + dir[1] * i;

                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    return false;

                GridNode currentNode = grid.nodesGrid[currentPosX, currentPosY];
                if ((currentNode.usage == Usage.road || currentNode.usage == Usage.point))
                {
                    return true;
                }

                i++;
            }
        }
    }
}