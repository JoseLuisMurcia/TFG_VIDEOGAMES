using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class should know where the car is right now. It should be notified when there is a change in a traffic light, or a car is too close and it should brake.
public class TrafficLightCarController : MonoBehaviour
{

    // Use the nodes in the path to know the current road
    public List<Node> nodes;

    PathFollower pathFollower;

    private Road currentRoad;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    private void CarDetection()
    {
        // This method should hold the logic to detect a possible collision with another car in front and
        // therefore it should be able to tell the pathFollower to brake. Always keep a safety distance
    }


    // WORK FROM HERE
    private void OnTrafficLightChange(TrafficLightColor newColor)
    {
        switch (newColor)
        {
            case TrafficLightColor.Green:
                // If the car was stopped or braking, put it to movement again, if not, dont do anything
                
                break;
            case TrafficLightColor.Amber:
                // If the car was moving and it is close enough, brake.
                break;
            case TrafficLightColor.Red:
                // If the car is coming to a red traffic light it should break in the closest position to it (Given there is no car in front)
                break;
        }
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
