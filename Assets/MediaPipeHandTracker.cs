using UnityEngine.UI;
using UnityEngine;
using System.IO;

public class MediaPipeHandTracker : MonoBehaviour
{
    [Header("Dependency")]
    [SerializeField] private WebcamController webcamController;
    [SerializeField] private RawImage displayImage;

    [Header("Debug Visualization")]
    [SerializeField] private GameObject landmarkPrefab;

    private string modelPath;
    private bool isInitialized = false;

    private void Start() {
        InitializeTracker();
    }

    private void InitializeTracker() {
        if (webcamController == null)
        {
            Debug.LogError("WebcamController is not assigned.");
            return;
        }

        modelPath = Path.Combine(Application.streamingAssetsPath, "hand_landmarker.task");   

        if (!File.Exists(modelPath))
        {
            Debug.LogError($"Model file not found at path: {modelPath}");
            return;
        }

        Debug.Log($"Model file found at path: {modelPath}");
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || !webcamController.IsPlaying) return;
        

        // Here you would typically call the MediaPipe hand tracking inference method


        ProcessWebcamFrame(webcamController.WebcamTexture);
    }

    private void ProcessWebcamFrame(WebCamTexture texture)
    {
        // 1. Convert the WebCamTexture to a format suitable for MediaPipe processing
        // 2. Run the MediaPipe hand tracking model on the frame
        // 3. Retrieve the hand landmarks and visualize them using landmarkPrefab
    }
}
