using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class RegionHelper
    {
        public Vector2Int DownLeft = Vector2Int.zero;
        public Vector2Int DownRight = Vector2Int.zero;
        public Vector2Int UpLeft = Vector2Int.zero;
        public Vector2Int UpRight = Vector2Int.zero;

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
            CreateMainDistrict(regions);
            CreateSuburbs(regions);
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
            List<VoronoiRegion> mainDistrictRegions = new List<VoronoiRegion>
            {
                regions[firstId]
            };
            mainDistrictRegions[0].addedToDistrict = true;
            int nodeCount = regions[firstId].nodes.Count;
            bool conditionsMet = false;

            Debug.Log("CreateMainDistrict");

            while (!conditionsMet)
            {
                // Lista que tiene las regiones que se van a comparar para ver cual es mejor elección para el distrito.
                VoronoiRegion selectedRegion = mainDistrictRegions.Last();

                // Get the best candidate from the current neighbour
                VoronoiRegion bestNeighbour = SelectBestNeighbour(selectedRegion, mainDistrictRegions, mainDistrictMaxNodes, nodeCount, Region.Main);

                // Puede ser que la región seleccionada ya no tenga vecinos válidos
                if (bestNeighbour == null)
                {
                    // This list holds a COPY of the original regions
                    List<VoronoiRegion> regionsWithValidNeighbours = GetAddedRegionsWithValidNeighbours(mainDistrictRegions);

                    // No se sigue ningun criterio para seleccionar candidatos, pero se podría elegir a aquellos que tuvieran más o menos vecinos libres.
                    Debug.LogWarning("Retrying to assign a new region");
                    bestNeighbour = SelectBestNeighbourFromList(regions, regionsWithValidNeighbours, mainDistrictRegions, mainDistrictMaxNodes, nodeCount, Region.Main);

                    if (bestNeighbour == null)
                    {
                        Debug.LogError("NO BEST CANDIDATE FOUND, ABORTING EXECUTION");
                        break;
                    }
                    else
                    {
                        conditionsMet = AddRegionToDistrict(bestNeighbour, mainDistrictRegions, ref nodeCount, mainDistrictMinNodes);
                    }
                }
                else
                {
                    conditionsMet = AddRegionToDistrict(bestNeighbour, mainDistrictRegions, ref nodeCount, mainDistrictMinNodes);
                }

            }
            Debug.Log("Node count: " + nodeCount);
            AssignTypeToRegions(mainDistrictRegions, Region.Main);
            CheckForIsolatedRegions(regions, Region.Main);
        }
        private void CreateSuburbs(List<VoronoiRegion> regions)
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
            Debug.Log("CreateSuburbs");
            while (!found)
            {
                int firstId = Random.Range(0, freeRegions.Count);
                firstRegion = freeRegions[firstId];
                if (IsRegionAwayFromType(firstRegion, 2, Region.Main))
                    found = true;
            }
            bool conditionsMet = false;
            List<VoronoiRegion> firstSuburbsDistrictRegions = new List<VoronoiRegion>
            {
                firstRegion
            };

            int nodeCount = 0;
            while (!conditionsMet)
            {
                VoronoiRegion selectedRegion = firstSuburbsDistrictRegions.Last();

                VoronoiRegion bestNeighbour = SelectBestNeighbour(selectedRegion, firstSuburbsDistrictRegions, suburbsMaxNodes, nodeCount, Region.Suburbs);

                if (bestNeighbour == null)
                {
                    List<VoronoiRegion> regionsWithValidNeighbours = GetAddedRegionsWithValidNeighbours(firstSuburbsDistrictRegions);

                    Debug.LogWarning("Retrying to assign a new region");
                    bestNeighbour = SelectBestNeighbourFromList(regions, regionsWithValidNeighbours, firstSuburbsDistrictRegions, suburbsMaxNodes, nodeCount, Region.Main);

                    if (bestNeighbour == null)
                    {
                        Debug.LogError("NO BEST CANDIDATE FOUND, ABORTING EXECUTION");
                        break;
                    }
                    else
                    {
                        conditionsMet = AddRegionToDistrict(bestNeighbour, firstSuburbsDistrictRegions, ref nodeCount, suburbsMinNodes);
                    }
                }
                else
                {
                    conditionsMet = AddRegionToDistrict(bestNeighbour, firstSuburbsDistrictRegions, ref nodeCount, suburbsMinNodes);
                }

            }
            Debug.Log("Node count: " + nodeCount);
            AssignTypeToRegions(firstSuburbsDistrictRegions, Region.Suburbs);
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
    }

    public enum Region
    {
        Main,
        Residential,
        Suburbs
    }
}

