using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField] LayerMask roadMask;
    public TrafficLightColor currentColor;
    private Vector3 rayPos;
    void Start()
    {
        currentColor = TrafficLightColor.Red;
        FindRoad();
    }


    public void FindRoad()
    {
        float forwardDistance = 0.9f;
        float rightDistance = 0.9f;
        //rayPos = (-Vector3.back * forwardDistance) + (-Vector3.left * rightDistance) + transform.position;
        rayPos = -transform.forward* forwardDistance + -transform.right*rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            Debug.DrawLine(ray.origin, hit.point);
            GameObject roadGameObject = hit.collider.gameObject;
            Road road = roadGameObject.GetComponent<Road>();
            if(road != null)
            {
                road.trafficLight = this;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(rayPos, .2f);
        Gizmos.color = Color.magenta;
    }
}

public enum TrafficLightColor
{
    Green,
    Amber,
    Red
}
