using PG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class PropPlacer : MonoBehaviour
    {
        private float minDistanceBetweenProps = 6f;
        private int maxAttemptsPerPos = 15;

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
            List<Vector3> propPositions = new List<Vector3>();  // Already placed prop positions
            HashSet<Vector2Int> processedSidewalks = new HashSet<Vector2Int>();  // Track processed sidewalks
            float edgeOffset = 0.5f;  // Adjust this value as needed to control how far from the center props are placed

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
                Vector3 startPos = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode, edgeOffset);

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
                        Vector3 newPos = GenerateRandomPositionAlongSidewalkEdge(randomSidewalkNode, edgeOffset);

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

        //private void PlaceDistrictProps(Dictionary<Vector2Int, GridNode> sidewalkPositions, GameObject[] propPrefabs, Region regionType)
        //{
        //    List<Vector3> propPositions = new List<Vector3>();  // Already placed prop positions
        //    HashSet<Vector2Int> processedSidewalks = new HashSet<Vector2Int>();  // Track processed sidewalks

        //    // Loop until all sidewalk positions have been processed
        //    while (processedSidewalks.Count < sidewalkPositions.Count)
        //    {
        //        // Step 1: Select an unprocessed random sidewalk position to start
        //        Vector3 startPos = GetRandomUnprocessedSidewalkPosition(sidewalkPositions, processedSidewalks);
        //        if (startPos == Vector3.zero)
        //        {
        //            break; // No more valid unprocessed sidewalks
        //        }

        //        // Queue for active prop placements
        //        Queue<Vector3> activeList = new Queue<Vector3>();
        //        if (IsValidPosition(startPos, propPositions, sidewalkPositions, regionType))
        //        {
        //            activeList.Enqueue(startPos);
        //            propPositions.Add(startPos);                 
        //            PlacePropAtPosition(startPos, propPrefabs);
        //        }
        //        processedSidewalks.Add(GetGridPosition(startPos));  // Mark as processed

        //        // Step 2: Loop through active list and attempt to place more props
        //        while (activeList.Count > 0)
        //        {
        //            Vector3 currentPos = activeList.Dequeue();

        //            // Try to place new props around the current position
        //            for (int i = 0; i < maxAttemptsPerPos; i++)
        //            {
        //                Vector3 newPos = GenerateRandomPositionAlongSidewalkEdge(currentPos, minDistanceBetweenProps);

        //                // Check if the new position is valid (sidewalk only, no props too close)
        //                if (IsValidPosition(newPos, propPositions, sidewalkPositions, regionType))
        //                {
        //                    activeList.Enqueue(newPos);
        //                    propPositions.Add(newPos);
        //                    processedSidewalks.Add(GetGridPosition(newPos));  // Mark as processed
        //                    PlacePropAtPosition(newPos, propPrefabs);
        //                }
        //            }
        //        }
        //    }
        //}
        // Helper function to convert a Vector3 world position to its corresponding grid position
        private Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            GridNode node = Grid.Instance.GetClosestNodeToPosition(worldPosition);
            return new Vector2Int(node.gridX, node.gridY);
        }
        // Adjusted method to generate a random position on the edge of the sidewalk
        private Vector3 GenerateRandomPositionAlongSidewalkEdge(GridNode node, float edgeOffset)
        {
            float nodeDiameter = Grid.Instance.nodeDiameter;
            // Determine the world position of the node and its boundaries
            Vector3 worldPos = node.worldPosition;

            // Calculate the size of the node (this can vary based on how big a sidewalk grid cell is)
            float nodeHalfSizeX = nodeDiameter / 2f;
            float nodeHalfSizeY = nodeDiameter / 2f;

            // Randomly select one of the 4 edges of the node (left, right, top, bottom)
            int edge = Random.Range(0, 4);

            Vector3 spawnPos = new Vector3(worldPos.x, worldPos.y, worldPos.z);
            var neighbours = Grid.Instance.GetNeighbourDirections(node, new List<Usage> { Usage.decoration });
            if (neighbours.Count == 2)
            {
                float firstOffsetDistance = nodeDiameter * .6f;
                float secondOffsetDistance = nodeDiameter * .6f;
                if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right))
                {
                    // First move node radius distance to forward/back and then left/right
                    List<Direction> firstOffsetDirections = new List<Direction> { Direction.back, Direction.forward };
                    List<Direction> secondOffsetDirections = new List<Direction> { Direction.left, Direction.right };
                    Direction firstOffsetDirection = firstOffsetDirections[Random.Range(0, 2)];
                    Direction secondOffsetDirection = secondOffsetDirections[Random.Range(0, 2)];

                    int[] firstOffset = RoadPlacer.Instance.DirectionToInt(firstOffsetDirection);
                    int[] secondOffset = RoadPlacer.Instance.DirectionToInt(secondOffsetDirection);
                    spawnPos = new Vector3(spawnPos.x + (firstOffset[0] * firstOffsetDistance), spawnPos.y, spawnPos.z + (firstOffset[1] * firstOffsetDistance));
                }
                else if (neighbours.Contains(Direction.back) && neighbours.Contains(Direction.forward))
                {
                    // First move node radius distance to left/right and then back/forward
                    List<Direction> firstOffsetDirections = new List<Direction> { Direction.left, Direction.right };
                    List<Direction> secondOffsetDirections = new List<Direction> { Direction.back, Direction.forward };
                    Direction firstOffsetDirection = firstOffsetDirections[Random.Range(0, 2)];
                    Direction secondOffsetDirection = secondOffsetDirections[Random.Range(0, 2)];

                    int[] firstOffset = RoadPlacer.Instance.DirectionToInt(firstOffsetDirection);
                    int[] secondOffset = RoadPlacer.Instance.DirectionToInt(secondOffsetDirection);
                    spawnPos = new Vector3(spawnPos.x + (firstOffset[0] * firstOffsetDistance), spawnPos.y, spawnPos.z + (firstOffset[1] * firstOffsetDistance));
                }
            }
            else
            {
                switch (edge)
                {
                    case 0: // Left edge
                        spawnPos.x -= nodeHalfSizeX - edgeOffset;
                        break;
                    case 1: // Right edge
                        spawnPos.x += nodeHalfSizeX - edgeOffset;
                        break;
                    case 2: // Top edge
                        spawnPos.z += nodeHalfSizeY - edgeOffset;
                        break;
                    case 3: // Bottom edge
                        spawnPos.z -= nodeHalfSizeY - edgeOffset;
                        break;
                }
            }

            // Return the calculated position along the edge
            return spawnPos;
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
