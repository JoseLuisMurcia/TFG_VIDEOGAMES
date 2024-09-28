using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class GridNode : IHeapItem<GridNode>
    {
        public Vector3 worldPosition;

        public float gCost;
        public float hCost;
        public bool occupied = false;
        public Usage usage = Usage.empty;
        public GridNode parent; // Used by pathfinding
        int heapIndex;
        public int gridX, gridY;
        public bool isProcessedStraight = false;
        public Region regionType = Region.Residential;
        public GridNode previousNode; // Used by grid creation
        public VoronoiRegion voronoiRegion = null;
        public readonly List<GridNode> neighbours = new List<GridNode>();
        public RoadType roadType = RoadType.Default; // Used by roadPlacer and its helpers
        public bool isAlley = false;
        public bool isContained = false;
        public GridNode(Vector3 _worldPos, int _gridX, int _gridY)
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

        public int CompareTo(GridNode nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }

        public void AddNeighbour(GridNode neighbour)
        {
            if (!neighbours.Contains(neighbour))
            {
                neighbours.Add(neighbour);

            }

            if (neighbour.previousNode == null)
                neighbour.previousNode = this;
        }
    }
    public enum RoadType
    {
        Default,
        Roundabout,
        Intersection,
        Bridge
    }
}