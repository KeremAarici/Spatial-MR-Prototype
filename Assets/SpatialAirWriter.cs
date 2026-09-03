using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialAirWriter : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("Threshold distance between the thumb and index finger for pinch detection (Normalized in UI space)")]
    [SerializeField] private float pinchThreshold = 0.05f;

    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float startWidth = 0.15f;
    [SerializeField] private float endWidth = 0.15f;
    [SerializeField] private float minVertexDistance = 0.005f;
    [SerializeField] private Color defaultLineColor = Color.cyan;

    [Header("Depth Baseline")]
    [Tooltip("Default depth (Z) value for the hand landmarks when no depth information is available.")]
    [SerializeField] private float defaultDepthZ = 2.0f;

    private LineRenderer currentLineRenderer;
    private List<Vector3> currentStrokePoints = new List<Vector3>();
    private bool isPinching = false;


    /// <summary>
    /// Processes the air writing input based on the positions of the thumb tip and index finger tip.
    /// If the distance between the two points is less than the pinch threshold, it starts or
    /// updates the current stroke.
    /// </summary>
    /// <param name="drawPointWorld">The world position of the drawing point.</param>
    /// <param name="isPinchActive">Indicates whether the pinch gesture is active.</param>

    public void ProcessAirWriting(Vector3 drawPointWorld, bool isPinchActive)
    {
        if (isPinchActive)
        {
            if (!isPinching)
            {
                StartNewStroke(drawPointWorld);
                isPinching = true;
            }
            else
            {
                UpdateStroke(drawPointWorld);
            }
        }
         else
        {
            if (isPinching)
            {
                EndStroke();
                isPinching = false;
            }
        }
    }

    private void StartNewStroke(Vector3 startPoint)
    {
        GameObject lineObj = new GameObject($"SpatialStroke_{Time.time}");
        lineObj.transform.SetParent(transform);

        currentLineRenderer = lineObj.AddComponent<LineRenderer>();
        currentLineRenderer.startWidth = startWidth;
        currentLineRenderer.endWidth = endWidth;
        currentLineRenderer.positionCount = 0;
        currentLineRenderer.useWorldSpace = true;
        currentLineRenderer.numCornerVertices = 5;
        currentLineRenderer.numCapVertices = 5;
        currentLineRenderer.sortingOrder = 100;

        Material unlitMat = new Material(Shader.Find("Unlit/Color"));
        unlitMat.color = defaultLineColor;
        currentLineRenderer.material = unlitMat;

        if (lineMaterial != null)
        {
            currentLineRenderer.material = lineMaterial;
        }
        else
        {
            currentLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        currentLineRenderer.startColor = defaultLineColor;
        currentLineRenderer.endColor = defaultLineColor;

        currentStrokePoints.Clear();
        AddPointToStroke(startPoint);
    }

    private void UpdateStroke(Vector3 currentPoint)
    {
        if (currentStrokePoints.Count == 0) return;

        Vector3 lastPoint = currentStrokePoints[currentStrokePoints.Count - 1];
        if (Vector3.Distance(lastPoint, currentPoint) >= minVertexDistance)
        {
            AddPointToStroke(currentPoint);
        }
    }

    private void AddPointToStroke(Vector3 point)
    {
        currentStrokePoints.Add(point);
        currentLineRenderer.positionCount = currentStrokePoints.Count;
        currentLineRenderer.SetPosition(currentStrokePoints.Count - 1, point);
    }

    private void EndStroke()
    {
        currentLineRenderer = null;
        currentStrokePoints.Clear();
    }

    /// <summary>
    /// Clears all existing strokes from the scene.
    /// </summary>
    public void ClearAllStrokes()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
