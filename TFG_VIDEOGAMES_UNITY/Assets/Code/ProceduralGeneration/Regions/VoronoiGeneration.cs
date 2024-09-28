using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private bool firstTime = true;
        Vector2[] points;
        Vector2[] centres;
        [HideInInspector]
        public Vector3 worldBottomLeft;

        [SerializeField]
        private DebugType debugType;

        [SerializeField]
        private bool lloydRelaxation;
        [SerializeField]
        private int relaxationIterations;

        private int numRelaxationIterations = 0;
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("Relaxation Iteration: " + numRelaxationIterations);
                IterativeVoronoi(numRelaxationIterations);
                numRelaxationIterations++;
            }
        }
        public void LloydRelaxation()
        {
            if (!lloydRelaxation) return;

            for (int k = 0; k < relaxationIterations; k++)
            {
                IterativeVoronoi(k);
            }
        }
        private void IterativeVoronoi(int currentIteration)
        {
            int regionsCount = voronoiRegions.Count;
            points = new Vector2[regionsCount];
            for (int i = 0; i < regionsCount; i++)
            {
                points[i] = centres[i];
                voronoiRegions[i].point = points[i];
                voronoiRegions[i].nodes = new List<GridNode>();
                voronoiRegions[i].neighbourRegions = new HashSet<VoronoiRegion>();
            }
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float distance = float.MaxValue;
                    int value = 0;

                    Vector2 currentPos = new Vector2(x, y);
                    for (int i = 0; i < regionsCount; i++)
                    {
                        if (Vector2.Distance(currentPos, points[i]) < distance)
                        {
                            distance = Vector2.Distance(currentPos, points[i]);
                            value = i;
                        }
                    }
                    int regionId = value % regionsCount;
                    GridNode node = Grid.Instance.nodesGrid[x, y];
                    voronoiRegions[regionId].nodes.Add(node);
                    node.voronoiRegion = voronoiRegions[regionId];
                }
            }
            Cleanup();
            if (!lloydRelaxation)
            {
                SetNeighbourRegions();
            }
            else
            {
                // Avoid setting neighbours until last iteration
                if (currentIteration == relaxationIterations - 1) SetNeighbourRegions();
            }
            SetCentres();     
        }
        public void SetupVoronoi(int gridSize)
        {
            firstTime = true;
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
        public void SetRegions(int x, int y, GridNode node)
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
                    GridNode currentNode = Grid.Instance.nodesGrid[x, y];
                    List<GridNode> neighbours = Grid.Instance.GetNeighboursForVoronoi(currentNode);
                    foreach (GridNode neighbour in neighbours)
                    {
                        if (currentNode.voronoiRegion.id != neighbour.voronoiRegion.id)
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
            centres = new Vector2[voronoiRegions.Count];
            for(int i=0; i < voronoiRegions.Count; i++)
            {
                var region = voronoiRegions[i];
                Vector2 centre = Vector2.zero;
                for (int j = 0; j < region.nodes.Count; j++)
                {
                    centre += new Vector2(region.nodes[j].gridX, region.nodes[j].gridY);
                }
                float nodesCount = (float)region.nodes.Count;
                centre = new Vector2 (centre.x / nodesCount, centre.y / nodesCount);
                centres[i] = centre;
                Vector3 centre3D = TransformToWorldPos(centre) + Vector3.up * 10f;
                region.centre = centre3D;
                if (firstTime)
                {
                    region.originalCentre = centre3D;
                }
            }
            if (firstTime)
            {
                firstTime = false;
            }
        }
        private void OnDrawGizmos()
        {
            if (voronoiRegions == null || debugType == DebugType.Disabled)
                return;

            foreach (VoronoiRegion region in voronoiRegions)
            {
                Gizmos.color = (debugType == DebugType.Fragments) ? region.color : GetColorFromRegionType(region.regionType);
                foreach (GridNode node in region.nodes)
                {
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * 4f);
                }

                if (debugType == DebugType.Fragments)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(region.centre, 2.5f);

                    /* Old centre debug */
                    //Gizmos.color = Color.white;
                    //Gizmos.DrawSphere(region.originalCentre, 2.5f);
                    //Gizmos.color = Color.gray;
                    //Gizmos.DrawLine(region.centre, region.originalCentre);

                    Gizmos.color = Color.white;
                    foreach (VoronoiRegion neighbour in region.neighbourRegions)
                    {
                        Gizmos.DrawLine(region.centre, neighbour.centre);
                    }
                }
            }
        }
        
        private Vector3 TransformToWorldPos(Vector2 voronoiPos)
        {
            Vector3 worldPoint = worldBottomLeft + Vector3.right * (voronoiPos.x * Grid.Instance.nodeDiameter + Grid.Instance.nodeRadius) + Vector3.forward * (voronoiPos.y * Grid.Instance.nodeDiameter + Grid.Instance.nodeRadius);
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
        public List<GridNode> nodes = new List<GridNode>();
        public List<GridNode> nodesContained = new List<GridNode>();
        public HashSet<VoronoiRegion> neighbourRegions = new HashSet<VoronoiRegion>();
        public Vector2 point;
        public Region regionType = Region.Residential;
        public bool addedToDistrict { set; get; }
        public Vector3 centre = Vector3.zero;
        public Vector3 originalCentre = Vector3.zero;
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

