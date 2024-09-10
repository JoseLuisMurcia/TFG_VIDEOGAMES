using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PG.Building;

namespace PG
{
    public class BuildingPlacer : MonoBehaviour
    {
        private Grid grid = null;
        [SerializeField] private List<Building> mainBuildings;
        [SerializeField] private List<Building> residentialBuildings;
        [SerializeField] private List<Building> gangBuildings;
        private Dictionary<Vector2Int, GridNode> buildingNodes = new Dictionary<Vector2Int, GridNode>();
        public void PlaceBuildings(Grid _grid)
        {
            grid = _grid;
            FindBuildingNodes();
            InstantiateBuildings();
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
                        buildingNodes.Add(new Vector2Int(i, j), currentNode);
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
                        buildingNodes.Add(new Vector2Int(i, j), currentNode);
                    }

                }
            }
        }
        private void InstantiateBuildings()
        {
            foreach (Vector2Int key in buildingNodes.Keys)
            {
                GridNode currentNode = buildingNodes[key];

                if (currentNode.occupied) continue;

                Region selectedRegion = currentNode.regionType;
                List<Building> availableBuildings = null;

                switch (selectedRegion)
                {
                    case Region.Main:
                        availableBuildings = mainBuildings;
                        break;
                    case Region.Residential:
                        availableBuildings = residentialBuildings;
                        break;
                    case Region.Suburbs:
                        availableBuildings = gangBuildings;
                        break;
                    default:
                        break;
                }

                Building selectedBuilding = availableBuildings[Random.Range(0, availableBuildings.Count)];
                BuildingInfo buildingInfo = selectedBuilding.buildingInfo;

                if (CanPlaceBuilding(key, buildingInfo))
                {
                    // Mark nodes as occupied
                    MarkNodesAsOccupied(key, buildingInfo);

                    // Calculate the average position for the prefab
                    Vector3 averagePosition = CalculateAveragePosition(key, buildingInfo);

                    // Instantiate the building prefab
                    Instantiate(selectedBuilding.gameObject, averagePosition, Quaternion.identity);
                }
            }
        }
        private void MarkNodesAsOccupied(Vector2Int startPos, BuildingInfo buildingInfo)
        {
            for (int x = 0; x < buildingInfo.xValue; x++)
            {
                for (int y = 0; y < buildingInfo.yValue; y++)
                {
                    Vector2Int nodePos = new Vector2Int(startPos.x + x, startPos.y + y);
                    if (buildingNodes.ContainsKey(nodePos))
                    {
                        buildingNodes[nodePos].occupied = true; // Mark nodes as occupied
                    }
                }
            }
        }
        private Vector3 CalculateAveragePosition(Vector2Int startNodeKey, BuildingInfo buildingInfo)
        {
            // Get the average position of all nodes the building will occupy
            Vector3 totalPosition = Vector3.zero;
            int nodeCount = 0;

            for (int x = 0; x < buildingInfo.xValue; x++)
            {
                for (int y = 0; y < buildingInfo.yValue; y++)
                {
                    Vector2Int newNodeKey = new Vector2Int(startNodeKey.x + x, startNodeKey.y + y);
                    if (buildingNodes.ContainsKey(newNodeKey))
                    {
                        GridNode node = buildingNodes[newNodeKey];
                        totalPosition += node.worldPosition;
                        nodeCount++;
                    }
                }
            }

            return totalPosition / nodeCount; // Return the averaged position
        }
        private bool CanPlaceBuilding(Vector2Int startNode, BuildingInfo buildingInfo)
        {
            for (int x = 0; x < buildingInfo.xValue; x++)
            {
                for (int y = 0; y < buildingInfo.yValue; y++)
                {
                    Vector2Int checkPos = new Vector2Int(startNode.x + x, startNode.y + y);
                    if (!buildingNodes.ContainsKey(checkPos) || buildingNodes[checkPos].occupied)
                    {
                        return false;
                    }
                }
            }
            return true;
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