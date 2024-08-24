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
        public GameObject[] centreHousePrefabs;
        public GameObject[] suburbsHousePrefabs;
        public GameObject[] naturePrefabs;
        public bool randomNaturePlacement = false;
        public float randomNaturePlacementThreshold = 0.3f;
        public Dictionary<Vector2Int, GameObject> structuresDictionary = new Dictionary<Vector2Int, GameObject>();
        public Dictionary<Vector2Int, GameObject> natureDictionary = new Dictionary<Vector2Int, GameObject>();
        private List<GridNode> surroundingNodes;

        public void PlaceStructuresAroundRoad()
        {
            // Get all surrounding nodes (nodes available to put a house in)
            surroundingNodes = Visualizer.instance.surroundingNodes.Distinct().ToList();

            List<GridNode> updatedList = new List<GridNode>();
            foreach (GridNode node in surroundingNodes)
            {
                if (node.usage == Usage.decoration)
                {
                    updatedList.Add(node);
                }
            }
            surroundingNodes = updatedList;

            // Get the proper orientations for those hypothetical houses 
            Dictionary<Vector2Int, Direction> spots = GetDirectionsForAssetsAroundRoad();

            List<GridNode> blockedNodes = new List<GridNode>();
            foreach (GridNode node in surroundingNodes)
            {
                if (blockedNodes.Contains(node))
                    continue;

                Vector2Int key = new Vector2Int(node.gridX, node.gridY);
                if (!spots.ContainsKey(key))
                    continue;
                Quaternion rotation = Quaternion.identity;
                switch (spots[key])
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

                int sizePerBuilding = GetSizePerBuilding();
                List<GridNode> fittingNodes = GetFittingNodes(node, spots[key], sizePerBuilding);
                if (fittingNodes == null)
                    continue;

                GameObject house;

                int randomInt;
                switch (node.regionType)
                {
                    case Region.Main:
                        randomInt = UnityEngine.Random.Range(0, centreHousePrefabs.Length);
                        house = Instantiate(centreHousePrefabs[randomInt], node.worldPosition, rotation, transform);
                        break;
                    case Region.Residential:
                        randomInt = UnityEngine.Random.Range(0, housePrefabs.Length);
                        house = Instantiate(housePrefabs[randomInt], node.worldPosition, rotation, transform);
                        break;
                    case Region.Suburbs:
                        randomInt = UnityEngine.Random.Range(0, suburbsHousePrefabs.Length);
                        house = Instantiate(suburbsHousePrefabs[randomInt], node.worldPosition, rotation, transform);
                        break;
                    default:
                        house = Instantiate(housePrefabs[0], node.worldPosition, rotation, transform);
                        break;
                }
                Vector3 housePosition = Vector3.zero;
                foreach (GridNode fittingNode in fittingNodes)
                {
                    Vector2Int fittingNodeKey = new Vector2Int(fittingNode.gridX, fittingNode.gridY);
                    structuresDictionary[fittingNodeKey] = house;
                    fittingNode.occupied = true;
                    blockedNodes.Add(fittingNode);
                    housePosition += fittingNode.worldPosition;
                }
                house.transform.position = housePosition/sizePerBuilding + RoadPlacer.Instance.GetOppositeVectorToDir(spots[key]);
            }
        }
        // This method creates a random size for each house
        private int GetSizePerBuilding()
        {
            float random = UnityEngine.Random.Range(0f, 1f);
            if (random < 0.1f)
            {
                return 2;
            }
            else if (random >= 0.1f && random < 0.5f)
            {
                return 3;
            }
            else if (random >= 0.5f && random < 0.85f)
            {
                return 4;
            }
            else
            {
                return 5;
            }
        }
        // Get the available nodes for placing the house
        private List<GridNode> GetFittingNodes(GridNode referenceNode, Direction direction, int sizePerBuilding)
        {
            int[] startOffset = new int[] { 0, 0 };
            if (direction == Direction.back || direction == Direction.forward)
            {
                startOffset[0] = -sizePerBuilding;
            }
            else
            {
                startOffset[1] = -sizePerBuilding;
            }
            // Test all n combinations and if one succeeds, abort
            for (int i = 0; i < sizePerBuilding; i++)
            {
                // Trying all avaliable nodes in combination
                if (direction == Direction.back || direction == Direction.forward)
                {
                    startOffset[0] += 1;
                }
                else
                {
                    startOffset[1] += 1;
                }

                List<GridNode> combination = new List<GridNode>();
                for (int j = 0; j < sizePerBuilding; j++)
                {
                    int x = referenceNode.gridX + startOffset[0];
                    int y = referenceNode.gridY + startOffset[1];
                    if (direction == Direction.back || direction == Direction.forward)
                    {
                        x += j;
                    }
                    else
                    {
                        y += j;
                    }

                    if (!Grid.Instance.OutOfGrid(x, y))
                    {
                        GridNode currentNode = Grid.Instance.nodesGrid[x, y];
                        if (!currentNode.occupied)
                        {
                            combination.Add(currentNode);
                        }
                        else
                        {
                            j = sizePerBuilding;
                        }
                    }
                }
                if (combination.Count == sizePerBuilding)
                    return combination;
            }
            return null;
        }
        private Dictionary<Vector2Int, Direction> GetDirectionsForAssetsAroundRoad()
        {
            // Para cada nodo libre, encontrar donde est� la carretera respecto a �l. 
            // Si da true, mirando hacia abajo pues ya sabes
            Dictionary<Vector2Int, Direction> freeSpaces = new Dictionary<Vector2Int, Direction>();

            foreach (GridNode node in surroundingNodes)
            {
                foreach (Direction direction in RoadPlacer.Instance.GetAllDirections())
                {
                    int[] offset = RoadPlacer.Instance.DirectionToInt(direction);
                    int x = node.gridX + offset[0];
                    int y = node.gridY + offset[1];

                    if (Grid.Instance.OutOfGrid(x, y))
                        continue;

                    GridNode selectedNode = Grid.Instance.nodesGrid[x, y];
                    if (selectedNode.usage == Usage.road || selectedNode.usage == Usage.point)
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

