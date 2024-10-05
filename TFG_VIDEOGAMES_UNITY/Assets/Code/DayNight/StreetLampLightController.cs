using UnityEngine;

public class StreetLampLightController : MonoBehaviour
{
    private Light[] streetLights;
    void Start()
    {
        streetLights = GetComponentsInChildren<Light>();
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
        return (time > 6f && time < 21f);
    }
    private void SwitchLightsOff()
    {
        foreach (Light light in streetLights)
        {
            light.enabled = false;
        }
    }
    private void SwitchLightsOn()
    {
        foreach (Light light in streetLights)
        {
            light.enabled = true;
        }
    }
}
