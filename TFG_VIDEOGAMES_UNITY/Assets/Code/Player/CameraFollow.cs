using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] float moveSmoothness;
    [SerializeField] float rotSmoothness;

    [SerializeField] Vector3 moveOffset;
    [SerializeField] Vector3 rotOffset;

    [SerializeField] Transform carTarget;

    private void FixedUpdate()
    {
        FollowTarget();
    }
    private void FollowTarget()
    {
        HandleMovement();
        HandleRotation();
    }
    void HandleMovement()
    {
        Vector3 targetPos = new Vector3();
        targetPos = carTarget.TransformPoint(moveOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * Time.deltaTime);
    }
    void HandleRotation()
    {
        Vector3 direction = carTarget.position - transform.position;
        Quaternion rotation = new Quaternion();

        rotation = Quaternion.LookRotation(direction + rotOffset, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotSmoothness * Time.deltaTime);
    }
}
