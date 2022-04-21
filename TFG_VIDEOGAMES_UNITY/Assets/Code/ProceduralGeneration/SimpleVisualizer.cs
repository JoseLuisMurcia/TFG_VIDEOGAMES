using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    public class SimpleVisualizer : MonoBehaviour
    {
        public LSystemGenerator lsystem;
        List<Vector3> positions = new List<Vector3>();
        public GameObject prefab;
        public Material lineMaterial;

        private int length = 12;
        private float angle = 25f;

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

        private void Start()
        {
            var sequence = lsystem.GenerateSentence();
            VisualizeSequence(sequence);
        }

        private void VisualizeSequence(string sequence)
        {
            Stack<AgentParameters> savePoints = new Stack<AgentParameters>();
            Vector3 currentPosition = Vector3.zero;

            Vector3 direction = Vector3.forward;
            Vector3 tempPosition = Vector3.zero;

            positions.Add(currentPosition);

            foreach (char letter in sequence)
            {
                EncodingLetters encoding = (EncodingLetters)letter;
                switch (encoding)
                {
                    case EncodingLetters.save:
                        savePoints.Push(new AgentParameters
                        {
                            position = currentPosition,
                            direction = direction,
                            length = Length
                        });

                        break;
                    case EncodingLetters.load:
                        if (savePoints.Count > 0)
                        {
                            AgentParameters agentParameter = savePoints.Pop();
                            currentPosition = agentParameter.position;
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
                        positions.Add(currentPosition);
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

            foreach(Vector3 position in positions)
            {
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject line = new GameObject("line");
            line.transform.position = start;
            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = .1f;
            lineRenderer.endWidth = .1f;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public enum EncodingLetters
        {
            unknown = '1',
            save = '[',
            load = ']',
            draw = 'F',
            turnRight = '+',
            turnLeft = '-'
        }
    }
}

