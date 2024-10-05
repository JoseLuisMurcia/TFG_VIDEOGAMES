using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class RegionNodeGrouper
    {
        private List<Color> colorList = new List<Color> { Color.red, Color.blue, Color.cyan, Color.gray, Color.black, Color.white, Color.magenta, Color.green, Color.yellow };
        private List<Color> shortColorList = new List<Color> { Color.cyan, Color.green, Color.yellow, Color.magenta };
        private int colorIndex = 0;
        private int shortColorIndex = 0;
        private BuildingPlacer buildingPlacer;
        public HashSet<HashSet<GridNode>> suburbsBlocks = new HashSet<HashSet<GridNode>>();
        private static readonly Vector2Int[] directions = new Vector2Int[]
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
        private static readonly Vector2Int[] cornerDirections = new Vector2Int[]
        {
            new Vector2Int(1, -1),  // Up + Left
            new Vector2Int(1, 1),  // Up + Right
            new Vector2Int(-1, -1),  // Down + Left
            new Vector2Int(-1, 1)  // Down + Right
        };
        private static readonly Direction[] directionsEnum = new Direction[]
        {
            Direction.left, Direction.right, Direction.back, Direction.forward
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
                    SortedNodesResult result = GetBoundaryNodes(currentGroup);
                    shortColorIndex = 0;
                    for (int i = 0; i < result.wholeNodesLists.Count; i++)
                    {
                        if (result.wholeNodesLists[i].Count > 0 && result.wholeNodesLists[i].First().regionType == Region.Suburbs)
                        {
                            suburbsBlocks.Add(new HashSet<GridNode>(result.wholeNodesLists[i]));
                        }
                        DivideAndMarkGroup(result.wholeNodesLists[i], minWidth, minHeight, true, result.wholeNodesLists[i].Count, result.sortedNodesLists[i]);
                    }
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
        public void DivideAndMarkGroup(List<GridNode> group, int minWidth, int minHeight, bool firstIteration = false, int numNodes = 0, List<GridNode> boundaries = null)
        {
            if (group.Count == 0) return;

            // Find the bounding box of the group of nodes
            int minX = group.Min(n => n.gridX);
            int maxX = group.Max(n => n.gridX);
            int minY = group.Min(n => n.gridY);
            int maxY = group.Max(n => n.gridY);

            if (!firstIteration && ShouldSkipDivision(minX, maxX, minY, maxY))
            {
                //SpawnSphere(new Vector3(
                //    group.Average(node => node.worldPosition.x),
                //    group.Average(node => node.worldPosition.y),
                //    group.Average(node => node.worldPosition.z)
                //), Color.blue, 3f, 4f);
                //Debug.Log("skip division: " + "minX: " + minX + ", maxX: " + maxX + ", minY: " + minY + ", maxY: " + maxY);
                return;
            }

            bool hasMarked = false;
            bool isConcaveShape = false;
            if (firstIteration)
            {
                isConcaveShape = IsConcaveShape(boundaries);
            }
            // Calculate width and height of the group
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Introduce variation in minWidth and minHeight
            minWidth = VaryMinDimension(minWidth);
            minHeight = VaryMinDimension(minHeight);

            // If the group size is already smaller than the minimum, we can't split it anymore
            if (width <= minWidth && height <= minHeight)
            {
                return;
            }

            int maxRetries = 5;

            // Decide how to split: horizontal or vertical
            if (width > minWidth)
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    // Vary the splitX position, offsetting randomly around the middle
                    int splitX = minX + (int)((width / 2) * UnityEngine.Random.Range(0.8f, 1.2f));

                    // Try marking a vertical division
                    if (CanMarkDivision(splitX, minY, maxY, group, minWidth, true))
                    {
                        hasMarked = true;
                        MarkDivision(splitX, minY, maxY, group, true);

                        // Subdivide only after marking the division
                        DivideAndMarkGroup(group.Where(n => n.gridX < splitX).ToList(), minWidth, minHeight);
                        DivideAndMarkGroup(group.Where(n => n.gridX > splitX).ToList(), minWidth, minHeight);
                        break; // Break the loop if successful
                    }
                }
            }
            else if (height > minHeight)
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    //int splitY = minY + height / 2;
                    int splitY = minY + (int)((height / 2) * UnityEngine.Random.Range(0.8f, 1.2f));

                    // Only continue subdivision if we can mark a valid horizontal division
                    if (CanMarkDivision(splitY, minX, maxX, group, minHeight, false))
                    {
                        hasMarked = true;
                        MarkDivision(splitY, minX, maxX, group, false);

                        // Subdivide only after marking the division
                        DivideAndMarkGroup(group.Where(n => n.gridY > splitY).ToList(), minWidth, minHeight);
                        DivideAndMarkGroup(group.Where(n => n.gridY < splitY).ToList(), minWidth, minHeight);
                        break;
                    }
                }

            }

            if (firstIteration && !hasMarked && (numNodes == group.Count))
            {
                // The division has failed for the whole group, should we retry?
                //SpawnSphere(group.First().worldPosition, Color.red, 3f, 4f);

                // Act
                if (numNodes > 20)
                {
                    TraceRandomLines(group, boundaries);
                }

            }
        }
        // New method to randomly trace lines within the group
        private void TraceRandomLines(List<GridNode> group, List<GridNode> boundaries)
        {
            int maxTraceAttempts = Mathf.Max(10, group.Count/ 11); // Max random traces to try
            HashSet<GridNode> boundariesHash = new HashSet<GridNode>(boundaries);

            // HashSet where I'll remove the non valid nodes
            List<GridNode> candidates = boundaries.FindAll(x => HasOnlyHorizontalOrVerticalNeighbours(x, boundariesHash));
            for (int i = 0; i < maxTraceAttempts; i++)
            {
                if (candidates.Count <= 0) return;

                // Pick a random starting node
                GridNode randomNode = null;
                int elemId = UnityEngine.Random.Range(0, candidates.Count);
                randomNode = candidates[elemId];
                candidates.RemoveAt(elemId);

                if (randomNode == null) return;

                // Encontrar la direccion para avanzar
                List<GridNode> nodesAvailable = buildingPlacer.GetUsageNeighbourNodes(randomNode.gridX, randomNode.gridY, new List<Usage>() { Usage.building });
                nodesAvailable = nodesAvailable.FindAll(x => !boundariesHash.Contains(x));
                List<Direction> directionsAvailable = new List<Direction>();
                foreach (GridNode neighbour in nodesAvailable)
                {
                    directionsAvailable.Add(RoadPlacer.Instance.GetDirectionBasedOnPos(randomNode, neighbour));
                }

                if (directionsAvailable.Count <= 0) break;

                if (directionsAvailable.Count > 1)
                {
                    Debug.LogError("Nodo boundary con más de 1 direccion disponible");
                    SpawnSphere(randomNode.worldPosition, Color.green, 2f, 8f);
                }
                // Comprobar si avanzar en la direccion disponible es viable, comprobar vecinos Decoration cerca
                Direction chosenDir = directionsAvailable.First();
                List<GridNode> path = DirectionMeetsDecoration(randomNode.gridX, randomNode.gridY, chosenDir);

                if (path != null)
                {
                    path.ForEach(x =>
                    {
                        //SpawnSphere(x.worldPosition, Color.green, 2f, 2f);
                        x.usage = Usage.decoration;
                        x.isAlley = true;
                    });
                }
            }
        }
        private bool IsDecorationNodeAt(int x, int y)
        {
            GridNode node = Grid.Instance.nodesGrid[x, y];
            return node != null && node.usage == Usage.decoration;
        }
        // Helper method: Adds variation to the min dimension (width or height)
        private int VaryMinDimension(int originalMin)
        {
            int variation = UnityEngine.Random.Range(-1, 2); // Vary by -1, 0, or +1
            return Math.Max(2, originalMin + variation); // Ensure the minimum is at least 2
        }

        // Helper method: Random chance to skip dividing the group
        private bool ShouldSkipDivision(int minX, int maxX, int minY, int maxY)
        {
            int xDif = Mathf.Abs(maxX - minX);
            int yDif = Mathf.Abs(maxY - minY);
            if (xDif > 14 || yDif > 14)
            {
                return false;
            }
            else if (xDif > 10 || yDif > 10)
            {
                return UnityEngine.Random.value < 0.3f;
            }
            else if (xDif > 5 || yDif > 5)
            {
                return UnityEngine.Random.value < 0.5f;
            }

            return false;
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
                    node.isAlley = true;
                }
            }
        }
        private bool IsConcaveShape(List<GridNode> group)
        {
            List<GridNode> boundaryNodes = group;

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
                    //SpawnSphere(prevNode.worldPosition, Color.black, 3f, 4f);
                    //SpawnSphere(currNode.worldPosition, Color.white, 3f, 4f);
                    //SpawnSphere(nextNode.worldPosition, Color.gray, 3f, 4f);
                    return true; // Shape is concave
                }
            }

            return false; // Shape is convex
        }
        private SortedNodesResult GetBoundaryNodes(List<GridNode> group)
        {
            // This stores all the different lists sorted
            List<List<GridNode>> sortedNodesLists = new List<List<GridNode>>();
            List<List<GridNode>> wholeNodesLists = new List<List<GridNode>>();
            SortedNodesResult sortedNodesResult = new SortedNodesResult(sortedNodesLists, wholeNodesLists);

            // List that stores all the boundaryNodes
            List<GridNode> boundaryNodes = new List<GridNode>();
            HashSet<GridNode> groupSet = new HashSet<GridNode>(group);

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
            List<GridNode> sortedNodes = SortBoundaryNodes(boundaryNodes);
            sortedNodesLists.Add(sortedNodes);
            wholeNodesLists.Add(GetWholeNodesFromBoundaries(boundaryNodes, group));
            // Comparar si el tamaño original ha cambiado, la forma puede que requiera dividir el grupo en N partes
            if ((sortedNodes.Count != boundaryNodes.Count) && sortedNodes.Count > 0)
            {
                // Let's remove the wrong missing nodes and re process them ;)
                List<GridNode> missingNodes = boundaryNodes.Except(sortedNodes).ToList();
                // Now we have all the different missingNodes unsorted, we need to process them N times until there are none left
                missingNodes = GetSortableMissingNodes(missingNodes);
                if (missingNodes.Count > 6)
                {
                    while (true)
                    {
                        sortedNodes = SortMissingNodes(missingNodes);
                        sortedNodesLists.Add(sortedNodes);
                        wholeNodesLists.Add(GetWholeNodesFromBoundaries(sortedNodes, group));
                        if (missingNodes.Count == sortedNodes.Count)
                            break;

                        // Remove the added group from remaining nodes to process
                        missingNodes = missingNodes.Except(sortedNodes).ToList();

                        // If there are enough to keep dividing, keep doing it
                        if (missingNodes.Count > 6)
                            continue;

                        break;
                    }
                }
            }
            return sortedNodesResult;
        }
        private List<GridNode> GetWholeNodesFromBoundaries(List<GridNode> boundaryNodes, List<GridNode> allNodes)
        {
            List<GridNode> wholeNodes = new List<GridNode>();
            var boundaryNodesHashSet = new HashSet<GridNode>(boundaryNodes);
            foreach (var node in allNodes)
            {
                if (IsNodeContainedInBoundaries(node, boundaryNodesHashSet))
                    wholeNodes.Add(node);
            }

            return wholeNodes;
        }
        private List<GridNode> SortMissingNodes(List<GridNode> group)
        {
            List<GridNode> sortedGroup = new List<GridNode>();
            HashSet<GridNode> boundaryNodes = new HashSet<GridNode>(group);
            GridNode currentNode = GetStartingNodeForSortingBoundaries(group);
            if (currentNode == null)
            {
                //SpawnSphere(group.First().worldPosition, Color.white, 2f, 6f);
                return group;
            }

            // Crear HashSet para comprobar más rapido los nodos ya añadidos
            HashSet<GridNode> addedNodes = new HashSet<GridNode>();
            Direction currentDir = Direction.zero;

            while (true)
            {
                // Añado el nodo actual a las listas
                sortedGroup.Add(currentNode);
                addedNodes.Add(currentNode);

                // Primero recupero todos los nodos vecinos building
                List<GridNode> nodesAvailable = buildingPlacer.GetUsageNeighbourNodes(currentNode.gridX, currentNode.gridY, new List<Usage>() { Usage.building });
                // Los filtro para quedarme solo aquellos que sean boundary
                nodesAvailable = nodesAvailable.FindAll(x => boundaryNodes.Contains(x));
                // Ahora los filtro por direccions disponibles y no añadidas
                List<Direction> directionsAvailable = new List<Direction>();
                foreach (GridNode neighbour in nodesAvailable)
                {
                    directionsAvailable.Add(RoadPlacer.Instance.GetDirectionBasedOnPos(currentNode, neighbour));
                }
                directionsAvailable = GetNotContainedNodesInDirections(directionsAvailable, currentNode.gridX, currentNode.gridY, addedNodes);

                // Ya tenemos todas las direcciones disponibles excluyendo nodos ya contenidos
                int currentX = currentNode.gridX;
                int currentY = currentNode.gridY;
                if (directionsAvailable.Count == 0)
                {
                    // Terminar iteración
                    break;
                }
                else if (directionsAvailable.Count == 1)
                {
                    currentDir = directionsAvailable.First();
                    currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                }
                else if (directionsAvailable.Count == 2)
                {
                    int penultimateId = sortedGroup.Count - 2;
                    GridNode lastNodeAdded = penultimateId > 0 ? sortedGroup[sortedGroup.Count - 2] : currentNode;
                    currentDir = GetCorrectDirectionFromList(currentNode, lastNodeAdded, directionsAvailable);
                    if (currentDir == Direction.zero)
                    {
                        SpawnSphere(sortedGroup.First().worldPosition, Color.cyan, 2f, 3f);
                        sortedGroup.ForEach(x => SpawnSphere(x.worldPosition, Color.green, 2f, 2f));
                    }
                    currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                }
                else if (directionsAvailable.Count > 2)
                {
                    bool isHorizontal = directionsAvailable.Contains(Direction.left) && directionsAvailable.Contains(Direction.right);
                    if (isHorizontal)
                    {
                        currentDir = directionsAvailable.First(x => x == Direction.left || x == Direction.right);
                    }
                    else
                    {
                        currentDir = directionsAvailable.First(x => x == Direction.back || x == Direction.forward);
                    }
                    currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                }
            }

            return sortedGroup;
        }
        private List<GridNode> SortBoundaryNodes(List<GridNode> group)
        {
            List<GridNode> sortedGroup = new List<GridNode>();
            // Crear HashSet para comprobar más rapido los nodos del conjunto total
            HashSet<GridNode> boundaryNodes = new HashSet<GridNode>(group);

            // Encontrar el nodo inicial
            GridNode currentNode = null;
            GridNode reserveNode = null;
            foreach (GridNode node in group)
            {
                if (currentNode != null)
                    break;

                int numHorizontal = 0;
                foreach (var horizontalNeighbour in Grid.Instance.GetNeighboursInHorizontal(node, new List<Usage>() { Usage.building }))
                {
                    if (boundaryNodes.Contains(horizontalNeighbour))
                        numHorizontal++;
                }
                int numVertical = 0;
                foreach (var verticalNeighbour in Grid.Instance.GetNeighboursInVertical(node, new List<Usage>() { Usage.building }))
                {
                    if (boundaryNodes.Contains(verticalNeighbour))
                        numVertical++;
                }
                if ((numHorizontal == 0 && numVertical == 2) || (numHorizontal == 2 && numVertical == 0))
                {
                    //SpawnSphere(node.worldPosition, Color.magenta, 2f, 1f);
                    // Comprobar que no es callejon
                    // Si no es callejon, puede tirar a una de las direcciones en las que no tiene vecinos y encontrar un building (no boundary)
                    List<Direction> directions = numHorizontal == 0 ? new List<Direction>() { Direction.left, Direction.right } : new List<Direction>() { Direction.back, Direction.forward };

                    foreach (Direction dir in directions)
                    {
                        GridNode neighbour = GetNodeInDirection(node.gridX, node.gridY, dir, 1);
                        if (neighbour.usage == Usage.building)
                        {
                            //SpawnSphere(node.worldPosition, Color.red, 2f, 2f);
                            currentNode = node;
                            break;
                        }
                    }
                }
                else if ((numHorizontal == 1 && numVertical == 2) || (numHorizontal == 2 && numVertical == 1))
                {
                    reserveNode = node;
                }
            }
            if (currentNode == null)
            {
                if (reserveNode == null)
                {
                    if (group.Count == 0)
                        return group;

                    group = group.OrderBy(node => node.gridX)
                        .ThenBy(node => node.gridY)
                        .ToList();

                    currentNode = group.First();
                }
                else
                {
                    currentNode = reserveNode;
                }

            }

            // Crear HashSet para comprobar más rapido los nodos ya añadidos
            HashSet<GridNode> addedNodes = new HashSet<GridNode>();
            Direction currentDir = Direction.zero;

            while (true)
            {
                // Añado el nodo actual a las listas
                sortedGroup.Add(currentNode);
                addedNodes.Add(currentNode);

                // Primero recupero todos los nodos vecinos building
                List<GridNode> nodesAvailable = buildingPlacer.GetUsageNeighbourNodes(currentNode.gridX, currentNode.gridY, new List<Usage>() { Usage.building });
                // Los filtro para quedarme solo aquellos que sean boundary
                nodesAvailable = nodesAvailable.FindAll(x => boundaryNodes.Contains(x));
                // Ahora los filtro por direccions disponibles y no añadidas
                List<Direction> directionsAvailable = new List<Direction>();
                foreach (GridNode neighbour in nodesAvailable)
                {
                    directionsAvailable.Add(RoadPlacer.Instance.GetDirectionBasedOnPos(currentNode, neighbour));
                }
                directionsAvailable = GetNotContainedNodesInDirections(directionsAvailable, currentNode.gridX, currentNode.gridY, addedNodes);

                // Ya tenemos todas las direcciones disponibles excluyendo nodos ya contenidos
                int currentX = currentNode.gridX;
                int currentY = currentNode.gridY;
                if (directionsAvailable.Count == 0)
                {
                    // Terminar iteración
                    break;
                }
                else if (directionsAvailable.Count == 1)
                {
                    currentDir = directionsAvailable.First();
                    if (!DirectionMeetsNoExit(currentX, currentY, currentDir, boundaryNodes))
                    {
                        currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (directionsAvailable.Count == 2)
                {
                    List<Direction> directionsMeetExit = new List<Direction>();
                    foreach (Direction directionAvailable in directionsAvailable)
                    {
                        if (DirectionMeetsNoExit(currentX, currentY, directionAvailable, boundaryNodes))
                        {
                            directionsMeetExit.Add(directionAvailable);
                        }
                    }
                    if (directionsMeetExit.Count > 0)
                    {
                        // Quitar de las direcciones disponibles la direccion que lleva a un callejon
                        directionsAvailable = directionsAvailable.Except(directionsMeetExit).ToList();
                        if (directionsAvailable.Count == 0)
                            break;
                        currentDir = directionsAvailable.First();
                        currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                    }
                    else
                    {
                        int penultimateId = sortedGroup.Count - 2;
                        GridNode lastNodeAdded = penultimateId > 0 ? sortedGroup[sortedGroup.Count - 2] : currentNode;
                        currentDir = GetCorrectDirectionFromList(currentNode, lastNodeAdded, directionsAvailable);
                        if (currentDir == Direction.zero)
                        {
                            SpawnSphere(sortedGroup.First().worldPosition, Color.cyan, 2f, 3f);
                            sortedGroup.ForEach(x => SpawnSphere(x.worldPosition, Color.green, 2f, 2f));
                        }
                        currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);
                    }

                }
                else if (directionsAvailable.Count > 2)
                {

                    bool isHorizontal = directionsAvailable.Contains(Direction.left) && directionsAvailable.Contains(Direction.right);
                    if (isHorizontal)
                    {
                        currentDir = directionsAvailable.First(x => x == Direction.left || x == Direction.right);
                    }
                    else
                    {
                        currentDir = directionsAvailable.First(x => x == Direction.back || x == Direction.forward);
                    }
                    currentNode = GetNodeInDirection(currentX, currentY, currentDir, 1);

                }
            }

            return sortedGroup;
        }
        private GridNode GetStartingNodeForSortingBoundaries(List<GridNode> boundaryNodes)
        {
            foreach (GridNode node in boundaryNodes)
            {
                int numHorizontal = 0;
                foreach (var horizontalNeighbour in Grid.Instance.GetNeighboursInHorizontal(node, new List<Usage>() { Usage.building }))
                {
                    if (boundaryNodes.Contains(horizontalNeighbour))
                        numHorizontal++;
                }
                int numVertical = 0;
                foreach (var verticalNeighbour in Grid.Instance.GetNeighboursInVertical(node, new List<Usage>() { Usage.building }))
                {
                    if (boundaryNodes.Contains(verticalNeighbour))
                        numVertical++;
                }
                if ((numHorizontal == 0 && numVertical == 2) || (numHorizontal == 2 && numVertical == 0))
                {
                    //SpawnSphere(node.worldPosition, Color.magenta, 2f, 1f);
                    // Comprobar que no es callejon
                    // Si no es callejon, puede tirar a una de las direcciones en las que no tiene vecinos y encontrar un building (no boundary)
                    List<Direction> directions = numHorizontal == 0 ? new List<Direction>() { Direction.left, Direction.right } : new List<Direction>() { Direction.back, Direction.forward };

                    foreach (Direction dir in directions)
                    {
                        GridNode neighbour = GetNodeInDirection(node.gridX, node.gridY, dir, 1);
                        if (neighbour.usage == Usage.building)
                        {
                            //SpawnSphere(node.worldPosition, Color.red, 2f, 2f);
                            return node;
                        }
                    }
                }
            }
            return null;
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
        private bool IsNodeContainedInBoundaries(GridNode node, HashSet<GridNode> boundaries)
        {
            // Check all directions
            int startX = node.gridX;
            int startY = node.gridY;
            foreach (Direction direction in directionsEnum)
            {
                int i = 1;
                while (true)
                {
                    GridNode newNode = GetNodeInDirection(startX, startY, direction, i);
                    if (newNode == null)
                        break;

                    if (newNode.usage == Usage.building && boundaries.Contains(newNode))
                        return true;

                    if (newNode.usage == Usage.decoration || newNode.usage == Usage.road)
                        break;

                    i++;
                }

            }

            return false;
        }
        private GridNode GetNodeInDirection(int startX, int startY, Direction direction, int iterationIncrement)
        {
            int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
            int newX = startX + offset[0] * iterationIncrement;
            int newY = startY + offset[1] * iterationIncrement;

            if (Grid.Instance.OutOfGrid(newX, newY))
                return null;

            return Grid.Instance.nodesGrid[newX, newY];
        }
        private GridNode GetNodeInDirection(int startX, int startY, Vector2Int offset, int iterationIncrement)
        {
            int newX = startX + offset.x * iterationIncrement;
            int newY = startY + offset.y * iterationIncrement;

            if (Grid.Instance.OutOfGrid(newX, newY))
                return null;

            return Grid.Instance.nodesGrid[newX, newY];
        }
        private Direction GetCorrectDirectionFromList(GridNode currentNode, GridNode previousNode, List<Direction> availableDirections)
        {
            int currentX = currentNode.gridX;
            int currentY = currentNode.gridY;

            // Check on which new node from the available directions, we are following the wall, that means, having a decoration node by the side
            int numDecoration = 0;
            foreach (Direction availableDir in availableDirections)
            {
                GridNode newNode = GetNodeInDirection(currentX, currentY, availableDir, 1);
                List<Direction> decorationDirections = IsHorizontal(availableDir) ? GetVerticalDirections() : GetHorizontalDirections();

                int newNodeX = newNode.gridX;
                int newNodeY = newNode.gridY;
                numDecoration = 0;
                foreach (Direction decorationDirection in decorationDirections)
                {
                    if (GetNodeInDirection(newNodeX, newNodeY, decorationDirection, 1).usage == Usage.decoration)
                    {
                        numDecoration++;
                    }
                }
                // If == 2 then we're entering a callejon sin salida
                if (numDecoration == 1)
                {
                    return availableDir;
                }
            }
            if (numDecoration == 0)
            {
                return RoadPlacer.Instance.GetDirectionBasedOnPos(previousNode, currentNode);
            }
            //SpawnSphere(currentNode.worldPosition, Color.blue, 2f, 5f);
            return Direction.zero;
        }
        private bool HasOnlyHorizontalOrVerticalNeighbours(GridNode node, HashSet<GridNode> boundaries)
        {
            int numHorizontal = 0;
            foreach (Direction hDirection in GetHorizontalDirections())
            {
                GridNode newNode = GetNodeInDirection(node.gridX, node.gridY, hDirection, 1);
                if (newNode.usage == Usage.building && boundaries.Contains(newNode))
                    numHorizontal++;
            }

            int numVertical = 0;
            foreach (Direction vDirection in GetVerticalDirections())
            {
                GridNode newNode = GetNodeInDirection(node.gridX, node.gridY, vDirection, 1);
                if (newNode.usage == Usage.building && boundaries.Contains(newNode))
                    numVertical++;
            }

            if ((numHorizontal == 0 && numVertical == 2) || (numHorizontal == 2 && numVertical == 0)) return true;

            return false;

            // Count horizontal and vertical neighbors
            numHorizontal = GetHorizontalDirections()
                .Select(dir => GetNodeInDirection(node.gridX, node.gridY, dir, 1))
                .Count(newNode => newNode.usage == Usage.building && boundaries.Contains(newNode));

            numVertical = GetVerticalDirections()
                .Select(dir => GetNodeInDirection(node.gridX, node.gridY, dir, 1))
                .Count(newNode => newNode.usage == Usage.building && boundaries.Contains(newNode));

            return (numHorizontal == 2 && numVertical == 0) || (numHorizontal == 0 && numVertical == 2);
        }
        private List<GridNode> GetSortableMissingNodes(List<GridNode> nodes)
        {
            List<GridNode> sortableNodes = new List<GridNode>();

            // Let's check for decoration nodes
            foreach (GridNode node in nodes)
            {
                // Si tiene nodos decoration a cada lado horizontal o vertical, eliminar

                // Horizontal
                int numHorizontal = 0;
                foreach (Direction hDirection in GetHorizontalDirections())
                {
                    GridNode newNode = GetNodeInDirection(node.gridX, node.gridY, hDirection, 1);
                    if (newNode.usage == Usage.decoration)
                        numHorizontal++;
                }

                // Vertical
                int numVertical = 0;
                foreach (Direction vDirection in GetVerticalDirections())
                {
                    GridNode newNode = GetNodeInDirection(node.gridX, node.gridY, vDirection, 1);
                    if (newNode.usage == Usage.decoration)
                        numVertical++;
                }


                if (!(numHorizontal == 0 && numVertical == 2)
                    && !(numHorizontal == 2 && numVertical == 0)
                    && (numHorizontal + numVertical < 3)
                    && !IsTightCorner(node.gridX, node.gridY, numHorizontal, numVertical))
                    sortableNodes.Add(node);
            }

            return sortableNodes;
        }
        private bool IsTightCorner(int numHorizontal, int numVertical, int x, int y)
        {
            if (numHorizontal != 1 || numVertical != 1)
                return false;

            // Check the corners, 3 corner decoration neighbours, return true
            int numCornerNeighbours = 0;
            foreach (Vector2Int offset in cornerDirections)
            {
                GridNode cornerNeighbour = GetNodeInDirection(x, y, offset, 1);
                if (cornerNeighbour.usage == Usage.decoration)
                    numCornerNeighbours++;
            }
            if (numCornerNeighbours > 3)
                return true;

            return false;
        }
        private bool DirectionMeetsNoExit(int startX, int startY, Direction direction, HashSet<GridNode> boundaryNodes)
        {
            int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
            int i = 1;
            while (true)
            {
                int newX = startX + offset[0] * i;
                int newY = startY + offset[1] * i;

                if (Grid.Instance.OutOfGrid(newX, newY))
                    return false;

                var currentNode = Grid.Instance.nodesGrid[newX, newY];
                if (currentNode.usage == Usage.building)
                {
                    // Check if it has more than one building neighbour, then it's not a straight
                    List<GridNode> buildingNeighbours = buildingPlacer.GetUsageNeighbourNodes(currentNode.gridX, currentNode.gridY, new List<Usage>() { Usage.building });
                    List<GridNode> newBoundaryNodes = new List<GridNode>();
                    foreach (var buildingNeighbour in buildingNeighbours)
                    {
                        if (boundaryNodes.Contains(buildingNeighbour))
                            newBoundaryNodes.Add(buildingNeighbour);
                    }
                    if (newBoundaryNodes.Count >= 2)
                    {
                        List<Direction> decorationDirections = IsHorizontal(direction) ? GetVerticalDirections() : GetHorizontalDirections();

                        int numDecoration = 0;
                        foreach (Direction decorationDirection in decorationDirections)
                        {
                            if (GetNodeInDirection(newX, newY, decorationDirection, 1).usage == Usage.decoration)
                            {
                                numDecoration++;
                            }
                        }
                        if (numDecoration == 2)
                            return true;

                        return false;
                    }

                    // Check if next node in direction is decoration
                    newX = startX + offset[0] * (i + 1);
                    newY = startY + offset[1] * (i + 1);
                    var nextNode = Grid.Instance.nodesGrid[newX, newY];
                    if (nextNode.usage == Usage.decoration)
                    {
                        // Necesito identificar si el current node previo al decoration es un final sin salida o si puede girar
                        if (buildingNeighbours.Count == 1)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                else
                {
                    //SpawnSphere(currentNode.worldPosition, Color.red, 2f, 4f);
                    return false;
                }
                i++;
            }
        }
        private List<GridNode> DirectionMeetsDecoration(int startX, int startY, Direction direction)
        {
            List<GridNode> path = new List<GridNode>() { Grid.Instance.nodesGrid[startX, startY] };
            int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
            int i = 1;
            int minDecorationDist = 2;
            List<Direction> decorationDirections = IsHorizontal(direction) ? GetVerticalDirections() : GetHorizontalDirections();
            while (true)
            {
                int newX = startX + offset[0] * i;
                int newY = startY + offset[1] * i;

                if (Grid.Instance.OutOfGrid(newX, newY))
                    return null;

                var currentNode = Grid.Instance.nodesGrid[newX, newY];
                if (currentNode.usage == Usage.building)
                {
                    //SpawnSphere(currentNode.worldPosition, Color.green, 2f, 2f);
                    // Check that there are no decoration neighbours near
                    for (int j = 1; j <= minDecorationDist; j++)
                    {
                        foreach (var decorationDirection in decorationDirections)
                        {
                            GridNode decorationNode = GetNodeInDirection(newX, newY, decorationDirection, j);
                            //SpawnSphere(decorationNode.worldPosition, Color.yellow, 2f, 2f);
                            if (decorationNode == null || decorationNode.usage == Usage.decoration)
                                return null;
                        }
                    }

                    path.Add(currentNode);
                    // Check if next node in direction is decoration
                    newX = startX + offset[0] * (i + 1);
                    newY = startY + offset[1] * (i + 1);
                    var nextNode = Grid.Instance.nodesGrid[newX, newY];
                    if (nextNode.usage == Usage.decoration)
                    {                       
                        // It could be an alley created by this algorithm
                        if (nextNode.isAlley && UnityEngine.Random.value < .4f)
                        {
                            // Skip with 40% percent change
                            // Increment index to skip the alleyNode
                            path.Add(nextNode);
                            i++;
                        }
                        else
                        {
                            return path;
                        }
                    }
                }
                else
                {
                    return null;
                }
                i++;
            }
        }
        private List<Direction> GetNotContainedNodesInDirections(List<Direction> directions, int startX, int startY, HashSet<GridNode> addedNodes)
        {
            List<Direction> directionsAvailable = new List<Direction>();
            foreach (Direction direction in directions)
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                int currentPosX = startX + dir[0];
                int currentPosY = startY + dir[1];

                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    continue;

                GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];
                if (currentNode.usage == Usage.building && !addedNodes.Contains(currentNode))
                {
                    directionsAvailable.Add(direction);
                }
            }
            return directionsAvailable;
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        private bool IsHorizontal(Direction direction)
        {
            return (direction == Direction.left || direction == Direction.right);
        }
        private List<Direction> GetHorizontalDirections()
        {
            return new List<Direction>() { Direction.left, Direction.right };
        }
        private List<Direction> GetVerticalDirections()
        {
            return new List<Direction>() { Direction.back, Direction.forward };
        }
        public class SortedNodesResult
        {
            public List<List<GridNode>> sortedNodesLists = new List<List<GridNode>>();
            public List<List<GridNode>> wholeNodesLists = new List<List<GridNode>>();

            public SortedNodesResult(List<List<GridNode>> sortedNodesLists, List<List<GridNode>> wholeNodesLists)
            {
                this.sortedNodesLists = sortedNodesLists;
                this.wholeNodesLists = wholeNodesLists;
            }
        }
    }
}

