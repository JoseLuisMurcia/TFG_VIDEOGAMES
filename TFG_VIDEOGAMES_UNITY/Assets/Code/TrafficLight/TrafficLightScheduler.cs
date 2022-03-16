using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightScheduler : MonoBehaviour
{
    private List<TrafficLight> trafficLights = new List<TrafficLight>();
    private Queue<TrafficLight> waitingQueue = new Queue<TrafficLight>();
    private TrafficLight currentTrafficLight;
    private TrafficLightColor currentColor;

    [SerializeField] float greenTime = 2f;
    [SerializeField] float amberTime = 1f;
    [SerializeField] float redTime = 3f;

    private void Start()
    {
        foreach(Transform child in transform)
        {
            TrafficLight tf = child.gameObject.GetComponent<TrafficLight>();
            if (tf != null)
            {
                trafficLights.Add(tf);
                waitingQueue.Enqueue(tf);
            }
        }

        if(trafficLights.Count > 0)
        {
            currentTrafficLight = waitingQueue.Dequeue();
            currentColor = TrafficLightColor.Green;
            SetNewColor(currentColor);
        }
        StartCoroutine(HandleTrafficLights());
    }

    IEnumerator HandleTrafficLights()
    {
        while (true)
        {
            switch (currentColor)
            {
                case TrafficLightColor.Green:
                    yield return new WaitForSeconds(greenTime);
                    currentColor = TrafficLightColor.Amber;
                    SetNewColor(currentColor);
                    break;

                case TrafficLightColor.Amber:
                    yield return new WaitForSeconds(amberTime);
                    // Se pone en rojo el que se tiene que poner
                    SetNewColor(TrafficLightColor.Red);
                    // Ponemos en verde el nuevo semaforo
                    SetNewActiveTrafficLight();
                    break; 

                case TrafficLightColor.Red:
                    yield return new WaitForSeconds(redTime);
                    currentColor = TrafficLightColor.Green;
                    SetNewColor(currentColor);
                    break;
            }
        }
    }

    private void SetNewActiveTrafficLight()
    {
        waitingQueue.Enqueue(currentTrafficLight);
        currentTrafficLight = waitingQueue.Dequeue();
        currentColor = TrafficLightColor.Green;
        SetNewColor(currentColor);
    }

    private void SetNewColor(TrafficLightColor color)
    {
        currentTrafficLight.road.trafficLightEvents.LightChange(color);
        currentTrafficLight.colorChanger.SetColor(color);
    }
}