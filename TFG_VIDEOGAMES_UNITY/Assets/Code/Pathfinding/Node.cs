using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node : IHeapItem<Node>
{
    public Vector3 worldPosition;

    public float gCost;
    public float hCost;
    public Node parent; // Used by pathfinding
    int heapIndex;
    public Road road;
    public readonly List<Node> neighbours = new List<Node>();
    public Node previousNode; // Used by grid creation
    public LaneSide laneSide = LaneSide.None;

    public Node(Vector3 _worldPos, Road _road)
    {
        worldPosition = _worldPos;
        road = _road;
    }

    public void SetLaneSide()
    {
        if(road.typeOfRoad == TypeOfRoad.Deviation)
        {
            if (road.numberOfLanes == 2)
            {
                if(road.entryNodes.Contains(this))
                    return;
            }
            
        }
        if (road.typeOfRoad == TypeOfRoad.Split)
            return;

        if (road.numberOfLanes > 1 && road.numDirection == NumDirection.OneDirectional)
        {
            if(road.typeOfRoad == TypeOfRoad.Roundabout)
            {
                if (road.entryNodes.Contains(this) || road.exitNodes.Contains(this))
                {
                    laneSide = LaneSide.None;
                    return;
                }
            }
            if (road.lanes[0].nodes.Contains(this))
            {
                laneSide = LaneSide.Left;
            }
            else
            {
                laneSide = LaneSide.Right;
            }
        }
        else
        {
            laneSide = LaneSide.None;
        }

        if(road.typeOfRoad == TypeOfRoad.Bridge)
        {
            Bridge bridge = (Bridge)road;
            if(bridge.lowerRoadNumLanes > 1 && bridge.lowerNumDirection == NumDirection.OneDirectional)
            {
                if (bridge.lowerLanes[0].nodes.Contains(this))
                {
                    laneSide = LaneSide.Left;
                }
                else if(bridge.lowerLanes[1].nodes.Contains(this))
                {
                    laneSide = LaneSide.Right;
                }
            }

            if (bridge.upperRoadNumLanes > 1 && bridge.upperNumDirection == NumDirection.OneDirectional)
            {
                if (bridge.upperLanes[0].nodes.Contains(this))
                {
                    laneSide = LaneSide.Left;
                }
                else if (bridge.upperLanes[1].nodes.Contains(this))
                {
                    laneSide = LaneSide.Right;
                }
            }

            if (laneSide != LaneSide.None)
                return;

            laneSide = LaneSide.None;
        }
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

    public void AddNeighbour(Node neighbour)
    {
        if (!neighbours.Contains(neighbour))
        {
            neighbours.Add(neighbour);

        }

        if (neighbour.previousNode == null)
            neighbour.previousNode = this;
    }
}
