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
                    int numNeighbours = GetNumNeighbours(i, j);
                    switch (numNeighbours)
                    {
                        case 1:
                            Instantiate(roadEnd, currentNode.worldPosition, Quaternion.identity, transform);
                            break;
                        case 2:
                            Instantiate(roadStraight, currentNode.worldPosition, Quaternion.identity, transform);
                            break;
                        case 3:
                            Instantiate(road3way, currentNode.worldPosition, Quaternion.identity, transform);
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

        private int GetNumNeighbours(int posX, int posY)
        {
            int numNeighbours = 0;
            int limitX = grid.gridSizeX; int limitY = grid.gridSizeY;
            if (posX - 1 >= 0 && posX + 1 < limitX)
            {
                if (grid.nodesGrid[posX + 1, posY].occupied && grid.nodesGrid[posX + 1, posY].usage != Usage.decoration)
                    numNeighbours += 1;
                if (grid.nodesGrid[posX - 1, posY].occupied && grid.nodesGrid[posX - 1, posY].usage != Usage.decoration)
                    numNeighbours += 1;
            }
            if (posY - 1 >= 0 && posY + 1 < limitY)
            {
                if (grid.nodesGrid[posX, posY + 1].occupied && grid.nodesGrid[posX, posY + 1].usage != Usage.decoration)
                    numNeighbours += 1;
                if (grid.nodesGrid[posX, posY - 1].occupied && grid.nodesGrid[posX, posY - 1].usage != Usage.decoration)
                    numNeighbours += 1;
            }
            return numNeighbours;
        }
    }
}
