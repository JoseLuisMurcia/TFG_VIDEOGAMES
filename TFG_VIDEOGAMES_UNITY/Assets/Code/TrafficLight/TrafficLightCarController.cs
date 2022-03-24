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

    [HideInInspector] public Road currentRoad;
    public float distanceToStopInAmberLight = 3f;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    private void OnTrafficLightChange(TrafficLightColor newColor, bool subscription)
    {
        if (currentRoad == null)
            return;

        float distance;
        switch (newColor)
        {
            case TrafficLightColor.Green:
                pathFollower.ContinueAtTrafficLight(subscription);
                break;
            case TrafficLightColor.Amber:
                distance = CheckDistanceWithTrafficLight(currentRoad.trafficLight.transform.position);
                if (distance > distanceToStopInAmberLight)
                {
                    pathFollower.StopAtTrafficLight(subscription);
                }
                break;
            case TrafficLightColor.Red:
                pathFollower.StopAtTrafficLight(subscription);
                break;
        }
        //Debug.Log("THE TRAFFIC LIGHT HAS CHANGED TO: " + newColor);
    }

    public float GiveDistanceToPathFollower()
    {
        if (currentRoad)
        {
            return CheckDistanceWithTrafficLight(currentRoad.trafficLight.transform.position);

        }
        return 100f;
    }

    private float CheckDistanceWithTrafficLight(Vector3 trafficLightPos)
    {
        // Encontrar punto a una distancia X a partir de una direccion.
        Vector3 trafficLightPosForward = currentRoad.trafficLight.transform.forward;
        Vector2 perpDir = Vector2.Perpendicular(new Vector2(trafficLightPosForward.x, trafficLightPosForward.z));
        Vector3 perpendicularDirection = new Vector3(perpDir.x, 0, perpDir.y).normalized;

        Ray ray = new Ray(trafficLightPos, perpendicularDirection);
        return Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;
    }

    public void SubscribeToTrafficLight(Road _newRoad)
    {
        currentRoad = _newRoad;
        currentRoad.trafficLightEvents.onLightChange += OnTrafficLightChange;
        // Auto send an event in order to know the state
        OnTrafficLightChange(currentRoad.trafficLight.currentColor, true);
    }

    public void UnsubscribeToTrafficLight()
    {
        currentRoad.trafficLightEvents.onLightChange -= OnTrafficLightChange;
        currentRoad = null;
    }
}
