using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Visualizer : MonoBehaviour
    {
        private RoadPlacer roadPlacer;
        private Procedural.LSystemGenerator lsystem;
        List<Node> nodes = new List<Node>();
        List<Node> surroundingNodes = new List<Node>();
        [SerializeField] public Grid grid;
        //List<Vector3> positions = new List<Vector3>();
        private int length = 8;
        private float angle = 90f;
        private int neighboursOffset = 1;
        private int[] lengthValues = { 6, 8, 10, 12 };
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

            nodes.Add(grid.nodesGrid[currentPosX, currentPosY]);

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
                        nodes.Add(grid.nodesGrid[currentPosX, currentPosY]);
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

            foreach (Node node in nodes)
            {
                if (!node.occupied)
                {
                    node.occupied = true;
                    node.usage = Usage.point;
                }

            }
            foreach (Node node in surroundingNodes)
            {
                if (node.occupied)
                    continue;
                node.occupied = true;
                node.usage = Usage.decoration;
            }
            roadPlacer.PlaceRoadAssets(grid);
        }
        private void DrawLine(int startX, int startY, int endX, int endY)
        {
            //Debug.Log("start: " + PrintPos(start));
            //Debug.Log("end: " + PrintPos(start));
            int length = -1;
            int[] posIncrement = { 0, 0 };
            int[] neighbourIncrement = { 0, 0 };
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
            for (int i = 0; i < length; i++)
            {
                int newX = startX + posIncrement[0] * i;
                int newY = startY + posIncrement[1] * i;
                // Un check aqui para comprobar que no haya carretera en un radio de 3 nodos
                bool valid = CheckSorroundings(newX, newY, neighbourIncrement[0], neighbourIncrement[1]);
                if (!valid)
                    break;

                int[] currentPos = { newX, newY };
                Node currentNode = grid.nodesGrid[currentPos[0], currentPos[1]];
                if (!currentNode.occupied)
                {
                    currentNode.occupied = true;
                    currentNode.usage = Usage.road;

                }
            }
        }
        private bool CheckSorroundings(int posX, int posY, int xIncrement, int yIncrement)
        {
            bool valid = true;
            int i = 1;
            while (valid && i <= neighboursOffset)
            {
                int incrementedXPos = posX + xIncrement * i;
                int decreasedXPos = posX - xIncrement * i;
                int incrementedYPos = posY + yIncrement * i;
                int decreasedYPos = posY - yIncrement * i;
                if (OutOfGrid(incrementedXPos, incrementedYPos))
                {
                    valid = false;
                }
                else
                {
                    if (NearbyRoad(incrementedXPos, incrementedYPos))
                        valid = false;
                }
                if (OutOfGrid(decreasedXPos, decreasedYPos))
                {
                    valid = false;
                }
                else
                {
                    if (NearbyRoad(decreasedXPos, decreasedYPos))
                        valid = false;
                }
                i++;
            }
            if (valid)
                MarkSorroundingNodes(posX, posY, xIncrement, yIncrement);
            return valid;
        }
        private void MarkSorroundingNodes(int posX, int posY, int xIncrement, int yIncrement)
        {
            for (int i = 1; i <= neighboursOffset; i++)
            {
                Node increasedNode = grid.nodesGrid[posX + xIncrement * i, posY + yIncrement * i];
                Node decreasedNode = grid.nodesGrid[posX - xIncrement * i, posY - yIncrement * i];
                increasedNode.occupied = true;
                decreasedNode.occupied = true;
                increasedNode.usage = Usage.decoration;
                decreasedNode.usage = Usage.decoration;
                surroundingNodes.Add(increasedNode);
                surroundingNodes.Add(decreasedNode);
            }
        }
        private bool NearbyRoad(int xPos, int yPos)
        {
            Node node = grid.nodesGrid[xPos, yPos];
            if (node.occupied && node.usage != Usage.decoration)
                return true;

            return false;
        }
        private string PrintPos(int[] position)
        {
            return "[" + position[0] + ", " + position[1] + "]";
        }

        private void AddNeighboursToList(List<Node> nodesList, int posX, int posY, int xIncrement, int yIncrement)
        {
            // Loop to allocate room to n = neighboursOffset
            for (int i = 1; i <= neighboursOffset; i++)
            {
                if (!OutOfGrid(posX + xIncrement * i, posY + yIncrement * i))
                    nodesList.Add(grid.nodesGrid[posX + xIncrement * i, posY + yIncrement * i]);
                if (!OutOfGrid(posX - xIncrement * i, posY - yIncrement * i))
                    nodesList.Add(grid.nodesGrid[posX - xIncrement * i, posY - yIncrement * i]);
            }

        }
        private bool OutOfGrid(int posX, int posY)
        {
            if (posX >= grid.gridSizeX || posY >= grid.gridSizeY)
                return true;
            if (posX < 0 || posY < 0)
                return true;

            return false;
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

