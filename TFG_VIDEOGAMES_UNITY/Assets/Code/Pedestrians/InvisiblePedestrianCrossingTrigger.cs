using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvisiblePedestrianCrossingTrigger : MonoBehaviour
{
    private Road parentRoad = null;
    void Start()
    {
        parentRoad = GetComponentInParent<Road>();
    }
    public Road GetParentRoad() { return parentRoad; }
}
