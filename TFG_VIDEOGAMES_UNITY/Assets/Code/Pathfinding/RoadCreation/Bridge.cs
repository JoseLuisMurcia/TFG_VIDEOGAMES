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
    [SerializeField] public List<Lane> lowerLanes = new List<Lane>();
    [SerializeField] public List<Lane> upperLanes = new List<Lane>();
    void Start()
    {
        typeOfRoad = TypeOfRoad.Bridge;
        numDirection = NumDirection.ZERO;
        numberOfLanes = lowerRoadNumLanes+upperRoadNumLanes;
        SetLanes();

        if (PG.Grid.Instance == null) Destroy(boxCollider);
    }
    private void SetLanes()
    {
        for (int i = 0; i < lowerRoadNumLanes; i++)
        {
            lowerLanes.Add(new Lane());
        }
        for (int i = 0; i < upperRoadNumLanes; i++)
        {
            upperLanes.Add(new Lane());
        }
    }
}
