using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : Road
{
    [Header("BridgeSpecific")]
    [SerializeField] public NumDirection upperNumDirection;
    [SerializeField] public NumDirection lowerNumDirection;
    [SerializeField] public bool invertUpperRoad;
    [SerializeField] public bool invertLowerRoad;
    [SerializeField] public int upperRoadNumLanes;
    [SerializeField] public int lowerRoadNumLanes;
    void Start()
    {
        typeOfRoad = TypeOfRoad.Bridge;
        numDirection = NumDirection.ZERO;
        SetLanes();
        Destroy(boxCollider);
    }
    private void SetLanes()
    {
        for (int i = 0; i < lowerRoadNumLanes; i++)
        {
            lanes.Add(new Lane());
        }
        for (int i = 0; i < upperRoadNumLanes; i++)
        {
            lanes.Add(new Lane());
        }
    }
}
