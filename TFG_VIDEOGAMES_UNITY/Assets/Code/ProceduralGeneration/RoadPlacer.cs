using System;
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
        [SerializeField] bool visualDebug;
        public static RoadPlacer Instance;

        private void Awake()
        {
            Instance = this;
        }
        public void PlaceRoadAssets(PG.Grid _grid, Visualizer _visualizer)
        {

            grid = _grid;
            visualizer = _visualizer;
            // Clear this data structure, from now on only the points who can go forward in a direction until they meet the end of the world will remain the pointNodesList
            List<Node> _pointNodes = new List<Node>();
            foreach (Node node in visualizer.pointNodes)
            {
                // FreeNeighbours is to check if it has any free neighbour
                // Should be eliminated checks the case where you have 1 neighbour and it is an intersection, wrong generation basically
                List<Node> freeNeighbours = GetFreeNeighbours(node);
                if (freeNeighbours.Count == 0)
                {
                    node.usage = Usage.road;
                    continue;
                }

                if (ShouldEliminateRedPoint(node))
                {
                    node.occupied = false;
                    node.usage = Usage.empty;
                    if (visualDebug) SpawnSphere(node.worldPosition, Color.black, 3f);
                    continue;
                }

                // Advance with the right 
                if (ReachesEndOfTheGrid(node, freeNeighbours))
                {
                    _pointNodes.Add(node);
                }
                else
                {
                    node.usage = Usage.road;
                    // if(visualDebug) SpawnSphere(node.worldPosition, Color.black);

                }
            }
            visualizer.pointNodes = _pointNodes;

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
                        switch (data.neighbours.Count)
                        {
                            case 1:
                                if (!ShouldBeEliminated(currentNode, 2))
                                {
                                    roadDictionary[new Vector2Int(i, j)] = Instantiate(roadEnd, currentNode.worldPosition, Quaternion.identity, transform);
                                    ConnectToOtherRoad(i, j, data);
                                }
                                //if (visualDebug) SpawnSphere(currentNode.worldPosition, Color.cyan, 3f);
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
                if (roadDictionary.ContainsKey(key))
                    Destroy(roadDictionary[key]);

                if (!node.occupied)
                    continue;

                switch (data.neighbours.Count)
                {
                    case 1:
                        Debug.LogWarning("WTF BRO THERE IS STILL A ROAD WITH ONLY ONE NEIGHBOUR");
                        roadDictionary[key] = Instantiate(roadEnd, node.worldPosition, Quaternion.identity, transform);
                        break;
                    case 2:

                        if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                        {
                            if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            roadDictionary[key] = Instantiate(roadStraight, node.worldPosition, rotation, transform);
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
                            roadDictionary[key] = Instantiate(roadCorner, node.worldPosition, rotation, transform);
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
                        roadDictionary[key] = Instantiate(road3way, node.worldPosition, rotation, transform);
                        break;
                    case 4:
                        roadDictionary[key] = Instantiate(road4way, node.worldPosition, Quaternion.identity, transform);
                        break;
                    default:
                        break;
                }
            }
        }
        private bool ShouldEliminateRedPoint(Node node)
        {
            List<Direction> neighbours = GetNumNeighbours(node.gridX, node.gridY).neighbours;
            if (neighbours.Count == 1)
            {
                // If your neighbour is an intersection, delete yourself, thank you.
                Direction direction = neighbours[0];
                int[] neighbourOffset = DirectionToInt(direction);
                if (GetNumNeighbours(node.gridX + neighbourOffset[0], node.gridY + neighbourOffset[1]).neighbours.Count > 2)
                    return true;
            }
            return false;
        }
        // This method is called when you only have one road neighbour
        private bool ShouldBeEliminated(Node startNode, int maxIterations)
        {
            Node currentNode = startNode;
            Node previousNode = startNode;
            bool intersectionFound = false;
            List<Node> pathToEliminate = new List<Node>();
            pathToEliminate.Add(currentNode);
            int i = 0;
            while (!intersectionFound && i < maxIterations)
            {
                List<Node> roadNeighbours = GetRoadNeighbours(currentNode);
                int numRoadNeighbours = roadNeighbours.Count;
                if (numRoadNeighbours == 1)
                {
                    currentNode = roadNeighbours[0];
                }
                else if (numRoadNeighbours == 2)
                {
                    roadNeighbours.Remove(previousNode);
                    pathToEliminate.Add(currentNode);
                    previousNode = currentNode;
                    currentNode = roadNeighbours[0];
                }
                else if (numRoadNeighbours >= 3)
                {
                    // We have reached the end, delete everything xd                   
                    intersectionFound = true;
                    foreach (Node node in pathToEliminate)
                    {
                        node.occupied = false;
                        node.usage = Usage.empty;
                        if (visualDebug) SpawnSphere(node.worldPosition, Color.black, 3f);
                        updatedNodes.Add(node);
                    }
                    return true;
                }
                else
                {
                    Debug.LogWarning("SHOULD BE ELIMINATED BROKE");
                    return false;
                }

                if (pathToEliminate.Count > 25)
                {
                    Debug.LogWarning("SHOULD BE ELIMINATED BROKE BECAUSE OF COUNT");
                    return false;
                }
                i++;
            }
            return false;
        }
        private bool ReachesEndOfTheGrid(Node node, List<Node> freeNeighbours)
        {
            foreach (Node neighbour in freeNeighbours)
            {
                int[] direction = GetIntDirectionToNode(node, neighbour);
                int i = 1;
                int startX = neighbour.gridX;
                int startY = neighbour.gridY;
                while (true)
                {
                    int x = startX + direction[0] * i;
                    int y = startY + direction[1] * i;
                    if (OutOfGrid(x, y))
                        return true;

                    Node currentNode = grid.nodesGrid[x, y];
                    if (currentNode.occupied)
                        break;
                    i++;
                }
            }
            return false;
        }

        private int[] GetIntDirectionToNode(Node actualNode, Node newNode)
        {
            Direction direction = GetDirectionBasedOnPos(actualNode, newNode);
            return DirectionToInt(direction);
        }
        // Return all the neighbour nodes that are not occupied
        private List<Node> GetFreeNeighbours(Node node)
        {
            List<Node> neighbours = grid.GetNeighboursInLine(node);
            List<Node> freeNeighbours = new List<Node>();
            foreach (Node neighbour in neighbours)
            {
                if (!neighbour.occupied)
                    freeNeighbours.Add(neighbour);
            }
            return freeNeighbours;
        }
        // Return all the neighbour nodes that are roads
        private List<Node> GetRoadNeighbours(Node node)
        {
            List<Node> neighbours = grid.GetNeighboursInLine(node);
            List<Node> roadNeighbours = new List<Node>();
            foreach (Node neighbour in neighbours)
            {
                if (neighbour.occupied && (neighbour.usage == Usage.road || neighbour.usage == Usage.point))
                    roadNeighbours.Add(neighbour);
            }
            return roadNeighbours;
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
            List<Node> path;
            while (i < 3)
            {
                Direction currentDirection = allDirections[i];
                path = GoStraight(currentDirection, gridX, gridY);
                if (path != null)
                {
                    foreach (Node node in path)
                    {
                        int x = node.gridX; int y = node.gridY;
                        int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(currentDirection);
                        visualizer.MarkSurroundingNodes(x, y, neighbourIncrement[0], neighbourIncrement[1]);

                        node.occupied = true;
                        updatedNodes.Add(node);
                        if (node.usage != Usage.point)
                            node.usage = Usage.road;

                    }
                    visualizer.MarkCornerDecorationNodes(path[path.Count-1]);
                    if (visualDebug) SpawnSphere(grid.nodesGrid[gridX, gridY].worldPosition, Color.cyan, 2.5f);
                    return;
                }
                i++;
            }


            // If going straight has not been successful, we must call the pathfinder to find a path for us
            // We must have a list of candidates positions to do the movement
            Node currentNode = grid.nodesGrid[gridX, gridY];
            if (visualDebug) SpawnSphere(currentNode.worldPosition, Color.cyan, 2.5f);
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
                path = Pathfinder.instance.FindPath(currentNode, targetNode);
                if (path != null)
                {
                    Direction direction = Direction.zero;
                    Node nextNode;
                    for (i = 0; i < path.Count; i++)
                    {
                        Node node = path[i];
                        if (i + 1 < path.Count)
                        {
                            nextNode = path[i + 1];
                            direction = GetDirectionBasedOnPos(node, nextNode);
                        }
                        updatedNodes.Add(node);
                        node.occupied = true;
                        int x = node.gridX; int y = node.gridY;
                        int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(direction);
                        visualizer.MarkSurroundingNodes(x, y, neighbourIncrement[0], neighbourIncrement[1]);
                        if (node.usage != Usage.point)
                            node.usage = Usage.road;
                    }
                    visualizer.MarkCornerDecorationNodes(path[path.Count-1]);
                    //visualizer.MarkCornerDecorationNodes(path[0]);
                    if (visualDebug) CreateSpheresInPath(path);
                    if (visualDebug) SpawnSphere(path[path.Count - 1].worldPosition, Color.magenta, 3f);
                    Debug.Log("PATH CREATED WITH PATHFINDING");
                    return;
                }
                i++;
            }

            // If path has not been found
            // REMEMBER TO UNMARK THOSE!!
            ShouldBeEliminated(currentNode, 30);

            Debug.LogWarning("UNA CARRETERA HA SIDO BORRADA");
        }
        private void CreateSpheresInPath(List<Node> path)
        {
            foreach (Node node in path)
            {
                if (node.usage != Usage.point)
                    if (visualDebug) SpawnSphere(node.worldPosition, Color.green, 3f);

            }

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
                    if (visualDebug) SpawnSphere(pos, Color.yellow, 4f);
                }

            }
        }
        private List<Node> GoStraight(Direction direction, int startX, int startY)
        {
            List<Node> path = new List<Node>();
            path.Add(grid.nodesGrid[startX, startY]);
            int[] dir = DirectionToInt(direction);
            int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(direction);

            int i = 1;
            while (true)
            {
                int currentPosX = startX + dir[0] * i;
                int currentPosY = startY + dir[1] * i;

                if (OutOfGrid(currentPosX, currentPosY))
                    return null;

                Node currentNode = grid.nodesGrid[currentPosX, currentPosY];
                path.Add(currentNode);
                if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                {
                    return path;
                }

                if (!visualizer.EnoughSpace(currentPosX, currentPosY, neighbourIncrement[0], neighbourIncrement[1]))
                    return null;


                i++;
            }
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.localScale = Vector3.one * 3f;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        public Vector3 DirectionToVector(Direction direction)
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
        public Direction VectorToDirection(Vector3 direction)
        {
            float dirX = direction.x;
            float dirY = direction.z;
            if (dirX > 0.5f)
                return Direction.right;
            if (dirX < 0.5f)
                return Direction.left;
            if (dirY > 0.5f)
                return Direction.forward;
            if (dirY < 0.5f)
                return Direction.back;

            return Direction.zero;
        }
        public int[] DirectionToInt(Direction direction)
        {
            switch (direction)
            {
                case Direction.left:
                    return new int[] { -1, 0 };
                case Direction.right:
                    return new int[] { 1, 0 };
                case Direction.forward:
                    return new int[] { 0, 1 };
                case Direction.back:
                    return new int[] { 0, -1 };
                case Direction.zero:
                    return null;
                default:
                    return null;
            }
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
        public void Reset()
        {
            updatedNodes.Clear();
            foreach (Vector2Int key in roadDictionary.Keys)
            {
                Destroy(roadDictionary[key]);
            }
            roadDictionary.Clear();
        }

        public Direction GetDirectionBasedOnPos(Node currentNode, Node newNode)
        {
            int x = currentNode.gridX; int y = currentNode.gridY;
            int newX = newNode.gridX; int newY = newNode.gridY;

            if (newX > x)
                return Direction.right;

            if (newY > y)
                return Direction.forward;

            if (newX < x)
                return Direction.left;

            if (newY < y)
                return Direction.back;

            return Direction.zero;
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
        back,
        zero = -1
    }
}
