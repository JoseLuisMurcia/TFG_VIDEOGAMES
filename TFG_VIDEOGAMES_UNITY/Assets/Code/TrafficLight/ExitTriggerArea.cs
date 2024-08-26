using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitTriggerArea : MonoBehaviour
{
    private CarTrafficLight trafficLight;
    private void OnTriggerExit(Collider other)
    {
        // FIND THE CAR THAT HAS COLLIDED WITH THE TRIGGER AND UNSUBSCRIBE IT TO THE ROADTRIGGERENTER
        TrafficLightCarController carController = other.gameObject.GetComponent<TrafficLightCarController>();
        if (carController == null)
            return;

        if (carController.trafficLight != null && carController.trafficLight == trafficLight)
            carController.UnsubscribeToTrafficLight();
    }
    public void SetTrafficLight(CarTrafficLight _trafficLight)
    {
        trafficLight = _trafficLight;
    }
}