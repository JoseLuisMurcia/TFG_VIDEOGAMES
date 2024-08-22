using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class should know where the car is right now. It should be notified when there is a change in a traffic light, or a car is too close and it should brake.
public class TrafficLightCarController : MonoBehaviour
{
    PathFollower pathFollower;

    [SerializeField] public Road currentRoad;
    [SerializeField] public CarTrafficLight trafficLight;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    private void OnTrafficLightChange(TrafficLightState newColor, bool subscription, float lightChangeTime)
    {
        if (currentRoad == null)
            return;

        float distance;
        switch (newColor)
        {
            case TrafficLightState.Green:
                pathFollower.ContinueAtTrafficLight(subscription);
                break;
            case TrafficLightState.Amber:
                distance = CheckDistanceWithTrafficLight(currentRoad.trafficLight.transform.position);
                if (ShouldBrake(distance,lightChangeTime))
                {
                    pathFollower.StopAtTrafficLight(subscription);
                }
                break;
            case TrafficLightState.Red:
                distance = CheckDistanceWithTrafficLight(currentRoad.trafficLight.transform.position);
                bool isClose = distance <= 1f ? true : false;
                bool isFast = pathFollower.speedPercent > 0.6f ? true : false;
                if (!(isClose && isFast))
                {
                    pathFollower.StopAtTrafficLight(subscription);
                }
                break;
        }
    }
    private bool ShouldBrake(float distance, float lightChangeTime)
    {
        // Calculate time to reach the traffic light
        float timeToReach = distance / (pathFollower.speedPercent * pathFollower.speed);

        // Decide based on time to reach and time until the light changes
        return timeToReach >= lightChangeTime;
    }
    public float GiveDistanceToPathFollower()
    {
        if (currentRoad == null)
        {
            pathFollower.shouldStopAtTrafficLight = false;
            return 100f;
        }
        return CheckDistanceWithTrafficLight(currentRoad.trafficLight.transform.position);
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
        trafficLight = _newRoad.trafficLight;
        currentRoad.trafficLightEvents.onLightChange += OnTrafficLightChange;
        // Auto send an event in order to know the state
        OnTrafficLightChange(currentRoad.trafficLight.currentColor, true, -1f);
    }

    public void UnsubscribeToTrafficLight()
    {
        currentRoad.trafficLightEvents.onLightChange -= OnTrafficLightChange;
        pathFollower.shouldStopAtTrafficLight = false;
        currentRoad = null;
        trafficLight = null;
    }
}
