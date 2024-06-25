using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TrafficLightWaitingSlotsManager
{
    private int numLanes = 2;
    private float verticalOffset = .8f;
    private int slotsPerLane = 5;

    private List<List<Slot>> slots;
    private Vector3 upperLeft;
    private Vector3 upperRight;
    private Vector3 forward;
    private Vector3 right;
    private float laneLength;
    public TrafficLightWaitingSlotsManager(Vector3 _upperLeft, Vector3 _upperRight, Vector3 _forward, Vector3 _right)
    {
        // Set variables useful for the future
        this.forward = _forward;
        this.right = _right;
        this.upperLeft = _upperLeft;
        this.upperRight = _upperRight;
        this.laneLength = Vector3.Distance(upperLeft, upperRight);

        float horizontalOffset = laneLength / (slotsPerLane - 1);
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
                slots[i].Add(new Slot(slotPosition, i*numLanes + j ));
            }
        }
    }
    public void AddLane()
    {
        float horizontalOffset = laneLength / (slotsPerLane - 1);
        float verticalInitialOffset = .5f;

        slots.Add(new List<Slot>());

        for (int i = 0; i < slotsPerLane; i++)
        {
            Vector3 slotPosition = new Vector3(
                    upperLeft.x + right.x * i * horizontalOffset,
                    upperLeft.y,
                    upperLeft.z - forward.z * verticalInitialOffset - forward.z * (slots.Count-1) * verticalOffset);
            slots[slots.Count-1].Add(new Slot(slotPosition, -1));
        }
        numLanes++;
    }

    public void RemoveLane()
    {
        slots.RemoveAt(numLanes - 1);
        numLanes--;
    }
    public List<List<Slot>> GetSlots()
    {
        return slots;
    }

    public List<Slot> GetSlotsForGroup(int numPedestrians)
    {
        List<Slot> waitingSlots = new List<Slot>();
        for (int i = 0; i < numLanes; i++)
        {
            foreach(var slot in slots[i])
            {
                // TODO: Establecer criterios de asignacion
                if (!slot.isLocked)
                    waitingSlots.Add(slot);

                if (waitingSlots.Count == numPedestrians) 
                {
                    waitingSlots.ForEach(assignedSlot => assignedSlot.isLocked = true);
                    return waitingSlots;
                    //return waitingSlots.Select(slot => slot.position).ToList();
                }
            }
        }
        return null;
    }

    public Slot GetSlotForPedestrian()
    {
        Slot waitingSlot = null;
        for (int i = 0; i < numLanes; i++)
        {
            foreach(var slot in slots[i])
            {
                // TODO: Establecer criterios de asignacion
                if (!slot.isLocked)
                {
                    slot.isLocked = true;
                    return slot;
                }
            }
        }
        return waitingSlot;
    }

    public bool LaneHasSlots(int laneId, int numPedestrians)
    {
        return slots[laneId].FindAll((slot) => !slot.isLocked).Count() >= numPedestrians;
    }
}

public class Slot
{
    public Vector3 position = Vector3.zero;
    public bool isLocked = false;
    public int id;

    public Slot(Vector3 _position, int id)
    {
        this.position = _position;
        this.id = id;
    }
}

public class SlotAsignation
{
    public NavMeshAgent agent = null;
    public Slot slot = null;

    public SlotAsignation(NavMeshAgent agent, Slot slot)
    {
        this.agent = agent;
        this.slot = slot;
    }
}
