using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class could be better if it included node information, dont put a navigation node too close to the border if possible or in a not navigable part.
// With the grid, we can find the node in that position and check if it is walkable and the movement penalty
public class CarObstacleAvoidance : MonoBehaviour
{
    [SerializeField] Transform centerSensor, leftSensor, rightSensor, leftCarsSensor, rightCarsSensor;
    [SerializeField] LayerMask obstacleLayer, carLayer;
    public bool objectHit = false;
    private PathFollower pathFollower;
    [SerializeField] float obstacleRayDistance = 0.75f;
    [SerializeField] float sideReach = 1.5f;
    [SerializeField] float centerReach = 4f;
    [SerializeField] float carRayDistance = 2f;

    private Vector3 rayOrigin;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        
    }

    // Update is called once per frame
    void Update()
    {
        // Think about if avoiding an obstacle you come too close with a car.
        if (objectHit) return;
        rayOrigin = centerSensor.position;
        bool carClose = CheckCars();
        if (carClose) return;
        CheckRoadObstacles();

    }

    private bool CheckCars()
    {
        RaycastHit hit;
        Ray ray = new Ray(rayOrigin, centerSensor.forward);
        if (Physics.Raycast(ray, out hit, centerReach * carRayDistance, carLayer))
        {
            Debug.DrawLine(rayOrigin, hit.point, Color.black);
            pathFollower.shouldBrakeBeforeCar = true;
            pathFollower.frontCarPos = hit.point;
            return true;
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + centerSensor.forward, Color.white);
        }
        pathFollower.shouldBrakeBeforeCar = false;
        return false;
    }

    private void CheckRoadObstacles()
    {
        Ray ray = new Ray(rayOrigin, leftSensor.forward);
        RaycastHit hit;

        // If the object is hit with the left ray, create a position to go to the right.
        if (Physics.Raycast(ray, out hit, sideReach * obstacleRayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            Vector3 newPoint = hit.point + transform.right;
            pathFollower.SetNewPathByAvoidance(newPoint);
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + leftSensor.forward, Color.blue);
        }

        // If the object is hit with the middle ray first, go left or right.
        ray = new Ray(rayOrigin, centerSensor.forward);
        if (Physics.Raycast(ray, out hit, centerReach * obstacleRayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(rayOrigin, hit.point, Color.red);

            Vector3 hitPoint = hit.point;
            Vector3 leftPos = hitPoint - transform.right;
            Vector3 rightPos = hitPoint + transform.right;

            //pathFollower.SetNewPathByAvoidance(GetBestCandidate(leftPos, rightPos));
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + centerSensor.forward, Color.blue);
        }

        // If the object is hit with the right ray, create a position to go to the left
        ray = new Ray(rayOrigin, rightSensor.forward);
        if (Physics.Raycast(ray, out hit, sideReach * obstacleRayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            Vector3 newPoint = hit.point + -transform.right;
            pathFollower.SetNewPathByAvoidance(newPoint);
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + rightSensor.forward, Color.blue);
        }
    }

    
}
