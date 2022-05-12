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
        if (carManager != null && !carManager.intersectionInSight)
        {
            Vector3 carForward = carManager.transform.forward.normalized;
            Vector3 carPos = carManager.transform.position;
            Vector3 intersectionPos = transform.parent.position;
            Vector3 dirFromCarToIntersection = (intersectionPos - carPos).normalized;
            float angleFromCarToIntersection = Vector3.Angle(carForward, dirFromCarToIntersection);

            if (angleFromCarToIntersection < 45f)
            {
                carManager.intersectionInSight = true;
                // Tell the pathfollower that it should activate the intersection sensor
            }
        }

    }

    public void DeleteIfTrafficLight(Road connection)
    {
        StartCoroutine(DeleteIfTrafficLightCoroutine(connection));
    }

    private IEnumerator DeleteIfTrafficLightCoroutine(Road connection)
    {
        yield return new WaitForSeconds(1f);
        if (connection.trafficLight != null)
            Destroy(gameObject);
    }
}
