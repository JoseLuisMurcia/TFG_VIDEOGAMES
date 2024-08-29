using PG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace PG
{
    public class StraightSplitter : MonoBehaviour
    {
        private GameObject crossingPrefab;
        private GameObject crossingTLPrefab;
        public StraightSplit HandleUnifiedStraight(Straight unifiedStraight, List<Direction> directions, Road entryRoad, Road exitRoad)
        {
            // Necesito definir reglas para c�mo voy a dividir la recta sin ser repetitivo, pero con l�gica
            // La densidad va en funcion de la regi�n/zona. 1 - centro, 2 - residencial, 3 - suburbios
            // Minimo tiene que haber X distancia entre el crossing y la interseccion o el otro crossing.

            // Descartar si no hay nodos suficientes
            if (unifiedStraight.gridNodes.Count < 5)
                return new StraightSplit(new List<Straight>(), new Dictionary<Vector2Int, GameObject>());

            // Encontrar la region mayoritaria en la que est� la recta. Esto se puede mejorar, haciendo una particion especifica para la region, pero meh
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

            // Dividir seg�n regi�n
            // Mirar al vecino interseccion del entry y el exit road
            // TODO Recuperar los vecinos en base al grid, no a las roads...
            // int numEntryRoadNeighbours = entryRoad.connections.First(road => road.typeOfRoad == TypeOfRoad.Intersection).connections.Count;
            // int numExitRoadNeighbours = exitRoad.connections.First(road => road.typeOfRoad == TypeOfRoad.Intersection).connections.Count;

            int divisionFactor;
            switch (roadRegion)
            {
                case Region.Main:
                    divisionFactor = 4; // Mayor densidad en la regi�n principal
                    break;
                case Region.Residential:
                    divisionFactor = 6; // Densidad media en �reas residenciales
                    break;
                case Region.Suburbs:
                    divisionFactor = 8; // Menor densidad en suburbios
                    break;
                default:
                    divisionFactor = 10; // Valor por defecto
                    break;
            }

            // Calcular el n�mero m�ximo de divisiones posibles
            int numDivisions = (unifiedStraight.gridNodes.Count) / (divisionFactor + 1);

            // Lista para almacenar los nuevos segmentos de Straight
            List<Straight> dividedStraights = new List<Straight>();

            // �ndice actual en el gridNodes
            int currentIndex = 0;

            for (int i = 0; i < numDivisions; i++)
            {
                // Crear nuevo segmento
                Straight newStraight = new Straight();

                // N�mero de nodos a tomar, ajustado para el �ltimo segmento
                int nodesToTake = (i == numDivisions - 1) ? unifiedStraight.gridNodes.Count - currentIndex : divisionFactor;

                newStraight.gridNodes = unifiedStraight.gridNodes.GetRange(currentIndex, nodesToTake);
                currentIndex += nodesToTake;

                // Establecer la posici�n media para el segmento
                Vector2Int averagePosition = CalculateAveragePosition(newStraight.gridNodes);
                newStraight.position = averagePosition;

                // A�adir el nuevo segmento a la lista
                dividedStraights.Add(newStraight);

                // Si no es el �ltimo segmento, reservar un nodo para el crossing
                if (i < numDivisions)
                {
                    currentIndex++; // Avanzar un nodo para dejar espacio para el crossing
                }
            }
            Dictionary<Vector2Int, GameObject> crossingDictionary = new Dictionary<Vector2Int, GameObject>();

            // Colocar los crossings entre los segmentos
            for (int i = 0; i < dividedStraights.Count - 1; i++)
            {
                PlacePedestrianCrossing(crossingDictionary, directions, dividedStraights[i], dividedStraights[i + 1]);
            }

            return new StraightSplit(dividedStraights, crossingDictionary);
        }

        // Funci�n auxiliar para calcular la posici�n media de los nodos
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

        // Funci�n auxiliar para colocar un pedestrian crossing entre dos segmentos
        private void PlacePedestrianCrossing(Dictionary<Vector2Int, GameObject> crossings, List<Direction> directions, Straight firstStraight, Straight secondStraight)
        {
            // L�gica para colocar un crossing entre firstStraight y secondStraight
            // Esto puede incluir instanciar un prefab, configurar posiciones, etc.
            GridNode firstNode = firstStraight.gridNodes[firstStraight.gridNodes.Count - 1];
            GridNode secondNode = secondStraight.gridNodes[0];
            Vector2Int position = CalculateAveragePosition(new List<GridNode> { firstNode, secondNode }); 
            Quaternion rotation = Quaternion.identity;
            if (directions.Contains(Direction.forward) || directions.Contains(Direction.back))
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
            GameObject crossingGO = Instantiate(crossingPrefab, Grid.Instance.nodesGrid[position.x, position.y].worldPosition, rotation, transform);
            crossings.Add(position, crossingGO);
        }
        public void SetCrossingPrefabs(GameObject _crossingPrefab, GameObject _crossingTLPrefab)
        {
            crossingPrefab = _crossingPrefab;
            crossingTLPrefab = _crossingTLPrefab;
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

