using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    [SerializeField] private Material black;
    Material green, amber, red;
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        green = meshRenderer.materials[0];
        amber = meshRenderer.materials[1];
        red = meshRenderer.materials[2];
    }

    public void SetColor(TrafficLightColor newColor)
    {
        switch (newColor)
        {
            case TrafficLightColor.Green:
                Material[] greenMats = { black, green, black};
                meshRenderer.materials = greenMats;
                break;
            case TrafficLightColor.Amber:
                Material[] amberMats = { amber, black, black };
                meshRenderer.materials = amberMats;
                break;
            case TrafficLightColor.Red:
                Material[] redMats = { black, black, red };
                meshRenderer.materials = redMats;
                break;
        }
    }
  
}
