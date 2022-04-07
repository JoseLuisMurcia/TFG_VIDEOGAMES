using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Split : Road
{

    [HideInInspector] public Vector3 laneRefEnd;
    [HideInInspector] public Vector3 laneRefStart;
    [HideInInspector] public Vector3 laneDir;
    void Start()
    {
        laneRefStart = transform.Find("laneRefStart").position;
        laneRefEnd = transform.Find("laneRefEnd").position;
        laneDir = (laneRefEnd - laneRefStart);

        typeOfRoad = TypeOfRoad.Split;
        numberOfLanes = 2;
    }

}
