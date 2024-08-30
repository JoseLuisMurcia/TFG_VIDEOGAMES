using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TrafficLightWaitingSlotsManager
{
    private int numLanes = 2;
    private float verticalOffset = 1f;
    private int slotsPerLane = 7;

    private List<List<Slot>> slots;
    private Vector3 upperLeft;
    private Vector3 upperRight;
    private Vector3 forward;
    private Vector3 right;
    private Vector3 crossingCentre;
    private float laneLength;
    private Dictionary<int, List<Slot>> tieredSlots = new Dictionary<int, List<Slot>>();
    private List<SlotAssignation> slotAsignations = new List<SlotAssignation>();
    bool isRotated;
    public TrafficLightWaitingSlotsManager(Vector3 _upperLeft, Vector3 _upperRight, Vector3 _forward, Vector3 _right, Vector3 _crossingCentre)
    {
        // Set variables useful for the future
        this.forward = _forward;
        this.right = _right;
        this.upperLeft = _upperLeft;
        this.upperRight = _upperRight;
        this.laneLength = Vector3.Distance(upperLeft, upperRight);
        this.crossingCentre = _crossingCentre;

        isRotated = Mathf.Abs(_forward.z) > 0.8f ? false : true;
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

                if (isRotated)
                {
                    slotPosition.x = upperLeft.x - forward.x * verticalInitialOffset - forward.x * i * verticalOffset;
                    slotPosition.z = upperLeft.z + right.z * j * horizontalOffset;
                }
                
                float distanceToCentre = Vector3.Distance(crossingCentre, slotPosition);
                slots[i].Add(new Slot(slotPosition, i * slotsPerLane + j, distanceToCentre, i));
            }
            // A higher tier number means that the slot is worse
            SortSlotsByTier(slots[i]);
        }
    }
    private void AddLane()
    {
        numLanes++;
        float horizontalOffset = laneLength / (slotsPerLane - 1);
        float verticalInitialOffset = .5f;

        for (int i = numLanes - 1; i < numLanes; i++)
        {
            slots.Add(new List<Slot>());
            for (int j = 0; j < slotsPerLane; j++)
            {
                Vector3 slotPosition = new Vector3(
                    upperLeft.x + right.x * j * horizontalOffset,
                    upperLeft.y,
                    upperLeft.z - forward.z * verticalInitialOffset - forward.z * i * verticalOffset);
                float distanceToCentre = Vector3.Distance(crossingCentre, slotPosition);
                slots[i].Add(new Slot(slotPosition, i * slotsPerLane + j, distanceToCentre, i));
            }
            // A higher tier number means that the slot is worse
            SortSlotsByTier(slots[i]);
        }
    }
    private void SortSlotsByTier(List<Slot> laneSlots)
    {
        // A higher tier number means that the slot is worse
        List<Slot> orderedSlots = new List<Slot>(laneSlots).OrderBy(x => x.distance).ToList();
        int tier = 0;
        for (int k = 0; k < orderedSlots.Count; k++)
        {
            if (orderedSlots[k].tier != -1) continue;

            if (!tieredSlots.ContainsKey(tier))
                tieredSlots[tier] = new List<Slot>();

            foreach (Slot slot in orderedSlots.FindAll(slot => Mathf.Approximately(orderedSlots[k].distance, slot.distance)))
            {
                slot.tier = tier;
                tieredSlots[tier].Add(slot);
            }
            tier++;
        }
    }
    private void RemoveLane()
    {
        slots.RemoveAt(numLanes - 1);
        numLanes--;
    }
    public List<List<Slot>> GetSlots()
    {
        return slots;
    }
    public Slot GetBestSlot()
    {
        return slots[0].Find((x) => x.tier == 0);
    }
    public List<Slot> GetSlotsForGroup(InvisibleLeader leader, int numPedestrians)
    {
        Vector3 groupPos = leader.transform.position;
        List<Slot> waitingSlots = GetBestSlotsForGroup(groupPos, numPedestrians);
        if (waitingSlots.Count == 0)
        {
            Debug.LogWarning("An extra row for a group has to be created, there are no free slots");
            AddLane();
            waitingSlots = GetBestSlotsForGroup(groupPos, numPedestrians);
        }

        if (waitingSlots.Count > numPedestrians)
        {
            //Select the best slots
            waitingSlots = ChooseBestSlotsForGroup(waitingSlots, groupPos, numPedestrians);
        }
        SlotAssignation newAssignation = new SlotAssignation(waitingSlots, null, leader, true);
        slotAsignations.Add(newAssignation);
        return waitingSlots;
    }
    private List<Slot> ChooseBestSlotsForGroup(List<Slot> waitingSlots, Vector3 groupPos, int numPedestrians)
    {
        List<Slot> bestSlots = new List<Slot>();

        int lowestTierSum = int.MaxValue;

        for (int i = 0; i < waitingSlots.Count; i += numPedestrians)
        {
            List<Slot> candidates = waitingSlots.Skip(i).Take(numPedestrians).ToList();
            int totalTier = candidates.Sum(slot => slot.tier);

            if (totalTier < lowestTierSum)
            {
                lowestTierSum = totalTier;
                bestSlots = candidates;
            }
            else if (totalTier == lowestTierSum)
            {
                //Take distance into account
                Vector3 avgCanPos = candidates.Aggregate(Vector3.zero, (acc, slot) => acc + slot.position) / (float)numPedestrians;
                Vector3 avgBestPos = bestSlots.Aggregate(Vector3.zero, (acc, slot) => acc + slot.position) / (float)numPedestrians;
                if (Vector3.Distance(groupPos, avgCanPos) < Vector3.Distance(groupPos, avgBestPos))
                {
                    lowestTierSum = totalTier;
                    bestSlots = candidates;
                }
            }
        }
        return bestSlots;
    }

    public Slot GetSlotForPedestrian(Pedestrian pedestrian)
    {
        Slot reservedSlot = CheckReservedAssignations(pedestrian);
        if (reservedSlot == null)
        {   
            return FindSlotForPedestrian(pedestrian);
        }
        return reservedSlot;
    }
    private Slot CheckReservedAssignations(Pedestrian newPedestrian)
    {
        // Check all the slots that are assigned already but not locked
        List<SlotAssignation> reservedAssignations = slotAsignations.Where(assignation => !assignation.isGroup && assignation.slots.All(slot => slot.isReserved && !slot.isLocked)).ToList();

        // Compare the current pedestrianPos with all the reservedAssignations, assign him to the closest one of all of them. 
        SlotAssignation bestAssignation = GetClosestSlotAssignation(newPedestrian.transform.position, reservedAssignations);
        if (bestAssignation != null)
        {
            Pedestrian oldPedestrian = bestAssignation.pedestrian;
            // Assign the bestPos to the new pedestrian
            bestAssignation.isGroup = false;
            bestAssignation.pedestrian = newPedestrian;
            // Free the assignation and assign those pedestrians a new one
            // Call PedestrianReassignation and assign a new position to the old pedestrian+
            PedestrianReassignation(oldPedestrian);
            return bestAssignation.slots.First();
        }
        return null;
    }
    private Slot FindSlotForPedestrian(Pedestrian pedestrian)
    {
        Vector3 pedestrianPos = pedestrian.transform.position;
        List<Slot> waitingSlots = GetBestTierSlots(pedestrianPos, true);
        // Never return a null, always find a solution
        if (waitingSlots.Count == 0)
        {
            waitingSlots = GetBestTierSlots(pedestrianPos, false);
            // What happens if there's no slot still?
            if (waitingSlots.Count == 0)
            {
                Debug.LogWarning("An extra row for a pedestrian has to be created, there are no free slots");
                AddLane();
                waitingSlots = GetBestTierSlots(pedestrianPos, false);
            }
        }
        Slot selectedSlot = ChooseBestSlot(waitingSlots, pedestrianPos);
        SlotAssignation newAssignation = new SlotAssignation(new List<Slot>() { selectedSlot }, pedestrian, null, false);
        slotAsignations.Add(newAssignation);
        return selectedSlot;
    }
    private void PedestrianReassignation(Pedestrian pedestrian)
    {
        Slot selectedSlot = FindSlotForPedestrian(pedestrian);
        pedestrian.ReassignSlot(selectedSlot);
        SlotAssignation reassignation = new SlotAssignation(new List<Slot>() { selectedSlot }, pedestrian, null, false);
        slotAsignations.Add(reassignation);
    }
    private Slot ChooseBestSlot(List<Slot> waitingSlots, Vector3 pedestrianPos)
    {
        if (waitingSlots.Count == 1)
        {
            return waitingSlots.First();
        }
        else
        {
            // waitingSlots.Count > 1
           // Slot bestSlot = waitingSlots
           //.OrderBy(slot => (slot.tier * 5) + (slot.lane * 9.5f))
           //.First();
            Slot bestSlot = waitingSlots
           .OrderBy(slot => slot.lane)
           .First();
            return bestSlot;
        }
    }
    private SlotAssignation GetClosestSlotAssignation(Vector3 pedestrianPos, List<SlotAssignation> reservedAssignations)
    {
        SlotAssignation closestAssignation = null;
        float closestDistance = Mathf.Infinity;

        foreach (var assignation in reservedAssignations)
        {
            // Calculate the average position of the slots in the current assignation
            Vector3 averageSlotPosition = Vector3.zero;
            foreach (var slot in assignation.slots)
            {
                averageSlotPosition += slot.position;
            }
            averageSlotPosition /= assignation.slots.Count;

            // Calculate the distance from the average position to the pedestrian position
            float pedestrianDistance = Vector3.Distance(averageSlotPosition, pedestrianPos);
            float assignedDistance = assignation.isGroup ? 
                Vector3.Distance(assignation.leader.transform.position, averageSlotPosition) : 
                Vector3.Distance(assignation.pedestrian.transform.position, averageSlotPosition);

            // Check if this assignation is closer than the already assigned pedestrian/group && closer than the previous closest one
            if (pedestrianDistance < assignedDistance && pedestrianDistance < closestDistance)
            {
                closestDistance = pedestrianDistance;
                closestAssignation = assignation;
            }
        }

        return closestAssignation;
    }
    private bool SlotCanSee(Slot slot)
    {
        int slotLane = slot.lane;
        if (slotLane == 0)
            return true;

        // From this lane, check the slot in front to see if its free
        List<Slot> slotsFromPreviousLane = slots[slotLane - 1];
        Slot previousSlot = slotsFromPreviousLane.Find((x) => x.id == (slot.id - slotsPerLane));
        if (previousSlot != null)
        {
            return !previousSlot.isLocked && !previousSlot.isReserved;
        }
        Debug.LogError("The previous matching lane slot was not found");
        return true;
    }
    //private bool NextSlotCanSee(Slot slot)
    //{
    //    int slotLane = slot.lane;
    //    if (slotLane == numLanes - 1)
    //        return true;

    //    // From this lane, check the slot behind to see if its free
    //    List<Slot> slotsFromNextLane = slots[slotLane + 1];
    //    Slot previousSlot = slotsFromNextLane.Find((x) => x.id == (slot.id + slotsPerLane));
    //    if (previousSlot != null)
    //    {
    //        return !previousSlot.isLocked;
    //    }
    //    Debug.LogError("The next lane matching slot was not found");
    //    return true;
    //}
    private List<Slot> GetBestTierSlots(Vector3 pedestrianPos, bool isRestrictive)
    {
        List<Slot> bestSlots = new List<Slot>();
        // For each lane
        for (int i = 0; i < numLanes; i++)
        {
            // Iterate each tier
            foreach (var tier in tieredSlots.Keys)
            {
                List<Slot> freeLaneTieredSlots = null;
                if (isRestrictive)
                {
                    freeLaneTieredSlots = tieredSlots[tier]
                    .Where(slot => slot.lane == i && !slot.isLocked && !slot.isReserved)
                    .Where(slot => SlotCanSee(slot))
                    .ToList();
                }
                else
                {
                    freeLaneTieredSlots = tieredSlots[tier].Where(slot => slot.lane == i && !slot.isLocked && !slot.isReserved).ToList();
                }

                // Get all that are not locked, and then, if there's more than one try to lock the closest one
                int numFreeSlots = freeLaneTieredSlots.Count;
                if (numFreeSlots > 0)
                {
                    Slot selectedSlot = null;
                    if (numFreeSlots == 1)
                    {
                        // Easy assignation
                        selectedSlot = freeLaneTieredSlots.First();
                    }
                    else
                    {
                        // Get the closest one
                        float bestDistance = Mathf.Infinity;
                        foreach (var slot in freeLaneTieredSlots)
                        {
                            float distance = Vector3.Distance(pedestrianPos, slot.position);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                selectedSlot = slot;
                            }
                        }
                    }
                    // If there's a free slot, move on to the next lane
                    bestSlots.Add(selectedSlot);
                    break;
                }
            }
        }
        return bestSlots;
    }
    private List<Slot> GetBestSlotsForGroup(Vector3 groupPos, int numPedestrians)
    {
        List<Slot> bestSlots = new List<Slot>();
        // For each lane
        for (int i = 0; i < numLanes; i++)
        {
            //Find all free slots on the lane that are not locked
            List<Slot> freeSlots = slots[i].FindAll(x => !x.isLocked && !x.isReserved);

            // Get all that are not locked, and then, if there's more than one try to lock the closest one
            int numFreeSlots = freeSlots.Count;
            if (numFreeSlots >= numPedestrians)
            {
                // Look for adjacent slots
                // Store all the available combinations and then get the best one based on TIER
                for (int j = 0; j <= numFreeSlots - numPedestrians; j++)
                {
                    List<Slot> candidateSlots = freeSlots.Skip(j).Take(numPedestrians).ToList();
                    // Check if they are adjacent
                    bool areAdjacent = true;
                    for (int k = 1; k < candidateSlots.Count; k++)
                    {
                        if (candidateSlots[k].id != candidateSlots[k - 1].id + 1)
                        {
                            areAdjacent = false;
                            break;
                        }
                    }

                    if (areAdjacent)
                    {
                        bestSlots.AddRange(candidateSlots);
                    }
                }
            }
            if (bestSlots.Count > 0)
            {
                return bestSlots;
            }
        }
        return bestSlots;
    }
    private bool LaneHasSlots(int laneId, int numPedestrians)
    {
        return slots[laneId].FindAll((slot) => !slot.isLocked).Count() >= numPedestrians;
    }
    public void RemoveAssignation(Pedestrian pedestrian)
    {
        slotAsignations.RemoveAll(assignation => assignation.pedestrian == pedestrian);
    }
}

public class Slot
{
    public Vector3 position = Vector3.zero;
    public bool isLocked = false;
    public bool isReserved = false;
    public bool isGroup = false;
    public int id;
    public float distance;
    public int tier = -1;
    public int lane = -1;

    public Slot(Vector3 _position, int _id, float _distance, int _lane)
    {
        position = _position;
        id = _id;
        distance = _distance;
        lane = _lane;
    }
}

public class SlotAssignation
{
    public List<Slot> slots = null;
    public Pedestrian pedestrian = null;
    public InvisibleLeader leader = null;
    public bool isGroup = false;

    public SlotAssignation(List<Slot> _slots, Pedestrian _pedestrian, InvisibleLeader _leader, bool _isGroup)
    {
        slots = _slots;
        pedestrian = _pedestrian;
        leader = _leader;
        isGroup = _isGroup;
    }
}
