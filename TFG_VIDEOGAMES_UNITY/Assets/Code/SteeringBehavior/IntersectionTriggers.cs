using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTriggers : MonoBehaviour
{
    bool hasTrafficLight = false;
    public Road parentRoad;
    Road belongingRoad = null;

    void Start()
    {
        transform.LookAt(transform.parent, Vector3.up);
        StartCoroutine(FindBelongingRoad());
    }

    // Try to find the road that this intersection is placed in
    private IEnumerator FindBelongingRoad()
    {
        yield return new WaitForSeconds(1.5f);
        float bestDistance = Mathf.Infinity;
        foreach (Road neighbour in parentRoad.connections)
        {
            if (neighbour == null || parentRoad == null)
            {
                Destroy(gameObject);
                break;
            }
            float distance = Vector3.Distance(neighbour.transform.position, transform.position);
            if (distance < bestDistance)
            {
                belongingRoad = neighbour;
                bestDistance = distance;
            }
        }
        if (parentRoad != null && belongingRoad != null)
        {
            if (belongingRoad.trafficLights.Count > 0)
            {
                hasTrafficLight = true;
                DestroyIfTrafficLight();
            }
        }
        
    }

    private void DestroyIfTrafficLight()
    {
        Vector3 triggerForward = transform.forward;
        foreach (var trafficLight in belongingRoad.trafficLights)
        {
            Vector3 trafficLightForward = trafficLight.transform.forward;
            float angle = Vector3.Angle(triggerForward, trafficLightForward);
            if (angle > 145f)
            {
                Destroy(gameObject);
                break;
            }
        }      
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
                foreach (var trafficLight in belongingRoad.trafficLights)
                {
                    Vector3 trafficLightForward = trafficLight.transform.forward;
                    float angleBetweenCarForwardAndTrafficLightForward = Vector3.Angle(carForward, trafficLightForward);
                    if (angleBetweenCarForwardAndTrafficLightForward > 145f)
                    {
                        carManager.intersectionInSight = true;
                        break;
                    }
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
