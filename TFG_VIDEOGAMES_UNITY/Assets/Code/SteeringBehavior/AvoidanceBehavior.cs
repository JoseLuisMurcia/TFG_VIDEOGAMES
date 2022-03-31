using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class could be better if it included node information, dont put a navigation node too close to the border if possible or in a not navigable part.
// With the grid, we can find the node in that position and check if it is walkable and the movement penalty
public class AvoidanceBehavior
{
    List<Transform> whiskers = new List<Transform>(); 
    public bool objectHit = false;
    private PathFollower pathFollower;
    public PathFollower hitCarPathFollower;
    private TrafficLightCarController trafficLightController;
    private TrafficLightCarController hitCarTrafficLightController;
    private Vector3 rayOrigin;
    private Transform carTarget;
    public bool hasTarget = false;
    LayerMask obstacleLayer, carLayer;
    private Transform transform;
    private bool visualDebug;

    public AvoidanceBehavior(LayerMask _carLayer, LayerMask _obstacleLayer, List<Transform> _whiskers, PathFollower _pathFollower, TrafficLightCarController _trafficLightCarController)
    {
        carLayer = _carLayer;
        obstacleLayer = _obstacleLayer;
        whiskers = _whiskers;
        pathFollower = _pathFollower;
        trafficLightController = _trafficLightCarController;
    }


    // Update is called once per frame
    public void Update(Transform _transform, bool _visualDebug)
    {
        // Think about if avoiding an obstacle you come too close with a car.
        visualDebug = _visualDebug;
        transform = _transform;
        if (objectHit) return;
        rayOrigin = whiskers[0].position;

        //CheckRoadObstacles();
        CheckIfTargetIsValid();
    }

    private void CheckIfTargetIsValid()
    {
        if (carTarget != null)
        {
            if (trafficLightController.currentRoad != null)
            {
                if (DifferentRoads(trafficLightController.currentRoad, hitCarTrafficLightController.currentRoad))
                {
                    UnableTarget();
                }
                else if (Vector3.Distance(carTarget.position, transform.position) > 4.5)
                {
                    UnableTarget();
                }
                else
                {
                    EnableTarget();
                }

            }
            else
            {
                if (Vector3.Distance(carTarget.position, transform.position) > 4.5)
                {
                    UnableTarget();
                }
                else
                {
                    EnableTarget();
                }
            }

        }
    }
    private void UnableTarget()
    {
        carTarget = null;
        hasTarget = false;
        pathFollower.carTarget = null;
        pathFollower.shouldBrakeBeforeCar = false;
    }

    private void EnableTarget()
    {
        pathFollower.carTarget = carTarget;
        hasTarget = true;
        pathFollower.shouldBrakeBeforeCar = true;
        Debug.DrawLine(rayOrigin, carTarget.position, Color.magenta);
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

    private bool DifferentRoads(Road carRoad, Road hitCarRoad)
    {
        if (hitCarRoad == null)
            return true;

        if (carRoad == hitCarRoad)
            return false;


        return false;
    }

    public void ProcessCarHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
        Vector3 carForward = transform.forward;
        float angleTolerance = 75f;
        if (Vector3.Angle(hitCarForward, carForward) < angleTolerance)
        {
            hitCarPathFollower = hit.collider.gameObject.GetComponent<PathFollower>();
            hitCarTrafficLightController = hit.collider.gameObject.GetComponent<TrafficLightCarController>();

            if (trafficLightController.currentRoad != null)
            {
                if (DifferentRoads(trafficLightController.currentRoad, hitCarTrafficLightController.currentRoad))
                {
                    UnableTarget();
                }
                else
                {
                    carTarget = hitCarPathFollower.transform;
                    EnableTarget();
                    return;
                }

            }
            else
            {
                carTarget = hitCarPathFollower.transform;
                EnableTarget();
                return;
            }
        }
    }
}
