using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMovementAI : MonoBehaviour
{
    private Vector3 targetPosition;
    //[SerializeField] Transform debugObject;
    [SerializeField] private bool shouldStopAtWaypoint;
    [SerializeField] private bool targetReached = false;
    private bool hasTarget = false;
    [SerializeField] bool debugDontMove;

    private CarMovement carSteering;

    private void Awake()
    {
        carSteering = GetComponent<CarMovement>();
    }

    private void Update()
    {
        //DEBUG
        //SetTarget(debugObject.transform.position);+ç
        if (debugDontMove) return;
        if (!hasTarget) return;

        if (!shouldStopAtWaypoint)
        {
            SetDirection();
        }
        else
        {
            SetDirectionWithStop();
        }


    }
    private void SetDirection()
    {
        float forwardAmount = 1f;
        float turnAmount = 0f;
        float reachedTargetDistance = 0.2f;
        Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;

        float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
        if (angleToDir > 0)
        {
            turnAmount = 1f;
        }
        else if (angleToDir < 0)
        {
            turnAmount = -1f;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < reachedTargetDistance)
        {
            targetReached = true;
        }
        else
        {
            targetReached = false;
        }

        carSteering.SetInputs(forwardAmount, turnAmount);
    }
    private void SetDirectionWithStop()
    {
        float forwardAmount = 0f;
        float turnAmount = 0f;

        float reachedTargetDistance = 1f;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > reachedTargetDistance)
        {
            // Still too far, keep going
            Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);

            if (dot > 0)
            {
                // Target in front
                forwardAmount = 1f;

                float stoppingDistance = 0.5f;
                float stoppingSpeed = 1f;
                if (distanceToTarget < stoppingDistance && carSteering.GetSpeed() > stoppingSpeed)
                {
                    // Within stopping distance and moving forward too fast
                    forwardAmount = -1f;
                }
            }
            else
            {
                // Target behind
                float reverseDistance = 25f;
                if (distanceToTarget > reverseDistance)
                {
                    // Too far to reverse
                    forwardAmount = 1f;
                }
                else
                {
                    forwardAmount = -1f;
                }
            }

            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);

            if (angleToDir > 0)
            {
                turnAmount = 1f;
            }
            else
            {
                turnAmount = -1f;
            }
        }
        else
        {
            // Reached target
            if (carSteering.GetSpeed() > 15f)
            {
                forwardAmount = -1f;
            }
            else
            {
                forwardAmount = 0f;
            }
            turnAmount = 0f;
        }

        carSteering.SetInputs(forwardAmount, turnAmount);
    }

    public void SetTargetPosition(Vector3 _targetPosition, bool _shouldStopAtWaypoint)
    {
        targetPosition = _targetPosition;
        shouldStopAtWaypoint = _shouldStopAtWaypoint;
        hasTarget = true;
    }

    public bool GetTargetReached()
    {
        return targetReached;
    }

}
