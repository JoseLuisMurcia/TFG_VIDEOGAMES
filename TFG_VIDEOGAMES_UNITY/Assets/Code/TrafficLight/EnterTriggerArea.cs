using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterTriggerArea : MonoBehaviour
{
    private CarTrafficLight trafficLight;

    private void OnTriggerEnter(Collider other)
    {
        if (trafficLight == null)
            return;
        // FIND THE CAR THAT HAS COLLIDED WITH THE TRIGGER AND SUBSCRIBE IT TO THE ROADTRIGGERENTER
        TrafficLightCarController carController = other.gameObject.GetComponent<TrafficLightCarController>();

        if (carController == null)
            return;

        // Hacer un check con el producto escalar, si sale negativo no debería suscribirse
        Vector3 carForward = carController.transform.forward;
        Vector3 dirToMovePosition = (trafficLight.transform.position - carController.transform.position).normalized;
        float dot = Vector3.Dot(carForward, dirToMovePosition);
        if(dot > 0)
        {
            carController.SubscribeToTrafficLight(trafficLight);
        }
    }
    public void SetTrafficLight(CarTrafficLight _trafficLight)
    {
        trafficLight = _trafficLight;
    }

}