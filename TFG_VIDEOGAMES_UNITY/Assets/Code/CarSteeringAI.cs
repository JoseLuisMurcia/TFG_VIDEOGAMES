using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSteeringAI : MonoBehaviour
{
    public Transform targetPositionTransform;
    private CarSteering carSteering;
    private Vector3 targetPosition;
    [SerializeField]
    float reachedTargetDistance = 1f;

    private void Awake()
    {
        carSteering = GetComponent<CarSteering>();
    }

    private void Update()
    {
        SetTargetPosition(targetPositionTransform.position);
        SetDirection();
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
            Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);
            if (dot > 0)
            {
                // Target in front
                forwardAmount = 1f;

                float stoppingDistance = 30f;
                float stoppingSpeed = 50f;
                if(distanceToTarget < stoppingDistance && carSteering.GetSpeed() > stoppingSpeed)
                {
                    // Within stopping distance and moving forward too fast
                    forwardAmount = -1f;
                }
            }
            else
            {
                // Target behind
                float reverseDistance = 25f;
                if(distanceToTarget > reverseDistance)
                {
                    forwardAmount = 1f;
                }
                else
                {
                    forwardAmount = -1f;

                }
            }

            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
            Debug.Log("angleToDir: " + angleToDir);
            Debug.Log("dot: " + dot);
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
            //Reached target
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
}
