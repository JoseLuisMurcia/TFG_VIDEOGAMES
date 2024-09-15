using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static PG.Building;

namespace PG
{
    public class BuildingPlacer : MonoBehaviour
    {
        private Grid grid = null;
        [SerializeField] private List<Building> gangBuildings, residentialBuildings, mainBuildings;
        [SerializeField] private GameObject gangFloor, residentialFloor, mainFloor;
        private Dictionary<Vector2Int, GridNode> buildingNodes = new Dictionary<Vector2Int, GridNode>();
        private RegionNodeGrouper regionNodeGrouper;
        private void Start()
        {
            regionNodeGrouper = new RegionNodeGrouper();
        }
        public void PlaceBuildings(Grid _grid)
        {
            grid = _grid;
            FindBuildingNodes();
            regionNodeGrouper.GroupConnectedNodes(buildingNodes.Values.ToList());
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
            return;
            foreach (Vector2Int key in buildingNodes.Keys)
            {
                GridNode currentNode = buildingNodes[key];

                if (currentNode.occupied) continue;

                Region selectedRegion = currentNode.regionType;
                List<Building> availableBuildings = null;
                GameObject availableFloor = null;

                switch (selectedRegion)
                {
                    case Region.Main:
                        availableBuildings = mainBuildings;
                        availableFloor = mainFloor;
                        break;
                    case Region.Residential:
                        availableBuildings = residentialBuildings;
                        availableFloor = residentialFloor;
                        break;
                    case Region.Suburbs:
                        availableBuildings = gangBuildings;
                        availableFloor = gangFloor;
                        break;
                    default:
                        break;
                }

                Building selectedBuilding = availableBuildings[Random.Range(0, availableBuildings.Count)];
                BuildingInfo buildingInfo = selectedBuilding.buildingInfo;

                // Calculate the rotation
                Quaternion rotation = CalculateRotation(key, buildingInfo);
                int width = buildingInfo.xValue;
                int height = buildingInfo.yValue;

                // The necessary rotation can influence the nodes to select
                if (rotation.eulerAngles.y != 180f && rotation.eulerAngles.y != 0f)
                {
                    width = buildingInfo.yValue;
                    height = buildingInfo.xValue;
                }

                if (CanPlaceBuilding(key, width, height))
                {
                    // Mark nodes as occupied
                    MarkNodesAsOccupied(key, width, height);

                    // Calculate the average position for the prefab
                    Vector3 averagePosition = CalculateAveragePosition(key, width, height);

                    // Instantiate the building prefab
                    Instantiate(selectedBuilding.gameObject, averagePosition, rotation);
                }
                else
                {
                    Instantiate(availableFloor, currentNode.worldPosition, Quaternion.identity);
                }
            }
        }
        private void MarkNodesAsOccupied(Vector2Int startPos, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int nodePos = new Vector2Int(startPos.x + x, startPos.y + y);
                    if (buildingNodes.ContainsKey(nodePos))
                    {
                        buildingNodes[nodePos].occupied = true; // Mark nodes as occupied
                    }
                }
            }
        }
        private Vector3 CalculateAveragePosition(Vector2Int startNodeKey, int width, int height)
        {
            // Get the average position of all nodes the building will occupy
            Vector3 totalPosition = Vector3.zero;
            int nodeCount = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
        private Quaternion CalculateRotation(Vector2Int key, BuildingInfo buildingInfo)
        {
            int bestDistance = int.MaxValue;
            GridNode node = grid.nodesGrid[key.x, key.y];
            Direction bestDirection = Direction.zero;

            foreach (Direction direction in RoadPlacer.Instance.GetAllDirections())
            {
                int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
                int i = 0;
                bool roadNotFound = true;

                while (roadNotFound)
                {
                    // If we've already found a road for another direction and our distance for the current direction is greater, abort
                    if (bestDirection != Direction.zero && i > bestDistance)
                        break;

                    // Calculate new pos
                    int x = node.gridX + offset[0] * i;
                    int y = node.gridY + offset[1] * i;

                    if (Grid.Instance.OutOfGrid(x, y))
                        break;

                    // Check if newNode is road
                    GridNode newNode = grid.nodesGrid[x, y];
                    if (newNode.usage == Usage.road || newNode.usage == Usage.point)
                    {
                        roadNotFound = false;
                        bestDistance = i;
                        bestDirection = direction;
                    }
                    i++;
                }
                
            }
            switch (bestDirection)
            {
                case Direction.left:
                    return Quaternion.Euler(0, 90, 0);
                case Direction.right:
                    return Quaternion.Euler(0, -90, 0);
                case Direction.forward:
                    return Quaternion.Euler(0, 180, 0);
                case Direction.back:
                case Direction.zero:
                default:
                    return Quaternion.identity;
            }
            
        }
        private bool CanPlaceBuilding(Vector2Int startNode, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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