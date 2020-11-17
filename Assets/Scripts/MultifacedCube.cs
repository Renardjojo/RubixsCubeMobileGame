using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MultifacedCube : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CreateMultifacedCube();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void CreateMultifacedCube()
    {
        Vector3[] vertices =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
        };
        
        Vector2[] uv =
        {
            new Vector3(0, 0 ),
            new Vector3(1, 0 ),
            new Vector3(1, 1),
            new Vector3(0, 1),
            new Vector3(0, 1),
            new Vector3(1, 1),
            new Vector3(1, 0),
            new Vector3(0, 0),
        };

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        
        int[] triangles =
        {
            0, 2, 1, //face front
            0, 3, 2
        };
        mesh.SetTriangles(triangles, 0);

        triangles = new int[]
        {
            2, 3, 4, //face top
            2, 4, 5
        };

        mesh.SetTriangles(triangles, 1);

        triangles = new int[]
        {
            1, 2, 5, //face right
            1, 5, 6
        };

        mesh.SetTriangles(triangles, 2);

        triangles = new int[]
        {
            0, 7, 4, //face left
            0, 4, 3
        };

        mesh.SetTriangles(triangles, 3);

        triangles = new int[]
        {
            5, 4, 7, //face back
            5, 7, 6
        };

        mesh.SetTriangles(triangles, 4);

        triangles = new int[]
        {
            0, 6, 7, //face bottom
            0, 1, 6
        };

        mesh.SetTriangles(triangles, 5);
        mesh.subMeshCount = 6;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.Optimize();
        mesh.RecalculateNormals();
    }
}