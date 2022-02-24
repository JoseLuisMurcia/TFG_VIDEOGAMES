using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterTriggerArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // FIND THE CAR THAT HAS COLLIDED WITH THE TRIGGER AND SUBSCRIBE IT TO THE ROADTRIGGERENTER
        TrafficLightCarController carController = other.gameObject.GetComponent<TrafficLightCarController>();
        carController.SubscribeToTrafficLight();
    }


}