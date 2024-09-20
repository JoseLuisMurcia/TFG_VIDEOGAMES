using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace PG
{
    public class RegionNodeGrouper
    {
        private List<Color> colorList = new List<Color> { Color.red, Color.blue, Color.cyan, Color.gray, Color.black, Color.white, Color.magenta, Color.green, Color.yellow };
        private int colorIndex = 0;
        private BuildingPlacer buildingPlacer;
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
            this.buildingPlacer = buildingPlacer;
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
                if (!IsInteriorGroup(currentGroup)) continue;

                // Set region for the group
                SetRegionTypeForGroup(currentGroup);

                // Divide the groupedRegion to mark new decoration nodes in between if possible
                int minWidth;
                int minHeight;
                switch (currentGroup.First().regionType)
                {
                    case Region.Main:
                        minWidth = UnityEngine.Random.Range(7, 8);
                        minHeight = UnityEngine.Random.Range(7, 8);
                        break;
                    case Region.Residential:
                        minWidth = UnityEngine.Random.Range(4, 7);
                        minHeight = UnityEngine.Random.Range(4, 7);
                        break;
                    case Region.Suburbs:
                    default:
                        minWidth = UnityEngine.Random.Range(6, 8);
                        minHeight = UnityEngine.Random.Range(6, 8);
                        break;
                }
                currentGroup.RemoveAll(x => x.usage == Usage.decoration);
                if (currentGroup.Count > 0)
                {
                    IsConcaveShape(currentGroup);
                    DivideAndMarkGroup(currentGroup, minWidth, minHeight, true);
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

        public void DivideAndMarkGroup(List<GridNode> group, int minWidth, int minHeight, bool firstIteration = false)
        {
            if (!firstIteration && ShouldSkipDivision()) return;

            // Find the bounding box of the group of nodes
            int minX = group.Min(n => n.gridX);
            int maxX = group.Max(n => n.gridX);
            int minY = group.Min(n => n.gridY);
            int maxY = group.Max(n => n.gridY);

            // Calculate width and height of the group
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Introduce variation in minWidth and minHeight
            minWidth = VaryMinDimension(minWidth);
            minHeight = VaryMinDimension(minHeight);

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
                    DivideAndMarkGroup(group.Where(n => n.gridX > splitX).ToList(), minWidth, minHeight);
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
                    DivideAndMarkGroup(group.Where(n => n.gridY > splitY).ToList(), minWidth, minHeight);
                    DivideAndMarkGroup(group.Where(n => n.gridY < splitY).ToList(), minWidth, minHeight);
                }
            }
        }
        // Helper method: Adds variation to the min dimension (width or height)
        private int VaryMinDimension(int originalMin)
        {
            int variation = UnityEngine.Random.Range(-1, 2); // Vary by -1, 0, or +1
            return Math.Max(2, originalMin + variation); // Ensure the minimum is at least 2
        }

        // Helper method: Random chance to skip dividing the group
        private bool ShouldSkipDivision()
        {
            return UnityEngine.Random.value < 0.1f; // 10% chance to skip division
        }
        private bool CanMarkDivision(int split, int min, int max, List<GridNode> group, int minDistance, bool isVertical)
        {
            minDistance = 2;
            for (int i = min; i <= max; i++)
            {
                for (int offset = -minDistance; offset <= minDistance; offset++)
                {
                    int posX = isVertical ? split + offset : i;
                    int posY = isVertical ? i : split + offset;
                    GridNode nearbyNode = Grid.Instance.nodesGrid[posX, posY];

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
                    node.usage = Usage.decoration;
                    node.isAlley = true; ;
                }
            }
        }
        private bool IsConcaveShape(List<GridNode> group)
        {
            // Get boundary nodes of the group
            List<GridNode> boundaryNodes = GetBoundaryNodes(group);

            // Use the cross product to determine concavity.
            // For each boundary node, compare the angle between neighboring edges.
            for (int i = 0; i < boundaryNodes.Count; i++)
            {
                GridNode prevNode = boundaryNodes[(i - 1 + boundaryNodes.Count) % boundaryNodes.Count];
                GridNode currNode = boundaryNodes[i];
                GridNode nextNode = boundaryNodes[(i + 1) % boundaryNodes.Count];
                Vector2 prev = new Vector2(prevNode.gridX, prevNode.gridY);
                Vector2 curr = new Vector2(currNode.gridX, currNode.gridY);
                Vector2 next = new Vector2(nextNode.gridX, nextNode.gridY);

                // Calculate the cross product of vectors (prev -> curr) and (curr -> next)
                Vector2 dir1 = curr - prev;
                Vector2 dir2 = next - curr;

                float crossProduct = dir1.x * dir2.y - dir1.y * dir2.x;

                // If the cross product is negative, we found a concave corner
                if (crossProduct < 0)
                {
                    SpawnSphere(prevNode.worldPosition, Color.red, 2f, 2f);
                    SpawnSphere(currNode.worldPosition, Color.cyan, 2f, 2f);
                    SpawnSphere(nextNode.worldPosition, Color.yellow, 2f, 2f);
                    return true; // Shape is concave
                }
            }

            return false; // Shape is convex
        }

        private List<GridNode> GetBoundaryNodes(List<GridNode> group)
        {
            List<GridNode> boundaryNodes = new List<GridNode>();
            HashSet<GridNode> groupSet = new HashSet<GridNode>(group); // For fast lookup

            foreach (GridNode node in group)
            {
                // Check if the node has any neighbor that is not part of the group
                List<GridNode> neighbors = Grid.Instance.GetNeighbours(node, new List<Usage>() { Usage.decoration }); // You will need to implement GetNeighbors()

                foreach (GridNode neighbor in neighbors)
                {
                    if (!groupSet.Contains(neighbor))
                    {
                        // This node is on the boundary                      
                        boundaryNodes.Add(node);
                        break;
                    }
                }
            }
            // I need to sort them as if I was looping the boundaries, advancing in the proper direction each time

            // Sort the boundary nodes first by GridX, then by GridY
            return boundaryNodes
                .OrderBy(node => node.gridX)    // First, order by GridX
                .ThenBy(node => node.gridY)     // Then, by GridY
                .ToList();                      // Return as a sorted list
        }
        private List<GridNode> SortBoundaryNodes(List<GridNode> group)
        {
            List<GridNode> sortedGroup = new List<GridNode>();
            GridNode node = sortedGroup.Find(x => Grid.Instance.GetNeighbours(x, new List<Usage>() { Usage.building }).Count == 2);           
            // Definir qué hacer cuando el nodo tenga 1 vecino, 2 vecinos, 3 vecinos

            // Poner variable a false cuando para un nodo, no queden movimientos disponibles que no impliquen visitar nodos contenidos en addedNodes
            bool hasNodesLeft = true;
            HashSet<GridNode> addedNodes = new HashSet<GridNode>();

            while (hasNodesLeft)
            {
                List<Direction> directionsAvailable = buildingPlacer.GetUsageNeighbours(node.gridX, node.gridY, new List<Usage>() { Usage.decoration });
                // El caso base sería tener 2 directions disponibles

                // Caso alerta es cuando hay 3 directions, ahí la direccion a tomar puede ser decisiva sobre la precision del resultado.
                // Comprobar los nodos vecinos cuando haya 3 directions, uno debería estar ya añadido (el previo) de los 2 restantes, elegir el que implique cambio de direccion. Siempre comprobando que no lleve a callejon sin salida

                // Metodo para una vez elegida una direccion caso 3 directions. Avanzar recto hasta llegar a ultimo nodo antes de decoration, ahí comprobar si es callejon sin salida

                // Una vez tenemos 3 vecinos, puede ser que aun girando, cerremos el circulo y cerremos un camino de 1 que era válido, ahi es donde tenemos que devolver la seleccion actual, creando un nuevo grupo, luego se deberá procesar el resto
            }

            // En caso de estar en 1 vecino, que hacemos? No se puede ordenar ya que hay que volver atrás.

            // Para 2 vecinos, avanzar con la direccion actual

            // Para 3 probablemente haya que girar


            return sortedGroup;
        }
        private bool IsInteriorGroup(List<GridNode> group)
        {
            foreach (GridNode node in group)
            {
                if (node.usage == Usage.decoration) continue;

                // Check directions for building nodes
                List<Direction> neighbours = buildingPlacer.GetUsageNeighbours(node.gridX, node.gridY, new List<Usage>() { Usage.empty, Usage.building, Usage.decoration }); ;

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

