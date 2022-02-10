using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}

public class Connection
{
    Waypoint fromWaypoint;
    Waypoint toWaypoint;
    public float GetCost()
    {
        return Vector3.Distance(fromWaypoint.GetPosition(), toWaypoint.GetPosition());
    }
}
