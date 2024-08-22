using System;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public List<Wheel> wheels;
    private Rigidbody rb;

    public float maxAcceleration = 30f;
    public float brakeAcceleration = 50f;

    public float turnSensitivity = 1f;
    public float maxSteerAngle = 30f;

    // WTF donde ponerlo?
    [SerializeField] private Vector3 centerOfMass = Vector3.zero;
    float moveInput;
    float steerInput;
    public enum Axel
    {
        Front, Rear
    }
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }
    private void Update()
    {
        GetInputs();
        AnimateWheels();
    }
    private void LateUpdate()
    {
        Move();
        Steer();
        Brake();
    }
    void GetInputs()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }
    void Move()
    {
        wheels.ForEach(wheel => wheel.wheelCollider.motorTorque = moveInput * 550f * maxAcceleration * Time.deltaTime);
    }
    // The torques (motorTorque and brakeTorque ) shouldn't depend on Time.deltaTime. Remove those multiplications.
    // The wheelColliders should also be updated from FixedUpdate(), not LateUpdate().
    void Steer()
    {
        wheels.ForEach(wheel => 
            { 
                if(wheel.axel == Axel.Front)
                {
                    var inputSteer = steerInput * turnSensitivity * maxSteerAngle;
                    wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, inputSteer, .6f);
                }
            }
        );
    }
    void Brake()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            wheels.ForEach(wheel => wheel.wheelCollider.brakeTorque = 300f * brakeAcceleration * Time.deltaTime);
        }
        else
        {
            wheels.ForEach(wheel => wheel.wheelCollider.brakeTorque = 0f);
        }
    }
    void AnimateWheels()
    {
        wheels.ForEach(wheel =>
        {
            Quaternion rot;
            Vector3 pos;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
        });
    }
    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    
}
