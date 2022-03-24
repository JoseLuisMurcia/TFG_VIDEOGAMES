using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] GameObject carPrefab;

    public void SpawnOneCar()
    {
        Vector3 randomPos = WorldGrid.Instance.GetRandomNodeInRoads().worldPosition;
        Instantiate(carPrefab, randomPos, Quaternion.identity);
    }

    public void SpawnFiveCars()
    {
        for (int i = 0; i < 5; i++)
            SpawnOneCar();
    }
}
