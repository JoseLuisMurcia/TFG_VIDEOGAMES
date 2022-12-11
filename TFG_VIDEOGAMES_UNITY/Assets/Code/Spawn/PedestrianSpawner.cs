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
    // 0 for src, 1 for dst
    private int[] GetSourceAndDestHouse()
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
        } while (!destFound);
        return new int[] {srcHouseId, dstHouseId };
    }
    public void Spawn()
    {
        int[] housesIds = GetSourceAndDestHouse();
        Vector3 spawnPosition = houses[housesIds[0]].position - houses[housesIds[0]].forward * 3f;
        //Debug.DrawLine(spawnPosition + Vector3.up * 5, spawnPosition - Vector3.up * 5, Color.red, 60f);
        int randomInt = Random.Range(0, pedestrianPrefabs.Length);
        Pedestrian pedestrian = Instantiate(pedestrianPrefabs[randomInt], spawnPosition, houses[housesIds[0]].rotation);
        pedestrian.SetTarget(houses[housesIds[1]]);
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


    public void Spawn3Formation()
    {
        FormationManager formationManager = new FormationManager();
        List<Pedestrian> pedestrians= new List<Pedestrian>();
        int[] housesIds = GetSourceAndDestHouse();
        Vector3 spawnPosition = houses[housesIds[0]].position - houses[housesIds[0]].forward * 3f;
        List<int> pedestrianIds = GenerateFormation(3);
        foreach (int id in pedestrianIds)
            pedestrians.Add(Instantiate(pedestrianPrefabs[id], spawnPosition, houses[housesIds[0]].rotation));

    }

    private List<int> GenerateFormation(int n)
    {
        HashSet<int> candidates = new HashSet<int>();
        System.Random r = new System.Random();
        while (candidates.Count < pedestrianPrefabs.Length)
        {
            candidates.Add(r.Next(0, pedestrianPrefabs.Length - 1));
        }

        List<int> result = new List<int>();
        result.AddRange(candidates);
        return result;
    }

}
