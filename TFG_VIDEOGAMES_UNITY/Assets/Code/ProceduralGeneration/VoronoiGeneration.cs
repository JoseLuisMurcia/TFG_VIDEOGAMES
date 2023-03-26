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

        public int regionColorAmount;

        [SerializeField]
        List<Color> myRegionColors = new List<Color>();
        List<VoronoiRegion> voronoiRegions;
        Vector2[] points;
        Color[] regionColors;
        Color[] pixelColors;
        [HideInInspector]
        public Vector3 worldBottomLeft;


        [SerializeField]
        private DebugType debugType;

        public void Start()
        {
            //CreateVoronoiDiagram();
            //CreateVoronoiDiagramWPerlinNoise();
        }
        public void SetupVoronoi(int gridXSize)
        {
            size = gridXSize;
            points = new Vector2[regionAmount];
            regionColors = new Color[regionColorAmount];
            pixelColors = new Color[size * size];

            voronoiRegions = new List<VoronoiRegion>(size);
            for (int i = 0; i < regionAmount; i++)
            {
                points[i] = new Vector2(Random.Range(0, size), Random.Range(0, size));
            }

            for (int i = 0; i < regionColorAmount; i++)
            {
                regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
                voronoiRegions.Add(new VoronoiRegion(regionColors[i], points[i], i));
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
            int regionId = value % regionColorAmount;
            Color currentColor = regionColors[regionId];
            pixelColors[x + y * size] = currentColor;
            voronoiRegions[regionId].nodes.Add(node);
            node.voronoiRegion = voronoiRegions[regionId];
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
        private void OnDrawGizmos()
        {
            if (voronoiRegions == null || debugType == DebugType.None)
                return;

            int i = 0;
            foreach (VoronoiRegion region in voronoiRegions)
            {
                Gizmos.color = debugType == DebugType.Fragments ? region.color : GetColorFromRegionType(region.regionType);
                foreach (Node node in region.nodes)
                {
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (4f - .1f));
                }
                
                if(debugType == DebugType.Fragments)
                {
                    Vector3 point = TransformToWorldPos(points[i]);
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(point, 2.5f);

                    foreach (VoronoiRegion neighbour in region.neighbourRegions)
                    {
                        Vector2 neighbourPoint = neighbour.point;
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(point, TransformToWorldPos(neighbourPoint));
                    }
                }               
                
                i++;
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
        public void CreateVoronoiDiagram()
        {
            size = 512;
            regionAmount = 64;
            regionColorAmount = 64;

            pixelColors = new Color[size * size];
            points = new Vector2[regionAmount];
            regionColors = new Color[regionColorAmount];

            voronoiRegions = new List<VoronoiRegion>(size);
            for (int i = 0; i < regionAmount; i++)
            {
                points[i] = new Vector2(Random.Range(0, size), Random.Range(0, size));
            }

            for (int i = 0; i < regionColorAmount; i++)
            {
                regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
                voronoiRegions.Add(new VoronoiRegion(regionColors[i], points[i], i));
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
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
                    int regionId = value % regionColorAmount;
                    pixelColors[x + y * size] = regionColors[regionId];
                }
            }

            Texture2D myTexture = new Texture2D(size, size);
            myTexture.SetPixels(pixelColors);
            myTexture.Apply();

            GetComponent<Renderer>().material.mainTexture = myTexture;
        }

        public void CreateVoronoiDiagramWPerlinNoise()
        {
            Vector2[] points = new Vector2[regionAmount];
            Color[] regionColors = new Color[regionColorAmount];

            for (int i = 0; i < regionAmount; i++)
            {
                points[i] = new Vector2(Random.Range(0, size), Random.Range(0, size));
            }

            for (int i = 0; i < regionColorAmount; i++)
            {
                regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
            }

            for (int i = 0; i < regionColorAmount; i++)
            {
                regionColors[i] = myRegionColors[i];
            }

            Color[] pixelColors = Generation.GenerateVoronoi(size, regionAmount, regionColorAmount, points, regionColors);

            Texture2D myTexture = new Texture2D(size, size);
            myTexture.filterMode = FilterMode.Point;
            myTexture.SetPixels(pixelColors);
            myTexture.Apply();

            GetComponent<Renderer>().material.mainTexture = myTexture;
        }
    }
    public class VoronoiRegion
    {
        public Color color { set; get; }
        public List<Node> nodes = new List<Node>();
        public HashSet<VoronoiRegion> neighbourRegions = new HashSet<VoronoiRegion>();
        public Vector2 point;
        public Region regionType = Region.Residential;
        public int id;

        public VoronoiRegion(Color _color, Vector2 _point, int _id)
        {
            color = _color;
            point = _point;
            id = _id;
        }
    }

    public static class Generation
    {
        public static float GenerateBiomeNoise(int x, int y, float scale, int octaves, float persistance, float lacunarity)
        {
            float perlinValue = new float();

            float ampitude = 1;
            float frequency = 1;
            float noiseHeight = 1;

            for (int i = 0; i < octaves; i++)
            {
                float posX = x / scale * frequency;
                float posY = y / scale * frequency;

                perlinValue = Mathf.PerlinNoise(posX, posY) * 2 - 1;
                noiseHeight += perlinValue * ampitude;
                ampitude *= persistance;
                frequency *= lacunarity;
            }

            perlinValue = noiseHeight;
            perlinValue = Mathf.InverseLerp(-0.5f, 2f, perlinValue);

            return perlinValue;
        }

        public static Color[] GenerateVoronoi(int size, int regionAmount, int regionColorAmount, Vector2[] points, Color[] regionColors)
        {
            Color[] pixelColors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = float.MaxValue;
                    int value = 0;
                    float perlinValue = Generation.GenerateBiomeNoise(x, y, 12, 4, 0.1f, 2.2f);

                    for (int i = 0; i < regionAmount; i++)
                    {
                        if (Vector2.Distance(new Vector2(x, y), points[i]) < distance)
                        {
                            distance = Vector2.Distance(new Vector2(x, y), points[i]);
                            value = i;
                        }
                    }

                    int closestRegionIndex = 0;
                    float distanceRegion = float.MaxValue;
                    for (int i = 0; i < regionAmount; i++)
                    {
                        if (i != value)
                        {
                            if (Vector2.Distance(new Vector2(x, y), points[i]) < distanceRegion)
                            {
                                distanceRegion = Vector2.Distance(new Vector2(x, y), points[i]);
                                closestRegionIndex = i;
                            }
                        }
                    }

                    if (distanceRegion - distance < value)
                    {
                        if (perlinValue < 0.5f)
                            pixelColors[x + y * size] = regionColors[value % regionColorAmount];
                        else
                            pixelColors[x + y * size] = regionColors[closestRegionIndex % regionColorAmount];
                    }
                    else
                    {
                        pixelColors[x + y * size] = regionColors[value % regionColorAmount];
                    }
                }
            }

            for (int i = 0; i < regionAmount * (int)(size / 256); i++)
            {
                for (int amount = 1; amount <= 2; amount++)
                {
                    for (int y = amount; y < size - amount; y++)
                    {
                        for (int x = amount; x < size - amount; x++)
                        {
                            if ((pixelColors[(x + amount) + y * size] != pixelColors[x + y * size] && pixelColors[(x - amount) + y * size] != pixelColors[x + y * size]) ||
                            (pixelColors[x + (y + amount) * size] != pixelColors[x + y * size] && pixelColors[x + (y - amount) * size] != pixelColors[x + y * size]))
                            {
                                pixelColors[x + y * size] = pixelColors[(x + amount) + y * size];
                                if (i == regionAmount * (int)(size / 256) - 1 && amount == 1)
                                    pixelColors[x + y * size] = pixelColors[x + (y + amount) * size];
                            }
                        }
                    }
                }
            }


            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    if ((pixelColors[(x + 1) + y * size] != pixelColors[x + y * size] && pixelColors[(x - 1) + y * size] != pixelColors[x + y * size]) &&
                    (pixelColors[x + (y + 1) * size] != pixelColors[x + y * size] && pixelColors[x + (y - 1) * size] != pixelColors[x + y * size]))
                    {
                        pixelColors[x + y * size] = pixelColors[(x + 1) + y * size];

                    }
                }
            }

            return pixelColors;
        }
    }

    enum DebugType
    {
        None,
        Fragments,
        Region
    }
}
