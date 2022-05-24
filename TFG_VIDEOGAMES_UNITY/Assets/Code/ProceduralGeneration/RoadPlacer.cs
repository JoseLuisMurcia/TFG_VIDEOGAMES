using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PG
{
    public class RoadPlacer : MonoBehaviour
    {
        public GameObject roadStraight, roadCorner, road3way, road4way, roadEnd;
        private PG.Grid grid;

        public void PlaceRoadAssets(PG.Grid _grid)
        {
            grid = _grid;
            for (int i = 0; i < grid.gridSizeX; i++)
            {
                for (int j = 0; j < grid.gridSizeY; j++)
                {
                    Node currentNode = grid.nodesGrid[i, j];
                    NeighboursData data = GetNumNeighbours(i, j);
                    List<Directions> neighbours = data.neighbours;
                    if (currentNode.occupied && currentNode.usage != Usage.decoration)
                    {
                        Quaternion rotation = Quaternion.identity;
                        //SpawnSphere(currentNode.worldPosition);
                        switch (data.neighbours.Count)
                        {
                            case 1:
                                Instantiate(roadEnd, currentNode.worldPosition, Quaternion.identity, transform);
                                break;
                            case 2:

                                if ((neighbours.Contains(Directions.left) && neighbours.Contains(Directions.right)) || (neighbours.Contains(Directions.forward) && neighbours.Contains(Directions.back)))
                                {
                                    if (neighbours.Contains(Directions.forward) || neighbours.Contains(Directions.back))
                                    {
                                        rotation = Quaternion.Euler(0, 90, 0);
                                    }
                                    Instantiate(roadStraight, currentNode.worldPosition, rotation, transform);
                                }
                                else
                                {
                                    if (neighbours.Contains(Directions.left) && neighbours.Contains(Directions.back))
                                    {
                                        rotation = Quaternion.Euler(0, 180, 0);
                                    }
                                    else if (neighbours.Contains(Directions.left) && neighbours.Contains(Directions.forward))
                                    {
                                        rotation = Quaternion.Euler(0, -90, 0);
                                    }
                                    else if (neighbours.Contains(Directions.back) && neighbours.Contains(Directions.right))
                                    {
                                        rotation = Quaternion.Euler(0, 90, 0);
                                    }
                                    Instantiate(roadCorner, currentNode.worldPosition, rotation, transform);
                                }
                                break;
                            case 3:
                                if (neighbours.Contains(Directions.left) && neighbours.Contains(Directions.forward) && neighbours.Contains(Directions.back))
                                {
                                    rotation = Quaternion.Euler(0, -90, 0);
                                }
                                else if (neighbours.Contains(Directions.right) && neighbours.Contains(Directions.back) && neighbours.Contains(Directions.left))
                                {
                                    rotation = Quaternion.Euler(0, 180, 0);
                                }
                                else if (neighbours.Contains(Directions.right) && neighbours.Contains(Directions.forward) && neighbours.Contains(Directions.back))
                                {
                                    rotation = Quaternion.Euler(0, 90, 0);
                                }
                                Instantiate(road3way, currentNode.worldPosition, rotation, transform);
                                break;
                            case 4:
                                Instantiate(road4way, currentNode.worldPosition, Quaternion.identity, transform);
                                break;
                            default:
                                break;
                        }
                    }

                }
            }
        }
        private NeighboursData GetNumNeighbours(int posX, int posY)
        {
            NeighboursData data = new NeighboursData();
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX + 1 < limitX)
            {
                if (grid.nodesGrid[posX + 1, posY].occupied && grid.nodesGrid[posX + 1, posY].usage != Usage.decoration) // Right
                    data.neighbours.Add(Directions.right);
            }
            if (posX - 1 >= 0)
            {
                if (grid.nodesGrid[posX - 1, posY].occupied && grid.nodesGrid[posX - 1, posY].usage != Usage.decoration) // Left
                    data.neighbours.Add(Directions.left);
            }

            if (posY + 1 < limitY)
            {
                if (grid.nodesGrid[posX, posY + 1].occupied && grid.nodesGrid[posX, posY + 1].usage != Usage.decoration) // Up
                    data.neighbours.Add(Directions.forward);
            }

            if (posY - 1 >= 0)
            {
                if (grid.nodesGrid[posX, posY - 1].occupied && grid.nodesGrid[posX, posY - 1].usage != Usage.decoration) // Down
                    data.neighbours.Add(Directions.back);
            }
            return data;
        }

        private void SpawnSphere(Vector3 pos)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.parent = transform;
            startSphere.transform.position = pos + Vector3.up * 3f;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
        }
    }

    public class NeighboursData
    {
        public List<Directions> neighbours = new List<Directions>();
    }

    public enum Directions
    {
        left,
        right,
        forward,
        back
    }
}
