using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera playerCamera;
    public Camera droneCamera;
    public MonoBehaviour playerScript;
    public MonoBehaviour droneScript;

     private bool droneActive = false;
    void Start()
    {
        playerScript.enabled = true;
        droneScript.enabled = false;

        playerCamera.enabled = true;
        droneCamera.enabled = true;

        playerCamera.targetDisplay = 0;
        droneCamera.targetDisplay = 1;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            droneActive = !droneActive;

            playerScript.enabled = !droneActive;
            droneScript.enabled = droneActive;
            
            int playerDisplay = playerCamera.targetDisplay;
            playerCamera.targetDisplay = droneCamera.targetDisplay;
            droneCamera.targetDisplay = playerDisplay;
        }
    }
}