using System.Collections;
using System.Collections.Generic;
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
                node.region = Region.Center;
            }
            else if(distanceToCentre <= residentialDistance)
            {
                node.region = Region.Residential;
            }
            else
            {
                node.region = Region.Outskirts;
            }
        }
        public List<Vector2Int> GetBoundaries()
        {
            return new List<Vector2Int> { DownLeft, DownRight, UpLeft, UpRight };
        }
    }

    public enum Region
    {
        Center,
        Residential,
        Outskirts
    }
}

