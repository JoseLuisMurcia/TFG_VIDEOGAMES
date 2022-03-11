using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
	public Vector3 worldPosition;

	public float gCost;
	public float hCost;
	public Node parent;
	int heapIndex;
	public bool hasTrafficLightClose;
	public Road road;
	public List<Node> neighbours = new List<Node>();


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
}
