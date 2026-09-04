using System;
using UnityEngine;
using Unity.Sentis;
using UnityEngine.Timeline;


public class DepthEstimationRunner : MonoBehaviour
    {
        [Header("AI Model Settings")]
        [SerializeField] private ModelAsset depthModelAsset;
        [SerializeField] private WebcamController webcamController;
        [Header("Model Input Resulition")]
        [SerializeField] private int modelWidth = 256;
        [SerializeField] private int modelHeight = 256;

        [Header("Depth Scaling (Metre)")]
        [Tooltip("the minimum distance in meters for the closest object in the room to the camera")]
        [SerializeField] private float minDepthMeters = 0.5f;
        [Tooltip("the maximum distance in meters for the farthest object in the room to the camera")]
        [SerializeField] private float maxDepthMeters = 4.0f;

        private Model runtimeModel;
        private Worker worker;
        private Tensor<float> inputTensor;
        private float[] depthData;
        private bool isModelLoaded = false;

        private void Start()
        {
            InitializeSentisModel();
        }

        private void InitializeSentisModel()
        {
            if (depthModelAsset == null)
            {
                Debug.LogWarning("model is not assigned. Depth estimation will not work.");
                return;
            }

            try
            {
                runtimeModel = ModelLoader.Load(depthModelAsset); 
        
                // Maximum performance is achieved by using GPUCompute backend for inference
                worker = new Worker(runtimeModel, BackendType.GPUCompute);
                inputTensor = new Tensor<float>(new TensorShape(1, 3, modelHeight, modelWidth));
                depthData = new float[modelWidth * modelHeight];

                isModelLoaded = true;
                Debug.Log($"[Sentis AI] Depth Estimation Model Loaded! Input Resolution: ({modelWidth}x{modelHeight})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Sentis AI] Model loading error: {e.Message}");
            }
        }

        private void Update()
        {
            if (!isModelLoaded || webcamController == null || !webcamController.IsPlaying) return;

            WebCamTexture webCamTex = webcamController.WebcamTexture;
            if (webCamTex.width < 100) return;

            RunDepthInference(webCamTex);
        }

        private void RunDepthInference(WebCamTexture sourceTexture)
        {
            // 1. Converts the webcam texture to a tensor for the model input
            TextureConverter.ToTensor(sourceTexture, inputTensor, new TextureTransform());
            // 2. Execute the model inference
            worker.Schedule(inputTensor);

            // 3. Get the output tensor and copy the depth data to the depthData array
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            if (outputTensor != null)
            {
                var array = outputTensor.DownloadToArray();

                if (array.Length == depthData.Length)
                {
                    Array.Copy(array, depthData, array.Length);
                }
            }
            
        }

        /// <summary>
        /// Returns the estimated depth in meters at the given normalized UV coordinates (0 to 1) of the camera feed.
        /// If the model is not loaded or depth data is unavailable, it returns -1.
        /// </summary>
        public float GetDepthAtUV(float normX, float normY)
        {
            if (!isModelLoaded || depthData == null || depthData.Length == 0)
            {
                return -1f;
            }

            int x = Mathf.Clamp(Mathf.FloorToInt(normX * modelWidth), 0, modelWidth - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((1f - normY) * modelHeight), 0, modelHeight - 1);

            int index = y * modelWidth + x;
            if (index >= 0 && index < depthData.Length)
            {
                float rawDepthValue = depthData[index];
                
                // Map the raw depth value to the specified depth range in meters
                return Mathf.Lerp(minDepthMeters, maxDepthMeters, rawDepthValue);
            }

            return -1f;
        }

        private void OnDestroy()
        {
            worker?.Dispose();
        }
    }