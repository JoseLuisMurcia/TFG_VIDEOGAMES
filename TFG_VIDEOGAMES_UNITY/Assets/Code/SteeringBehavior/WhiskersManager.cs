using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiskersManager : MonoBehaviour
{
    private AvoidanceBehavior avoidanceBehavior;
    private PriorityBehavior targetPriorityBehavior;
    private PriorityBehavior priorityBehavior;
    private PathFollower pathFollower;
    private TrafficLightCarController trafficLightCarController;
    [SerializeField] LayerMask obstacleLayer, carLayer, signalLayer;

    private List<Transform> whiskers = new List<Transform>();
    private List<Transform> trafficSignalWhiskers = new List<Transform>();
    private List<Transform> incorporationWhiskers = new List<Transform>();
    private Vector3 rayOrigin;
    private float centerReach = 3.5f;
    private float sideReach = 15f;
    [SerializeField] bool visualDebug = false;
    public bool intersectionInSight = false;

    //[SerializeField] bool visualDebug = false;
    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        trafficLightCarController = GetComponent<TrafficLightCarController>();

        Transform whiskersParent = transform.Find("Whiskers");
        foreach (Transform sensor in whiskersParent.transform)
        {
            whiskers.Add(sensor);
            if (sensor.localRotation.eulerAngles.y > 1 && sensor.localRotation.eulerAngles.y < 100)
            {
                trafficSignalWhiskers.Add(sensor);
            }
        }
        CreateIncorporationWhiskers(whiskersParent);
        avoidanceBehavior = new AvoidanceBehavior(carLayer, obstacleLayer, whiskers, pathFollower, trafficLightCarController);
        priorityBehavior = new PriorityBehavior(carLayer, signalLayer, whiskers, pathFollower, avoidanceBehavior);
        pathFollower.avoidanceBehavior = avoidanceBehavior;
    }

    void CreateIncorporationWhiskers(Transform whiskersParent)
    {
        int numWhiskers = 14;
        float minAngle = -85f;
        float maxAngle = 85f;
        float increment = 0f;
        float yRotThreshold = 20f;
        float currentAngle = minAngle + increment;
        increment = (Mathf.Abs(minAngle) + maxAngle) / numWhiskers;
        for (int i = 0; i <= numWhiskers; i++)
        {
            if (Mathf.Abs(currentAngle) > yRotThreshold)
            {
                Vector3 localRotation = new Vector3(0f, currentAngle, 0f);
                GameObject _newWhisker = new GameObject();
                _newWhisker.transform.parent = whiskersParent;
                _newWhisker.transform.position = whiskers[0].position;
                _newWhisker.transform.localEulerAngles = localRotation;
                incorporationWhiskers.Add(_newWhisker.transform);
            }
            currentAngle += increment;

        }

    }

    // Update is called once per frame
    void Update()
    {
        rayOrigin = whiskers[0].position;
        avoidanceBehavior.Update(transform, visualDebug);
        priorityBehavior.Update(transform, visualDebug);

        CheckCars();

        if (!priorityBehavior.hasSignalInSight)
        {
            CheckSignals();
        }
        else
        {
            CheckForIncorporation();
        }
    }

    void CheckForIncorporation()
    {
        if (priorityBehavior.isInRoundabout)
            return;

        RaycastHit hit;
        foreach (Transform sensor in incorporationWhiskers)
        {
            float reach = 12f;

            if (pathFollower.priorityLevel == PriorityLevel.Roundabout)
            {
                reach = 6f;
                if (sensor.localEulerAngles.y < 180f)
                {
                    continue;
                }
            }

            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.black);
                priorityBehavior.ProcessCarHit(ray, hit, sensor);
            }
            else
            {
                if (visualDebug) Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward.normalized * reach, Color.white);
            }
        }
    }
    void CheckSignals()
    {
        RaycastHit hit;
        foreach (Transform sensor in trafficSignalWhiskers)
        {
            float reach = 10f;

            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, signalLayer))
            {
                priorityBehavior.ProcessSignalHit(ray, hit);
            }
            else
            {
                if (visualDebug) Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward.normalized * reach, Color.red);
            }
        }
    }

    void CheckCars()
    {
        RaycastHit hit;
        foreach (Transform sensor in whiskers)
        {
            float reach;
            float yRotation = Mathf.Abs(sensor.localRotation.eulerAngles.y);
            reach = SetReachForCarRays(yRotation);
            if (priorityBehavior.isInRoundabout)
                reach = 4f;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                avoidanceBehavior.ProcessCarHit(ray, hit, sensor);
                if (!priorityBehavior.hasSignalInSight && intersectionInSight && trafficLightCarController.currentRoad == null) priorityBehavior.ProcessCarHit(ray, hit, sensor);

                //if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.black);
            }
            else
            {
                //if (visualDebug) Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * reach, Color.white);
            }
        }
    }

    private float SetReachForCarRays(float _yRotation)
    {
        float reach;
        if (_yRotation > 220)
        {
            _yRotation = Mathf.Abs(360 - _yRotation);
        }

        if (_yRotation == 0f)
        {
            reach = centerReach;
        }
        else if (_yRotation > 1f && _yRotation < 15f)
        {
            reach = sideReach;
        }
        else
        {
            reach = sideReach * 0.8f;
        }
        return reach;
    }

    public void RoundaboutTrigger(bool entry)
    {
        priorityBehavior.isInRoundabout = entry;
        pathFollower.priorityLevel = PriorityLevel.Max;
        if (!entry)
            priorityBehavior.RemoveSignalFromSight();
    }

}
