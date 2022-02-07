using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSteering : MonoBehaviour
{
    [SerializeField]
    bool allWheelDrive;
    private float horizontalInput, verticalInput, steeringAngle;

    public WheelCollider frontDriverWheel, frontPassengerWheel, rearDriverWheel, rearPassengerWheel;
    public Transform frontDriverT, frontPassengerT, rearDriverT, rearPassengerT;

    [SerializeField]
    float maxSteerAngle = 30;
    [SerializeField]
    float motorForce = 50;

    public void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    public void SetInputs(float forwardAmount, float turnAmount)
    {
        horizontalInput = turnAmount;
        verticalInput = forwardAmount;
    }

    private void Steer()
    {
        steeringAngle = maxSteerAngle * horizontalInput;
        frontDriverWheel.steerAngle = steeringAngle;
        frontPassengerWheel.steerAngle = steeringAngle;
    }

    private void Accelerate()
    {
        // Front wheel drive, 4 wheel drive or rear wheel drive
        if(allWheelDrive)
        {
            frontDriverWheel.motorTorque = verticalInput * motorForce;
            frontPassengerWheel.motorTorque = verticalInput * motorForce;
            rearDriverWheel.motorTorque = verticalInput * motorForce;
            rearPassengerWheel.motorTorque = verticalInput * motorForce;
        }
        else
        {
            frontDriverWheel.motorTorque = verticalInput * motorForce;
            frontPassengerWheel.motorTorque = verticalInput * motorForce;
        }
        
    }

    private void UpdateWheels()
    {
        UpdateWheel(frontDriverWheel, frontDriverT);
        UpdateWheel(frontPassengerWheel, frontPassengerT);
        UpdateWheel(rearDriverWheel, rearDriverT);
        UpdateWheel(rearPassengerWheel, rearPassengerT);
    }

    private void UpdateWheel(WheelCollider _collider, Transform _transform)
    {
        Vector3 _pos = _transform.position;
        Quaternion _quat = _transform.rotation;
        Vector3 previousPos = new Vector3(_pos.x, _pos.y, _pos.z);
        Quaternion previousQuat = new Quaternion(_quat.x, _quat.y, _quat.z, _quat.w);
        _collider.GetWorldPose(out _pos, out _quat);

        _transform.position = Vector3.Lerp(previousPos, _pos, 0.5f);
        _transform.rotation = Quaternion.Lerp(previousQuat, _quat, 0.5f);
        //_transform.position = _pos;
        //_transform.rotation = _quat;
    }

    private void FixedUpdate()
    {
        //GetInput();
        Steer();
        Accelerate();
        UpdateWheels();
    }

    public float GetSpeed()
    {
        return Mathf.Abs((frontDriverWheel.motorTorque + frontPassengerWheel.motorTorque) * 0.5f);
    }
    
}
