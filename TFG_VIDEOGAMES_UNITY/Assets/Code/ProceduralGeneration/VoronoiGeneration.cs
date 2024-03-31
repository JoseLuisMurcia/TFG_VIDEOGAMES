using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace PG
{
    public class VoronoiGeneration : MonoBehaviour
    {
        [SerializeField]
        public int size;

        public int regionAmount;

        List<VoronoiRegion> voronoiRegions;
        Vector2[] points;
        [HideInInspector]
        public Vector3 worldBottomLeft;

        [SerializeField]
        private DebugType debugType;

        public void SetupVoronoi(int gridSize)
        {
            size = gridSize;
            points = new Vector2[regionAmount];

            voronoiRegions = new List<VoronoiRegion>(size);
            for (int i = 0; i < regionAmount; i++)
            {
                points[i] = new Vector2(Random.Range(0, size), Random.Range(0, size));
                Color nodeColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
                voronoiRegions.Add(new VoronoiRegion(nodeColor, points[i], i));
            }
        }
        public void SetRegions(int x, int y, Node node)
        {
            float distance = float.MaxValue;
            int value = 0;

            Vector2 currentPos = new Vector2(x, y);
            for (int i = 0; i < regionAmount; i++)
            {
                if (Vector2.Distance(currentPos, points[i]) < distance)
                {
                    distance = Vector2.Distance(currentPos, points[i]);
                    value = i;
                }
            }
            int regionId = value % regionAmount;
            voronoiRegions[regionId].nodes.Add(node);
            node.voronoiRegion = voronoiRegions[regionId];
        }
        public void Cleanup()
        {
            List<VoronoiRegion> regionsToRemove = new List<VoronoiRegion>();
            foreach(var region in voronoiRegions)
            {
                if (region.nodes.Count <= 0) 
                { 
                    regionsToRemove.Add(region);
                }
            }
            voronoiRegions.RemoveAll(r => regionsToRemove.Contains(r));
            regionAmount = voronoiRegions.Count;
        }
        public void SetNeighbourRegions()
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Node currentNode = Grid.Instance.nodesGrid[x, y];
                    List<Node> neighbours = Grid.Instance.GetNeighboursForVoronoi(currentNode);
                    Color currentColor = currentNode.voronoiRegion.color;
                    foreach (Node neighbour in neighbours)
                    {
                        Color neighbourColor = neighbour.voronoiRegion.color;
                        // Find colour for that region and compare it with current node region
                        // IF != then add neighbour region for both voronoiRegions
                        if (currentColor != neighbourColor)
                        {
                            VoronoiRegion currentRegion = currentNode.voronoiRegion;
                            VoronoiRegion neighbourRegion = neighbour.voronoiRegion;
                            currentRegion.neighbourRegions.Add(neighbourRegion);
                            neighbourRegion.neighbourRegions.Add(currentRegion);
                        }
                    }
            
                }
            }
        }
        public void SetCentres()
        {
            foreach (var region in voronoiRegions)
            {
                float nodesCount = (float) region.nodes.Count;
                Vector2 centre = Vector2.zero;
                for (int i = 0; i < region.nodes.Count; i++)
                {
                    centre += new Vector2(region.nodes[i].gridX, region.nodes[i].gridY);
                }
                centre = new Vector2 (centre.x / nodesCount, centre.y / nodesCount);
                region.centre = TransformToWorldPos(centre);
            }
        }
        private void OnDrawGizmos()
        {
            if (voronoiRegions == null || debugType == DebugType.Disabled)
                return;

            foreach (VoronoiRegion region in voronoiRegions)
            {
                Gizmos.color = (debugType == DebugType.Fragments) ? region.color : GetColorFromRegionType(region.regionType);
                foreach (Node node in region.nodes)
                {
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (4f - .1f));
                }

                if (debugType == DebugType.Fragments)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(region.centre, 2.5f);

                    foreach (VoronoiRegion neighbour in region.neighbourRegions)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(region.centre, neighbour.centre);
                    }
                }
            }
        }
        
        private Vector3 TransformToWorldPos(Vector2 voronoiPos)
        {
            Vector3 worldPoint = worldBottomLeft + Vector3.right * (voronoiPos.x * 4 + 2) + Vector3.forward * (voronoiPos.y * 4 + 2);
            return worldPoint;
        }
        public List<VoronoiRegion> GetVoronoiRegions()
        {
            return voronoiRegions;
        }

        private Color GetColorFromRegionType(Region regionType)
        {
            switch (regionType)
            {
                case Region.Residential:
                    return Color.yellow;
                case Region.Suburbs:
                    return Color.red;
                case Region.Main:
                    return Color.green;
                default:
                    return Color.black;
            }
        }

    }

    public class VoronoiRegion
    {
        public Color color { set; get; }
        public List<Node> nodes = new List<Node>();
        public HashSet<VoronoiRegion> neighbourRegions = new HashSet<VoronoiRegion>();
        public Vector2 point;
        public Region regionType = Region.Residential;
        public bool addedToDistrict { set; get; }
        public Vector3 centre = Vector3.zero;
        public int id;

        public VoronoiRegion(Color _color, Vector2 _point, int _id)
        {
            color = _color;
            point = _point;
            id = _id;
        }
    }

    enum DebugType
    {
        Disabled,
        Fragments,
        Region
    }
}

