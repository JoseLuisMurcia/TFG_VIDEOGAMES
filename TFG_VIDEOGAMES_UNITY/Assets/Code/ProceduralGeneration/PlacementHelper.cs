using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    public static class PlacementHelper
    {
        public static List<Direction> FindNeighbour(Vector3Int position, ICollection<Vector3Int> collection)
        {
            List<Direction> neighbourDirections = new List<Direction>();
            if(collection.Contains(position + Vector3Int.right))
            {
                neighbourDirections.Add(Direction.Right);
            }
            if (collection.Contains(position - Vector3Int.right))
            {
                neighbourDirections.Add(Direction.Left);
            }
            if (collection.Contains(position + Vector3Int.forward))
            {
                neighbourDirections.Add(Direction.Up);
            }
            if (collection.Contains(position - Vector3Int.forward))
            {
                neighbourDirections.Add(Direction.Down);
            }
            return neighbourDirections;
        }
    }
}

