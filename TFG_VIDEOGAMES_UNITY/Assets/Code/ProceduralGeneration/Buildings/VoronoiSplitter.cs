using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PG
{
    public class VoronoiSplitter
    {
        [SerializeField]
        private int regionAmount;
        private int minX;
        private int maxX;
        private int minY;
        private int maxY;

        List<VoronoiRegion> voronoiRegions;
        List<GridNode> group;
        private bool firstTime = true;
        Vector2[] points;
        Vector2[] centres;
        [HideInInspector]

        [SerializeField]
        private bool lloydRelaxation;
        [SerializeField]
        private int relaxationIterations;

        private int numRelaxationIterations = 0;

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
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
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
        public void SetupVoronoi(List<GridNode> group)
        {
            int nodesPerRegion = Random.Range(16, 20);
            this.group = group;
            minX = group.Min(n => n.gridX);
            maxX = group.Max(n => n.gridX);
            minY = group.Min(n => n.gridY);
            maxY = group.Max(n => n.gridY);
            firstTime = true;
            regionAmount = Mathf.CeilToInt((float)group.Count / nodesPerRegion); points = new Vector2[regionAmount];

            voronoiRegions = new List<VoronoiRegion>(regionAmount);
            for (int i = 0; i < regionAmount; i++)
            {
                points[i] = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                Color nodeColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
                voronoiRegions.Add(new VoronoiRegion(nodeColor, points[i], i));
            }
        }
        public void SetRegions()
        {
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    float distance = float.MaxValue;
                    int value = 0;

                    Vector2 currentPos = new Vector2(x, y);
                    GridNode node = Grid.Instance.nodesGrid[x, y];
                    if (node.usage != Usage.building || !group.Contains(node))
                        continue;

                    for (int i = 0; i < voronoiRegions.Count; i++)
                    {
                        if (Vector2.Distance(currentPos, points[i]) < distance)
                        {
                            distance = Vector2.Distance(currentPos, points[i]);
                            value = i;
                        }
                    }
                    int regionId = value % voronoiRegions.Count;                 
                    voronoiRegions[regionId].nodes.Add(node);
                    node.voronoiRegion = voronoiRegions[regionId];
                }
            }
        }
        public void Cleanup()
        {
            List<VoronoiRegion> regionsToRemove = new List<VoronoiRegion>();
            foreach (var region in voronoiRegions)
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
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    GridNode node = Grid.Instance.nodesGrid[x, y];
                    if (node.usage != Usage.building || !group.Contains(node))
                        continue;

                    List<GridNode> neighbours = Grid.Instance.GetNeighboursInLine(node, new List<Usage>() { Usage.building });
                    foreach (GridNode neighbour in neighbours)
                    {
                        if (node.voronoiRegion.id != neighbour.voronoiRegion.id)
                        {
                            VoronoiRegion currentRegion = node.voronoiRegion;
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
            for (int i = 0; i < voronoiRegions.Count; i++)
            {
                var region = voronoiRegions[i];
                Vector2 centre = Vector2.zero;
                for (int j = 0; j < region.nodes.Count; j++)
                {
                    centre += new Vector2(region.nodes[j].gridX, region.nodes[j].gridY);
                }
                float nodesCount = (float)region.nodes.Count;
                centre = new Vector2(centre.x / nodesCount, centre.y / nodesCount);
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
        public void OnDraw()
        {
            if (voronoiRegions == null)
                return;

            foreach (VoronoiRegion region in voronoiRegions)
            {
                foreach (GridNode node in region.nodes)
                {
                    SpawnSphere(node.worldPosition, region.color, 1.5f, 4f);
                }
            }
        }
        private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
        {
            GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startSphere.transform.localScale = Vector3.one * size;
            startSphere.transform.position = pos + Vector3.up * 3f * offset;
            startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        }
        private Vector3 TransformToWorldPos(Vector2 voronoiPos)
        {
            Vector3 worldPoint = Grid.Instance.worldBottomLeft + Vector3.right * (voronoiPos.x * Grid.Instance.nodeDiameter + Grid.Instance.nodeRadius) + Vector3.forward * (voronoiPos.y * Grid.Instance.nodeDiameter + Grid.Instance.nodeRadius);
            return worldPoint;
        }
        public List<VoronoiRegion> GetVoronoiRegions()
        {
            return voronoiRegions;
        }

    }
}
