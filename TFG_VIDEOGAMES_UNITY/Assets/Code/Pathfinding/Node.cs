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

	public Node(Vector3 _worldPos, Road _road)
	{
		worldPosition = _worldPos;
		road = _road;
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
