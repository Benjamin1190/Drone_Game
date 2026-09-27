using UnityEngine;

public class FPVDroneController : MonoBehaviour
{
    [Header("Flight")]
    public float maxLift = 18f;
    public float throttleAcceleration = 4f;

    public float pitchTorque = 8f;
    public float rollTorque = 8f;
    public float yawTorque = 5f;

    [Header("Physics")]
    public float mass = 1.2f;
    public float airResistance = 0.05f;
    public float rotationResistance = 1.5f;

    [Header("Propellers")]
    public float propellerSpeed = 2000f;
    public float idleSpeed = 500f;

    [Header("Battery")]
    public float maxBattery = 100f;
    public float battery = 100f;
    public float batteryDrainPerSecond = 0.5f;

    [HideInInspector]
    public bool acceptInput = false;
    [HideInInspector]
    public float movementIntensity = 0f;
    [HideInInspector]
    public bool armed = false;
    [HideInInspector]
    public bool rthActive = false;
    [HideInInspector]
    public bool isDisconnected = false;
    [HideInInspector]
    public bool forceMotorSpin = false;

    private Rigidbody rb;

    private Transform motor1;
    private Transform motor2;
    private Transform motor3;
    private Transform motor4;

    private float throttle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearDamping = airResistance;
        rb.angularDamping = rotationResistance;

        motor1 = FindChildRecursive(transform, "motor 1");
        motor2 = FindChildRecursive(transform, "motor 2");
        motor3 = FindChildRecursive(transform, "motor 3");
        motor4 = FindChildRecursive(transform, "motor 4");

        battery = maxBattery;

        if (motor1 == null)
            Debug.LogWarning("No se encontró motor 1");

        if (motor2 == null)
            Debug.LogWarning("No se encontró motor 2");

        if (motor3 == null)
            Debug.LogWarning("No se encontró motor 3");

        if (motor4 == null)
            Debug.LogWarning("No se encontró motor 4");
    }

    void FixedUpdate()
    {
        if (acceptInput)
        {
            HandleThrottle();
            HandleMovement();
            HandleBattery();
        }

        RotateMotors();
    }

    void HandleThrottle()
    {
        bool throttleUp =
            Input.GetKey(KeyCode.Space) ||
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.Z);

        bool throttleDown =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.X);

        if (throttleUp)
        {
            throttle += throttleAcceleration * Time.fixedDeltaTime;
        }

        if (throttleDown)
        {
            throttle -= throttleAcceleration * Time.fixedDeltaTime;
        }

        throttle = Mathf.Clamp(throttle, 0f, maxLift);
    }

    void HandleMovement()
    {
        if (battery <= 0f)
            return;

        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.W))
            pitch = 1f;

        if (Input.GetKey(KeyCode.S))
            pitch = -1f;

        if (Input.GetKey(KeyCode.A))
            roll = -1f;

        if (Input.GetKey(KeyCode.D))
            roll = 1f;

        if (Input.GetKey(KeyCode.Q))
            yaw = -1f;

        if (Input.GetKey(KeyCode.E))
            yaw = 1f;

        rb.AddForce(transform.up * throttle, ForceMode.Force);

        rb.AddTorque(
            transform.right * pitch * pitchTorque +
            transform.forward * -roll * rollTorque +
            transform.up * yaw * yawTorque,
            ForceMode.Force
        );
    }

    void HandleBattery()
    {
        if (battery <= 0f)
        {
            battery = 0f;
            throttle = 0f;
            return;
        }

        float throttlePercent = throttle / maxLift;

        float consumption = batteryDrainPerSecond;

        consumption += throttlePercent * batteryDrainPerSecond * 2f;

        battery -= consumption * Time.fixedDeltaTime;

        battery = Mathf.Clamp(battery, 0f, maxBattery);
    }

    void RotateMotors()
    {
        float speed;

        if (forceMotorSpin)
        {
            speed = propellerSpeed;
        }
        else if ((!acceptInput && !rthActive) || !armed || battery <= 0f)
        {
            speed = 0f;
        }
        else
        {
            float throttlePercent = throttle / maxLift;
            float intensity = Mathf.Max(throttlePercent, movementIntensity);
            speed = Mathf.Lerp(idleSpeed, propellerSpeed, Mathf.Clamp01(intensity));
        }

        if (motor1 != null)
            motor1.Rotate(Vector3.forward, speed * Time.fixedDeltaTime);

        if (motor2 != null)
            motor2.Rotate(Vector3.forward, -speed * Time.fixedDeltaTime);

        if (motor3 != null)
            motor3.Rotate(Vector3.forward, -speed * Time.fixedDeltaTime);

        if (motor4 != null)
            motor4.Rotate(Vector3.forward, speed * Time.fixedDeltaTime);
    }

    public void FullStop()
    {
        throttle = 0f;
        acceptInput = false;
        armed = false;
        rthActive = false;
        movementIntensity = 0f;
        forceMotorSpin = false;
        isDisconnected = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }
}
