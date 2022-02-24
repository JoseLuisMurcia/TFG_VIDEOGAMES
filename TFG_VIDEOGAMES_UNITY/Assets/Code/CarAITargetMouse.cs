using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAITargetMouse : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;

    public bool isFollowing = false;

    private void Update()
    {
        if(isFollowing)
        {
            targetTransform.position = Mouse3D.GetMouseWorldPosition();
        }

        if(Input.GetMouseButtonDown(0))
        {
            isFollowing = !isFollowing;
        }
    }
}
