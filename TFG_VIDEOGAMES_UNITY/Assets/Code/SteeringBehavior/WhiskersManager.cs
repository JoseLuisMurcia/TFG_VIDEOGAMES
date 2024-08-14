using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiskersManager : MonoBehaviour
{
    private AvoidanceBehavior avoidanceBehavior;
    public PriorityBehavior priorityBehavior;
    private PedestrianAvoidanceBehavior pedestrianBehavior;
    private OvertakeBehavior overtakeBehavior;
    private PathFollower pathFollower;
    private TrafficLightCarController trafficLightCarController;
    [SerializeField] LayerMask obstacleLayer, carLayer, signalLayer, pedestrianLayer;

    private List<Transform> whiskers = new List<Transform>();
    private List<Transform> incorporationWhiskers = new List<Transform>();
    private Vector3 rayOrigin;
    private const float centerReach = 15f;
    private const float sideReach = 10f;

    [SerializeField] bool visualDebug = false;
    public bool intersectionInSight = false;
    [Header("Pedestrian")]
    public bool pedestrianCrossingInSight = false;

    [Header("Overtake")]
    private Vector3 leftMirrorPos;
    private Vector3 rightMirrorPos;
    private List<float> overtakeRaysReach = new List<float>() { 3f, 2.5f, 1.5f, 2f, 3.5f, 7f, 7.5f };
    private List<float> unovertakeRaysReach = new List<float>() { 5f, 3.5f, 1.5f, 2f, 3.5f, 4f, 4.5f };
    private BoxCollider boxCollider;

    private List<float> leftAngles = new List<float>() { -15f, -30f, -70f, -130f, -160f, -170f, -175f };
    private List<float> rightAngles = new List<float>() { 10f, 30f, 70f, 125f, 145f, 160f, 165f };

    private List<float> unovertakeAngles = new List<float>() { 2.5f, 5f, 7.5f, 12.5f };
    private List<float> unovertakeLongRaysReach = new List<float>() { 15f, 12.5f, 10f, 7.5f };

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        trafficLightCarController = GetComponent<TrafficLightCarController>();

        CreateWhiskers();
        CreateIncorporationWhiskers();
        boxCollider = GetComponent<BoxCollider>();

        avoidanceBehavior = new AvoidanceBehavior(pathFollower, trafficLightCarController);
        pathFollower.avoidanceBehavior = avoidanceBehavior;
        priorityBehavior = new PriorityBehavior(pathFollower, avoidanceBehavior);
        pathFollower.priorityBehavior = priorityBehavior;
        overtakeBehavior = new OvertakeBehavior(pathFollower, avoidanceBehavior, this, priorityBehavior);
        pedestrianBehavior = new PedestrianAvoidanceBehavior(pathFollower);
    }
    void CreateWhiskers()
    {
        List<float> angles = new List<float>() { -30f, -15f, -5f, -0f, 5f, 15f, 30f };
        Transform whiskersParent = transform.Find("Whiskers");
        foreach (float angle in angles)
        {
            GameObject sensor = new GameObject("Sensor: " + angle);
            sensor.transform.position = whiskersParent.position;
            sensor.transform.parent = whiskersParent;
            sensor.transform.localEulerAngles = new Vector3(0, angle, 0);
            whiskers.Add(sensor.transform);
        }
    }
    void CreateIncorporationWhiskers()
    {
        List<float> angles = new List<float>() { -85f, -70f, -60f, -50f, -45f, -40f, -35f, -30f, -25f, -20f, -10f, -6f, -3f, 0f,
            3f, 6f, 10f, 20f, 25f, 30f, 35f, 40f, 45f, 50f, 60f, 70f, 85f};
        Transform whiskersParent = transform.Find("Whiskers");
        for (int i = 0; i < angles.Count; i++)
        {
            Vector3 localRotation = new Vector3(0f, angles[i], 0f);
            GameObject _newWhisker = new GameObject("Incorporation sensor: " + angles[i]);
            _newWhisker.transform.parent = whiskersParent;
            _newWhisker.transform.position = whiskers[0].position;
            _newWhisker.transform.localEulerAngles = localRotation;
            incorporationWhiskers.Add(_newWhisker.transform);
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
        if (pedestrianCrossingInSight && !pathFollower.TargetIsStoppingBeforePedestrians()) CheckPedestrians();
        if (pathFollower.roadValidForOvertaking) CheckLaneSwap(pathFollower.laneSide);
        if (intersectionInSight || pathFollower.priorityLevel == PriorityLevel.Roundabout) CheckForIncorporation();

    }
    void CheckForIncorporation()
    {
        if (priorityBehavior.isInRoundabout)
            return;

        RaycastHit hit;
        foreach (Transform sensor in incorporationWhiskers)
        {
            float reach = 15f;
            float localAngle = sensor.localEulerAngles.y;
            if (pathFollower.priorityLevel == PriorityLevel.Roundabout)
            {
                reach = 9f;
                if (localAngle < 180f)
                {
                    continue;
                }
            }
            else
            {
                if (localAngle < 10f || localAngle > 351f)
                    reach *= 1.5f;
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
    void CheckLaneSwap(LaneSide laneSide)
    {
        if (priorityBehavior.isInRoundabout || pathFollower.priorityLevel == PriorityLevel.Roundabout)
        {
            overtakeBehavior.canSwapLane = false;
            return;
        }
        int i = 0;
        RaycastHit hit;
        if (laneSide == LaneSide.Right) // Swapping from right lane to the left lane
        {
            foreach (float reach in overtakeRaysReach)
            {
                Quaternion rotation = Quaternion.AngleAxis(leftAngles[i], transform.up);
                Vector3 rotatedForward = rotation * transform.forward;

                Ray ray = new Ray(leftMirrorPos, rotatedForward);
                if (Physics.Raycast(ray, out hit, reach, carLayer))
                {
                    if (visualDebug) Debug.DrawLine(leftMirrorPos, hit.point, Color.black);
                    overtakeBehavior.canSwapLane = false;
                    return;
                }
                else
                {
                    if (visualDebug) Debug.DrawRay(leftMirrorPos, rotatedForward * reach, Color.blue);
                }
                i++;
            }
        }
        else
        { // Swapping from left lane to the right lane
            foreach (float reach in unovertakeRaysReach)
            {
                Quaternion rotation = Quaternion.AngleAxis(rightAngles[i], transform.up);
                Vector3 rotatedForward = rotation * transform.forward;

                Ray ray = new Ray(rightMirrorPos, rotatedForward);
                if (Physics.Raycast(ray, out hit, reach, carLayer))
                {
                    if (visualDebug) Debug.DrawLine(rightMirrorPos, hit.point, Color.black);
                    overtakeBehavior.canSwapLane = false;
                    return;
                }
                else
                {
                    if (visualDebug) Debug.DrawRay(rightMirrorPos, rotatedForward * reach, Color.blue);
                }
                i++;
            }

        }
        overtakeBehavior.canSwapLane = true;
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
            if (intersectionInSight && !priorityBehavior.isInRoundabout)
                reach = reach * 2.5f;
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                avoidanceBehavior.ProcessCarHit(ray, hit, sensor);
                if (yRotation < 10 && pathFollower.roadValidForOvertaking && pathFollower.laneSide == LaneSide.Left)
                    overtakeBehavior.ProcessFrontCarHit(hit);
                if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.black);
            }
            else
            {
                if (visualDebug) Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * reach, Color.white);
            }
        }
    }
    void CheckPedestrians()
    {
        RaycastHit hit;
        foreach (Transform sensor in incorporationWhiskers)
        {
            float reach = 7.5f;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, reach, pedestrianLayer))
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.black);
                pedestrianBehavior.ProcessPedestrianHit(ray, hit, sensor);
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
    public PathFollower HasFurtherCarsBeforeReturning()
    {
        int i = 0;
        RaycastHit hit;

        foreach (float reach in unovertakeLongRaysReach)
        {
            Quaternion rotation = Quaternion.AngleAxis(unovertakeAngles[i], transform.up);
            Vector3 rotatedForward = rotation * transform.forward;

            Ray ray = new Ray(rightMirrorPos, rotatedForward);
            if (Physics.Raycast(ray, out hit, reach, carLayer))
            {
                if (visualDebug) Debug.DrawLine(rightMirrorPos, hit.point, Color.black);
                PathFollower hitCar = hit.collider.gameObject.GetComponent<PathFollower>();
                if (pathFollower.speed - hitCar.speed > 0.2f)
                {
                    return hitCar;
                }
                return null;
            }
            else
            {
                if (visualDebug) Debug.DrawRay(rightMirrorPos, rotatedForward * reach, Color.red);
            }
            i++;
        }

        return null;
    }
    public void DelayLaneSwapRequest()
    {
        StartCoroutine(overtakeBehavior.RequestLaneSwapUntilPossible());
    }
    public void DelayFreeLaneRequest(AvoidanceBehavior _notificator)
    {
        StartCoroutine(DelayFreeLane(_notificator));
    }
    IEnumerator DelayFreeLane(AvoidanceBehavior _notificator)
    {
        float randomTime = UnityEngine.Random.Range(1f, 2f);
        yield return new WaitForSeconds(randomTime);
        _notificator.AddCarToBlacklist(pathFollower);
        _notificator.UnableTarget();
    }
    public void ExitPedestriansPriority()
    {
        pedestrianCrossingInSight = false;
        pedestrianBehavior.ResetStructures();
    }
    private void OnDrawGizmos()
    {
        if (!visualDebug || pathFollower == null) return;

        if (pathFollower.priorityLevel == PriorityLevel.Roundabout)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 4f, .4f);
        }
        if (pathFollower.shouldStopPriority && pathFollower.priorityLevel == PriorityLevel.Max)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 3f, .4f);
            foreach (PathFollower car in priorityBehavior.relevantCarsInSight)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(car.transform.position + Vector3.up * 2.5f, Vector3.one * .3f);
            }
        }

        if (priorityBehavior.relevantCarsInSight.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, .2f);
            foreach (PathFollower car in priorityBehavior.relevantCarsInSight)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(car.transform.position + Vector3.up, Vector3.one * .2f);
            }
        }

        
    }

    public void SetCrossingPos(Vector3 crossingPos)
    {
        pedestrianBehavior.SetCrossingPos(crossingPos);
    }

}
