using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTriggers : MonoBehaviour
{
    public Road parentRoad = null; // Una interseccion
    public Road belongingRoad = null; // La carretera donde está el trigger (Puede tener traffic lights)

    void Start()
    {
        transform.LookAt(transform.parent, Vector3.up);
        StartCoroutine(FindBelongingRoad());
    }

    // Try to find the road that this intersection is placed in
    private IEnumerator FindBelongingRoad()
    {
        yield return new WaitForSeconds(1.5f);

        if (parentRoad != null && belongingRoad != null)
        {
            // Carretera cuyos unicos neighbours son trafficLights
            if (belongingRoad.trafficLights.Count > 1)
            {
                Destroy(gameObject);
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

            if (angleFromCarToIntersection < 45f)
            {
                if (belongingRoad.trafficLights.Count > 0)
                {
                    CarTrafficLight trafficLight = belongingRoad.trafficLights[0];
                    float angleToTrafficLight = Vector3.Angle(carForward, trafficLight.transform.forward);

                    // Facing the intersection without trafficLight
                    if (angleToTrafficLight <= 150f)
                    {
                        carManager.intersectionInSight = true;
                    }
                }
                else
                {
                    carManager.intersectionInSight = true;
                }
            }
        }

    }
}
