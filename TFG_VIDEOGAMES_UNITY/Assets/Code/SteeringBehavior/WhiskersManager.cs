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
    private const float centerReach = 7.5f;
    private const float sideReach = 10f;

    [SerializeField] bool visualDebug = false;
    public bool intersectionInSight = false;

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
        boxCollider = GetComponent<BoxCollider>();

        avoidanceBehavior = new AvoidanceBehavior(pathFollower, trafficLightCarController);
        pathFollower.avoidanceBehavior = avoidanceBehavior;
        priorityBehavior = new PriorityBehavior(pathFollower, avoidanceBehavior);
        pathFollower.priorityBehavior = priorityBehavior;
        overtakeBehavior = new OvertakeBehavior(pathFollower, avoidanceBehavior, this, priorityBehavior);
        pedestrianBehavior = new PedestrianAvoidanceBehavior(pathFollower);
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
        if (pathFollower.roadValidForOvertaking)
        {
            CheckLaneSwap(pathFollower.laneSide);
        }

        if (!priorityBehavior.hasSignalInSight)
        {
            CheckSignals();
        }

        if(intersectionInSight) CheckForIncorporation();
        
    }
    void CheckForIncorporation()
    {
        if (priorityBehavior.isInRoundabout)
            return;

        RaycastHit hit;
        foreach (Transform sensor in incorporationWhiskers)
        {
            float reach = 15f;

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
                if (intersectionInSight && trafficLightCarController.currentRoad == null) priorityBehavior.ProcessCarHit(ray, hit, sensor);
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
        float randomTime = Random.Range(.6f, 1.3f);
        yield return new WaitForSeconds(randomTime);
        _notificator.AddCarToBlacklist(pathFollower);
        _notificator.UnableTarget();
    }

    private void OnDrawGizmos()
    {

    }

}
