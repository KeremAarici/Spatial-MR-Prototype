using UnityEngine.UI;
using UnityEngine;
using System.IO;
using System;

public class MediaPipeHandTracker : MonoBehaviour
{
    [Header("Dependency")]
    [SerializeField] private WebcamController webcamController;
    [SerializeField] private RawImage displayImage;

    [Header("Debug Visualization")]
    [SerializeField] private GameObject landmarkPrefab;
    [SerializeField] private Transform landmarkParent;

    private const int LANDMARK_COUNT = 21; // MediaPipe hand model predicts 21 landmarks
    private GameObject[] landmarkNodes = new GameObject[LANDMARK_COUNT];

    private string modelPath;
    private bool isInitialized = false;

    private void Start() {
        CreateLandmarkPool();
        InitializeTracker();
    }

    private void CreateLandmarkPool()
    {
        if (landmarkPrefab == null)
        {
            Debug.LogError("Landmark prefab is not assigned.");
            return;
        }

        Transform parentTransform = landmarkParent != null ? landmarkParent : transform;

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            GameObject node = Instantiate(landmarkPrefab, parentTransform);
            node.name = $"Landmark_{i}";
            node.SetActive(false); // Initially inactive
            landmarkNodes[i] = node;
        }
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

    public void UpdateLandmarkPositions(Vector3[] normalizedLandmarks)
    {
        if (normalizedLandmarks == null || normalizedLandmarks.Length != LANDMARK_COUNT)
        {
            Debug.LogError("Invalid landmark data received.");
            return;
        }

        RectTransform rawImageRectTransform = displayImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rawImageRectTransform.GetWorldCorners(corners);

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            Vector3 landmark = normalizedLandmarks[i];
            float worldX = Mathf.Lerp(minX, maxX, landmark.x);
            float worldY = Mathf.Lerp(minY, maxY, 1f - landmark.y); // Invert Y for Unity's coordinate system
            float worldZ = landmark.z; // Depth can be used for scaling or other effects

            landmarkNodes[i].transform.position = new Vector3(worldX, worldY, worldZ);
            landmarkNodes[i].SetActive(true);
            
        }
    }

    public void HideLandmarks()
    {
        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            if (landmarkNodes[i] != null)
            {
                landmarkNodes[i].SetActive(false);
            }
        }
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
