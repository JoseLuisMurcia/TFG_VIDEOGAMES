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

    [SerializeField] float greenTime = 1f;
    [SerializeField] float amberTime = 1f;
    [SerializeField] float redTime = 1f;
    [SerializeField] float pedestrianTime = 1f;
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
                PedestrianTrafficLightTrigger ptft = child.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
                if (tf != null)
                {
                    trafficLights.Add(tf);
                    waitingQueue.Enqueue(tf);
                }
                if (ptf != null)
                {
                    pedestrianTrafficLights.Add(ptf);
                }
                if (ptft != null)
                {
                    ptft.SetScheduler(this);
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
                    // Start animation
                    float totalTime = 10f;
                    float firstBlinkInterval = 1f;
                    float secondBlinkInterval = .6f;
                    float blinkInterval = firstBlinkInterval;
                    float secondIntervalStartTime = 5f;
                    float currentTime = 0f;
                    bool isBlack = false;
                    while (currentTime < totalTime)
                    {
                        // Update time
                        currentTime += blinkInterval;
                        // Check if we are in the last 5 seconds
                        if (currentTime >= totalTime - secondIntervalStartTime)
                        {
                            // Accelerate blinking
                            blinkInterval = secondBlinkInterval;
                        }
                        // Blink green light
                        SetColorPedestrians(isBlack ? TrafficLightState.Pedestrian : TrafficLightState.Black);
                        isBlack = !isBlack;
                        // Wait for blink interval
                        yield return new WaitForSeconds(blinkInterval);
                    }
                    // End animation
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
    
    private void SetColorPedestrians(TrafficLightState color)
    {
        foreach (PedestrianTrafficLight pedestrianTrafficLight in pedestrianTrafficLights)
        {
            pedestrianTrafficLight.colorChanger.SetColor(color);
        }
    }

    public TrafficLightState GetState()
    {
        return currentState;
    }
}
