using PG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class PropPlacer : MonoBehaviour
    {
        private float minDistanceBetweenProps = 5f;
        private int maxAttemptsPerPos = 10;
        private float nodeRadius;

        [SerializeField] private Prop[] mainProps;
        [SerializeField] private Prop[] residentialProps;
        [SerializeField] private Prop[] gangProps;
        [SerializeField] private Prop[] mainStreetLamps;
        [SerializeField] private Prop[] residentialStreetLamps;
        [SerializeField] private Prop[] gangStreetLamps;

        private Dictionary<Vector2Int, GridNode> mainSidewalks = new Dictionary<Vector2Int, GridNode>();
        private Dictionary<Vector2Int, GridNode> residentialSidewalks = new Dictionary<Vector2Int, GridNode>();
        private Dictionary<Vector2Int, GridNode> gangSidewalks = new Dictionary<Vector2Int, GridNode>();

        public void PlaceProps(BuildingPlacer buildingPlacer)
        {
            buildingPlacer.sidewalkGridNodes.ForEach(x =>
            {
                switch (x.regionType)
                {
                    case Region.Main:
                        mainSidewalks.Add(new Vector2Int(x.gridX, x.gridY), x);
                        break;
                    case Region.Residential:
                        residentialSidewalks.Add(new Vector2Int(x.gridX, x.gridY), x);
                        break;
                    case Region.Suburbs:
                        gangSidewalks.Add(new Vector2Int(x.gridX, x.gridY), x);
                        break;
                }
            });
            PlaceDistrictProps(mainSidewalks, mainProps, Region.Main);
            PlaceDistrictProps(residentialSidewalks, residentialProps, Region.Residential);
            PlaceDistrictProps(gangSidewalks, gangProps, Region.Suburbs);
        }
        private void PlaceDistrictProps(Dictionary<Vector2Int, GridNode> sidewalkPositions, Prop[] propPrefabs, Region regionType)
        {
            if (propPrefabs.Length == 0) return;

            nodeRadius = Grid.Instance.nodeRadius;

            List<Vector3> propPositions = new List<Vector3>();  // Already placed prop positions
            List<Vector3> streetLampPositions = new List<Vector3>();  // Already placed street lamp positions
            HashSet<Vector2Int> processedSidewalks = new HashSet<Vector2Int>();  // Track processed sidewalks

            int propsCounter = 0;  // Counter for props placed
            int streetLampFrequency = GetStreetLampFrequency(regionType);  // Frequency of street lamps
            Prop[] streetLampPrefabs = GetStreetLampPrefabs(regionType);

            // Loop until all sidewalk positions have been processed
            while (processedSidewalks.Count < sidewalkPositions.Count)
            {
                // Step 1: Select an unprocessed random sidewalk position to start
                GridNode randomSidewalkNode = GetRandomUnprocessedSidewalkNode(sidewalkPositions, processedSidewalks);
                if (randomSidewalkNode == null)
                {
                    break; // No more valid unprocessed sidewalks
                }

                // Generate the first position along the edge of the sidewalk
                PlacingResult placingResult = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode);

                // Queue for active prop placements
                Queue<Vector3> activeList = new Queue<Vector3>();

                bool isStreetLamp = (propsCounter % streetLampFrequency == 0);  // Check if it's time to place a street lamp

                // Validate position based on the type of prop (street lamp or regular prop)
                if (isStreetLamp)
                {
                    if (IsValidPositionForStreetLamp(placingResult.pos, propPositions, streetLampPositions, sidewalkPositions, regionType))
                    {
                        activeList.Enqueue(placingResult.pos);
                        propPositions.Add(placingResult.pos);
                        streetLampPositions.Add(placingResult.pos);
                        propsCounter++;
                        PlacePropAtPosition(placingResult, randomSidewalkNode, streetLampPrefabs);  // Place street lamp
                    }
                }
                else
                {
                    if (IsValidPosition(placingResult.pos, propPositions, sidewalkPositions, regionType))
                    {
                        activeList.Enqueue(placingResult.pos);
                        propPositions.Add(placingResult.pos);
                        propsCounter++;
                        PlacePropAtPosition(placingResult, randomSidewalkNode, propPrefabs);  // Place regular prop
                    }
                }

                // Mark the sidewalk as processed
                processedSidewalks.Add(new Vector2Int(randomSidewalkNode.gridX, randomSidewalkNode.gridY));

                // Step 2: Loop through active list and attempt to place more props
                while (activeList.Count > 0)
                {
                    Vector3 currentPos = activeList.Dequeue();
                    bool propPlaced = false; // Track if a prop is placed in the current cycle

                    // Try to place new props around the current position
                    for (int i = 0; i < maxAttemptsPerPos && !propPlaced; i++)
                    {
                        placingResult = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode);

                        isStreetLamp = (propsCounter % streetLampFrequency == 0);

                        if (isStreetLamp)
                        {
                            if (IsValidPositionForStreetLamp(placingResult.pos, propPositions, streetLampPositions, sidewalkPositions, regionType))
                            {
                                activeList.Enqueue(placingResult.pos);
                                propPositions.Add(placingResult.pos);
                                streetLampPositions.Add(placingResult.pos);
                                propPlaced = true; // Successfully placed a street lamp
                                propsCounter++;
                                PlacePropAtPosition(placingResult, randomSidewalkNode, streetLampPrefabs);
                            }
                        }
                        else
                        {
                            if (IsValidPosition(placingResult.pos, propPositions, sidewalkPositions, regionType))
                            {
                                activeList.Enqueue(placingResult.pos);
                                propPositions.Add(placingResult.pos);
                                propPlaced = true; // Successfully placed a regular prop
                                propsCounter++;
                                PlacePropAtPosition(placingResult, randomSidewalkNode, propPrefabs);
                            }
                        }

                    }
                    
                }
            }
        }
        private GridNode GetRandomUnprocessedSidewalkNode(Dictionary<Vector2Int, GridNode> sidewalkPositions, HashSet<Vector2Int> processedSidewalks)
        {
            List<Vector2Int> unprocessedPositions = new List<Vector2Int>();

            // Collect all unprocessed sidewalk positions
            foreach (var sidewalk in sidewalkPositions)
            {
                if (!processedSidewalks.Contains(sidewalk.Key))
                {
                    unprocessedPositions.Add(sidewalk.Key);
                }
            }

            // If there are no unprocessed sidewalks, return null
            if (unprocessedPositions.Count == 0)
            {
                return null;
            }

            // Randomly select one of the unprocessed sidewalk positions
            Vector2Int randomPos = unprocessedPositions[Random.Range(0, unprocessedPositions.Count)];

            // Return the corresponding GridNode
            return sidewalkPositions[randomPos];
        }

        // Adjusted method to generate a random position on the edge of the sidewalk
        private PlacingResult GenerateRandomPositionAlongSidewalkEdge(GridNode node)
        {           
            // Determine the world position of the node and its boundaries
            Vector3 worldPos = node.worldPosition;

            Vector3 spawnPos = new Vector3(worldPos.x, worldPos.y, worldPos.z);
            PlacingResult result;
            var neighbours = Grid.Instance.GetNeighbourDirections(node, new List<Usage> { Usage.decoration });
            float firstOffsetDistance = nodeRadius * Random.Range(.6f, .8f);
            float secondOffsetDistance;
            if (neighbours.Count == 2)
            {                      
                if (AreDirectionsOpposite(neighbours))
                {
                    secondOffsetDistance = nodeRadius * Random.Range(.0f, .8f);
                    result = ApplyOffsets(spawnPos, GetOppositeAxisDirections(neighbours), neighbours, firstOffsetDistance, secondOffsetDistance);
                }
                else
                {
                    Direction firstDir = RoadPlacer.Instance.GetAllDirections()[Random.Range(0, 4)];
                    List<Direction> firstDirList = new List<Direction> { firstDir };
                    if (GetOppositeDirections(neighbours).Contains(firstDir))
                    {
                        secondOffsetDistance = nodeRadius * Random.Range(.0f, .8f);
                    }
                    else
                    {
                        // Restricted
                        secondOffsetDistance = nodeRadius * Random.Range(.6f, .8f);

                    }
                    result = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
                }
            }
            else if (neighbours.Count == 3)
            {
                // Only the non-neighbour dir is non-restricted on secondOffset
                secondOffsetDistance = nodeRadius * Random.Range(.0f, .8f);
                List<Direction> allDirs = RoadPlacer.Instance.GetAllDirections();
                Direction firstDir = allDirs[Random.Range(0, 4)];
                List<Direction> firstDirList = new List<Direction> { firstDir };
                Direction nonNeighbourDir = allDirs.Except(neighbours).First();
                if (nonNeighbourDir == firstDir)
                {
                    secondOffsetDistance = nodeRadius * Random.Range(.0f, .8f);
                }
                else
                {
                    // Restricted
                    secondOffsetDistance = nodeRadius * Random.Range(.6f, .8f);
                }
                result = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
            }
            else if (neighbours.Count == 4)
            {
                // All directions are restricted on secondOffset
                secondOffsetDistance = nodeRadius * Random.Range(.6f, .8f);
                Direction firstDir = RoadPlacer.Instance.GetAllDirections()[Random.Range(0, 4)];
                List<Direction> firstDirList = new List<Direction> { firstDir };
                result = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
            }
            else
            {
                SpawnSphere(spawnPos, Color.red, 5f);
                Debug.LogWarning("Cuantos neighbours tiene este desgraciado?¿");
                return new PlacingResult();
            }
            // Return the calculated position along the edge
            return result;
        }
        private PlacingResult ApplyOffsets(Vector3 pos, List<Direction> primaryDirections, List<Direction> secondaryDirections, float primaryDistance, float secondaryDistance)
        {
            Direction primaryDir = primaryDirections[Random.Range(0, primaryDirections.Count)];
            Direction secondaryDir = secondaryDirections[Random.Range(0, secondaryDirections.Count)];

            int[] primaryOffset = RoadPlacer.Instance.DirectionToInt(primaryDir);
            int[] secondaryOffset = RoadPlacer.Instance.DirectionToInt(secondaryDir);
            
            PlacingResult result = new PlacingResult(new Vector3(
                pos.x + (primaryOffset[0] * primaryDistance) + (secondaryOffset[0] * secondaryDistance),
                pos.y,
                pos.z + (primaryOffset[1] * primaryDistance) + (secondaryOffset[1] * secondaryDistance)
            ), primaryDir, secondaryDir);

            return result;
        }
        private bool AreDirectionsOpposite(List<Direction> directions)
        {
            return (directions.Contains(Direction.back) && directions.Contains(Direction.forward)) || (directions.Contains(Direction.left) && directions.Contains(Direction.right));
        }
        private List<Direction> GetOppositeDirections(List<Direction> directions)
        {
            List<Direction> oppositeDirections = new List<Direction>();

            directions.ForEach(x =>
            {
                switch (x)
                {
                    case Direction.left:
                        oppositeDirections.Add(Direction.right);
                        break;
                    case Direction.right:
                        oppositeDirections.Add(Direction.left);
                        break;
                    case Direction.forward:
                        oppositeDirections.Add(Direction.back);
                        break;
                    case Direction.back:
                        oppositeDirections.Add(Direction.forward);
                        break;
                    case Direction.zero:
                    default:
                        break;
                }
            });

            return oppositeDirections;
        }
        private List<Direction> GetOppositeAxisDirections(List<Direction> directions)
        {
            if (directions.Count == 1)
            {
                if (IsHorizontal(directions[0]))
                    return GetVerticalDirections();

                return GetHorizontalDirections();
            }

            // If vertical, return horizontal
            if (directions.Contains(Direction.back) && directions.Contains(Direction.forward))
                return GetHorizontalDirections();

            // If horizontal, return vertical
            if (directions.Contains(Direction.left) && directions.Contains(Direction.right))
                return GetVerticalDirections();

            return new List<Direction>();
        }
        private bool IsHorizontal(Direction direction)
        {
            return (direction == Direction.left) || (direction == Direction.right);
        }
        private List<Direction> GetHorizontalDirections()
        {
            return new List<Direction> { Direction.left, Direction.right };
        }
        private List<Direction> GetVerticalDirections()
        {
            return new List<Direction> { Direction.back, Direction.forward };
        }
        // Check if the new position is valid (not too close to other props, and it's on a sidewalk)
        private bool IsValidPosition(Vector3 newPos, List<Vector3> propPositions, Dictionary<Vector2Int, GridNode> sidewalkPositions, Region regionType)
        {
            // Ensure it's on a sidewalk
            GridNode belongingNode = Grid.Instance.GetClosestNodeToPosition(newPos);
            if (belongingNode == null || belongingNode.regionType != regionType || belongingNode.usage != Usage.decoration)
                return false;


            // Check if the new position is far enough from other props
            foreach (Vector3 propPos in propPositions)
            {
                if (Vector3.Distance(newPos, propPos) < minDistanceBetweenProps)
                    return false;
            }

            return true;
        }
        // Check if a street lamp can be placed at a position
        private bool IsValidPositionForStreetLamp(Vector3 newPos, List<Vector3> propPositions, List<Vector3> streetLampPositions, Dictionary<Vector2Int, GridNode> sidewalkPositions, Region regionType)
        {
            GridNode belongingNode = Grid.Instance.GetClosestNodeToPosition(newPos);
            if (belongingNode == null || belongingNode.regionType != regionType || belongingNode.usage != Usage.decoration)
                return false;

            // Check distance to other props (small distance allowed, e.g., 1f)
            foreach (Vector3 propPos in propPositions)
            {
                if (Vector3.Distance(newPos, propPos) < 1f)  // Small distance check for props
                    return false;
            }

            // Check distance to other street lamps (larger distance required, e.g., 5f)
            foreach (Vector3 streetLampPos in streetLampPositions)
            {
                if (Vector3.Distance(newPos, streetLampPos) < 5f)  // Larger distance check for street lamps
                    return false;
            }

            return true;
        }
        private int GetStreetLampFrequency(Region regionType)
        {
            // Define how often street lamps are placed based on the district
            switch (regionType)
            {
                case Region.Main:
                    return 2;
                case Region.Residential:
                    return 3;
                case Region.Suburbs:
                    return 4;
                default:
                    return 30;
            }
        }

        private Prop[] GetStreetLampPrefabs(Region regionType)
        {
            switch (regionType)
            {
                case Region.Main:
                    return mainStreetLamps;
                case Region.Residential:
                    return residentialStreetLamps;
                case Region.Suburbs:
                    return gangStreetLamps;
                default:
                    return null;
            }
        }

        // Place a prop at a given position
        private void PlacePropAtPosition(PlacingResult result, GridNode node, Prop[] propPrefabs)
        {
            Vector3 selectedPos = result.pos;
            Quaternion rotation = Quaternion.identity;
            const int maxAttempts = 5;
            int attempt = 0;

            // Track props already tried
            HashSet<Prop> triedProps = new HashSet<Prop>();
            List<Prop> alleyProps = propPrefabs.Where(x => x.propInfo.isAlley).ToList();
            Prop prop = null;

            while (attempt < maxAttempts) 
            {
                attempt++;

                // Select a random prop that hasn't been tried yet in this call
                do
                {
                    prop = propPrefabs[Random.Range(0, propPrefabs.Length)];
                } 
                while (triedProps.Contains(prop) && triedProps.Count < propPrefabs.Length);

                if (triedProps.Contains(prop))
                    break;
                
                // Add the selected prop to the tried set
                triedProps.Add(prop);

                // Check if the prop is meant to be spawned in an alley node
                if (node.isAlley)
                {
                    // If prop is alley, spawn it
                    if (prop.propInfo.isAlley)
                    {
                        SpawnPrefabBetweenBuildings(prop, result.firstDir, selectedPos);
                    }
                    else
                    {
                        // If prop is not alley, decide whether to spawn it                 
                        if(alleyProps.Count > 0 && Random.value < .15f)
                        {
                            // Get random alley prop
                            prop = alleyProps[Random.Range(0, alleyProps.Count - 1)];

                        }
                        SpawnPrefabBetweenBuildings(prop, result.firstDir, selectedPos);
                    }
                    break;
                }

                // Never allow an alley prefab to be spawned on normal nodes
                if (prop.propInfo.isAlley)
                {
                    // Try another prefab
                    if (!node.isAlley)
                        continue;

                    // Spawn prefab on alley
                    SpawnPrefabBetweenBuildings(prop, result.firstDir, selectedPos);
                    break;
                }
                // Get nodes to the side
                Vector3 firstDirVector = RoadPlacer.Instance.DirectionToVector(result.firstDir);
                Vector3 posInDirection = node.worldPosition + firstDirVector * Grid.Instance.nodeDiameter;
                Vector3 posInOppDirection = node.worldPosition - firstDirVector * Grid.Instance.nodeDiameter;
                GridNode nodeInDirection = Grid.Instance.GetClosestNodeToPosition(posInDirection);
                GridNode nodeInOppDirection = Grid.Instance.GetClosestNodeToPosition(posInOppDirection);
    
               
                if (nodeInDirection == null || nodeInOppDirection == null)
                {
                    Debug.LogWarning("Node in direction is null");
                    SpawnSphere(node.worldPosition + Vector3.up * 2f, Color.red, 2.5f);

                    // TODO: Spawnear asset si no es isExterior o isInterior :)
                    break;
                }
                else
                {
                    if (nodeInDirection.usage == Usage.building && nodeInOppDirection.usage == Usage.building)
                    {
                        if (prop.propInfo.requiresRoad) continue;

                        SpawnPrefabBetweenBuildings(prop, result.firstDir, selectedPos);
                        break;
                    }

                    if ((nodeInDirection.usage == Usage.building && nodeInOppDirection.usage == Usage.road) || (nodeInDirection.usage == Usage.road && nodeInOppDirection.usage == Usage.building))
                    {                      
                        // Find the node that the selectedPos is closer to
                        GridNode closerNode = Vector3.Distance(selectedPos, nodeInDirection.worldPosition) < Vector3.Distance(selectedPos, nodeInOppDirection.worldPosition) ? nodeInDirection : nodeInOppDirection;
                        if (closerNode.usage == Usage.road)
                        {
                            // El prefab se va a instanciar pegado a la carretera

                            // No instanciar props que requieran estar pegados a carretera (parada de bus)
                            if (prop.propInfo.requiresRoad && (closerNode.roadType == RoadType.Roundabout || closerNode.roadType == RoadType.Bridge)) continue;

                            // No instanciar prefab marcados como isInterior cerca de una road
                            if (prop.propInfo.isInterior) continue;

                            SpawnPrefabBetweenRoadAndBuilding(prop, result.firstDir, selectedPos, true);
                        }
                        else
                        {
                            // El prefab se va a instanciar alejado de la carretera

                            // No instanciar prefab marcados como isExterior cerca de un building
                            if (prop.propInfo.isExterior) continue;

                            SpawnPrefabBetweenRoadAndBuilding(prop, result.firstDir, selectedPos, false);
                        }
                        // Has been spawned
                        break;
                    }
                    else
                    {
                        // Road and road
                        // No spawning
                        break;
                    }
                }
               
            }
                
        }
        private void SpawnPrefabBetweenRoadAndBuilding(Prop prop, Direction firstDir, Vector3 selectedPos, bool isCloseToRoad)
        {
            Quaternion rotation = Quaternion.identity;
            if (isCloseToRoad)
            {
                if (prop.propInfo.looksToRoad)
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, 90f, 0f),
                        Direction.right => Quaternion.Euler(0f, -90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 180f, 0f),
                        Direction.back => Quaternion.Euler(0f, 0f, 0f),
                        _ => Quaternion.identity
                    };
                }
                else if (prop.propInfo.looksToSidewalk)
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, -90f, 0f),
                        Direction.right => Quaternion.Euler(0f, 90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 0f, 0f),
                        Direction.back => Quaternion.Euler(0f, 180f, 0f),
                        _ => Quaternion.identity
                    };
                }
                else
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, -90f, 0f),
                        Direction.right => Quaternion.Euler(0f, 90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 0f, 0f),
                        Direction.back => Quaternion.Euler(0f, 180f, 0f),
                        _ => Quaternion.identity
                    };
                }
            }
            else
            {
                if (prop.propInfo.looksToRoad)
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, 90f, 0f),
                        Direction.right => Quaternion.Euler(0f, -90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 180f, 0f),
                        Direction.back => Quaternion.Euler(0f, 0f, 0f),
                        _ => Quaternion.identity
                    };
                }
                else if (prop.propInfo.looksToSidewalk)
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, -90f, 0f),
                        Direction.right => Quaternion.Euler(0f, 90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 0f, 0f),
                        Direction.back => Quaternion.Euler(0f, 180f, 0f),
                        _ => Quaternion.identity
                    };
                }
                else
                {
                    rotation = firstDir switch
                    {
                        Direction.left => Quaternion.Euler(0f, -90f, 0f),
                        Direction.right => Quaternion.Euler(0f, 90f, 0f),
                        Direction.forward => Quaternion.Euler(0f, 0f, 0f),
                        Direction.back => Quaternion.Euler(0f, 180f, 0f),
                        _ => Quaternion.identity
                    };
                }
            }
            Instantiate(prop, selectedPos, rotation);
        }
        private void SpawnPrefabBetweenBuildings(Prop prop, Direction firstDir, Vector3 selectedPos)
        {
            // Ignorar todas las variables, siempre tiene que mirar hacia el sidewalk

            // Rotate depending on direction
            Quaternion rotation = firstDir switch
            {
                Direction.left => Quaternion.Euler(0f, -90f, 0f),
                Direction.right => Quaternion.Euler(0f, 90f, 0f),
                Direction.forward => Quaternion.Euler(0f, 0f, 0f),
                Direction.back => Quaternion.Euler(0f, 180f, 0f),
                _ => Quaternion.identity
            };
            Instantiate(prop, selectedPos, rotation);
        }
        private void SpawnSphere(Vector3 pos, Color color, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        struct PlacingResult
        {
            public Vector3 pos;
            public Direction firstDir;
            public Direction secondDir;

            public PlacingResult(Vector3 pos, Direction firstDir, Direction secondDir)
            {
                this.pos = pos;
                this.firstDir = firstDir;
                this.secondDir = secondDir;
            }

        }
    }
}
