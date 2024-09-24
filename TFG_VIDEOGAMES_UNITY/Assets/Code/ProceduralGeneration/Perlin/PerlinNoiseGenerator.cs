using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class PerlinNoiseGenerator : MonoBehaviour
    {
        private int width;
        private int height;
        private float noiseScale = 1f;
        private Vector2 perlinOffset = new Vector2(0, 0);
        public int perlinGridStepSizeX = 4;
        public int perlinGridStepSizeY = 4;
        Texture2D perlinTexture;
        public static PerlinNoiseGenerator Instance;
        public void GenerateNoise(int width, int height)
        {
            this.width = width;
            this.height = height;

            perlinTexture = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    perlinTexture.SetPixel(x, y, SampleNoise(x, y));
                }
            }
            perlinTexture.Apply();
        }
        private Color SampleNoise(int x, int y)
        {
            float xPos = (float)x / width * noiseScale + perlinOffset.x;
            float yPos = (float)y / height * noiseScale + perlinOffset.y;

            float sample = Mathf.PerlinNoise(xPos, yPos);
            Color perlinColor = new Color(sample, sample, sample);

            return perlinColor;
        }
        public float SampleStepped(int x, int y)
        {
            int gridStepSizeX = width / perlinGridStepSizeX;
            int gridStepSizeY = height / perlinGridStepSizeY;

            float sampledFloat = perlinTexture.GetPixel
                       ((Mathf.FloorToInt(x * gridStepSizeX)), (Mathf.FloorToInt(y * gridStepSizeX))).grayscale;

            return sampledFloat;
        }
        public float PerlinSteppedPosition(Vector3 worldPosition)
        {
            int xToSample = Mathf.FloorToInt(worldPosition.x + perlinGridStepSizeX * .5f);
            int yToSample = Mathf.FloorToInt(worldPosition.z + perlinGridStepSizeY * .5f);

            xToSample = xToSample % perlinGridStepSizeX;
            yToSample = yToSample % perlinGridStepSizeY;

            float sampledValue = SampleStepped(xToSample, yToSample);

            return sampledValue;
        }
        private void VisualizeGrid()
        {
            GameObject visualizationParent = new GameObject("VisualizationParent");
            visualizationParent.transform.SetParent(this.transform);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //GameObject clone = Instantiate(visualizationCube,
                    //    new Vector3(x, SampleStepped(x, y) * visualizationHeightScale, y)
                    //    + transform.position, transform.rotation);

                    //clone.transform.SetParent(visualizationParent.transform);
                }
            }

            //visualizationParent.transform.position =
            //    new Vector3(-perlinGridStepSizeX * .5f, -visualizationHeightScale * .5f, -perlinGridStepSizeY * .5f);
        }
    }
}
