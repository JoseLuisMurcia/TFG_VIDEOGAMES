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
    private AvoidanceBehavior notificator = null;
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

        if (hasBeenNotified)
            CheckNotifications();
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

            PathFollower sightedCar = whiskersManager.HasFurtherCarsBeforeReturning();
            if (sightedCar == null) // No ha visto un coche lo suficientemente cerca y lento, elige entre volver y no
            {
                pathFollower.overtaking = false;
                // Random para que vuelva o no
                int choice = Random.Range(0, 2);
                if (choice == 0) // Stay in the lane
                {
                    Debug.Log("NO LANE SWAP");
                }
                else // Swap lane if possible
                {
                    Debug.Log("SWAP LANE ON DELAY");
                    if (!hasBeenNotified) whiskersManager.DelayLaneSwapRequest();
                }
                
            }
            else
            {
                Debug.Log("CAR SIGHTED AFTER OVERTAKING");
                overtakenCar = sightedCar;
                pathFollower.overtaking = true;
            }            
        }
    }

    public void RequestLaneSwapUntilPossible()
    {
        if (canSwapLane && !hasBeenNotified)
        {
            pathFollower.RequestLaneSwap();
            Debug.Log("LANE SWAP");
        }
    }

    // Method called when you are on the left lane and the car in front is slower than you, you tell him to switch to the right lane
    public void ProcessCarHit(RaycastHit hit)
    {
        PathFollower hitCar = hit.collider.gameObject.GetComponent<PathFollower>();
        if (avoidanceBehavior.BothCarsInSameLane(pathFollower, hitCar) && pathFollower.targetPathFollower == hitCar && pathFollower.speed > hitCar.speed)
        {
            hitCar.overtakeBehavior.OnNotification(this.avoidanceBehavior);
        }
    }

    // When this method is called, the car should try to switch to the right lane if possible
    public void OnNotification(AvoidanceBehavior _notificator)
    {
        if (hasBeenNotified)
            return;

        notificator = _notificator;
        hasBeenNotified = true;
    }

    public void CheckNotifications()
    {
        if (canSwapLane && !pathFollower.overtaking)
        {
            SwitchToRightOnNotification();
        }
    }

    private void SwitchToRightOnNotification()
    {
        pathFollower.RequestLaneSwap(); // If consigue cambiar de carril, pondremos hasBeenNotified a false
        pathFollower.overtaking = false;

        // Hay que ejecutar estas dos lineas con delay
        whiskersManager.DelayFreeLaneRequest(notificator);
        hasBeenNotified = false;
    }

}

