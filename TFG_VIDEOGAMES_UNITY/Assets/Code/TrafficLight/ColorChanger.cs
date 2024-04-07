using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    [SerializeField] private Material black;
    Material green, amber, red;

    private Renderer debugSphereRenderer;
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        green = meshRenderer.materials[0];
        amber = meshRenderer.materials[1];
        red = meshRenderer.materials[2];
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = transform.parent;
        sphere.transform.position = transform.position + Vector3.up * 5f;
        debugSphereRenderer = sphere.GetComponent<Renderer>();
    }

    public void SetColor(TrafficLightState newColor)
    {
        switch (newColor)
        {
            case TrafficLightState.Green:
                Material[] greenMats = { black, green, black};
                meshRenderer.materials = greenMats;
                debugSphereRenderer.material = green;
                break;
            case TrafficLightState.Amber:
                Material[] amberMats = { amber, black, black };
                meshRenderer.materials = amberMats;
                debugSphereRenderer.material = amber;
                break;
            case TrafficLightState.Red:
                Material[] redMats = { black, black, red };
                meshRenderer.materials = redMats;
                debugSphereRenderer.material = red;
                break;
        }
    }
  
}
