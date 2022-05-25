using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PG
{
    public class RoadPlacer : MonoBehaviour
    {
        public GameObject roadStraight, roadCorner, road3way, road4way, roadEnd;
        private PG.Grid grid;
        private PG.Visualizer visualizer;
        List<Node> updatedNodes = new List<Node>();
        Dictionary<Vector2Int, GameObject> roadDictionary = new Dictionary<Vector2Int, GameObject>();
        public void PlaceRoadAssets(PG.Grid _grid, Visualizer _visualizer)
        {
            grid = _grid;
            visualizer = _visualizer;
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    Node currentNode = grid.nodesGrid[i, j];
                    NeighboursData data = GetNumNeighbours(i, j);
                    List<Direction> neighbours = data.neighbours;
                    if (currentNode.occupied && currentNode.usage != Usage.decoration)
                    {
                        Quaternion rotation = Quaternion.identity;
                        //SpawnSphere(currentNode.worldPosition);
                        switch (data.neighbours.Count)
                        {
                            case 1:
                                roadDictionary[new Vector2Int(i, j)] = Instantiate(roadEnd, currentNode.worldPosition, Quaternion.identity, transform);
                                SpawnSphere(currentNode.worldPosition, Color.cyan);
                                ConnectToOtherRoad(i, j, data);
                                break;
                            case 2:

                                if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                                {
                                    if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                                    {
                                        rotation = Quaternion.Euler(0, 90, 0);
                                    }
                                    roadDictionary[new Vector2Int(i, j)] = Instantiate(roadStraight, currentNode.worldPosition, rotation, transform);
                                }
                                else
                                {
                                    if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.back))
                                    {
                                        rotation = Quaternion.Euler(0, 180, 0);
                                    }
                                    else if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.forward))
                                    {
                                        rotation = Quaternion.Euler(0, -90, 0);
                                    }
                                    else if (neighbours.Contains(Direction.back) && neighbours.Contains(Direction.right))
                                    {
                                        rotation = Quaternion.Euler(0, 90, 0);
                                    }
                                    roadDictionary[new Vector2Int(i, j)] = Instantiate(roadCorner, currentNode.worldPosition, rotation, transform);
                                }
                                break;
                            case 3:
                                if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
                                {
                                    rotation = Quaternion.Euler(0, -90, 0);
                                }
                                else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.back) && neighbours.Contains(Direction.left))
                                {
                                    rotation = Quaternion.Euler(0, 180, 0);
                                }
                                else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
                                {
                                    rotation = Quaternion.Euler(0, 90, 0);
                                }
                                roadDictionary[new Vector2Int(i, j)] = Instantiate(road3way, currentNode.worldPosition, rotation, transform);
                                break;
                            case 4:
                                roadDictionary[new Vector2Int(i, j)] = Instantiate(road4way, currentNode.worldPosition, Quaternion.identity, transform);
                                break;
                            default:
                                break;
                        }
                    }

                }
            }

            // Delete outdated prefabs and spawn the correct ones
            foreach (Node node in updatedNodes)
            {
                Quaternion rotation = Quaternion.identity;
                int gridX = node.gridX;
                int gridY = node.gridY;
                NeighboursData data = GetNumNeighbours(node.gridX, node.gridY);
                List<Direction> neighbours = data.neighbours;
                Vector2Int key = new Vector2Int(gridX, gridY);
                if(roadDictionary.ContainsKey(key))
                    Destroy(roadDictionary[key]);
                switch (data.neighbours.Count)
                {
                    case 1:
                        Instantiate(roadEnd, node.worldPosition, Quaternion.identity, transform);
                        SpawnSphere(node.worldPosition, Color.cyan);
                        break;
                    case 2:

                        if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                        {
                            if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            Instantiate(roadStraight, node.worldPosition, rotation, transform);
                        }
                        else
                        {
                            if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.forward))
                            {
                                rotation = Quaternion.Euler(0, -90, 0);
                            }
                            else if (neighbours.Contains(Direction.back) && neighbours.Contains(Direction.right))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            Instantiate(roadCorner, node.worldPosition, rotation, transform);
                        }
                        break;
                    case 3:
                        if (neighbours.Contains(Direction.left) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
                        {
                            rotation = Quaternion.Euler(0, -90, 0);
                        }
                        else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.back) && neighbours.Contains(Direction.left))
                        {
                            rotation = Quaternion.Euler(0, 180, 0);
                        }
                        else if (neighbours.Contains(Direction.right) && neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back))
                        {
                            rotation = Quaternion.Euler(0, 90, 0);
                        }
                        Instantiate(road3way, node.worldPosition, rotation, transform);
                        break;
                    case 4:
                        Instantiate(road4way, node.worldPosition, Quaternion.identity, transform);
                        break;
                    default:
                        break;
                }
            }
        }
        private NeighboursData GetNumNeighbours(int posX, int posY)
        {
            NeighboursData data = new NeighboursData();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (grid.nodesGrid[posX + 1, posY].occupied && grid.nodesGrid[posX + 1, posY].usage != Usage.decoration) // Right
                    data.neighbours.Add(Direction.right);
            }
            if (posX - 1 >= 0)
            {
                if (grid.nodesGrid[posX - 1, posY].occupied && grid.nodesGrid[posX - 1, posY].usage != Usage.decoration) // Left
                    data.neighbours.Add(Direction.left);
            }

            if (posY + 1 < limitY)
            {
                if (grid.nodesGrid[posX, posY + 1].occupied && grid.nodesGrid[posX, posY + 1].usage != Usage.decoration) // Up
                    data.neighbours.Add(Direction.forward);
            }

            if (posY - 1 >= 0)
            {
                if (grid.nodesGrid[posX, posY - 1].occupied && grid.nodesGrid[posX, posY - 1].usage != Usage.decoration) // Down
                    data.neighbours.Add(Direction.back);
            }
            return data;
        }
        private void ConnectToOtherRoad(int gridX, int gridY, NeighboursData data)
        {
            // Try going straight in the 3 directions possible
            Direction neighbourDirection = data.neighbours[0];
            List<Direction> allDirections = GetAllDirections();
            allDirections.Remove(neighbourDirection);
            int i = 0;
            CreateSpheresInFreeDirections(gridX, gridY, allDirections);
            List<Node> path;
            while (i < 3)
            {
                Direction currentDirection = allDirections[i];
                path = GoStraight(currentDirection, gridX, gridY);
                if (path != null)
                {
                    // Generate nodes path, not prefabs
                    foreach (Node node in path)
                    {
                        node.occupied = true;
                        if (node.usage != Usage.point)
                        {
                            node.usage = Usage.road;
                        }
                        else if (node.usage == Usage.point)
                        {
                            updatedNodes.Add(node);
                            //visualizer.AddToSavedPoints(node);
                        }
                    }
                    return;
                }
                i++;
            }
            // If going straight has not been successful, we must attempt a two direction movement, e.g: right and down, right and up, etc.
            // We must have a list of candidates positions to do the movement
            i = 0;
            while (i < visualizer.pointNodes.Count)
            {
                Node targetNode = visualizer.pointNodes[i];
                int targetX = targetNode.gridX;
                int targetY = targetNode.gridY;
                if (targetX == gridX || targetY == gridY)
                {
                    i++;
                    continue;
                }

                // Decompose the movement
                List<Direction> movementDirections = new List<Direction>();
                if (targetX > gridX)
                {
                    movementDirections.Add(Direction.right);
                }
                else
                {
                    movementDirections.Add(Direction.left);
                }

                if (targetY > gridY)
                {
                    movementDirections.Add(Direction.forward);
                }
                else
                {
                    movementDirections.Add(Direction.back);
                }

                if (movementDirections[0] == neighbourDirection)
                {
                    movementDirections.Reverse();
                }

                int tempStartX = gridX;
                int tempStartY = gridY;
                path = new List<Node>();
                path.Add(grid.nodesGrid[gridX, gridY]);
                int subTargetX = tempStartX;
                int subTargetY = tempStartY;
                foreach (Direction direction in movementDirections)
                {
                    Vector3 dir = DirectionToVector(direction);
                    int dirX = Mathf.RoundToInt(dir.x); int dirY = Mathf.RoundToInt(dir.z);

                    if (direction == Direction.forward || direction == Direction.back)
                    {
                        subTargetY = targetY;
                    }
                    else
                    {
                        subTargetX = targetX;
                    }

                    List<Node> subPath = GoInDirection(dirX, dirY, tempStartX, tempStartY, subTargetX, subTargetY);
                    if (subPath == null)
                    {
                        path = null;
                        break;
                    }

                    tempStartX = subTargetX;
                    tempStartY = subTargetY;
                    subPath[subPath.Count - 1].usage = Usage.point;
                    path.AddRange(subPath);

                }
                // Path found, mark all those nodes as road.
                if (path != null)
                {
                    foreach (Node node in path)
                    {
                        updatedNodes.Add(node);
                        node.occupied = true;
                        if (node.usage != Usage.point)
                            node.usage = Usage.road;
                    }
                    return;
                }
                i++;
            }

        }
        private List<Node> GoInDirection(int dirX, int dirY, int startX, int startY, int targetX, int targetY)
        {
            List<Node> path = new List<Node>();
            int movementLength;
            if (dirX == 0) // Forward or back movement, calculate length based on startY - targetY
            {
                if (targetY > startY)
                {
                    movementLength = targetY - startY; // Forward movement
                }
                else
                {
                    movementLength = startY - targetY; // Back movement
                }
            }
            else // Right or back left, calculate length based on startX - targetX
            {
                if (targetX > startX)
                {
                    movementLength = targetX - startX; // Right movement
                }
                else
                {
                    movementLength = startX - targetX; // Left movement
                }
            }
            int i = 1;
            while (i <= movementLength)
            {
                int newX = startX + dirX * i;
                int newY = startY + dirY * i;
                if (OutOfGrid(newX, newY))
                    return null;

                path.Add(grid.nodesGrid[newX, newY]);
                i++;
            }

            return path;
        }
        private void CreateSpheresInFreeDirections(int x, int y, List<Direction> freeDirections)
        {
            foreach (Direction direction in freeDirections)
            {
                Vector3 dir = DirectionToVector(direction);
                int dirX = Mathf.RoundToInt(dir.x); int dirZ = Mathf.RoundToInt(dir.z);
                int newX = x + dirX;
                int newY = y + dirZ;
                if (!OutOfGrid(newX, newY))
                {
                    Vector3 pos = grid.nodesGrid[newX, newY].worldPosition;
                    SpawnSphere(pos, Color.yellow);
                }

            }
        }
        private List<Node> GoStraight(Direction direction, int startX, int startY)
        {
            int i = 1;
            List<Node> path = new List<Node>();
            path.Add(grid.nodesGrid[startX, startY]);
            Vector3 dir = DirectionToVector(direction);
            int dirX = Mathf.RoundToInt(dir.x); int dirZ = Mathf.RoundToInt(dir.z);
            while (true)
            {
                int currentPosX = startX + dirX * i;
                int currentPosY = startY + dirZ * i;

                if (OutOfGrid(currentPosX, currentPosY))
                    return null;

                Node currentNode = grid.nodesGrid[currentPosX, currentPosY];
                path.Add(currentNode);
                if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                {
                    return path;
                }
                i++;
            }
        }
        private void SpawnSphere(Vector3 pos, Color color)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.localScale = Vector3.one * 3f;
            startSphere.transform.position = pos + Vector3.up * 3f;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        private Vector3 DirectionToVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.left:
                    return Vector3.left;
                case Direction.right:
                    return Vector3.right;
                case Direction.forward:
                    return Vector3.forward;
                case Direction.back:
                    return Vector3.back;
            }
            return Vector3.zero;
        }
        private Vector3 GetOppositeVectorToDir(Direction direction)
        {
            switch (direction)
            {
                case Direction.left:
                    return Vector3.right;
                case Direction.right:
                    return Vector3.left;
                case Direction.forward:
                    return Vector3.back;
                case Direction.back:
                    return Vector3.forward;
            }
            return Vector3.zero;
        }
        private bool OutOfGrid(int posX, int posY)
        {
            return grid.OutOfGrid(posX, posY);
        }
        private List<Direction> GetAllDirections()
        {
            return new List<Direction> { Direction.left, Direction.right, Direction.forward, Direction.back };
        }
    }

    public class NeighboursData
    {
        public List<Direction> neighbours = new List<Direction>();
    }
    public enum Direction
    {
        left,
        right,
        forward,
        back
    }
}
