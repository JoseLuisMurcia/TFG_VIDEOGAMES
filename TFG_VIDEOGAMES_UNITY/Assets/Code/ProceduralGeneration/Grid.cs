using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
	public class Grid : MonoBehaviour
	{
		public LayerMask unwalkableMask;
		public Vector2 gridWorldSize;
		public float nodeRadius;
		public Node[,] nodesGrid;

		float nodeDiameter;
		public int gridSizeX, gridSizeY;

		void Start()
		{
			nodeDiameter = nodeRadius * 2;
			gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
			gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
			CreateGrid();
		}

		void CreateGrid()
		{
			nodesGrid = new Node[gridSizeX, gridSizeY];
			Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

			for (int x = 0; x < gridSizeX; x++)
			{
				for (int y = 0; y < gridSizeY; y++)
				{
					Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
					nodesGrid[x, y] = new Node(worldPoint);
				}
			}
		}

		void OnDrawGizmos()
		{
			Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));


			if (nodesGrid != null)
			{
				foreach (Node n in nodesGrid)
				{
                    switch (n.usage)
                    {
                        case Usage.empty:
							Gizmos.color = Color.white;
							break;
                        case Usage.road:
							Gizmos.color = Color.magenta;
							break;
                        case Usage.point:
							Gizmos.color = Color.red;
							break;
                        default:
                            break;
                    }
					Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
				}
			}
		}
	}

	public class Node
	{
		public Vector3 worldPosition;
		public bool occupied = false;
		public Usage usage = Usage.empty;
		public Node(Vector3 _worldPos)
		{
			worldPosition = _worldPos;
		}
	}

	public enum Usage
    {
		empty,
		road,
		point
    }
}

