using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OvertakeBehavior
{
    private PathFollower pathFollower;
    private Vector3 rayOrigin;
    private Transform transform;
    private bool visualDebug = false;
    private AvoidanceBehavior avoidanceBehavior;
    public PathFollower overtakenCar;

    public bool canSwapLane = true;
    public bool hasBeenNotified = false;
    private bool requestedLaneSwap = false;
    WhiskersManager whiskersManager;

    public OvertakeBehavior(PathFollower _pathFollower, AvoidanceBehavior _avoidanceBehavior, WhiskersManager _whiskersManager)
    {
        pathFollower = _pathFollower;
        avoidanceBehavior = _avoidanceBehavior;
        pathFollower.overtakeBehavior = this;
        whiskersManager = _whiskersManager;
    }

    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        rayOrigin = _rayOrigin;
        visualDebug = _visualDebug;
        transform = _transform;

        if (overtakenCar != null)
        {
            CheckOvertakenCar();
        }
    }


    private void CheckOvertakenCar()
    {
        Vector3 dirToOvertakenCar = (overtakenCar.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dirToOvertakenCar);
        const float minDistance = 3.5f;
        float distance = Vector3.Distance(transform.position, overtakenCar.transform.position);
        if (distance > minDistance && dot < 0 && canSwapLane)
        {
            overtakenCar = null;
            avoidanceBehavior.UnableTarget();

            if (whiskersManager.HasFurtherCarsBeforeReturning())
            {
                // No vuelve
            }
            else
            {
                // Random para que vuelva o no
            }
            //pathFollower.RequestLaneSwap();
            //pathFollower.overtaking = false;
        }
    }

    // Method called when you are on the left lane and the car in front is slower than you, you tell him to switch to the right lane
    public void ProcessCarHit(RaycastHit hit)
    {
        PathFollower hitCar = hit.collider.gameObject.GetComponent<PathFollower>();
        if (avoidanceBehavior.BothCarsInSameLane(pathFollower, hitCar) && pathFollower.targetPathFollower == hitCar && pathFollower.speed > hitCar.speed)
        {
            hitCar.overtakeBehavior.OnNotification(this); 
        }
    }

    // When this method is called, the car should try to switch lane if possible
    public void OnNotification(OvertakeBehavior notificator)
    {
        if (hasBeenNotified)
            return;

        hasBeenNotified = true;
        if (canSwapLane && !requestedLaneSwap)
        {
            pathFollower.RequestLaneSwap(); // If consigue cambiar de carril, pondremos hasBeenNotified a false
            pathFollower.overtaking = false;
            notificator.avoidanceBehavior.AddCarToBlacklist(pathFollower);
            notificator.avoidanceBehavior.UnableTarget();
            hasBeenNotified = false;
        }
    }

}

