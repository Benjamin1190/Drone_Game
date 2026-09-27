using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TMP_Text fpsText;

    void Update()
    {
        float fps = 1f / Time.unscaledDeltaTime;
        fpsText.text = Mathf.RoundToInt(fps) + " FPS";
    }
}