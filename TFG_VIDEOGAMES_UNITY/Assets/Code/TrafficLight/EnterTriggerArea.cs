using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterTriggerArea : MonoBehaviour
{
    private Road road;

    private void Start()
    {
        road = GetComponentInParent<Road>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (road.trafficLight == null)
            return;
        // FIND THE CAR THAT HAS COLLIDED WITH THE TRIGGER AND SUBSCRIBE IT TO THE ROADTRIGGERENTER
        TrafficLightCarController carController = other.gameObject.GetComponent<TrafficLightCarController>();

        if (carController == null)
            return;

        // Hacer un check con el producto escalar, si sale negativo no debería suscribirse
        Vector3 carForward = carController.transform.forward;
        Vector3 dirToMovePosition = (road.trafficLight.transform.position - carController.transform.position).normalized;
        float dot = Vector3.Dot(carForward, dirToMovePosition);
        if(dot > 0)
        {
            carController.SubscribeToTrafficLight(road);
        }
    }

}
