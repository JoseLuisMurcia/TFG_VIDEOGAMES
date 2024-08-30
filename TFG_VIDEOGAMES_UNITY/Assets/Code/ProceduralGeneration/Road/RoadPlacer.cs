using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;


namespace PG
{
    public class RoadPlacer : MonoBehaviour
    {
        #region prefabs
        [SerializeField]
        private GameObject straight, corner, roundabout, bridge, slant, intersection3way, intersection4way, pedestrianCrossing, pedestrianCrossingTL;
        [SerializeField]
        private GameObject trafficLights;
        [SerializeField]
        private GameObject stopSignal, yieldSignal, sidewalk;
        #endregion

        private Grid grid;
        private Visualizer visualizer;
        private List<GridNode> updatedNodes = new List<GridNode>();
        private Dictionary<Vector2Int, GameObject> roadDictionary = new Dictionary<Vector2Int, GameObject>();
        private List<Vector2Int> gameObjectsToRemove = new List<Vector2Int>();
        private List<GameObject> trafficSignsAndLights = new List<GameObject>();
        [SerializeField] bool visualDebug;
        public static RoadPlacer Instance;
        private StraightSplitter straightSplitter;
        private void Awake()
        {
            Instance = this;
            straightSplitter = GetComponent<StraightSplitter>();
            if (straightSplitter)
            {
                straightSplitter.SetCrossingPrefabs(pedestrianCrossing, pedestrianCrossingTL);
            }
        }

        public List<GameObject> PlaceRoadAssets(Grid _grid, Visualizer _visualizer)
        {
            grid = _grid;
            visualizer = _visualizer;

            // Clear useless pointNodes, from now on only the points who can go forward in a direction until they meet the end of the world will remain the pointNodesList
            List<GridNode> _pointNodes = new List<GridNode>();
            foreach (GridNode node in visualizer.pointNodes)
            {
                // FreeNeighbours is to check if it has any free neighbour
                // Should be eliminated checks the case where you have 1 neighbour and it is an intersection, wrong generation basically
                List<GridNode> freeNeighbours = GetFreeNeighbours(node);
                if (freeNeighbours.Count == 0)
                {
                    node.usage = Usage.road;
                    continue;
                }

                if (ShouldEliminateRedPoint(node))
                {
                    node.occupied = false;
                    node.usage = Usage.decoration;
                    if (visualDebug) SpawnSphere(node.worldPosition, Color.black, 3f, 2f);
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
                    if(visualDebug) SpawnSphere(node.worldPosition, Color.black, 1f, 2f);

                }
            }
            visualizer.pointNodes = _pointNodes;

            // Spawn the road prefabs
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    GridNode currentNode = grid.nodesGrid[i, j];
                    NeighboursData data = GetNeighboursData(i, j);
                    List<Direction> neighbours = data.neighbours;
                    if (currentNode.occupied)
                    {
                        Quaternion rotation = Quaternion.identity;
                        switch (data.neighbours.Count)
                        {
                            case 1:
                                if (!ShouldBeEliminated(currentNode, 2))
                                {
                                    ConnectToOtherRoad(i, j, data);
                                }
                                break;
                            case 2:
                                if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                                {
                                    if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                                    {
                                        rotation = Quaternion.Euler(0, 90, 0);
                                    }
                                    roadDictionary[new Vector2Int(i, j)] = Instantiate(straight, currentNode.worldPosition, rotation, transform);
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
                                    roadDictionary[new Vector2Int(i, j)] = Instantiate(corner, currentNode.worldPosition, rotation, transform);
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
                                roadDictionary[new Vector2Int(i, j)] = Instantiate(intersection3way, currentNode.worldPosition, rotation, transform);
                                break;
                            case 4:
                                roadDictionary[new Vector2Int(i, j)] = Instantiate(intersection4way, currentNode.worldPosition, Quaternion.identity, transform);
                                break;
                            default:
                                break;
                        }
                    }

                }
            }

            // Delete outdated prefabs and spawn the correct ones
            foreach (GridNode node in updatedNodes)
            {
                Quaternion rotation = Quaternion.identity;
                int gridX = node.gridX;
                int gridY = node.gridY;
                NeighboursData data = GetNeighboursData(node.gridX, node.gridY);
                List<Direction> neighbours = data.neighbours;
                Vector2Int key = new Vector2Int(gridX, gridY);
                if (roadDictionary.ContainsKey(key))
                {
                    Destroy(roadDictionary[key]);
                    roadDictionary.Remove(key);
                }

                if (!node.occupied)
                    continue;

                switch (data.neighbours.Count)
                {
                    case 1:
                        Debug.LogWarning("WTF BRO THERE IS STILL A ROAD WITH ONLY ONE NEIGHBOUR");
                        //roadDictionary[key] = Instantiate(roadEnd, node.worldPosition, Quaternion.identity, transform);
                        break;
                    case 2:

                        if ((neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) || (neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                        {
                            if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                            }
                            roadDictionary[key] = Instantiate(straight, node.worldPosition, rotation, transform);
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
                            roadDictionary[key] = Instantiate(corner, node.worldPosition, rotation, transform);
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
                        roadDictionary[key] = Instantiate(intersection3way, node.worldPosition, rotation, transform);
                        break;
                    case 4:
                        roadDictionary[key] = Instantiate(intersection4way, node.worldPosition, Quaternion.identity, transform);
                        break;
                    default:
                        break;
                }
            }

            // Straights recreation
            Dictionary<Vector2Int, GameObject> addedStraights = new Dictionary<Vector2Int, GameObject>();
            Dictionary<Vector2Int, GameObject> addedCrossings = new Dictionary<Vector2Int, GameObject>();
            foreach (Vector2Int position in roadDictionary.Keys)
            {
                GridNode currentNode = grid.nodesGrid[position.x, position.y];
                NeighboursData data = GetNeighboursData(position.x, position.y);
                List<Direction> neighbours = data.neighbours;
                if (neighbours.Count != 2)
                    continue;

                // It should be a straight road, not a corner/bend
                if (!(neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) && !(neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                    continue;

                // If it is already marked, ignore it
                if (currentNode.isProcessedStraight)
                    continue;

                StraightSplit split;
                if (neighbours.Contains(Direction.forward) || neighbours.Contains(Direction.back))
                {
                    // Vertical 
                    // Advance vertically until finding the extremes, add all those positions to a straight and mark those nodes with the belonging straight
                    split = CreateStraight(position.x, position.y, new List<Direction> { Direction.forward, Direction.back });
                }
                else
                {
                    // Horizontal
                    // Advance horizontally until finding the extremes, add all those positions to a straight and mark those nodes with the belonging straight
                    split = CreateStraight(position.x, position.y, new List<Direction> { Direction.left, Direction.right });
                }
                
                foreach(Straight straight in split.dividedStraights)
                {
                    addedStraights[straight.position] = straight.gameObject;
                }
                foreach (Vector2Int key in split.crossingDictionary.Keys)
                {
                    addedCrossings[key] = split.crossingDictionary[key];
                }
            }
            RemoveRedundantRoads();
            foreach(Vector2Int position in addedStraights.Keys)
            {
                roadDictionary[position] = addedStraights[position];
            }
            foreach (Vector2Int position in addedCrossings.Keys)
            {
                roadDictionary[position] = addedCrossings[position];
            }

            // Intersections creation
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    GridNode currentNode = grid.nodesGrid[i, j];
                    NeighboursData data = GetNeighboursData(i, j);
                    List<Direction> neighbours = data.neighbours;
                    if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                    {
                        switch (data.neighbours.Count)
                        {
                            case 3:
                                float random = Random.Range(0f, 1f);
                                if (random < 0.5f)
                                {
                                    // Instantiate 2 signals
                                    Vector2Int key = new Vector2Int(currentNode.gridX, currentNode.gridY);
                                    GameObject intersection = roadDictionary[key];
                                    Direction d1 = neighbours[Random.Range(0, neighbours.Count)];
                                    neighbours.Remove(d1);
                                    Direction d2 = neighbours[Random.Range(0, neighbours.Count)];
                                    neighbours.Remove(d2);

                                    GameObject stop = Instantiate(stopSignal, currentNode.worldPosition + GetOffsetForSignal(d1), GetRotationForSignal(d1), transform);
                                    GameObject yield = Instantiate(yieldSignal, currentNode.worldPosition + GetOffsetForSignal(d2), GetRotationForSignal(d2), transform);
                                    trafficSignsAndLights.Add(stop);
                                    trafficSignsAndLights.Add(yield);
                                }
                                else
                                {
                                    trafficSignsAndLights.Add(Instantiate(trafficLights, currentNode.worldPosition, Quaternion.identity, transform));
                                }
                                break;

                            case 4:
                                trafficSignsAndLights.Add(Instantiate(trafficLights, currentNode.worldPosition, Quaternion.identity, transform));
                                break;
                        }
                    }
                }
            }

            // Instantiate sidewalk
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    GridNode node = grid.nodesGrid[i, j];
                    if (node.usage == Usage.decoration) Instantiate(sidewalk, node.worldPosition, Quaternion.identity, transform);
                }
            }

            return roadDictionary.Values.ToList();
        }
        private StraightSplit CreateStraight(int x, int y, List<Direction> directions)
        {
            Straight unifiedStraight = new Straight();
            GridNode currentNode = grid.nodesGrid[x, y];
            currentNode.isProcessedStraight = true;
            unifiedStraight.gridNodes.Add(currentNode);
            Road initRoad = GetRoadFromPosition(x,y);
            Road entryRoad = null;
            Road exitRoad = null;
            DestroyRoadOnPosition(x, y);

            foreach (Direction direction in directions)
            {
                int i = 1;
                int[] offset = DirectionToInt(direction);
                // Foreach direction
                // While keeps finding valid straight nodes
                while (true)
                {
                    // Advance in that direction
                    GridNode advancedNode = AdvanceInDirection(x, y, offset, i);
                    if (advancedNode == null)
                    {
                        // Enter here on last iteration for each direction so that we can store the last road and get the reference points
                        if (i != 1) {
                            GridNode lastNodeInDirection = AdvanceInDirection(x, y, offset, i-1);
                            if (directions[0] != direction)
                            {
                                exitRoad = GetRoadFromPosition(lastNodeInDirection.gridX, lastNodeInDirection.gridY);
                                //entryRoad = GetRoadFromPosition(lastNodeInDirection.gridX, lastNodeInDirection.gridY);
                            }
                            else
                            {
                                entryRoad = GetRoadFromPosition(lastNodeInDirection.gridX, lastNodeInDirection.gridY);
                                //exitRoad = GetRoadFromPosition(lastNodeInDirection.gridX, lastNodeInDirection.gridY);
                            }
                        }
                        break;
                    }
                    else
                    {
                        // Add to straight and destroy the prefab in position
                        advancedNode.isProcessedStraight = true;
                        unifiedStraight.gridNodes.Add(advancedNode);
                        DestroyRoadOnPosition(advancedNode.gridX, advancedNode.gridY);
                    }
                    i++;
                }
            }
            if (entryRoad == null)
            {
                entryRoad = initRoad;
            }
            if (exitRoad == null)
            {
                exitRoad = initRoad;
            }

            // Create a pedestrian crossing by splitting the straight road in half
            // But first, let's order the nodes :)
            List<GridNode> sortedNodes;
            if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
            {
                sortedNodes = unifiedStraight.gridNodes.OrderByDescending(x => x.gridY).ToList();
            }
            else
            {
                sortedNodes = unifiedStraight.gridNodes.OrderBy(x => x.gridX).ToList();
            }
            unifiedStraight.gridNodes = sortedNodes;
            StraightSplit split = straightSplitter.HandleUnifiedStraight(unifiedStraight, directions);
            // Create a straight perfectly scaled and centered

            if (split.dividedStraights.Count <= 1)
            {
                unifiedStraight.SetCenterPosition();
                Quaternion rotation = Quaternion.identity;
                if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
                {
                    rotation = Quaternion.Euler(0, 90, 0);
                }
                GameObject straightGO = Instantiate(straight, unifiedStraight.center, rotation, transform);
                Vector3 newScale = new Vector3(4f * unifiedStraight.gridNodes.Count, 4f, 4f);
                straightGO.transform.localScale = newScale;
                unifiedStraight.gameObject = straightGO;
                unifiedStraight.position = new Vector2Int(x, y);
                // Adjust points for car navigation
                Road road = straightGO.GetComponent<Road>();
                if (road)
                {
                    road.numDirection = NumDirection.TwoDirectional;
                    road.laneReferencePoints[road.laneReferencePoints.Count - 1] = entryRoad.laneReferencePoints[entryRoad.laneReferencePoints.Count - 1];
                    road.laneReferencePoints[0] = exitRoad.laneReferencePoints[0];
                }
                straightGO.SetActive(false);
                split.dividedStraights = new List<Straight> { unifiedStraight };
            }
            else
            {
                foreach (Straight dividedStraight in split.dividedStraights)
                {
                    // We need to get all the roads from the divided straight so that we can look at the laneReferencePoints.
                    GridNode entryGridNode = dividedStraight.gridNodes[0];
                    GridNode exitGridNode = dividedStraight.gridNodes[dividedStraight.gridNodes.Count - 1];
                    entryRoad = GetRoadFromPosition(entryGridNode.gridX, entryGridNode.gridY);
                    exitRoad = GetRoadFromPosition(exitGridNode.gridX, exitGridNode.gridY);

                    if (entryRoad == null || exitRoad == null)
                    {
                        Debug.LogWarning("SE PUDRIO");
                    }
                    // Spawn gameobject
                    dividedStraight.SetCenterPosition();
                    Quaternion rotation = Quaternion.identity;
                    if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }
                    GameObject straightGO = Instantiate(straight, dividedStraight.center, rotation, transform);
                    Vector3 newScale = new Vector3(4f * dividedStraight.gridNodes.Count, 4f, 4f);
                    straightGO.transform.localScale = newScale;
                    dividedStraight.gameObject = straightGO;
                    // Adjust points for car navigation
                    Road road = straightGO.GetComponent<Road>();
                    if (road)
                    {
                        road.numDirection = NumDirection.TwoDirectional;
                        road.numberOfLanes = 2;

                        if (Vector3.Distance(entryRoad.laneReferencePoints[1], exitGridNode.worldPosition) > Vector3.Distance(entryRoad.laneReferencePoints[0], exitGridNode.worldPosition))
                        {
                            road.laneReferencePoints[1] = entryRoad.laneReferencePoints[1]; // entry
                            road.laneReferencePoints[0] = exitRoad.laneReferencePoints[0]; // exit
                        }
                        else
                        {
                            road.laneReferencePoints[0] = entryRoad.laneReferencePoints[0]; // entry
                            road.laneReferencePoints[1] = exitRoad.laneReferencePoints[1]; // exit
                        }
                        //SpawnSphere(road.laneReferencePoints[1], Color.yellow, 3f, 3f); // entry
                        //SpawnSphere(road.laneReferencePoints[0], Color.green, 3f, 3f); // exit
                        Vector3 originPos = road.laneReferencePoints[1] + Vector3.up * 9f;
                        Vector3 destPos = road.laneReferencePoints[0] + Vector3.up * 9f;
                        Vector3 dir = destPos - originPos;
                        Debug.DrawRay(originPos, dir, Color.red, 500f);
                    }
                    straightGO.SetActive(false);
                }
            }

            return split;
        }
        private Road GetRoadFromPosition(int x, int y)
        {
            Vector2Int key = new Vector2Int(x, y);
            if (roadDictionary.ContainsKey(key))
            {
                Road road = roadDictionary[key].GetComponent<Road>();
                if (road) { return road; }
            }
            return null;
        }
        private void DestroyRoadOnPosition(int x, int y)
        {
            Destroy(roadDictionary[new Vector2Int(x, y)]);
            gameObjectsToRemove.Add(new Vector2Int(x, y));
        }
        private void RemoveRedundantRoads()
        {
            foreach(Vector2Int key in gameObjectsToRemove)
            {
                roadDictionary.Remove(key);
            }
        }
        // This method, should return the node if it is not out of bounds and it is a straight, not an intersection nor bend.
        private GridNode AdvanceInDirection(int x, int y, int[] offset, int iterationIncrement)
        {
            int newX = x + offset[0] * iterationIncrement;
            int newY = y + offset[1] * iterationIncrement;

            // Check if it is out of bounds. If it is out of bounds, we have already reached the limit, continue with next direction.
            if (OutOfGrid(newX, newY))
                return null;

            // If exists, check the node and neighbours, is it a straight too?
            NeighboursData data = GetNeighboursData(newX, newY);
            List<Direction> neighbours = data.neighbours;
            if (neighbours.Count != 2)
                return null;

            // Is it a bend or a straight?
            if (!(neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right)) && !(neighbours.Contains(Direction.forward) && neighbours.Contains(Direction.back)))
                return null;

            // If it is a straight, add to straight nodes list. If it is not a straight, we have already reached the limit, continue with next direction.
            return grid.nodesGrid[newX, newY];
        }
        private Vector3 GetOffsetForSignal(Direction direction)
        {
            float multiplier = 2.5f;
            Vector3 offset = RoadPlacer.Instance.DirectionToVector(direction) * 3f;
            switch (direction)
            {
                case Direction.left:
                    return offset + Vector3.back * multiplier;
                case Direction.right:
                    return offset + Vector3.forward * multiplier;
                case Direction.forward:
                    return offset + Vector3.left * multiplier;
                case Direction.back:
                    return offset + Vector3.right * multiplier;
                default:
                    return Vector3.zero;
            }
        }
        private Quaternion GetRotationForSignal(Direction direction)
        {
            switch (direction)
            {
                case Direction.left:
                    return Quaternion.Euler(0f, -90f, 0f);
                case Direction.right:
                    return Quaternion.Euler(0f, 90f, 0f);
                case Direction.forward:
                    return Quaternion.Euler(0f, 0f, 0f);
                case Direction.back:
                    return Quaternion.Euler(0f, 180f, 0f);
                default:
                    return Quaternion.Euler(0f, 0f, 0f);
            }
        }
        private bool ShouldEliminateRedPoint(GridNode node)
        {
            List<Direction> neighbours = GetNeighboursData(node.gridX, node.gridY).neighbours;
            if (neighbours.Count == 1)
            {
                return ShouldEliminateSingleNeighbourNode(node, neighbours[0]);
            }
            if (neighbours.Count == 2)
            {
                return ShouldEliminateStraightPathNode(neighbours);
            }
            return false;
        }
        private bool ShouldEliminateSingleNeighbourNode(GridNode node, Direction direction)
        {
            int[] neighbourOffset = DirectionToInt(direction);
            GridNode neighbour = grid.nodesGrid[node.gridX + neighbourOffset[0], node.gridY + neighbourOffset[1]];
            if (GetNeighboursData(neighbour.gridX, neighbour.gridY).neighbours.Count > 2)
            {
                visualizer.MarkCornerDecorationNodes(neighbour);
                return true;
            }
            return false;
        }
        private bool ShouldEliminateStraightPathNode(List<Direction> neighbours)
        {
            bool isHorizontalPath = neighbours.Contains(Direction.left) && neighbours.Contains(Direction.right);
            bool isVerticalPath = neighbours.Contains(Direction.back) && neighbours.Contains(Direction.forward);

            return isHorizontalPath || isVerticalPath;
        }
        // This method is called when you only have one road neighbour and you cant be merged with another road.
        private bool ShouldBeEliminated(GridNode startNode, int maxIterations)
        {
            GridNode currentNode = startNode;
            GridNode previousNode = startNode;
            List<GridNode> pathToEliminate = new List<GridNode>
            {
                currentNode
            };
            int i = 0;
            while (i < maxIterations)
            {
                List<GridNode> roadNeighbours = GetRoadNeighbours(currentNode);
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
                    pathToEliminate.Add(currentNode);

                    Direction direction = Direction.zero;
                    GridNode nextNode;
                    for (i = 0; i < pathToEliminate.Count; i++)
                    {
                        currentNode = pathToEliminate[i];
                        int x = currentNode.gridX; int y = currentNode.gridY;
                        int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(direction);
                        if (i + 1 < pathToEliminate.Count)
                        {
                            nextNode = pathToEliminate[i + 1];
                            Direction newDirection = GetDirectionBasedOnPos(currentNode, nextNode);
                            if (direction != newDirection)
                                visualizer.UnmarkCornerDecorationNodes(currentNode);
                            visualizer.UnmarkSurroundingNodes(x, y, neighbourIncrement[0], neighbourIncrement[1]);
                            currentNode.occupied = false;
                            currentNode.usage = Usage.empty;
                            if (visualDebug) SpawnSphere(currentNode.worldPosition, Color.black, 3f, 2f);
                            updatedNodes.Add(currentNode);
                        }
                        else
                        {
                            visualizer.MarkCornerDecorationNodes(currentNode);
                        }

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
        private bool ReachesEndOfTheGrid(GridNode node, List<GridNode> freeNeighbours)
        {
            foreach (GridNode neighbour in freeNeighbours)
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

                    GridNode currentNode = grid.nodesGrid[x, y];
                    if (currentNode.occupied)
                        break;
                    i++;
                }
            }
            return false;
        }

        public int[] GetIntDirectionToNode(GridNode actualNode, GridNode newNode)
        {
            Direction direction = GetDirectionBasedOnPos(actualNode, newNode);
            return DirectionToInt(direction);
        }
        // Return all the neighbour nodes that are not occupied
        private List<GridNode> GetFreeNeighbours(GridNode node)
        {
            List<GridNode> neighbours = grid.GetNeighboursInLine(node);
            List<GridNode> freeNeighbours = new List<GridNode>();
            foreach (GridNode neighbour in neighbours)
            {
                if (!neighbour.occupied)
                    freeNeighbours.Add(neighbour);
            }
            return freeNeighbours;
        }
        // Return all the neighbour nodes that are roads
        private List<GridNode> GetRoadNeighbours(GridNode node)
        {
            List<GridNode> neighbours = grid.GetNeighboursInLine(node);
            List<GridNode> roadNeighbours = new List<GridNode>();
            foreach (GridNode neighbour in neighbours)
            {
                if (neighbour.occupied)
                    roadNeighbours.Add(neighbour);
            }
            return roadNeighbours;
        }

        private NeighboursData GetNeighboursData(int posX, int posY)
        {
            NeighboursData data = new NeighboursData();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (grid.nodesGrid[posX + 1, posY].occupied) // Right
                    data.neighbours.Add(Direction.right);
            }
            if (posX - 1 >= 0)
            {
                if (grid.nodesGrid[posX - 1, posY].occupied) // Left
                    data.neighbours.Add(Direction.left);
            }

            if (posY + 1 < limitY)
            {
                if (grid.nodesGrid[posX, posY + 1].occupied) // Up
                    data.neighbours.Add(Direction.forward);
            }

            if (posY - 1 >= 0)
            {
                if (grid.nodesGrid[posX, posY - 1].occupied) // Down
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
            List<GridNode> path;
            while (i < 3)
            {
                Direction currentDirection = allDirections[i];
                path = GoStraight(currentDirection, gridX, gridY);
                if (path != null)
                {
                    visualizer.MarkCornerDecorationNodes(path[0]);
                    foreach (GridNode node in path)
                    {
                        int x = node.gridX; int y = node.gridY;
                        int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(currentDirection);
                        visualizer.MarkSurroundingNodes(x, y, neighbourIncrement[0], neighbourIncrement[1]);

                        node.occupied = true;
                        updatedNodes.Add(node);
                        if (node.usage != Usage.point)
                            node.usage = Usage.road;

                    }
                    visualizer.MarkCornerDecorationNodes(path[path.Count - 1]);
                    if (visualDebug) SpawnSphere(grid.nodesGrid[gridX, gridY].worldPosition, Color.cyan, 2.5f, 2f);
                    return;
                }
                i++;
            }


            // If going straight has not been successful, we must call the pathfinder to find a path for us
            // We must have a list of candidates positions to do the movement
            GridNode currentNode = grid.nodesGrid[gridX, gridY];
            if (visualDebug) SpawnSphere(currentNode.worldPosition, Color.cyan, 2.5f, 2f);
            i = 0;
            while (i < visualizer.pointNodes.Count)
            {
                GridNode targetNode = visualizer.pointNodes[i];
                if (!CheckMergingNodeTerms(targetNode))
                {
                    i++; continue;
                }
                int targetX = targetNode.gridX;
                int targetY = targetNode.gridY;
                if (targetX == gridX || targetY == gridY)
                {
                    i++; continue;
                }
                path = GridPathfinder.instance.FindPath(currentNode, targetNode);
                if (path != null)
                {
                    Direction direction = Direction.zero;
                    GridNode nextNode;
                    for (i = 0; i < path.Count; i++)
                    {
                        GridNode node = path[i];
                        if (i + 1 < path.Count)
                        {
                            nextNode = path[i + 1];
                            Direction newDirection = GetDirectionBasedOnPos(node, nextNode);
                            if (direction != newDirection)
                                visualizer.MarkCornerDecorationNodes(node);
                        }
                        node.occupied = true;
                        updatedNodes.Add(node);
                        int x = node.gridX; int y = node.gridY;
                        int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(direction);
                        visualizer.MarkSurroundingNodes(x, y, neighbourIncrement[0], neighbourIncrement[1]);
                        if (node.usage != Usage.point)
                            node.usage = Usage.road;
                    }
                    visualizer.MarkCornerDecorationNodes(path[path.Count - 1]);
                    //visualizer.MarkCornerDecorationNodes(path[0]);
                    if (visualDebug) CreateSpheresInPath(path);
                    if (visualDebug) SpawnSphere(path[path.Count - 1].worldPosition, Color.magenta, 3f, 2f);
                    //Debug.Log("PATH CREATED WITH PATHFINDING");
                    return;
                }
                i++;
            }

            // If path has not been found
            // REMEMBER TO UNMARK THOSE!!
            // AND THEIR NEIGHBOURS
            ShouldBeEliminated(currentNode, 30);

            //Debug.LogWarning("UNA CARRETERA HA SIDO BORRADA");
        }
        private void CreateSpheresInPath(List<GridNode> path)
        {
            foreach (GridNode node in path)
            {
                if (node.usage != Usage.point)
                    if (visualDebug) SpawnSphere(node.worldPosition, Color.green, 3f, 2f);

            }

        }
        private List<GridNode> GoStraight(Direction direction, int startX, int startY)
        {
            List<GridNode> path = new List<GridNode>{ grid.nodesGrid[startX, startY] };
            int[] dir = DirectionToInt(direction);
            int[] neighbourIncrement = visualizer.GetLateralIncrementOnDirection(direction);

            int i = 1;
            while (true)
            {
                int currentPosX = startX + dir[0] * i;
                int currentPosY = startY + dir[1] * i;

                if (OutOfGrid(currentPosX, currentPosY))
                    return null;

                GridNode currentNode = grid.nodesGrid[currentPosX, currentPosY];
                path.Add(currentNode);
                if (currentNode.usage == Usage.road || currentNode.usage == Usage.point)
                {
                    // Here check the last node, because there are some things to be respected before merging.
                    // 1) The last node should be at least 2 nodes away from an intersection. Otherwise intersections are going to be created stupidly close to each other.
                    // 2) If the last node is going to create an intersection, such intersection should not have a bending neighbour, otherwise that's going to be problematic for the triggers.
                    if (CheckMergingNodeTerms(currentNode))
                        return path;
                    return null;
                }

                if (!visualizer.EnoughSpace(currentPosX, currentPosY, neighbourIncrement[0], neighbourIncrement[1]))
                    return null;


                i++;
            }
        }
        private bool CheckMergingNodeTerms(GridNode mergingNode)
        {
            // Here check the last node, because there are some things to be respected before merging.
            // 1) The last node should be at least 2 nodes away from an intersection. Otherwise intersections are going to be created stupidly close to each other.
            // 2) If the last node is going to create an intersection, such intersection should not have a bending neighbour, otherwise that's going to be problematic for the triggers.
            // Return false if it should not merge
            int mergeX = mergingNode.gridX;
            int mergeY = mergingNode.gridY;
            NeighboursData nb = GetNeighboursData(mergeX, mergeY);
            int mergingIntersectionDist = 2;
            foreach (Direction dir in nb.neighbours)
            {
                // From the mergingNode, explore neighbours to find intersections or corners
                int[] direction = DirectionToInt(dir);
                for (int i = 1; i <= mergingIntersectionDist; i++)
                {
                    int newX = mergeX + direction[0] * i;
                    int newY = mergeY + direction[1] * i;

                    if (!OutOfGrid(newX, newY))
                    {
                        List<Direction> neighboursDir = GetNeighboursData(newX, newY).neighbours;

                        // 1) Intersection found too close
                        if (neighboursDir.Count == 3)
                            return false;

                        // 2) Bending found
                        if (!(neighboursDir.Contains(Direction.left) && neighboursDir.Contains(Direction.right)) && !(neighboursDir.Contains(Direction.forward) && neighboursDir.Contains(Direction.back)))
                            return false;
                    }
                }
            }
            return true;
        }
        private int GetNumNeighbours(int posX, int posY)
        {
            int count = 0;
            if (grid.nodesGrid[posX + 1, posY].occupied) // Right
                count++;

            if (grid.nodesGrid[posX - 1, posY].occupied) // Left
                count++;

            if (grid.nodesGrid[posX, posY + 1].occupied) // Up
                count++;

            if (grid.nodesGrid[posX, posY - 1].occupied) // Down
                count++;
            return count;
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.localScale = Vector3.one * size;
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
            if (dirX < -0.5f)
                return Direction.left;
            if (dirY > 0.5f)
                return Direction.forward;
            if (dirY < -0.5f)
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
        public Vector3 GetOppositeVectorToDir(Direction direction)
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
        public List<Direction> GetAllDirections()
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
            foreach (var obj in trafficSignsAndLights)
            {
                Destroy(obj);
            }
            trafficSignsAndLights.Clear();
        }
        public void DestroyAssets()
        {
            updatedNodes.Clear();
            foreach (Vector2Int key in roadDictionary.Keys)
            {
                Destroy(roadDictionary[key]);
            }
            roadDictionary.Clear();
            foreach (var obj in trafficSignsAndLights)
            {
                Destroy(obj);
            }
            trafficSignsAndLights.Clear();
        }
        public Direction GetDirectionBasedOnPos(GridNode currentNode, GridNode newNode)
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
