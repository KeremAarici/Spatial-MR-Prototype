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
    [Header("Air Writing Settings")]
    [SerializeField] private SpatialAirWriter airWriter;


    [Header("Dependency")]
    [SerializeField] private WebcamController webcamController;
    [SerializeField] private RawImage displayImage;

    [Header("Debug Visualization")]
    [SerializeField] private GameObject leftHandLandmarkPrefab;
    [SerializeField] private GameObject rightHandLandmarkPrefab;
    [SerializeField] private Transform landmarkParent;

    private const int LANDMARK_COUNT = 21; // MediaPipe hand model predicts 21 landmarks
    private const int MAX_HANDS = 2;
    private GameObject[] leftLandmarkNodes = new GameObject[LANDMARK_COUNT];
    private GameObject[] rightLandmarkNodes = new GameObject[LANDMARK_COUNT];

    private HandLandmarker handLandmarker;
    private Texture2D texture2d;
    private Color32[] pixelData;
    

    private string modelPath;
    private bool isInitialized = false;
    private long frameTimestamp = 0;

    private class HandDataContainer
    {
        public Vector3[] landmarks;
        public bool isDetected;
    }

    private HandDataContainer leftHandData = new HandDataContainer();
    private HandDataContainer rightHandData = new HandDataContainer();


    private Vector3[] latestLandmarks = null;
    private bool hasNewData = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float distanceFromCamera = 2.0f; // Distance from the camera to the hand landmarks

    [Header("Alignment & Mirroring Settings")]
    [SerializeField] private bool flipX = true;  
    [SerializeField] private bool flipY = false;
    [SerializeField] private bool swapHandedness = true; // Swap left and right hands if the camera feed is mirrored

    private void Start() {
        CreateLandmarkPoolS();
        InitializeTracker();
    }

    private void CreateLandmarkPoolS()
    {
        Transform parentTransform = landmarkParent != null ? landmarkParent : transform;


        // Create left hand landmark nodes
        GameObject leftParent = new GameObject("LeftHand_Nodes");
        leftParent.transform.SetParent(parentTransform);
        GameObject lPrefab = leftHandLandmarkPrefab != null ? leftHandLandmarkPrefab : rightHandLandmarkPrefab; // Fallback to right hand prefab if left is not assigned

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            GameObject node = Instantiate(lPrefab, leftParent.transform);
            node.name = $"Left_Landmark_{i}";
            node.SetActive(false);
            leftLandmarkNodes[i] = node;
        }

        // Create right hand landmark nodes
        GameObject rightParent = new GameObject("RightHand_Nodes");
        rightParent.transform.SetParent(parentTransform);
        GameObject rPrefab = rightHandLandmarkPrefab != null ? rightHandLandmarkPrefab : lPrefab;

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            GameObject node = Instantiate(rPrefab, rightParent.transform);
            node.name = $"Right_Landmark_{i}";
            node.SetActive(false);
            rightLandmarkNodes[i] = node;
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
                numHands: MAX_HANDS,
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

    void Update()
    {
        if (!isInitialized || !webcamController.IsPlaying) return;
        
        WebCamTexture webCamTex = webcamController.WebcamTexture;
        if (webCamTex.width < 100) return;

        ProcessWebcamFrame(webCamTex);
        
        if (hasNewData)
        {
            UpdateHandVisualization(leftHandData, leftLandmarkNodes);
            UpdateHandVisualization(rightHandData, rightLandmarkNodes);

            ProcessAirWritingLogic();
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
        leftHandData.isDetected = false;
        rightHandData.isDetected = false;


        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            for (int h = 0; h < result.handLandmarks.Count; h++)
            {
                var landmarks = result.handLandmarks[h].landmarks;
                Vector3[] positions = new Vector3[LANDMARK_COUNT];

                for (int i = 0; i < LANDMARK_COUNT && i < landmarks.Count; i++)
                {
                    positions[i] = new Vector3(landmarks[i].x, landmarks[i].y, landmarks[i].z);
                }

                // Classify hand as left or right based on handedness information
                string handLabel = "Right"; // Default
                if (result.handedness != null && h < result.handedness.Count && result.handedness[h].categories.Count > 0)
                {
                    handLabel = result.handedness[h].categories[0].categoryName;
                }

                bool isLeft = (handLabel == "Left");

                if (swapHandedness)
                {
                    isLeft = !isLeft;
                }

            
                if (isLeft)
                {
                    leftHandData.landmarks = positions;
                    leftHandData.isDetected = true;
                }
                else
                {
                    rightHandData.landmarks = positions;
                    rightHandData.isDetected = true;
                   
                }
            }
        }
        hasNewData = true;
    }
    private void UpdateHandVisualization(HandDataContainer handData, GameObject[] nodes)
    {
        if (!handData.isDetected || handData.landmarks == null || displayImage == null)
        {
            for (int i = 0; i < LANDMARK_COUNT; i++)
            {
                if (nodes[i] != null) nodes[i].SetActive(false);
            }
            return;
        }

        Vector3[] corners = new Vector3[4];
        displayImage.rectTransform.GetWorldCorners(corners);

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;

        for (int i = 0; i < LANDMARK_COUNT; i++)
        {
            Vector3 landmark = handData.landmarks[i];

            float normX = flipX ? (1.0f - landmark.x) : landmark.x;
            float normY = flipY ? landmark.y : (1.0f - landmark.y);

            float worldX = Mathf.Lerp(minX, maxX, normX);
            float worldY = Mathf.Lerp(minY, maxY, normY);
            float worldZ = displayImage.transform.position.z - 0.1f - (landmark.z * 0.2f);

            nodes[i].transform.position = new Vector3(worldX, worldY, worldZ);
            nodes[i].SetActive(true);
        }
    }


    private void ProcessAirWritingLogic()
    {
        if (airWriter == null || displayImage == null) return;

        HandDataContainer activeHand = rightHandData.isDetected ? rightHandData : (leftHandData.isDetected ? leftHandData : null);

        if (activeHand != null && activeHand.isDetected && activeHand.landmarks != null && activeHand.landmarks.Length >= 9)
        {
            // 1. HAM MEDIAPIPE KOORDİNATLARINDA (0..1 Arası) PINCH MESAFESİ HESAPLA
            Vector3 rawThumb = activeHand.landmarks[4];
            Vector3 rawIndex = activeHand.landmarks[8];
            float rawDistance = Vector3.Distance(rawThumb, rawIndex);

            // 0.08f eşiği ham koordinat uzayında son derece hassas ve hatasız çalışır
            bool isPinchActive = rawDistance < 0.08f;

            // 2. 3D DÜNYA KOORDİNATLARINI HESAPLA
            Vector3[] corners = new Vector3[4];
            displayImage.rectTransform.GetWorldCorners(corners);

            Vector3 thumbWorldPos = CalculateLandmarkWorldPosition(rawThumb, corners);
            Vector3 indexWorldPos = CalculateLandmarkWorldPosition(rawIndex, corners);
            
            // Çizimin video ekranının önünde kalması için z-offset'i kesinleştiriyoruz
            Vector3 drawWorldPoint = Vector3.Lerp(thumbWorldPos, indexWorldPos, 0.5f);

            // 3. AIR WRITER'A GÖNDER
            airWriter.ProcessAirWriting(drawWorldPoint, isPinchActive);
        }
    }

    private Vector3 CalculateLandmarkWorldPosition(Vector3 landmark, Vector3[] corners)
    {
        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;

        float normX = flipX ? (1.0f - landmark.x) : landmark.x;
        float normY = flipY ? landmark.y : (1.0f - landmark.y);

        float worldX = Mathf.Lerp(minX, maxX, normX);
        float worldY = Mathf.Lerp(minY, maxY, normY);
        
        Vector3 targetWorldPos = new Vector3(worldX, worldY, displayImage.transform.position.z);

        if (mainCamera != null)
        {
            return Vector3.Lerp(mainCamera.transform.position, targetWorldPos, 0.7f);
        }

        targetWorldPos.z -= 0.8f;
        return targetWorldPos;

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
