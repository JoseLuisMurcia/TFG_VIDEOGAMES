using PG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class PropPlacer : MonoBehaviour
    {
        private float minDistanceBetweenProps = 6f;
        private int maxAttemptsPerPos = 15;
        private float nodeRadius;

        [SerializeField] private GameObject[] mainProps;
        [SerializeField] private GameObject[] residentialProps;
        [SerializeField] private GameObject[] gangProps;

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
        private void PlaceDistrictProps(Dictionary<Vector2Int, GridNode> sidewalkPositions, GameObject[] propPrefabs, Region regionType)
        {
            nodeRadius = Grid.Instance.nodeRadius;

            List<Vector3> propPositions = new List<Vector3>();  // Already placed prop positions
            HashSet<Vector2Int> processedSidewalks = new HashSet<Vector2Int>();  // Track processed sidewalks

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
                Vector3 startPos = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode);

                // Queue for active prop placements
                Queue<Vector3> activeList = new Queue<Vector3>();
                if (IsValidPosition(startPos, propPositions, sidewalkPositions, regionType))
                {
                    activeList.Enqueue(startPos);
                    propPositions.Add(startPos);
                    PlacePropAtPosition(startPos, propPrefabs);
                }

                // Mark the sidewalk as processed
                processedSidewalks.Add(new Vector2Int(randomSidewalkNode.gridX, randomSidewalkNode.gridY));

                // Step 2: Loop through active list and attempt to place more props
                while (activeList.Count > 0)
                {
                    Vector3 currentPos = activeList.Dequeue();

                    // Try to place new props around the current position
                    for (int i = 0; i < maxAttemptsPerPos; i++)
                    {
                        Vector3 newPos = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode);

                        // Check if the new position is valid (sidewalk only, no props too close)
                        if (IsValidPosition(newPos, propPositions, sidewalkPositions, regionType))
                        {
                            activeList.Enqueue(newPos);
                            propPositions.Add(newPos);
                            processedSidewalks.Add(new Vector2Int(randomSidewalkNode.gridX, randomSidewalkNode.gridY));
                            PlacePropAtPosition(newPos, propPrefabs);
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

        // Helper function to convert a Vector3 world position to its corresponding grid position
        private Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            GridNode node = Grid.Instance.GetClosestNodeToPosition(worldPosition);
            return new Vector2Int(node.gridX, node.gridY);
        }
        // Adjusted method to generate a random position on the edge of the sidewalk
        private Vector3 GenerateRandomPositionAlongSidewalkEdge(GridNode node)
        {           
            // Determine the world position of the node and its boundaries
            Vector3 worldPos = node.worldPosition;

            Vector3 spawnPos = new Vector3(worldPos.x, worldPos.y, worldPos.z);
            var neighbours = Grid.Instance.GetNeighbourDirections(node, new List<Usage> { Usage.decoration });
            float firstOffsetDistance = nodeRadius * Random.Range(.6f, .8f);
            float secondOffsetDistance;
            if (neighbours.Count == 2)
            {                      
                if (AreDirectionsOpposite(neighbours))
                {
                    secondOffsetDistance = nodeRadius * Random.Range(.0f, .8f);
                    spawnPos = ApplyOffsets(spawnPos, GetOppositeAxisDirections(neighbours), neighbours, firstOffsetDistance, secondOffsetDistance);
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
                    spawnPos = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
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
                spawnPos = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
            }
            else if (neighbours.Count == 4)
            {
                // All directions are restricted on secondOffset
                secondOffsetDistance = nodeRadius * Random.Range(.6f, .8f);
                Direction firstDir = RoadPlacer.Instance.GetAllDirections()[Random.Range(0, 4)];
                List<Direction> firstDirList = new List<Direction> { firstDir };
                spawnPos = ApplyOffsets(spawnPos, firstDirList, GetOppositeAxisDirections(firstDirList), firstOffsetDistance, secondOffsetDistance);
            }

            // Return the calculated position along the edge
            return spawnPos;
        }
        private Vector3 ApplyOffsets(Vector3 pos, List<Direction> primaryDirections, List<Direction> secondaryDirections, float primaryDistance, float secondaryDistance)
        {
            Direction primaryDir = primaryDirections[Random.Range(0, primaryDirections.Count)];
            Direction secondaryDir = secondaryDirections[Random.Range(0, secondaryDirections.Count)];

            int[] primaryOffset = RoadPlacer.Instance.DirectionToInt(primaryDir);
            int[] secondaryOffset = RoadPlacer.Instance.DirectionToInt(secondaryDir);

            return new Vector3(
                pos.x + (primaryOffset[0] * primaryDistance) + (secondaryOffset[0] * secondaryDistance),
                pos.y,
                pos.z + (primaryOffset[1] * primaryDistance) + (secondaryOffset[1] * secondaryDistance)
            );
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
            if (belongingNode == null)
                return false;

            // Check if it is a decoration node
            if (belongingNode.regionType != regionType || belongingNode.usage != Usage.decoration)
                return false;

            // Check if the new position is far enough from other props
            foreach (Vector3 propPos in propPositions)
            {
                if (Vector3.Distance(newPos, propPos) < minDistanceBetweenProps)
                    return false;
            }

            return true;
        }

        // Place a prop at a given position
        private void PlacePropAtPosition(Vector3 pos, GameObject[] propPrefabs)
        {
            // Randomly select a prop prefab for variety
            GameObject propPrefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
            Vector3 spawnPos = new Vector3(pos.x, pos.y, pos.z);

            // Instantiate the prop at the given position
            Instantiate(propPrefab, spawnPos, Quaternion.identity);
        }

        // Get a random position from the list of valid sidewalk positions
        private Vector3 GetRandomSidewalkPosition(Dictionary<Vector2Int, GridNode> sidewalkPositions)
        {
            int randomIndex = Random.Range(0, sidewalkPositions.Count);
            foreach (var sidewalk in sidewalkPositions)
            {
                if (randomIndex-- == 0)
                {
                    return sidewalk.Value.worldPosition;
                }
            }
            return Vector3.zero;
        }
        // Get an unprocessed random position from the list of valid sidewalk positions
        private Vector3 GetRandomUnprocessedSidewalkPosition(Dictionary<Vector2Int, GridNode> sidewalkPositions, HashSet<Vector2Int> processedSidewalks)
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

            // If there are no unprocessed sidewalks, return zero vector
            if (unprocessedPositions.Count == 0)
            {
                return Vector3.zero;
            }

            // Randomly select one of the unprocessed sidewalk positions
            Vector2Int randomPos = unprocessedPositions[Random.Range(0, unprocessedPositions.Count)];
            return sidewalkPositions[randomPos].worldPosition;
        }
    }
}
