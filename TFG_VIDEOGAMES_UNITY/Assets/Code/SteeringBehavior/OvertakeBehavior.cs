using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OvertakeBehavior
{
    private PathFollower pathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    private Transform transform;
    private bool visualDebug = false;
    private AvoidanceBehavior avoidanceBehavior;

    public bool canOvertake = true;
    public OvertakeBehavior(PathFollower _pathFollower, AvoidanceBehavior _avoidanceBehavior)
    {
        pathFollower = _pathFollower;
        avoidanceBehavior = _avoidanceBehavior;
    }

    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        rayOrigin = _rayOrigin;
        visualDebug = _visualDebug;
        transform = _transform;
    }


}

