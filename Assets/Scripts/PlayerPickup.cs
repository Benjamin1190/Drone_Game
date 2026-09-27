using UnityEngine;

public class ObjectTeleporter : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera playerCamera;
    public Camera droneCamera;

    [Header("Scripts")]
    public MonoBehaviour playerScript;

    [Header("Dron")]
    public FPVDroneController droneController;
    public float grabDistance = 5f;

    [Header("HUD")]
    public FPVBatteryHUD batteryHUD;

    [Header("Objetos")]
    public GameObject[] pickupObjects;
    public GameObject teleportPoint;
    public Vector3[] teleportRotations;

    private GameObject heldObject;

    void Start()
    {
        playerScript.enabled = true;

        if (droneController != null)
            droneController.acceptInput = false;

        playerCamera.enabled = true;
        droneCamera.enabled = true;

        playerCamera.targetDisplay = 0;
        droneCamera.targetDisplay = 1;

        SetBatteryHUD(false);

        CameraSwitcher oldSwitcher = FindAnyObjectByType<CameraSwitcher>();
        if (oldSwitcher != null)
            oldSwitcher.enabled = false;
    }

    private void Update()
    {
        // B = cambiar cámara (solo si tengo el control agarrado)
        if (Input.GetKeyDown(KeyCode.B) && heldObject != null)
        {
            SwitchCamera();
        }

        // R/Triangle = agarrar y soltar TODO (control y dron)
        if (Input.GetKeyDown(KeyCode.R))
        {
            HandleGrab();
        }

    }

    private void HandleGrab()
    {
        if (heldObject != null)
        {
            DropObject();
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance))
            return;

        for (int i = 0; i < pickupObjects.Length; i++)
        {
            if (hit.collider.gameObject == pickupObjects[i] ||
                hit.collider.transform.IsChildOf(pickupObjects[i].transform))
            {
                PickupObject(pickupObjects[i], i);
                return;
            }
        }
    }

    public void OnGamepadButton(KeyCode key)
    {
        if (key == KeyCode.B && heldObject != null)
            SwitchCamera();
        else if (key == KeyCode.R)
            HandleGrab();
    }

    private void SwitchCamera()
    {
        bool wouldShowDrone = playerCamera.targetDisplay == 0;

        if (wouldShowDrone && droneController != null && droneController.isDisconnected)
            return;

        int playerDisplay = playerCamera.targetDisplay;

        playerCamera.targetDisplay = droneCamera.targetDisplay;
        droneCamera.targetDisplay = playerDisplay;

        bool droneOnMain = droneCamera.targetDisplay == 0;

        if (droneController != null)
            droneController.acceptInput = droneOnMain;

        playerScript.enabled = !droneOnMain;

        SetBatteryHUD(droneOnMain);
    }

    private void PickupObject(GameObject obj, int index)
    {
        heldObject = obj;

        Vector3 rot = (index < teleportRotations.Length) ? teleportRotations[index] : Vector3.zero;

        obj.transform.SetParent(teleportPoint.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.Euler(rot);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        playerScript.enabled = false;

        if (droneController != null)
            droneController.acceptInput = true;

        SetBatteryHUD(true);

        playerCamera.targetDisplay = 1;
        droneCamera.targetDisplay = 0;
    }

    private void DropObject()
    {
        if (heldObject == null)
            return;

        heldObject.transform.SetParent(null);

        Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.3f;

        if (Physics.Raycast(dropPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            dropPos = hit.point + Vector3.up * 0.1f;

        heldObject.transform.position = dropPos;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = heldObject.GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        playerScript.enabled = true;

        if (droneController != null)
            droneController.acceptInput = false;

        SetBatteryHUD(false);

        playerCamera.targetDisplay = 0;
        droneCamera.targetDisplay = 1;

        heldObject = null;
    }


    public void ForcePlayerView()
    {
        playerCamera.targetDisplay = 0;
        droneCamera.targetDisplay = 1;

        if (droneController != null)
            droneController.acceptInput = false;

        playerScript.enabled = true;
        SetBatteryHUD(false);
    }

    public void SetBatteryHUD(bool active)
    {
        if (batteryHUD == null)
            return;

        batteryHUD.enabled = active;

        if (batteryHUD.voltageText != null)
            batteryHUD.voltageText.gameObject.SetActive(active);

        if (batteryHUD.lowBatteryText != null)
            batteryHUD.lowBatteryText.gameObject.SetActive(false);
    }
}
