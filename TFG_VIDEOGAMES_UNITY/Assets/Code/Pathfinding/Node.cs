using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
	public bool walkable;
	public Vector3 worldPosition;
	public int movementPenalty;

	public float gCost;
	public float hCost;
	public Node parent;
	int heapIndex;
	public bool hasTrafficLightClose;
	public bool isRoad = false;
	public TypeOfRoad typeOfRoad = TypeOfRoad.None;
	public Road road;
	public List<Node> neighbours = new List<Node>();


	public Node(bool _walkable,Vector3 _worldPos)
	{
		walkable = _walkable;
		worldPosition = _worldPos;
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
