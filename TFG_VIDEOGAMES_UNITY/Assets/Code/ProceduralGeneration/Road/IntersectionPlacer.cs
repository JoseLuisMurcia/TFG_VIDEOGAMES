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
            // This method decides what's best to spawn at the given node between a roundabout or a bridge


            // Comprobar viabilidad de la rotonda
            endPos = Vector2Int.zero;
            bool is3Way = neighbours.Count == 3;
            bool isRoundaboutFeasible = false;
            List<GridNode> path = null;
            Direction direction = Direction.zero;
            bool hasOrphanNeighbour = CheckOrphanNeighbours(node, neighbours);

            bool hasRoundaboutNearby = CheckNearbyRoundabouts(node.gridX, node.gridY);
            bool hasEnoughSpace = CheckFreeDistance(node.gridX, node.gridY, neighbours);
            if (!hasOrphanNeighbour)
            {
                if (is3Way)
                {
                    // La dirección en la que hay que comprobar disponibilidad
                    direction = RoadPlacer.Instance.GetAllDirections().Except(neighbours).FirstOrDefault();
                    path = GoStraight(direction, node.gridX, node.gridY);
                    isRoundaboutFeasible = (path != null && !hasRoundaboutNearby && hasEnoughSpace) ? true : false;
                }
                else
                {
                    isRoundaboutFeasible = !hasRoundaboutNearby && hasEnoughSpace && !Was3WayRoundabout(node.gridX, node.gridY, neighbours);
                }
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

            // Determine if roundabout should be spawned
            bool shouldSpawnRoundabout = spawnRoundabout && isRoundaboutFeasible;

            // Spawning
            if (shouldSpawnRoundabout)
            {
                // Spawn roundabout
                SpawnRoundabout(node, is3Way, neighbours, is3Way ? path : null, is3Way ? direction : Direction.zero);
            }
            else
            {
                // Spawn intersection
                // Determine rotation for intersection
                Quaternion intersectionRotation = is3Way ? GetRotationFromNeighbours(neighbours) : Quaternion.identity;
                SpawnIntersection(node, intersectionRotation, is3Way);
            }
        }
        private bool CheckOrphanNeighbours(GridNode node, List<Direction> neighbours)
        {
            foreach (Direction direction in neighbours) 
            {
                if (HasOrphanNeighbour(node, direction))
                    return true;
            }
            return false;
        }
        private bool HasOrphanNeighbour(GridNode startingNode, Direction originalDirection)
        {
            int startX = startingNode.gridX;
            int startY = startingNode.gridY;
            int[] dir;

            Direction currentDirection = originalDirection;
            GridNode currentNode = startingNode;
            while (true)
            {
                dir = RoadPlacer.Instance.DirectionToInt(currentDirection);

                int currentPosX = currentNode.gridX + dir[0];
                int currentPosY = currentNode.gridY + dir[1];

                if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                    return false;

                currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];
                List<Direction> currentNeighbours = RoadPlacer.Instance.GetNeighboursData(currentPosX, currentPosY).neighbours;

                // Has orphan neighbour
                if (currentNeighbours.Count < 2)
                    return true;

                // Arrived to an intersection
                if (currentNeighbours.Count > 2)
                    return false;

                // Cuando tiene 2 vecinos, mirarlos para saber en que direccion avanzar
                foreach (Direction neighbour in currentNeighbours)
                {
                    // This is the neighbour behind the current direction
                    if (RoadPlacer.Instance.GetOppositeDir(neighbour) == currentDirection)
                        continue;

                    // New Direction 
                    if (neighbour != currentDirection)
                    {
                        currentDirection = neighbour;
                        break;
                    }
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

                    if (Grid.Instance.nodesGrid[currentPosX, currentPosY].roadType == RoadType.Roundabout)
                        return true;

                    i++;
                }
            }
            return false;
        }
        private bool CheckFreeDistance(int startX, int startY, List<Direction> neighbours)
        {
            // Comprobar que la rotonda se puede expandir minimo N nodos.
            // Esto es para que no se coloque una curva a 2 nodos del centro
            foreach (Direction direction in neighbours)
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                int i = 2;
                int minDistance = 3;
                while (i <= minDistance)
                {
                    int currentPosX = startX + dir[0] * i;
                    int currentPosY = startY + dir[1] * i;

                    if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                        break;

                    List<Direction> directions = RoadPlacer.Instance.GetNeighboursData(currentPosX, currentPosY).neighbours;
                    if(i < minDistance)
                    {
                        if (directions.Count == 1 || directions.Count >= 3)
                            return false;
                    }
                    else if (i == minDistance)
                    {
                        if (directions.Count < 2)
                            return false;
                    }
                    //GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];
                    //if (!(currentNode.usage == Usage.road || currentNode.usage == Usage.point))
                    //    return false;

                    i++;
                }
            }
            return true;
        }
        private bool Was3WayRoundabout(int startX, int startY, List<Direction> neighbours)
        {
            // Avanzar 2 nodos desde la startPos en todas las direcciones
            // Si se encuentra nodo marcado como roundabout en el segundo nodo, return true
            // No se deben sobreescribir las rotondas de 3 por intersecciones de 4
            foreach (Direction direction in neighbours)
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);

                int i = 2;
                int maxDistance = 2;
                while (i <= maxDistance)
                {
                    int currentPosX = startX + dir[0] * i;
                    int currentPosY = startY + dir[1] * i;

                    if (Grid.Instance.OutOfGrid(currentPosX, currentPosY))
                        break;

                    GridNode currentNode = Grid.Instance.nodesGrid[currentPosX, currentPosY];
                    if (currentNode.roadType == RoadType.Roundabout)
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
            if (roadDictionary.ContainsKey(key))
            {
                Destroy(roadDictionary[key]);
                roadDictionary.Remove(key);
            }
            roadDictionary[key] = Instantiate(intersectionPrefab, node.worldPosition, rotation, transform);
        }
        private void SpawnRoundabout(GridNode node, bool is3Way, List<Direction> neighbours, List<GridNode> path, Direction chosenDir)
        {
            // Remove all nearby roads
            node.roadType = RoadType.Roundabout;
            node.occupied = true;
            Vector2Int originKey = new Vector2Int(node.gridX, node.gridY);
            if (roadDictionary.ContainsKey(originKey))
            {
                Destroy(roadDictionary[originKey]);
                roadDictionary.Remove(originKey);
            }
            RoadPlacer.Instance.GetAllDirections().ForEach(direction =>
            {
                int[] dir = RoadPlacer.Instance.DirectionToInt(direction);
                GridNode newNode = Grid.Instance.nodesGrid[node.gridX + dir[0], node.gridY + dir[1]];
                newNode.roadType = RoadType.Roundabout;
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
            roadDictionary[originKey] = Instantiate(roundabout, node.worldPosition, Quaternion.identity, transform);

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
                    pathNode.occupied = true;
                    pathNode.roadType = RoadType.Roundabout;
                    Visualizer.Instance.MarkSurroundingNodes(pathNode.gridX, pathNode.gridY, neighbourIncrement[0], neighbourIncrement[1]);
                    if (pathNode.usage != Usage.point)
                        pathNode.usage = Usage.road;
                }


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
                endNode.roadType = RoadType.Roundabout;
                Visualizer.Instance.MarkCornerDecorationNodes(path.Last());
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

