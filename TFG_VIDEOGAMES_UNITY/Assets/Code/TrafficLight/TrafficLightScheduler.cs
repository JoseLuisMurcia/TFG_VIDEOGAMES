using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightScheduler : MonoBehaviour
{
    private List<CarTrafficLight> trafficLights = new List<CarTrafficLight>();
    private List<PedestrianTrafficLight> pedestrianTrafficLights = new List<PedestrianTrafficLight>();
    private Queue<TrafficLight> waitingQueue = new Queue<TrafficLight>();
    private TrafficLight currentTrafficLight;
    private TrafficLightState currentState;

    [SerializeField] float greenTime = 2f;
    [SerializeField] float amberTime = 1f;
    [SerializeField] float redTime = 3f;
    [SerializeField] float pedestrianTime = 5f;
    [SerializeField] float transitionTime = 1f;

    private void Start()
    {
        // By default this executes earlier than the TrafficLights so it needs to be delayed so that they can find their road first.
        StartCoroutine(StartRoutine());
    }
    IEnumerator StartRoutine()
    {
        yield return new WaitForSeconds(1f);
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                CarTrafficLight tf = child.gameObject.GetComponent<CarTrafficLight>();
                PedestrianTrafficLight ptf = child.gameObject.GetComponent<PedestrianTrafficLight>();
                if (tf != null)
                {
                    trafficLights.Add(tf);
                    waitingQueue.Enqueue(tf);
                }
                if (ptf != null)
                {
                    pedestrianTrafficLights.Add(ptf);
                }
            }
        }
        // Enqueue one pedestrianTrafficLight so that it signals the pedestrians turn
        if (pedestrianTrafficLights.Count > 0)
        {
            waitingQueue.Enqueue(pedestrianTrafficLights.First());
        }
        if (trafficLights.Count > 0)
        {
            currentTrafficLight = waitingQueue.Dequeue();
            currentState = Random.Range(0, 2) == 0 ? TrafficLightState.Green : TrafficLightState.Red;
            SetNewColor(currentState);
            StartCoroutine(HandleTrafficLights());
        }
    }
    // Rehacer este metodo para que de amber no se meta en red y se ponga a esperar
    // El problema es que al pasar de amber a red, se llama dos veces a yield return, una en setNewActive y otra al entrar por el case Red.
    // Deshacer metodo SetNewActiveTrafficLight y meterlo dentro de amber de forma que el yield esté dentro.
    IEnumerator HandleTrafficLights()
    {
        while (true)
        {
            switch (currentState)
            {
                case TrafficLightState.Green:
                    yield return new WaitForSeconds(greenTime);
                    currentState = TrafficLightState.Amber;
                    SetNewColor(currentState);
                    break;

                case TrafficLightState.Amber:
                    yield return new WaitForSeconds(amberTime);
                    // Se pone en rojo el que se tiene que poner
                    SetNewColor(TrafficLightState.Red);
                    // Ponemos en verde el nuevo semaforo o damos paso a peatones
                    waitingQueue.Enqueue(currentTrafficLight);
                    currentTrafficLight = waitingQueue.Dequeue();
                    yield return new WaitForSeconds(transitionTime);

                    if (currentTrafficLight is PedestrianTrafficLight)
                    {
                        // Turn for pedestrians
                        currentState = TrafficLightState.Pedestrian;
                        SetNewColorPedestrians(TrafficLightState.Pedestrian);
                    }
                    else
                    {
                        currentState = TrafficLightState.Green;
                        SetNewColor(TrafficLightState.Green);
                    }
                    break; 

                case TrafficLightState.Red:
                    yield return new WaitForSeconds(redTime);
                    currentState = TrafficLightState.Green;
                    SetNewColor(currentState);
                    break;

                case TrafficLightState.Pedestrian:
                    yield return new WaitForSeconds(pedestrianTime);
                    SetNewColorPedestrians(TrafficLightState.Red);

                    waitingQueue.Enqueue(currentTrafficLight);
                    currentTrafficLight = waitingQueue.Dequeue();
                    yield return new WaitForSeconds(2f);
                    currentState = TrafficLightState.Green;
                    SetNewColor(TrafficLightState.Green);
                    break;

            }
        }
    }

    IEnumerator SetNewActiveTrafficLight()
    {
        waitingQueue.Enqueue(currentTrafficLight);
        currentTrafficLight = waitingQueue.Dequeue();
        yield return new WaitForSeconds(transitionTime);
        if (currentTrafficLight is PedestrianTrafficLight)
        {
            // Turn for pedestrians
            currentState = TrafficLightState.Pedestrian;
            SetNewColorPedestrians(TrafficLightState.Pedestrian);
        }
        else
        {
            currentState = TrafficLightState.Green;
            SetNewColor(currentState);
        }
    }

    private void SetNewColor(TrafficLightState color)
    {
        CarTrafficLight currentCarTrafficLight = currentTrafficLight as CarTrafficLight;
        currentCarTrafficLight.currentColor = color;
        currentCarTrafficLight.colorChanger.SetColor(color);
        currentCarTrafficLight.road.trafficLightEvents.LightChange(color, false);
    }

    private void SetNewColorPedestrians(TrafficLightState color)
    {
        foreach (PedestrianTrafficLight pedestrianTrafficLight in pedestrianTrafficLights)
        {
            pedestrianTrafficLight.currentColor = color;
            pedestrianTrafficLight.colorChanger.SetColor(color);
        }
    }
}
