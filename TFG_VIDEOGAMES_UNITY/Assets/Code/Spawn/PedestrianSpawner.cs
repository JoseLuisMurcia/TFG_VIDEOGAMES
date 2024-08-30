using PG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    private List<Transform> houses = new List<Transform>();
    [SerializeField] public List<Pedestrian> pedestrianPrefabs;
    [SerializeField] private PedestrianGroupMovement groupMovementPrefab;
    public static PedestrianSpawner Instance;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        if (RoadPlacer.Instance == null)
        {
            var housesAssets = GameObject.FindGameObjectWithTag("Houses");
            foreach (Transform child in housesAssets.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    houses.Add(child);
                }
            }
        }
        else // Procedural
        {
            // TODO
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
    public void Spawn1Pedestrian()
    {
       SpawnPedestrian();
    }
    public void Spawn5Pedestrians()
    {
        for (int i = 0; i < 5; i++)
            SpawnPedestrian();
    }

    public void Spawn50Pedestrians()
    {
        for (int i = 0; i < 50; i++)
            SpawnPedestrian();
    }
    private void SpawnPedestrian()
    {
        int[] housesIds = GetSourceAndDestHouse();
        Vector3 spawnPosition = houses[housesIds[0]].position - houses[housesIds[0]].forward * 3f;
        //Debug.DrawLine(spawnPosition + Vector3.up * 5, spawnPosition - Vector3.up * 5, Color.red, 60f);
        int randomInt = Random.Range(0, pedestrianPrefabs.Count);
        Pedestrian pedestrian = Instantiate(pedestrianPrefabs[randomInt], spawnPosition, houses[housesIds[0]].rotation);
         pedestrian.SetTarget(houses[housesIds[1]]);
        var dest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dest.transform.position = houses[housesIds[1]].position + Vector3.up * 5f;
        dest.transform.localScale = Vector3.one * 5f;
    }

    private void SpawnFormation(int groupSize)
    {
        int[] housesIds = GetSourceAndDestHouse();
        Vector3 spawnPosition = houses[housesIds[0]].position - houses[housesIds[0]].forward * 4f;
        PedestrianGroupMovement groupMovement = Instantiate(groupMovementPrefab, spawnPosition, houses[housesIds[0]].rotation);
        groupMovement.groupSize = groupSize;
        groupMovement.SetTarget(houses[housesIds[1]]);
    }
    public void Spawn2Formation()
    {
        SpawnFormation(2);
    }
    public void Spawn3Formation()
    {
        SpawnFormation(3);
    }

}
