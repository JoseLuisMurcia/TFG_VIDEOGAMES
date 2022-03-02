using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSteeringAI : MonoBehaviour
{
    private CarSteering carSteering;
    private Vector3 targetPosition;
    private bool shouldStopAtWaypoint;
    private bool targetReached = false;

    private void Awake()
    {
        carSteering = GetComponent<CarSteering>();
    }

    private void Update()
    {
        if (!shouldStopAtWaypoint)
        {
            SetDirection();
        }
        else
        {
            SetDirectionWithStop();
        }
        //DebugMethod();
    }

    private void DebugMethod()
    {
        Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dirToMovePosition);

        float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
        Debug.Log("angleToDir: " + angleToDir);

        //Debug.Log("Car speed: " + carSteering.GetSpeed());
    }

    public void SetTargetPosition(Vector3 _targetPosition, bool _shouldStopAtWaypoint)
    {
        targetPosition = _targetPosition;
        shouldStopAtWaypoint = _shouldStopAtWaypoint;
    }

    private void SetDirection()
    {
        float forwardAmount = 1f;
        float turnAmount = 0f;
        float reachedTargetDistance = 0.1f;
        Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;

        float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
        if (!(angleToDir < 5f && angleToDir > -5f))
        {
            if (angleToDir > 0)
            {
                turnAmount = 1f;
            }
            else if(angleToDir < 0)
            {
                turnAmount = -1f;
            }
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
        float reachedTargetDistance = 1.5f;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > reachedTargetDistance)
        {
            // The target is still so far
            Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);
            if (dot > 0)
            {
                // Target in front         
                float stoppingDistance = 8f;
                float stoppingSpeed = 2f;
                if (distanceToTarget < stoppingDistance && carSteering.GetSpeed() > stoppingSpeed)
                {
                    // Within stopping distance and moving forward too fast
                    forwardAmount = -1f;
                }
                else
                {
                    forwardAmount = 1f;
                }

            }
            else
            {
                // Target behind
                float reverseDistance = 3f;
                if (distanceToTarget > reverseDistance)
                {
                    // Too far to reverse
                    forwardAmount = 1f;
                }
                else
                {
                    // Not too far to reverse
                    float reverseStoppingDistance = 1.5f;
                    float stoppingSpeed = 0.5f;
                    //Debug.Log("Distance to target: " + distanceToTarget);
                    //Debug.Log("carSteering.GetSpeed(): " + carSteering.GetSpeed());

                    if (distanceToTarget < reverseStoppingDistance && carSteering.GetSpeed() > stoppingSpeed)
                    {
                        // Within stopping distance and moving back too fast
                        forwardAmount = 1f;
                    }
                    else
                    {
                        // Not withing stopping distance nor moving back too fast
                        forwardAmount = -1f;
                    }


                }
            }

            /* Codigo que funciona para delante pero no para detras */
            //float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
            //float anglePerfection = 5f;
            //float absAngleToDir = Mathf.Abs(angleToDir);
            //if (absAngleToDir > anglePerfection)
            //{
            //    if (angleToDir > 0)
            //    {
            //        turnAmount = 1f;
            //    }
            //    else
            //    {
            //        turnAmount = -1f;
            //    }
            //}


            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
            if (!(angleToDir < 5f && angleToDir > -5f) && !(angleToDir > 175f && angleToDir < 180f) && !(angleToDir > -180f && angleToDir < -175f))
            {
                if (angleToDir > 0)
                {
                    turnAmount = 1f;
                }
                else
                {
                    turnAmount = -1f;
                }
            }


        }
        else
        {
            // Reached target
            targetReached = true;
            forwardAmount = 0f;
            // Target in front
            if (carSteering.GetSpeed() > 0.1f)
            {
                // Hit the brakes if going too fast
                forwardAmount = -1f;
            }


            turnAmount = 0f;
        }

        carSteering.SetInputs(forwardAmount, turnAmount);
    }

    public bool GetTargetReached()
    {
        return targetReached;
    }
}
