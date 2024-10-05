using UnityEngine;

public class CarLightController : MonoBehaviour
{
    private Light[] carLights;

    // Randomized times for each car to switch lights
    private float randomizedTurnOnTime;
    private float randomizedTurnOffTime;
    void Start()
    {
        carLights = GetComponentsInChildren<Light>();

        // Randomize the turn on/off times within a range
        randomizedTurnOnTime = Random.Range(18f, 21f); // Evening time range (6PM to 9PM)
        randomizedTurnOffTime = Random.Range(5f, 7f);  // Morning time range (5AM to 7AM)

        // Subscribe to the hour change events
        if (LightingManager.Instance != null)
        {
            LightingManager.Instance.SubscribeToHourChangeEvent(OnHourChange);
            if (IsDayTime())
                SwitchLightsOff();
        }

    }
    private void OnHourChange(float hour)
    {
        if (IsDayTime())
        {
            SwitchLightsOff();
        }
        else
        {
            SwitchLightsOn();
        }
    }
    private bool IsDayTime()
    {
        float time = LightingManager.Instance.TimeOfDay;
        return (time > randomizedTurnOffTime && time < randomizedTurnOnTime);
    }
    private void SwitchLightsOff()
    {
        foreach (Light carLight in carLights)
        {
            carLight.enabled = false;
        }
    }
    private void SwitchLightsOn()
    {
        foreach (Light carLight in carLights)
        {
            carLight.enabled = true;
        }
    }
}
