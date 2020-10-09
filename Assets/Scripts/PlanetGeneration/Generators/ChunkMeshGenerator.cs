using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Threading;



public class ChunkData
{
    public Mesh _Mesh;
    public Vector2[] ChunkUVs;
    public Vector3[] VertData;
    public float MaxHeight;
    public float[] Height;
    public GrassStore GrassData;

    //public List<HouseCompute> HouseLocations;

    //public Texture2D TextureData;

    public ChunkData(Mesh mesh, Vector2[] chunkUVs, Vector3[] vertData, float[] height, float maxHeight, GrassStore grassData)
    {
        _Mesh = mesh;
        ChunkUVs = chunkUVs;
        VertData = vertData;
        MaxHeight = maxHeight;
        Height = height;
        GrassData = grassData;
        //HouseLocations = houseLocations;
        //TextureData = textureData;
    }
}



public class ChunkDataStorage { 

    //PlanetName -> Tree Id -> GrassStorage
    private Dictionary<string, Dictionary<string, Store<GrassStore>>> GrassDataDict;

    //PlanetName -> Tree Id -> MeshStorage
    private Dictionary<string, Dictionary<string, Store<ChunkData>>> MeshDataDict;

    private static ChunkDataStorage Storage = null;

    private Thread StorageThread;

    private System.Diagnostics.Stopwatch Watch;

    private static int THREAD_SLEEP_TIME_MS = 10000;
    private static int STORAGE_ITEM_ALIVE_TIME_MS = 20000;

    private ChunkDataStorage()
    {
        Init();

        Watch = new System.Diagnostics.Stopwatch();
        Watch.Start();

        StorageThread = new Thread(new ThreadStart(StorageTheadJob));
        StorageThread.Start();
        
    }

    private void Init()
    {
        GrassDataDict = new Dictionary<string, Dictionary<string, Store<GrassStore>>>();
        MeshDataDict = new Dictionary<string, Dictionary<string, Store<ChunkData>>>();
    }

    private void StorageTheadJob()
    {
        while(true)
        {

            IEnumerator<string> PlanetKeys = GrassDataDict.Keys.GetEnumerator();
            List<string> PlanetNames = new List<string>();
            while (PlanetKeys.MoveNext()) PlanetNames.Add(PlanetKeys.Current);

            foreach (string Planet in PlanetNames)
            {
                IEnumerator<string> TreeIdKeys = GrassDataDict[Planet].Keys.GetEnumerator();
                List<string> TreeIds = new List<string>();
                while (TreeIdKeys.MoveNext()) TreeIds.Add(TreeIdKeys.Current);

                foreach (string TreeId in TreeIds)
                {
                    //long AliveTime = Watch.ElapsedMilliseconds - GrassDataDict[Planet][TreeId].CreatedTimestampMs;
                    //if (AliveTime > STORAGE_ITEM_ALIVE_TIME_MS)
                    //{

                    if (GrassDataDict[Planet][TreeId].IsExpired) { 
                        GrassDataDict[Planet].Remove(TreeId);
                        //Debug.Log("Deleting Grass Item:"+TreeId+", Alive Time:"+AliveTime/1000);
                    }
                }
            }

            PlanetKeys = MeshDataDict.Keys.GetEnumerator();
            PlanetNames = new List<string>();
            while (PlanetKeys.MoveNext()) PlanetNames.Add(PlanetKeys.Current);

            foreach (string Planet in PlanetNames)
            {
                IEnumerator<string> TreeIdKeys = MeshDataDict[Planet].Keys.GetEnumerator();
                List<string> TreeIds = new List<string>();
                while (TreeIdKeys.MoveNext()) TreeIds.Add(TreeIdKeys.Current);

                foreach (string TreeId in TreeIds)
                {
                    //long AliveTime = Watch.ElapsedMilliseconds - MeshDataDict[Planet][TreeId].CreatedTimestampMs;
                    //if (AliveTime > STORAGE_ITEM_ALIVE_TIME_MS)
                    if(MeshDataDict[Planet][TreeId].IsExpired)
                    {
                        MeshDataDict[Planet].Remove(TreeId);
                        //Debug.Log("Deleting Mesh Item:" + TreeId + ", Alive Time:" + AliveTime / 1000);
                    }
                }
            }

            //TODO: Cleanup Memory after time
            Debug.Log("Grass Stored Planets:" + GrassDataDict.Count + ", Grass Count:" + TotalGrassStorageCount() + ", Mesh Stored Planets:" + MeshDataDict.Count + ", Mesh Count:" + TotalMeshStorageCount() + ", Seconds:" + Watch.ElapsedMilliseconds / 1000);
            Thread.Sleep(THREAD_SLEEP_TIME_MS);
        }
    }

    private int TotalGrassStorageCount()
    {
        int total = 0;
        IEnumerator<string> Keys = GrassDataDict.Keys.GetEnumerator();
        while(Keys.MoveNext()) total += GrassDataDict[Keys.Current].Count;
        return total;
    }

    private int TotalMeshStorageCount()
    {
        int total = 0;
        IEnumerator<string> Keys = MeshDataDict.Keys.GetEnumerator();
        while (Keys.MoveNext()) total += MeshDataDict[Keys.Current].Count;
        return total;
    }

    public static ChunkDataStorage GetInstance()
    {
        if (Storage == null) Storage = new ChunkDataStorage();
        return Storage;
    }

    public GrassStore StorageGetGrassData(string PlanetName, string TreeId)
    {
        if(HasGrassDataPlanet(PlanetName)) return GrassDataDict[PlanetName][TreeId].StorageItem;
        return null;
    }

    public void StorageSetGrassData(string PlanetName, string TreeId, GrassStore GrassData)
    {
        if(!HasGrassDataPlanet(PlanetName))
        {
            GrassDataDict.Add(PlanetName, new Dictionary<string, Store<GrassStore>>());
        }
     
        if (!HasGrassData(PlanetName, TreeId))
        {
            GrassDataDict[PlanetName].Add(TreeId, new Store<GrassStore>(Watch, TreeId, GrassData, STORAGE_ITEM_ALIVE_TIME_MS));
        }
    }

    public bool HasGrassDataPlanet(string PlanetName)
    {
        return GrassDataDict.ContainsKey(PlanetName);
    }

    public bool HasGrassData(string PlanetName, string TreeId)
    {
        return HasGrassDataPlanet(PlanetName) && GrassDataDict[PlanetName].ContainsKey(TreeId);
    }

    public void StorageDeleteGrassPlanet(string PlanetName)
    {
        if(HasGrassDataPlanet(PlanetName))
        {
            GrassDataDict.Remove(PlanetName);
        }
    }

    public void StorageDeleteChunkDataPlanet(string PlanetName)
    {
        if (HasChunkDataPlanet(PlanetName))
        {
            MeshDataDict.Remove(PlanetName);
        }
    }

    public void StorageCleanUp()
    {
        IEnumerator<string> Keys = GrassDataDict.Keys.GetEnumerator();
        List<string> KeysList = new List<string>();
        while (Keys.MoveNext()) KeysList.Add(Keys.Current);
        KeysList.ForEach(v => GrassDataDict.Remove(v));

        Keys = MeshDataDict.Keys.GetEnumerator();
        KeysList = new List<string>();
        while (Keys.MoveNext()) KeysList.Add(Keys.Current);
        KeysList.ForEach(v => MeshDataDict.Remove(v));

        Init();
    }

    
    public ChunkData StorageGetChunkData(string PlanetName, string TreeId)
    {
        if (HasChunkData(PlanetName, TreeId)) return MeshDataDict[PlanetName][TreeId].StorageItem;
        return null;
    }

    public void StorageSetChunkData(string PlanetName, string TreeId, ChunkData MeshData)
    {
        if (!HasChunkDataPlanet(PlanetName))
        {
            MeshDataDict.Add(PlanetName, new Dictionary<string, Store<ChunkData>>());
        }

        if (!HasChunkData(PlanetName, TreeId))
        {
            MeshDataDict[PlanetName].Add(TreeId, new Store<ChunkData>(Watch, TreeId, MeshData, STORAGE_ITEM_ALIVE_TIME_MS));
        }
    }


    public bool HasChunkDataPlanet(string PlanetName)
    {
        return MeshDataDict.ContainsKey(PlanetName);
    }

    public bool HasChunkData(string PlanetName, string TreeId)
    {
        return HasChunkDataPlanet(PlanetName) && MeshDataDict[PlanetName].ContainsKey(TreeId);
    }

    public void Kill()
    {
        StorageThread.Abort();
    }

}


public class ChunkMeshGenerator
{
    private NoiseGenerator GenNoise;
    private PlanetoidMapEngineSolution Solution;
    private PlanetoidSizeSetting SizeSettings;
    private ComputeShader MeshCompute;
    private Gradient ColorGradient;

    public ChunkMeshGenerator(PlanetoidSizeSetting sizeSettings, NoiseGenerator genNoise, PlanetoidMapEngineSolution solution, ComputeShader meshCompute, Gradient colorGradient)
    {
        GenNoise = genNoise;
        Solution = solution;
        SizeSettings = sizeSettings;
        MeshCompute = meshCompute;
        ColorGradient = colorGradient;
    }

    private int StitchResolution(PlanetoidQuadTree Tree, PlanetoidQuadTree Other)
    {
        if (Other == null) return 1;
        int DiffRes = (Other.Resolution / Tree.Resolution);
        int EdgeRes = (Tree.Size / Tree.Resolution);
        if (DiffRes == 1) return 1;

        int Size = Other.Size >= Tree.Size && Other.Resolution >= Tree.Resolution
            ? Other.Resolution > Tree.Size
                ? EdgeRes
                : DiffRes
            : 1;

        //return Size>2?2:1;

        //return Size>4?4:Size;
        return Size;
    }

    private bool NormalCopyCheck(PlanetoidQuadTree Tree, PlanetoidQuadTree Other, int ExpectedNormalLength, out Vector3[] OutOtherNormals)
    {
        //return Other != null && Other.Resolution == 1 && Other.Face.Equals(Tree.Face) && Tree.Size == Other.Size;
        if (Other != null && Other.Resolution == Tree.Resolution && Other.Size == Tree.Size && Other.Face.Equals(Tree.Face))
        {
            GameObject ChunkObj = GameObject.Find(Other.Id);
            Vector3[] OtherNormals = ChunkObj != null ? ChunkObj.GetComponent<PlanetoidChunk>()._ChunkData._Mesh.normals : null;
            if (OtherNormals != null && OtherNormals.Length == ExpectedNormalLength)
            {
                OutOtherNormals = OtherNormals;
                return true;
            }
        }

        OutOtherNormals = null;
        return false;
    }

    public ChunkData GenerateMesh(PlanetoidQuadTree Tree)
    {
        ChunkDataStorage Storage = ChunkDataStorage.GetInstance();

        string TreeIdWithRes = Tree.Id + "-Res:" + Tree.Resolution;

        ChunkData chunkData = Storage.StorageGetChunkData(SizeSettings.PlanetName, TreeIdWithRes);

        if (chunkData!=null) return chunkData;
        

        System.Diagnostics.Stopwatch Watch = new System.Diagnostics.Stopwatch();
        Watch.Start();

        int MaxResolution = SizeSettings.MaxResolution;
        int Edges = Tree.Size;
        int Vert = Tree.Size + 1;
        int EdgeRes = (Tree.Size / Tree.Resolution);
        int VertRes = (Tree.Size / Tree.Resolution) + 1;
        int Resolution = Tree.Resolution;
        Vector2 StartPosition = Tree.Start;
        PlanetoidFace Face = Tree.Face;
        float Radius = SizeSettings.Radius;
        int MaxTriangle = EdgeRes * EdgeRes * 6;
        //int MaxHouses = 30;
        int numThreadsPerAxis = Mathf.CeilToInt((float)(VertRes + 1) / (float)8);
        int HouseDistanceApart = 16 / Resolution;

        Vector3[] VertData = new Vector3[VertRes * VertRes];
        Vector2[] ChunkUVs = new Vector2[VertRes * VertRes];
        Color[] VertColors = new Color[VertRes * VertRes];
        float[] Height = new float[VertRes * VertRes];
        int[] HouseLocationsMap = new int[VertRes * VertRes];
        Vector3[] VertNormals;
        //List<HouseCompute> HouseLocations = new List<HouseCompute>();
        int[] ChunkTriangles;


        //RenderTexture RenderTextureData = new RenderTexture(VertRes, VertRes, 0,RenderTextureFormat.ARGBFloat);
        //RenderTextureData.useMipMap = false;
        //RenderTextureData.enableRandomWrite = true;
        //RenderTextureData.Create();


        PlanetoidTreeManager Manager = Tree.TreeManager;

        PlanetoidQuadTree UpTree = Manager.FindNeighbourUp(Tree);
        PlanetoidQuadTree DownTree = Manager.FindNeighbourDown(Tree);
        PlanetoidQuadTree LeftTree = Manager.FindNeighbourLeft(Tree);
        PlanetoidQuadTree RightTree = Manager.FindNeighbourRight(Tree);

        ComputeBuffer VertexBufferArray = new ComputeBuffer(VertRes * VertRes, sizeof(float) * 3);
        ComputeBuffer HeightBufferArray = new ComputeBuffer(VertRes * VertRes, sizeof(float));
        ComputeBuffer TriangleBufferList = new ComputeBuffer(MaxTriangle, sizeof(int) * 3, ComputeBufferType.Append);
        ComputeBuffer TriangleBufferCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        TriangleBufferList.SetCounterValue(0);
        ComputeBuffer UvBufferArray = new ComputeBuffer(VertRes * VertRes, sizeof(float) * 2);
        ComputeBuffer NoiseBufferArray = new ComputeBuffer(GenNoise.SimpleNoiseSettingsCompute.Length, Marshal.SizeOf(GenNoise.SimpleNoiseSettingsCompute[0]));
        NoiseBufferArray.SetData(GenNoise.SimpleNoiseSettingsCompute, 0, 0, GenNoise.SimpleNoiseSettingsCompute.Length);

        MeshCompute.SetBuffer(0, "vertexBufferArray", VertexBufferArray);
        MeshCompute.SetBuffer(0, "triangleBufferList", TriangleBufferList);
        MeshCompute.SetBuffer(0, "uvBufferArray", UvBufferArray);
        MeshCompute.SetBuffer(0, "heightBufferArray", HeightBufferArray);
        MeshCompute.SetBuffer(0, "noiseBufferArray", NoiseBufferArray);

        //MeshCompute.SetTexture(0, "renderTextureData", RenderTextureData);
        MeshCompute.SetInt("VertRes", VertRes);
        MeshCompute.SetInt("EdgeRes", EdgeRes);
        MeshCompute.SetInt("TotalVerts", VertRes * VertRes);
        MeshCompute.SetInt("NumberOfNoiseSettings", GenNoise.SimpleNoiseSettingsCompute.Length);
        MeshCompute.SetInt("TotalEdges", SizeSettings.MaxEdgeTiles);
        MeshCompute.SetInt("Resolution", Resolution);
        MeshCompute.SetInt("UpResolution", StitchResolution(Tree, UpTree));
        MeshCompute.SetInt("DownResolution", StitchResolution(Tree, DownTree));
        MeshCompute.SetInt("LeftResolution", StitchResolution(Tree, LeftTree));
        MeshCompute.SetInt("RightResolution", StitchResolution(Tree, RightTree));
        MeshCompute.SetFloat("Radius", Radius);
        MeshCompute.SetFloat("MaxHeight", GenNoise._MaxNoise);
        MeshCompute.SetVector("StartPosition", new Vector4(StartPosition.x, StartPosition.y));
        MeshCompute.SetVector("DirVec", new Vector4(Face.DirVec.x, Face.DirVec.y, Face.DirVec.z));
        MeshCompute.SetVector("AxisA", new Vector4(Face.DirVecAxisA.x, Face.DirVecAxisA.y, Face.DirVecAxisA.z));
        MeshCompute.SetVector("AxisB", new Vector4(Face.DirVecAxisB.x, Face.DirVecAxisB.y, Face.DirVecAxisB.z));

        MeshCompute.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, 1);


        VertexBufferArray.GetData(VertData, 0, 0, VertexBufferArray.count);
        UvBufferArray.GetData(ChunkUVs, 0, 0, UvBufferArray.count);
        HeightBufferArray.GetData(Height, 0, 0, HeightBufferArray.count);

        //Copy Trangles List from Buffer
        ComputeBuffer.CopyCount(TriangleBufferList, TriangleBufferCount, 0);
        int[] triCountArray = { 0 };
        TriangleBufferCount.GetData(triCountArray);
        int numTris = triCountArray[0];
        TriangleCompute[] ChunkTriangleComputeArr = new TriangleCompute[numTris];
        TriangleBufferList.GetData(ChunkTriangleComputeArr, 0, 0, numTris);
        ChunkTriangles = new int[numTris * 3];


        //400-500 Ticks to copy
        for (int i = 0; i < numTris; i++)
        {
            ChunkTriangles[(i * 3)] = ChunkTriangleComputeArr[i].A;
            ChunkTriangles[(i * 3) + 1] = ChunkTriangleComputeArr[i].B;
            ChunkTriangles[(i * 3) + 2] = ChunkTriangleComputeArr[i].C;
        }

        VertexBufferArray.Release();
        TriangleBufferList.Release();
        TriangleBufferCount.Release();
        //HouseBufferList.Release();
        //HouseBufferCount.Release();
        UvBufferArray.Release();
        NoiseBufferArray.Release();
        HeightBufferArray.Release();



        //RenderTexture.active = RenderTextureData;
        //Texture2D TextureData = new Texture2D(RenderTextureData.width, RenderTextureData.height,TextureFormat.RGBAFloat,false);
        //TextureData.ReadPixels(new Rect(0, 0, RenderTextureData.width, RenderTextureData.height), 0, 0);
        //TextureData.Apply();
        //RenderTexture.active = null;
        //RenderTextureData.Release();


        //for (int y = 0; y < VertRes; y++)
        //{
        //for (int x = 0; x < VertRes; x++)
        // {
        //int VectorIndex = (y * VertRes) + x;
        //float height01 = GenNoise._MaxNoise == 0 ? 1f : Height[VectorIndex] / GenNoise._MaxNoise;
        //VertColors[VectorIndex] = ColorGradient.Evaluate(height01);
        //float height01 = GenNoise._MaxNoise == 0?1f:Height[VectorIndex] / GenNoise._MaxNoise;
        //float randColor = ((float)Rand.NextDouble()*(0.015f- 0.01f) + 0.01f) * ((float)Rand.NextDouble()>0.5f?1f:-1f);

        //height01 = Mathf.Clamp01(height01 + randColor);
        //VertColors[VectorIndex] = ColorGradient.Evaluate(height01);
        //float outH, outS, outV;
        //Color.RGBToHSV(VertColors[VectorIndex], out outH, out outS, out outV);
        //outH = Mathf.Clamp01(outH + (float)Rand.NextDouble()*(randColor * 2f) + (-randColor));
        //outS = Mathf.Clamp01(outS + (float)Rand.NextDouble() * (randColor * 2f) + (-randColor));
        //outV = Mathf.Clamp01(outV + (float)Rand.NextDouble() * (randColor * 2f) + (-randColor));
        //VertColors[VectorIndex] = Color.HSVToRGB(outH, outS, outV);

        /*if (Tree.Resolution == SizeSettings.MinResolution && HouseLocations.Count < MaxHouses)
        {
            //if ((x * Resolution) > 5 && (x * Resolution) < (VertRes - 1) - 5 && (y * Resolution) > 5 && (y * Resolution) < (VertRes - 1) - 5)


            //if ((x * Resolution) > 1 && (x * Resolution) < (VertRes - 1) - 1 && (y * Resolution) > 1 && (y * Resolution) < (VertRes - 1) - 1)
            //{

                if (Height[VectorIndex] < 0.5f && HouseLocationsMap[VectorIndex] == 0)
                {
                    HouseLocationsMap[VectorIndex] = 1;
                    HouseCompute House;
                    House.DirectionOrientation = VertData[VectorIndex].normalized;
                    House.Position = VertData[VectorIndex];
                    House.Scale = Vector3.one;
                    HouseLocations.Add(House);
                    for (int yy = y - (HouseDistanceApart*2); yy < y + (HouseDistanceApart * 2); yy++)
                    {
                        for (int xx = x - (HouseDistanceApart * 2); xx < x + (HouseDistanceApart * 2); xx++)
                        {
                            if (yy < 0 || yy > VertRes - 1 || xx < 0 || xx > VertRes - 1) continue;
                            int HouseIndex = (yy * VertRes) + xx;
                            HouseLocationsMap[HouseIndex] = 1;
                        }
                    }
                }
            //}
        }*/
        //}             
        //}

        System.Random Rand = new System.Random(SizeSettings.Seed);
        for (int i = 0; i < VertColors.Length; i++)
        {
            float height01 = GenNoise._MaxNoise == 0 ? 1f : Height[i] / GenNoise._MaxNoise;
            float change = 0.01f;

            //float randH = (((float)Rand.NextDouble() * change * 2f) - change) * Mathf.Pow((float)Rand.NextDouble(), 2) * 2f;
            //float randS = (((float)Rand.NextDouble() * change * 2f) - change) * Mathf.Pow((float)Rand.NextDouble(), 2) * 2f;
            //float randV = (((float)Rand.NextDouble() * change * 2f) - change) * Mathf.Pow((float)Rand.NextDouble(), 2) * 2f;

            float randH = (((float)Rand.NextDouble() * change * 2f) - change);
            float randS = (((float)Rand.NextDouble() * change * 2f) - change);
            float randV = (((float)Rand.NextDouble() * change * 2f) - change);

            Color height01Color = ColorGradient.Evaluate(height01);
            float hColor, sColor, vColor;
            Color.RGBToHSV(height01Color, out hColor, out sColor, out vColor);
            hColor = Mathf.Clamp01(hColor + randH);
            sColor = Mathf.Clamp01(sColor + randS);
            vColor = Mathf.Clamp01(vColor + randV);
            VertColors[i] = Color.HSVToRGB(hColor, sColor, vColor);
        }

        Mesh NewMesh = new Mesh();
        NewMesh.Clear();
        NewMesh.vertices = VertData;
        NewMesh.triangles = ChunkTriangles;
        NewMesh.uv = ChunkUVs;
        NewMesh.colors = VertColors;


        NewMesh.RecalculateNormals();
        VertNormals = NewMesh.normals;

        if (Tree.IsRoot)
        {
            //for (int i = 0; i < VertColors.Length; i++)
            //VertNormals[i] = VertData[i].normalized;  
        }

        Vector3[] OtherNormals;

        if (NormalCopyCheck(Tree, UpTree, VertNormals.Length, out OtherNormals))
        {
            for (int x = 0; x < VertRes; x++)
            {
                int OtherY = VertRes - 1;
                int OtherNormalIndex = (OtherY * VertRes) + x;

                int ThisY = 0;
                int ThisNormalIndex = (ThisY * VertRes) + x;

                VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
            }
        }

        if (NormalCopyCheck(Tree, DownTree, VertNormals.Length, out OtherNormals))
        {
            for (int x = 0; x < VertRes; x++)
            {
                int OtherY = 0;
                int OtherNormalIndex = (OtherY * VertRes) + x;

                int ThisY = VertRes - 1;
                int ThisNormalIndex = (ThisY * VertRes) + x;

                VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
            }
        }


        if (NormalCopyCheck(Tree, LeftTree, VertNormals.Length, out OtherNormals))
        {
            for (int y = 0; y < VertRes; y++)
            {
                int OtherX = VertRes - 1;
                int OtherNormalIndex = (y * VertRes) + OtherX;

                int ThisX = 0;
                int ThisNormalIndex = (y * VertRes) + ThisX;

                VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
            }
        }


        if (NormalCopyCheck(Tree, RightTree, VertNormals.Length, out OtherNormals))
        {
            for (int y = 0; y < VertRes; y++)
            {
                int OtherX = 0;
                int OtherNormalIndex = (y * VertRes) + OtherX;

                int ThisX = VertRes - 1;
                int ThisNormalIndex = (y * VertRes) + ThisX;

                VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
            }

        }

        NewMesh.normals = VertNormals;

        GrassStore GrassData = Storage.HasGrassData(SizeSettings.PlanetName, Tree.Id) ? Storage.StorageGetGrassData(SizeSettings.PlanetName, Tree.Id) : new GrassStore();
        //List<List<Matrix4x4>> GrassData = new List<List<Matrix4x4>>();

        if (false)
        {
            if (GrassData.Count == 0 && Tree.Resolution <= SizeSettings.MinResolution * 2)
            {
                List<Matrix4x4> GrassBatoh = new List<Matrix4x4>();
                float ResProbMulti = (Tree.Resolution == SizeSettings.MinResolution) ? 0.25f : 1f;
                float GrassProb = 0.25f;

                //for (int i = 0; i < VertData.Length; i++)
                //{ int VectorIndex = i;

                float GrassRange = 0.25f;

                for (int y = 0; y < VertRes; y++)
                {
                    for (int x = 0; x < VertRes; x++)
                    {
                        int VectorIndex = (y * VertRes) + x;

                        float PointProb = ResProbMulti;
                        if (x == 0 || y == 0 || x == VertRes - 1 || y == VertRes - 1) PointProb *= 0.5f;

                        float FinalProb = PointProb * GrassProb;
                        Quaternion MeshFixRotation = Quaternion.LookRotation(VertNormals[VectorIndex], Vector3.forward);
                        Quaternion RandomRotation = Quaternion.Euler(new Vector3(0, 0, UnityEngine.Random.Range(0f, 360f)));
                        Quaternion Rotation = MeshFixRotation * RandomRotation;

                        if (Rand.NextDouble() < FinalProb)
                        {
                            Vector3 StartPoint = VertData[VectorIndex] + SizeSettings.Position + (VertNormals[VectorIndex] * 0.15f);

                            //Matrix4x4 MatrixStart = Matrix4x4.TRS(StartPoint, Rotation, Vector3.one * (1f + (float)Rand.NextDouble()));
                            //GrassBatoh = GrassBatchAdd(GrassData, GrassBatoh, MatrixStart);

                            for (int i = 0; i < 5; i++)
                            {
                                if (Rand.NextDouble() < 0.25f)
                                {
                                    float Xs = (float)Rand.NextDouble() * (GrassRange - (-GrassRange)) + (-GrassRange);
                                    float Ys = (float)Rand.NextDouble() * (GrassRange - (-GrassRange)) + (-GrassRange);
                                    Vector3 AxisA = new Vector3(VertNormals[VectorIndex].y, VertNormals[VectorIndex].z, VertNormals[VectorIndex].x);
                                    Vector3 AxisB = Vector3.Cross(VertNormals[VectorIndex], AxisA);
                                    Vector3 GrassPosition = StartPoint + (AxisA * Xs) + (AxisB * Ys);
                                    Matrix4x4 Matrix = Matrix4x4.TRS(GrassPosition, Rotation, Vector3.one * (1f + (float)Rand.NextDouble()));
                                    GrassBatoh = GrassBatchAdd(GrassData, GrassBatoh, Matrix);
                                }
                            }

                           
                        }
                    }
                }
                if (GrassBatoh.Count > 0) GrassData.Add(GrassBatoh);
            }
            if (GrassData.Count > 0) Storage.StorageSetGrassData(SizeSettings.PlanetName, Tree.Id, GrassData);
        }



        chunkData = new ChunkData(NewMesh, ChunkUVs, VertData, Height, GenNoise._MaxNoise, GrassData);

        Storage.StorageSetChunkData(SizeSettings.PlanetName, TreeIdWithRes, chunkData);


        Watch.Stop();

        //if (Tree.IsPlayerTree) Debug.Log("Time To Create: " + Watch.ElapsedTicks);

        return chunkData;
    }

    private static List<Matrix4x4> GrassBatchAdd(List<List<Matrix4x4>> GrassData, List<Matrix4x4> GarssBatoh, Matrix4x4 Grass)
    {
        if(GarssBatoh.Count==1023)
        {
            GrassData.Add(GarssBatoh);
            GarssBatoh = new List<Matrix4x4>();
        }
        GarssBatoh.Add(Grass);
        return GarssBatoh;
    }

}
