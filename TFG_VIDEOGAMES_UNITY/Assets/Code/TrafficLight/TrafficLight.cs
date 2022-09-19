using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField] LayerMask roadMask;
    [HideInInspector] public TrafficLightColor currentColor = TrafficLightColor.Red;
    private Vector3 rayPos;
    [HideInInspector] public Road road;

    [HideInInspector] public ColorChanger colorChanger;

    private void Awake()
    {
        colorChanger = GetComponentInChildren<ColorChanger>();
        FindRoad();
    }

    void Start()
    {
        colorChanger.SetColor(currentColor);
    }


    public void FindRoad()
    {
        float forwardDistance = 1f;
        float rightDistance = 1f;
        rayPos = transform.forward* forwardDistance + transform.right*rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.blue, 50f);
            GameObject roadGameObject = hit.collider.gameObject;
            road = roadGameObject.GetComponent<Road>();
            if(road != null)
            {
                road.trafficLight = this;
                road.CreateTrafficLightTriggers();
            }
            else
            {
                //Debug.LogWarning("Road found null by Traffic Light: " + gameObject.name);
                gameObject.SetActive(false);
                //Destroy(gameObject);
            }
        }
        else
        {
            //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.magenta, 50f);
            //Debug.LogWarning("Road not found by Traffic Light: " + gameObject.name);
            gameObject.SetActive(false);
            //Destroy(gameObject);
        }
    }

}

public enum TrafficLightColor
{
    Green,
    Amber,
    Red
}
