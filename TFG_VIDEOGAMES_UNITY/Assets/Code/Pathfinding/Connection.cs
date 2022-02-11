using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection: MonoBehaviour
{
    public Waypoint sourceWaypoint;
    public Waypoint destWaypoint;

    public Connection(Waypoint src, Waypoint dst)
    {
        sourceWaypoint = src;
        destWaypoint = dst;
    }
    public float GetCost()
    {
        return Vector3.Distance(sourceWaypoint.GetPosition(), destWaypoint.GetPosition());
    }
}

