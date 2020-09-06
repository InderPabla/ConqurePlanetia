using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public PlanetoidChunk PlanetoidChunkPrefab;
    public GameObject RadiusSpherePrefab;
    public ComputeShader MeshCompute;

    public Gradient ColorGradient;
    public PlanetoidSizeSetting SizeSetting;
    public SimpleNoiseSetting[] NoiseSettings;
    public bool EnableGravity = false;
    public bool IgnoreNoiseSetting = false;
    public bool WireframeMode = false;
    public bool HideRangeShell = false;

    private PlanetoidMapEngine Engine;
    private NoiseGenerator GenNoise;
    private MeshGenerator GenMesh;
    private PlanetoidPlayer Player;

    private Rigidbody RigPlanet;
    private PlanetoidMapEngineOperation Operations;

    private float GRAVITY_CONST = 1f;

    private GameObject[] RangeShellSphere;

    private List<PlanetoidQuadTree> CreateList;
    private List<PlanetoidQuadTree> UpdateList;
    private List<PlanetoidQuadTree> DeleteList;

    private bool CreateImmediate = false;


    public Planetoid()
    {
        Operations = new PlanetoidMapEngineOperation(UpdateTree, CreateTree,DeleteTree
            ,WorldSpacePlayerLocation,WorldSpacePlanetLocation
            ,IsTreeCreated,TreeUnderPlayer);

        CreateList = new List<PlanetoidQuadTree>();
        UpdateList = new List<PlanetoidQuadTree>();
        DeleteList = new List<PlanetoidQuadTree>();
    }


    void Start()
    {
        SizeSetting.Position = transform.position;

        ColorGradient = new Gradient();

        System.Random Rand = new System.Random(SizeSetting.Seed);

        GradientColorKey[] colorKeys = new GradientColorKey[UnityEngine.Random.Range(3, 9)];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        for (int i = 0; i < colorKeys.Length; i++)
        {
            float rand = (float)Rand.NextDouble();

            float time = 0;

            if(i>0)
            {
                if(i==1)
                {
                    time = 0.25f * rand;
                }
                else
                {
                    time = ((1f - colorKeys[i - 1].time) * rand) + colorKeys[i - 1].time;
                }
            }

            Color color = Color.HSVToRGB((float)Rand.NextDouble(), (float)Rand.NextDouble() * 0.1f + 0.9f, (float)Rand.NextDouble() * 0.1f + 0.9f);
            colorKeys[i] = new GradientColorKey(color,time);
        }

        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i] = new GradientAlphaKey(1f, i);
        }


        ColorGradient.SetKeys(colorKeys, alphaKeys);
        ColorGradient.mode = GradientMode.Blend;
        
        name = SizeSetting.PlanetName;
        Player = FindObjectOfType<PlanetoidPlayer>();
        
        RigPlanet = GetComponent<Rigidbody>();

        CreateImmediate = true;
        Init();
        CreateImmediate = false;

       
    }

    private void Init()
    {
        RigPlanet.mass = SizeSetting.Mass;

        Debug.Log(SizeSetting.ToString());

        Engine = new PlanetoidMapEngine(SizeSetting, Operations);

        GenNoise = new NoiseGenerator(NoiseSettings, Engine.Solution, IgnoreNoiseSetting);
        GenMesh = new MeshGenerator(SizeSetting, GenNoise, Engine.Solution,MeshCompute, ColorGradient);
        Engine.Init();

        InitRaycastObjectsOnSurface();

        //InitRangeShellSphere();
        if (!HideRangeShell) InitAtmosphereSphere();
    }

    private void InitRaycastObjectsOnSurface()
    {
        float Radius = SizeSetting.Radius;
        float Circumference = SizeSetting.Circumference;
        float RadiansPerMeter = SizeSetting.RadiansPerMeter;

        Vector3 PlanetCenter = WorldSpacePlanetLocation();
        Vector3 AboveRadius = new Vector3(SizeSetting.Radius, 0,0);

        for (int i = 0; i < 160; i++)
        {
            float RadiansPerMeterUpdate = i * RadiansPerMeter;
            float Xupdate = Radius * Mathf.Cos(RadiansPerMeterUpdate);
            float Yupdate = Radius * Mathf.Sin(RadiansPerMeterUpdate);

            Vector3 UpdatedLocationOnSphere = AboveRadius;
            UpdatedLocationOnSphere.x = Xupdate;
            UpdatedLocationOnSphere.y = Yupdate;

            Vector3 DirectionToPlanet = (Vector3.zero-UpdatedLocationOnSphere).normalized;
            Vector3 DirectionFromPlanet = UpdatedLocationOnSphere.normalized;
            //Vector3 UpdatedLocationOnGeom = UpdatedLocationOnSphere + PlanetCenter;

            RaycastHit Hit;
            int IgnoreLayer = ~(1 << LayerMask.NameToLayer("PlayerMask"));
            int SelectionLayer = 1 << LayerMask.NameToLayer("PlanetoidChunkMask");

            if (Physics.Raycast((DirectionFromPlanet * (SizeSetting.Radius+100+GenNoise._MaxNoise))+ PlanetCenter, DirectionToPlanet, out Hit, 1000f, IgnoreLayer | SelectionLayer))
            {
                if (Hit.collider.transform.GetComponent<PlanetoidChunk>() != null)
                {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.position = Hit.point;

                    Vector3 AxisA = new Vector3(DirectionToPlanet.y, DirectionToPlanet.z, DirectionToPlanet.x);
                    Vector3 AxisB = Vector3.Cross(DirectionToPlanet, AxisA);

                    Quaternion LookRotation = Quaternion.LookRotation(AxisB, DirectionToPlanet);
                    obj.transform.rotation = LookRotation;
                    obj.transform.position -= DirectionToPlanet;
                    obj.name = "Cube" + (i+1);
                    obj.transform.parent = transform;
                }
            }

            


            
            
        }
    }

    private void InitRangeShellSphere()
    {
        RangeShellSphere = new GameObject[(int)PlanetoidRenderType.PLAYER_VERY_FAR + 1];
        float Radius = SizeSetting.Radius;
        GameObject ParentSphere = new GameObject();
        ParentSphere.name = "RangeSpheres";
        ParentSphere.transform.position = transform.position;
        ParentSphere.transform.parent = transform;
        for (int i = 0; i <= (int)PlanetoidRenderType.PLAYER_VERY_FAR; i++)
        {
            Color color = Color.HSVToRGB((float)i / (float)PlanetoidRenderType.PLAYER_VERY_FAR, 1f, 1f);
            color.a = 0.1f;

            float Diameter = PlanetoidSizeSetting.RenderTypeDiameter((PlanetoidRenderType)i, Radius)*2;

            RangeShellSphere[i] = GenerateRangeSphere(color, Diameter, ParentSphere.transform);
        }
;   }

    private void InitAtmosphereSphere()
    {
        int numberOfSpheres = UnityEngine.Random.Range(15, 25);
        float DistanceIncrease = UnityEngine.Random.Range(12, 15);
        RangeShellSphere = new GameObject[numberOfSpheres];
        float Radius = SizeSetting.Radius;
        float Diameter = PlanetoidSizeSetting.RenderTypeDiameter(PlanetoidRenderType.PLAYER_ON_PLANET, Radius) * 2.5f;
        float Bias = UnityEngine.Random.Range(0f, 1f);
        GameObject ParentSphere = new GameObject();
        ParentSphere.name = "RangeSpheres";
        ParentSphere.transform.position = transform.position;
        ParentSphere.transform.parent = transform;
        for (int i = 0; i < numberOfSpheres; i++)
        {
            float hue = (UnityEngine.Random.Range(0f, 1f) + Bias) * Bias;
            Color color = Color.HSVToRGB(hue > 1f? 1f: hue, 1f,1f);
            color.a = UnityEngine.Random.Range(0.04f,0.1f);
            float DiameterForSphere = Diameter + (DistanceIncrease * i);

            RangeShellSphere[i] = GenerateRangeSphere(color, DiameterForSphere, ParentSphere.transform);
        }
;
    }

    private GameObject GenerateRangeSphere(Color color, float Diameter, Transform Parent)
    {

        GameObject RangeShellSphere = Instantiate(RadiusSpherePrefab);
        RangeShellSphere.transform.localScale = new Vector3(Diameter, Diameter, Diameter);
        RangeShellSphere.transform.position = transform.position;
        Destroy(RangeShellSphere.GetComponent<SphereCollider>());

        //https://answers.unity.com/questions/1608815/change-surface-type-with-lwrp.html?_ga=2.207187795.1187703980.1596923527-2044146239.1595823173
        MeshRenderer Ren = RangeShellSphere.GetComponent<MeshRenderer>();
        Ren.receiveShadows = false;
        Ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Material Mat = new Material(Shader.Find("Lightweight Render Pipeline/Simple Lit"));
        Mat.SetColor("_BaseColor", color);
        // wallMaterial.SetFloat("_Surface", (float)SurfaceType.Opaque);
        Mat.SetFloat("_Surface", 1f);
        Mat.SetOverrideTag("RenderType", "Transparent");
        Mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        Mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        Mat.SetInt("_ZWrite", 0);
        Mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        Mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Mat.SetShaderPassEnabled("ShadowCaster", false);

        Ren.material = Mat;

        RangeShellSphere.name = name + "-RangeSphere-" + (int)Diameter + "m";
        RangeShellSphere.transform.parent = Parent;

        return RangeShellSphere;
    }


    void Update()
    {
        Engine.Update();
    }

 

    void FixedUpdate()
    {
        if (EnableGravity)
        {
           
            float otherMass = Player.RigPlayer.mass;
            Vector3 otherPos = Player.RigPlayer.transform.position;
            Vector3 direction = Vector3.Normalize(otherPos - transform.position);
            float distance = Vector3.Distance(transform.position, otherPos);

            /*Vector3 PlanetLocation = WorldSpacePlanetLocation();
            Vector3 PlayerLocation = WorldSpacePlayerLocation();
            RaycastHit Hit;
            int IgnoreLayer = ~(1 << LayerMask.NameToLayer("PlayerMask"));
            int SelectionLayer = 1 << LayerMask.NameToLayer("PlanetoidChunkMask");
            if (Physics.Raycast(PlayerLocation, Direction, out Hit, 1000f, IgnoreLayer | SelectionLayer))
            {
                if (Hit.collider.transform.GetComponent<PlanetoidChunk>() != null)
                {
                    distance = Hit.distance;
                }
            }*/

            float force = (GRAVITY_CONST * RigPlanet.mass * otherMass) / Mathf.Pow(distance, 2);

            Vector3 forceVector = (direction * force) * Time.fixedDeltaTime;
            Player.RigPlayer.AddForce(-forceVector);

        }
    }

    public void UpdateTree(PlanetoidQuadTree Tree)
    {
        /*PlanetoidChunk Chunk = ChunkFromId(NewTree.Id);
        PlanetoidQuadTree OldTree = Chunk.Tree;

        PlanetoidQuadTree UpNew = NewTree.TreeManager.FindNeighbourUp(NewTree);
        PlanetoidQuadTree DownNew = NewTree.TreeManager.FindNeighbourDown(NewTree);
        PlanetoidQuadTree LeftNew = NewTree.TreeManager.FindNeighbourLeft(NewTree);
        PlanetoidQuadTree RightNew = NewTree.TreeManager.FindNeighbourRight(NewTree);

        PlanetoidQuadTree UpOld = OldTree.TreeManager.FindNeighbourUp(OldTree);
        PlanetoidQuadTree DownOld = OldTree.TreeManager.FindNeighbourDown(OldTree);
        PlanetoidQuadTree LeftOld = OldTree.TreeManager.FindNeighbourLeft(OldTree);
        PlanetoidQuadTree RightOld = OldTree.TreeManager.FindNeighbourRight(OldTree);

        //if (NewTree.Id.Equals("UP_FACE-00-00-10-01-10"))
        //{
           // Debug.Log("UPDATING "+"UP_FACE-00-00-10-01-10");
           // Debug.Log(NewTree + " " + OldTree);
           // Debug.Log(UpNew+" "+UpOld);
          //  Debug.Log(DownNew + " " + DownOld);
           // Debug.Log(LeftNew + " " + LeftOld);
            //Debug.Log(RightNew + " " + RightOld);
            
        //}

        if (OldTree.Resolution == NewTree.Resolution
            && (   PlanetoidQuadTree.EqualsWithResolution(UpNew, UpOld)
                && PlanetoidQuadTree.EqualsWithResolution(DownNew, DownOld)
                && PlanetoidQuadTree.EqualsWithResolution(LeftNew, LeftOld)
                && PlanetoidQuadTree.EqualsWithResolution(RightNew, RightOld)
               )
        ) {
            Chunk.Tree = NewTree;
            return false;
        }*/

        DeleteTree(Tree);
        CreateTree(Tree);
  
        //Debug.Log(NewTree.Id + " Updated.");
    }

    public void CreateTree(PlanetoidQuadTree Tree)
    {
        PlanetoidChunk NewChunk = Instantiate(PlanetoidChunkPrefab, transform.position, Quaternion.identity);
        NewChunk.transform.parent = transform;
        NewChunk.name = Tree.Id;

        //Mesh NewMesh = DEBUG_UseCompute?GenMesh.GenerateMeshComputeShader(Tree):GenMesh.GenerateMeshManual(Tree);
        MeshGeneratorData MeshData = GenMesh.GenerateMeshComputeShader(Tree);

        NewChunk.Instantiate(this, MeshData, Tree);

        //Debug.Log(Tree.Id+" Created.");
    }

    public void DeleteTree(PlanetoidQuadTree Tree)
    {
        DeleteTreeById(Tree.Id);
        //Debug.Log(Tree.Id + " Destoyed.");
    }

    public void DeleteTreeById(string Id)
    {
        Destroy(transform.Find(Id).gameObject);
    }

    public bool IsTreeCreated(PlanetoidQuadTree Tree)
    {
        return ChunkFromId(Tree.Id) != null;
    }

    public PlanetoidChunk ChunkFromId(string Id)
    {
        Transform ChunkTransform = transform.Find(Id);
        if (ChunkTransform == null)
            return null;

        return ChunkTransform.GetComponent<PlanetoidChunk>();
    }

    public Vector3 WorldSpacePlayerLocation()
    {
        return Player.transform.position;
    }

    public Vector3 WorldSpacePlanetLocation()
    {
        return transform.position;
    }

    public PlanetoidQuadTree TreeUnderPlayer()
    {
        Vector3 PlanetLocation = WorldSpacePlanetLocation();
        Vector3 PlayerLocation = WorldSpacePlayerLocation();

        RaycastHit Hit;
        Vector3 Direction = (PlanetLocation - PlayerLocation).normalized;

        int IgnoreLayer = ~(1 << LayerMask.NameToLayer("PlayerMask"));
        int SelectionLayer = 1 << LayerMask.NameToLayer("PlanetoidChunkMask");

        if (Physics.Raycast(PlayerLocation, Direction, out Hit, 1000f, IgnoreLayer | SelectionLayer))
        {
            PlanetoidChunk Chunk = Hit.collider.gameObject.GetComponent<PlanetoidChunk>();
            if (Chunk == null)
            {
                Debug.DrawLine(PlayerLocation, PlanetLocation, Color.red);
                return null;
            }
            Debug.DrawLine(PlayerLocation, Hit.point, Color.green);
            return Chunk.Tree;
        }
        else
        {
            Debug.DrawLine(PlayerLocation, PlanetLocation, Color.red);
        }

        return null;
    }

}
