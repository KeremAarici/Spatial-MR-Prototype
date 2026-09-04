using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Room3DReconstructor : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DepthEstimationRunner depthRunner;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private Camera mainCamera;

    [Header("Mesh Res")]
    [SerializeField] private int gridWidth = 64;
    [SerializeField] private int gridHeight = 64;

    [Header("Scan mode")]
    [SerializeField] private bool liveUpdate = true;


    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;


    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        CreateDefaultMaterial();
        InitializeGridMesh();
    }

    private void CreateDefaultMaterial()
    {
        Shader defaultShader = Shader.Find("Universal Render Pipeline/Unlit") 
                            ?? Shader.Find("Unlit/Texture") 
                            ?? Shader.Find("Standard");

        Material mat = new Material(defaultShader);
        meshRenderer.material = mat;
    }


    private void InitializeGridMesh()
    {
        mesh = new Mesh();
        mesh.name = "ReconstructedRoomMesh";
        GetComponent<MeshFilter>().mesh = mesh;

        int numVertices = gridWidth * gridHeight;
        vertices = new Vector3[numVertices];
        uvs = new Vector2[numVertices];


        // Create Triangles
        List<int> triList = new List<int>();
        for (int y = 0; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth - 1; x++)
            {
                int current = y * gridWidth + x;
                int nextRow = (y + 1) * gridWidth + x;

                //Triangle 1
                triList.Add(current);
                triList.Add(nextRow);
                triList.Add(current + 1);

                //Triangle 2
                triList.Add(current + 1);
                triList.Add(nextRow);
                triList.Add(nextRow + 1);
            }
        }

        triangles = triList.ToArray();


        //Calculate the UV Coordinates
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                uvs[index] = new Vector2((float)x / (gridWidth - 1), (float)y / (gridHeight - 1));
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
    }

    void Update()
    {
        if (displayImage != null && displayImage.texture != null && meshRenderer.material.mainTexture == null)
        {
            meshRenderer.material.mainTexture = displayImage.texture;
        }

        if (liveUpdate)
        {
            ReconstructRoomGeometry();
        }
    }

    /// <summary>
    /// Updating the 3d room mesh by using the depth estimation datas.
    /// <summary>
    public void ReconstructRoomGeometry()
    {
        if (depthRunner == null || displayImage == null) return;

        Vector3[] corners = new Vector3[4];
        displayImage.rectTransform.GetWorldCorners(corners);


        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;


        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                Vector2 uv = uvs[index];

                
                float worldX = Mathf.Lerp(minX, maxX, uv.x);
                float worldY = Mathf.Lerp(minY, maxY, uv.y);

                
                float depthInMeters = depthRunner.GetDepthAtUV(uv.x, uv.y);

                float worldZ;
                if (depthInMeters > 0)
                {
                    
                    worldZ = displayImage.transform.position.z - depthInMeters;
                }
                else
                {
                    worldZ = displayImage.transform.position.z;
                }

                vertices[index] = new Vector3(worldX, worldY, worldZ);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void CaptureStaticRoomScan()
    {
        liveUpdate = false;
        ReconstructRoomGeometry();
    }
}
