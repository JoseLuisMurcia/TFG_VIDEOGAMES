using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An array of connections outgoing from the given node.
// The graph class simply returns an array of connection objects for any node that is queried.
// From these objects the end node and cost can be retrieved.
// A simple implementation of this class would store the connections for each node and
// simply return the list. Each connection would have the cost and end node stored in memory.

// A more complex implementation might calculate the cost only when it is required, using
// information from the current structure of the game level.
// Notice that there is no interface for a Waypoint in this listing, because we don’t need to specify
// one. In many cases it is sufficient just to give nodes a unique number and to use integers as
// the data type. In fact, we will see that this is a particularly powerful implementation because
// it opens up some specific, very fast optimizations of the A * algorithm
public class Graph : MonoBehaviour
{

    // Grafo debe contener todos los waypoints
    [SerializeField] List<Waypoint> graph = new List<Waypoint>();

    public List<Waypoint> path = new List<Waypoint>();
    [SerializeField] WaypointNavigator carNavigator;

    // Crear método que sepa con qué está conectado un nodo. Se deberán crear las Connection cuando se vaya recorriendo el array.
    // El grafo tiene que saber a partir de un nodo, con qué nodos está conectados y el coste de cada conexión.
    public List<Connection> GetConnections(Waypoint fromNode)
    {
        return fromNode.connections;
    }

    public Waypoint FindClosestWaypointFromWorldPoint(Vector3 worldPoint)
    {
        float shortestDistance = Mathf.Infinity;
        Waypoint closestWaypoint = null;
        foreach(Waypoint waypoint in graph)
        {
            float distanceToWorldPoint = Mathf.Abs(Vector3.Distance(worldPoint, waypoint.GetPosition()));
            if (distanceToWorldPoint < shortestDistance)
            {
                closestWaypoint = waypoint;
                shortestDistance = distanceToWorldPoint;
            }
        }
        return closestWaypoint;
    }


    private void OnDrawGizmos()
    {
        foreach (Waypoint waypoint in graph)
        {  
            Gizmos.color = Color.yellow;
            if (path != null)
            {
                if (path.Contains(waypoint))
                    Gizmos.color = Color.red;
            }
            Gizmos.DrawSphere(waypoint.transform.position, .05f);
        }
       
    }

    private void SetPathToCar()
    {
        carNavigator.currentWaypoint = path[0];
    }

}


