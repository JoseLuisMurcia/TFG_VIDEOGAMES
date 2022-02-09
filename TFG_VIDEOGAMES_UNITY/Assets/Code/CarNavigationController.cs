using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarNavigationController : MonoBehaviour
{
    public bool reachedDestination = true;
    Vector3 destination;
    [SerializeField]
    float speed = 1f;
    [SerializeField]
    float distanceThreshold = 0.05f;

    void Update()
    {
        if (reachedDestination) return;

        if(Vector3.Distance(transform.position, destination) < distanceThreshold)
        {
            reachedDestination = true;
        }
        if (!reachedDestination)
        {
            //SUSTITUIR POR LAS FISICAS
            
        }
    }

    public void SetDestination(Vector3 dest)
    {
        Debug.Log("Prev Destination: " + destination);
        Debug.Log("New Destination: " + dest);
        reachedDestination = false;
        destination = dest;
    }
}
