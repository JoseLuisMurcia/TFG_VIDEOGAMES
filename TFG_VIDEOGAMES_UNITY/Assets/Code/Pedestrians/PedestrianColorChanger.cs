using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianColorChanger : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    Material black, metallicBlack, green, red;
    private Renderer debugSphereRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        black = meshRenderer.materials[0];
        metallicBlack = meshRenderer.materials[1];
        red = meshRenderer.materials[2];
        green = meshRenderer.materials[3];
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = transform.parent;
        sphere.transform.position = transform.position + Vector3.up * 5f;
        debugSphereRenderer = sphere.GetComponent<Renderer>();
    }

    public void SetColor(TrafficLightState newColor)
    {
        switch (newColor)
        {
            case TrafficLightState.Pedestrian:
                Material[] greenMats = { black, metallicBlack, black, green };
                meshRenderer.materials = greenMats;
                debugSphereRenderer.material = green;
                break;
            case TrafficLightState.Red:
                Material[] redMats = { black, metallicBlack, red, black };
                meshRenderer.materials = redMats;
                debugSphereRenderer.material = red;
                break;
            case TrafficLightState.Black:
                Material[] blackMats = { black, metallicBlack, black, black };
                meshRenderer.materials = blackMats;
                debugSphereRenderer.material = black;
                break;
        }
    }

}
