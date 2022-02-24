using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    private void OnRoadTriggerEnter()
    {
        // set the actual road
    }

    private void OnRoadTriggerExit()
    {
        // set the actual road
    }

    public void SubscribeToTrafficLight()
    {
        TrafficLightEvents.instance.onRoadTriggerEnter += OnRoadTriggerEnter;

    }

    public void UnsubscribeToTrafficLight()
    {
        TrafficLightEvents.instance.onRoadTriggerExit -= OnRoadTriggerEnter;

    }
}
