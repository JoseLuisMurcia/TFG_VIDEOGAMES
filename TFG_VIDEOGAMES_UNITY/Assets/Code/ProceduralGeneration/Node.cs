using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Node : IHeapItem<Node>
    {
        public Vector3 worldPosition;
        public float gCost;
        public float hCost;
        public Node parent;
        int heapIndex;
        public bool occupied = false;
        public Usage usage = Usage.empty;
        public int gridX, gridY;
        public Straight belongingStraight = null;
        public Region region = Region.Residential;
        public VoronoiRegion voronoiRegion = null;
        public Node(Vector3 _worldPos, int _gridX, int _gridY)
        {
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
    }
}