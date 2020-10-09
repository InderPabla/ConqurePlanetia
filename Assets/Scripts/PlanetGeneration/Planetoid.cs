using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum CoroutineState
{
    RUNNING, COMPLETED, STOPPED
}

public class Planetoid : MonoBehaviour
{
    public PlanetoidChunk PlanetoidChunkPrefab;
    public GameObject DebirsPrefab;
    public PlanetoidRadiusSphere RadiusSpherePrefab;
    public ComputeShader MeshCompute;

    public Gradient ColorGradient;
    public PlanetoidSizeSetting SizeSetting;
    public SimpleNoiseSetting[] NoiseSettings;
    public bool EnableGravity = false;
    public bool IgnoreNoiseSetting = false;
    public bool WireframeMode = false;
    public bool HideRangeShell = false;
    public Font TextFont;

    private PlanetoidMapEngine Engine;

    private PlanetoidPlayer Player;

    private Rigidbody RigPlanet;
    private PlanetoidMapEngineOperation Operations;

    private float GRAVITY_CONST = 1f;

    private GameObject[] RangeShellSphere;

    private Material GrassMaterial;

    private Mesh CircleMesh;
    public Mesh FirTreeMesh;
    public Mesh OakTreeMesh;
    public Mesh GrassMesh;


    private Material CircleMat;
    private Material OakTreeMat;
    private Material FirTreeMat;

    private Canvas _Canvas;
    private Text _Text;

    public GameObject TempCubePrefab;

    private List<List<Matrix4x4>> FirTreeRenderList = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> FirTreeRenderListTemp = new List<List<Matrix4x4>>();

    private List<List<Matrix4x4>> OakTreeRenderList = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> OakTreeRenderListTemp = new List<List<Matrix4x4>>();

    private List<List<Matrix4x4>> GrassTreeRenderList = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> GrassTreeRenderListTemp = new List<List<Matrix4x4>>();


    private System.Diagnostics.Stopwatch Watch;


    private bool QueuedTreeRoutine;
    private CoroutineState TreeRoutineState = CoroutineState.STOPPED;
    //private GameObject[] Debris;

    public Planetoid()
    {
        Operations = new PlanetoidMapEngineOperation(UpdateTree, CreateTree,DeleteTree, ReplaceTree, CurrentTree
            , WorldSpacePlayerLocation,WorldSpacePlanetLocation
            ,IsTreeCreated,TreeUnderPlayer,RaycastOnPlanet);
        Watch = new System.Diagnostics.Stopwatch();
        Watch.Start();
    }

    void Start()
    {
        name = SizeSetting.PlanetName;
        Player = FindObjectOfType<PlanetoidPlayer>();
        RigPlanet = GetComponent<Rigidbody>();
        _Canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
       
        Init();
    
    }

    private void Init()
    {
        InitProperties();

        RigPlanet.mass = SizeSetting.Mass;

        Debug.Log(SizeSetting.ToString());

        Engine = new PlanetoidMapEngine(SizeSetting, Operations, ColorGradient, NoiseSettings, IgnoreNoiseSetting, MeshCompute);
        InitMaterials();
        Engine.Init();
       
        //InitRaycastObjectsOnSurface();
        if (!HideRangeShell) InitAtmosphereSphere();

       /* Debris = new GameObject[25];
        for (int i = 0; i < Debris.Length; i++)
        {

            Debris[i] = Instantiate(DebirsPrefab);
            PlanetoidNode RandomNode = Engine.GetNodeAtIndex(UnityEngine.Random.Range(0, Engine.TotalNodes));

            Debris[i].transform.position = transform.position + RandomNode.LocalSpacePoint + RandomNode.LocalSpacePoint.normalized * (Engine.GenNoise._MaxNoise + 350f);// transform.position + new Vector3(0, SizeSetting.Radius + Engine.GenNoise._MaxNoise + 250f, 0);
            Debris[i].transform.parent = transform;


        }*/
    }

    private void InitProperties()
    {
        System.Random Rand = new System.Random(SizeSetting.PlanetSeed);
        float distanceMulti = 50000f;
        float x = (float)Rand.NextDouble() * distanceMulti * 2f - distanceMulti;
        float y = (float)Rand.NextDouble() * distanceMulti * 2f - distanceMulti;
        float z = (float)Rand.NextDouble() * distanceMulti * 2f - distanceMulti;
        transform.position = new Vector3(x, y, z);

        SizeSetting.Position = transform.position;

        ColorGradient = new Gradient();

        int numberOfColors = (int)(Rand.NextDouble() * (9 - 3) + 3);
        GradientColorKey[] colorKeys = new GradientColorKey[numberOfColors];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        for (int i = 0; i < colorKeys.Length; i++)
        {
            float rand = (float)Rand.NextDouble();

            float time = 0;

            if (i > 0)
            {
                if (i == 1)
                {
                    time = 0.25f * rand;
                }
                else if (i==colorKeys.Length-1)
                {
                    time = 1f;
                }
                else
                {
                    time = ((1f - colorKeys[i - 1].time) * rand) + colorKeys[i - 1].time;
                }
            }

            Color color = Color.HSVToRGB((float)Rand.NextDouble(), (float)Rand.NextDouble() * 0.15f + 0.85f, (float)Rand.NextDouble() * 0.15f + 0.85f);
            colorKeys[i] = new GradientColorKey(color, time);
        }

        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i] = new GradientAlphaKey(1f, i);
        }


        ColorGradient.SetKeys(colorKeys, alphaKeys);
        ColorGradient.mode = GradientMode.Blend;

        GameObject DistanceTextObj = new GameObject();
        DistanceTextObj.name = SizeSetting.PlanetName + "_Distance_Text";
        DistanceTextObj.transform.parent = _Canvas.transform;
        _Text = DistanceTextObj.AddComponent<Text>();
        _Text.font = TextFont;
        _Text.fontSize = 12;
    }

    private void InitMaterials()
    {
        Material BaseGrassMaterial = Resources.Load<Material>("BaseGrassColor");
        GrassMaterial = new Material(Shader.Find("Shader Graphs/GrassShader2"));
        GrassMaterial.CopyPropertiesFromMaterial(BaseGrassMaterial);
        GrassMaterial.SetFloat("_maxHeight", Engine.GenNoise._MaxNoise);
        GrassMaterial.SetFloat("_radius", SizeSetting.Radius);
        GrassMaterial.SetVector("_planetLocation", transform.position);
        GrassMaterial.SetColor("_startPlanetColor", ColorGradient.colorKeys[0].color);
        GrassMaterial.SetColor("_endPlanetColor", ColorGradient.colorKeys[ColorGradient.colorKeys.Length - 1].color);
        GrassMaterial.SetVector("_playerLocation", Player.transform.position);

        CircleMesh = PrimitiveMeshGenerator.GenerateCircleMesh(64);
        CircleMat = new Material(Shader.Find("Lightweight Render Pipeline/Unlit"));
        CircleMat.SetColor("_BaseColor", ColorGradient.colorKeys[0].color);

        CircleMat = new Material(Shader.Find("Lightweight Render Pipeline/Unlit"));
        CircleMat.SetColor("_BaseColor", ColorGradient.colorKeys[0].color);

        Material BaseOakTreeMaterial = Resources.Load<Material>("OakTreeMat");
        OakTreeMat = new Material(Shader.Find(BaseOakTreeMaterial.shader.name));
        OakTreeMat.CopyPropertiesFromMaterial(BaseOakTreeMaterial);
        //TreeMat.SetColor("_BaseColor", ColorGradient.colorKeys[0].color);
        OakTreeMat.SetColor("_BaseColor", Color.white);

        Material BaseFirTreeMaterial = Resources.Load<Material>("FirTreeMat");
        FirTreeMat = new Material(Shader.Find(BaseFirTreeMaterial.shader.name));
        FirTreeMat.CopyPropertiesFromMaterial(BaseFirTreeMaterial);
        //TreeMat.SetColor("_BaseColor", ColorGradient.colorKeys[0].color);
        FirTreeMat.SetColor("_BaseColor", Color.white);
    }

    private RaycastHit RaycastOnPlanet(Vector3 LocalPointOnSphere)
    {
        Vector3 PlanetCenter = transform.position;
        Vector3 DirectionToPlanet = (Vector3.zero - LocalPointOnSphere).normalized;
        Vector3 DirectionFromPlanet = LocalPointOnSphere.normalized;
        RaycastHit Hit;
        int IgnoreLayer = ~(1 << LayerMask.NameToLayer("PlayerMask"));
        int SelectionLayer = 1 << LayerMask.NameToLayer("PlanetoidChunkMask");
        float MaxNoise = Engine.GenNoise._MaxNoise;

        Vector3 Origin = (DirectionFromPlanet * (SizeSetting.Radius + 5f + MaxNoise)) + PlanetCenter;
        if (Physics.Raycast(Origin, DirectionToPlanet, out Hit, MaxNoise + 100f, SelectionLayer))
        {
            if (Hit.collider.transform.GetComponent<PlanetoidChunk>() == null)
            {
                throw new System.Exception(string.Format("Raycast at origin {0} hit objected ${1}, which is not a PlanetoidChunk", Origin,Hit.collider.name));
            }
        }

        return Hit;
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

            float Diameter = PlanetoidSizeSetting.RenderTypeRadius((PlanetoidRenderType)i, Radius, Engine.GenNoise._MaxNoise) *2f;

            RangeShellSphere[i] = GenerateRangeSphere(color, Diameter, ParentSphere.transform,false, 0f);

        }
;   }

    private void InitAtmosphereSphere()
    {
        System.Random Rand = new System.Random(SizeSetting.Seed);

        int numberOfSpheres = 20;
        //(int)(Rand.NextDouble() * (25 - 15) + 15);
        float DistanceIncrease = (float)(Rand.NextDouble() * (15f - 12f) + 12f)*SizeSetting.CloudScaler;
        RangeShellSphere = new GameObject[numberOfSpheres];
        float Radius = SizeSetting.Radius;
        float Diameter = PlanetoidSizeSetting.RenderTypeRadius(PlanetoidRenderType.PLAYER_ON_PLANET, Radius, Engine.GenNoise._MaxNoise) * 2f;
        //float Bias = (float)Rand.NextDouble();

        GameObject ParentSphere = new GameObject();
        ParentSphere.name = "RangeSpheres";
        ParentSphere.transform.position = transform.position;
        ParentSphere.transform.parent = transform;
       

        for (int i = 0; i < numberOfSpheres; i++)
        {
            //float hue = ((float)Rand.NextDouble() + Bias);// * Bias;
            float hue = ((float)Rand.NextDouble());// * Bias;
            Color color = Color.HSVToRGB(hue > 1f? 1f: hue, 1f,1f);
            color.a = (float)Rand.NextDouble() * (0.1f - 0.04f) + 0.04f;
            float DiameterForSphere = Diameter + (DistanceIncrease * i);
            bool InverTrangles = Rand.NextDouble() > 0.75f;
            float RotationTimeScale = ((float)Rand.NextDouble() * 0.1f) + 0.01f;
            RangeShellSphere[i] = GenerateRangeSphere(color, DiameterForSphere, ParentSphere.transform, InverTrangles, RotationTimeScale);
        }
;
    }

    private GameObject GenerateRangeSphere(Color color, float Diameter, Transform Parent, bool InvertTriangles, float rotationTimeScale)
    {

        GameObject RangeShellSphere = Instantiate(RadiusSpherePrefab.gameObject);
        RangeShellSphere.transform.localScale = new Vector3(Diameter, Diameter, Diameter);
        RangeShellSphere.transform.position = transform.position;
        Destroy(RangeShellSphere.GetComponent<SphereCollider>());

        //https://answers.unity.com/questions/1608815/change-surface-type-with-lwrp.html?_ga=2.207187795.1187703980.1596923527-2044146239.1595823173
        MeshRenderer Ren = RangeShellSphere.GetComponent<MeshRenderer>();
        Ren.receiveShadows = false;
        Ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        /*Material Mat = new Material(Shader.Find("Lightweight Render Pipeline/Simple Lit"));
        Mat.SetColor("_BaseColor", color);
        // wallMaterial.SetFloat("_Surface", (float)SurfaceType.Opaque);
        Mat.SetFloat("_Surface", 1f);
        Mat.SetOverrideTag("RenderType", "Transparent");
        Mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        Mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        Mat.SetInt("_ZWrite", 0);
        Mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        Mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Mat.SetShaderPassEnabled("ShadowCaster", false);*/

        Material Mat = new Material(Shader.Find("Shader Graphs/CloudSphereShader"));
        Mat.SetColor("_sphereColor", color);
        Mat.SetVector("_planetLocation", transform.position);
        Mat.SetFloat("_min", -6);
        Mat.SetFloat("_max", 6);
        Mat.SetFloat("_rotationTimeScale", rotationTimeScale);
        Ren.material = Mat;

        RangeShellSphere.name = name + "-RangeSphere-" + (int)Diameter + "m";
        RangeShellSphere.transform.parent = Parent;

        PlanetoidRadiusSphere PlanetoidRangeSphere = RangeShellSphere.GetComponent<PlanetoidRadiusSphere>();
        if(InvertTriangles) PlanetoidRangeSphere.InvertTriangles();

        return RangeShellSphere;
    }


    void Update()
    {
        Vector3 PlanetWorldPosition = transform.position;
        Vector3 PlayerWorldPosition = Player.transform.position;
        float DistanceBetweenPlayerAndPlanetMeter = Vector3.Distance(PlanetWorldPosition, PlayerWorldPosition);
        float DistanceKm = (DistanceBetweenPlayerAndPlanetMeter / 1000f);
        string DistanceKmRounded = string.Format("{0:0.00}", DistanceKm);

        Vector3 ViewportPoint = Camera.main.WorldToViewportPoint(PlanetWorldPosition);
        Vector3 ScreenPoint = Camera.main.WorldToScreenPoint(PlanetWorldPosition);
        ScreenPoint.z = 0;
        Vector3 ScreenDim = new Vector3(Screen.width, Screen.height, 0);
        
        bool UpdateOccured = Engine.Update();
        GrassMaterial.SetVector("_playerLocation", Player.transform.position);


        _Text.text = SizeSetting.PlanetName+": "+ DistanceKmRounded + "km";
        _Text.rectTransform.position = ScreenPoint;

       
        if (DistanceKm>2 && ViewportPoint.z > 0 && ViewportPoint.x > 0 && ViewportPoint.x < 1 && ViewportPoint.y > 0 && ViewportPoint.y < 1)
        {
            float TextAlpha = DistanceKm / 40f;
            TextAlpha = TextAlpha > 1f ? 1f : TextAlpha;
            Color TextColor = _Text.color;
            TextColor.a = TextAlpha;
            _Text.color = TextColor;
        }
        else
        {
            _Text.text = "";
        }

        //.rect = new Rect(Vector2.zero, new Vector2(100f,50f));

        //transform.rotation = transform.rotation * Quaternion.Euler(0.01f,0.01f,0.01f);



        //Debug.Log(ScreenPoint.x+","+ ScreenPoint.y+" - " +name);
        //GameObject spinnerObj = GameObject.Find("Planetoid8km_Spinner1");
        //RectTransform spinnerRect = spinnerObj.GetComponent<RectTransform>();
        //RectTransform cannvasRect = spinnerRect.parent.GetComponent<RectTransform>();
        //spinnerRect.position = (ScreenPoint ) - new Vector3(0,0, ScreenPoint.z);
        //spinnerRect.position = new Vector3();
        //spinnerRect.position = ScreenPoint;
        //Debug.Log(obj!=null);


        /*float Magnitude = Vector3.Distance(Player.transform.position, transform.position);

        Vector3 DirToPlayer = (Player.transform.position-transform.position).normalized;
        Vector3 AxisA = new Vector3(DirToPlayer.y, DirToPlayer.z, DirToPlayer.x);
        Vector3 AxisB = Vector3.Cross(DirToPlayer, AxisA);
        
        Vector3 Scale = Vector3.one * (SizeSetting.Radius + Engine.GenNoise._MaxNoise + (Magnitude*0.001f));
        Quaternion Rotation = Quaternion.LookRotation(AxisB, DirToPlayer);
        Matrix4x4 Matrix = Matrix4x4.TRS(transform.position, Rotation, Scale);
        Graphics.DrawMesh(CircleMesh, Matrix, CircleMat, 0, null, 0, null, false, false, false);*/


        if (Engine.RenderType <= PlanetoidRenderType.PLAYER_VERY_FAR)
        {
           /* Vector3 LightDirection = (PlanetWorldPosition - PlayerWorldPosition).normalized;
            Vector3 Normal = LightDirection;
           
            Vector3 AxisA = new Vector3(Normal.y, Normal.z, Normal.x);
            Vector3 AxisB = Vector3.Cross(Normal, AxisA);
            //Rotation Towards = LookTowards(World Up, World Forward) * TurnOnAxis(TrunDegrees,Local Direction To Turn Defrees On)
            Quaternion LookRotation = Quaternion.LookRotation(AxisA, Normal) * Quaternion.AngleAxis(-90,Vector3.right);

            GameObject.Find("Directional Light").transform.rotation = LookRotation;*/

            if (UpdateOccured)
            {
                Debug.Log(SizeSetting.PlanetName + " Update Occured.");
                QueuedTreeRoutine = true;
            }

            long CurrentTick = Watch.ElapsedTicks;
            if (TreeRoutineState == CoroutineState.COMPLETED)
            {
                TreeRoutineState = CoroutineState.STOPPED;
                //Debug.Log(SizeSetting.PlanetName + " Tree Routine Stopped at ticks: " + CurrentTick);
                //int TotalCreatedSoFar = ((FirTreeRenderListTemp.Count - 1) * 1023) + FirTreeRenderListTemp[FirTreeRenderListTemp.Count - 1].Count;
                StopCoroutine("TreeFinderCoroutine");

                FirTreeRenderList = FirTreeRenderListTemp;
                OakTreeRenderList = OakTreeRenderListTemp;
                GrassTreeRenderList = GrassTreeRenderListTemp;

                FirTreeRenderListTemp = new List<List<Matrix4x4>>();
                OakTreeRenderListTemp = new List<List<Matrix4x4>>();
                GrassTreeRenderListTemp = new List<List<Matrix4x4>>();
            }
            else if (QueuedTreeRoutine == true && TreeRoutineState == CoroutineState.STOPPED && Engine.IsAllQueueEmpty)
            {
                //Debug.Log(SizeSetting.PlanetName + "Tree Routine Started at ticks: " + CurrentTick);
                TreeRoutineState = CoroutineState.RUNNING;
                QueuedTreeRoutine = false;

                FirTreeRenderListTemp = new List<List<Matrix4x4>>();
                OakTreeRenderListTemp = new List<List<Matrix4x4>>();
                GrassTreeRenderListTemp = new List<List<Matrix4x4>>();

                StartCoroutine("TreeFinderCoroutine");
            }

            foreach (List<Matrix4x4> TreeList in FirTreeRenderList)
            {
                if (TreeList.Count > 0)
                {
                    Graphics.DrawMeshInstanced(FirTreeMesh, 0, FirTreeMat, TreeList, null, UnityEngine.Rendering.ShadowCastingMode.On, false);
                }
            }

            foreach (List<Matrix4x4> TreeList in OakTreeRenderList)
            {
                if (TreeList.Count > 0)
                {
                    Graphics.DrawMeshInstanced(OakTreeMesh, 0, OakTreeMat, TreeList, null, UnityEngine.Rendering.ShadowCastingMode.On, false);
                }
            }

            foreach (List<Matrix4x4> GrassList in GrassTreeRenderList)
            {
                if (GrassList.Count > 0)
                {
                    Graphics.DrawMeshInstanced(GrassMesh, 0, GrassMaterial, GrassList, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
                }
            }
        }
        else
        {
            TreeRoutineState = CoroutineState.STOPPED;
        }

        //for(int i = 0; i< Debris.Length; i++)
            //Debris[i].transform.RotateAround(PlanetWorldPosition, Vector3.right, 5f * Time.deltaTime);
    }


    //https://math.stackexchange.com/questions/268064/move-a-point-up-and-down-along-a-sphere
    //https://keisan.casio.com/exec/system/1359534351
    //https://math.libretexts.org/Bookshelves/Calculus/Book%3A_Calculus_(OpenStax)/12%3A_Vectors_in_Space/12.7%3A_Cylindrical_and_Spherical_Coordinates#:~:text=To%20convert%20a%20point%20from,y2%2Bz2).
    //https://math.stackexchange.com/questions/386476/mapping-random-points-on-a-sphere-onto-a-uniform-grid
    //https://math.stackexchange.com/questions/175805/moving-points-along-a-curve-on-sphere
    //https://stackoverflow.com/questions/26453951/rotateing-vector-on-plane-in-3d
    //https://www.gamedev.net/forums/topic/681795-moving-a-point-around-a-sphere/5308724/
    //https://mathinsight.org/spherical_coordinates  (cool visual)
    IEnumerator TreeFinderCoroutine()
    {
        Vector3 PlayerWorldPosition = Player.transform.position;
        Vector3 PlanetWorldPosition = transform.position;
        PlanetoidNode NearestNode = Engine.Solution.FindNearestNodeToPlayer();
        Debug.Log(SizeSetting.PlanetName+" "+NearestNode.LongLatPoint);
        List<PlanetoidNode> NearestNodes = Engine.Solution.FindNearestNodeToPlayerWithinIndexRange(15);
        //List<PlanetoidNode> NearestNodes = new List<PlanetoidNode>();
        //NearestNodes.Add(NearestNode);

        int EndIndex = 0;
        int StartIndex = 0;

        while (StartIndex <= NearestNodes.Count - 1)
        {
            EndIndex = EndIndex + 1023; 
            EndIndex = EndIndex >= NearestNodes.Count ? NearestNodes.Count - 1 : EndIndex;

            for (int i = StartIndex; i <= EndIndex; i++)
            {
                PlanetoidNode Node = NearestNodes[i];
                if (Node.NodeType == PlanetoidNodeType.EMPTY) continue;

                RaycastHit Hit = RaycastOnPlanet(Node.LocalSpacePoint);

                Vector3 Normal = Node.InitialHitNormal;
                Vector3 AxisA = new Vector3(Normal.y, Normal.z, Normal.x);
                Vector3 AxisB = Vector3.Cross(Normal, AxisA);


                if (Node.NodeType == PlanetoidNodeType.GRASS)
                {
                    //Rotation Towards = LookTowards(World Up, World Forward) * TurnOnAxis(TrunDegrees,Local Direction To Turn Defrees On)
                    Quaternion LookRotation = Quaternion.LookRotation(AxisB, Normal) * Quaternion.AngleAxis(135, Vector3.up);

                    Quaternion MeshFixRotation = Quaternion.LookRotation(Normal, Vector3.forward);
                    Quaternion RandomRotation = Quaternion.Euler(new Vector3(0, 0, UnityEngine.Random.Range(0f, 0f)));
                    LookRotation = MeshFixRotation * RandomRotation;

                    Vector3 TargetUp = LookRotation * Vector3.up;
                    Vector3 Position = Hit.collider ? Hit.point : Node.InitialLocalSpaceHitPoint + PlanetWorldPosition;
                    Vector3 Scale = Vector3.one*3f;
                    Position -= TargetUp * Scale.y * 0.1f;

                    Matrix4x4 Matrix = Matrix4x4.TRS(Position, LookRotation, Scale);
                    if (GrassTreeRenderListTemp.Count == 0 || GrassTreeRenderListTemp[GrassTreeRenderListTemp.Count - 1].Count == 1023)
                    {
                        GrassTreeRenderListTemp.Add(new List<Matrix4x4>());
                    }
                    GrassTreeRenderListTemp[GrassTreeRenderListTemp.Count - 1].Add(Matrix);

                    
                    for(int j = 0; j < Node.LongLatPointsNearIndex.Length;j++)
                    {
                        Hit = RaycastOnPlanet(Node.LongLatPointsNearIndex[j].Point);

                        Normal = Hit.collider? Hit.normal:Node.LongLatPointsNearIndex[j].Point.normalized;
                        AxisA = new Vector3(Normal.y, Normal.z, Normal.x);
                        AxisB = Vector3.Cross(Normal, AxisA);

                        MeshFixRotation = Quaternion.LookRotation(Normal, Vector3.forward);
                        RandomRotation = Quaternion.Euler(new Vector3(0, 0, UnityEngine.Random.Range(0f, 0f)));
                        LookRotation = MeshFixRotation * RandomRotation;

                        TargetUp = LookRotation * Vector3.up;
                        Position = Hit.collider ? Hit.point : Node.LongLatPointsNearIndex[j].Point + PlanetWorldPosition;
                        Scale = Vector3.one * 2f;
                        Position -= TargetUp * Scale.y * 0.1f;

                        Matrix = Matrix4x4.TRS(Position, LookRotation, Scale);
                        if (GrassTreeRenderListTemp.Count == 0 || GrassTreeRenderListTemp[GrassTreeRenderListTemp.Count - 1].Count == 1023)
                        {
                            GrassTreeRenderListTemp.Add(new List<Matrix4x4>());
                        }
                        GrassTreeRenderListTemp[GrassTreeRenderListTemp.Count - 1].Add(Matrix);
                    }


                }
                else
                {
                    //Rotation Towards = LookTowards(World Up, World Forward) * TurnOnAxis(TrunDegrees,Local Direction To Turn Defrees On)
                    Quaternion LookRotation = Quaternion.LookRotation(AxisB, Normal) * Quaternion.AngleAxis(135, Vector3.up);
                    Vector3 TargetUp = LookRotation * Vector3.up;
                    Vector3 Position = Hit.collider ? Hit.point : Node.InitialLocalSpaceHitPoint + PlanetWorldPosition;
                    Vector3 Scale = Vector3.one * 7f;
                    Position -= TargetUp * Scale.y * 0.1f;

                    if (Node.NodeType == PlanetoidNodeType.FIR_TREE)
                    {
                        Matrix4x4 Matrix = Matrix4x4.TRS(Position, LookRotation, Scale);

                        if (FirTreeRenderListTemp.Count == 0 || FirTreeRenderListTemp[FirTreeRenderListTemp.Count - 1].Count == 1023)
                        {
                            FirTreeRenderListTemp.Add(new List<Matrix4x4>());
                        }
                        FirTreeRenderListTemp[FirTreeRenderListTemp.Count - 1].Add(Matrix);
                    }
                    else if (Node.NodeType == PlanetoidNodeType.OAK_TREE)
                    {
                        Matrix4x4 Matrix = Matrix4x4.TRS(Position, LookRotation, Scale);

                        if (OakTreeRenderListTemp.Count == 0 || OakTreeRenderListTemp[OakTreeRenderListTemp.Count - 1].Count == 1023)
                        {
                            OakTreeRenderListTemp.Add(new List<Matrix4x4>());
                        }
                        OakTreeRenderListTemp[OakTreeRenderListTemp.Count - 1].Add(Matrix);
                    }
                }
     
            }

            StartIndex = EndIndex+1;

            //yield return new WaitForSeconds(0.15f);
            yield return null;
        }

        int FirTreeCount = (FirTreeRenderListTemp.Count - 1) * 1023 + FirTreeRenderListTemp[FirTreeRenderListTemp.Count - 1].Count;
        int OakTreeCount = (OakTreeRenderListTemp.Count - 1) * 1023 + OakTreeRenderListTemp[OakTreeRenderListTemp.Count - 1].Count;
        int GrassTreeCount = (GrassTreeRenderListTemp.Count - 1) * 1023 + GrassTreeRenderListTemp[GrassTreeRenderListTemp.Count - 1].Count;
     
        Debug.Log(string.Format("Update Completed: Nearest Nodes:{0}, Fir Trees:{1}, Oak Trees:{2}, Grass:{3}",NearestNodes.Count,FirTreeCount,OakTreeCount,GrassTreeCount));


        TreeRoutineState = CoroutineState.COMPLETED;
        yield break;
    }




    void FixedUpdate()
    {
        if (EnableGravity)
        {
           
            float otherMass = Player.RigPlayer.mass;
            Vector3 otherPos = Player.RigPlayer.transform.position;
            Vector3 direction = Vector3.Normalize(otherPos - transform.position);
            float distance = Vector3.Distance(transform.position, otherPos);
            float force = (GRAVITY_CONST * RigPlanet.mass * otherMass) / Mathf.Pow(distance, 2);
            Vector3 forceVector = (direction * force) * Time.fixedDeltaTime;
            Player.RigPlayer.AddForce(-forceVector);
        }
    }

    public void UpdateTree(PlanetoidQuadTree Tree)
    {
        DeleteTree(Tree);
        CreateTree(Tree);
        //Debug.Log(NewTree.Id + " Updated.");
    }

    public void CreateTree(PlanetoidQuadTree Tree)
    {
        PlanetoidChunk NewChunk = Instantiate(PlanetoidChunkPrefab, transform.position, transform.rotation);
        NewChunk.transform.parent = transform;
        NewChunk.name = Tree.Id;
        ChunkData MeshData = Engine.GenMesh.GenerateMesh(Tree);
        NewChunk.Instantiate(this, MeshData, Tree, GrassMaterial);
        //Debug.Log(Tree.Id+" Created.");
    }

    public PlanetoidQuadTree CurrentTree(PlanetoidQuadTree Tree)
    {
        PlanetoidChunk Chunk = ChunkFromId(Tree.Id);
        return Chunk.Tree;
    }

    public void DeleteTree(PlanetoidQuadTree Tree)
    {
        DeleteTreeById(Tree.Id);
        //Debug.Log(Tree.Id + " Destoyed.");
    }

    public void ReplaceTree(PlanetoidQuadTree Tree)
    {
        PlanetoidChunk Chunk = ChunkFromId(Tree.Id);
        Chunk.Tree = Tree;
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
        Vector3 Direction = (PlanetLocation - PlayerLocation).normalized;
        Vector3 LocalPointOnSphere = Direction * SizeSetting.Radius;
        RaycastHit Hit = RaycastOnPlanet(LocalPointOnSphere);
        if(Hit.collider)
            return Hit.collider.gameObject.GetComponent<PlanetoidChunk>().Tree;
        return null;
    }

}
