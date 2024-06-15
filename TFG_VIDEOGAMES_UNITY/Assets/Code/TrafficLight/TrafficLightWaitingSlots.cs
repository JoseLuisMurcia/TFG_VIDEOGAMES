using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightWaitingSlots
{
    private int numLanes = 2;
    private float verticalOffset = .8f;
    private int slotsPerLane = 5;

    private List<List<Slot>> slots;
    private Vector3 forward;
    private Vector3 right;
    public TrafficLightWaitingSlots(Vector3 upperLeft, Vector3 upperRight, Vector3 _forward, Vector3 _right)
    {
        this.forward = _forward;
        this.right = _right;
        float length = Vector3.Distance(upperLeft, upperRight);
        float horizontalOffset = length / (slotsPerLane - 1);
        float verticalInitialOffset = .5f;
        slots = new List<List<Slot>>();
        for (int i = 0; i < numLanes; i++)
        {
            slots.Add(new List<Slot>());
            for (int j = 0; j < slotsPerLane; j++)
            {
                Vector3 slotPosition = new Vector3(
                    upperLeft.x + right.x * j * horizontalOffset, 
                    upperLeft.y, 
                    upperLeft.z - forward.z * verticalInitialOffset - forward.z * i * verticalOffset);
                slots[i].Add(new Slot(slotPosition));
            }
        }
    }

    public bool AddLane()
    {
        return true;
    }

    public bool RemoveLane()
    {
        return false;
    }
    public List<List<Slot>> GetSlots()
    {
        return slots;
    }
}

public class Slot
{
    public Vector3 position = Vector3.zero;

    public Slot(Vector3 _position)
    {
        this.position = _position;
    }
}
