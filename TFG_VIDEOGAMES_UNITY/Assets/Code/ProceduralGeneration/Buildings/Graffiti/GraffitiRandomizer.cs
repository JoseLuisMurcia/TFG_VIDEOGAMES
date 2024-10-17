using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class GraffitiRandomizer : MonoBehaviour
    {
        [SerializeField]
        private List<GraffitiGameObject> graffitiPrefabs = new List<GraffitiGameObject>();
        [SerializeField]
        private List<Graffiti> graffities = new List<Graffiti>();
        void Start()
        {
            if (graffities.Count == 0 || graffitiPrefabs.Count == 0) return;

            // Loop through each Graffiti object and decide if we should instantiate a graffiti there.
            foreach (var graffiti in graffities)
            {
                // Check the probability of spawning a graffiti based on its rarity.
                if (ShouldSpawnGraffiti(graffiti.graffitiInfo.rarity))
                {
                    // Get a random prefab from the available graffiti prefabs.
                    GraffitiGameObject selectedPrefab = graffitiPrefabs[Random.Range(0, graffitiPrefabs.Count)];

                    // Calculate the random vertical offset within the allowed range.
                    float yOffset = Random.Range(graffiti.graffitiInfo.yMinOffset, graffiti.graffitiInfo.yMaxOffset);

                    // Initialize the lateral offset to zero.
                    float xOffset = 0f;

                    // If the graffiti is small, also calculate the random lateral offset.
                    if (selectedPrefab.isSmall)
                    {
                        xOffset = Random.Range(graffiti.graffitiInfo.xMinOffset, graffiti.graffitiInfo.xMaxOffset);
                    }

                    // Calculate the position for the graffiti, adding the yOffset.
                    Vector3 spawnPosition = graffiti.transform.position + new Vector3(xOffset, yOffset, 0);

                    // Instantiate the selected graffiti prefab at the calculated position.
                    Instantiate(selectedPrefab, spawnPosition, graffiti.transform.rotation, transform);
                }
            }
        }


        // Determines whether to spawn a graffiti based on its rarity.
        private bool ShouldSpawnGraffiti(Graffiti.Rarity rarity)
        {
            // Define the probabilities for each rarity.
            float spawnProbability = 0f;

            switch (rarity)
            {
                case Graffiti.Rarity.Low:
                    spawnProbability = 0.2f; // 20% chance
                    break;
                case Graffiti.Rarity.Medium:
                    spawnProbability = 0.5f; // 50% chance
                    break;
                case Graffiti.Rarity.High:
                    spawnProbability = 0.8f; // 80% chance
                    break;
            }

            // Roll a random value between 0 and 1 and return true if it's less than the spawn probability.
            return Random.value < spawnProbability;
        }
    }
}
