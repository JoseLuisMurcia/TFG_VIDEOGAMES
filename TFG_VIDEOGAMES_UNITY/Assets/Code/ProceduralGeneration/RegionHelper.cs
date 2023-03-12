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

        // AHORA SE VA A HACER UTILIZANDO LOS POL�GONOS DE VORONOI HEHEHEHEH
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
            int mainDistrictMinNodes = 10000;
            int mainDistrictMaxNodes = 15000;
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
            while (!conditionsMet)
            {
                // REVISAR FALLO, LOS VECINOS NO SE AGREGAN BIEN? SI YO SOY TU VECINO, TU DEBES SER MI VECINO, TIENE QUE SER BIDIRECCIONAL.
                // COMPROBAR NO REPETIR COMO CANDIDATO SI YA EST� EN MAINDISTRICTREGIONS
                List<VoronoiRegion> neighbourRegions = mainDistrictRegions[mainDistrictRegions.Count-1].neighbourRegions.ToList();
                int neighbourId = Random.Range(0, neighbourRegions.Count);
                VoronoiRegion candidateRegion = regions[neighbourId];
                while (!CanBeAdded(mainDistrictMaxNodes, nodeCount, candidateRegion))
                {
                    candidateRegion = regions[Random.Range(0, neighbourRegions.Count)];
                }
                mainDistrictRegions.Add(candidateRegion);
                nodeCount += candidateRegion.nodes.Count;
                conditionsMet = nodeCount >= mainDistrictMinNodes ? true : false; 
            }

            // Define the suburbs
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

