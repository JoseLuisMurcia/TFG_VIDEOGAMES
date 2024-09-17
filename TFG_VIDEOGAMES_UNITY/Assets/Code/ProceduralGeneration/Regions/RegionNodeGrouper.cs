using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class RegionNodeGrouper
    {
        private List<Color> colorList = new List<Color> { Color.red, Color.blue, Color.cyan, Color.gray, Color.black, Color.white, Color.magenta, Color.green, Color.yellow };
        private int colorIndex = 0;
        private static readonly Vector2Int[] directions = new Vector2Int[] // Reuse directions globally
        {
            new Vector2Int(1, 0),  // Right
            new Vector2Int(-1, 0), // Left
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(1, -1),  // Up + Left
            new Vector2Int(1, 1),  // Up + Right
            new Vector2Int(-1, -1),  // Down + Left
            new Vector2Int(-1, 1)  // Down + Right
        };
        public void GroupConnectedNodes(List<GridNode> allNodes, BuildingPlacer buildingPlacer)
        {
            // Keep track of visited nodes
            HashSet<GridNode> visited = new HashSet<GridNode>();

            // Traverse all nodes
            foreach (GridNode node in allNodes)
            {
                // If the node is already visited, skip it
                if (visited.Contains(node))
                    continue;

                // Otherwise, perform DFS/BFS from this node and collect all connected nodes
                List<GridNode> currentGroup = new List<GridNode>();
                DFS(node, allNodes, visited, currentGroup);

                // Check if the group is not on open to the outside
                if (!IsInteriorGroup(currentGroup, buildingPlacer)) continue;

                // Set region for the group
                SetRegionTypeForGroup(currentGroup);

                // Divide the groupedRegion to mark new decoration nodes in between if possible
                if (currentGroup.First().regionType == Region.Residential) 
                {
                    currentGroup.RemoveAll(x => x.usage == Usage.decoration);
                    if (currentGroup.Count > 0) 
                        DivideAndMarkGroup(currentGroup, 4, 4);
                }
            }
        }
        private void DFS(GridNode node, List<GridNode> allNodes, HashSet<GridNode> visited, List<GridNode> currentGroup)
        {
            // Mark the node as visited
            visited.Add(node);
            currentGroup.Add(node);

            // Explore each neighbor
            foreach (GridNode neighbor in GetNeighbors(node, allNodes))
            {
                // Only process building nodes and unvisited nodes
                if (!visited.Contains(neighbor) && (neighbor.usage == Usage.building || neighbor.usage == Usage.decoration))
                {
                    DFS(neighbor, allNodes, visited, currentGroup);
                }
            }
        }
        private List<GridNode> GetNeighbors(GridNode node, List<GridNode> allNodes)
        {
            List<GridNode> neighbors = new List<GridNode>();
            Vector2Int nodePos = new Vector2Int(node.gridX, node.gridY);

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = nodePos + dir;
                // Check if the neighbor exists in the list of nodes
                GridNode neighbor = allNodes.Find(n => n.gridX == neighborPos.x && n.gridY == neighborPos.y);
                if (neighbor != null && (neighbor.usage == Usage.building || neighbor.usage == Usage.decoration))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }
        private void SetRegionTypeForGroup(List<GridNode> group)
        {
            // Initialize a dictionary to count the occurrences of each region type.
            Dictionary<Region, int> regionCounts = new Dictionary<Region, int>()
            {
                { Region.Main, 0 },
                { Region.Residential, 0 },
                { Region.Suburbs, 0 }
            };

            // Count each region type in the group
            foreach (GridNode node in group)
            {
                regionCounts[node.regionType]++;
            }

            // Find the region with the maximum count
            Region primaryRegion = regionCounts.Aggregate((maxRegion, currentRegion) =>
                currentRegion.Value > maxRegion.Value ? currentRegion : maxRegion).Key;

            // Assign the primaryRegion to all nodes in the group
            group.ForEach(x => x.regionType = primaryRegion);
        }

        public void DivideAndMarkGroup(List<GridNode> group, int minWidth, int minHeight)
        {
            // Find the bounding box of the group of nodes
            int minX = group.Min(n => n.gridX);
            int maxX = group.Max(n => n.gridX);
            int minY = group.Min(n => n.gridY);
            int maxY = group.Max(n => n.gridY);

            // Calculate width and height of the group
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // If the group size is already smaller than the minimum, we can't split it anymore
            if (width <= minWidth && height <= minHeight)
                return;

            // Decide how to split: horizontal or vertical
            if (width > minWidth)
            {
                int splitX = minX + width / 2;

                // Only continue subdivision if we can mark a valid vertical division
                if (CanMarkDivision(splitX, minY, maxY, group, minWidth, true))
                {
                    // Mark the division
                    MarkDivision(splitX, minY, maxY, group, true);

                    // Subdivide only after marking the division
                    DivideAndMarkGroup(group.Where(n => n.gridX < splitX).ToList(), minWidth, minHeight);
                    DivideAndMarkGroup(group.Where(n => n.gridX >= splitX).ToList(), minWidth, minHeight);
                }
            }
            else if (height > minHeight)
            {
                int splitY = minY + height / 2;

                // Only continue subdivision if we can mark a valid horizontal division
                if (CanMarkDivision(splitY, minX, maxX, group, minHeight, false))
                {
                    // Mark the division
                    MarkDivision(splitY, minX, maxX, group, false);

                    // Subdivide only after marking the division
                    DivideAndMarkGroup(group.Where(n => n.gridY >= splitY).ToList(), minWidth, minHeight);
                    DivideAndMarkGroup(group.Where(n => n.gridY < splitY).ToList(), minWidth, minHeight);
                }
            }
        }

        private bool CanMarkDivision(int split, int min, int max, List<GridNode> group, int minDistance, bool isVertical)
        {
            for (int i = min; i <= max; i++)
            {
                for (int offset = -minDistance; offset <= minDistance; offset++)
                {
                    GridNode nearbyNode = group.FirstOrDefault(n =>
                        (isVertical ? n.gridX == split + offset && n.gridY == i : n.gridY == split + offset && n.gridX == i));
                    if (nearbyNode != null && nearbyNode.usage == Usage.decoration)
                        return false;
                }
            }
            return true;
        }
        private void MarkDivision(int split, int min, int max, List<GridNode> group, bool isVertical)
        {
            Color debugColor = colorList[colorIndex];
            colorIndex = (colorIndex + 1) % colorList.Count;

            for (int i = min; i <= max; i++)
            {
                GridNode node = group.FirstOrDefault(n => isVertical ? n.gridX == split && n.gridY == i : n.gridY == split && n.gridX == i);
                if (node != null)
                {
                    SpawnSphere(node.worldPosition, debugColor, 2f, 1.5f);
                    node.usage = Usage.decoration;
                }
            }
        }
        private bool IsInteriorGroup(List<GridNode> group, BuildingPlacer buildingPlacer)
        {
            foreach (GridNode node in group)
            {
                if (node.usage == Usage.decoration) continue;

                // Check directions for building nodes
                List<Direction> neighbours = buildingPlacer.GetNeighboursData(node.gridX, node.gridY).neighbours;

                if (neighbours.Count < 4)
                    return false;

                // Explore all the neighbours that are empty
                foreach (Direction direction in neighbours)
                {
                    // See if they are between roads (should spawn building)
                    if (!buildingPlacer.AdvanceUntilRoad(direction, node.gridX, node.gridY))
                    {
                        return false;
                    }
                }
            }
            return true;
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

