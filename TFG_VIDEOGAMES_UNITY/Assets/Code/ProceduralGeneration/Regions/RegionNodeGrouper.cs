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
        public List<List<GridNode>> GroupConnectedNodes(List<GridNode> allNodes)
        {
            // This will hold all the groups of connected nodes
            List<List<GridNode>> groupedNodes = new List<List<GridNode>>();

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

                // Set region for the group
                SetRegionTypeForGroup(currentGroup);

                // Add the group of connected nodes to the result
                groupedNodes.Add(currentGroup);
            }

            return groupedNodes;
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
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
    }
}

