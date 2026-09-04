using System.Collections.Generic;
using UnityEngine;

public class ProceduralTubeWriter : MonoBehaviour
{
    [Header("Tube Mesh Settings")]
    [Tooltip("Tubes Radius")]
    [SerializeField] private float tubeRadius = 0.015f;
    [Tooltip("ideal 8-12")]
    [SerializeField] private int radialSegments = 8;
    [Tooltip("The minimum distance between points to add a new vertex to the tube. Lower values = more vertices, but smoother tubes.")]
    [SerializeField] private float minVertexDistance = 0.015f;
    [SerializeField] private Material metallicMaterial;

    private GameObject currentTubeObj;
    private Mesh currentMesh;
    private List<Vector3> pathPoints = new List<Vector3>();
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private bool isPinching = false;
    private Vector3 smoothedCursorPos;
    private Vector3 cursorVelocity = Vector3.zero;
    private float brushSmoothTime = 0.01f;
    private bool isFirstPoint = true;

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
                StartNewTube(smoothedCursorPos);
                isPinching = true;
                isFirstPoint = false;
            }
            else
            {
                UpdateTube(smoothedCursorPos);
            }
        }
        else
        {
            if (isPinching)
            {
                EndTube();
                isPinching = false;
                isFirstPoint = true;
            }
        }
    }

    private void StartNewTube(Vector3 startPoint)
    {
        currentTubeObj = new GameObject($"SpatialTube_{Time.time}");
        currentTubeObj.transform.position = Vector3.zero;
        currentTubeObj.transform.rotation = Quaternion.identity;

        MeshFilter mf = currentTubeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = currentTubeObj.AddComponent<MeshRenderer>();

        if (metallicMaterial != null)
        {
            mr.material = metallicMaterial;
        }
        else
        {
            Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mr.material = new Material(defaultShader) { color = Color.yellow};
        }

        currentMesh = new Mesh();
        mf.mesh = currentMesh;

        pathPoints.Clear();
        vertices.Clear();
        normals.Clear();
        triangles.Clear();

        pathPoints.Add(startPoint);
    }

    private void UpdateTube(Vector3 currentPoint)
    {
        if (pathPoints.Count == 0) return;

        Vector3 lastPoint = pathPoints[pathPoints.Count - 1];
        if (Vector3.Distance(lastPoint, currentPoint) >= minVertexDistance)
        {
            pathPoints.Add(currentPoint);
            GenerateTubeMesh();
        }
    }

    private void GenerateTubeMesh()
    {
        if (pathPoints.Count < 2) return;

        vertices.Clear();
        normals.Clear();
        triangles.Clear();

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 position = pathPoints[i];
            Vector3 forward = GetPathForward(i);
            Quaternion rotation = Quaternion.LookRotation(forward);

            
            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 localNormal = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 worldNormal = rotation * localNormal;
                Vector3 vertexPos = position + (worldNormal * tubeRadius);

                vertices.Add(vertexPos);
                normals.Add(worldNormal);
            }

           
            if (i > 0)
            {
                int ringStart = i * radialSegments;
                int prevRingStart = (i - 1) * radialSegments;

                for (int j = 0; j < radialSegments; j++)
                {
                    int nextJ = (j + 1) % radialSegments;

                    int currentRingV1 = ringStart + j;
                    int currentRingV2 = ringStart + nextJ;
                    int prevRingV1 = prevRingStart + j;
                    int prevRingV2 = prevRingStart + nextJ;

                    triangles.Add(prevRingV1);
                    triangles.Add(currentRingV1);
                    triangles.Add(prevRingV2);

                    triangles.Add(currentRingV2);
                    triangles.Add(prevRingV2);
                    triangles.Add(currentRingV1);
                }
            }
        }

        currentMesh.Clear();
        currentMesh.SetVertices(vertices);
        currentMesh.SetNormals(normals);
        currentMesh.SetTriangles(triangles, 0);

        currentMesh.RecalculateBounds();
        currentMesh.RecalculateNormals();
    }

    private Vector3 GetPathForward(int index)
    {
        if (pathPoints.Count == 1) return Vector3.forward;
        if (index == 0) return (pathPoints[1] - pathPoints[0]).normalized;
        if (index == pathPoints.Count - 1) return (pathPoints[index] - pathPoints[index - 1]).normalized;

        return ((pathPoints[index + 1] - pathPoints[index - 1])).normalized;
    }

    private void EndTube()
    {
        currentTubeObj = null;
        currentMesh = null;
    }
}
