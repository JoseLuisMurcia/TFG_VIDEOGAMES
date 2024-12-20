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
        [SerializeField] private List<Building> gangBuildings, residentialBuildings, mainBuildings, smallResidentialBuildings;
        [SerializeField] private GameObject gangFloor, residentialFloor, mainFloor;
        [SerializeField] private GameObject residentialSidewalk, cornerResidentialSidewalk, flatResidentialSidewalk, threeWayResidentialSidewalk, fourWayResidentialSidewalk;
        [SerializeField] private GameObject gangSidewalk, gangAlleySidewalk, mainSidewalk, mainAlleySidewalk;
        private Dictionary<Vector2Int, GridNode> buildingNodes = new Dictionary<Vector2Int, GridNode>();
        private Dictionary<Vector2Int, GridNode> decorationNodes = new Dictionary<Vector2Int, GridNode>();
        private RegionNodeGrouper regionNodeGrouper;

        [HideInInspector] public List<GridNode> sidewalkGridNodes = new List<GridNode>();
        private static readonly Vector2Int[] cornerDirections = new Vector2Int[]
        {
            new Vector2Int(1, -1),  // Up + Left
            new Vector2Int(1, 1),  // Up + Right
            new Vector2Int(-1, -1),  // Down + Left
            new Vector2Int(-1, 1)  // Down + Right
        };

        // This dictionary will track the number of times each limited-instance building has been placed
        private Dictionary<Building, int> limitedBuildingInstances = new Dictionary<Building, int>();

        // Lists of buildings that have limited instances for each region
        [SerializeField] private List<Building> mainLimitedBuildings;
        [SerializeField] private List<Building> residentialLimitedBuildings;
        [SerializeField] private List<Building> gangLimitedBuildings;
        // Dictionary to track placed positions for each limited-instance building type
        private Dictionary<Building, List<Vector3>> placedBuildingPositions = new Dictionary<Building, List<Vector3>>();
        // Dictionary for irregular meshes that are wrongly placed, let's use MeshRenderer to get the real center
        private Dictionary<GameObject, Vector3> placedLimitedBuildings = new Dictionary<GameObject, Vector3>();

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

            // Return if only sidewalks
            //return;
            // Instantiate buildings
            InstantiateBuildings();

            // Correct placing for irregular meshes
            StartCoroutine(RepositionLimitedBuildings());
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
                                // En este caso hay que unir los que est�n debajo del puente en el nodo slant
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
                                // Identificar si deber�a ser corner o no
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
                                // TODO: Caso extremo donde el 3 way a eliminar est� en el borde del mapa?
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

                Region selectedRegion = currentNode.regionType;
                List<Building> regionBuildings = null;
                List<Building> regionLimitedBuildings = null; // New list for buildings with limited instances
                GameObject availableFloor = null;

                switch (selectedRegion)
                {
                    case Region.Main:
                        regionBuildings = mainBuildings;
                        regionLimitedBuildings = mainLimitedBuildings;
                        availableFloor = mainFloor;
                        break;
                    case Region.Residential:
                        regionBuildings = residentialBuildings;
                        regionLimitedBuildings = residentialLimitedBuildings;
                        availableFloor = residentialFloor;
                        break;
                    case Region.Suburbs:
                        regionBuildings = gangBuildings;
                        regionLimitedBuildings = gangLimitedBuildings;
                        availableFloor = gangFloor;
                        break;
                    default:
                        break;
                }

                // If no building was placed after the attempts, instantiate the floor
                Instantiate(availableFloor, currentNode.worldPosition, Quaternion.identity);

                // If the current node is occupied by other building, ignore
                if (currentNode.occupied || currentNode.usage == Usage.decoration) continue;
                       
                bool buildingPlaced = false;               

                // Randomly select a subset of limited-instance buildings (e.g., 5 buildings)
                int limitedSubsetCount = 5; // Number of limited buildings to try each time
                List<Building> limitedSubset = GetRandomSubset(regionLimitedBuildings, limitedSubsetCount);

                // Sort the subset by size (larger buildings get priority)
                limitedSubset.Sort((a, b) => (b.buildingInfo.xValue * b.buildingInfo.yValue)
                                             .CompareTo(a.buildingInfo.xValue * a.buildingInfo.yValue));

                // Try to place limited-instance buildings first
                foreach (Building limitedBuilding in limitedSubset.ToList()) // Use ToList() to allow safe removal
                {
                    BuildingInfo buildingInfo = limitedBuilding.buildingInfo;

                    int currentInstances = limitedBuildingInstances.ContainsKey(limitedBuilding) ? limitedBuildingInstances[limitedBuilding] : 0;

                    // Check if the current building has reached its max instances
                    if (currentInstances >= buildingInfo.maxInstances)
                    {
                        // Remove the building from the regionLimitedBuildings to prevent further attempts
                        regionLimitedBuildings.Remove(limitedBuilding);
                        continue; // Move to the next limited building
                    }

                    Quaternion rotation = CalculateRotation(key, buildingInfo);
                    int width = buildingInfo.xValue;
                    int height = buildingInfo.yValue;

                    // Adjust width and height based on rotation
                    if (rotation.eulerAngles.y != 180f && rotation.eulerAngles.y != 0f)
                    {
                        width = buildingInfo.yValue;
                        height = buildingInfo.xValue;
                    }

                    Vector3 averagePosition = CalculateAveragePosition(key, width, height);

                    if (IsValidLimitedPlacement(limitedBuilding, averagePosition, 25f))
                    {
                        if (CanPlaceBuilding(key, width, height, buildingInfo.isExterior))
                        {
                            MarkNodesAsOccupied(key, width, height);
                            var go = Instantiate(limitedBuilding.gameObject, averagePosition, rotation);
                            placedLimitedBuildings.Add(go, averagePosition);
                            // Update instance count
                            limitedBuildingInstances[limitedBuilding] = currentInstances + 1;

                            // Track the position
                            if (!placedBuildingPositions.ContainsKey(limitedBuilding))
                            {
                                placedBuildingPositions[limitedBuilding] = new List<Vector3>();
                            }
                            placedBuildingPositions[limitedBuilding].Add(averagePosition);

                            buildingPlaced = true;
                            break;  // Exit after placing a limited-instance building
                        }
                    }
                }

                // Track attempted buildings to avoid duplicates
                HashSet<Building> attemptedBuildings = new HashSet<Building>();
                List<Building> availableBuildings = regionBuildings.ToList();
                List<Building> availableSmallBuildings = smallResidentialBuildings.ToList();
                int maxAttempts = 5;

                // If no limited-instance building was placed, proceed with normal building placement
                for (int attempt = 0; attempt < maxAttempts && !buildingPlaced; attempt++)
                {
                    Building selectedBuilding = null;
                    List<Building> buildingsToChooseFrom = (attempt > 0 && selectedRegion == Region.Residential)
                        ? availableSmallBuildings
                        : availableBuildings;

                    if (buildingsToChooseFrom.Count == 0)
                        break;

                    selectedBuilding = buildingsToChooseFrom[Random.Range(0, buildingsToChooseFrom.Count)];

                    if (attemptedBuildings.Contains(selectedBuilding))
                        continue;

                    attemptedBuildings.Add(selectedBuilding);
                    buildingsToChooseFrom.Remove(selectedBuilding);

                    BuildingInfo buildingInfo = selectedBuilding.buildingInfo;
                    Quaternion rotation = CalculateRotation(key, buildingInfo);
                    int width = buildingInfo.xValue;
                    int height = buildingInfo.yValue;

                    if (rotation.eulerAngles.y != 180f && rotation.eulerAngles.y != 0f)
                    {
                        width = buildingInfo.yValue;
                        height = buildingInfo.xValue;
                    }

                    if (CanPlaceBuilding(key, width, height, false))
                    {
                        MarkNodesAsOccupied(key, width, height);
                        Vector3 averagePosition = CalculateAveragePosition(key, width, height);
                        Instantiate(selectedBuilding.gameObject, averagePosition, rotation);
                        buildingPlaced = true;
                    }
                }

                
            }
        }

        // Helper method to get a random subset of buildings
        private List<Building> GetRandomSubset(List<Building> buildings, int count)
        {
            List<Building> shuffled = buildings.OrderBy(b => Random.value).ToList();
            return shuffled.Take(Mathf.Min(count, buildings.Count)).ToList();
        }
        // Method to check if the placement of a limited-instance building is valid based on the minimum distance
        private bool IsValidLimitedPlacement(Building building, Vector3 position, float minDistance)
        {
            // If there are no recorded positions for this building, it's valid by default
            if (!placedBuildingPositions.ContainsKey(building))
            {
                return true;
            }

            // Check all previously placed positions for this building
            foreach (Vector3 placedPosition in placedBuildingPositions[building])
            {
                // If the distance between the new position and any existing one is less than the minimum, return false
                if (Vector3.Distance(position, placedPosition) < minDistance)
                {
                    return false;
                }
            }

            return true;
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
                bool decorationNotFound = true;

                while (decorationNotFound)
                {
                    // If we've already found a decoration for another direction and our distance for the current direction is greater, abort
                    if (bestDirection != Direction.zero && i > bestDistance)
                        break;

                    // Calculate new decoration
                    int x = node.gridX + offset[0] * i;
                    int y = node.gridY + offset[1] * i;

                    if (Grid.Instance.OutOfGrid(x, y))
                        break;

                    // Check if newNode is road
                    GridNode newNode = grid.nodesGrid[x, y];
                    if (newNode.usage == Usage.decoration)
                    {
                        decorationNotFound = false;
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
        private bool CanPlaceBuilding(Vector2Int startNode, int width, int height, bool isExterior)
        {
            if (isExterior)
            {
                // Check if startNode is neighbour of a decoration node
                var decorationNeighbours = Grid.Instance.GetNeighbours(buildingNodes[startNode], new List<Usage> { Usage.decoration });
                if (decorationNeighbours.Count == 0)
                    return false;
            }

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
        private IEnumerator RepositionLimitedBuildings()
        {
            yield return new WaitForSeconds(2f);
            foreach (var go in placedLimitedBuildings.Keys)
            {
                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer == null)
                    continue;

                Vector3 realPos = renderer.bounds.center;
                Vector3 originalPos = placedLimitedBuildings[go];
                float xOffset = originalPos.x - realPos.x;
                float zOffset = originalPos.z - realPos.z;

                Vector3 newPos = new Vector3(originalPos.x + xOffset, 0f, originalPos.z + zOffset);
                go.transform.position = newPos;
                //SpawnSphere(new Vector3(realPos.x, 0f, realPos.z), Color.blue, 4f, 4f);
                //SpawnSphere(newPos, Color.green, 4f, 4f);
                //SpawnSphere(new Vector3(originalPos.x, 0f, originalPos.z), Color.yellow, 5f, 4f);
            }

        }
        private bool IsStraight(List<Direction> directions)
        {
            if (directions.Count != 2) return false;

            if (directions.Contains(Direction.left) && directions.Contains(Direction.right)) return true;

            if (directions.Contains(Direction.back) && directions.Contains(Direction.forward)) return true;

            return false;
        }
        // Este m�todo es necesario para separar los 3 ways residenciales problem�ticos de los normales
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