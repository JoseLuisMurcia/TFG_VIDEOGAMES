using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    [SerializeField] public List<Direction> directions = new List<Direction>();
    public TypeOfRoad typeOfRoad = TypeOfRoad.None;
    public TrafficLight trafficLight;
    public TrafficLightEvents trafficLightEvents;

    private void Start()
    {
        trafficLightEvents = GetComponent<TrafficLightEvents>();
        SetTypeOfRoad();
    }

    private void SetTypeOfRoad()
    {
        if (directions.Count == 1)
        {
            // Important, the order of enums must be the same in order for the cast to work properly
            typeOfRoad = (TypeOfRoad)directions[0];
        }
        else
        {
            if (directions.Contains(Direction.Up))
            {
                if (directions.Contains(Direction.Left))
                {
                    typeOfRoad = TypeOfRoad.UpToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
            }
            else if (directions.Contains(Direction.Down))
            {
                if (directions.Contains(Direction.Left))
                {
                    typeOfRoad = TypeOfRoad.DownToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.DownToRight;
                }
            }
            else if (directions.Contains(Direction.Right))
            {
                if (directions.Contains(Direction.Up))
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.DownToRight;
                }
            }
            else if (directions.Contains(Direction.Left))
            {
                if (directions.Contains(Direction.Up))
                {
                    typeOfRoad = TypeOfRoad.UpToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
            }
        }
    }
}

public enum TypeOfRoad
{
    None = -1,
    Right,
    Up, 
    Down,
    Left,
    DownToLeft,
    DownToRight,
    UpToRight,
    UpToLeft
}

public enum Direction
{
    None = -1,
    Right,
    Up,
    Down,
    Left
}
