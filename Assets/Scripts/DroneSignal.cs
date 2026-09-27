using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DroneSignal : MonoBehaviour
{
    [Header("Referencias")]
    public FPVDroneController drone;
    public Transform player;
    public GamepadDroneInput gamepadInput;

    [Header("Imágenes de señal (arrastrar las 4 imágenes .png)")]
    public Texture2D signal3Bars;
    public Texture2D signal2Bars;
    public Texture2D signal1Bar;
    public Texture2D signal0Bars;

    [Header("Desconexión")]
    public TextMeshProUGUI connectionText;
    public Image greyOverlay;
    public RawImage noiseOverlay;
    [Range(1f, 10f)]
    public float textDuration = 2f;

[Header("Rango DJI (metros)")]
    public float djiMaxRange = 500f;

    [Header("Rango FPV (metros)")]
    public float fpvMaxRange = 150f;

    [Header("Cancelar RTH")]
    public GamepadDroneInput.GamepadButton cancelRTHButton = GamepadDroneInput.GamepadButton.Circle;

    [Header("Return To Home (solo DJI)")]
    public float rthAltitude = 25f;
    public float rthSpeed = 8f;
    public float rthAscentSpeed = 5f;
    public float rthDescentSpeed = 2f;

    public enum RTHState { None, Ascending, Returning, Descending, Landed }

    [HideInInspector]
    public RTHState rthState = RTHState.None;

    private Image signalDisplay;
    private Sprite sprite3, sprite2, sprite1, sprite0;
    private Rigidbody droneRb;
    private ObjectTeleporter teleporter;
    private bool signalLost = false;
    private float connectionTextTimer = 0f;
    private Texture2D noiseTexture;
    private Color32[] noisePixels;

    void Start()
    {
        if (drone != null)
            droneRb = drone.GetComponent<Rigidbody>();

        teleporter = FindAnyObjectByType<ObjectTeleporter>();

        signalDisplay = GetComponent<Image>();

        sprite3 = TextureToSprite(signal3Bars);
        sprite2 = TextureToSprite(signal2Bars);
        sprite1 = TextureToSprite(signal1Bar);
        sprite0 = TextureToSprite(signal0Bars);

        if (signalDisplay != null)
            signalDisplay.enabled = false;

        noiseTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        noiseTexture.filterMode = FilterMode.Point;
        noisePixels = new Color32[64 * 64];

        if (noiseOverlay != null)
        {
            noiseOverlay.texture = noiseTexture;
            noiseOverlay.gameObject.SetActive(false);
        }

        if (greyOverlay != null)
            greyOverlay.gameObject.SetActive(false);

        if (connectionText != null)
            connectionText.gameObject.SetActive(false);

    }

    void Update()
    {
        if (drone == null || player == null)
            return;

        if (connectionTextTimer > 0f)
        {
            connectionTextTimer -= Time.deltaTime;
            if (connectionTextTimer <= 0f && connectionText != null)
                connectionText.gameObject.SetActive(false);
        }

        bool rthInProgress = rthState != RTHState.None && rthState != RTHState.Landed;

        if (rthInProgress)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && WasCancelPressed(gamepad))
            {
                CancelRTH();
                return;
            }
        }

        float distance = Vector3.Distance(player.position, drone.transform.position);
        float maxRange = GetMaxRange();

        if (signalLost)
        {
            HandleDisconnectOverlays();

            if (distance < maxRange)
            {
                OnSignalRestored();
                return;
            }
        }

        if (!drone.acceptInput && !rthInProgress && !signalLost)
        {
            if (signalDisplay != null)
                signalDisplay.enabled = false;

            return;
        }

        if (!signalLost)
        {
            UpdateSignalImage(distance, maxRange);

            if (distance > maxRange)
                OnSignalLost();
        }
    }

    void FixedUpdate()
    {
        if (rthState == RTHState.None || rthState == RTHState.Landed || droneRb == null)
            return;

        HandleRTH();

        if (drone.battery > 0f)
        {
            drone.battery -= drone.batteryDrainPerSecond * Time.fixedDeltaTime;

            if (drone.battery <= 0f)
            {
                drone.battery = 0f;
                drone.armed = false;
                drone.rthActive = false;
                drone.movementIntensity = 0f;
                rthState = RTHState.Landed;
            }
        }
    }

void HandleDisconnectOverlays()
    {
        if (greyOverlay != null)
            greyOverlay.gameObject.SetActive(false);

        if (noiseOverlay != null)
            noiseOverlay.gameObject.SetActive(false);
    }

    void UpdateNoise()
    {
        for (int i = 0; i < noisePixels.Length; i++)
        {
            byte val = (byte)Random.Range(0, 256);
            noisePixels[i] = new Color32(val, val, val, 200);
        }

        noiseTexture.SetPixels32(noisePixels);
        noiseTexture.Apply();
    }

    void OnSignalLost()
    {
        signalLost = true;
        drone.acceptInput = false;
        drone.isDisconnected = true;

        if (signalDisplay != null)
            signalDisplay.enabled = false;

        if (teleporter != null)
        {
            teleporter.ForcePlayerView();
            teleporter.droneCamera.enabled = false;
        }

        if (connectionText != null)
        {
            connectionText.gameObject.SetActive(true);
            connectionText.text = "DISCONNECTED";
            connectionText.color = Color.white;
            connectionTextTimer = textDuration;
        }

        if (IsDJIMode())
        {
            rthState = RTHState.Ascending;
            drone.armed = true;
            drone.rthActive = true;
        }
        else
        {
            drone.armed = false;
            drone.rthActive = false;
        }
    }

    void OnSignalRestored()
    {
        signalLost = false;
        drone.isDisconnected = false;

        if (greyOverlay != null)
            greyOverlay.gameObject.SetActive(false);

        if (noiseOverlay != null)
            noiseOverlay.gameObject.SetActive(false);

        if (teleporter != null)
            teleporter.droneCamera.enabled = true;

        if (rthState != RTHState.None)
        {
            droneRb.linearVelocity = Vector3.zero;
            rthState = RTHState.None;
            drone.rthActive = false;
            drone.movementIntensity = 0f;
        }

        drone.acceptInput = true;
        drone.armed = false;
        if (gamepadInput != null)
            gamepadInput.ForceDisarm();

        if (connectionText != null)
        {
            connectionText.gameObject.SetActive(true);
            connectionText.text = "CONNECTED";
            connectionText.color = Color.white;
            connectionTextTimer = textDuration;
        }
    }

    void CancelRTH()
    {
        droneRb.linearVelocity = Vector3.zero;
        drone.rthActive = false;
        drone.movementIntensity = 0f;
        rthState = RTHState.None;
        signalLost = false;
        drone.isDisconnected = false;

        if (greyOverlay != null)
            greyOverlay.gameObject.SetActive(false);

        if (noiseOverlay != null)
            noiseOverlay.gameObject.SetActive(false);

        if (teleporter != null)
            teleporter.droneCamera.enabled = true;

        float distance = Vector3.Distance(player.position, drone.transform.position);

        if (distance < GetMaxRange())
        {
            drone.acceptInput = true;
            drone.armed = false;
            if (gamepadInput != null)
                gamepadInput.ForceDisarm();

            if (connectionText != null)
            {
                connectionText.gameObject.SetActive(true);
                connectionText.text = "CONNECTED";
                connectionText.color = Color.white;
                connectionTextTimer = textDuration;
            }
        }
        else
        {
            drone.armed = false;

            if (connectionText != null)
            {
                connectionText.gameObject.SetActive(true);
                connectionText.text = "DISCONNECTED";
                connectionText.color = Color.white;
                connectionTextTimer = textDuration;
            }
        }
    }

    float GetMaxRange()
    {
        if (gamepadInput != null && gamepadInput.mode == GamepadDroneInput.StickMode.DJI)
            return djiMaxRange;

        return fpvMaxRange;
    }

    bool IsDJIMode()
    {
        return gamepadInput != null && gamepadInput.mode == GamepadDroneInput.StickMode.DJI;
    }

    void UpdateSignalImage(float distance, float maxRange)
    {
        if (signalDisplay == null)
            return;

        signalDisplay.enabled = true;

        float ratio = distance / maxRange;

        if (ratio < 0.33f)
            signalDisplay.sprite = sprite3;
        else if (ratio < 0.66f)
            signalDisplay.sprite = sprite2;
        else if (ratio < 0.9f)
            signalDisplay.sprite = sprite1;
        else
            signalDisplay.sprite = sprite0;
    }

    Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null)
            return null;

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    bool WasCancelPressed(Gamepad gamepad)
    {
        switch ((int)cancelRTHButton)
        {
            case 0: return gamepad.buttonSouth.wasPressedThisFrame;
            case 1: return gamepad.buttonEast.wasPressedThisFrame;
            case 2: return gamepad.buttonWest.wasPressedThisFrame;
            case 3: return gamepad.buttonNorth.wasPressedThisFrame;
            case 4: return gamepad.leftShoulder.wasPressedThisFrame;
            case 5: return gamepad.rightShoulder.wasPressedThisFrame;
            case 6: return gamepad.leftStickButton.wasPressedThisFrame;
            case 7: return gamepad.rightStickButton.wasPressedThisFrame;
            case 8: return gamepad.startButton.wasPressedThisFrame;
            case 9: return gamepad.selectButton.wasPressedThisFrame;
            case 10: return gamepad.dpad.up.wasPressedThisFrame;
            case 11: return gamepad.dpad.down.wasPressedThisFrame;
            case 12: return gamepad.dpad.left.wasPressedThisFrame;
            case 13: return gamepad.dpad.right.wasPressedThisFrame;
            default: return gamepad.buttonEast.wasPressedThisFrame;
        }
    }

    void HandleRTH()
    {
        Vector3 dronePos = drone.transform.position;
        Vector3 playerPos = player.position;

        Quaternion targetRot = Quaternion.Euler(0f, drone.transform.eulerAngles.y, 0f);
        drone.transform.rotation = Quaternion.Slerp(drone.transform.rotation, targetRot, Time.fixedDeltaTime * 5f);

        droneRb.AddForce(-Physics.gravity * drone.mass, ForceMode.Force);

        switch (rthState)
        {
            case RTHState.Ascending:
                drone.movementIntensity = 0.7f;

                if (dronePos.y < rthAltitude)
                {
                    droneRb.linearVelocity = new Vector3(0f, rthAscentSpeed, 0f);
                }
                else
                {
                    rthState = RTHState.Returning;
                }
                break;

            case RTHState.Returning:
                drone.movementIntensity = 0.7f;

                Vector3 dirToPlayer = new Vector3(playerPos.x - dronePos.x, 0f, playerPos.z - dronePos.z);
                float horizontalDist = dirToPlayer.magnitude;

                if (horizontalDist > 2f)
                {
                    Vector3 moveDir = dirToPlayer.normalized * rthSpeed;
                    float altError = rthAltitude - dronePos.y;
                    droneRb.linearVelocity = new Vector3(moveDir.x, altError * 2f, moveDir.z);

                    Quaternion lookRot = Quaternion.LookRotation(dirToPlayer.normalized, Vector3.up);
                    drone.transform.rotation = Quaternion.Slerp(drone.transform.rotation, lookRot, Time.fixedDeltaTime * 3f);
                }
                else
                {
                    rthState = RTHState.Descending;
                }
                break;

            case RTHState.Descending:
                drone.movementIntensity = 0.3f;

                droneRb.linearVelocity = new Vector3(0f, -rthDescentSpeed, 0f);

                if (Physics.Raycast(dronePos, Vector3.down, 1.5f))
                {
                    droneRb.linearVelocity = Vector3.zero;
                    drone.armed = false;
                    drone.rthActive = false;
                    drone.movementIntensity = 0f;
                    rthState = RTHState.Landed;
                }
                break;
        }
    }
}
