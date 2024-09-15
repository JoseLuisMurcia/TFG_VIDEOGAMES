using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class RegionNodeGrouper
    {
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
                if (!visited.Contains(neighbor) && neighbor.usage == Usage.building)
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
            new Vector2Int(0, -1)  // Down
            };

            Vector2Int nodePos = new Vector2Int(node.gridX, node.gridY);
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = nodePos + dir;

                // Check if the neighbor exists in the list of nodes
                GridNode neighbor = allNodes.Find(n => n.gridX == neighborPos.x && n.gridY == neighborPos.y);
                if (neighbor != null && neighbor.usage == Usage.building)
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }
    }
}

