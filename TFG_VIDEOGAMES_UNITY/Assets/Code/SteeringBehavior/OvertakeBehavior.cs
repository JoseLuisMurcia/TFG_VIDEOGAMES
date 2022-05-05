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
    public OvertakeBehavior(PathFollower _pathFollower, AvoidanceBehavior _avoidanceBehavior)
    {
        pathFollower = _pathFollower;
        avoidanceBehavior = _avoidanceBehavior;
        pathFollower.overtakeBehavior = this;  
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
        const float minDistance = 4f;
        float distance = Vector3.Distance(transform.position, overtakenCar.transform.position);
        if (distance > minDistance && dot < 0 && canSwapLane)
        {
            //if puede volver al carril, que lo haga
            pathFollower.overtaking = false;
            overtakenCar = null;
            avoidanceBehavior.UnableTarget();
            pathFollower.RequestLaneSwap();
        }
    }

    // Method called when you are on the left lane and the car in front is slower than you, you tell him to switch to the right lane
    public void ProcessCarHit(RaycastHit hit)
    {
        OvertakeBehavior hitCar = hit.collider.gameObject.GetComponent<OvertakeBehavior>();
        hitCar.OnNotification(); // Hay que comprobar que estemos en el mismo carril que el coche de enfrente
    }

    // When this method is called, the car should try to switch lane if possible
    public void OnNotification()
    {
        if (hasBeenNotified)
            return; 

        hasBeenNotified = true;
        if (canSwapLane)
        {
            pathFollower.RequestLaneSwap(); // If consigue cambiar de carril, pondremos hasBeenNotified a false
        }
    }


}

