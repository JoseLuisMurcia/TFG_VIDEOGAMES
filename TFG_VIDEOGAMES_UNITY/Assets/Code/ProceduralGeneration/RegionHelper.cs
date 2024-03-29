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

        // AHORA SE VA A HACER UTILIZANDO LOS POLÍGONOS DE VORONOI HEHEHEHEH
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
            List<VoronoiRegion> mainDistrictRegions = CreateMainDistrict(regions);
            CreateSuburbs(regions, mainDistrictRegions);
        }

        private List<VoronoiRegion> CreateMainDistrict(List<VoronoiRegion> regions)
        {
            /* Define the main city district */
            // Create params
            int mainDistrictMinNodes = 5000;
            int mainDistrictMaxNodes = 9000;

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

            while (!conditionsMet)
            {
                // Lista que tiene las regiones que se van a comparar para ver cual es mejor elección para el distrito.
                VoronoiRegion selectedRegion = mainDistrictRegions[mainDistrictRegions.Count - 1];

                // Get the best candidate from the current neighbour
                VoronoiRegion bestNeighbour = SelectBestNeighbour(selectedRegion, mainDistrictRegions, mainDistrictMaxNodes, nodeCount);

                // Puede ser que la región seleccionada ya no tenga vecinos válidos
                if (bestNeighbour == null)
                {
                    // Variable que contiene si para una region seleccionada, todos sus vecinos ya están contenidos en mainDistrictRegions
                    bool allCandidateRegionsAreAlreadyAdded = selectedRegion.neighbourRegions.All(neighbourRegion => mainDistrictRegions.Contains(neighbourRegion));

                    // This list holds a COPY of the original regions
                    List<VoronoiRegion> regionsWithValidNeighbours = new List<VoronoiRegion>();
                    if (allCandidateRegionsAreAlreadyAdded)
                    {
                        foreach (var region in mainDistrictRegions)
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
                    }
                    else
                    {
                        Debug.LogError("bestNeighbour wasn't found");
                    }

                    // No se sigue ningun criterio para seleccionar candidatos, pero se podría elegir a aquellos que tuvieran más o menos vecinos libres.
                    Debug.LogWarning("Retrying to assign a new region");
                    int maxAttempts = 5;

                    for (int currentAttempt = 0; currentAttempt < maxAttempts && bestNeighbour == null; currentAttempt++)
                    {
                        Debug.Log("Current Attempt: " + currentAttempt);
                        // Select best neighbour
                        var selectedRegionCopy = regionsWithValidNeighbours[Random.Range(0, regionsWithValidNeighbours.Count)];
                        selectedRegion = regions.Find(region => region.id == selectedRegionCopy.id);
                        bestNeighbour = SelectBestNeighbour(selectedRegion, mainDistrictRegions, mainDistrictMaxNodes, nodeCount);
                        regionsWithValidNeighbours.Remove(selectedRegionCopy);
                    }

                    if (bestNeighbour == null)
                    {
                        Debug.LogError("NO BEST CANDIDATE FOUND, ABORTING EXECUTION");
                        conditionsMet = true;
                    }
                    else
                    {
                        bestNeighbour.addedToDistrict = true;
                        mainDistrictRegions.Add(bestNeighbour);
                        nodeCount += bestNeighbour.nodes.Count;
                        conditionsMet = nodeCount >= mainDistrictMinNodes ? true : false;
                    }
                }
                else
                {
                    bestNeighbour.addedToDistrict = true;
                    mainDistrictRegions.Add(bestNeighbour);
                    nodeCount += bestNeighbour.nodes.Count;
                    conditionsMet = nodeCount >= mainDistrictMinNodes ? true : false;
                }

            }
            foreach (VoronoiRegion region in mainDistrictRegions)
            {
                region.regionType = Region.Main;
                AddRegionToDistrict(region, Region.Main);
            }

            List<VoronoiRegion> unselectedRegions = regions.ToList();
            unselectedRegions.RemoveAll(region => region.addedToDistrict);

            // BUG 1 - EL CENTRE NO ESTÁ SETEADO
            for (int i = 0; i < 4; i++)
            {
                var addedRegions = new List<VoronoiRegion>();
                foreach (var region in unselectedRegions)
                {
                    if (AllNeighboursAreInTheSameDistrict(region, Region.Main))
                    {
                        Debug.Log("Fixed in " + i + " iteration");
                        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.position = new Vector3(region.centre.x, 2f, region.centre.z);
                        cube.transform.localScale *= 7f;

                        foreach (var neighbour in region.neighbourRegions)
                        {
                            var neighbourSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            neighbourSphere.transform.position = new Vector3(neighbour.centre.x, 2f, neighbour.centre.z);
                            neighbourSphere.transform.localScale *= 4f;
                        }
                        region.addedToDistrict = true;
                        region.regionType = Region.Main;
                        addedRegions.Add(region);
                        AddRegionToDistrict(region, Region.Main);
                    }
                }
                foreach (var addedRegion in addedRegions)
                {
                    unselectedRegions.Remove(addedRegion);
                }
            }
            return mainDistrictRegions;
        }
        private bool AllNeighboursAreInTheSameDistrict(VoronoiRegion region, Region neighbourRegionType)
        {
            return region.neighbourRegions.All(region => region.regionType == neighbourRegionType) && region.regionType != neighbourRegionType;
        }
        private VoronoiRegion SelectBestNeighbour(VoronoiRegion selectedRegion, List<VoronoiRegion> mainDistrictRegions, int maxNodes, int currentNodes)
        {
            VoronoiRegion bestNeighbour = null;
            List<VoronoiRegion> neighbourRegions = selectedRegion.neighbourRegions.ToList();
            neighbourRegions.RemoveAll(region => region.addedToDistrict);

            int bestHits = -1;

            foreach (VoronoiRegion candidate in neighbourRegions)
            {
                int currentHits = 0;
                foreach (VoronoiRegion addedRegion in mainDistrictRegions)
                {
                    if (addedRegion.neighbourRegions.Contains(candidate))
                    {
                        currentHits++;
                    }
                }
                //if (AllNeighboursAreInTheSameDistrict(selectedRegion, Region.Main))
                //{
                //    return candidate;
                //}
                if (currentHits > bestHits && CanBeAdded(maxNodes, currentNodes, candidate))
                {
                    bestNeighbour = candidate;
                    bestHits = currentHits;
                }
            }
            return bestNeighbour;
        }
        private void CreateSuburbs(List<VoronoiRegion> regions, List<VoronoiRegion> mainDistrictRegions)
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

            // Can't have a main city district neighbour, for the first region,
            bool found = false;
            VoronoiRegion firstRegion = null;
            int firstId = -1;
            while (!found)
            {
                firstId = Random.Range(0, freeRegions.Count);
                firstRegion = freeRegions[firstId];
                if (IsTwoRegionsApartFromType(firstRegion, Region.Main))
                    found = true;
            }
            // Initial region found, now iterate through neighbours
            bool conditionsMet = false;
            List<VoronoiRegion> firstSuburbsRegion = new List<VoronoiRegion>
            {
                firstRegion
            };
            HashSet<int> mainDistrictRegionsIds = new HashSet<int>
            {
                firstId
            };
            int nodeCount = 0;
            while (!conditionsMet)
            {
                List<VoronoiRegion> neighbourRegions = firstSuburbsRegion[firstSuburbsRegion.Count - 1].neighbourRegions.ToList();
                var mainDistrictRegionsInNeighbours = neighbourRegions.FindAll(region => region.regionType == Region.Main);
                // always unable to add a region if it has a main district neighbour.
                foreach (VoronoiRegion region in mainDistrictRegionsInNeighbours)
                    neighbourRegions.Remove(region);

                // Get the best candidate from the current neighbour
                // Compare the matches with all the firstSuburbsRegion
                VoronoiRegion bestCandidate = null;
                int bestHits = -1;
                foreach (VoronoiRegion candidate in neighbourRegions)
                {
                    int currentHits = 0;

                    foreach (VoronoiRegion addedRegions in firstSuburbsRegion)
                    {
                        if (addedRegions.neighbourRegions.Contains(candidate))
                        {
                            currentHits++;
                        }
                    }
                    if (currentHits > bestHits && CanBeAdded(suburbsMaxNodes, nodeCount, candidate) && !mainDistrictRegionsIds.Contains(candidate.id))
                    {
                        bestCandidate = candidate;
                        bestHits = currentHits;
                    }
                }

                mainDistrictRegionsIds.Add(bestCandidate.id);

                firstSuburbsRegion.Add(bestCandidate);
                nodeCount += bestCandidate.nodes.Count;
                conditionsMet = nodeCount >= suburbsMinNodes ? true : false;
            }
            foreach (VoronoiRegion region in firstSuburbsRegion)
            {
                region.regionType = Region.Suburbs;
                AddRegionToDistrict(region, Region.Suburbs);
            }
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
                desiredTypeRegion = exteriorNeighbours.Find(neighbour => neighbour.regionType == regionType);
                if (desiredTypeRegion != null)
                    return false;
            }
            return true;
        }
        private void AddRegionToDistrict(VoronoiRegion region, Region regionType)
        {
            foreach(Node node in region.nodes)
            {
                node.region = regionType;
            }
        }
        private bool CanBeAdded(int maxNodes,int nodeCount, VoronoiRegion region)
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

