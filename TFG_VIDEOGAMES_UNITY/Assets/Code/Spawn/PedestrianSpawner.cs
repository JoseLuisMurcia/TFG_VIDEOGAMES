using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    List<Transform> houses = new List<Transform>();
    public static PedestrianSpawner instance;
    [SerializeField] Pedestrian[] pedestrianPrefabs;
    void Start()
    {
        instance = this;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                houses.Add(child);
            }
        }
    }

    public void Spawn()
    {
        float minDistance = 8f;
        float distance = Mathf.NegativeInfinity;
        int srcHouseId = Random.Range(0, houses.Count);
        int dstHouseId = -1;
        bool destFound = false;
        do
        {
            dstHouseId = Random.Range(0, houses.Count);
            if (dstHouseId != srcHouseId)
            {
                distance = Vector3.Distance(houses[srcHouseId].position, houses[dstHouseId].position);
                if (distance > minDistance)
                    destFound = true;
            }
        } while(!destFound);
        Vector3 spawnPosition = houses[srcHouseId].position - houses[srcHouseId].forward * 3f;
        //Debug.DrawLine(spawnPosition + Vector3.up * 5, spawnPosition - Vector3.up * 5, Color.red, 60f);
        int randomInt = Random.Range(0, pedestrianPrefabs.Length);
        Pedestrian pedestrian = Instantiate(pedestrianPrefabs[randomInt], spawnPosition, houses[srcHouseId].rotation);
        pedestrian.SetTarget(houses[dstHouseId]);
    }

    public void SpawnFivePedestrians()
    {
        for (int i = 0; i < 5; i++)
            Spawn();
    }

    public void Spawn50Pedestrians()
    {
        for (int i = 0; i < 50; i++)
            Spawn();
    }

}
