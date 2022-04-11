using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] GameObject carPrefab;

    public void SpawnOneCar()
    {
        Node startNode = WorldGrid.Instance.GetRandomNodeInRoads();
        GameObject instantiatedCar = Instantiate(carPrefab, startNode.worldPosition, Quaternion.identity);
        // Spawn with the correct rotation
        instantiatedCar.transform.LookAt(startNode.neighbours[0].worldPosition);
        PathFollower pathFollower = instantiatedCar.GetComponent<PathFollower>();
        pathFollower.StartPathfinding(startNode);
    }

    public void SpawnFiveCars()
    {
        for (int i = 0; i < 5; i++)
            SpawnOneCar();
    }
}
