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
	public LaneSide laneSide;

	public Node(Vector3 _worldPos, Road _road)
	{
		worldPosition = _worldPos;
		road = _road;
	}

	public void SetLaneSide()
    {
		if(road.numberOfLanes > 1 && road.numDirection == NumDirection.OneDirectional)
        {
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
		neighbours.Add(neighbour);

		if(neighbour.previousNode == null)
			neighbour.previousNode = this;
    }
}
