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
                if(currentGroup.First().regionType == Region.Residential) DivideAndMarkGroup(currentGroup, 4, 4);
            }
        }
        private void DFS(GridNode node, List<GridNode> allNodes, HashSet<GridNode> visited, List<GridNode> currentGroup)
        {
            // Mark the node as visited
            visited.Add(node);
            currentGroup.Add(node);

            // Get the neighboring nodes
            List<GridNode> neighbors = GetNeighbors(node, allNodes);

            // Explore each neighbor
            foreach (GridNode neighbor in neighbors)
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
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),  // Right
                new Vector2Int(-1, 0), // Left
                new Vector2Int(0, 1),  // Up
                new Vector2Int(0, -1),  // Down
                new Vector2Int(1, -1),  // Up + Left
                new Vector2Int(1, 1),  // Up + Right
                new Vector2Int(-1, -1),  // Down + Left
                new Vector2Int(-1, 1)  // Down + Right
            };

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
            //debugColor = colorList[colorIndex];
            //colorIndex = (colorIndex + 1) % colorList.Count;

            // Count each region type in the group
            foreach (GridNode node in group)
            {
                regionCounts[node.regionType]++;
                //Color debugColor;
                //switch (node.regionType)
                //{
                //    case Region.Main:
                //        debugColor = Color.cyan;
                //        break;
                //    case Region.Residential:
                //        debugColor = Color.green;
                //        break;
                //    case Region.Suburbs:
                //    default:
                //        debugColor = Color.red;
                //        break;
                //}
                //SpawnSphere(node.worldPosition, debugColor, 2f, 1.5f);
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
                // Split vertically
                int splitX = minX + width / 2; // split in the middle
                MarkVerticalDivision(splitX, minY, maxY, group);

                // Divide each half recursively
                List<GridNode> leftHalf = group.Where(n => n.gridX < splitX).ToList();
                List<GridNode> rightHalf = group.Where(n => n.gridX >= splitX).ToList();
                DivideAndMarkGroup(leftHalf, minWidth, minHeight);
                DivideAndMarkGroup(rightHalf, minWidth, minHeight);
            }
            else if (height > minHeight)
            {
                // Split horizontally
                int splitY = minY + height / 2; // split in the middle
                MarkHorizontalDivision(splitY, minX, maxX, group);

                // Divide each half recursively
                List<GridNode> topHalf = group.Where(n => n.gridY >= splitY).ToList();
                List<GridNode> bottomHalf = group.Where(n => n.gridY < splitY).ToList();
                DivideAndMarkGroup(topHalf, minWidth, minHeight);
                DivideAndMarkGroup(bottomHalf, minWidth, minHeight);
            }
        }

        private void MarkVerticalDivision(int splitX, int minY, int maxY, List<GridNode> group)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GridNode node = group.FirstOrDefault(n => n.gridX == splitX && n.gridY == y);
                if (node != null)
                {
                    node.usage = Usage.decoration;  // Mark the node as a sidewalk
                }
            }
        }

        private void MarkHorizontalDivision(int splitY, int minX, int maxX, List<GridNode> group)
        {
            for (int x = minX; x <= maxX; x++)
            {
                GridNode node = group.FirstOrDefault(n => n.gridX == x && n.gridY == splitY);
                if (node != null)
                {
                    node.usage = Usage.decoration;  // Mark the node as a sidewalk
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
                    bool nodeMeetsRoad = buildingPlacer.AdvanceUntilRoad(direction, node.gridX, node.gridY);
                    if (!nodeMeetsRoad)
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

