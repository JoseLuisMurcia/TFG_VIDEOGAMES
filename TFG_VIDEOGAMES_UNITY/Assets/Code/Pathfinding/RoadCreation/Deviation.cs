using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;


public class Deviation : Road
{
    [HideInInspector] public List<Transform> exits = new List<Transform>();
    [HideInInspector] public List<Transform> entries = new List<Transform>();
    [HideInInspector] public List<Vector3> laneTwoDirReferencePoints = new List<Vector3>();
    [HideInInspector] public Vector3 laneWidthObjectPos;
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Vector3 endPos;

    void Start()
    {
        PathCreator[] pathCreators = GetComponents<PathCreator>();
        laneWidthObjectPos = transform.Find("LaneWidth").position;
        Sort2DirectionReferencePoints(pathCreators[1]);
        Transform startTransform = transform.Find("Start");
        Transform endTransform = transform.Find("End");
        startPos = startTransform.position;
        endPos = endTransform.position;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void Sort2DirectionReferencePoints(PathCreator _pathCreator)
    {
        List<Vector3> localPoints = _pathCreator.bezierPath.GetAnchorPoints();
        foreach (Vector3 localPoint in localPoints)
            laneTwoDirReferencePoints.Add(transform.TransformPoint(localPoint));
    }

}
