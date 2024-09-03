using PG;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (node.roadType == RoadType.Roundabout || node.roadType == RoadType.Bridge)
                return false;

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

            // Segun el numero de vecinos se actúa de forma distinta
            // Cuando spawneamos un puente tenemos 2 casos segun la direccion que enfoquemos.
            // 1 - Puede que no haya vecinos en esa direccion, por lo que tenemos que expandir y ver si encontramos un nuevo camino
            // 2 - Sí hay camino en esa dirección, hay que comprobar si es válido

            // Guardamos los paths validos que ha generado en las direccions donde no habia vecinos y lo procesamos.
            // Basta con que falle una sola direccion para que se aborte el intento de spawnear un bridge.
            Dictionary<Direction, List<GridNode>> bridgeDictionary = new Dictionary<Direction, List<GridNode>>();

            // Procesar las direcciones en las que no hay camino (Aquí no entra si tiene 4 vecinos ya)
            List<Direction> directionsToExplore = RoadPlacer.Instance.GetAllDirections().Except(neighbours).ToList();
            foreach (Direction direction in directionsToExplore)
            {
                List<GridNode> path = FindPath(node, direction);
                if (path == null) return false;

                bridgeDictionary[direction] = path;
            }

            // Procesar las direcciones en las que sí hay camino (vecinos)
            bool neighboursAreValid = CheckDirections(node, neighbours);
            if (!neighboursAreValid) return false;

            // Si hemos llegado hasta aquí, el puente se va a spawnear
            // Primero hay que procesar los nodos de los paths de cada direccion (si es que hay)

            // Luego se spawnean los assets como si tuvieramos ya 4 vecinos
            bool isHorizontal = true;
            spawnBridge = true;
            if (spawnBridge)
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
        private bool CheckDirections(GridNode startingNode, List<Direction> directions)
        {
            // These is the minimum number of nodes that are checked FROM the starting node in all directions
            int minNodesToCheck = 3;
            foreach (Direction direction in directions)
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
                if (currentNode.roadType == RoadType.Roundabout || currentNeighbours.Count > 2 || currentNeighbours.Count == 1 || IsCornerNode(currentNeighbours ))
                    return false;

                i++;
            }
            return true;
        }
        private List<GridNode> FindPath(GridNode startingNode, Direction direction)
        {
            List<GridNode> path = FindPathInDirection(startingNode, direction);
            if (path != null)
            {
                // Going straight
                return path;
            }
            // Pathfinding
            return FindPathWithPathfinding(startingNode, direction);
        }
        private List<GridNode> FindPathInDirection(GridNode startingNode, Direction direction)
        {
            return RoadPlacer.Instance.GoStraight(direction, startingNode.gridX, startingNode.gridY);
        }
        private List<GridNode> FindPathWithPathfinding(GridNode startingNode, Direction direction)
        {
            // Create path with pathfinder
            int i = 0;
            List<GridNode> shuffledNodes = Visualizer.Instance.pointNodes.OrderBy(x => Random.value).ToList();
            while (i < shuffledNodes.Count)
            {
                GridNode targetNode = shuffledNodes[i];
                if (!RoadPlacer.Instance.CheckMergingNodeTerms(targetNode) || targetNode == startingNode)
                {
                    i++;
                    continue;
                }
                List<GridNode> path = GridPathfinder.instance.FindPath(startingNode, targetNode);
                if (path != null)
                    return path;

                // Path not found, try with another target node
                i++;
            }
            return null;
        }
        private bool IsCornerNode(List<Direction> neighbours)
        {
            if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                return false;

            return true;
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
