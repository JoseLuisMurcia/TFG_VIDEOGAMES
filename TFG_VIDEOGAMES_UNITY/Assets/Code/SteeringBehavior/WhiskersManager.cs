using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiskersManager : MonoBehaviour
{
    private AvoidanceBehavior avoidanceBehavior;
    private PriorityBehavior priorityBehavior;
    private PathFollower pathFollower;
    private TrafficLightCarController trafficLightCarController;
    [SerializeField] LayerMask obstacleLayer, carLayer, signalLayer;

    private List<Transform> whiskers = new List<Transform>();
    private List<Transform> trafficSignalWhiskers = new List<Transform>();
    private List<Transform> incorporationWhiskers = new List<Transform>();
    private Vector3 rayOrigin;
    private float centerReach = 6f;
    private float sideReach = 14f;

    [SerializeField] bool visualDebug = false;
    // Start is called before the first frame update
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
        priorityBehavior = new PriorityBehavior(carLayer, signalLayer, whiskers, pathFollower);
        pathFollower.avoidanceBehavior = avoidanceBehavior;
    }

    void CreateIncorporationWhiskers(Transform whiskersParent)
    {
        int numWhiskers = 12;
        float minAngle = -87f;
        float maxAngle = 87f;
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
        avoidanceBehavior.Update(transform);
        priorityBehavior.Update(transform);

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
        RaycastHit hit;
        foreach (Transform sensor in incorporationWhiskers)
        {
            float reach = 15f;

            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.black);
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * reach, Color.white);
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
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                priorityBehavior.ProcessSignalHit(ray, hit);
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * reach, Color.red);
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

            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                if(!avoidanceBehavior.hasTarget) avoidanceBehavior.ProcessCarHit(ray, hit, sensor);
                priorityBehavior.ProcessCarHit(ray, hit, sensor);
                //Debug.DrawLine(rayOrigin, hit.point, Color.black);
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * reach, Color.white);
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
            reach = sideReach * .75f;
        }
        return reach;
    }
}
