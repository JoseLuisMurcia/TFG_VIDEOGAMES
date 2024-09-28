using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class RegionHelper
    {
        private Dictionary<int, VoronoiRegion> regionsDictionary = new Dictionary<int, VoronoiRegion>();
        private List<VoronoiRegion> regions;
        private List<VoronoiRegion> mainRegions;
        private List<VoronoiRegion> gangRegions;
        private HashSet<GridNode> boundaries;
        private HashSet<GridNode> boundariesToAdd;

        // Used in the first part of the algorithm
        private int mainDistrictMinNodes = 4000;
        private int mainDistrictMaxNodes = 6000;

        // Used after roads have been created
        private float mainDistrictMinPercentage = 25f;
        private float mainDistrictMaxPercentage = 30f;
        private float gangDistrictMinPercentage = 15f;
        private float gangDistrictMaxPercentage = 20f;
        int totalNodesContained = 0;

        /* ALGORITHM 
         * 1 - Define parameters:
         * Range for districts size (min 100 nodes, max 200)
         * 2 - Select a random polygon that is in the road structure
         * Select one of his neighbours and add him to a list of selected polygons for city district
         * Do the process until the requirements are met, and then, make all those nodes belong to the main city district
         * 3 - Repeat 2 for the suburbs, keep in mind that it should not be conected to the main city district
         * 4 - Every other node should be residential
         * 5 - After road creation, AdjustRegions() is called to check which nodes fall inside the boundaries of the road network. 
         * We need to remove or add polygons (VoronoiRegions) in order to reach a certain percentage.
         * That way we are in real control of the generation.
         */
        public void SetRegions(List<VoronoiRegion> regions)
        {
            foreach (VoronoiRegion region in regions)
            {
                regionsDictionary.Add(region.id, region);
            }
            this.regions = regions;
            CreateMainDistrict(regions);
            //CreateGangDistrict(regions);
        }
        public void AdjustRegions()
        {
            // Find the boundaries of the road network
            boundaries = new HashSet<GridNode>();
            List<Usage> decorationUsage = new List<Usage>() { Usage.decoration };
            List<Usage> emptyUsage = new List<Usage>() { Usage.empty, Usage.EOW };
            for (int i = 0; i < Grid.Instance.gridSizeX; i++)
            {
                for (int j = 0; j < Grid.Instance.gridSizeY; j++)
                {
                    GridNode currentNode = Grid.Instance.nodesGrid[i, j];
                    
                    if (currentNode.usage != Usage.empty)
                        continue;

                    // Check if it has a decorationNeighbour
                    List<GridNode> decorationNeighbours = Grid.Instance.GetNeighbours(currentNode, decorationUsage);
                    if (decorationNeighbours.Count > 0)
                    {
                        List<GridNode> allNeighbours = Grid.Instance.GetNeighboursInLine(currentNode);
                        // Check if this node is already a boundary
                        if (allNeighbours.Count < 4)
                        {
                            boundaries.Add(currentNode);
                            currentNode.usage = Usage.EOW;
                            continue;
                        }
                        // Get empty or EOW neighbours
                        List<GridNode> availableNeighbours = Grid.Instance.GetNeighboursInLine(currentNode, emptyUsage);
                        List<GridNode> freeNeighbours = availableNeighbours.Except(decorationNeighbours).ToList();
                        if (freeNeighbours.Count <= 0)
                            continue;

                        // Advance in the direction of the neighbours until EOW
                        foreach (GridNode neighbour in freeNeighbours)
                        {
                            Direction direction = RoadPlacer.Instance.GetDirectionBasedOnPos(currentNode, neighbour);
                            bool directionMeetsEOW = MeetsEndOfWorld(neighbour.gridX, neighbour.gridY, direction);
                            if (directionMeetsEOW)
                            {
                                boundaries.Add(currentNode);
                                currentNode.usage = Usage.EOW;
                                break;
                            }
                        }
                    }

                }
            }
            // Reattempt
            boundariesToAdd = new HashSet<GridNode>();
            foreach (GridNode boundary in boundaries)
            {
                // Let's find decoration nodes from the boundaries
                List<GridNode> decorationNeighbours = Grid.Instance.GetNeighboursInLine(boundary, decorationUsage);
                foreach (GridNode decorationNeighbour in decorationNeighbours)
                {
                    Direction dirToNeighbour = RoadPlacer.Instance.GetDirectionBasedOnPos(boundary, decorationNeighbour);
                    Direction oppositeDir = RoadPlacer.Instance.GetOppositeDir(dirToNeighbour);

                    if (MeetsDecoration(boundary.gridX, boundary.gridY, oppositeDir))
                    {
                        break;
                    }
                }           
            }
            foreach (GridNode boundary in boundariesToAdd)
            {
                boundaries.Add(boundary);
            }
            boundariesToAdd.Clear();


            // With the boundaries, we can check which nodes fall inside the boundaries
            totalNodesContained = 0;
            for (int i = 0; i < Grid.Instance.gridSizeX; i++)
            {
                for (int j = 0; j < Grid.Instance.gridSizeY; j++)
                {
                    GridNode currentNode = Grid.Instance.nodesGrid[i, j];
                    if (boundaries.Contains(currentNode))
                        continue;

                    if (IsContainedInBoundaries(currentNode))
                    {
                        totalNodesContained++;
                        currentNode.isContained = true;
                        regionsDictionary[currentNode.voronoiRegion.id].nodesContained.Add(currentNode);
                    }
                }
            }

            // Iterate the regions in the main district to check if the minimum amount of nodes is reached
            int totalMainNodesContained = 0;
            
            foreach (var region in mainRegions)
            {
                totalMainNodesContained += region.nodesContained.Count;
            }
            float percentageMainContained = ((float)totalMainNodesContained / (float)totalNodesContained) * 100f;
            Debug.Log("[BEFORE] totalNodesContained: " + totalNodesContained + ", totalMainNodesContained: " + totalMainNodesContained + ", percentage: " + percentageMainContained + "%");
            if (percentageMainContained < mainDistrictMinPercentage)
            {
                // Add more regions if there are not enough nodes
                ExpandMainRegion(percentageMainContained, totalMainNodesContained);
            }
            else if (percentageMainContained > mainDistrictMaxPercentage)
            {
                // Remove regions if we've exceeded the maximum percentage
                ContractMainRegion(percentageMainContained, totalMainNodesContained);
            }
            totalMainNodesContained = 0;
            foreach (var region in mainRegions)
            {
                totalMainNodesContained += region.nodesContained.Count;
            }
            percentageMainContained = ((float)totalMainNodesContained / (float)totalNodesContained) * 100f;
            Debug.Log("[AFTER] totalNodesContained: " + totalNodesContained + ", totalMainNodesContained: " + totalMainNodesContained + ", percentage: " + percentageMainContained + "%");


            // Create the gang district after the main has been created
            CreateGangDistrict(regions);
            int totalGangNodesContained = 0;
            float percentageGangContained = 0f;
            foreach (var region in gangRegions)
            {
                totalGangNodesContained += region.nodesContained.Count;
            }
            percentageGangContained = ((float)totalGangNodesContained / (float)totalNodesContained) * 100f;
            Debug.Log("totalNodesContained: " + totalNodesContained + ", totalGangNodesContained: " + totalGangNodesContained + ", percentage: " + percentageGangContained + "%");
        }
        private void ExpandMainRegion(float percentageMainContained, float totalMainNodesContained)
        {
            /* Add regions to the mainRegions list */

            // Look for the neighbours with less neighbours availabe (close gaps) and select them as the next region to add

            // Iterate until the percentage is met
            while (percentageMainContained < mainDistrictMinPercentage)
            {
                // Find the next candidate region to add: a neighboring region of the main district
                VoronoiRegion nextRegion = null;
                int fewestNeighbours = int.MaxValue;

                foreach (var region in mainRegions)
                {
                    foreach (var neighbour in region.neighbourRegions)
                    {
                        // Ignore regions that are already in the main district
                        if (neighbour.addedToDistrict || !HasContainedNodes(neighbour)) continue;

                        // Check if the neighbor has fewer neighboring regions, to "close gaps"
                        int neighbourCount = neighbour.neighbourRegions.Count(n => !n.addedToDistrict);
                        if (neighbourCount < fewestNeighbours && IsSizeOk(mainDistrictMaxPercentage, percentageMainContained, neighbour))
                        {
                            fewestNeighbours = neighbourCount;
                            nextRegion = neighbour;
                        }
                    }
                }

                // If no neighboring region was found to expand the main region, we should break to avoid an infinite loop
                if (nextRegion == null)
                {
                    Debug.LogWarning("No more regions available to expand the main district.");
                    break;
                }

                // Add the selected neighboring region to the main district
                nextRegion.addedToDistrict = true;
                mainRegions.Add(nextRegion);
                SpawnSphere(nextRegion.centre, Color.blue, 2f, 6f);
                totalMainNodesContained += nextRegion.nodesContained.Count;

                AssignTypeToRegion(nextRegion, Region.Main);
                // Recalculate the percentage of main district nodes contained
                percentageMainContained = ((float)totalMainNodesContained / (float)totalNodesContained) * 100f;
            }
        }
        private void ContractMainRegion(float percentageMainContained, int totalMainNodesContained)
        {
            // While the percentage of nodes contained in the main district is above the maximum threshold
            while (percentageMainContained > mainDistrictMaxPercentage)
            {
                // Find the next candidate region to remove: a region that has the fewest connections to other mainRegions
                VoronoiRegion regionToRemove = null;
                int fewestNeighbours = int.MaxValue;

                foreach (var region in mainRegions)
                {
                    // Count the number of neighbors that are still in the main district
                    int neighbourCount = region.neighbourRegions.Count(n => n.addedToDistrict);

                    // Prioritize regions with fewer neighbors in the district (i.e., "peripheral" regions)
                    if (neighbourCount < fewestNeighbours)
                    {
                        fewestNeighbours = neighbourCount;
                        regionToRemove = region;
                    }
                }

                // If no region is found (which is unlikely), break the loop to avoid infinite loops
                if (regionToRemove == null)
                {
                    Debug.LogWarning("No more regions available to contract from the main district.");
                    break;
                }

                // Remove the region from the main district
                regionToRemove.addedToDistrict = false;
                mainRegions.Remove(regionToRemove);
                SpawnSphere(regionToRemove.centre, Color.red, 2f, 6f);
                totalMainNodesContained -= regionToRemove.nodesContained.Count;

                AssignTypeToRegion(regionToRemove, Region.Residential);
                // Recalculate the percentage of main district nodes contained
                percentageMainContained = ((float)totalMainNodesContained / (float)totalNodesContained) * 100f;

                Debug.Log($"Removed region {regionToRemove.id} from main district. New percentage: {percentageMainContained}%");
            }
        }
        private void CreateMainDistrict(List<VoronoiRegion> regions)
        {
            /* Define the main city district */

            // Select the first polygon
            int firstId = Random.Range(0, regions.Count);

            // Add adjacent polygons to the main district 
            mainRegions = new List<VoronoiRegion>
            {
                regions[firstId]
            };
            mainRegions[0].addedToDistrict = true;
            int nodeCount = regions[firstId].nodes.Count;
            bool conditionsMet = false;

            //Debug.Log("CreateMainDistrict");

            while (!conditionsMet)
            {
                // Lista que tiene las regiones que se van a comparar para ver cual es mejor elección para el distrito.
                VoronoiRegion selectedRegion = mainRegions.Last();

                // Get the best candidate from the current neighbour
                VoronoiRegion bestNeighbour = SelectBestNeighbour(selectedRegion, mainRegions, mainDistrictMaxNodes, nodeCount, Region.Main);

                // Puede ser que la región seleccionada ya no tenga vecinos válidos
                if (bestNeighbour == null)
                {
                    // This list holds a COPY of the original regions
                    List<VoronoiRegion> regionsWithValidNeighbours = GetAddedRegionsWithValidNeighbours(mainRegions);

                    // No se sigue ningun criterio para seleccionar candidatos, pero se podría elegir a aquellos que tuvieran más o menos vecinos libres.
                    Debug.LogWarning("Retrying to assign a new region");
                    bestNeighbour = SelectBestNeighbourFromList(regions, regionsWithValidNeighbours, mainRegions, mainDistrictMaxNodes, nodeCount, Region.Main);

                    if (bestNeighbour == null)
                    {
                        Debug.LogError("NO BEST CANDIDATE FOUND, ABORTING EXECUTION");
                        break;
                    }
                    else
                    {
                        conditionsMet = AddRegionToDistrict(bestNeighbour, mainRegions, ref nodeCount, mainDistrictMinNodes);
                    }
                }
                else
                {
                    conditionsMet = AddRegionToDistrict(bestNeighbour, mainRegions, ref nodeCount, mainDistrictMinNodes);
                }

            }
            //Debug.Log("Node count: " + nodeCount);
            AssignTypeToRegions(mainRegions, Region.Main);
            CheckForIsolatedRegions(regions, Region.Main);
        }
        private void CreateGangDistrict(List<VoronoiRegion> regions)
        {
            // List of free (not added to any district) regions
            List<VoronoiRegion> freeRegions = new List<VoronoiRegion>();
            foreach (VoronoiRegion region in regions)
            {
                if (!region.addedToDistrict)
                    freeRegions.Add(region);
            }

            // Initialize the gang district regions list
            gangRegions = new List<VoronoiRegion>();

            // Find an initial region that is far enough from the main district
            VoronoiRegion initialRegion = null;
            foreach (var region in freeRegions)
            {
                if (IsRegionAwayFromType(region, 2, Region.Main) && HasContainedNodes(region))
                {
                    initialRegion = region;
                    break;
                }
            }

            // If no valid initial region is found, return
            if (initialRegion == null)
            {
                Debug.LogWarning("No suitable starting region found for the gang district.");
                return;
            }

            // Add the initial region to the gang district
            initialRegion.addedToDistrict = true;
            gangRegions.Add(initialRegion);
            freeRegions.Remove(initialRegion);

            // Initialize variables for tracking gang district size
            float percentageGangContained = 0f;
            int totalGangNodesContained = initialRegion.nodesContained.Count;
            percentageGangContained = ((float)totalGangNodesContained / (float)totalNodesContained) * 100f;

            // Set of regions that are neighbors of the current gangRegions
            HashSet<VoronoiRegion> candidateRegions = new HashSet<VoronoiRegion>();

            // Add the neighbors of the initial region to the candidate list
            foreach (var neighbor in initialRegion.neighbourRegions)
            {
                if (!neighbor.addedToDistrict)
                {
                    candidateRegions.Add(neighbor);
                }
            }

            // While the percentage hasn't been met, keep expanding the gang district
            while (percentageGangContained < gangDistrictMinPercentage)
            {
                VoronoiRegion bestCandidate = null;
                int minFreeNeighbours = int.MaxValue; // Track the minimum number of free neighbors

                // Iterate over the candidate regions to find the best one
                foreach (var candidate in candidateRegions)
                {
                    // Count how many of the candidate's neighbors are still free
                    int freeNeighboursCount = 0;
                    foreach (var neighbourOfNeighbour in candidate.neighbourRegions)
                    {
                        if (!neighbourOfNeighbour.addedToDistrict && IsRegionAwayFromType(candidate, 1, Region.Main))
                        {
                            freeNeighboursCount++;
                        }
                    }

                    // Keep track of the candidate with the fewest free neighbors
                    if (freeNeighboursCount < minFreeNeighbours && freeNeighboursCount > 0 && HasContainedNodes(candidate))
                    {
                        minFreeNeighbours = freeNeighboursCount;
                        bestCandidate = candidate;
                    }
                }

                // If no best candidate was found, stop (no valid expansions)
                if (bestCandidate == null)
                {
                    Debug.LogWarning("Unable to reach the target percentage for gang district. No valid expansion found.");
                    break;
                }

                // Add the best candidate to the gang district
                bestCandidate.addedToDistrict = true;
                gangRegions.Add(bestCandidate);
                freeRegions.Remove(bestCandidate);

                // Update the total gang nodes and percentage
                totalGangNodesContained += bestCandidate.nodesContained.Count;
                percentageGangContained = ((float)totalGangNodesContained / (float)totalNodesContained) * 100f;

                Debug.Log($"Added region {bestCandidate.id} to gang district. New percentage: {percentageGangContained}%");

                // Remove the best candidate from the candidate list
                candidateRegions.Remove(bestCandidate);

                // Add its neighbors to the candidate list if they are free and meet the distance criteria
                foreach (var neighbour in bestCandidate.neighbourRegions)
                {
                    if (!neighbour.addedToDistrict && IsRegionAwayFromType(neighbour, 2, Region.Main))
                    {
                        candidateRegions.Add(neighbour);
                    }
                }

                // Check if we've reached the target percentage
                if (percentageGangContained >= gangDistrictMinPercentage)
                    break;
            }

            // Assign the gang district type to the regions
            AssignTypeToRegions(gangRegions, Region.Suburbs);
        }

        private void AssignTypeToRegions(List<VoronoiRegion> regions, Region regionType)
        {
            foreach (VoronoiRegion region in regions)
            {
                AssignTypeToRegion(region, regionType);
            }
        }
        private void AssignTypeToRegion(VoronoiRegion region, Region regionType)
        {
            region.regionType = regionType;
            region.nodes = region.nodes.Select(node =>
            {
                node.regionType = regionType;
                return node;
            }).ToList();
        }        
        private void CheckForIsolatedRegions(List<VoronoiRegion> regions, Region regionType)
        {
            // This method could be improved as currently it only solves one scenario, when a region is surrounded completely.
            // But there could be a group of 2 that's completely surrounded, for example
            List<VoronoiRegion> unselectedRegions = regions.ToList();
            unselectedRegions.RemoveAll(region => region.addedToDistrict);

            for (int i = 0; i < 1; i++)
            {
                var addedRegions = new List<VoronoiRegion>();
                foreach (var region in unselectedRegions)
                {
                    if (AllNeighboursAreInTheSameDistrict(region, regionType))
                    {
                        Debug.Log("Fixed in " + i + " iteration");
                        //var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //cube.transform.position = new Vector3(region.centre.x, 2f, region.centre.z);
                        //cube.transform.localScale *= 7f;

                        //foreach (var neighbour in region.neighbourRegions)
                        //{
                        //    var neighbourSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        //    neighbourSphere.transform.position = new Vector3(neighbour.centre.x, 2f, neighbour.centre.z);
                        //    neighbourSphere.transform.localScale *= 4f;
                        //}
                        region.addedToDistrict = true;
                        AssignTypeToRegion(region, regionType);

                        addedRegions.Add(region);
                    }
                }
                foreach (var addedRegion in addedRegions)
                {
                    unselectedRegions.Remove(addedRegion);
                }
            }
        }
        private bool AddRegionToDistrict(VoronoiRegion region, List<VoronoiRegion> districtRegions, ref int nodeCount, int minNodes)
        {
            region.addedToDistrict = true;
            districtRegions.Add(region);
            nodeCount += region.nodes.Count;
            return nodeCount >= minNodes ? true : false;
        }
        private bool AllNeighboursAreInTheSameDistrict(VoronoiRegion region, Region neighbourRegionType)
        {
            return region.neighbourRegions.All(region => region.regionType == neighbourRegionType) && region.regionType != neighbourRegionType;
        }
        private VoronoiRegion SelectBestNeighbour(VoronoiRegion selectedRegion, List<VoronoiRegion> districtRegionList, int maxNodes, int currentNodes, Region regionType)
        {
            VoronoiRegion bestNeighbour = null;
            List<VoronoiRegion> neighbourRegions = regionType == Region.Main ? GetValidMainNeighbours(selectedRegion) : GetValidSuburbsNeighbours(selectedRegion);

            int bestHits = -1;

            foreach (VoronoiRegion candidate in neighbourRegions)
            {
                int currentHits = 0;
                foreach (VoronoiRegion addedRegion in districtRegionList)
                {
                    if (addedRegion.neighbourRegions.Contains(candidate))
                    {
                        currentHits++;
                    }
                }

                if (currentHits > bestHits && IsSizeOk(maxNodes, currentNodes, candidate))
                {
                    bestNeighbour = candidate;
                    bestHits = currentHits;
                }
            }
            return bestNeighbour;
        }
        private List<VoronoiRegion> GetAddedRegionsWithValidNeighbours(List<VoronoiRegion> addedRegions)
        {
            List<VoronoiRegion> regionsWithValidNeighbours = new List<VoronoiRegion>();
            foreach (var region in addedRegions)
            {
                var freeNeighbours = region.neighbourRegions.Where(nr => !nr.addedToDistrict).ToList();
                if (freeNeighbours.Any())
                {
                    VoronoiRegion regionCopy = new VoronoiRegion(region.color, region.point, region.id);
                    foreach (var freeNeighbour in freeNeighbours)
                    {
                        regionCopy.neighbourRegions.Add(freeNeighbour);
                    }
                    regionsWithValidNeighbours.Add(regionCopy);
                }
            }
            return regionsWithValidNeighbours;
        }
        private VoronoiRegion SelectBestNeighbourFromList(List<VoronoiRegion> regions, List<VoronoiRegion> regionsWithValidNeighbours, List<VoronoiRegion> addedDistrictRegions, 
            int maxNodes, int nodeCount, Region regionType)
        {
            // This method does not follow any criteria, the region from regionsWithValidNeighbours is picked randomly.
            VoronoiRegion bestNeighbour = null;
            int maxAttempts = 5;
            for (int currentAttempt = 0; currentAttempt < maxAttempts && bestNeighbour == null; currentAttempt++)
            {
                //Debug.Log("Current Attempt: " + currentAttempt);
                var selectedRegionCopy = regionsWithValidNeighbours[Random.Range(0, regionsWithValidNeighbours.Count)];
                var selectedRegion = regions.Find(region => region.id == selectedRegionCopy.id);
                bestNeighbour = SelectBestNeighbour(selectedRegion, addedDistrictRegions, maxNodes, nodeCount, regionType);
                regionsWithValidNeighbours.Remove(selectedRegionCopy);
            }
            return bestNeighbour;
        }
        private List<VoronoiRegion> GetValidMainNeighbours(VoronoiRegion selectedRegion)
        {
            return selectedRegion.neighbourRegions.Where(neighbour => !neighbour.addedToDistrict).ToList();
        }
        private List<VoronoiRegion> GetValidSuburbsNeighbours(VoronoiRegion selectedRegion)
        {
            return selectedRegion.neighbourRegions.Where(neighbour => !neighbour.addedToDistrict && IsTwoRegionsApartFromType(neighbour, Region.Main)).ToList();
        }
        private bool IsTwoRegionsApartFromType(VoronoiRegion region, Region regionType)
        {
            List<VoronoiRegion> neighbours = region.neighbourRegions.ToList();
            VoronoiRegion desiredTypeRegion = neighbours.Find(neighbour => neighbour.regionType == regionType);
            if (desiredTypeRegion != null)
                return false;

            // No first neighbour was of the exluded type, now look at the exterior neighbours
            foreach(VoronoiRegion neighbour in neighbours)
            {
                // Look at all the neighbours in the selected region
                List<VoronoiRegion> exteriorNeighbours = neighbour.neighbourRegions.ToList();
                desiredTypeRegion = exteriorNeighbours.Find(exteriorNeighbour => exteriorNeighbour.regionType == regionType);
                if (desiredTypeRegion != null)
                    return false;
            }
            return true;
        }
        private bool IsRegionAwayFromType(VoronoiRegion region, int regionsApart, Region targetRegionType)
        {
            Queue<VoronoiRegion> regionsToVisit = new Queue<VoronoiRegion>();
            HashSet<VoronoiRegion> regionsVisited = new HashSet<VoronoiRegion>();

            regionsToVisit.Enqueue(region);
            regionsVisited.Add(region);

            while (regionsToVisit.Count > 0 && regionsApart >= 0)
            {
                int level = regionsToVisit.Count;
                for (int i = 0; i < level; i++)
                {
                    var currentRegion = regionsToVisit.Dequeue();
                    if (currentRegion.regionType == targetRegionType)
                        return false;

                    foreach (var neighbour in currentRegion.neighbourRegions)
                    {
                        if (!regionsVisited.Contains(neighbour))
                        {
                            regionsToVisit.Enqueue(neighbour);
                            regionsVisited.Add(neighbour);
                        }
                    }
                }
                regionsApart--;
            }
            return true;
        }
        private bool IsSizeOk(int maxNodes,int nodeCount, VoronoiRegion region)
        {
            if (nodeCount + region.nodes.Count > maxNodes)
                return false;
            return true;
        }
        private bool IsSizeOk(float maxPercentage, float currentPercentage, VoronoiRegion region)
        {
            float newPercentage = currentPercentage + GetPercentageContained(region);
            if (newPercentage > maxPercentage)
                return false;
            return true;
        }
        private float GetPercentageContained(VoronoiRegion region)
        {
            return ((float)region.nodesContained.Count / (float)totalNodesContained) * 100f;
        }
        private bool HasContainedNodes(VoronoiRegion region)
        {
            return region.nodesContained.Count > 0;
        }
        private bool MeetsEndOfWorld(int startX, int startY, Direction direction)
        {
            Vector2Int key = new Vector2Int(startX, startY);
            int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

            int currentPosX = startX;
            int currentPosY = startY;

            while (true)
            {
                currentPosX += dir[0];
                currentPosY += dir[1];

                // Check if the current position is out of the grid
                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    return true;

                // Get the current grid node
                GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];

                if (currentNode.usage == Usage.EOW)
                    return true;

                if (currentNode.usage == Usage.empty)
                    continue;
                
                return false;
            }
        }
        private bool MeetsDecoration(int startX, int startY, Direction direction)
        {
            int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

            int currentPosX = startX;
            int currentPosY = startY;

            while (true)
            {
                currentPosX += dir[0];
                currentPosY += dir[1];

                // Check if the current position is out of the grid
                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    return true;

                // Get the current grid node
                GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];

                if (currentNode.usage == Usage.EOW)
                    return true;

                if (currentNode.usage == Usage.empty)
                {
                    var decorationNeighbours = Grid.Instance.GetNeighboursInLine(currentNode, new List<Usage>() { Usage.decoration });
                    if (decorationNeighbours.Count > 0)
                    {
                        boundariesToAdd.Add(currentNode);
                        currentNode.usage = Usage.EOW;
                    }
                    continue;
                }

                return false;
            }
        }
        private bool IsContainedInBoundaries(GridNode startNode)
        {
            // Get the grid size
            int gridSizeX = Grid.Instance.gridSizeX;
            int gridSizeY = Grid.Instance.gridSizeY;

            // Determine the direction checking order based on node's position
            List<Direction> checkOrder = GetDirectionCheckOrder(startNode, gridSizeX, gridSizeY);

            // A node is contained in boundaries if it meets a boundary node in all 4 directions     
            foreach (Direction direction in checkOrder)
            {
                // Init pos  
                int currentPosX = startNode.gridX;
                int currentPosY = startNode.gridY;
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                // Advance in direction
                while (true)
                {
                    currentPosX += dir[0];
                    currentPosY += dir[1];

                    // Check if the current position is out of the grid
                    if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                        return false;

                    // Get the current grid node
                    GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];

                    // If contained, check the next direction
                    if (boundaries.Contains(currentNode))
                        break;
                }
            }
            // If this part is reached, all directions lead to boundaries
            return true;
        }
        private List<Direction> GetDirectionCheckOrder(GridNode node, int gridSizeX, int gridSizeY)
        {
            List<Direction> directions = new List<Direction>();

            // Determine whether the node is closer to the left or right edge of the grid
            if (node.gridX < gridSizeX / 2)
            {
                directions.Add(Direction.left);  // Closer to the left side, check left first
                directions.Add(Direction.right); // Check right later
            }
            else
            {
                directions.Add(Direction.right); // Closer to the right side, check right first
                directions.Add(Direction.left);  // Check left later
            }

            // Determine whether the node is closer to the top or bottom edge of the grid
            if (node.gridY < gridSizeY / 2)
            {
                directions.Add(Direction.back);  // Closer to the bottom side, check down first
                directions.Add(Direction.forward);    // Check up later
            }
            else
            {
                directions.Add(Direction.forward);    // Closer to the top side, check up first
                directions.Add(Direction.back);  // Check down later
            }

            return directions;
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
    }

    public enum Region
    {
        Main,
        Residential,
        Suburbs
    }
}

