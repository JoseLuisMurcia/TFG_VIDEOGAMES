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
        public RegionHelper(int _centerX, int _centerY)
        {
            centerX = _centerX;
            centerY = _centerY;
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

