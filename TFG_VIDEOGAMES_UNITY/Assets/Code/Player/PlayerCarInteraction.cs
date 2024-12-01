using StarterAssets;
using UnityEngine;

public class PlayerCarInteraction : MonoBehaviour
{
    private ThirdPersonShooterController thirdPersonShooterController;
    private ThirdPersonController thirdPersonController;
    private CrosshairController crosshairController;
    private CarInteractionController carInteractionController;
    [SerializeField] private Camera carCamera;
    [SerializeField] private Camera pedestrianCamera;
    public Transform carEntryPoint; // Point where player appears when entering the car

    private bool nearCar = false;
    private bool inCar = false;

    private void Awake()
    {
        thirdPersonShooterController = GetComponent<ThirdPersonShooterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        crosshairController = GetComponent<CrosshairController>();
    }
    void Start()
    {
        // Initially, the player is a pedestrian
        EnablePedestrian();
    }

    void Update()
    {
        if (nearCar && !inCar && Input.GetKeyDown(KeyCode.E))
        {
            EnterCar();
        }
        else if (inCar && Input.GetKeyDown(KeyCode.E))
        {
            ExitCar();
        }
    }

    void EnterCar()
    {
        inCar = true;
        nearCar = false;

        // Switch controllers
        EnableCar();

        // Position the player in the car at the entry point
        //transform.position = carEntryPoint.position;
        //transform.rotation = carEntryPoint.rotation;


        // Optionally, play a car door opening animation
        // Animator.SetTrigger("OpenDoor"); // Add Animator and animation if needed
    }

    void ExitCar()
    {
        inCar = false;

        // Switch controllers
        EnablePedestrian();

        // Position the player outside the car
        // Adjust the position slightly to avoid clipping with the car
        //transform.position = carEntryPoint.position + transform.right * 1.5f;

        // Optionally, play a car door closing animation
        // Animator.SetTrigger("CloseDoor"); // Add Animator and animation if needed
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            nearCar = true;
            Debug.Log("Is near car: " + other.gameObject.name);
            carInteractionController = other.gameObject.GetComponent<CarInteractionController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            nearCar = false;
            Debug.Log("Out of range for car: " + other.gameObject.name);
            carInteractionController = null;
        }
    }

    private void EnablePedestrian()
    {
        thirdPersonShooterController.enabled = true;
        thirdPersonController.enabled = true;
        crosshairController.enabled = true;
        pedestrianCamera.gameObject.SetActive(true);
        carCamera.gameObject.SetActive(false);
        if (carInteractionController != null)
        {
            carInteractionController.DisableController();
        }
    }
    private void EnableCar()
    {
        thirdPersonShooterController.enabled = false;
        thirdPersonController.enabled = false;
        crosshairController.enabled = false;
        pedestrianCamera.gameObject.SetActive(false);
        carCamera.gameObject.SetActive(true);
        carInteractionController.EnableController();
    }
}
