using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Visualizer : MonoBehaviour
    {
        public Procedural.LSystemGenerator lsystem;
        List<Node> nodes = new List<Node>();
        [SerializeField] public Grid grid;
        //List<Vector3> positions = new List<Vector3>();
        private int length = 4;
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

        void Start()
        {
            var sequence = lsystem.GenerateSentence();
            VisualizeSequence(sequence);
        }
        // Hay que desplazarse en nodos, en cubitos del grid, luego ya mapeamos a carreteras, nada de posiciones, cubos
        private void VisualizeSequence(string sequence)
        {
            Stack<AgentParameters> savePoints = new Stack<AgentParameters>();
            Vector3 currentPosition = Vector3.zero;

            Vector3 direction = Vector3.forward;
            Vector3 tempPosition = Vector3.zero;

            Node currentNode = null;//Random

            foreach (char letter in sequence)
            {
                EncodingLetters encoding = (EncodingLetters)letter;
                switch (encoding)
                {
                    case EncodingLetters.save:
                        savePoints.Push(new AgentParameters
                        {
                            node = currentNode,
                            direction = direction,
                            length = Length
                        });

                        break;
                    case EncodingLetters.load:
                        if (savePoints.Count > 0)
                        {
                            AgentParameters agentParameter = savePoints.Pop();
                            //currentPosition = agentParameter.position; ESTO DESCOMENTAR EH
                            direction = agentParameter.direction;
                            Length = agentParameter.length;
                        }
                        else
                        {
                            throw new System.Exception("Dont have saved point in our stack");
                        }
                        break;
                    case EncodingLetters.draw:
                        tempPosition = currentPosition;
                        currentPosition += direction * length;
                        DrawLine(tempPosition, currentPosition, Color.red);
                        Length -= 2;
                        nodes.Add(currentNode);
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
                //Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject line = new GameObject("line");
            line.transform.position = start;
            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
            //lineRenderer.material = lineMaterial;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = .1f;
            lineRenderer.endWidth = .1f;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
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

    public class AgentParameters
    {
        public Node node;
        public Vector3 direction;
        public int length;
    }
}

