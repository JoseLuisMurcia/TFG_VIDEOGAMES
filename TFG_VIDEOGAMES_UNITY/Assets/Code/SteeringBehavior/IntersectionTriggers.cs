using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTriggers : MonoBehaviour
{
    // Start is called before the first frame update
    bool hasTrafficLight = false;
    Road belongingRoad = null;
    void Start()
    {
        transform.LookAt(transform.parent, Vector3.up);
        StartCoroutine(FindBelongingRoad());
    }

    private IEnumerator FindBelongingRoad()
    {
        yield return new WaitForSeconds(1f);
        Road road = transform.GetComponentInParent<Road>();
        float bestDistance = Mathf.Infinity;
        foreach (Road neighbour in road.connections)
        {
            float distance = Vector3.Distance(neighbour.transform.position, transform.position);
            if (distance < bestDistance)
            {
                belongingRoad = neighbour;
                bestDistance = distance;
            }
        }
        if (belongingRoad.trafficLight != null)
            hasTrafficLight = true;
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

            if (hasTrafficLight) // SI TIENE TRAFFIC LIGHT SI O SI HAY QUE NO ACEPTARLA? CREO QUE TENGO QUE HACER EL CASO INVERSO
            {
                Vector3 trafficLightForward = belongingRoad.trafficLight.transform.forward;
                float angleBetweenCarForwardAndTrafficLightForward = Vector3.Angle(carForward, trafficLightForward);
                if(angleBetweenCarForwardAndTrafficLightForward < 20)
                {
                    carManager.intersectionInSight = true;
                }
            }
            else
            {
                if (angleFromCarToIntersection < 45f)
                {
                    // Tell the pathfollower that it should activate the intersection sensor
                    carManager.intersectionInSight = true;
                }
            }     
        }

    }
}
