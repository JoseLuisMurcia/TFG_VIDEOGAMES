using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IHeapItem<Waypoint>
{
    //public bool walkable;
    public Waypoint prevWaypoint;
    public Waypoint nextWaypoint;

    [Range(0f, 5f)]
    public float width = 1f;

    [Range(0f, 1f)]
    public float branchRatio = 0.5f;

    public List<Waypoint> branches = new List<Waypoint>();

    public List<Connection> connections = new List<Connection>();

    public float gCost;
    public float hCost;
    public Waypoint pathfindingParent;
    int heapIndex;

    private void Start()
    {
        if(nextWaypoint != null)
        {
            connections.Add(new Connection(this, nextWaypoint));
        }
        foreach(Waypoint branch in branches)
        {
            connections.Add(new Connection(this, branch));
        }

    }

    public Vector3 GetPosition()
    {
        return transform.position;

        //Vector3 minBound = transform.position + transform.right * width * 0.5f;
        //Vector3 maxBound = transform.position - transform.right * width * 0.5f;

        //return Vector3.Lerp(minBound, maxBound, Random.Range(0f, 0.1f));
    }

    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex { get { return heapIndex; } set { heapIndex = value; } }

    public int CompareTo(Waypoint nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}

