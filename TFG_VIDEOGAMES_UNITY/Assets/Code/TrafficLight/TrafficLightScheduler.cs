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
    [SerializeField] private TrafficLightState currentState;
    private PedestrianIntersectionController pedestrianIntersectionController;

    [SerializeField] float greenTime = 1f;
    [SerializeField] float amberTime = 1f;
    [SerializeField] float redTime = 1f;
    [SerializeField] float pedestrianTime = 1f;
    [SerializeField] float redToGreenTime = 1f;
    [SerializeField] float carsToPedestrianTime = 1f;

    [SerializeField] bool takeTurnsBetweenTrafficLights = false;

    private void Start()
    {
        // By default this executes earlier than the TrafficLights so it needs to be delayed so that they can find their road first.
        StartCoroutine(StartRoutine());
    }
    IEnumerator StartRoutine()
    {
        yield return new WaitForSeconds(1f);
        pedestrianIntersectionController = GetComponent<PedestrianIntersectionController>();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                CarTrafficLight tf = child.gameObject.GetComponent<CarTrafficLight>();
                PedestrianTrafficLight ptf = child.gameObject.GetComponent<PedestrianTrafficLight>();
                if (tf != null)
                {
                    trafficLights.Add(tf);
                    //waitingQueue.Enqueue(tf);
                }
                if (ptf != null)
                {
                    pedestrianTrafficLights.Add(ptf);
                }
            }
        }

        // Enqueue one pedestrianTrafficLight so that it signals the pedestrians turn

        if (takeTurnsBetweenTrafficLights)
        {
            waitingQueue.Enqueue(trafficLights.First());
            waitingQueue.Enqueue(pedestrianTrafficLights.First());
            currentState = TrafficLightState.Cars;
            currentTrafficLight = waitingQueue.Dequeue();
            SetNewColorCars(TrafficLightState.Green);
            StartCoroutine(HandleTwoTurnTrafficLights());
        }
        else if (trafficLights.Count > 0)
        {
            foreach (var tf in trafficLights)
                waitingQueue.Enqueue(tf);

            if(pedestrianTrafficLights.Any())
                waitingQueue.Enqueue(pedestrianTrafficLights.First());

            currentState = TrafficLightState.Red;
            currentTrafficLight = waitingQueue.Dequeue();
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
                    yield return new WaitForSeconds(redToGreenTime);

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
                    float totalTime = pedestrianTime;
                    float firstBlinkInterval = 1f;
                    float secondBlinkInterval = .6f;
                    float blinkInterval = firstBlinkInterval;
                    float secondIntervalStartTime = pedestrianTime * .5f;
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

    // This is the logic for the crossings in which there are only two turns: Pedestrians and Cars
    IEnumerator HandleTwoTurnTrafficLights()
    {
        while (true)
        {
            switch (currentState)
            {
                case TrafficLightState.Cars:
                    yield return new WaitForSeconds(greenTime);
                    currentState = TrafficLightState.Amber;
                    SetNewColorCars(currentState);
                    break;

                case TrafficLightState.Amber:
                    yield return new WaitForSeconds(amberTime);
                    // Se pone en rojo el que se tiene que poner
                    SetNewColorCars(TrafficLightState.Red);
                    // Ponemos en verde el nuevo semaforo o damos paso a peatones
                    waitingQueue.Enqueue(currentTrafficLight);
                    currentTrafficLight = waitingQueue.Dequeue();
                    yield return new WaitForSeconds(redToGreenTime);

                    // Turn for pedestrians
                    currentState = TrafficLightState.Pedestrian;
                    SetNewColorPedestrians(TrafficLightState.Pedestrian);
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
                    currentState = TrafficLightState.Cars;
                    SetNewColorCars(TrafficLightState.Green);
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

    private void SetNewColorCars(TrafficLightState color)
    {
        foreach(var carTrafficLight in trafficLights)
        {
            carTrafficLight.currentColor = color;
            carTrafficLight.colorChanger.SetColor(color);
            carTrafficLight.road.trafficLightEvents.LightChange(color, false);
        }
    }

    private void SetNewColorPedestrians(TrafficLightState color)
    {
        // Launch event to subscribed pedestrians
        pedestrianIntersectionController.ThrowLightChangeEvent(color, true);
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
