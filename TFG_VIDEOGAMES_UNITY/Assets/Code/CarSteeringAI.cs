using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSteeringAI : MonoBehaviour
{
    public Transform targetPositionTransform;
    private CarSteering carSteering;
    private Vector3 targetPosition;
    float reachedTargetDistance = 1f;


    private void Awake()
    {
        carSteering = GetComponent<CarSteering>();
    }

    private void Update()
    {
        SetTargetPosition(targetPositionTransform.position);
        SetDirection();
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

    public void SetTargetPosition(Vector3 _targetPosition)
    {
        targetPosition = _targetPosition;
    }

    private void SetDirection()
    {
        float forwardAmount = 0f;
        float turnAmount = 0f;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > reachedTargetDistance)
        {
            // The target is still so far
            Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);
            if (dot > 0)
            {
                // Target in front         
                float stoppingDistance = 5f;
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
                    forwardAmount = -1f;

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
            Debug.Log("REACHED TARGET");
            if (carSteering.GetSpeed() > 0.5f)
            {
                // Hit the brakes if going too fast
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
}
