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
        [SerializeField] private List<Building> gangBuildings, residentialBuildings, mainBuildings, smallResidentialBuildings;
        [SerializeField] private GameObject gangFloor, residentialFloor, mainFloor;
        [SerializeField] private GameObject residentialSidewalk, cornerResidentialSidewalk, flatResidentialSidewalk, threeWayResidentialSidewalk, fourWayResidentialSidewalk;
        [SerializeField] private GameObject gangSidewalk, gangAlleySidewalk, mainSidewalk, mainAlleySidewalk;
        private Dictionary<Vector2Int, GridNode> buildingNodes = new Dictionary<Vector2Int, GridNode>();
        private Dictionary<Vector2Int, GridNode> decorationNodes = new Dictionary<Vector2Int, GridNode>();
        private RegionNodeGrouper regionNodeGrouper;
        [SerializeField] private PerlinNoiseGenerator perlinGeneratorPrefab;

        [HideInInspector] public List<GridNode> sidewalkGridNodes = new List<GridNode>();
        private static readonly Vector2Int[] cornerDirections = new Vector2Int[]
        {
            new Vector2Int(1, -1),  // Up + Left
            new Vector2Int(1, 1),  // Up + Right
            new Vector2Int(-1, -1),  // Down + Left
            new Vector2Int(-1, 1)  // Down + Right
        };
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

            foreach (var block in regionNodeGrouper.suburbsBlocks)
            {
                break;
                block.RemoveWhere(x => x.isAlley);
                PerlinNoiseGenerator generator = Instantiate(perlinGeneratorPrefab);

                int minX = block.Min(x => x.gridX);
                int maxX = block.Max(x => x.gridX);
                int minY = block.Min(x => x.gridY);
                int maxY = block.Max(x => x.gridY);
                generator.GenerateNoise(maxX - minX, maxY - minY, block);
                //HashSet<HashSet<GridNode>> splitBlock = SplitGangBlock(block);
                //foreach (var node in block)
                //{
                //    //SpawnSphere(node.worldPosition, Color.green, 2f, 2f);
                //}
            }

            return;
            // Instantiate buildings
            InstantiateBuildings();
        }
        private void PlaceSidewalks(Dictionary<Vector2Int, GameObject> roadDictionary)
        {
            Dictionary<Vector2Int, GameObject> sidewalksDictionary = new Dictionary<Vector2Int, GameObject>();
            Dictionary<Vector2Int, GameObject> threeWaysDictionary = new Dictionary<Vector2Int, GameObject>();
            Dictionary<Vector2Int, GameObject> cornerSidewalksDictionary = new Dictionary<Vector2Int, GameObject>();
            // Sidewalk creation
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    GridNode currentNode = grid.nodesGrid[i, j];
                    List<Direction> neighbours = GetUsageNeighbours(i, j, new List<Usage>() { Usage.empty, Usage.building, Usage.decoration });
                    Vector2Int key = new Vector2Int(i, j);

                    GameObject _sidewalk = null;
                    GameObject _alleySidewalk = null;
                    switch (currentNode.regionType)
                    {
                        case Region.Main:
                            _sidewalk = mainSidewalk;
                            _alleySidewalk = mainAlleySidewalk;
                            break;
                        case Region.Residential:
                            _sidewalk = residentialSidewalk;
                            break;
                        case Region.Suburbs:
                            _sidewalk = gangSidewalk;
                            _alleySidewalk = gangAlleySidewalk;
                            break;
                        default:
                            break;
                    }
                    if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                    {
                        switch (neighbours.Count)
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
                        sidewalkGridNodes.Add(currentNode);
                        Quaternion rotation = Quaternion.identity;
                        // Not residential
                        if (currentNode.regionType != Region.Residential)
                        {
                            GameObject sidewalkGO;
                            if (currentNode.isAlley)
                            {
                                sidewalkGO = Instantiate(_alleySidewalk, currentNode.worldPosition, rotation, transform);
                            }
                            else
                            {
                                sidewalkGO = Instantiate(_sidewalk, currentNode.worldPosition, rotation, transform);
                            }

                            sidewalksDictionary.Add(key, sidewalkGO);
                            continue;
                        }

                        // Residential
                        List<Direction> decorationNeighbours = GetUsageNeighbours(i, j, new List<Usage>() { Usage.decoration });

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
                            cornerSidewalksDictionary.Add(key, sidewalkGO);
                        }
                    }
                }
            }

            // Look for the bridge slant nodes in sidewalks in residential nodes
            foreach (Vector2Int key in sidewalksDictionary.Keys)
            {
                var currentNode = grid.nodesGrid[key.x, key.y];
                if (currentNode.roadType != RoadType.Bridge || currentNode.regionType != Region.Residential || !roadDictionary.ContainsKey(key)) continue;

                // Discard straight roads (this happens when the bridge is merged to another bridge)
                GameObject roadPrefab = roadDictionary[key];
                Road road;
                if (roadPrefab.TryGetComponent(out road))
                {
                    if (road.typeOfRoad == TypeOfRoad.Straight) continue;
                }

                // We've found the slant node
                // Delete prefab underneath and neighbours depending on horizontal or vertical bridge
                bool isHorizontal = IsHorizontalBridge(key.x, key.y);
                if (isHorizontal)
                {
                    // If horizontal delete vertical
                    Destroy(sidewalksDictionary[key]);
                    Vector2Int positiveVerticalKey = new Vector2Int(key.x, key.y + 1);
                    Vector2Int negativeVerticalKey = new Vector2Int(key.x, key.y - 1);
                    GridNode negativeNode = grid.nodesGrid[negativeVerticalKey.x, negativeVerticalKey.y];

                    if (negativeNode.regionType != Region.Residential) continue;

                    if (sidewalksDictionary.ContainsKey(positiveVerticalKey)) Destroy(sidewalksDictionary[positiveVerticalKey]);
                    if (sidewalksDictionary.ContainsKey(negativeVerticalKey)) Destroy(sidewalksDictionary[negativeVerticalKey]);

                    Instantiate(residentialSidewalk, currentNode.worldPosition, Quaternion.identity, transform);
                    // Esta en la izquierda si a la derecha esta el nodo central del bridge
                    bool isLeft = roadDictionary.ContainsKey(new Vector2Int(key.x + 1, key.y));
                    Quaternion rotation = isLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition + Vector3.forward * 4f, rotation, transform);
                    //SpawnSphere(currentNode.worldPosition + Vector3.forward * 4f, Color.magenta, 2f, 2f);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition - Vector3.forward * 4f, rotation, transform);
                    //SpawnSphere(currentNode.worldPosition - Vector3.forward * 4f, Color.cyan, 2f, 2f);
                }
                else
                {
                    // If vertical delete horizontal
                    Destroy(sidewalksDictionary[key]);
                    Vector2Int positiveHorizontalKey = new Vector2Int(key.x + 1, key.y);
                    Vector2Int negativeHorizontalKey = new Vector2Int(key.x - 1, key.y);
                    GridNode negativeNode = grid.nodesGrid[negativeHorizontalKey.x, negativeHorizontalKey.y];

                    if (negativeNode.regionType != Region.Residential) continue;

                    if (sidewalksDictionary.ContainsKey(positiveHorizontalKey)) Destroy(sidewalksDictionary[positiveHorizontalKey]);
                    if (sidewalksDictionary.ContainsKey(negativeHorizontalKey)) Destroy(sidewalksDictionary[negativeHorizontalKey]);

                    Instantiate(residentialSidewalk, currentNode.worldPosition, Quaternion.Euler(0f, 90f, 0f), transform);
                    // Esta arriba si abajo esta el nodo central del bridge
                    bool isUp = roadDictionary.ContainsKey(new Vector2Int(key.x, key.y - 1));
                    Quaternion rotation = isUp ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition + Vector3.right * 4f, rotation, transform);
                    //SpawnSphere(currentNode.worldPosition + Vector3.right * 4f, Color.red, 2f, 2f);
                    Instantiate(threeWayResidentialSidewalk, currentNode.worldPosition - Vector3.right * 4f, rotation, transform);
                    //SpawnSphere(currentNode.worldPosition - Vector3.right * 4f, Color.blue, 2f, 2f);

                }

            }

            // Process the three way residential nodes to see if there are sidewalks to substitute with the normal 2 way
            foreach (Vector2Int key in cornerSidewalksDictionary.Keys)
            {
                List<Direction> roadNeighbours = GetUsageNeighbours(key.x, key.y, new List<Usage>() { Usage.road });
                if (roadNeighbours.Count == 2)
                {
                    // Get the right opposite direction to look for 3 ways                  
                    // The right opposite direction will be the one where there's not a corner residential sidewalk prefab
                    foreach (Direction roadDirection in roadNeighbours)
                    {
                        Direction oppositeDirection = RoadPlacer.Instance.GetOppositeDir(roadDirection);
                        int[] dir = RoadPlacer.Instance.DirectionToInt(oppositeDirection);
                        // Check if the first neighbour is a 3way, only if it is we should iterate in a straight line
                        int currentPosX = key.x + dir[0];
                        int currentPosY = key.y + dir[1];
                        Vector2Int newPosKey = new Vector2Int(currentPosX, currentPosY);
                        if (!threeWaysDictionary.ContainsKey(newPosKey))
                            continue;

                        // If we are in the long scenario (corner-3way-3way...corner) iterate
                        int i = 1;
                        GridNode currentNode = grid.nodesGrid[key.x, key.y];
                        while (true)
                        {
                            currentPosX = key.x + dir[0] * i;
                            currentPosY = key.y + dir[1] * i;
                            newPosKey = new Vector2Int(currentPosX, currentPosY);
                            try
                            {
                                currentNode = grid.nodesGrid[currentPosX, currentPosY];
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError("End of world reached: " + ex.Message);
                            }
                            List<Direction> decorationNeighbours = GetUsageNeighbours(currentPosX, currentPosY, new List<Usage>() { Usage.decoration });

                            // We've reached a corner or empty space or current node is a straight
                            if (IsStraight(decorationNeighbours) || cornerSidewalksDictionary.ContainsKey(newPosKey) || currentNode.usage != Usage.decoration)
                            {
                                break;
                            }

                            // Check if current node is a threeWay, we should delete it and spawn either a corner or a straight
                            if (threeWaysDictionary.ContainsKey(newPosKey))
                            {
                                Destroy(threeWaysDictionary[newPosKey]);
                                // Identificar si debería ser corner o no
                                // Si avanzo en estad direccion y no hay sidewalk, debe ser corner
                                currentPosX = key.x + dir[0] * (i + 1);
                                currentPosY = key.y + dir[1] * (i + 1);
                                if (!Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                                {
                                    GridNode newNode = grid.nodesGrid[currentPosX, currentPosY];
                                    if (newNode.usage != Usage.decoration)
                                    {
                                        Quaternion cornerRotation = Quaternion.identity;
                                        if (roadNeighbours.Contains(Direction.left) && roadNeighbours.Contains(Direction.back))
                                        {
                                            cornerRotation = Quaternion.Euler(0, 180, 0);
                                        }
                                        else if (roadNeighbours.Contains(Direction.left) && roadNeighbours.Contains(Direction.forward))
                                        {
                                            cornerRotation = Quaternion.Euler(0, -90, 0);
                                        }
                                        else if (roadNeighbours.Contains(Direction.back) && roadNeighbours.Contains(Direction.right))
                                        {
                                            cornerRotation = Quaternion.Euler(0, 90, 0);
                                        }
                                        Instantiate(cornerResidentialSidewalk, currentNode.worldPosition, cornerRotation, transform);
                                    }
                                    else
                                    {
                                        Quaternion rotation = Quaternion.identity;
                                        if (oppositeDirection == Direction.left || oppositeDirection == Direction.right)
                                        {
                                            rotation = Quaternion.Euler(0, 90, 0);
                                        }
                                        Instantiate(residentialSidewalk, currentNode.worldPosition, rotation, transform);
                                    }
                                }
                                // TODO: Caso extremo donde el 3 way a eliminar está en el borde del mapa?
                            }

                            i++;
                        }

                    }
                    // Once you've identified that, advance in that direction destroying the 3 way prefab and spawning the straight one until we reach a corner sidewalk

                    // Special scenario with only one problematic prefab
                    // We either delete the bottom part or the top one
                    // If we delete the top part (3way) we need to spawn a normal 2 way instead
                    // If we delete the bottom part, we need to delete the bottom corners and the top 3 way, then replace the top with 2 ways
                }
            }
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
                    List<Direction> neighbours = GetUsageNeighbours(i, j, new List<Usage>() { Usage.empty, Usage.building, Usage.decoration });

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

                if (currentNode.occupied || currentNode.usage == Usage.decoration) continue;

                Region selectedRegion = currentNode.regionType;
                List<Building> regionBuildings = null;
                GameObject availableFloor = null;

                switch (selectedRegion)
                {
                    case Region.Main:
                        regionBuildings = mainBuildings;
                        availableFloor = mainFloor;
                        break;
                    case Region.Residential:
                        regionBuildings = residentialBuildings;
                        availableFloor = residentialFloor;
                        break;
                    case Region.Suburbs:
                        regionBuildings = gangBuildings;
                        availableFloor = gangFloor;
                        break;
                    default:
                        break;
                }

                // Track attempted buildings to avoid duplicates
                HashSet<Building> attemptedBuildings = new HashSet<Building>();
                List<Building> availableBuildings = regionBuildings.ToList();
                List<Building> availableSmallBuildings = smallResidentialBuildings.ToList();
                bool buildingPlaced = false;
                int maxAttempts = 5;

                for (int attempt = 0; attempt < maxAttempts && !buildingPlaced; attempt++)
                {
                    Building selectedBuilding = null;

                    // Create a temporary list to hold available buildings to choose from
                    List<Building> buildingsToChooseFrom = (attempt > 0 && selectedRegion == Region.Residential)
                        ? availableSmallBuildings
                        : availableBuildings;

                    // Randomly pick a building that hasn't been attempted yet
                    if (buildingsToChooseFrom.Count == 0)
                        break;

                    selectedBuilding = buildingsToChooseFrom[Random.Range(0, buildingsToChooseFrom.Count)];

                    // If we've already tried this building, skip it
                    if (attemptedBuildings.Contains(selectedBuilding))
                        continue;

                    // Store the attempted building to prevent retries
                    attemptedBuildings.Add(selectedBuilding);

                    // Since we're working with copies of the original lists, we don't need to remove the building
                    // from the original availableBuildings or availableSmallBuildings collections, only from our local copy
                    buildingsToChooseFrom.Remove(selectedBuilding);


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

                        buildingPlaced = true;  // Successfully placed a building, exit the loop
                    }
                }

                // If no building was placed after the attempts, instantiate the floor
                if (!buildingPlaced)
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
            return grid.GetNeighbours(node, new List<Usage>() { Usage.decoration }).Count > 0;
        }
        public List<Direction> GetUsageNeighbours(int posX, int posY, List<Usage> usages)
        {
            List<Direction> neighbours = new List<Direction>();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (usages.Contains(grid.nodesGrid[posX + 1, posY].usage)) // Right
                    neighbours.Add(Direction.right);
            }
            if (posX - 1 >= 0)
            {
                if (usages.Contains(grid.nodesGrid[posX - 1, posY].usage)) // Left
                    neighbours.Add(Direction.left);
            }

            if (posY + 1 < limitY)
            {
                if (usages.Contains(grid.nodesGrid[posX, posY + 1].usage)) // Up
                    neighbours.Add(Direction.forward);
            }

            if (posY - 1 >= 0)
            {
                if (usages.Contains(grid.nodesGrid[posX, posY - 1].usage)) // Down
                    neighbours.Add(Direction.back);
            }
            return neighbours;
        }
        public List<GridNode> GetUsageNeighbourNodes(int posX, int posY, List<Usage> usages)
        {
            List<GridNode> neighbours = new List<GridNode>();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (usages.Contains(grid.nodesGrid[posX + 1, posY].usage)) // Right
                    neighbours.Add(grid.nodesGrid[posX + 1, posY]);
            }
            if (posX - 1 >= 0)
            {
                if (usages.Contains(grid.nodesGrid[posX - 1, posY].usage)) // Left
                    neighbours.Add(grid.nodesGrid[posX - 1, posY]);
            }

            if (posY + 1 < limitY)
            {
                if (usages.Contains(grid.nodesGrid[posX, posY + 1].usage)) // Up
                    neighbours.Add(grid.nodesGrid[posX, posY + 1]);
            }

            if (posY - 1 >= 0)
            {
                if (usages.Contains(grid.nodesGrid[posX, posY - 1].usage)) // Down
                    neighbours.Add(grid.nodesGrid[posX, posY - 1]);
            }
            return neighbours;
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
        private bool IsStraight(List<Direction> directions)
        {
            if (directions.Count != 2) return false;

            if (directions.Contains(Direction.left) && directions.Contains(Direction.right)) return true;

            if (directions.Contains(Direction.back) && directions.Contains(Direction.forward)) return true;

            return false;
        }
        // Este método es necesario para separar los 3 ways residenciales problemáticos de los normales
        private bool HasCornerDecorationNeighbour(int x, int y)
        {
            foreach (Vector2Int direction in cornerDirections)
            {
                int newX = x + direction[0];
                int newY = y + direction[1];

                if (grid.OutOfGrid(newX, newY)) continue;

                if (grid.nodesGrid[newX, newY].usage == Usage.decoration)
                    return true;
            }
            return true;
        }
        private HashSet<HashSet<GridNode>> SplitGangBlock(HashSet<GridNode> block)
        {
            HashSet<HashSet<GridNode>> splitBlocks = new HashSet<HashSet<GridNode>>();
            // Find out if it's horizontal or vertical
            int minX = block.Min(x => x.gridX);
            int maxX = block.Max(x => x.gridX);
            int minY = block.Min(x => x.gridY);
            int maxY = block.Max(x => x.gridY);

            if ((maxX - minX) < 2 || (maxY - minY) < 2)
            {
                SpawnSphere(new Vector3(
                    block.Average(node => node.worldPosition.x),
                    block.Average(node => node.worldPosition.y),
                    block.Average(node => node.worldPosition.z)
                ), Color.blue, 3f, 4f);
                return new HashSet<HashSet<GridNode>>();
            }

            return splitBlocks;
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
    }
}