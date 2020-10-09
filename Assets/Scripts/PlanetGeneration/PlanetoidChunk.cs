using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetoidChunk : MonoBehaviour
{
    private Planetoid Planet;
    private PlanetoidQuadTree _Tree;

    private Material PlanetMaterial;
    public Material HighlightMaterial;
    //public GameObject HousePrefab;

    private MeshRenderer MeshRen;
    public ChunkData _ChunkData;

    //public Shader WireframeShader;
    //public Shader TerrainShader;


    private Mesh GrassMesh;
    private Material GrassMaterial;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_Tree != null && _ChunkData.GrassData.Count>0 && Vector3.Distance(WorldSpaceLocation(), Planet.WorldSpacePlayerLocation()) <= 500)
        {
            //https://github.com/keijiro/NoiseBall3
            //https://github.com/Pandanym/GrassExperiments/tree/master/Assets

            //MeshData.GrassDataTemp
            /*foreach (List<Matrix4x4> GrassBatch in _ChunkData.GrassData)
            {
                if (GrassBatch.Count > 0)
                {
                    Graphics.DrawMeshInstanced(GrassMesh, 0, GrassMaterial, GrassBatch, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
                }
            }*/

           

        } 
    }

    public void Instantiate(Planetoid planet, ChunkData chunkData, PlanetoidQuadTree tree, Material grassMaterial)
    {
        _ChunkData = chunkData;
        Planet = planet;

        MeshRen = gameObject.AddComponent<MeshRenderer>();
        MeshFilter Filter = gameObject.AddComponent<MeshFilter>();
        Filter.sharedMesh = _ChunkData._Mesh;

        PlanetMaterial = new Material(Shader.Find("Shader Graphs/TerrainShader"));
        PlanetMaterial.SetFloat("_maxHeight", _ChunkData.MaxHeight);
        PlanetMaterial.SetFloat("_radius", Planet.SizeSetting.Radius);
        PlanetMaterial.SetVector("_planetLocation", Planet.transform.position);

        GrassMaterial = grassMaterial;

        GrassMesh = Resources.Load<Mesh>("Grass_Mesh");

        //HighlightMaterial = new Material(Shader.Find("VR/SpatialMapping/Wireframe"));

        //PlanetMaterial = new Material(TerrainShader);
        //HighlightMaterial = new Material(WireframeShader);

        Tree = tree;
       
        MeshCollider Collider = gameObject.AddComponent<MeshCollider>();
        Collider.sharedMesh = _ChunkData._Mesh;
        Collider.convex = false;
        //tree.Resolution != 2;
        //Collider.convex =true;

        /*foreach(HouseCompute House in meshData.HouseLocations)
        {
            Vector3 AxisA = new Vector3(House.DirectionOrientation.y, House.DirectionOrientation.z, House.DirectionOrientation.x);
            Vector3 AxisB = Vector3.Cross(House.DirectionOrientation, AxisA);

            Quaternion LookRotation = Quaternion.LookRotation(AxisB,House.DirectionOrientation);
            //LookRotation *= Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);

            Vector3 Position = (House.Position + transform.position) - (House.DirectionOrientation*0.5f);

            GameObject houseObj = Instantiate(HousePrefab,Position, LookRotation);

            houseObj.transform.parent = transform;
        }*/

    }

    public Vector3 WorldSpaceLocation()
    {
        return _ChunkData.VertData[_ChunkData.VertData.Length / 2] + Planet.transform.position;
    }

    public void UpdateMeshColor(Color[] colors)
    {
        gameObject.GetComponent<MeshFilter>().sharedMesh.colors = colors;
    }

    public PlanetoidQuadTree Tree
    {
       get
       {
            return _Tree;
       }

       set
       {
            _Tree = value;

            if(Planet.WireframeMode)
            {
                MeshRen.material = HighlightMaterial;
            }
            else
            {
                MeshRen.material = PlanetMaterial;
            }

            /*if (_Tree.IsPlayerTree || Planet.WireframeMode)
            {
                MeshRen.material = HighlightMaterial;
            }
            else
            {
                //if(_Tree.Id.Equals("UP_FACE-00-01-10"))
                    //Debug.Log(MeshData.TextureData.GetPixel(0, 0) + " " + MeshData.TextureData.GetPixel(0, 4)+" " + MeshData.TextureData.GetPixel(0, 5) + " " + MeshData.TextureData.GetPixel(0, 6) + " " + MeshData.TextureData.GetPixel(0, 7));
                //PlanetMaterial.SetTexture("_UVHeightTexture", MeshData.TextureData);
                
                MeshRen.material = PlanetMaterial;
            }*/
        }
    }
}
