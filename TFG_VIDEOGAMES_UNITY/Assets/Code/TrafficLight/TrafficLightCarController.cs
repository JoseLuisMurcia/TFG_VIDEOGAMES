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
    [SerializeField] float distanceToStopInAmberLight = 5f;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    private void OnTrafficLightChange(TrafficLightColor newColor)
    {
        float distance;
        switch (newColor)
        {
            case TrafficLightColor.Green:
                // If the car was stopped or braking, put it to movement again, if not, dont do anything
                pathFollower.shouldStopAtTrafficLight = false;
                break;
            case TrafficLightColor.Amber:
                // If the car was moving and it is close enough, brake.
                distance = CheckDistanceWithTrafficLight(currentRoad.typeOfRoad, currentRoad.trafficLight.transform.position);
                if (distance > distanceToStopInAmberLight)
                {
                    pathFollower.SetTrafficLightPos(currentRoad.trafficLight.transform.position);
                }
                // if distance lesser than X and velocity greater than -> dont break; otherwise -> break
                break;
            case TrafficLightColor.Red:
                // If the car is coming to a red traffic light it should break in the closest position to it (Given there is no car in front)
                pathFollower.SetTrafficLightPos(currentRoad.trafficLight.transform.position);
                break;
        }
        Debug.Log("THE TRAFFIC LIGHT HAS CHANGED TO: " + newColor);
    }

    public float GiveDistanceToPathFollower()
    {
        return CheckDistanceWithTrafficLight(currentRoad.typeOfRoad, currentRoad.trafficLight.transform.position);
    }
    private float CheckDistanceWithTrafficLight(TypeOfRoad typeOfRoad, Vector3 trafficLightPos)
    {
        Vector3 carPosition = transform.position;
        switch (typeOfRoad)
        {
            case TypeOfRoad.Down:
                return Vector2.Distance(new Vector2(0, carPosition.z), new Vector2(0, trafficLightPos.z));
            case TypeOfRoad.Right:
                return Vector2.Distance(new Vector2(carPosition.x, 0), new Vector2(trafficLightPos.x, 0));
            case TypeOfRoad.Left:
                return Vector2.Distance(new Vector2(carPosition.x, 0), new Vector2(carPosition.x, 0));
            case TypeOfRoad.Up:
                return Vector2.Distance(new Vector2(0, carPosition.z), new Vector2(0, trafficLightPos.z));
        }
        return -1f;
    }

    public void SubscribeToTrafficLight(Road _newRoad)
    {
        Debug.Log("SubscribeToTrafficLight");
        currentRoad = _newRoad;
        currentRoad.trafficLightEvents.onLightChange += OnTrafficLightChange;
        // Auto send an event in order to know the state
        OnTrafficLightChange(currentRoad.trafficLight.currentColor);
    }

    public void UnsubscribeToTrafficLight()
    {
        Debug.Log("UnsubscribeToTrafficLight");
        currentRoad.trafficLightEvents.onLightChange -= OnTrafficLightChange;
        currentRoad = null;
    }
}
