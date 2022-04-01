using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTriggers : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Vector3 dirToIntersection = (transform.parent.position - transform.position).normalized;
        transform.LookAt(transform.parent, Vector3.up);
    }

    private void OnTriggerEnter(Collider other)
    {
        WhiskersManager carManager = other.GetComponent<WhiskersManager>();
        if(carManager != null)
        {
            Vector3 carForward = carManager.transform.forward.normalized;
            float angleToIntersection = Vector3.Angle(transform.forward.normalized, carForward);
            if(angleToIntersection < 10)
            {
                carManager.intersectionInSight = true;
                // Tell the pathfollower that it should activate the intersection sensor
            }
        }

    }
}
