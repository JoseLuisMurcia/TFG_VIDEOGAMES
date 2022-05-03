using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiskersManager : MonoBehaviour
{
    private AvoidanceBehavior avoidanceBehavior;
    private PriorityBehavior priorityBehavior;
    private PedestrianAvoidanceBehavior pedestrianBehavior;
    private OvertakeBehavior overtakeBehavior;
    private PathFollower pathFollower;
    private TrafficLightCarController trafficLightCarController;
    [SerializeField] LayerMask obstacleLayer, carLayer, signalLayer, pedestrianLayer;

    private List<Transform> whiskers = new List<Transform>();
    private List<Transform> trafficSignalWhiskers = new List<Transform>();
    private List<Transform> incorporationWhiskers = new List<Transform>();
    private Vector3 rayOrigin;
    private const float centerReach = 4.5f;
    private const float sideReach = 15f;

    [SerializeField] bool visualDebug = false;
    public bool intersectionInSight = false;

    [Header("Overtake")]
    private Vector3 leftMirrorPos;
    private Vector3 rightMirrorPos;
    private List<Vector3> lOvertakeRaysForward = new List<Vector3>();
    private List<Vector3> rOvertakeRaysForward = new List<Vector3>();
    private List<float> overtakeRaysReach = new List<float>() { 5f, 2.5f, 1.5f, 2f, 3f, 7.5f };
    private BoxCollider boxCollider;

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
        CreateOvertakeRays();
        avoidanceBehavior = new AvoidanceBehavior(pathFollower, trafficLightCarController);
        priorityBehavior = new PriorityBehavior(pathFollower, avoidanceBehavior);
        overtakeBehavior = new OvertakeBehavior(pathFollower, avoidanceBehavior);
        pedestrianBehavior = new PedestrianAvoidanceBehavior(pathFollower);
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
    void CreateOvertakeRays()
    {
        List<float> leftAngles = new List<float>() { -10f, -30f, -70f, -120f, -150f, -170f };
        List<float> rightAngles = new List<float>() { 10f, 30f, 70f, 120f, 150f, 170f };
        int numRays = leftAngles.Count;

        boxCollider = GetComponent<BoxCollider>();
        leftMirrorPos = transform.position - transform.right * .5f + new Vector3(0, boxCollider.size.y * .2f, 0);
        rightMirrorPos = transform.position + transform.right * .5f + new Vector3(0, boxCollider.size.y * .2f, 0);

        for (int i = 0; i < numRays; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, leftAngles[i], 0);
            lOvertakeRaysForward.Add(rotation * transform.forward);

            rotation = Quaternion.Euler(0, rightAngles[i], 0);
            rOvertakeRaysForward.Add(rotation * transform.forward);
        }
    }
    void Update()
    {
        rayOrigin = whiskers[0].position;
        leftMirrorPos = transform.position - transform.right * .5f + new Vector3(0, boxCollider.size.y * .2f, 0);
        rightMirrorPos = transform.position + transform.right * .5f + new Vector3(0, boxCollider.size.y * .2f, 0);

        avoidanceBehavior.Update(transform, visualDebug, rayOrigin);
        priorityBehavior.Update(transform, visualDebug, rayOrigin);
        pedestrianBehavior.Update(transform, visualDebug, rayOrigin);
        overtakeBehavior.Update(transform, visualDebug, rayOrigin);

        if (pathFollower.isFullyStopped) return;

        CheckCars();
        CheckPedestrians();
        if (pathFollower.roadValidForOvertaking && avoidanceBehavior.hasTarget)
        {
            CheckOvertake(pathFollower.laneSide);
        }

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
                //if (visualDebug) Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward.normalized * reach, Color.red);
            }
        }
    }
    void CheckOvertake(LaneSide laneSide)
    {
        RaycastHit hit;
        int i = 0;
        
        if(laneSide == LaneSide.Right)
        {
            foreach (Vector3 sensorForward in lOvertakeRaysForward)
            {
                Ray ray = new Ray(leftMirrorPos, sensorForward);
                if (Physics.Raycast(ray, out hit, overtakeRaysReach[i], carLayer))
                {
                    if (visualDebug) Debug.DrawLine(leftMirrorPos, hit.point, Color.black);
                    overtakeBehavior.canOvertake = false;
                    return;
                }
                else
                {
                    if (visualDebug) Debug.DrawLine(leftMirrorPos, leftMirrorPos + sensorForward * overtakeRaysReach[i], Color.white);
                }
                i++;
            }
        }
        else
        {
            foreach (Vector3 sensorForward in rOvertakeRaysForward)
            {
                Ray ray = new Ray(rightMirrorPos, sensorForward);
                if (Physics.Raycast(ray, out hit, overtakeRaysReach[i], carLayer))
                {
                    if (visualDebug) Debug.DrawLine(rightMirrorPos, hit.point, Color.black);
                    overtakeBehavior.canOvertake = false;
                    return;
                }
                else
                {
                    if (visualDebug) Debug.DrawLine(rightMirrorPos, rightMirrorPos + sensorForward * overtakeRaysReach[i], Color.white);
                }
                i++;
            }
        }

        overtakeBehavior.canOvertake = true;

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
    void CheckPedestrians()
    {
        RaycastHit hit;
        foreach (Transform sensor in whiskers)
        {
            float reach = 7.5f;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, pedestrianLayer))
            {
                //if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.black);
                pedestrianBehavior.ProcessPedestrianHit(ray, hit, sensor);
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

    private void OnDrawGizmos()
    {

    }

}
