using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvisiblePedestrianTrafficLightTrigger : MonoBehaviour
{
    private PedestrianIntersectionController intersectionController;

    public PedestrianIntersectionController GetIntersectionController()
    {
        return intersectionController;
    }
    public void SetIntersectionController(PedestrianIntersectionController _intersectionController)
    {
        intersectionController = _intersectionController;
    }
}
