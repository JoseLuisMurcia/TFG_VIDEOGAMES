using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGeneration : MonoBehaviour
{
    public int size;

    public int regionAmount;

    public int regionColorAmount;

    public void Start()
    {
        CreateVoronoiDiagram();
    }
    public void CreateVoronoiDiagram()
    {
        Vector2[] points = new Vector2[regionAmount];
        Color[] regionColors = new Color[regionColorAmount];

        for(int i=0; i< regionAmount; i++)
        {
            points[i] = new Vector2(Random.Range(0, size), Random.Range(0, size));
        }

        for(int i=0; i < regionColorAmount; i++)
        {
            regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
        }

        Color[] pixelColors = new Color[size * size];

        for(int y=0; y < size; y++)
        {
            for(int x=0; x < size; x++)
            {
                float distance = float.MaxValue;
                int value = 0;

                for(int i=0; i<regionAmount; i++)
                {
                    if(Vector2.Distance(new Vector2(x, y), points[i]) < distance)
                    {
                        distance = Vector2.Distance(new Vector2(x, y), points[i]);
                        value = i;
                    }
                }

                pixelColors[x + y * size] = regionColors[value % regionColorAmount];
            }
        }

        Texture2D myTexture = new Texture2D(size, size);
        myTexture.SetPixels(pixelColors);
        myTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = myTexture;
    }
}
