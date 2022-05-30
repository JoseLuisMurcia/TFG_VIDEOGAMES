using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class DecorationPlacer : MonoBehaviour
    {
        public BuildingType[] buildingTypes;
        public GameObject[] naturePrefabs;
        public bool randomNaturePlacement = false;
        public float randomNaturePlacementThreshold = 0.3f;
        public Dictionary<Vector3Int, GameObject> structuresDictionary = new Dictionary<Vector3Int, GameObject>();
        public Dictionary<Vector3Int, GameObject> natureDictionary = new Dictionary<Vector3Int, GameObject>();

        public void PlaceStructuresAroundRoad(List<Node> freeSpots)
        {

        }
    }
}

