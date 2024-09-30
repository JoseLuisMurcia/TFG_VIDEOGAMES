using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace PG
{
    public class Visualizer : MonoBehaviour
    {
        [HideInInspector] public RoadPlacer roadPlacer;
        [HideInInspector] private RoadConnecter roadConnecter;
        [HideInInspector] private BuildingPlacer buildingPlacer;
        [HideInInspector] private PropPlacer propPlacer;
        private LSystemGenerator lsystem;
        private GenerationUI generationUI;
        [HideInInspector] public List<GridNode> pointNodes = new List<GridNode>();
        [HideInInspector] public List<GridNode> surroundingNodes = new List<GridNode>();
        [SerializeField] public Grid grid;
        //List<Vector3> positions = new List<Vector3>();
        private int length = 8;
        private float angle = 90f;
        [SerializeField] public int neighboursOffset;
        [SerializeField] public int decorationOffset;
        [SerializeField] private NavMeshGenerator navMeshGenerator;
        //private int[] lengthValues = { 6, 8, 10, 12 };
        private int[] lengthValues = { 8, 12 };
        public static Visualizer Instance;
        public int Length
        {
            get
            {
                if (length > 0)
                {
                    return length;
                }
                else
                {
                    return 1;
                }
            }
            set => length = value;
        }

        private void Awake()
        {
            Instance = this;
            lsystem = GetComponent<LSystemGenerator>();
            roadPlacer = GetComponent<RoadPlacer>();
            roadConnecter = GetComponent<RoadConnecter>();
            buildingPlacer = GetComponent<BuildingPlacer>();
            propPlacer = GetComponent<PropPlacer>();
            generationUI = GetComponent<GenerationUI>();
        }
        void Start()
        {
            StartGeneration();
            //Manipulacion();
        }
        //private void Manipulacion()
        //{
        //    // Aqui lo manipulo
        //    List<Vector2Int> squarePositions = new List<Vector2Int>()
        //    {new Vector2Int(0,0), new Vector2Int(1, 0), new Vector2Int(2, 0),  new Vector2Int(3,0),
        //        new Vector2Int(0, 1), new Vector2Int(3, 1),
        //        new Vector2Int(0, 2), new Vector2Int(3, 2),
        //    new Vector2Int(0,3),new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3)};

        //    //List<Vector2Int> otherPos = new List<Vector2Int>()
        //    //{new Vector2Int(7,4), new Vector2Int(7,3),new Vector2Int(7,2),new Vector2Int(7,1), new Vector2Int(7,0),
        //    //new Vector2Int(8,3), new Vector2Int(9,3), new Vector2Int(10,3), new Vector2Int(11,3),
        //    //new Vector2Int(11,2), new Vector2Int(11,1), new Vector2Int(11,0),
        //    //new Vector2Int(8,0),new Vector2Int(9,0), new Vector2Int(10,0)};

        //    List<Vector2Int> otherPos = new List<Vector2Int>()
        //    {new Vector2Int(7,2),new Vector2Int(7,1), new Vector2Int(7,0),
        //    new Vector2Int(8,0),new Vector2Int(9,0), new Vector2Int(10,0),
        //    new Vector2Int(11,0),new Vector2Int(12,0),
        //    new Vector2Int(12,1), new Vector2Int(12,2), new Vector2Int(11,2), new Vector2Int(10,2),
        //    new Vector2Int(10,1)};

        //    List<Vector2Int> redPositions = new List<Vector2Int>() { new Vector2Int(3, 3), new Vector2Int(7, 3) };

        //    grid.nodesGrid[3, 3].occupied = true;
        //    grid.nodesGrid[3, 3].usage = Usage.point;

        //    foreach (Vector2Int pos in squarePositions)
        //    {
        //        Node node = grid.nodesGrid[pos.x, pos.y];
        //        node.usage = Usage.road;
        //        node.occupied = true;
        //    }
        //    foreach (Vector2Int pos in otherPos)
        //    {
        //        Node node = grid.nodesGrid[pos.x, pos.y];
        //        node.usage = Usage.road;
        //        node.occupied = true;
        //    }

        //    foreach (Vector2Int pos in redPositions)
        //    {
        //        Node node = grid.nodesGrid[pos.x, pos.y];
        //        node.usage = Usage.point;
        //        node.occupied = true;
        //        pointNodes.Add(node);
        //    }

        //    //roadPlacer.PlaceRoadAssets(grid, this);
        //}
        public void StartGeneration()
        {
            surroundingNodes.Clear();
            pointNodes.Clear();
            string sequence = lsystem.GenerateSentence();
            VisualizeSequence(sequence);
        }
        // Hay que desplazarse en nodos, en cubitos del grid, luego ya mapeamos a carreteras, nada de posiciones, cubos
        private async void VisualizeSequence(string sequence)
        {
            Stack<AgentParameters> savePoints = new Stack<AgentParameters>();
            int gridTopX = grid.gridSizeX;
            int gridTopY = grid.gridSizeY;
            int currentPosX = gridTopX / 2;
            int currentPosY = gridTopY / 2;
            int tempPosX = currentPosX;
            int tempPosY = currentPosY;

            Vector3 direction = Vector3.forward;

            GridNode firstNode = grid.nodesGrid[currentPosX, currentPosY];
            AddToSavedPoints(firstNode);

            foreach (char letter in sequence)
            {
                EncodingLetters encoding = (EncodingLetters)letter;
                switch (encoding)
                {
                    case EncodingLetters.save:
                        savePoints.Push(new AgentParameters
                        {
                            posX = currentPosX,
                            posY = currentPosY,
                            direction = direction,
                            length = Length
                        });

                        break;
                    case EncodingLetters.load:
                        if (savePoints.Count > 0)
                        {
                            AgentParameters agentParameter = savePoints.Pop();
                            currentPosX = agentParameter.posX;
                            currentPosY = agentParameter.posY;
                            direction = agentParameter.direction;
                            Length = agentParameter.length;
                        }
                        else
                        {
                            throw new System.Exception("Dont have saved point in our stack");
                        }
                        break;
                    case EncodingLetters.draw:
                        tempPosX = currentPosX; tempPosY = currentPosY;
                        int dirX = Mathf.RoundToInt(direction.x); int dirZ = Mathf.RoundToInt(direction.z);
                        currentPosX += dirX * length;
                        currentPosY += dirZ * length;
                        if (currentPosX < 0 || currentPosX >= gridTopX)
                        {
                            if (currentPosX < 0)
                                currentPosX = 0;
                            else
                                currentPosX = gridTopX - 1;
                        }
                        if (currentPosY < 0 || currentPosY >= gridTopY)
                        {
                            if (currentPosY < 0)
                                currentPosY = 0;
                            else
                                currentPosY = gridTopY - 1;
                        }
                        DrawLine(tempPosX, tempPosY, currentPosX, currentPosY, dirX, dirZ);
                        int randomInt = Random.Range(0, lengthValues.Length);
                        Length = lengthValues[randomInt];
                        //Length -= 2;
                        break;
                    case EncodingLetters.turnRight:
                        direction = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                        break;
                    case EncodingLetters.turnLeft:
                        direction = Quaternion.AngleAxis(-angle, Vector3.up) * direction;
                        break;
                    default:
                        break;
                }
            }

            // Set regions
            RegionHelper regionHelper = new RegionHelper();
            regionHelper.SetRegions(grid.voronoiGenerator.GetVoronoiRegions());
            // Create road prefabs and perfect road network
            Dictionary<Vector2Int, GameObject> roadDictionary = roadPlacer.PlaceRoadAssets(grid, this);
            // Create road nodes used by cars and connect roads
            await roadConnecter.ConnectRoads(roadDictionary.Values.ToList());
            // Adjust the voronoi regions to the generated road network
            regionHelper.AdjustRegions();
            // Place sidewalks and buildings
            buildingPlacer.PlaceBuildings(grid, roadDictionary);
            // Place props
            //propPlacer.PlaceProps(buildingPlacer);
            generationUI.OnCityCreated();
            //navMeshGenerator.BakeNavMesh();
        }

        private void DrawLine(int startX, int startY, int endX, int endY, int dirX, int dirY)
        {
            GridNode startNode = grid.nodesGrid[startX, startY];
            if (startNode.usage != Usage.point)
                return;

            int length = GetMovementLength(startX, startY, endX, endY);
            int[] neighbourIncrement = GetLateralIncrementOnDirection(dirX, dirY);

            // Check if the grid is available for this advance
            for (int i = 1; i < length; i++)
            {
                int newX = startX + dirX * i;
                int newY = startY + dirY * i;

                if (!EnoughSpace(newX, newY, neighbourIncrement[0], neighbourIncrement[1]))
                    return;
            }

            MarkCornerDecorationNodes(startNode);
            // Mark the road
            for (int i = 0; i < length; i++)
            {
                int newX = startX + dirX * i;
                int newY = startY + dirY * i;
                int[] currentPos = { newX, newY };
                GridNode currentNode = grid.nodesGrid[currentPos[0], currentPos[1]];
                //regionHelper.SetBoundaries(currentNode);
                if (currentNode.usage != Usage.road && currentNode.usage != Usage.point)
                {
                    currentNode.occupied = true;
                    currentNode.usage = Usage.road;
                }
                MarkSurroundingNodes(newX, newY, neighbourIncrement[0], neighbourIncrement[1]);


            }
            GridNode endNode = grid.nodesGrid[endX, endY];
            MarkSurroundingNodes(endX, endY, neighbourIncrement[0], neighbourIncrement[1]);
            MarkCornerDecorationNodes(endNode);
            AddToSavedPoints(endNode);
        }
        // This method receives a startPosition and the increment with direction it has to perform to reach a target, it has to be called on a loop
        // The increment received is to advance laterally to the main road and check if there is a road.
        public bool EnoughSpace(int posX, int posY, int xIncrement, int yIncrement, GridNode targetNode)
        {
            int i = 1;
            while (i <= 2)
            {
                int incrementedXPos = posX + xIncrement * i;
                int incrementedYPos = posY + yIncrement * i;
                int decreasedXPos = posX - xIncrement * i;
                int decreasedYPos = posY - yIncrement * i;

                if (!OutOfGrid(incrementedXPos, incrementedYPos))
                {
                    if (NearbyRoad(incrementedXPos, incrementedYPos))
                        return false;

                }
                if (!OutOfGrid(decreasedXPos, decreasedYPos))
                {
                    if (NearbyRoad(decreasedXPos, decreasedYPos))
                        return false;
                }
                i++;
            }
            return true;
        }
        public bool EnoughSpace(int posX, int posY, int xIncrement, int yIncrement)
        {
            int i = 1;
            while (i <= neighboursOffset)
            {
                int incrementedXPos = posX + xIncrement * i;
                int decreasedXPos = posX - xIncrement * i;
                int incrementedYPos = posY + yIncrement * i;
                int decreasedYPos = posY - yIncrement * i;

                if (!OutOfGrid(incrementedXPos, incrementedYPos))
                {
                    // Hay que detectar el caso de que el nearbyRoad sea nuestro nodoTarget, en ese caso
                    // Lo que hay que hacer
                    if (NearbyRoad(incrementedXPos, incrementedYPos))
                        return false;
                }
                if (!OutOfGrid(decreasedXPos, decreasedYPos))
                {

                    if (NearbyRoad(decreasedXPos, decreasedYPos))
                        return false;
                }
                i++;
            }
            return true;
        }
        public void MarkSurroundingNodes(int posX, int posY, int xIncrement, int yIncrement)
        {
            for (int i = 1; i <= decorationOffset; i++)
            {
                if (!OutOfGrid(posX + xIncrement * i, posY + yIncrement * i))
                {
                    GridNode increasedNode = grid.nodesGrid[posX + xIncrement * i, posY + yIncrement * i];
                    if (!increasedNode.occupied)
                    {
                        MarkNodeAsDecoration(increasedNode);
                    }
                }

                if (!OutOfGrid(posX - xIncrement * i, posY - yIncrement * i))
                {
                    GridNode decreasedNode = grid.nodesGrid[posX - xIncrement * i, posY - yIncrement * i];
                    if (!decreasedNode.occupied)
                    {
                        MarkNodeAsDecoration(decreasedNode);
                    }
                }

            }
        }
        public void UnmarkSurroundingNodes(int posX, int posY, int xIncrement, int yIncrement)
        {
            for (int i = 1; i <= decorationOffset; i++)
            {
                int incrementedX = posX + xIncrement * i;
                int incrementedY = posY + yIncrement * i;
                if (!OutOfGrid(incrementedX, incrementedY) && !IsDecorationShared(posX, posY, xIncrement, yIncrement, i + 1))
                {
                    GridNode increasedNode = grid.nodesGrid[incrementedX, incrementedY];
                    RemoveNodeFromDecoration(increasedNode);
                }

                int decrementedX = posX - xIncrement * i;
                int decrementedY = posY - yIncrement * i;
                if (!OutOfGrid(decrementedX, decrementedY) && !IsDecorationShared(posX, posY, -xIncrement, -yIncrement, i + 1))
                {
                    GridNode decreasedNode = grid.nodesGrid[decrementedX, decrementedY];
                    RemoveNodeFromDecoration(decreasedNode);
                }

            }
        }
        // Método necesario para que no borren nodos qué deben estar marcados como decoration
        private bool IsDecorationShared(int posX, int posY, int xIncrement, int yIncrement, int index)
        {
            int newX = posX + (xIncrement * index);
            int newY = posY + (yIncrement * index);

            if (OutOfGrid(newX, newX)) 
                return false;

            GridNode newNode = grid.nodesGrid[newX, newY];
            if (newNode.occupied) 
                return true;

            return false;
        }
        public void MarkCornerDecorationNodes(GridNode node)
        {
            List<Vector2Int> positions = new List<Vector2Int>() { new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(1, 0)};
            int x = node.gridX;
            int y = node.gridY;
            foreach (Vector2Int position in positions)
            {
                if (OutOfGrid(x + position.x, y + position.y))
                    continue;

                GridNode neighbour = grid.nodesGrid[x + position.x, y + position.y];
                if (!neighbour.occupied)
                {
                    MarkNodeAsDecoration(neighbour);
                }
            }
        }
        public void UnmarkCornerDecorationNodes(GridNode node)
        {
            List<Vector2Int> positions = new List<Vector2Int>() { new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(1, 0)};
            int x = node.gridX;
            int y = node.gridY;
            foreach (Vector2Int position in positions)
            {
                if (OutOfGrid(x + position.x, y + position.y) || IsDecorationShared(x, y, position.x, position.y, 2))
                    continue;

                GridNode neighbour = grid.nodesGrid[x + position.x, y + position.y];
                RemoveNodeFromDecoration(neighbour);
            }
        }
        private void MarkNodeAsDecoration(GridNode node)
        {
            node.occupied = false;
            node.usage = Usage.decoration;
            surroundingNodes.Add(node);
        }
        private void RemoveNodeFromDecoration(GridNode node)
        {
            if (!node.occupied)
            {
                node.usage = Usage.empty;
                surroundingNodes.Remove(node);
            }         
        }
        public void AddToSavedPoints(GridNode _node)
        {
            _node.occupied = true;
            _node.usage = Usage.point;
            pointNodes.Add(_node);
        }
        public bool NearbyRoad(int xPos, int yPos)
        {
            GridNode node = grid.nodesGrid[xPos, yPos];
            if (node.usage == Usage.road || node.usage == Usage.point)
            {
                return true;
            }

            return false;
        }
        private bool OutOfGrid(int posX, int posY)
        {
            return grid.OutOfGrid(posX, posY);
        }
        public int[] GetLateralIncrementOnDirection(Direction direction)
        {
            int[] neighbourOffset = new int[] { 0, 0 };
            if (direction == Direction.left || direction == Direction.right)
            {
                neighbourOffset[1] = 1;
            }
            else
            {
                neighbourOffset[0] = 1;
            }
            return neighbourOffset;
        }
        public int[] GetLateralIncrementOnDirection(int dirX, int dirY)
        {
            int[] neighbourOffset = new int[] { 0, 0 };
            if (dirX != 0)
            {
                neighbourOffset[1] = 1;
            }
            else
            {
                neighbourOffset[0] = 1;
            }
            return neighbourOffset;
        }
        public int GetMovementLength(int startX, int startY, int endX, int endY)
        {
            if (startX - endX == 0) // Vertical Movement
            {
                if (endY > startY) // Subir
                {
                    return endY - startY;
                }
                return startY - endY;

            }
            else // HorizontalMovement
            {
                if (endX > startX) // Hacia derecha
                {
                    return endX - startX;
                }
                return startX - endX;
            }
        }
    }

    public class AgentParameters
    {
        public Vector3 direction;
        public int length;
        public int posX, posY;
    }

    public enum EncodingLetters
    {
        unknown = 'X',
        save = '[',
        load = ']',
        draw = 'F',
        turnRight = '+',
        turnLeft = '-'
    }

}

