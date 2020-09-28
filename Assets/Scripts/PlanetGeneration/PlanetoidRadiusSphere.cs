using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetoidRadiusSphere : MonoBehaviour
{

    Vector3 StartScale;

    void Start()
    {
        StartScale = transform.localScale;

        /*Vector3[] Normals = mesh.normals;
        for (int i = 0; i < Normals.Length; i++)
        {
            Normals[i] = (Vector3.zero - Normals[i]).normalized;
        }

        mesh.normals = Normals;*/


        
    }

    public void InvertTriangles()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = filter.mesh;
        int[] Tris = mesh.triangles;
        for (int i = 0; i < Tris.Length; i += 3)
        {
            int T1 = Tris[i];
            int T3 = Tris[i + 2];

            Tris[i] = T3;
            Tris[i + 2] = T1;
        }
        mesh.triangles = Tris;
    }
}
