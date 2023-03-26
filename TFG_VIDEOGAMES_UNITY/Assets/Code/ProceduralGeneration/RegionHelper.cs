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

        private int centerX, centerY;
        private float centreDistance;
        private float residentialDistance;
        private Vector3 centrePosition;

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
        public RegionHelper(int _centerX, int _centerY, Grid grid)
        {
            centerX = _centerX;
            centerY = _centerY;

            Vector3 startPosition = grid.nodesGrid[0, 0].worldPosition;
            centrePosition = grid.nodesGrid[centerX, centerY].worldPosition;
            centreDistance = Vector3.Distance(startPosition, centrePosition) / 3f; // Everything equal or lower than this distance will be the center of the city
            residentialDistance = centreDistance * 1.5f; // Everything equal or lower than this distance will be a residential area
            centreDistance *= 0.8f;
        }
        public void SetRegions(List<VoronoiRegion> regions)
        {
            /* Define the main city district */
            // Create params
            int mainDistrictMinNodes = 5000;
            int mainDistrictMaxNodes = 10000;
            int nodeCount;

            // Select the first polygon
            int firstId = Random.Range(0, regions.Count);

            // Add adjacent polygons to the main district 
            List<VoronoiRegion> mainDistrictRegions = new List<VoronoiRegion>
            {
                regions[firstId]
            };
            nodeCount = regions[firstId].nodes.Count;
            bool conditionsMet = false;
            HashSet<int> candidatesIds = new HashSet<int>
            {
                firstId
            };
            while (!conditionsMet)
            {
                // REVISAR FALLO, LOS VECINOS NO SE AGREGAN BIEN? SI YO SOY TU VECINO, TU DEBES SER MI VECINO, TIENE QUE SER BIDIRECCIONAL.
                // COMPROBAR NO REPETIR COMO CANDIDATO SI YA ESTÁ EN MAINDISTRICTREGIONS
                List<VoronoiRegion> neighbourRegions = mainDistrictRegions[mainDistrictRegions.Count-1].neighbourRegions.ToList();

                // Get the best candidate from the current neighbour
                // Compare the matches with all the mainDistrictRegions
                VoronoiRegion bestCandidate = null;
                int bestHits = -1;
                foreach (VoronoiRegion candidate in neighbourRegions)
                {
                    int currentHits = 0;
                    foreach(VoronoiRegion addedRegions in mainDistrictRegions)
                    {
                        if(addedRegions.neighbourRegions.Contains(candidate))
                        {
                            currentHits++;
                        }
                    }
                    if(currentHits > bestHits && CanBeAdded(mainDistrictMaxNodes, nodeCount, candidate) && !candidatesIds.Contains(candidate.id))
                    {
                        bestCandidate = candidate;
                        bestHits = currentHits;
                    }
                }

                candidatesIds.Add(bestCandidate.id);

                mainDistrictRegions.Add(bestCandidate);
                nodeCount += bestCandidate.nodes.Count;
                conditionsMet = nodeCount >= mainDistrictMinNodes ? true : false; 
            }
            foreach (VoronoiRegion region in mainDistrictRegions)
            {
                region.regionType = Region.Main;
                AddRegionToDistrict(region, Region.Main);
            }

            // Define the suburbs
            int suburbsMinNodes = 1000;
            int suburbsMaxNodes = 2000;
            List <VoronoiRegion> freeRegions = new List<VoronoiRegion>();
            foreach(VoronoiRegion region in regions)
            {
                if (!mainDistrictRegions.Contains(region))
                    freeRegions.Add(region);
            }

            // Can't have a main city district neighbour, for the first region,
            bool found = false;
            VoronoiRegion firstRegion = null;
            while (!found)
            {
                firstId = Random.Range(0, freeRegions.Count);
                firstRegion = freeRegions[firstId];
                if (IsTwoRegionsApartFromType(firstRegion, Region.Main))
                    found = true;
            }
            // Initial region found, now iterate through neighbours
            conditionsMet = false;
            List<VoronoiRegion> firstSuburbsRegion = new List<VoronoiRegion>
            {
                firstRegion
            };
            candidatesIds = new HashSet<int>
            {
                firstId
            };
            nodeCount = 0;
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
                    if (currentHits > bestHits && CanBeAdded(suburbsMaxNodes, nodeCount, candidate) && !candidatesIds.Contains(candidate.id))
                    {
                        bestCandidate = candidate;
                        bestHits = currentHits;
                    }
                }

                candidatesIds.Add(bestCandidate.id);

                firstSuburbsRegion.Add(bestCandidate);
                nodeCount += bestCandidate.nodes.Count;
                conditionsMet = nodeCount >= suburbsMinNodes ? true : false;
            }
            foreach (VoronoiRegion region in firstSuburbsRegion)
            {
                region.regionType = Region.Suburbs;
                AddRegionToDistrict(region, Region.Suburbs);
            }

            // Están cogiendo de vecinos a regiones con distrito main
            // Hay que modulizar el código, debe estar en funciones que hagan eso mejor.
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
        public void SetBoundaries(Node node)
        {
            int nodeX = node.gridX;
            int nodeY = node.gridY;
            
            // New node is to the left
            if(nodeX < centerX)
            {
                if (DownLeft.x < nodeX)
                    return;

                if(nodeY < centerY) // New node is down left
                {
                    if(nodeY < DownLeft.y)
                    {
                        DownLeft = new Vector2Int(nodeX, nodeY);
                    }
                }
                else // New node is up left
                {
                    if (nodeY > DownLeft.y)
                    {
                        UpLeft = new Vector2Int(nodeX, nodeY);
                    }
                }
            }
            else // New node is to the right
            {
                if (DownLeft.x > nodeX)
                    return;

                if (nodeY < centerY) // New node is down right
                {
                    if (nodeY < DownLeft.y)
                    {
                        DownRight = new Vector2Int(nodeX, nodeY);
                    }
                }
                else // New node is up right
                {
                    if (nodeY > DownLeft.y)
                    {
                        UpRight = new Vector2Int(nodeX, nodeY);
                    }
                }
            }
        }
        public void SetRegionToNode(Node node)
        {
            float distanceToCentre = Vector3.Distance(centrePosition, node.worldPosition);
            if(distanceToCentre <= centreDistance)
            {
                node.region = Region.Main;
            }
            else if(distanceToCentre <= residentialDistance)
            {
                node.region = Region.Residential;
            }
            else
            {
                node.region = Region.Suburbs;
            }
        }
        public List<Vector2Int> GetBoundaries()
        {
            return new List<Vector2Int> { DownLeft, DownRight, UpLeft, UpRight };
        }
    }

    public enum Region
    {
        Main,
        Residential,
        Suburbs
    }
}

