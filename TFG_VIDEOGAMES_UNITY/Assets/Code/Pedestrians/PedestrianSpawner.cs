using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    List<Vector3> houses = new List<Vector3>();
    public static PedestrianSpawner instance;
    [SerializeField] Pedestrian pedestrianPrefab;
    void Start()
    {
        instance = this;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                houses.Add(child.position);
            }
        }
    }

    public void Spawn()
    {
        float minDistance = 10f;
        float distance = Mathf.NegativeInfinity;
        int srcHouseId = Random.Range(0, houses.Count);
        int dstHouseId;
        while (distance > minDistance)
        {
            dstHouseId = Random.Range(0, houses.Count);
            if (dstHouseId != srcHouseId)
            {
                distance = Vector3.Distance(houses[srcHouseId], houses[dstHouseId]);
            }
        }
        Pedestrian pedestrian = Instantiate(pedestrianPrefab);
        // Add destination to pedestrian
    }

}
