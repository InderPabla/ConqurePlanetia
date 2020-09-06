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
    public MeshGeneratorData MeshData;

    //public Shader WireframeShader;
    //public Shader TerrainShader;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_Tree != null)
        {

        } 
    }

    public void Instantiate(Planetoid planet, MeshGeneratorData meshData, PlanetoidQuadTree tree)
    {
        MeshData = meshData;
        Planet = planet;

        MeshRen = gameObject.AddComponent<MeshRenderer>();
        MeshFilter Filter = gameObject.AddComponent<MeshFilter>();
        Filter.sharedMesh = meshData._Mesh;

        PlanetMaterial = new Material(Shader.Find("Shader Graphs/TerrainShader"));
        //HighlightMaterial = new Material(Shader.Find("VR/SpatialMapping/Wireframe"));

        //PlanetMaterial = new Material(TerrainShader);
        //HighlightMaterial = new Material(WireframeShader);

        Tree = tree;
       
        MeshCollider Collider = gameObject.AddComponent<MeshCollider>();
        Collider.sharedMesh = meshData._Mesh;
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
