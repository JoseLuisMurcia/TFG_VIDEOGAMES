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

    [HideInInspector] public List<Lane> upperLanes = new List<Lane>();
    [HideInInspector] public List<Lane> lowerLanes = new List<Lane>();


    // Start is called before the first frame update
    void Start()
    {
        typeOfRoad = TypeOfRoad.Bridge;
        numDirection = NumDirection.ZERO;
        SetLanes();
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
