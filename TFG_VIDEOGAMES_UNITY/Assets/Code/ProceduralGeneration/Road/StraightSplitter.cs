using PG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;
namespace PG
{
    public class StraightSplitter : MonoBehaviour
    {
        private GameObject crossingPrefab;
        private GameObject crossingTLPrefab;
        private int[] divisionFactor = new int[2];
        public StraightSplit HandleUnifiedStraight(Straight unifiedStraight, List<Direction> directions)
        {
            // Necesito definir reglas para cómo voy a dividir la recta sin ser repetitivo, pero con lógica
            // La densidad va en funcion de la región/zona. 1 - centro, 2 - residencial, 3 - suburbios
            // Minimo tiene que haber X distancia entre el crossing y la interseccion o el otro crossing.

            // Descartar si no hay nodos suficientes
            if (unifiedStraight.gridNodes.Count < 5)
                return new StraightSplit(new List<Straight>(), new Dictionary<Vector2Int, GameObject>());

            // Encontrar la region mayoritaria en la que está la recta. Esto se puede mejorar, haciendo una particion especifica para la region, pero meh
            Dictionary<Region, int> regionCounts = new Dictionary<Region, int>
            {
                { Region.Main, 0 },
                { Region.Residential, 0 },
                { Region.Suburbs, 0 }
            };

            foreach (GridNode gridNode in unifiedStraight.gridNodes)
            {
                if (regionCounts.ContainsKey(gridNode.regionType))
                {
                    regionCounts[gridNode.regionType]++;
                }
            }

            Region roadRegion = regionCounts.OrderByDescending(kv => kv.Value).First().Key;

            // Con esto podemos saber cuantos vecinos tiene el entry y exit.
            // Si tiene 1 o 2 vecinos, es una curva o sin salida. Por lo que no tiene señales ni trafficlight
            // Si tiene 3 vecinos, tiene señales o trafficLight
            // Si tiene 4 vecinos, tiene trafficLight
            GridNode entryNode;
            GridNode exitNode;
            if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
            {
                entryNode = Grid.Instance.nodesGrid[unifiedStraight.gridNodes[0].gridX, unifiedStraight.gridNodes[0].gridY + 1];
                exitNode = Grid.Instance.nodesGrid[unifiedStraight.gridNodes.Last().gridX, unifiedStraight.gridNodes.Last().gridY - 1];              
            }
            else
            {
                entryNode = Grid.Instance.nodesGrid[unifiedStraight.gridNodes[0].gridX - 1, unifiedStraight.gridNodes[0].gridY];
                exitNode = Grid.Instance.nodesGrid[unifiedStraight.gridNodes.Last().gridX + 1, unifiedStraight.gridNodes.Last().gridY];
            }
            // Direction to advance
            Direction direction = RoadPlacer.Instance.GetDirectionBasedOnPos(entryNode, exitNode);

            bool connectsToRoundabout = ConnectsToRoundabout(exitNode, direction);
            // The min nodes to be able to split a road
            int minNodes = -1;
            int nodesToTakeOnFailure = -1;
            
            int roadLength = unifiedStraight.gridNodes.Count;
            if (roadLength <= 10) // 5-10 Short road
            {
                nodesToTakeOnFailure = 3;
                // Dividir según región
                switch (roadRegion)
                {
                    case Region.Main: // Mayor densidad en la región principal
                        divisionFactor[0] = 3;
                        divisionFactor[1] = 5;
                        minNodes = connectsToRoundabout ? 7 : 6;
                        break;
                    case Region.Residential: // Densidad media en áreas residenciales
                        divisionFactor[0] = 4;
                        divisionFactor[1] = 5;
                        minNodes = connectsToRoundabout ? 7 : 6;
                        break;
                    case Region.Suburbs: // Menor densidad en suburbios
                        divisionFactor[0] = 5;
                        divisionFactor[1] = 6;
                        minNodes = connectsToRoundabout ? 7 : 6;
                        break;
                    default: // Valor por defecto
                        divisionFactor[0] = 10;
                        divisionFactor[1] = 12;
                        minNodes = connectsToRoundabout ? 7 : 6;
                        break;
                }
            }
            else if (roadLength <= 20) // 11-20 Medium road
            {
                nodesToTakeOnFailure = 4;
                // Dividir según región
                switch (roadRegion)
                {
                    case Region.Main: // Mayor densidad en la región principal
                        divisionFactor[0] = 4;
                        divisionFactor[1] = 7;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Residential: // Densidad media en áreas residenciales
                        divisionFactor[0] = 5;
                        divisionFactor[1] = 7;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Suburbs: // Menor densidad en suburbios
                        divisionFactor[0] = 6;
                        divisionFactor[1] = 8;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    default: // Valor por defecto
                        divisionFactor[0] = 10;
                        divisionFactor[1] = 12;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                }
            }
            else if (roadLength <= 30) // 21-30 Long road
            {
                nodesToTakeOnFailure = 5;
                // Dividir según región
                switch (roadRegion)
                {
                    case Region.Main: // Mayor densidad en la región principal
                        divisionFactor[0] = 7;
                        divisionFactor[1] = 11;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Residential: // Densidad media en áreas residenciales
                        divisionFactor[0] = 8;
                        divisionFactor[1] = 11;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Suburbs: // Menor densidad en suburbios
                        divisionFactor[0] = 9;
                        divisionFactor[1] = 13;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    default: // Valor por defecto
                        divisionFactor[0] = 10;
                        divisionFactor[1] = 12;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                }
            }
            else // 30-?? Ultra long road
            {
                nodesToTakeOnFailure = 5;
                // Dividir según región
                switch (roadRegion)
                {
                    case Region.Main: // Mayor densidad en la región principal
                        divisionFactor[0] = 9;
                        divisionFactor[1] = 14;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Residential: // Densidad media en áreas residenciales
                        divisionFactor[0] = 10;
                        divisionFactor[1] = 14;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    case Region.Suburbs: // Menor densidad en suburbios
                        divisionFactor[0] = 11;
                        divisionFactor[1] = 16;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                    default: // Valor por defecto
                        divisionFactor[0] = 10;
                        divisionFactor[1] = 12;
                        minNodes = connectsToRoundabout ? 8 : 7;
                        break;
                }
            }

            // Calcular el número máximo de divisiones posibles basado en el rango
            int minDivisionLength = divisionFactor[0];
            int maxDivisionLength = divisionFactor[1];
            // Lista para almacenar los nuevos segmentos de Straight
            List<Straight> dividedStraights = new List<Straight>();

            // Índice actual en el gridNodes
            int currentIndex = 0;
            while (currentIndex < unifiedStraight.gridNodes.Count)
            {
                Straight newStraight = new Straight();

                int remainingNodes = unifiedStraight.gridNodes.Count - currentIndex;
                int nodesToTake = Random.Range(minDivisionLength, maxDivisionLength + 1);

                bool lastIteration = false;
                if (remainingNodes <= nodesToTake + 1)
                {
                    lastIteration = true;
                    nodesToTake = remainingNodes; // Leave at least one node for the crossing
                }

                // Hacer un check para recalcular nodesToTake en caso de que para la siguiente iteracion
                // vaya a quedar una recta demasiado corta (A.K.A) un crossing demasiado cerca del final de la recta
                if (!lastIteration)
                {
                    int nodesAvailableForNextIteration = remainingNodes - (nodesToTake + 1);
                    // Si para la siguiente recta quedarán menos de 3 nodos, el crossing estaría a 2 nodos de la interseccion.
                    // Si quedan 3 o 4, la siguiente iteración será la ultima
                    if (nodesAvailableForNextIteration < 3)
                    {
                        // Recalcular hasta encontrar solucion
                        // Cuantos nodos quedan? Ver si con los que quedan es factible partir la recta o no
                        if (remainingNodes < minNodes)
                        {
                            nodesToTake = remainingNodes;
                        }
                        else
                        {
                            // Split the road
                            // Esto puede que falle
                            nodesToTake = nodesToTakeOnFailure;
                        }
                    }
                }

                try
                {
                    newStraight.gridNodes = unifiedStraight.gridNodes.GetRange(currentIndex, nodesToTake);
                }
                catch (System.Exception ex)
                {
                    var currentNode = unifiedStraight.gridNodes[currentIndex];
                    SpawnSphere(Grid.Instance.nodesGrid[currentNode.gridX, currentNode.gridY].worldPosition, Color.white, 2f, 2.5f);
                    foreach (var node in newStraight.gridNodes)
                    {
                        SpawnSphere(node.worldPosition, Color.yellow, 2f, 1.5f);
                    }
                    Debug.LogWarning(ex.Message + ", currentIndex: " + currentIndex + ", nodesToTake: " + nodesToTake + 
                        ", remainingNodes: " + remainingNodes + ", totalNodesCount: " + unifiedStraight.gridNodes.Count
                        + ", maxDivisionLength: " + maxDivisionLength);
                }
                currentIndex += nodesToTake;

                newStraight.position = CalculateAveragePosition(newStraight.gridNodes);
                dividedStraights.Add(newStraight);

                // Reservar un nodo para el crossing si no es el último segmento
                if (currentIndex < unifiedStraight.gridNodes.Count)
                {
                    currentIndex++;
                }
            }
            Dictionary<Vector2Int, GameObject> crossingDictionary = new Dictionary<Vector2Int, GameObject>();

            // Colocar los crossings entre los segmentos
            for (int i = 0; i < dividedStraights.Count - 1; i++)
            {
                PlacePedestrianCrossing(crossingDictionary, directions, dividedStraights[i], dividedStraights[i + 1], roadRegion);
            }

            return new StraightSplit(dividedStraights, crossingDictionary);
        }

        // Función auxiliar para calcular la posición media de los nodos
        private Vector2Int CalculateAveragePosition(List<GridNode> gridNodes)
        {
            int totalX = 0;
            int totalY = 0;

            foreach (var node in gridNodes)
            {
                totalX += node.gridX;
                totalY += node.gridY;
            }

            int avgX = Mathf.RoundToInt((float)totalX / gridNodes.Count);
            int avgY = Mathf.RoundToInt((float)totalY / gridNodes.Count);

            return new Vector2Int(avgX, avgY);
        }
        private bool ConnectsToRoundabout(GridNode node, Direction direction)
        {
            // Check if the proposed node connects to a roundabout
            int[] movementOffset = RoadPlacer.Instance.DirectionToInt(direction);
            int newX = node.gridX + movementOffset[0];
            int newY = node.gridY + movementOffset[1];

            if (Grid.Instance.OutOfGrid(newX, newY))
                return false;

            GridNode connectionNode = Grid.Instance.nodesGrid[newX, newY];
            if (connectionNode.roadType == RoadType.Roundabout)
                return true;

            return false;
        }
        // Función auxiliar para colocar un pedestrian crossing entre dos segmentos
        private void PlacePedestrianCrossing(Dictionary<Vector2Int, GameObject> crossings, List<Direction> directions, Straight firstStraight, Straight secondStraight, Region roadRegion)
        {
            // Assign randomPrefab
            GameObject chosenPrefab;
            float value = Random.value;
            switch (roadRegion)
            {
                case Region.Main: // 40% Chance traffic light
                    chosenPrefab = value > 0.4f ? crossingPrefab : crossingTLPrefab;
                    break;
                case Region.Residential: // 30% Chance traffic light
                    chosenPrefab = value > 0.3f ? crossingPrefab : crossingTLPrefab;
                    break;
                case Region.Suburbs: // 15% Chance traffic light
                    chosenPrefab = value > 0.15f ? crossingPrefab : crossingTLPrefab;
                    break;
                default:
                    chosenPrefab = crossingPrefab;
                    break;
            }

            // Spawn prefab
            // Lógica para colocar un crossing entre firstStraight y secondStraight
            // Esto puede incluir instanciar un prefab, configurar posiciones, etc.
            GridNode firstNode = firstStraight.gridNodes.Last();
            GridNode secondNode = secondStraight.gridNodes.First();
            Vector2Int position = CalculateAveragePosition(new List<GridNode> { firstNode, secondNode });
            Quaternion rotation = Quaternion.identity;

            if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
            GameObject crossingGO = Instantiate(chosenPrefab, Grid.Instance.nodesGrid[position.x, position.y].worldPosition, rotation, transform);
            crossings.Add(position, crossingGO);
        }
        public void SetCrossingPrefabs(GameObject _crossingPrefab, GameObject _crossingTLPrefab)
        {
            crossingPrefab = _crossingPrefab;
            crossingTLPrefab = _crossingTLPrefab;
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
    }

    public class Straight
    {
        public List<GridNode> gridNodes = new List<GridNode>();
        public Vector2Int position = Vector2Int.zero;
        public GameObject gameObject;
        public Vector3 center = Vector3.zero;

        public void SetCenterPosition()
        {
            int numNodes = gridNodes.Count;
            Vector3 pos = Vector3.zero;
            foreach (GridNode node in gridNodes)
            {
                node.isProcessedStraight = true;
                pos += node.worldPosition;
            }
            center = pos / numNodes;
        }
    }

    public struct StraightSplit
    {
        public List<Straight> dividedStraights;
        public Dictionary<Vector2Int, GameObject> crossingDictionary;

        public StraightSplit(List<Straight> _dividedStraights, Dictionary<Vector2Int, GameObject> _crossingDictionary)
        {
            dividedStraights = _dividedStraights;
            crossingDictionary = _crossingDictionary;
        }
    }
}

