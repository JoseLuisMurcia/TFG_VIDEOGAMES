using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class should know where the car is right now. It should be notified when there is a change in a traffic light, or a car is too close.
public class TrafficLightCarController : MonoBehaviour
{
    public Path path;

    // Use the nodes in the path to know the current road
    public List<Node> nodes;

    PathFollower pathFollower;

    private Road currentRoad;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }


    // WORK FROM HERE
    private void OnTrafficLightChange(TrafficLightColor newColor)
    {
        Debug.Log("THE TRAFFIC LIGHT HAS CHANGED TO: " + newColor);
    }

    public void SubscribeToTrafficLight(Road _newRoad)
    {
        Debug.Log("SubscribeToTrafficLight");
        currentRoad = _newRoad;
        currentRoad.trafficLightEvents.onLightChange += OnTrafficLightChange;

    }

    public void UnsubscribeToTrafficLight()
    {
        Debug.Log("UnsubscribeToTrafficLight");
        currentRoad.trafficLightEvents.onLightChange -= OnTrafficLightChange;
        currentRoad = null;
    }
}
