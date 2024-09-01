using PG;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PG
{
    public class IntersectionPlacer : MonoBehaviour
    {
        // This script decides what to spawn when there's 3 or 4 neighbour nodes, a roundabout or an intersection
        // Start is called before the first frame update
        private GameObject intersection3way, intersection4way, roundabout, end;
        private Dictionary<Vector2Int, GameObject> roadDictionary;
        private Vector2Int endPos;

        public void HandleIntersection(GridNode node, List<Direction> neighbours)
        {
            // Primero identificar si es factible colocar una rotonda en el nodo
            // Comprobar alrededores, ocupa 5 nodos, el actual + alrededores.
            // Y si ya se han instanciado carreteras alrededor, hay que agregarlas como Keys a ser borradas
            // Comprobar viabilidad
            // Spawnear
            endPos = Vector2Int.zero;
            bool is3Way = neighbours.Count == 3;
            bool isRoundaboutFeasible;
            List<GridNode> path = null;
            Direction direction = Direction.zero;
            bool hasRoundaboutNearby = CheckNearbyRoundabouts(node.gridX, node.gridY);
            // Viabilidad de la rotonda
            if (is3Way)
            {
                // La dirección en la que hay que comprobar disponibilidad
                direction = RoadPlacer.Instance.GetAllDirections().Except(neighbours).FirstOrDefault();
                path = GoStraight(direction, node.gridX, node.gridY);
                isRoundaboutFeasible = (path != null && !hasRoundaboutNearby) ? true : false;
            }
            else
            {
                isRoundaboutFeasible = !hasRoundaboutNearby;
            }

            // Calcular probabilidad de spawnear rotonda
            Region roadRegion = node.regionType;
            bool spawnRoundabout;
            float randomValue = is3Way ? Random.value - 0.1f : Random.value;
            switch (roadRegion)
            {
                // TODO Ajustar probabilidades
                case Region.Main:
                    spawnRoundabout = randomValue > 0.35 ? false : true;
                    break;
                case Region.Residential:
                    spawnRoundabout = randomValue > 0.25 ? false : true;
                    break;
                case Region.Suburbs:
                    spawnRoundabout = randomValue > 0.45 ? false : true;
                    break;
                default:
                    spawnRoundabout = false;
                    break;
            }

            // Spawning
            if (is3Way)
            {
                // 3 way
                if (spawnRoundabout && isRoundaboutFeasible)
                {
                    SpawnRoundabout(node, is3Way, neighbours, path, direction);
                }
                else
                {
                    SpawnIntersection(node, GetRotationFromNeighbours(neighbours), is3Way);
                }
            }
            else
            {
                // 4 way
                if (spawnRoundabout && isRoundaboutFeasible)
                {
                    SpawnRoundabout(node, is3Way, neighbours, null, Direction.zero);
                }
                else
                {
                    SpawnIntersection(node, Quaternion.identity, is3Way);
                }
            }
        }
        private bool CheckNearbyRoundabouts(int startX, int startY)
        {
            foreach(Direction direction in RoadPlacer.Instance.GetAllDirections())
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                int i = 1;
                int minDistance = 10;
                while (i <= minDistance)
                {
                    int currentPosX = startX + dir[0] * i;
                    int currentPosY = startY + dir[1] * i;

                    if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                        break;

                    if (Grid.Instance.nodesGrid[currentPosX, currentPosY].isRoundabout)
                        return true;

                    i++;
                }
            }
            return false;
        }
        private List<GridNode> GoStraight(Direction direction, int startX, int startY)
        {
            List<GridNode> path = new List<GridNode> { Grid.Instance.nodesGrid[startX, startY] };
            int[] dir = RoadPlacer.Instance.DirectionToInt(direction);
            int[] neighbourIncrement = Visualizer.Instance.GetLateralIncrementOnDirection(direction);

            endPos = new Vector2Int(startX + dir[0] * 2, startY + dir[1] * 2);
            int i = 1;
            int maxIterations = 3;
            while (i <= maxIterations)
            {
                int currentPosX = startX + dir[0] * i;
                int currentPosY = startY + dir[1] * i;

                bool outOfGrid = Grid.Instance.OutOfGrid(currentPosX, currentPosY);
                if (outOfGrid) 
                {
                    // If out of grid, return endPos for the last iteration
                    if (i < maxIterations)
                        return null;
                    return path;
                } 

                GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];

                if (i < maxIterations)
                    path.Add(currentNode);

                if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)             
                    return null;

                if (!Visualizer.Instance.EnoughSpace(currentPosX, currentPosY, neighbourIncrement[0], neighbourIncrement[1]))
                    return null;

                i++;
            }      
            return path;
        }
        private Quaternion GetRotationFromNeighbours(List<Direction> neighbours)
        {
            if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
            {
                return Quaternion.Euler(0, -90, 0);
            }
            else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.back) && neighbours.Contains(Direction.left))
            {
                return Quaternion.Euler(0, 180, 0);
            }
            else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
            {
                return Quaternion.Euler(0, 90, 0);
            }
            return Quaternion.identity;
        }
        private void SpawnIntersection(GridNode node, Quaternion rotation, bool is3Way)
        {
            Vector2Int key = new Vector2Int(node.gridX, node.gridY);
            GameObject intersectionPrefab = is3Way ? intersection3way : intersection4way;
            roadDictionary[key] = Instantiate(intersectionPrefab, node.worldPosition, rotation, transform);
        }
        private void SpawnRoundabout(GridNode node, bool is3Way, List<Direction> neighbours, List<GridNode> path, Direction chosenDir)
        {
            // Remove all nearby roads
            node.isRoundabout = true;
            RoadPlacer.Instance.GetAllDirections().ForEach(direction =>
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);
                GridNode newNode = Grid.Instance.nodesGrid[node.gridX + dir[0], node.gridY + dir[1]];
                newNode.isRoundabout = true;
                newNode.occupied = true;
                newNode.usage = Usage.road;
            });
            foreach (Direction direction in neighbours)
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);
                Vector2Int key = new Vector2Int(node.gridX + dir[0], node.gridY + dir[1]);
                if (roadDictionary.ContainsKey(key))
                {
                    Destroy(roadDictionary[key]);
                    roadDictionary.Remove(key);
                }
            }

            // Instantiate roundabout
            Vector2Int roundaboutKey = new Vector2Int(node.gridX, node.gridY);
            roadDictionary[roundaboutKey] = Instantiate(roundabout, node.worldPosition, Quaternion.identity, transform);

            // 3 Way, we need
            if (is3Way)
            {
                // Mark nodes
                for (int j = 2; j <= 2; j++)
                {
                    if (path.Count < 3 && j == 2)
                        break;

                    GridNode pathNode = path[j];
                    int[] neighbourIncrement = Visualizer.Instance.GetLateralIncrementOnDirection(chosenDir);
                    Visualizer.Instance.MarkSurroundingNodes(pathNode.gridX, pathNode.gridY, neighbourIncrement[0], neighbourIncrement[1]);
                    pathNode.occupied = true;
                    pathNode.isRoundabout = true;
                    if (pathNode.usage != Usage.point)
                        pathNode.usage = Usage.road;
                }

                Visualizer.Instance.MarkCornerDecorationNodes(path.Last());

                // Spawn end road
                Direction oppositeDirection = RoadPlacer.Instance.GetOppositeDir(RoadPlacer.Instance.GetAllDirections().Except(neighbours).FirstOrDefault());
                Quaternion endRotation;
                switch (oppositeDirection)
                {
                    case Direction.left:
                        endRotation = Quaternion.identity;
                        break;
                    case Direction.right:
                        endRotation = Quaternion.Euler(0, 180, 0);
                        break;
                    case Direction.forward:
                        endRotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case Direction.back:
                        endRotation = Quaternion.Euler(0, -90, 0);
                        break;
                    case Direction.zero:
                    default:
                        endRotation = Quaternion.identity;
                        break;
                }
                GridNode endNode = Grid.Instance.nodesGrid[endPos.x, endPos.y];
                endNode.occupied = true;
                endNode.isRoundabout = true;
                //endNode.usage = Usage.point;
                roadDictionary[endPos] = Instantiate(end, endNode.worldPosition, endRotation, transform);
            }
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        public void SetIntersectionPrefabs(GameObject _intersection3way, GameObject _intersection4way, GameObject _roundabout, GameObject _end, Dictionary<Vector2Int, GameObject> _roadDictionary)
        {
            intersection3way = _intersection3way;
            intersection4way = _intersection4way;
            roundabout = _roundabout;
            end = _end;
            roadDictionary = _roadDictionary;
        }
    }
}

