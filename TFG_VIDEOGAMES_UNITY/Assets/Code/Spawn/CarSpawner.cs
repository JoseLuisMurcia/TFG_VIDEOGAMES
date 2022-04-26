using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] carPrefabs;
    public Dictionary<string, GameObject> carPrefabsDictionary = new Dictionary<string, GameObject>();

    private void Start()
    {
        foreach(GameObject car in carPrefabs)
            carPrefabsDictionary.Add(car.name, car);
    }
    public void SpawnOneCar()
    {
        Node startNode = WorldGrid.Instance.GetRandomNodeInRoads();
        float randomNumber = Random.value;
        string key = "sedan";
        if (randomNumber < 0.05f)
        {
            key = "delivery";
        }
        else if (randomNumber >= 0.05f && randomNumber < 0.15f)
        {
            key = "van";
        }
        else if (randomNumber >= 0.15f && randomNumber < 0.2f)
        {
            key = "suvLuxury";
        }
        else if (randomNumber >= 0.2f && randomNumber < 0.25f)
        {
            key = "truck";
        }
        else if (randomNumber >= 0.25f && randomNumber < 0.65f)
        {
            key = "sedan";
        }
        else if (randomNumber >= 0.65f && randomNumber < 0.8f)
        {
            key = "sedanSport";
        }
        else if (randomNumber >= 0.8f && randomNumber <= 1f)
        {
            key = "suv";
        }
        GameObject prefab;
        carPrefabsDictionary.TryGetValue(key, out prefab);

        GameObject instantiatedCar = Instantiate(prefab, startNode.worldPosition, Quaternion.identity);
        // Spawn with the correct rotation
        instantiatedCar.transform.LookAt(startNode.neighbours[0].worldPosition);
        PathFollower pathFollower = instantiatedCar.GetComponent<PathFollower>();
        pathFollower.StartPathfindingOnSpawn(startNode);
    }

    public void SpawnFiveCars()
    {
        for (int i = 0; i < 5; i++)
            SpawnOneCar();
    }
}
