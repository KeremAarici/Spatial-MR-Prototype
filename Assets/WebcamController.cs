using UnityEngine.UI;
using UnityEngine;

public class WebcamController : MonoBehaviour
{
    [Header("UI Render Target")]
    [SerializeField] private RawImage displayImage;

    [Header("Webcam Settings")]
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;

    private WebCamTexture webcamTexture;

    public WebCamTexture WebcamTexture => webcamTexture;
    public bool IsPlaying => webcamTexture != null && webcamTexture.isPlaying;

    private void Start() {
        InitializeWebcam();
    }

    private void InitializeWebcam() {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("No webcam devices found.");
            return;
        }

        // Use the first available webcam device
        string deviceName = devices[0].name;
        webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);

        if (displayImage != null)
        {
            displayImage.texture = webcamTexture;
        }

        webcamTexture.Play();
        Debug.Log($"Webcam initialized: {deviceName} at {requestedWidth}x{requestedHeight} @ {requestedFPS} FPS");
    }

    void OnDestroy() {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
