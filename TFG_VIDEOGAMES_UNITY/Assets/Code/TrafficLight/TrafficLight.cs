using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [HideInInspector] public TrafficLightState currentColor = TrafficLightState.Red;

}
// Colors only reference car traffic lights, only one state for pedestrians as it's obviously green if active and red in any other state
public enum TrafficLightState
{
    Green,
    Amber,
    Red,
    Pedestrian,
    Black
}
