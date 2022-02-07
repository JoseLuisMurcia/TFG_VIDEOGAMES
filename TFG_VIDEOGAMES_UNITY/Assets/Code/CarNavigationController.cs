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
            Debug.Log("Position: " + transform.position);
            /*
            Vector3 move = transform.right * destination.x + transform.forward * destination.z;
            controller.Move(move * speed * Time.deltaTime);
            */
            transform.position = Vector3.MoveTowards(transform.position, destination, speed*Time.deltaTime);
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
