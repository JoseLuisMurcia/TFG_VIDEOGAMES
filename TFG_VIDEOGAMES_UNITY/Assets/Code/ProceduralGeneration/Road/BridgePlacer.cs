using PG;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace PG
{
    public class BridgePlacer : MonoBehaviour
    {
        private GameObject bridge, slantCurve, slantCurve2, slantFlat, slantFlatHigh, slantFlatHigh2, straight;
        private Dictionary<Vector2Int, GameObject> roadDictionary;

        // Returns true if a bridge is spawned
        // Returns false if not
        public bool SpawnBridge(GridNode node, List<Direction> neighbours)
        {
            // Describir algoritmo
            // Segun el numero de vecinos se actúa de forma distinta
            // Si tenemos 4 vecinos, simplemente tenemos que ver si nos podemos expandir N nodos en la direccion y sentido en la que coloquemos el puente
            // Si tenemos 3 vecinos, colocaremos el lado expandible del puente en la direccion completada y tendremos que expandirnos en la que no lo está
            // Si tenemos 2 vecinos, colocaremos el lado expandible del puente en la direccion completada y tendremos que expandirnos en la que no lo está
            // Si tenemos 1 vecino, comprobar la expansion en todos los lados que faltan
            if (node.roadType == RoadType.Roundabout || node.roadType == RoadType.Bridge)
                return false;

            // This variable means the direction of expansion of the bridge
            // The asset is spawned by default as vertical
            // 90 euler degrees rotation on Y axis turns it to horizontal
            bool isHorizontal = true;
            int numNeighbours = neighbours.Count;
            bool bridgeCanBeSpawned = false;
            switch (numNeighbours)
            {
                case 1:
                    return false;
                    break;
                case 2:
                    return false;
                    break;
                case 3:
                    return false;
                    break;
                case 4:
                    bridgeCanBeSpawned = CheckAllDirections(node);
                    isHorizontal = Random.value > .5f;
                    break;
                default:
                    return false;
            }

            // Calcular probabilidad de spawnear bridge segun region
            Region roadRegion = node.regionType;
            bool spawnBridge;
            float randomValue = Random.value;
            switch (roadRegion)
            {
                // TODO Ajustar probabilidades
                case Region.Main:
                    spawnBridge = randomValue > 0.1 ? false : true;
                    break;
                case Region.Residential:
                    spawnBridge = randomValue > 0.3 ? false : true;
                    break;
                case Region.Suburbs:
                    spawnBridge = randomValue > 0.5 ? false : true;
                    break;
                default:
                    spawnBridge = false;
                    break;
            }

            spawnBridge = true;
            if (bridgeCanBeSpawned)
            {
                SpawnBridge(node, isHorizontal);
                return true;
            }
            return false;
        }
        private void SpawnBridge(GridNode bridgeNode, bool isHorizontal)
        {
            // Instantiate bridge
            bridgeNode.roadType = RoadType.Bridge;
            Vector2Int bridgeKey = new Vector2Int(bridgeNode.gridX, bridgeNode.gridY);
            Quaternion bridgeRotation = isHorizontal ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            DeleteGameObjectIfExistent(bridgeKey);
            roadDictionary[bridgeKey] = Instantiate(bridge, bridgeNode.worldPosition, bridgeRotation, transform);

            // Instantiate slants in the bridge orientation
            foreach (Direction direction in isHorizontal ? GetHorizontalDirections() : GetVerticalDirections())
            {
                Quaternion slantRotation = GetSlantRotation(direction);
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                int currentX = bridgeNode.gridX + dir[0];
                int currentY = bridgeNode.gridY + dir[1];

                GridNode currentNode = Grid.Instance.nodesGrid[currentX, currentY];
                currentNode.roadType = RoadType.Bridge;
                Vector2Int slantKey = new Vector2Int(currentNode.gridX, currentNode.gridY);
                DeleteGameObjectIfExistent(slantKey);

                // Also check for one position extra in the current direction, as a straight road might be there
                int nextX = bridgeNode.gridX + dir[0] * 2;
                int nextY = bridgeNode.gridY + dir[1] * 2;
                GridNode nextNode = Grid.Instance.nodesGrid[nextX, nextY];
                nextNode.roadType = RoadType.Bridge;
                DeleteGameObjectIfExistent(new Vector2Int(nextX, nextY));

                // Instantiate
                Vector3 slantPosition = (nextNode.worldPosition + currentNode.worldPosition) * .5f;
                roadDictionary[slantKey] = Instantiate(slantCurve, slantPosition, slantRotation, transform);
            }
        }
        private bool CheckAllDirections(GridNode startingNode)
        {
            // These is the minimum number of nodes that are checked FROM the starting node in all directions
            int minNodesToCheck = 3;
            foreach (Direction direction in RoadPlacer.Instance.GetAllDirections())
            {
                if (!IsPathFree(startingNode, direction, minNodesToCheck))
                    return false;
            }
            return true;
        }
        private bool IsPathFree(GridNode startingNode, Direction direction, int numNodes)
        {
            int startX = startingNode.gridX;
            int startY = startingNode.gridY;
            int[] dir = RoadPlacer.Instance.DirectionToInt(direction);
            int[] neighbourIncrement = Visualizer.Instance.GetLateralIncrementOnDirection(direction);

            int i = 1;
            while (i <= numNodes) 
            {
                int currentPosX = startX + dir[0] * i;
                int currentPosY = startY + dir[1] * i;

                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    return false;

                GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];
                List<Direction> currentNeighbours = RoadPlacer.Instance.GetNeighboursData(currentPosX, currentPosY).neighbours;

                // Check the feasability of the node
                if (currentNode.roadType == RoadType.Roundabout || currentNeighbours.Count > 2 || !IsCornerNode(currentNeighbours))
                    return false;

                i++;
            }
            return true;
        }
        private bool IsCornerNode(List<Direction> neighbours)
        {
            if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                return true;

            return false;
        }
        private void DeleteGameObjectIfExistent(Vector2Int key)
        {
            if (roadDictionary.ContainsKey(key))
            {
                Destroy(roadDictionary[key]);
                roadDictionary.Remove(key);
            }
        }
        private List<Direction> GetHorizontalDirections()
        {
            return new List<Direction> { Direction.left, Direction.right };
        }
        private List<Direction> GetVerticalDirections()
        {
            return new List<Direction> { Direction.back, Direction.forward };
        }
        private Quaternion GetSlantRotation(Direction direction)
        {
            // If the slant is positioned to X direction from the bridge, return Y direction
            switch (direction)
            {
                case Direction.left:
                    return Quaternion.Euler(0, 180, 0);
                case Direction.right:
                    return Quaternion.identity;
                case Direction.forward:
                    return Quaternion.Euler(0, -90, 0);
                case Direction.back:
                    return Quaternion.Euler(0, 90, 0);
                case Direction.zero:
                default:
                    return Quaternion.identity;
            }
        }
        public void SetBridgePrefabs(
            GameObject _bridge,
            GameObject _slantCurve,
            GameObject _slantCurve2,
            GameObject _slantFlat,
            GameObject _slantFlatHigh,
            GameObject _slantFlatHigh2,
            GameObject _straight,
            Dictionary<Vector2Int, GameObject> _roadDictionary)
        {
            bridge = _bridge;
            slantCurve = _slantCurve;
            slantCurve2 = _slantCurve2;
            slantFlat = _slantFlat;
            slantFlatHigh = _slantFlatHigh;
            slantFlatHigh2 = _slantFlatHigh2;
            straight = _straight;
            roadDictionary = _roadDictionary;
        }
    }
}
