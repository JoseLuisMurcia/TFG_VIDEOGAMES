using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class GridSpawner : MonoBehaviour
    {
        public BuildingGenerator buildingGeneratorPrefab;
        private Vector3 worldBottomLeft = Vector3.zero;
        public float gridOffset = 2f;

        public void SpawnGrid(HashSet<GridNode> nodes, PerlinNoiseGenerator perlinNoiseGenerator)
        {
            worldBottomLeft = Grid.Instance.worldBottomLeft;
            float nodeDiameter = Grid.Instance.nodeDiameter;
            float nodeRadius = Grid.Instance.nodeRadius;
            foreach (GridNode node in nodes) 
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (node.gridX * nodeDiameter + nodeRadius) + Vector3.forward * (node.gridY * nodeDiameter + nodeRadius);
                BuildingGenerator generator = Instantiate(buildingGeneratorPrefab, worldPoint, transform.rotation);
                if (generator != null)
                {
                    generator.transform.SetParent(transform);
                    generator.SetPerlinNoiseGenerator(perlinNoiseGenerator);
                }
               
            }
        }


    }
}
