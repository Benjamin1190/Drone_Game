using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GamepadDroneInput : MonoBehaviour
{
    [Header("Drone")]
    public FPVDroneController drone;

    [Header("Jugador")]
    public FPSController playerController;

    [Header("Modo de control")]
    public StickMode mode = StickMode.DJI;

    [Header("Armar dron (solo Mode1/Mode2)")]
    public GamepadButton armButton = GamepadButton.Cross;

    [Header("Texto Armed")]
    public TextMeshProUGUI armedText;

    [Header("Cámara Gimbal (solo DJI)")]
    public Transform gimbalCamera;
    [Range(1f, 5f)]
    public float gimbalSpeed = 2f;
    public float gimbalMinAngle = -90f;
    public float gimbalMaxAngle = 30f;

    [Header("Auto-estabilización (solo DJI)")]
    [Range(1f, 20f)]
    public float stabilizeForce = 12f;

    [Header("Sensibilidad vuelo")]
    [Range(0.1f, 2f)]
    public float throttleSensitivity = 1f;
    [Range(0.1f, 2f)]
    public float yawSensitivity = 1f;
    [Range(0.1f, 2f)]
    public float pitchSensitivity = 1f;
    [Range(0.1f, 2f)]
    public float rollSensitivity = 1f;

    [Header("Sensibilidad cámara jugador")]
    [Range(0.5f, 10f)]
    public float lookSensitivity = 3f;

    [Header("Dead Zone")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.15f;

    [Header("Invertir ejes vuelo")]
    public bool invertThrottle = false;
    public bool invertYaw = false;
    public bool invertPitch = false;
    public bool invertRoll = false;

    [Header("Invertir cámara jugador")]
    public bool invertLookX = false;
    public bool invertLookY = false;

    [Header("Botones del control")]
    public GamepadButton switchCameraButton = GamepadButton.Circle;
    public GamepadButton dropButton = GamepadButton.Square;
    public GamepadButton resetDroneButton = GamepadButton.Triangle;

    public enum StickMode
    {
        Mode1,
        Mode2,
        DJI
    }

    public enum GamepadButton
    {
        Cross,
        Circle,
        Square,
        Triangle,
        L1,
        R1,
        L3,
        R3,
        Options,
        Share,
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight
    }

    private Rigidbody rb;
    private ObjectTeleporter teleporter;
    private float gimbalAngle = 0f;
    private Quaternion gimbalOriginalRotation;
    private bool armed = false;
    private float cscTimer = 0f;
    private float cscRequiredTime = 1f;
    private bool wasAcceptingInput = false;
    private float armedTextTimer = 0f;

    void Start()
    {
        if (drone != null)
        {
            rb = drone.GetComponent<Rigidbody>();

            if (mode == StickMode.DJI)
            {
                drone.maxBattery = 4480f;
                drone.battery = 4480f;
                drone.batteryDrainPerSecond = 3.25f;
            }
        }

        teleporter = FindAnyObjectByType<ObjectTeleporter>();

        if (gimbalCamera != null)
            gimbalOriginalRotation = gimbalCamera.localRotation;

        if (armedText != null)
            armedText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (armedTextTimer > 0f)
        {
            armedTextTimer -= Time.deltaTime;
            if (armedTextTimer <= 0f && armedText != null)
                armedText.gameObject.SetActive(false);
        }

        var gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        // Botones generales
        if (WasButtonPressed(gamepad, switchCameraButton))
            SimulateKey(KeyCode.B);

        if (WasButtonPressed(gamepad, resetDroneButton))
            SimulateKey(KeyCode.R);

        // Armar/Desarmar
        if (drone != null && drone.acceptInput)
        {
            HandleArming(gamepad);

            if (!wasAcceptingInput)
            {
                drone.armed = armed;
                UpdateArmedText();
            }

            wasAcceptingInput = true;
        }
        else
        {
            cscTimer = 0f;

            if (armedText != null)
                armedText.gameObject.SetActive(false);

            wasAcceptingInput = false;
        }

        // Cámara del jugador con stick derecho cuando NO está en el dron
        if (drone != null && !drone.acceptInput && playerController != null)
        {
            Vector2 look = gamepad.rightStick.ReadValue();

            float lookX = ApplyDeadZone(look.x) * lookSensitivity;
            float lookY = ApplyDeadZone(look.y) * lookSensitivity;

            if (invertLookX) lookX = -lookX;
            if (invertLookY) lookY = -lookY;

            playerController.transform.Rotate(Vector3.up * lookX);

            playerController.xRotation -= lookY;
            playerController.xRotation = Mathf.Clamp(playerController.xRotation, -90f, 90f);
        }

        // Gimbal con L1/R1 solo en modo DJI (funciona sin estar armado)
        if (mode == StickMode.DJI && drone != null && drone.acceptInput && gimbalCamera != null)
        {
            bool l1 = gamepad.leftShoulder.isPressed;
            bool r1 = gamepad.rightShoulder.isPressed;

            if (l1)
                gimbalAngle += gimbalSpeed * Time.deltaTime * 60f;

            if (r1)
                gimbalAngle -= gimbalSpeed * Time.deltaTime * 60f;

            gimbalAngle = Mathf.Clamp(gimbalAngle, gimbalMinAngle, gimbalMaxAngle);

            gimbalCamera.localRotation = gimbalOriginalRotation;
            gimbalCamera.Rotate(gimbalAngle, 0f, 0f, Space.Self);
        }
    }

    void HandleArming(Gamepad gamepad)
    {
        if (mode == StickMode.DJI)
        {
            // CSC: stick izq abajo-derecha + stick der abajo-izquierda por 1 segundo
            Vector2 left = gamepad.leftStick.ReadValue();
            Vector2 right = gamepad.rightStick.ReadValue();

            bool leftCorrect = left.y < -0.3f && left.x > 0.3f;
            bool rightCorrect = right.y < -0.3f && right.x < -0.3f;

            if (leftCorrect && rightCorrect)
            {
                cscTimer += Time.deltaTime;

                if (cscTimer >= cscRequiredTime)
                {
                    armed = !armed;
                    drone.armed = armed;
                    cscTimer = 0f;
                    UpdateArmedText();
                }
            }
            else
            {
                cscTimer = 0f;
            }
        }
        else
        {
            // Mode 1/2: botón para armar/desarmar
            if (WasButtonPressed(gamepad, armButton))
            {
                armed = !armed;
                drone.armed = armed;
                UpdateArmedText();
            }
        }
    }

    public void ForceDisarm()
    {
        armed = false;
        if (drone != null)
            drone.armed = false;
        UpdateArmedText();
    }

    void UpdateArmedText()
    {
        if (armedText == null)
            return;

        armedText.gameObject.SetActive(true);
        armedText.text = armed ? "ARMED" : "DISARMED";
        armedText.color = Color.white;
        armedTextTimer = 3f;
    }

    void FixedUpdate()
    {
        if (drone == null || rb == null || !drone.acceptInput || !armed)
            return;

        var gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        if (drone.battery <= 0f)
            return;

        Vector2 left = gamepad.leftStick.ReadValue();
        Vector2 right = gamepad.rightStick.ReadValue();

        float throttleRaw, yawRaw, pitchRaw, rollRaw;

        if (mode == StickMode.Mode2)
        {
            throttleRaw = left.y;
            yawRaw = left.x;
            pitchRaw = right.y;
            rollRaw = right.x;
        }
        else if (mode == StickMode.Mode1)
        {
            pitchRaw = left.y;
            yawRaw = left.x;
            throttleRaw = right.y;
            rollRaw = right.x;
        }
        else
        {
            throttleRaw = left.y;
            yawRaw = left.x;
            pitchRaw = 0f;
            rollRaw = 0f;
        }

        float throttle = ApplyDeadZone(throttleRaw) * throttleSensitivity;
        float yaw = ApplyDeadZone(yawRaw) * yawSensitivity;
        float pitch = ApplyDeadZone(pitchRaw) * pitchSensitivity;
        float roll = ApplyDeadZone(rollRaw) * rollSensitivity;

        if (invertThrottle) throttle = -throttle;
        if (invertYaw) yaw = -yaw;
        if (invertPitch) pitch = -pitch;
        if (invertRoll) roll = -roll;

        float liftForce;

        if (mode == StickMode.DJI)
        {
            // DJI: stick centrado = hover, arriba = sube, abajo = baja
            float hoverForce = drone.mass * Physics.gravity.magnitude;
            float liftRange = drone.maxLift - hoverForce;
            liftForce = hoverForce + throttle * liftRange;
        }
        else
        {
            // FPV: sin throttle = cae, throttle máximo = sube
            liftForce = ((throttle + 1f) / 2f) * drone.maxLift;
        }

        liftForce = Mathf.Clamp(liftForce, 0f, drone.maxLift);

        rb.AddForce(drone.transform.up * liftForce, ForceMode.Force);

        if (mode == StickMode.DJI)
        {
            float moveForward = ApplyDeadZone(right.y) * pitchSensitivity;
            float moveRight = ApplyDeadZone(right.x) * rollSensitivity;

            if (invertPitch) moveForward = -moveForward;
            if (invertRoll) moveRight = -moveRight;

            Vector3 forward = Vector3.ProjectOnPlane(drone.transform.forward, Vector3.up).normalized;
            Vector3 rightDir = Vector3.ProjectOnPlane(drone.transform.right, Vector3.up).normalized;

            float moveForce = drone.maxLift * 0.7f;

            rb.AddForce(forward * moveForward * moveForce, ForceMode.Force);
            rb.AddForce(rightDir * moveRight * moveForce, ForceMode.Force);

            rb.AddTorque(drone.transform.up * yaw * drone.yawTorque, ForceMode.Force);

            drone.movementIntensity = Mathf.Max(Mathf.Abs(moveForward), Mathf.Abs(moveRight), Mathf.Abs(yaw));

            Quaternion targetRotation = Quaternion.Euler(0f, drone.transform.eulerAngles.y, 0f);
            Quaternion correction = targetRotation * Quaternion.Inverse(drone.transform.rotation);

            correction.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
                angle -= 360f;

            if (Mathf.Abs(angle) > 0.5f)
            {
                rb.AddTorque(axis * angle * stabilizeForce * Mathf.Deg2Rad, ForceMode.Force);
            }

            rb.angularVelocity *= 0.95f;

            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float stickInput = Mathf.Max(Mathf.Abs(moveForward), Mathf.Abs(moveRight));
            if (stickInput < 0.1f)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x * 0.95f,
                    rb.linearVelocity.y,
                    rb.linearVelocity.z * 0.95f
                );
            }
        }
        else
        {
            rb.AddTorque(
                drone.transform.right * pitch * drone.pitchTorque +
                drone.transform.forward * -roll * drone.rollTorque +
                drone.transform.up * yaw * drone.yawTorque,
                ForceMode.Force
            );
        }
    }

    private void SimulateKey(KeyCode key)
    {
        if (teleporter == null)
            return;

        teleporter.SendMessage("OnGamepadButton", key, SendMessageOptions.DontRequireReceiver);
    }

    bool WasButtonPressed(Gamepad gamepad, GamepadButton button)
    {
        return GetButton(gamepad, button).wasPressedThisFrame;
    }

    UnityEngine.InputSystem.Controls.ButtonControl GetButton(Gamepad gamepad, GamepadButton button)
    {
        switch ((int)button)
        {
            case 0: return gamepad.buttonSouth;
            case 1: return gamepad.buttonEast;
            case 2: return gamepad.buttonWest;
            case 3: return gamepad.buttonNorth;
            case 4: return gamepad.leftShoulder;
            case 5: return gamepad.rightShoulder;
            case 6: return gamepad.leftStickButton;
            case 7: return gamepad.rightStickButton;
            case 8: return gamepad.startButton;
            case 9: return gamepad.selectButton;
            case 10: return gamepad.dpad.up;
            case 11: return gamepad.dpad.down;
            case 12: return gamepad.dpad.left;
            case 13: return gamepad.dpad.right;
            default: return gamepad.buttonSouth;
        }
    }

    float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < deadZone)
            return 0f;

        float sign = Mathf.Sign(value);
        return sign * (Mathf.Abs(value) - deadZone) / (1f - deadZone);
    }
}
