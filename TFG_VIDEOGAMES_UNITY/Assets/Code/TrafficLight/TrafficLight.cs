using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// RECUERDA QUE POR ALGUN MOTIVO, SOLO ESTÁ FUNCIONANDO CON EL ULTIMO SEMÁFORO AÑADIDO EN LA LISTA
// PUTA MADRE QUE SE ESTÁ SOBREESCRIBIENDO AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
public class TrafficLight : MonoBehaviour
{
    [SerializeField] Vector2 wireCubeSize;
    BoxCollider stopPointsCollider;
    public TrafficLightColor currentColor;
    public List<Node> nodesToStop = new List<Node>();
    void Awake()
    {
        stopPointsCollider = GetComponent<BoxCollider>();
        float forwardDistance = 9f;
        float rightDistance = 7.3f;
        stopPointsCollider.center = (Vector3.back * forwardDistance) + (Vector3.left * rightDistance) + transform.position;
        stopPointsCollider.size = new Vector3(wireCubeSize.x, 1, wireCubeSize.y)*10f;
        currentColor = TrafficLightColor.Red;
    }


    public bool IsInBounds(Vector3 nodePos)
    {
        return stopPointsCollider.bounds.Contains(nodePos);
    }
}

public enum TrafficLightColor
{
    Green,
    Amber,
    Red
}
