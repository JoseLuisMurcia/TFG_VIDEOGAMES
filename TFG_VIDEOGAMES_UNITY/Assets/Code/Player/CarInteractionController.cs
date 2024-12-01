using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarInteractionController : MonoBehaviour
{
    private PrometeoCarController carController;

    private void Awake()
    {
        carController = GetComponent<PrometeoCarController>();
    }

    private void Start()
    {
        DisableController();
    }

    public void EnableController()
    {
        if (carController != null)
        {
            carController.enabled = true;
        }
    }
    public void DisableController()
    {
        if (carController != null)
        {
            carController.enabled = false;
        }
    }
}
