using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using System;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera m_aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;

    private ThirdPersonController m_thirdPersonController;
    private StarterAssetsInputs m_starterAssetsInputs;
    private CrosshairController m_crosshairController;
    private Animator m_animator;
    private void Awake()
    {
        m_starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        m_thirdPersonController = GetComponent<ThirdPersonController>();
        m_crosshairController = GetComponent<CrosshairController>();
        m_animator = GetComponent<Animator>();
    }
    void Update()
    {
        Vector3 mouseWorldPosition = Vector3.zero;
        Vector2 screenCenterPoint = new Vector2(Screen.width * .5f, Screen.height * .5f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        Transform hitTransform = null;
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
        {
            debugTransform.position = raycastHit.point;
            mouseWorldPosition = raycastHit.point;
            hitTransform = raycastHit.transform;
        }
        else
        {
            debugTransform.position = Camera.main.transform.position + Camera.main.transform.forward * 999f;
            mouseWorldPosition = Camera.main.transform.position + Camera.main.transform.forward * 999f;
        }

        // Update crosshair status
        if (IsShootingTarget(hitTransform))
        {
            m_crosshairController.ShowCrosshairHit();
        }
        else
        {
            m_crosshairController.HideCrosshairHit();
        }

        if (m_starterAssetsInputs.aim)
        {
            m_aimVirtualCamera.gameObject.SetActive(true);
            m_thirdPersonController.SetSensitivity(aimSensitivity);
            m_thirdPersonController.SetRotateOnMove(false);
            m_animator.SetLayerWeight(1, Mathf.Lerp(m_animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);           
        }
        else
        {           
            m_aimVirtualCamera.gameObject.SetActive(false);
            m_thirdPersonController.SetSensitivity(normalSensitivity);
            m_thirdPersonController.SetRotateOnMove(true);
            m_animator.SetLayerWeight(1, Mathf.Lerp(m_animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
        }

        if(m_starterAssetsInputs.shoot)
        {
            if (IsShootingTarget(hitTransform))
            {
                m_crosshairController.ShowHitmarker();
            }
            m_starterAssetsInputs.shoot = false;
        }
    }

    private bool IsShootingTarget(Transform hitTransform)
    {
        if (hitTransform != null && hitTransform.GetComponent<ShootingTarget>() != null) 
        {
            return true;
        }
        return false;
    }
}
