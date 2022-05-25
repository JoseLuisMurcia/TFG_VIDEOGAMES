using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Visualizer : MonoBehaviour
    {
        private RoadPlacer roadPlacer;
        private Procedural.LSystemGenerator lsystem;
        [HideInInspector] public List<Node> pointNodes = new List<Node>();
        [HideInInspector] List<Node> surroundingNodes = new List<Node>();
        [SerializeField] public Grid grid;
        //List<Vector3> positions = new List<Vector3>();
        private int length = 8;
        private float angle = 90f;
        [SerializeField] private int neighboursOffset = 3;
        //private int[] lengthValues = { 6, 8, 10, 12 };
        private int[] lengthValues = {8, 12 };
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
            lsystem = GetComponent<Procedural.LSystemGenerator>();
            roadPlacer = GetComponent<RoadPlacer>();
        }
        void Start()
        {
            var sequence = lsystem.GenerateSentence();
            VisualizeSequence(sequence);
            Debug.Log(sequence);
        }
        // Hay que desplazarse en nodos, en cubitos del grid, luego ya mapeamos a carreteras, nada de posiciones, cubos
        private void VisualizeSequence(string sequence)
        {
            Stack<AgentParameters> savePoints = new Stack<AgentParameters>();
            int gridTopX = grid.gridSizeX;
            int gridTopY = grid.gridSizeY;
            int currentPosX = gridTopX / 2;
            int currentPosY = gridTopY / 2;
            int tempPosX = currentPosX;
            int tempPosY = currentPosY;

            Vector3 direction = Vector3.forward;

            Node firstNode = grid.nodesGrid[currentPosX, currentPosY];
            AddToSavedPoints(firstNode);

            foreach (char letter in sequence)
            {
                //Debug.Log("Current Pos: [" + currentPos[0] + ", " + currentPos[1] + "]");
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
                        DrawLine(tempPosX, tempPosY, currentPosX, currentPosY);
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
            roadPlacer.PlaceRoadAssets(grid, this);
        }
        private void DrawLine(int startX, int startY, int endX, int endY)
        {
            Node startNode = grid.nodesGrid[startX, startY];
            if (startNode.usage != Usage.point)
                return;
            //Debug.Log("start: " + PrintPos(start));
            //Debug.Log("end: " + PrintPos(start));
            int length = -1;
            int[] posIncrement = { 0, 0 };
            int[] neighbourIncrement = { 0, 0 };
            // Calculate the offset
            if (startX - endX == 0) // Vertical Movement
            {
                if (endY > startY) // Subir
                {
                    length = endY - startY;
                    posIncrement[1] = 1;
                }
                else // Bajar
                {
                    length = startY - endY;
                    posIncrement[1] = -1;
                }
                neighbourIncrement[0] = 1;
            }
            else // HorizontalMovement
            {
                if (endX > startX) // Hacia derecha
                {
                    length = endX - startX;
                    posIncrement[0] = 1;
                }
                else // Hacia izquierda
                {
                    length = startX - endX;
                    posIncrement[0] = -1;
                }
                neighbourIncrement[1] = 1;
            }

            // Check if the grid is available for this advance
            for (int i = 1; i < length; i++)
            {
                int newX = startX + posIncrement[0] * i;
                int newY = startY + posIncrement[1] * i;

                if (!CheckSurroundings(newX, newY, neighbourIncrement[0], neighbourIncrement[1]))
                    return;
            }

            // Mark the road
            for (int i = 0; i < length; i++)
            {
                int newX = startX + posIncrement[0] * i;
                int newY = startY + posIncrement[1] * i;
                // Un check aqui para comprobar que no haya carretera en un radio de 3 nodos
                // Habria que hacer checksurroundings para toda la length en la que nos vayamos a extender o sea, las 2 dimensiones, porque si no, estamos marcando como
                // ocupados y carreteras, nodos que son invalidos y luego no se puede revertir
                int[] currentPos = { newX, newY };
                Node currentNode = grid.nodesGrid[currentPos[0], currentPos[1]];
                if (currentNode.usage != Usage.road && currentNode.usage != Usage.point)
                {
                    currentNode.occupied = true;
                    currentNode.usage = Usage.road;
                    MarkSurroundingNodes(newX, newY, neighbourIncrement[0], neighbourIncrement[1]);
                }
            }
            Node endNode = grid.nodesGrid[endX, endY];
            AddToSavedPoints(endNode);
        }
        private bool CheckSurroundings(int posX, int posY, int xIncrement, int yIncrement)
        {
            int i = 1;
            while (i <= neighboursOffset)
            {
                int incrementedXPos = posX + xIncrement * i;
                int decreasedXPos = posX - xIncrement * i;
                int incrementedYPos = posY + yIncrement * i;
                int decreasedYPos = posY - yIncrement * i;

                if (OutOfGrid(incrementedXPos, incrementedYPos))
                {
                    return false;
                }
                else
                {
                    if (NearbyRoad(incrementedXPos, incrementedYPos))
                        return false;
                }
                if (OutOfGrid(decreasedXPos, decreasedYPos))
                {
                    return false;
                }
                else
                {
                    if (NearbyRoad(decreasedXPos, decreasedYPos))
                        return false;
                }
                i++;
            }
            return true;
        }
        private void MarkSurroundingNodes(int posX, int posY, int xIncrement, int yIncrement)
        {
            for (int i = 1; i <= neighboursOffset; i++)
            {
                if (!OutOfGrid(posX + xIncrement * i, posY + yIncrement * i))
                {
                    Node increasedNode = grid.nodesGrid[posX + xIncrement * i, posY + yIncrement * i];
                    if (!increasedNode.occupied)
                    {
                        increasedNode.occupied = true;
                        increasedNode.usage = Usage.decoration;
                        surroundingNodes.Add(increasedNode);
                    }
                }

                if (!OutOfGrid(posX - xIncrement * i, posY - yIncrement * i))
                {
                    Node decreasedNode = grid.nodesGrid[posX - xIncrement * i, posY - yIncrement * i];
                    if (!decreasedNode.occupied)
                    {
                        decreasedNode.occupied = true;
                        decreasedNode.usage = Usage.decoration;
                        surroundingNodes.Add(decreasedNode);
                    }
                }

            }
        }
        public void AddToSavedPoints(Node _node)
        {
            _node.occupied = true;
            _node.usage = Usage.point;
            pointNodes.Add(_node);
        }
        private bool NearbyRoad(int xPos, int yPos)
        {
            Node node = grid.nodesGrid[xPos, yPos];
            if (node.occupied && node.usage != Usage.decoration)
            {
                //SpawnSphere(node.worldPosition);
                return true;
            }

            return false;
        }
        private bool OutOfGrid(int posX, int posY)
        {
            return grid.OutOfGrid(posX, posY);
        }
        private void SpawnSphere(Vector3 pos)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.position = pos + Vector3.up * 2f;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
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

