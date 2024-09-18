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
        [SerializeField] private GameObject gangSidewalk, residentialSidewalk, mainSidewalk, cornerResidentialSidewalk, flatResidentialSidewalk, threeWayResidentialSidewalk, fourWayResidentialSidewalk;
        private Dictionary<Vector2Int, GridNode> buildingNodes = new Dictionary<Vector2Int, GridNode>();
        private Dictionary<Vector2Int, GridNode> decorationNodes = new Dictionary<Vector2Int, GridNode>();
        private RegionNodeGrouper regionNodeGrouper;
        private void Start()
        {
            regionNodeGrouper = new RegionNodeGrouper();
        }
        public void PlaceBuildings(Grid _grid, Dictionary<Vector2Int, GameObject> roadDictionary)
        {
            grid = _grid;

            // Find the nodes suitable for placing buildings
            FindBuildingNodes();

            // Combine building and decoration nodes into one list
            List<GridNode> buildingAndSidewalkNodes = new List<GridNode>(buildingNodes.Values);
            buildingAndSidewalkNodes.AddRange(decorationNodes.Values);

            // Group connected nodes based on regions
            regionNodeGrouper.GroupConnectedNodes(buildingAndSidewalkNodes, this);

            // Place sidewalks based on road data
            PlaceSidewalks(roadDictionary);

            // Instantiate buildings
            InstantiateBuildings();
        }
        private void PlaceSidewalks(Dictionary<Vector2Int, GameObject> roadDictionary)
        {
            Dictionary<Vector2Int, GameObject> sidewalksDictionary = new Dictionary<Vector2Int, GameObject>();
            Dictionary<Vector2Int, GameObject> threeWaysDictionary = new Dictionary<Vector2Int, GameObject>();
            // Sidewalk creation
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    GridNode currentNode = grid.nodesGrid[i, j];
                    NeighboursData data = GetNeighboursData(i, j);
                    List<Direction> neighbours = data.neighbours;
                    Vector2Int key = new Vector2Int(i, j);

                    GameObject _sidewalk = null;
                    switch (currentNode.regionType)
                    {
                        case Region.Main:
                            _sidewalk = mainSidewalk;
                            break;
                        case Region.Residential:
                            _sidewalk = residentialSidewalk;
                            break;
                        case Region.Suburbs:
                            _sidewalk = gangSidewalk;
                            break;
                        default:
                            break;
                    }
                    if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                    {
                        switch (data.neighbours.Count)
                        {
                            case 2:
                                // Slants
                                // En este caso hay que unir los que están debajo del puente en el nodo slant
                                GameObject selectedSidewalk = currentNode.regionType == Region.Residential ? flatResidentialSidewalk : _sidewalk;

                                if (currentNode.roadType == RoadType.Bridge)
                                {
                                    GameObject sidewalkGO = Instantiate(selectedSidewalk, currentNode.worldPosition, Quaternion.identity, transform);
                                    sidewalksDictionary.Add(key, sidewalkGO);
                                    break;
                                }
                                // If corner and not straight
                                if (!(neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                                {
                                    Instantiate(selectedSidewalk, currentNode.worldPosition, Quaternion.identity, transform);
                                }
                                break;
                        }
                    }
                    else if (currentNode.usage == Usage.decoration)
                    {
                        Quaternion rotation = Quaternion.identity;
                        // Not residential
                        if (currentNode.regionType != Region.Residential)
                        {
                            Instantiate(_sidewalk, currentNode.worldPosition, rotation, transform);
                            continue;
                        }

                        // Residential
                        List<Direction> decorationNeighbours = GetDecorationNeighbours(i, j).neighbours;

                        // If 3 way
                        if (decorationNeighbours.Count == 3)
                        {
                            if (!decorationNeighbours.Contains(Direction.forward))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            else if (!decorationNeighbours.Contains(Direction.right))
                            {
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else if (!decorationNeighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, -90, 0);
                            }

                            GameObject sidewalkGO = Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition, rotation, transform);
                            threeWaysDictionary.Add(key, sidewalkGO);
                            continue;
                        }
                        else if (decorationNeighbours.Count == 4) // If 4 way
                        {
                            Instantiate(fourWayResidentialSidewalk, currentNode.worldPosition, rotation, transform);
                            continue;
                        }

                        // If straight
                        if ((decorationNeighbours.Contains(Direction.left) && decorationNeighbours.Contains(Direction.right))
                            || (decorationNeighbours.Contains(Direction.forward) && decorationNeighbours.Contains(Direction.back)))
                        {
                            if (decorationNeighbours.Contains(Direction.left) || decorationNeighbours.Contains(Direction.right))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            Instantiate(_sidewalk, currentNode.worldPosition, rotation, transform);
                        }
                        else // Corner
                        {
                            if (decorationNeighbours.Contains(Direction.left) && decorationNeighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else if (decorationNeighbours.Contains(Direction.left) && decorationNeighbours.Contains(Direction.forward))
                            {
                                rotation = Quaternion.Euler(0, -90, 0);
                            }
                            else if (decorationNeighbours.Contains(Direction.back) && decorationNeighbours.Contains(Direction.right))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            GameObject sidewalkGO = Instantiate(cornerResidentialSidewalk, currentNode.worldPosition, rotation, transform);
                            sidewalksDictionary.Add(key, sidewalkGO);
                        }
                    }
                }
            }

            // Look for the bridge slant nodes in sidewalks in residential nodes
            foreach (Vector2Int key in sidewalksDictionary.Keys)
            {
                var currentNode = grid.nodesGrid[key.x, key.y];
                if (currentNode.roadType != RoadType.Bridge || currentNode.regionType != Region.Residential || !roadDictionary.ContainsKey(key)) continue;

                // We've found the slant node
                // Delete prefab underneath and neighbours depending on horizontal or vertical bridge
                bool isHorizontal = IsHorizontalBridge(key.x, key.y);
                if (isHorizontal)
                {
                    // If horizontal delete vertical
                    Destroy(sidewalksDictionary[key]);
                    Vector2Int positiveVerticalKey = new Vector2Int(key.x, key.y + 1);
                    Vector2Int negativeVerticalKey = new Vector2Int(key.x, key.y - 1);
                    if (sidewalksDictionary.ContainsKey(positiveVerticalKey)) Destroy(sidewalksDictionary[positiveVerticalKey]);
                    if (sidewalksDictionary.ContainsKey(negativeVerticalKey)) Destroy(sidewalksDictionary[negativeVerticalKey]);

                    Instantiate(residentialSidewalk, currentNode.worldPosition, Quaternion.identity, transform);
                    // Esta en la izquierda si a la derecha esta el nodo central del bridge
                    bool isLeft = roadDictionary.ContainsKey(new Vector2Int(key.x + 1, key.y));
                    Quaternion rotation = isLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition + Vector3.forward * 4f, rotation, transform);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition - Vector3.forward * 4f, rotation, transform);
                }
                else
                {
                    // If vertical delete horizontal
                    Destroy(sidewalksDictionary[key]);
                    Vector2Int positiveHorizontalKey = new Vector2Int(key.x + 1, key.y);
                    Vector2Int negativeHorizontalKey = new Vector2Int(key.x - 1, key.y);
                    if (sidewalksDictionary.ContainsKey(positiveHorizontalKey)) Destroy(sidewalksDictionary[positiveHorizontalKey]);
                    if (sidewalksDictionary.ContainsKey(negativeHorizontalKey)) Destroy(sidewalksDictionary[negativeHorizontalKey]);

                    Instantiate(residentialSidewalk, currentNode.worldPosition, Quaternion.Euler(0f, 90f, 0f), transform);
                    // Esta arriba si abajo esta el nodo central del bridge
                    bool isUp = roadDictionary.ContainsKey(new Vector2Int(key.x, key.y - 1));
                    Quaternion rotation = isUp ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition + Vector3.right * 4f, rotation, transform);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition - Vector3.right * 4f, rotation, transform);

                }

            }

            // Process the three way residential nodes to see if there are sidewalks to substitute with the normal 2 way
            foreach (Vector2Int key in threeWaysDictionary.Keys)
            {

            }
        }
        private NeighboursData GetDecorationNeighbours(int posX, int posY)
        {
            NeighboursData data = new NeighboursData();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (grid.nodesGrid[posX + 1, posY].usage == Usage.decoration) // Right
                    data.neighbours.Add(Direction.right);
            }
            if (posX - 1 >= 0)
            {
                if (grid.nodesGrid[posX - 1, posY].usage == Usage.decoration) // Left
                    data.neighbours.Add(Direction.left);
            }

            if (posY + 1 < limitY)
            {
                if (grid.nodesGrid[posX, posY + 1].usage == Usage.decoration) // Up
                    data.neighbours.Add(Direction.forward);
            }

            if (posY - 1 >= 0)
            {
                if (grid.nodesGrid[posX, posY - 1].usage == Usage.decoration) // Down
                    data.neighbours.Add(Direction.back);
            }
            return data;
        }
        private bool IsHorizontalBridge(int slantX, int slantY)
        {
            // We know that bridges are a list of 5 nodes marked as bridges either horizontally or vertically
            // Advancing positive X should always find either the center node or the last node from the slant IF its horizontal
            int newX = slantX + 1;
            int newY = slantY;
            if (grid.nodesGrid[newX, newY].roadType == RoadType.Bridge)
                return true;

            return false;
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
                    if (currentNode.usage == Usage.decoration)
                        decorationNodes.Add(new Vector2Int(i, j), currentNode);

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

                if (currentNode.occupied || currentNode.usage == Usage.decoration) continue;

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
                    if (!buildingNodes.ContainsKey(checkPos) || buildingNodes[checkPos].occupied || buildingNodes[checkPos].usage == Usage.decoration)
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
        public NeighboursData GetNeighboursData(int posX, int posY)
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
        public bool AdvanceUntilRoad(Direction direction, int startX, int startY)
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