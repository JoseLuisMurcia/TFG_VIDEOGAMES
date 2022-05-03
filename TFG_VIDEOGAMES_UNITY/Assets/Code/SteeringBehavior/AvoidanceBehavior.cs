using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class could be better if it included node information, dont put a navigation node too close to the border if possible or in a not navigable part.
// With the grid, we can find the node in that position and check if it is walkable and the movement penalty
public class AvoidanceBehavior
{
    public bool objectHit = false;
    private PathFollower pathFollower;
    public PathFollower hitCarPathFollower;
    private TrafficLightCarController trafficLightController;
    private TrafficLightCarController hitCarTrafficLightController;
    private Vector3 rayOrigin;
    public bool hasTarget = false;
    private Transform transform;
    private bool visualDebug;

    private List<PathFollower> blackList = new List<PathFollower>();

    public AvoidanceBehavior(PathFollower _pathFollower, TrafficLightCarController _trafficLightCarController)
    {
        pathFollower = _pathFollower;
        trafficLightController = _trafficLightCarController;
    }


    // Update is called once per frame
    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        // Think about if avoiding an obstacle you come too close with a car.
        visualDebug = _visualDebug;
        transform = _transform;
        if (objectHit) return;
        rayOrigin = _rayOrigin;
        //CheckRoadObstacles();
        if (hasTarget) CheckIfTargetIsValid();
    }

    private void CheckIfTargetIsValid()
    {
        if (TargetIsFar())
        {
            UnableTarget();
        }
        else if (trafficLightController.currentRoad != null)
        {
            if (DifferentRoads(trafficLightController, hitCarTrafficLightController))
            {
                UnableTarget();
            }
        }
        else if (pathFollower.roadValidForOvertaking && !BothCarsInSameLane(pathFollower.laneSide, hitCarPathFollower.laneSide))
        {
            UnableTarget();
        }
    }

    private bool TargetIsFar()
    {
        return Vector3.Distance(pathFollower.carTarget.position, transform.position) > 4.5f;
    }
    public void UnableTarget()
    {
        hasTarget = false;
        pathFollower.UnableTarget();
    }

    private void EnableTarget(Transform hitCarTransform)
    {
        if (hasTarget)
            return;
        pathFollower.EnableTarget(hitCarTransform, hitCarPathFollower.priorityBehavior, hitCarPathFollower);
        hasTarget = true;
        Debug.DrawLine(rayOrigin, hitCarTransform.position, Color.magenta);
    }

    private void CheckRoadObstacles()
    {
        //Ray ray = new Ray(rayOrigin, leftSensor.forward);
        //RaycastHit hit;

        //// If the object is hit with the left ray, create a position to go to the right.
        //if (Physics.Raycast(ray, out hit, sideReach * obstacleRayDistance, obstacleLayer))
        //{
        //    objectHit = true;
        //    Debug.DrawLine(rayOrigin, hit.point, Color.red);
        //    Vector3 newPoint = hit.point + transform.right;
        //    pathFollower.SetNewPathByAvoidance(newPoint);
        //}
        //else
        //{
        //    Debug.DrawLine(rayOrigin, rayOrigin + leftSensor.forward, Color.blue);
        //}

        //// If the object is hit with the middle ray first, go left or right.
        //ray = new Ray(rayOrigin, centerSensor.forward);
        //if (Physics.Raycast(ray, out hit, centerReach * obstacleRayDistance, obstacleLayer))
        //{
        //    objectHit = true;
        //    Debug.DrawLine(rayOrigin, hit.point, Color.red);

        //    Vector3 hitPoint = hit.point;
        //    Vector3 leftPos = hitPoint - transform.right;
        //    Vector3 rightPos = hitPoint + transform.right;

        //    //pathFollower.SetNewPathByAvoidance(GetBestCandidate(leftPos, rightPos));
        //}
        //else
        //{
        //    Debug.DrawLine(rayOrigin, rayOrigin + centerSensor.forward, Color.blue);
        //}

        //// If the object is hit with the right ray, create a position to go to the left
        //ray = new Ray(rayOrigin, rightSensor.forward);
        //if (Physics.Raycast(ray, out hit, sideReach * obstacleRayDistance, obstacleLayer))
        //{
        //    objectHit = true;
        //    Debug.DrawLine(rayOrigin, hit.point, Color.red);
        //    Vector3 newPoint = hit.point + -transform.right;
        //    pathFollower.SetNewPathByAvoidance(newPoint);
        //}
        //else
        //{
        //    Debug.DrawLine(rayOrigin, rayOrigin + rightSensor.forward, Color.blue);
        //}
    }

    private bool DifferentRoads(TrafficLightCarController carTrafficLightCont, TrafficLightCarController hitCarTrafficLightCont)
    {
        Road ownRoad = carTrafficLightCont.currentRoad;
        Road hitCarRoad = hitCarTrafficLightCont.currentRoad;

        if (hitCarRoad == null)
            return true;

        if (ownRoad == hitCarRoad)
            return false;


        return false;
    }

    private bool BothShouldStopBeforeLight()
    {
        bool shouldStopAtLight = pathFollower.shouldStopAtTrafficLight;
        bool hitCarShouldStopAtLight = hitCarPathFollower.shouldStopAtTrafficLight;
        return (shouldStopAtLight && hitCarShouldStopAtLight) || (!shouldStopAtLight && !hitCarShouldStopAtLight);
    }

    public void ProcessCarHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
        Vector3 carForward = transform.forward;
        float angleTolerance = 75f;
        if (Vector3.Angle(hitCarForward, carForward) < angleTolerance && Vector3.Distance(transform.position, hit.point) < 4.5f)
        {
            hitCarPathFollower = hit.collider.gameObject.GetComponent<PathFollower>();
            hitCarTrafficLightController = hit.collider.gameObject.GetComponent<TrafficLightCarController>();

            if (trafficLightController.currentRoad != null) // If our car has to stop before traffic, only enable the target traffic light if the car is in the same traffic light as us.
            {
                if (!DifferentRoads(trafficLightController, hitCarTrafficLightController) && BothShouldStopBeforeLight())
                {
                    if (NewTargetIsValid())
                        return;
                }

            }
            else
            {
                if (NewTargetIsValid())
                    return;
            }
        }
    }

    private bool NewTargetIsValid()
    {
        if (hasTarget)
        {
            if (NewCarIsCloserThanTarget())
            {
                if (pathFollower.roadValidForOvertaking)
                {
                    if (BothCarsInSameLane(pathFollower.laneSide, hitCarPathFollower.laneSide) && !blackList.Contains(hitCarPathFollower))
                    {
                        EnableTarget(hitCarPathFollower.transform);
                        return true;
                    }
                    return false;
                }
                else
                {
                    EnableTarget(hitCarPathFollower.transform);
                    return true;
                }

            }
            return false;
        }
        else
        {
            if (pathFollower.roadValidForOvertaking)
            {
                if (BothCarsInSameLane(pathFollower.laneSide, hitCarPathFollower.laneSide) && !blackList.Contains(hitCarPathFollower))
                {
                    EnableTarget(hitCarPathFollower.transform);
                    return true;
                }
                return false;
            }
            else
            {
                EnableTarget(hitCarPathFollower.transform);
                return true;
            }
        }
    }

    private bool BothCarsInSameLane(LaneSide laneSide, LaneSide targetLaneSide)
    {
        return laneSide == targetLaneSide;
    }
    private bool NewCarIsCloserThanTarget()
    {
        Vector3 hitCarPos = hitCarPathFollower.transform.position;
        Vector3 carPos = transform.position;
        return Vector3.Distance(carPos, hitCarPos) < Vector3.Distance(carPos, pathFollower.carTarget.position);
    }


    public void AddCarToBlacklist(PathFollower targetPathFollower)
    {
        blackList.Add(targetPathFollower);
    }
}
