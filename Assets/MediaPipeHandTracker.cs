using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using System;
using Unity.Collections;

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

    private HandLandmarker handLandmarker;
    private Texture2D texture2d;
    private Color32[] pixelData;
    

    private string modelPath;
    private bool isInitialized = false;
    private long frameTimestamp = 0;
    private Vector3[] latestLandmarks = null;
    private bool hasNewData = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float distanceFromCamera = 2.0f; // Distance from the camera to the hand landmarks

    [Header("Alignment & Mirroring Settings")]
    [SerializeField] private bool flipX = true;  
    [SerializeField] private bool flipY = false;

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
        Debug.Log($"[CHECK 1] Is Tracker Initialized -> {isInitialized}");
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

        try
        {
            var baseOptions = new BaseOptions(BaseOptions.Delegate.CPU, modelAssetPath: modelPath);
            var options = new HandLandmarkerOptions(
                baseOptions: baseOptions, 
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                numHands: 1,
                resultCallback: OnHandLandmarkerResult
                );
            handLandmarker = HandLandmarker.CreateFromOptions(options);
            isInitialized = true;
            Debug.Log("Hand landmarker initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing hand landmarker: {e.Message}");
        }
    }

    public void UpdateLandmarkPositions(Vector3[] normalizedLandmarks)
    {
        if (normalizedLandmarks == null || normalizedLandmarks.Length != LANDMARK_COUNT)
        {
            Debug.LogError("[MediaPipe] Geçersiz landmark verisi alındı.");
            return;
        }

        if (displayImage == null)
        {
            Debug.LogError("[MediaPipe] displayImage (RawImage) referansı eksik!");
            return;
        }   


        Vector3[] corners = new Vector3[4];
        displayImage.rectTransform.GetWorldCorners(corners);

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;
        float imageZ = displayImage.transform.position.z;

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            Vector3 landmark = normalizedLandmarks[i];

            
            float normX = flipX ? (1.0f - landmark.x) : landmark.x;
            float normY = flipY ? landmark.y : (1.0f - landmark.y);

            float worldX = Mathf.Lerp(minX, maxX, normX);
            float worldY = Mathf.Lerp(minY, maxY, normY);
        
            
            float worldZ = imageZ - 0.1f - (landmark.z * 0.2f);

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
        
        WebCamTexture webCamTex = webcamController.WebcamTexture;
        if (webCamTex.width < 100 || !webCamTex.didUpdateThisFrame) return;

        ProcessWebcamFrame(webCamTex);
        
        if (hasNewData)
        {
            if (latestLandmarks != null)
            {
                UpdateLandmarkPositions(latestLandmarks);
            }
            else
            {
                HideLandmarks();
            }
            hasNewData = false;
        }
    }

    private void ProcessWebcamFrame(WebCamTexture webCamTex)
    {

        if (texture2d == null || texture2d.width != webCamTex.width || texture2d.height != webCamTex.height)
        {
            texture2d = new Texture2D(webCamTex.width, webCamTex.height, TextureFormat.RGBA32, false);
            pixelData = new Color32[webCamTex.width * webCamTex.height];
        }

        pixelData = webCamTex.GetPixels32(pixelData);
        texture2d.SetPixels32(pixelData);
        texture2d.Apply();

        using (var mpImage = new Mediapipe.Image(ImageFormat.Types.Format.Srgba, texture2d.width, texture2d.height, texture2d.width * 4, texture2d.GetRawTextureData<byte>()))
        {
            frameTimestamp += 33; // Increment timestamp for each frame
            handLandmarker.DetectAsync(mpImage, frameTimestamp);
        }
    }

    private void OnHandLandmarkerResult(HandLandmarkerResult result,Mediapipe.Image image, long timestamp)
    {
        
        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            var landmarks = result.handLandmarks[0].landmarks;
            Vector3[] normalizedPositions = new Vector3[LANDMARK_COUNT];

            for (int i = 0; i < LANDMARK_COUNT && i < landmarks.Count; i++)
            {
                normalizedPositions[i] = new Vector3(landmarks[i].x, landmarks[i].y, landmarks[i].z);
            }
            latestLandmarks = normalizedPositions;
        }
        else
        {
            latestLandmarks = null; // No hands detected
        }

        hasNewData = true;
    }

    private void OnDestroy()
    {
        if (handLandmarker != null)
        {
            handLandmarker.Close();
            handLandmarker = null;
        }
    }
}
