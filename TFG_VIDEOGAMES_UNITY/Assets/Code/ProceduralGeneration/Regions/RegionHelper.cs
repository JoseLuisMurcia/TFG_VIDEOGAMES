using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class RegionHelper
    {
        private List<VoronoiRegion> regions;
        private List<VoronoiRegion> mainRegions;
        private List<VoronoiRegion> gangRegions;
        private HashSet<GridNode> boundaries;
        private static List<Direction> allDirections = new List<Direction>() { Direction.left, Direction.right, Direction.back, Direction.forward };
        /* ALGORITHM 
         * 1 - Define parameters:
         * Range for districts size (min 100 nodes, max 200)
         * 2 - Select a random polygon that is in the road structure
         * Select one of his neighbours and add him to a list of selected polygons for city district
         * Do the process until the requirements are met, and then, make all those nodes belong to the main city district
         * 3 - Repeat 2 for the suburbs, keep in mind that it should not be conected to the main city district
         * 4 - Every other node should be residential
         */
        public void SetRegions(List<VoronoiRegion> regions)
        {
            this.regions = regions;
            CreateMainDistrict(regions);
            CreateGangDistrict(regions);
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


            // With the boundaries, we can check which nodes of the generated regions fall inside
            for (int i = 0; i < Grid.Instance.gridSizeX; i++)
            {
                for (int j = 0; j < Grid.Instance.gridSizeY; j++)
                {
                    GridNode currentNode = Grid.Instance.nodesGrid[i, j];
                    if (boundaries.Contains(currentNode))
                        continue;

                    if (IsContainedInBoundaries(currentNode))
                    {
                        Vector3 rayOrigin = currentNode.worldPosition + Vector3.up * 20f;
                        Debug.DrawRay(rayOrigin, Vector3.down * 30f, Color.black, 50f);
                    }
                }
            }
            // Regenerate to reach a minimum standard
        }
        private void CreateMainDistrict(List<VoronoiRegion> regions)
        {
            /* Define the main city district */
            // Create params
            int mainDistrictMinNodes = 4000;
            int mainDistrictMaxNodes = 8000;

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
            // Define the suburbs
            int suburbsMinNodes = 1000;
            int suburbsMaxNodes = 2000;
            List<VoronoiRegion> freeRegions = new List<VoronoiRegion>();
            foreach (VoronoiRegion region in regions)
            {
                if (!region.addedToDistrict)
                    freeRegions.Add(region);
            }

            bool found = false;
            VoronoiRegion firstRegion = null;
            //Debug.Log("CreateSuburbs");
            while (!found)
            {
                int firstId = Random.Range(0, freeRegions.Count);
                firstRegion = freeRegions[firstId];
                if (IsRegionAwayFromType(firstRegion, 2, Region.Main))
                    found = true;
            }
            bool conditionsMet = false;
            gangRegions = new List<VoronoiRegion>
            {
                firstRegion
            };

            int nodeCount = 0;
            while (!conditionsMet)
            {
                VoronoiRegion selectedRegion = gangRegions.Last();

                VoronoiRegion bestNeighbour = SelectBestNeighbour(selectedRegion, gangRegions, suburbsMaxNodes, nodeCount, Region.Suburbs);

                if (bestNeighbour == null)
                {
                    List<VoronoiRegion> regionsWithValidNeighbours = GetAddedRegionsWithValidNeighbours(gangRegions);

                    Debug.LogWarning("Retrying to assign a new region");
                    bestNeighbour = SelectBestNeighbourFromList(regions, regionsWithValidNeighbours, gangRegions, suburbsMaxNodes, nodeCount, Region.Main);

                    if (bestNeighbour == null)
                    {
                        Debug.LogError("NO BEST CANDIDATE FOUND, ABORTING EXECUTION");
                        break;
                    }
                    else
                    {
                        conditionsMet = AddRegionToDistrict(bestNeighbour, gangRegions, ref nodeCount, suburbsMinNodes);
                    }
                }
                else
                {
                    conditionsMet = AddRegionToDistrict(bestNeighbour, gangRegions, ref nodeCount, suburbsMinNodes);
                }

            }
            //Debug.Log("Node count: " + nodeCount);
            AssignTypeToRegions(gangRegions, Region.Suburbs);
            CheckForIsolatedRegions(regions, Region.Suburbs);
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
                Debug.Log("Current Attempt: " + currentAttempt);
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
                        boundaries.Add(currentNode);
                        currentNode.usage = Usage.EOW;
                    }
                    continue;
                }

                return false;
            }
        }
        private bool IsContainedInBoundaries(GridNode startNode)
        {
            // A node is contained in boundaries if it meets a boundary node in all 4 directions     
            foreach (Direction direction in allDirections)
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

                    // If contained, check next direction
                    if (boundaries.Contains(currentNode))
                        break;
                }
            }
            // If this part is reached, all directions lead to boundaries
            return true;
        }
    }

    public enum Region
    {
        Main,
        Residential,
        Suburbs
    }
}

