using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TrafficLightWaitingSlotsManager
{
    private int numLanes = 2;
    private float verticalOffset = .8f;
    private int slotsPerLane = 6;

    private List<List<Slot>> slots;
    private Vector3 upperLeft;
    private Vector3 upperRight;
    private Vector3 forward;
    private Vector3 right;
    private float laneLength;
    private Dictionary<int, List<Slot>> tieredSlots = new Dictionary<int, List<Slot>>();
    public TrafficLightWaitingSlotsManager(Vector3 _upperLeft, Vector3 _upperRight, Vector3 _forward, Vector3 _right, Vector3 crossingCentre)
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
                float distanceToCentre = Vector3.Distance(crossingCentre, slotPosition);
                slots[i].Add(new Slot(slotPosition, i * numLanes + j, distanceToCentre, i));
            }
            // A higher tier number means that the slot is worse
            List<Slot> orderedSlots = new List<Slot>(slots[i]).OrderBy(x => x.distance).ToList();
            int tier = 0;
            for (int k = 0; k < orderedSlots.Count; k++)
            {
                if (orderedSlots[k].tier != -1) continue;

                if(!tieredSlots.ContainsKey(tier)) 
                    tieredSlots[tier] = new List<Slot>();
                
                foreach (Slot slot in orderedSlots.FindAll(slot => Mathf.Approximately(orderedSlots[k].distance, slot.distance)))
                {
                    slot.tier = tier;
                    tieredSlots[tier].Add(slot);
                }
                tier++;
            }
        }
    }
    //public void AddLane()
    //{
    //    float horizontalOffset = laneLength / (slotsPerLane - 1);
    //    float verticalInitialOffset = .5f;

    //    slots.Add(new List<Slot>());

    //    for (int i = 0; i < slotsPerLane; i++)
    //    {
    //        Vector3 slotPosition = new Vector3(
    //                upperLeft.x + right.x * i * horizontalOffset,
    //                upperLeft.y,
    //                upperLeft.z - forward.z * verticalInitialOffset - forward.z * (slots.Count - 1) * verticalOffset);
    //        slots[slots.Count - 1].Add(new Slot(slotPosition, -1, Mathf.Infinity, -1));
    //    }
    //    numLanes++;
    //}

    //public void RemoveLane()
    //{
    //    slots.RemoveAt(numLanes - 1);
    //    numLanes--;
    //}
    public List<List<Slot>> GetSlots()
    {
        return slots;
    }

    public List<Slot> GetSlotsForGroup(int numPedestrians)
    {
        List<Slot> waitingSlots = new List<Slot>();
        for (int i = 0; i < numLanes; i++)
        {
            foreach (var slot in slots[i])
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

    public Slot GetSlotForPedestrian(Vector3 pedestrianPos)
    {
        Slot waitingSlot = null;
        int numTiers = tieredSlots.Keys.Count;
        // For each lane
        for (int i = 0; i < numLanes; i++)
        {
            // Iterate each tier
            foreach (var tier in tieredSlots.Keys)
            {
                List<Slot> freeLaneTieredSlots = tieredSlots[tier].Where(slot => slot.lane == i && !slot.isLocked).ToList();
                // Get all that are not locked, and then, if there's more than one try to lock the closest one
                int numFreeSlots = freeLaneTieredSlots.Count;
                if (numFreeSlots > 0)
                {
                    Slot selectedSlot = null;
                    if(numFreeSlots == 1)
                    {
                        // Easy assignation
                        selectedSlot = freeLaneTieredSlots.First();
                    }
                    else
                    {
                        // Get the closest one
                        float bestDistance = Mathf.Infinity;
                        foreach(var slot in freeLaneTieredSlots)
                        {
                            float distance = Vector3.Distance(pedestrianPos, slot.position);
                            if(distance < bestDistance)
                            {
                                bestDistance = distance;
                                selectedSlot = slot;
                            }
                        }
                    }
                    selectedSlot.isLocked = true;
                    return selectedSlot;
                }
                // No free slots in this tier, try the next one or the next lane
            }
            
        }
        // Never return a null, always find a solution
        if(waitingSlot == null)
        {

        }
        // En la forma en la que está el método ahora, trata de asignar por linea y por tier
        // Separar el método de forma que se pueda elegir el criterio de asignación, a veces por cercanía, a veces por tier
        // Tener en cuenta en un futuro, a la hora de asignar en distintas lanes, que no se debe asignar una posición que haga que el personaje no vea el tráfico, es decir, esté tapado por otro personaje al frente
        return waitingSlot;
    }
    private bool IsGoodTier(int tier)
    {
        if(slotsPerLane >= 5 &&  slotsPerLane <= 6)
        {
            if(tier == 0 || tier == 1)
                return true;
            
            return false;
        }
        else
        {
            if (tier == 0 || tier == 1 || tier == 2)
                return true;
            return false;
        }
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
    public float distance;
    public int tier = -1;
    public int lane = -1;

    public Slot(Vector3 _position, int id, float _distance, int _lane)
    {
        this.position = _position;
        this.id = id;
        this.distance = _distance;
        this.lane = _lane;
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
