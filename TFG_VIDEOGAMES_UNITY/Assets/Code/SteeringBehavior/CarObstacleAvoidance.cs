using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class could be better if it included node information, dont put a navigation node too close to the border if possible or in a not navigable part.
// With the grid, we can find the node in that position and check if it is walkable and the movement penalty
public class CarObstacleAvoidance : MonoBehaviour
{
    List<Transform> sensors = new List<Transform>();
    [SerializeField] LayerMask obstacleLayer, carLayer;
    public bool objectHit = false;
    //[SerializeField] float obstacleRayDistance = 0.75f;
    //[SerializeField] float sideReach = 3f;
    [SerializeField] float centerReach = 4f;
    [SerializeField] float carRayDistance = 2f;
    private PathFollower pathFollower;
    private PathFollower hitCarPathFollower;
    private TrafficLightCarController trafficLightController;
    private TrafficLightCarController hitCarTrafficLightController;
    private Vector3 rayOrigin;

    private Transform carTarget;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        trafficLightController = GetComponent<TrafficLightCarController>();
        Transform sensorsParent = transform.Find("Whiskers");
        foreach (Transform sensor in sensorsParent.transform)
        {
            sensors.Add(sensor);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Think about if avoiding an obstacle you come too close with a car.
        if (objectHit) return;
        rayOrigin = sensors[0].position;

        if (carTarget != null)
        {
            if (trafficLightController.currentRoad != null)
            {
                if (DifferentRoads(trafficLightController.currentRoad, hitCarTrafficLightController.currentRoad))
                {
                    pathFollower.carTarget = null;
                    pathFollower.shouldBrakeBeforeCar = false;
                }
                else
                {
                    Debug.DrawLine(rayOrigin, carTarget.position, Color.magenta);
                }

                if (Vector3.Distance(carTarget.position, transform.position) > 4.5)
                {
                    carTarget = null;
                    pathFollower.shouldBrakeBeforeCar = false;
                }
            }
            else
            {
                if (Vector3.Distance(carTarget.position, transform.position) > 4.5)
                {
                    carTarget = null;
                    pathFollower.shouldBrakeBeforeCar = false;
                }
                else
                {
                    Debug.DrawLine(rayOrigin, carTarget.position, Color.magenta);
                }
            }

        }
        else
        {
            CheckCars();
        }
        //CheckRoadObstacles();

    }

    private void CheckCars()
    {
        foreach (Transform sensor in sensors)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, centerReach * carRayDistance, carLayer))
            {
                Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
                Vector3 carForward = transform.forward;
                float angleTolerance = 90f;
                if (Vector3.Angle(hitCarForward, carForward) < angleTolerance)
                {
                    hitCarPathFollower = hit.collider.gameObject.GetComponent<PathFollower>();
                    hitCarTrafficLightController = hit.collider.gameObject.GetComponent<TrafficLightCarController>();

                    if (trafficLightController.currentRoad != null)
                    {
                        if(DifferentRoads(trafficLightController.currentRoad, hitCarTrafficLightController.currentRoad))
                        {
                            Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * centerReach, Color.white);
                            pathFollower.carTarget = null;
                            pathFollower.shouldBrakeBeforeCar = false;
                        }
                        else
                        {
                            carTarget = hitCarPathFollower.transform;
                            Debug.DrawLine(rayOrigin, hit.point, Color.black);
                            pathFollower.carTarget = carTarget;
                            pathFollower.shouldBrakeBeforeCar = true;
                            return;
                        }
                        
                    }
                    else
                    {
                        carTarget = hitCarPathFollower.transform;
                        Debug.DrawLine(rayOrigin, hit.point, Color.black);
                        pathFollower.carTarget = carTarget;
                        pathFollower.shouldBrakeBeforeCar = true;
                        return;
                    }
                }
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * centerReach, Color.white);
            }
        }
        pathFollower.carTarget = null;
        pathFollower.shouldBrakeBeforeCar = false;
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
}
