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


}

