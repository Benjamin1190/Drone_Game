using UnityEngine;
using TMPro;

public class FPVBatteryHUD : MonoBehaviour
{
    [Header("Drone")]
    public FPVDroneController drone;

    [Header("Battery")]
    public TextMeshProUGUI voltageText;

    [Header("Low Battery")]
    public TextMeshProUGUI lowBatteryText;

    [Range(0f, 100f)]
    public float lowBatteryPercent = 20f;

    [Header("Blink")]
    public float blinkSpeed = 4f;

    [Header("Voltage")]
    public float maxVoltage = 16.8f;
    public float minVoltage = 13.2f;

    private float blinkTimer;

    void Start()
    {
        if (lowBatteryText != null)
        {
            lowBatteryText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (drone == null)
            return;

        float battery = drone.battery;
        float maxBattery = drone.maxBattery;

        float batteryPercent = (battery / maxBattery) * 100f;

        batteryPercent = Mathf.Clamp(batteryPercent, 0f, 100f);

        // Mostrar voltaje
        if (voltageText != null)
        {
            float voltage = Mathf.Lerp(
                minVoltage,
                maxVoltage,
                batteryPercent / 100f
            );

            voltageText.text = voltage.ToString("F1") + "V";
        }

        // LOW BATTERY
        if (lowBatteryText != null)
        {
            if (batteryPercent <= lowBatteryPercent && batteryPercent > 0f)
            {
                lowBatteryText.gameObject.SetActive(true);

                blinkTimer += Time.deltaTime * blinkSpeed;

                float alpha = Mathf.PingPong(blinkTimer, 1f);

                Color color = lowBatteryText.color;
                color.a = alpha;
                lowBatteryText.color = color;
            }
            else
            {
                lowBatteryText.gameObject.SetActive(false);

                blinkTimer = 0f;

                Color color = lowBatteryText.color;
                color.a = 1f;
                lowBatteryText.color = color;
            }
        }
    }
}
