using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetoidChunk : MonoBehaviour
{
    private MeshGenerator MeshGen;
    private Planetoid Parent;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Instantiate(Planetoid Planet, MeshGenerator MeshGen)
    {
        this.MeshGen = MeshGen;
        Parent = Planet;

        MeshRenderer MeshRen = gameObject.AddComponent<MeshRenderer>();
        MeshFilter Filter = gameObject.AddComponent<MeshFilter>();
        Filter.sharedMesh = MeshGen.Mesh;


        MeshRen.material = new Material(Shader.Find("Shader Graphs/TerrainShader"));


        MeshCollider Collider = gameObject.AddComponent<MeshCollider>();
        Collider.sharedMesh = MeshGen.Mesh;
        Collider.convex = true;
    }
}
