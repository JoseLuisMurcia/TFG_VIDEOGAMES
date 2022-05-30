using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace PG
{
    public class DecorationPlacer : MonoBehaviour
    {
        public GameObject[] housePrefabs;
        public BuildingType[] buildingTypes;
        public GameObject[] naturePrefabs;
        public bool randomNaturePlacement = false;
        public float randomNaturePlacementThreshold = 0.3f;
        public Dictionary<Vector2Int, GameObject> structuresDictionary = new Dictionary<Vector2Int, GameObject>();
        public Dictionary<Vector2Int, GameObject> natureDictionary = new Dictionary<Vector2Int, GameObject>();
        private List<Node> surroundingNodes;

        public void PlaceStructuresAroundRoad()
        {
            surroundingNodes = Visualizer.instance.surroundingNodes.Distinct().ToList();


            Dictionary<Vector2Int, Direction> freeSpaces = GetDirectionsForAssetsAroundRoad();

            List<Node> blockedNodes = new List<Node>();
            foreach (Node node in surroundingNodes)
            {
                if (blockedNodes.Contains(node))
                    continue;

                Vector2Int key = new Vector2Int(node.gridX, node.gridY);
                Quaternion rotation = Quaternion.identity;
                switch (freeSpaces[key])
                {
                    case Direction.left:
                        rotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case Direction.right:
                        rotation = Quaternion.Euler(0, -90, 0);

                        break;
                    case Direction.forward:
                        rotation = Quaternion.Euler(0, 180, 0);

                        break;
                    default:
                        break;
                }
                int randomInt = UnityEngine.Random.Range(0, housePrefabs.Length);
                structuresDictionary[key] = Instantiate(housePrefabs[randomInt], node.worldPosition, rotation, transform);
                blockedNodes.Add(node);
            }
        }

        private bool VerifyIfBuildingFits(
            int halfSize,
            Dictionary<Vector3Int, Direction> freeEstateSpots,
            KeyValuePair<Vector3Int, Direction> freeSpot,
            ref List<Vector3Int> tempPositionsBlocked)
        {
            Vector3Int direction = Vector3Int.zero;
            if (freeSpot.Value == Direction.back || freeSpot.Value == Direction.forward)
            {
                direction = Vector3Int.right;
            }
            else
            {
                direction = new Vector3Int(0, 0, 1);
            }
            for (int i = 1; i < halfSize; i++)
            {
                var pos1 = freeSpot.Key + direction * i;
                var pos2 = freeSpot.Key - direction * i;
                if (!freeEstateSpots.ContainsKey(pos1) || !freeEstateSpots.ContainsKey(pos2))
                {
                    return false;
                }
                tempPositionsBlocked.Add(pos1);
                tempPositionsBlocked.Add(pos2);
            }
            return true;
        }

        private Dictionary<Vector2Int, Direction> GetDirectionsForAssetsAroundRoad()
        {
            // Para cada nodo libre, encontrar donde está la carretera respecto a él. 
            // Si da true, mirando hacia abajo pues ya sabes
            Dictionary<Vector2Int, Direction> freeSpaces = new Dictionary<Vector2Int, Direction>();

            foreach (Node node in surroundingNodes)
            {
                foreach (Direction direction in RoadPlacer.Instance.GetAllDirections())
                {
                    int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
                    int x = node.gridX + offset[0];
                    int y = node.gridY + offset[1];

                    if (Grid.Instance.OutOfGrid(x, y))
                        continue;

                    Node selectedNode = Grid.Instance.nodesGrid[x, y];
                    if (selectedNode.usage == Usage.decoration)
                    {
                        freeSpaces.Add(new Vector2Int(node.gridX, node.gridY), direction);
                        break;
                    }
                }
            }
            return freeSpaces;

        }
        public void Reset()
        {

        }
    }
}

