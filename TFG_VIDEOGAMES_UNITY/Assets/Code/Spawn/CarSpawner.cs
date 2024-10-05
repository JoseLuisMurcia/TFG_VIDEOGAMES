using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] carPrefabs;
    [SerializeField] GameObject playerCar;
    [SerializeField] GameObject cameraGameObject;
    [SerializeField] Material[] carMaterials;
    public Dictionary<string, GameObject> carPrefabsDictionary = new Dictionary<string, GameObject>();

    private void Start()
    {
        foreach (GameObject car in carPrefabs)
            carPrefabsDictionary.Add(car.name, car);
    }
    public void SpawnOneCar()
    {
        
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

        bool isProcedural = RoadConnecter.Instance != null;
        Node startNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads();
        Vector3 directionToLookAt = (startNode.neighbours[0].worldPosition - startNode.worldPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(directionToLookAt, Vector3.up);
        Vector3 spawnPos = startNode.worldPosition - directionToLookAt;
        GameObject instantiatedCar = Instantiate(prefab, spawnPos, rotation);
        RandomizeCarColor(instantiatedCar.transform, key);

        // Spawn with the correct rotation
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
            GameObject spoiler = null;
            foreach(Transform child in body.transform)
            {
                spoiler = child.gameObject;
            }
            Renderer spoilerRenderer = spoiler.GetComponent<MeshRenderer>();
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

    public void Spawn25Cars()
    {
        for (int i = 0; i < 25; i++)
            SpawnOneCar();
    }

    public void Spawn50Cars()
    {
        for (int i = 0; i < 50; i++)
            SpawnOneCar();
    }

    public void Spawn100Cars()
    {
        for (int i = 0; i < 100; i++)
            SpawnOneCar();
    }
    public void PlayAsCar()
    {
        // Spawn player car
        bool isProcedural = RoadConnecter.Instance != null;
        Node startNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads();
        Vector3 directionToLookAt = (startNode.neighbours[0].worldPosition - startNode.worldPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(directionToLookAt, Vector3.up);
        Vector3 spawnPos = startNode.worldPosition - directionToLookAt;
        GameObject instantiatedCar = Instantiate(playerCar, spawnPos, rotation);

        // Enable camera for player
        var cameraFollow = cameraGameObject.GetComponent<CameraFollow>();
        cameraFollow.SetTarget(instantiatedCar.transform);
        cameraFollow.enabled = true;
    }
}
