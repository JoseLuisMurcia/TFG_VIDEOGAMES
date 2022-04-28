using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] carPrefabs;
    [SerializeField] Material[] carMaterials;
    public Dictionary<string, GameObject> carPrefabsDictionary = new Dictionary<string, GameObject>();

    private void Start()
    {
        foreach (GameObject car in carPrefabs)
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
        else if (randomNumber >= 0.05f && randomNumber < 0.1f)
        {
            key = "van";
        }
        else if (randomNumber >= 0.1f && randomNumber < 0.15f)
        {
            key = "suvLuxury";
        }
        else if (randomNumber >= 0.15f && randomNumber < 0.2f)
        {
            key = "truck";
        }
        else if (randomNumber >= 0.2f && randomNumber < 0.7f)
        {
            key = "sedan";
        }
        else if (randomNumber >= 0.7f && randomNumber < 0.8f)
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
        RandomizeCarColor(instantiatedCar.transform, key);

        // Spawn with the correct rotation
        instantiatedCar.transform.LookAt(startNode.neighbours[0].worldPosition);
        PathFollower pathFollower = instantiatedCar.GetComponent<PathFollower>();
        pathFollower.StartPathfindingOnSpawn(startNode);
    }

    private void RandomizeCarColor(Transform car, string key)
    {

        GameObject body = null;
        foreach (Transform child in car)
        {
            if (child.gameObject.name == "body")
            {
                body = child.gameObject;
                break;
            }
        }
        Renderer carRenderer = body.GetComponent<MeshRenderer>();
        int matId = Random.Range(0, carMaterials.Length);
        Material[] _materials = carRenderer.materials;
        _materials[1] = carMaterials[matId];
        carRenderer.materials = _materials;
        if (key == "sedanSport")
        {
            Renderer spoilerRenderer = body.transform.GetComponentInChildren<MeshRenderer>();
            Material[] _spoilerMats = spoilerRenderer.materials;
            _spoilerMats[1] = carMaterials[matId];
            spoilerRenderer.materials = _spoilerMats;
        }
    }

    public void SpawnFiveCars()
    {
        for (int i = 0; i < 5; i++)
            SpawnOneCar();
    }
}
