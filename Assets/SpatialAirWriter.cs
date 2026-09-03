using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialAirWriter : MonoBehaviour
{
    [Header("Stylus Stabilization Settings")]
    [Tooltip("Pen comes to rest at the new position over this time. Lower values = more responsive, but less stable.")]
    [SerializeField] private float brushSmoothTime = 0f; 
    [Tooltip("Minimum distance between points to add a new vertex to the line. Lower values = more vertices, but smoother lines.")]
    [SerializeField] private float minVertexDistance = 0.015f;

    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float startWidth = 0.15f;
    [SerializeField] private float endWidth = 0.15f;
    [SerializeField] private Color defaultLineColor = Color.green;

    private LineRenderer currentLineRenderer;
    private List<Vector3> currentStrokePoints = new List<Vector3>();
        
    // Stabilizasyon Değişkenleri
    private bool isPinching = false;
    private Vector3 smoothedCursorPos;
    private Vector3 cursorVelocity = Vector3.zero;
    private bool isFirstPoint = true; // Yeni çizgiye başlarken kalemi doğrudan ele oturtmak için

    public void ProcessAirWriting(Vector3 rawDrawPointWorld, bool isPinchActive)
    {
        
        if (isFirstPoint)
        {
            smoothedCursorPos = rawDrawPointWorld;
        }
        else
        {
            smoothedCursorPos = Vector3.SmoothDamp(smoothedCursorPos, rawDrawPointWorld, ref cursorVelocity, brushSmoothTime);
        }

            
        if (isPinchActive)
        {
            if (!isPinching)
            {
                StartNewStroke(smoothedCursorPos);
                isPinching = true;
                isFirstPoint = false;
            }
            else
            {
                UpdateStroke(smoothedCursorPos);
            }
        }
        else
        {
            if (isPinching)
            {
                EndStroke();
                isPinching = false;
                isFirstPoint = true;
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
            

        currentLineRenderer.numCornerVertices = 8;
        currentLineRenderer.numCapVertices = 8;
        currentLineRenderer.sortingOrder = 100;

        if (lineMaterial != null)
        {
            currentLineRenderer.material = lineMaterial;
        }
        else
        {
            Material unlitMat = new Material(Shader.Find("Sprites/Default"));
            unlitMat.color = defaultLineColor;
            currentLineRenderer.material = unlitMat;
        }
        currentStrokePoints.Clear();
        AddPointToStroke(startPoint);
    }

    private void UpdateStroke(Vector3 currentPoint)
    {
        if (currentStrokePoints.Count == 0) return;

        Vector3 lastPoint = currentStrokePoints[currentStrokePoints.Count - 1];
            
        // Eğer kalem yeterince hareket ettiyse noktayı ekle
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
}
