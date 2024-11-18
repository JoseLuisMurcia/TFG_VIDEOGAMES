using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using System;
using UnityEngine.Animations.Rigging;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private Rig aimRig;
    private float aimRigWeight;
    [SerializeField] private CinemachineVirtualCamera m_aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private bool hasGun = false;
    [SerializeField] private Transform gunTransform;

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

        if (hasGun)
        {
            gunTransform.gameObject.SetActive(true);
        }
        else
        {
            gunTransform.gameObject.SetActive(false);
        }
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


        if (hasGun)
        {   
            bool hasTargetInAim = IsShootingTarget(hitTransform);
            if (hasTargetInAim)
            {
                m_crosshairController.ShowCrosshairHit();
            }
            else
            {
                m_crosshairController.HideCrosshairHit();
            }

            if (m_starterAssetsInputs.aim)
            {
                // AIM
                m_aimVirtualCamera.gameObject.SetActive(true);
                m_thirdPersonController.SetSensitivity(aimSensitivity);
                m_thirdPersonController.SetRotateOnMove(false);
                m_animator.SetLayerWeight(1, Mathf.Lerp(m_animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));

                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = transform.position.y;
                Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

                transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
                aimRigWeight = 1f;


                // SHOOT
                if (m_starterAssetsInputs.shoot)
                {
                    if (hasTargetInAim)
                    {
                        m_crosshairController.ShowHitmarker();
                    }
                    m_starterAssetsInputs.shoot = false;
                }
            }
        }
        else
        {
            m_aimVirtualCamera.gameObject.SetActive(false);
            m_thirdPersonController.SetSensitivity(normalSensitivity);
            m_thirdPersonController.SetRotateOnMove(true);
            m_animator.SetLayerWeight(1, Mathf.Lerp(m_animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
            aimRigWeight = 0f;
            m_crosshairController.HideAllCrosshair();
        }

        
        aimRig.weight = Mathf.Lerp(aimRig.weight, aimRigWeight, Time.deltaTime * 20f);
    }

    private bool IsShootingTarget(Transform hitTransform)
    {
        if (hitTransform != null && hitTransform.GetComponent<ShootingTarget>() != null)
        {
            return true;
        }
        return false;
    }

    public void OnEnterCarEvent(bool newValue)
    {

    }
    public void OnEquipGunEvent(bool newValue)
    {
        hasGun = !hasGun;
        m_animator.SetBool("HasGun", hasGun);
        if (hasGun)
        {
            gunTransform.gameObject.SetActive(true);
        }
        else
        {
            gunTransform.gameObject.SetActive(false);
        }
    }
}
