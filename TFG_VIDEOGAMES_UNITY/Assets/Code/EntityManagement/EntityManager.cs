using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public float spawnRadius = 100f; // Radius for spawning entities around the player
    public float deactivationRadius = 110f; // Radius for despawning entities (buffer zone)
    public Transform playerTransform; // Reference to the player's position

    private List<GameObject> activeEntities = new List<GameObject>(); // List of currently active entities
    private Queue<GameObject> entityPool = new Queue<GameObject>(); // Pool for reusable entities

    private void Start()
    {
        StartCoroutine(ManageEntities());
    }

    private IEnumerator ManageEntities()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second for performance

            // Check each active entity's distance from the player
            for (int i = activeEntities.Count - 1; i >= 0; i--)
            {
                GameObject entity = activeEntities[i];
                float distanceToPlayer = Vector3.Distance(playerTransform.position, entity.transform.position);

                if (distanceToPlayer > deactivationRadius)
                {
                    // Deactivate the entity if it's beyond the deactivation radius
                    DeactivateEntity(entity);
                    activeEntities.RemoveAt(i);
                }
            }

            // Spawn new entities if needed
            SpawnEntitiesIfNeeded();
        }
    }

    private void SpawnEntitiesIfNeeded()
    {
        // Add logic to spawn entities within the spawnRadius around the player
        // Example: Spawn a pedestrian or car at a random position around the player
        Vector3 randomSpawnPosition = playerTransform.position + Random.insideUnitSphere * spawnRadius;
        randomSpawnPosition.y = playerTransform.position.y; // Ensure it's on the same level

        GameObject entity = GetEntityFromPool();
        entity.transform.position = randomSpawnPosition;
        entity.SetActive(true);
        activeEntities.Add(entity);
    }

    private void DeactivateEntity(GameObject entity)
    {
        entity.SetActive(false);
        entityPool.Enqueue(entity);
    }

    private GameObject GetEntityFromPool()
    {
        if (entityPool.Count > 0)
        {
            return entityPool.Dequeue();
        }

        return null;
        // If pool is empty, instantiate a new entity (prefab can be a car or pedestrian)
        //return Instantiate();
    }
}
