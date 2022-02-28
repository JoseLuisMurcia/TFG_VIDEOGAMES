using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitTriggerArea : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        // FIND THE CAR THAT HAS COLLIDED WITH THE TRIGGER AND UNSUBSCRIBE IT TO THE ROADTRIGGERENTER
        TrafficLightCarController carController = other.gameObject.GetComponent<TrafficLightCarController>();
        carController.UnsubscribeToTrafficLight();
    }
}
