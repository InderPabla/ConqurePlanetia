using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;


public class MeshGeneratorData
{
    public Mesh _Mesh;
    public Vector2[] ChunkUVs;
    public Vector3[] VertData;
    public float MaxHeight;
    public float[] Height;
    //public List<HouseCompute> HouseLocations;

    //public Texture2D TextureData;

    public MeshGeneratorData(Mesh mesh, Vector2[] chunkUVs, Vector3[] vertData, float[] height, float maxHeight)
    {
        _Mesh = mesh;
        ChunkUVs = chunkUVs;
        VertData = vertData;
        MaxHeight = maxHeight;
        Height = height;
        //HouseLocations = houseLocations;
        //TextureData = textureData;
    }
}


public class MeshGenerator
{
    private NoiseGenerator GenNoise;
    private PlanetoidMapEngineSolution Solution;
    private PlanetoidSizeSetting SizeSettings;
    private ComputeShader MeshCompute;
    private Gradient ColorGradient;

    public MeshGenerator(PlanetoidSizeSetting sizeSettings, NoiseGenerator genNoise, PlanetoidMapEngineSolution solution, ComputeShader meshCompute, Gradient colorGradient)
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

    private bool NormalCopyCheck(PlanetoidQuadTree Tree, PlanetoidQuadTree Other)
    {
        //return Other != null && Other.Resolution == 1 && Other.Face.Equals(Tree.Face) && Tree.Size == Other.Size;
        return Other != null && Other.Resolution == Tree.Resolution && Other.Size == Tree.Size && Other.Face.Equals(Tree.Face);
    }

    public MeshGeneratorData GenerateMeshComputeShader(PlanetoidQuadTree Tree)
    {
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
        int HouseDistanceApart = 16/Resolution;

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
        ComputeBuffer TriangleBufferList = new ComputeBuffer(MaxTriangle, sizeof (int) * 3, ComputeBufferType.Append);
        ComputeBuffer TriangleBufferCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        TriangleBufferList.SetCounterValue(0);
        ComputeBuffer UvBufferArray = new ComputeBuffer(VertRes * VertRes, sizeof(float) * 2);
        ComputeBuffer NoiseBufferArray = new ComputeBuffer(GenNoise.SimpleNoiseSettingsCompute.Length,Marshal.SizeOf(GenNoise.SimpleNoiseSettingsCompute[0]));
        NoiseBufferArray.SetData(GenNoise.SimpleNoiseSettingsCompute,0,0, GenNoise.SimpleNoiseSettingsCompute.Length);

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
        MeshCompute.SetInt("UpResolution", StitchResolution(Tree,UpTree));
        MeshCompute.SetInt("DownResolution", StitchResolution(Tree, DownTree));
        MeshCompute.SetInt("LeftResolution", StitchResolution(Tree, LeftTree));
        MeshCompute.SetInt("RightResolution", StitchResolution(Tree, RightTree));
        MeshCompute.SetFloat("Radius", Radius);
        MeshCompute.SetFloat("MaxHeight", GenNoise._MaxNoise);
        MeshCompute.SetVector("StartPosition", new Vector4(StartPosition.x,StartPosition.y));
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
        for (int i = 0; i<numTris;i++)
        {
            ChunkTriangles[(i * 3)] = ChunkTriangleComputeArr[i].A;
            ChunkTriangles[(i * 3)+1] = ChunkTriangleComputeArr[i].B;
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
        for(int i = 0;i < VertColors.Length; i++)
        {
            float height01 = GenNoise._MaxNoise == 0 ? 1f : Height[i] / GenNoise._MaxNoise;
            VertColors[i] = ColorGradient.Evaluate(height01);
        }

        Watch.Stop();

        if (Tree.IsPlayerTree) Debug.Log("Time To Create: " + Watch.ElapsedTicks);

        Mesh NewMesh = new Mesh();
        NewMesh.Clear();
        NewMesh.vertices = VertData;
        NewMesh.triangles = ChunkTriangles;
        NewMesh.uv = ChunkUVs;
        NewMesh.colors = VertColors;
        

        NewMesh.RecalculateNormals();
        VertNormals = NewMesh.normals;

        if(Tree.IsRoot)
        {
            for (int i = 0; i < VertColors.Length; i++)
            {
                VertNormals[i] = VertData[i].normalized;
            }
        }
        else if(Tree.Resolution == SizeSettings.MinResolution)
        {
            /*for (int y = 0; y < VertRes; y++)
            {
                int xLeft = 0;
                int xRight = VertRes - 1;

                int VertIndexLeft = (y * VertRes) + xLeft;
                int VertIndexRight = (y * VertRes) + xRight;

                VertNormals[VertIndexLeft] = VertData[VertIndexLeft].normalized;
                VertNormals[VertIndexRight] = VertData[VertIndexRight].normalized;
            }

            for (int x = 0; x < VertRes; x++)
            {
                int yUp = 0;
                int yDown = VertRes - 1;

                int VertIndexUp = (yUp * VertRes) + x;
                int VertIndexDown = (yDown * VertRes) + x;

                VertNormals[VertIndexUp] = VertData[VertIndexUp].normalized;
                VertNormals[VertIndexDown] = VertData[VertIndexDown].normalized;
            }*/

            if (NormalCopyCheck(Tree, UpTree))
            {
                GameObject ChunkObj = GameObject.Find(UpTree.Id);
                if (ChunkObj != null)
                {
                    PlanetoidChunk Chunk = ChunkObj.GetComponent<PlanetoidChunk>();
                    Mesh OtherMesh = Chunk.MeshData._Mesh;
                    Vector3[] OtherNormals = OtherMesh.normals;
                    if (OtherNormals.Length == VertNormals.Length)
                        for (int x = 0; x < VertRes; x++)
                        {
                            int OtherY = VertRes - 1;
                            int OtherNormalIndex = (OtherY * VertRes) + x;

                            int ThisY = 0;
                            int ThisNormalIndex = (ThisY * VertRes) + x;

                            VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
                        }
                }
            }

            if (NormalCopyCheck(Tree, DownTree))
            {
                GameObject ChunkObj = GameObject.Find(DownTree.Id);
                if (ChunkObj != null)
                {
                    PlanetoidChunk Chunk = ChunkObj.GetComponent<PlanetoidChunk>();
                    Mesh OtherMesh = Chunk.MeshData._Mesh;
                    Vector3[] OtherNormals = OtherMesh.normals;
                    if (OtherNormals.Length == VertNormals.Length)
                        for (int x = 0; x < VertRes; x++)
                        {
                            int OtherY = 0;
                            int OtherNormalIndex = (OtherY * VertRes) + x;

                            int ThisY = VertRes - 1;
                            int ThisNormalIndex = (ThisY * VertRes) + x;

                            VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
                        }
                }
            }

            if (NormalCopyCheck(Tree, LeftTree))
            {
                GameObject ChunkObj = GameObject.Find(LeftTree.Id);
                if (ChunkObj != null)
                {
                    PlanetoidChunk Chunk = ChunkObj.GetComponent<PlanetoidChunk>();
                    Mesh OtherMesh = Chunk.MeshData._Mesh;
                    Vector3[] OtherNormals = OtherMesh.normals;
                    if (OtherNormals.Length == VertNormals.Length)
                        for (int y = 0; y < VertRes; y++)
                        {
                            int OtherX = VertRes - 1;
                            int OtherNormalIndex = (y * VertRes) + OtherX;

                            int ThisX = 0;
                            int ThisNormalIndex = (y * VertRes) + ThisX;

                            VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
                        }

                }
            }

            if (NormalCopyCheck(Tree, RightTree))
            {
                GameObject ChunkObj = GameObject.Find(RightTree.Id);
                if (ChunkObj != null)
                {
                    PlanetoidChunk Chunk = ChunkObj.GetComponent<PlanetoidChunk>();
                    Mesh OtherMesh = Chunk.MeshData._Mesh;
                    Vector3[] OtherNormals = OtherMesh.normals;
                    if (OtherNormals.Length == VertNormals.Length)
                        for (int y = 0; y < VertRes; y++)
                        {
                            int OtherX = 0;
                            int OtherNormalIndex = (y * VertRes) + OtherX;

                            int ThisX = VertRes - 1;
                            int ThisNormalIndex = (y * VertRes) + ThisX;

                            VertNormals[ThisNormalIndex] = OtherNormals[OtherNormalIndex];
                        }

                }
            }
        }



        NewMesh.normals = VertNormals;


        MeshGeneratorData Data = new MeshGeneratorData(NewMesh,ChunkUVs, VertData, Height, GenNoise._MaxNoise);

        return Data;
    }

    public Mesh GenerateMeshManual(PlanetoidQuadTree Tree)
    {
        int MaxResolution = SizeSettings.MaxResolution;
        int Edges = Tree.Size;
        int Vert = Tree.Size + 1;
        int EdgeRes = (Tree.Size / Tree.Resolution);
        int VertRes = (Tree.Size / Tree.Resolution) + 1;
        int Resolution = Tree.Resolution;
        Vector2 StartPosition = Tree.Start;
        PlanetoidFace Face = Tree.Face;
        PlanetoidTreeManager Manager = Tree.TreeManager;

        //int[] ChunkTriangles = new int[EdgeRes * EdgeRes * 6];
        List<int> ChunkTriangles = new List<int>();
        Vector2[] ChunkUVs = new Vector2[VertRes * VertRes];
        Color[] VertColors = new Color[VertRes * VertRes];
        Vector3[] VertData = new Vector3[VertRes * VertRes];

        int TriangleIndex = 0;

        float[] NoiseData = GenNoise.GenerateNoise(StartPosition, Edges, Vert, EdgeRes, VertRes, Resolution, Face);
        float Radius = SizeSettings.Radius;
   
        PlanetoidQuadTree Up = Manager.FindNeighbourUp(Tree);
        PlanetoidQuadTree Down = Manager.FindNeighbourDown(Tree);
        PlanetoidQuadTree Left = Manager.FindNeighbourLeft(Tree);
        PlanetoidQuadTree Right = Manager.FindNeighbourRight(Tree);


        //bool EdgeFanUp =    Up != null      && Up.Resolution > Resolution             && Up.Size >= Tree.Size;
        //bool EdgeFanDown =  Down != null    && Down.Resolution > Resolution         && Down.Size >= Tree.Size;
        //bool EdgeFanRight = Right != null   && Right.Resolution > Resolution    && (Right.Resolution / Resolution == 2) && Right.Size >= Tree.Size;
        //bool EdgeFanLeft =  Left != null    && Left.Resolution > Resolution     && (Left.Resolution / Resolution == 2)  && Left.Size >= Tree.Size;

        bool EdgeFanUp = Up != null && Up.Resolution > Resolution && Up.Size >= Tree.Size;
        bool EdgeFanDown = Down != null && Down.Resolution > Resolution && Down.Size >= Tree.Size;
        bool EdgeFanRight = Right != null && Right.Resolution > Resolution && Right.Size >= Tree.Size;
        bool EdgeFanLeft = Left != null && Left.Resolution > Resolution && Left.Size >= Tree.Size;

        if (Tree.Id.Equals("UP_FACE-00-10-01-01-10-11"))
        {
            Debug.Log(Tree.ToString());
            Debug.Log(Up.ToString());
        }

        for (int y = 0; y < VertRes; y++)
        {
            for (int x = 0; x < VertRes; x++)
            {
                int VectorIndex = (y * VertRes) + x;

                float X = StartPosition.x + (x*Resolution);
                float Y = StartPosition.y + (y*Resolution);

                /*if (y==0 && Up!=null && EdgeFanUp)
                {
                    X = StartPosition.x;
                    if (x % 2 != 0)
                        X += ((x - 1) * Resolution);
                    else
                        X += (x * Resolution);
                }*/

                /*else if (y == VertRes - 1 && EdgeFanDown)
                {
                    X += (x * Resolution);
                }
                else
                {
                    X += (x * Resolution);
                }*/

                float noiseAtPoint = NoiseData[VectorIndex];
  
                VertData[VectorIndex] = (Solution.CubeToSphere(Face, X, Y) * (Radius + (Radius * 0.25f * noiseAtPoint)));



                /*if (y == VertRes - 1 && EdgeFanDown)
                {
                    int DivRes = Down.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (x % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (x % DivRes)];
                }*/




                /*if (y == VertRes - 1 && EdgeFanDown)
                {
                    int DivRes = Down.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (x % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (x % DivRes)];
                }

                else if (x == 0 && EdgeFanLeft)
                {
                    int DivRes = Left.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (y % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (VertRes * (y % DivRes))];
                }*/

                /*if (y == 0 && EdgeFanUp)
                {
                    int DivRes = Up.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (x % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (x % DivRes)];
                }
                else if (y == VertRes-1 && EdgeFanDown)
                {
                    int DivRes = Down.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (x % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (x % DivRes)];
                }

                if (x == VertRes - 1 && EdgeFanRight)
                {
                    int DivRes = Right.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (y % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (VertRes * (y % DivRes))];
                }
                else if (x == 0 && EdgeFanLeft)
                {
                    int DivRes = Left.Resolution / Resolution;
                    if (DivRes > 2) DivRes = 2;
                    if (y % DivRes != 0)
                        VertData[VectorIndex] = VertData[VectorIndex - (VertRes*(y % DivRes))];
                }*/


                VertColors[VectorIndex] = Color.HSVToRGB(((float)Resolution / (float)MaxResolution) / 2f, 1f, 1f);
                VertColors[VectorIndex] = Color.HSVToRGB(noiseAtPoint, 1f, 1f);

                ChunkUVs[VectorIndex] = new Vector2((float)x / ((float)EdgeRes), (float)y / ((float)EdgeRes));

                if (x < VertRes - 1 && y < VertRes - 1)
                {

                    if (x == 0 && EdgeFanLeft)
                    {
                        int DivRes = Left.Resolution / Resolution;
                        if (DivRes > 2) DivRes = 2;

                        if (y % DivRes == 0)
                        {
                            ChunkTriangles.Add(VectorIndex);
                            ChunkTriangles.Add(VectorIndex + VertRes + 1);
                            ChunkTriangles.Add(VectorIndex + VertRes + VertRes);

                            if (y > 0)
                            {
                                ChunkTriangles.Add(VectorIndex);
                                ChunkTriangles.Add(VectorIndex - VertRes + 1);
                                ChunkTriangles.Add(VectorIndex + VertRes + 1);
                            }
                        }
                       
                    }
                    else if (x == VertRes - 2 && EdgeFanRight)
                    {
                        int DivRes = Right.Resolution / Resolution;
                        if (DivRes > 2) DivRes = 2;

                        if (y % DivRes !=0)
                        {
                            ChunkTriangles.Add(VectorIndex);
                            ChunkTriangles.Add(VectorIndex - VertRes + 1);
                            ChunkTriangles.Add(VectorIndex + VertRes + 1);
                            
                        }

                    }

                    if (y == 0 && EdgeFanUp)
                    {
                        int DivRes = Up.Resolution / Resolution;
                        if (DivRes > 2) DivRes = 2;
        
                        if(x % DivRes == 0)
                        {
                            TriangleIndex += TriangleBoundaryUpNext(ChunkTriangles, VectorIndex, VertRes);
                            if(x>0)
                                TriangleIndex += TriangleBoundaryUpPrevious(ChunkTriangles, VectorIndex, VertRes);
                        }
                        
                        
                    }
                    else if (y == VertRes - 2 && EdgeFanDown)
                    {
                        int DivRes = Down.Resolution / Resolution;
                        if (DivRes > 2) DivRes = 2;

                        if (x % DivRes == 0)
                        {
                            TriangleIndex += TriangleBoundaryDownNext1(ChunkTriangles, VectorIndex, VertRes);    
                        }
                        else
                        {
                            TriangleIndex += TriangleBoundaryDownNext2(ChunkTriangles, VectorIndex, VertRes);
                        }
                    }
                    else
                    {
                        TriangleIndex += TriangleSquareNormal(ChunkTriangles, VectorIndex, VertRes);
                    }
                }
                else if (y == 0 && EdgeFanUp && x==VertRes-1)
                {
                    TriangleIndex += TriangleBoundaryUpPrevious(ChunkTriangles, VectorIndex, VertRes);
                }

            }
        }

        Mesh NewMesh = new Mesh();
        NewMesh.Clear();
        NewMesh.vertices = VertData;
        //NewMesh.triangles = ChunkTriangles;
        NewMesh.triangles = ChunkTriangles.ToArray();
        
        NewMesh.uv = ChunkUVs;
        NewMesh.colors = VertColors;
        NewMesh.RecalculateNormals();

        return NewMesh;
    }

    /*public static int TriangleSquareNormal( List<int> TriangleList, int TriangleIndex, int VectorIndex, int VerticesInRow)
    {
        //int[] TriangleList

        //TriangleList[TriangleIndex] = VectorIndex;
        //TriangleList[TriangleIndex + 1] = VectorIndex + 1 + VerticesInRow;
        //TriangleList[TriangleIndex + 2] = VectorIndex + VerticesInRow;

        //TriangleList[TriangleIndex + 3] = VectorIndex;
        //TriangleList[TriangleIndex + 4] = VectorIndex + 1;
        //TriangleList[TriangleIndex + 5] = VectorIndex + 1 + VerticesInRow;

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);
        TriangleList.Add(VectorIndex + VerticesInRow);

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);

        return 6;
    }*/

    public static int TriangleSquareNormal(List<int> TriangleList, int VectorIndex, int VerticesInRow)
    {

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);
        TriangleList.Add(VectorIndex + VerticesInRow);

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);

        return 6;
    }

    public static int TriangleBoundaryUpNext(List<int> TriangleList, int VectorIndex, int VerticesInRow)
    {

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);
        TriangleList.Add(VectorIndex + VerticesInRow);

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 2);
        TriangleList.Add(VectorIndex + 1 + VerticesInRow);

        return 6;
    }

    public static int TriangleBoundaryUpPrevious(List<int> TriangleList, int VectorIndex, int VerticesInRow)
    {
        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + VerticesInRow);
        TriangleList.Add(VectorIndex + VerticesInRow - 1);

        return 3;
    }

    public static int TriangleBoundaryDownNext1(List<int> TriangleList, int VectorIndex, int VerticesInRow)
    {

        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1 );
        TriangleList.Add(VectorIndex + VerticesInRow);

        return 6;
    }

    public static int TriangleBoundaryDownNext2(List<int> TriangleList, int VectorIndex, int VerticesInRow)
    {
        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + 1);
        TriangleList.Add(VectorIndex + VerticesInRow + 1);


        TriangleList.Add(VectorIndex);
        TriangleList.Add(VectorIndex + VerticesInRow + 1);
        TriangleList.Add(VectorIndex + VerticesInRow - 1);


        return 3;
    }
}
 