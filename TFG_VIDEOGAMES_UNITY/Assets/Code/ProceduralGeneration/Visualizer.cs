using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Visualizer : MonoBehaviour
    {
        private Procedural.LSystemGenerator lsystem;
        List<Node> nodes = new List<Node>();
        [SerializeField] public Grid grid;
        //List<Vector3> positions = new List<Vector3>();
        private int length = 8;
        private float angle = 90f;
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
                            currentPosY= agentParameter.posY;
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
                        if(currentPosX < 0 || currentPosX >= gridTopX)
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
                        Length -= 2;
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
                node.occupied = true;
                node.usage = Usage.point;
            }
        }

        private void DrawLine(int startX, int startY, int endX, int endY)
        {
            //Debug.Log("start: " + PrintPos(start));
            //Debug.Log("end: " + PrintPos(start));
            int length = -1;
            int[] posIncrement = { 0, 0 };
            if (startX - endX == 0) // Vertical Movement
            {
                if (endY > startY)
                {
                    length = endY - startY;
                    posIncrement[1] = 1;
                }
                else
                {
                    length = startY - endY;
                    posIncrement[1] = -1;
                }
            }
            else // HorizontalMovement
            {
                if (endX > startX)
                {
                    length = endX - startX;
                    posIncrement[0] = 1;
                }
                else
                {
                    length = startX - endX;
                    posIncrement[0] = -1;
                }
            }
            for (int i = 0; i < length; i++)
            {
                int[] currentPos = { startX + posIncrement[0] * i, startY + posIncrement[1] * i };
                Node currentNode = grid.nodesGrid[currentPos[0], currentPos[1]];
                currentNode.occupied = true;
                currentNode.usage = Usage.road;
            }
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
        private string PrintPos(int[] position)
        {
            return "[" + position[0] + ", " + position[1] + "]";
        }
    }

    public class AgentParameters
    {
        public Vector3 direction;
        public int length;
        public int posX, posY;
    }

    
}

