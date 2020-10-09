using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetoidNodeData
{
    public int NodeIndex;
    public List<int> ChildrenNodeIndexList = new List<int>();
    public PlanetoidNodeData(int nodeIndex)
    {
        NodeIndex = nodeIndex;
    }
}

public class PlanetoidNodeManager 
{
    
    public int TotalNodes;
    private PlanetoidNode[] AllNodeArr;
   
    private float MinDistanceBetweenTwoPoints = 50f;
    private float MinDistanceBetweenTwoPointsScale = 2f;
    //private List<PlanetoidNode> RootNodes;
    private List<PlanetoidNode>[,] AllNodes2dArr;

    public PlanetoidMapEngineOperation Operations;
    public PlanetoidSizeSetting SizeSettings;
    public int LongitudeXCount;
    public int LatitudeYCount;
    public float LongLatScale;

    public PlanetoidNodeManager(PlanetoidSizeSetting sizeSettings, PlanetoidMapEngineOperation operations)
    {
        SizeSettings = sizeSettings;
        Operations = operations;

        float SurfaceAreaRatioToSmall = (SizeSettings.SurfaceArea / PlanetoidSizeSetting.SmallPlanet.SurfaceArea);
        TotalNodes = (int)(SizeSettings.NodeScaler * SurfaceAreaRatioToSmall);
        LongLatScale = (Mathf.Sqrt(SurfaceAreaRatioToSmall))/10f;
        LongitudeXCount = (int)(180 * 2 * LongLatScale);
        LatitudeYCount = (int)(90 * 2 * LongLatScale);
        Debug.Log(SizeSettings.PlanetName+"- Total Nodes:"+TotalNodes+"- Long,Lat:"+LongitudeXCount+","+LatitudeYCount);
    }

    public void InitNodes()
    {
        float Radius = SizeSettings.Radius;
        
        Vector3 PlanetCenter = Operations.WorldSpacePlanetLocation();
        Vector3 PlayerLocation = Operations.WorldSpacePlayerLocation();
        AllNodeArr = new PlanetoidNode[TotalNodes];
        AllNodes2dArr = new List<PlanetoidNode>[LatitudeYCount, LongitudeXCount];
        float MinDistance = float.MaxValue;
        Sphere sphere = SizeSettings.Sphere;

        List<int> NodeIndexList = new List<int>();
        System.Random Rand = new System.Random(SizeSettings.Seed);

        for (int NodeIndex = 0; NodeIndex < TotalNodes; NodeIndex++)
        {
            float RandVal = (float)Rand.NextDouble();
            PlanetoidNodeType NodeType = RandVal <= 0.05f ? PlanetoidNodeType.FIR_TREE : RandVal <= 0.1f ? PlanetoidNodeType.OAK_TREE : RandVal <= 0.2f ? PlanetoidNodeType.GRASS : PlanetoidNodeType.EMPTY;

            AllNodeArr[NodeIndex] = PlanetoidNode.GenNode(this, NodeIndex, TotalNodes, Radius, LongLatScale, PlanetCenter, NodeType);
            PlanetoidNodeLongLat LongLatPoint = AllNodeArr[NodeIndex].LongLatPoint;
            if (NodeIndex != 0)
            {
                float DistanceBetweenPoint = sphere.Distance(AllNodeArr[NodeIndex].LocalSpacePoint, AllNodeArr[0].LocalSpacePoint);
                if (DistanceBetweenPoint < MinDistance)
                {
                    MinDistance = DistanceBetweenPoint;
                }
            }

            NodeIndexList.Add(NodeIndex);
            if(AllNodes2dArr[LongLatPoint.LatYIndex, LongLatPoint.LongXIndex]==null)
            {
                AllNodes2dArr[LongLatPoint.LatYIndex, LongLatPoint.LongXIndex] = new List<PlanetoidNode>();
            }
            AllNodes2dArr[LongLatPoint.LatYIndex, LongLatPoint.LongXIndex].Add(AllNodeArr[NodeIndex]);
        }

        ///if (MinDistance < 50f) MinDistance = 50f;
        MinDistanceBetweenTwoPoints = MinDistance * MinDistanceBetweenTwoPointsScale;

    
        float MinDistanceStarted = MinDistanceBetweenTwoPoints * Mathf.Pow(((float)SizeSettings.EdgePower / (float)PlanetoidSizeSetting.SmallPlanet.EdgePower), 8) * 4f;

        string ChildCount = "[";
        string NodeCount = "[";
        Dictionary<string, int> DistinctNodeCountsOnLongLat = new Dictionary<string, int>();
        int indexCount = 0;

       /* List<PlanetoidNodeData> RootNodeDataList = InitRootNodeTrees(MinDistanceStarted, NodeIndexList, null);
        RootNodes = new List<PlanetoidNode>();
        for(int r = 0; r < RootNodeDataList.Count; r++)
        {
            RootNodes.Add(AllNodeArr[RootNodeDataList[r].NodeIndex]);
        }

       
        List<PlanetoidNode> NodeList = RootNodes;
       
        while (NodeList != null)
        {
            if (indexCount == 0) ChildCount += NodeList.Count + "";
            else ChildCount += "," + NodeList.Count;

            if(NodeList.Count==0) NodeList = null;
            else NodeList = NodeList[0].Children;

            indexCount++;
        }
        ChildCount += "]";*/


        for(int y = 0; y<LatitudeYCount;y++)
        {
            for (int x = 0; x < LongitudeXCount; x++)
            {
                string key = -1+"";
                if (AllNodes2dArr[y, x] != null)
                {
                    key = AllNodes2dArr[y, x].Count+"";
                }

                if (!DistinctNodeCountsOnLongLat.ContainsKey(key)) DistinctNodeCountsOnLongLat.Add(key, 1);
                else DistinctNodeCountsOnLongLat[key] = DistinctNodeCountsOnLongLat[key]+1;
               
            }
        }
        IEnumerator<string> It = DistinctNodeCountsOnLongLat.Keys.GetEnumerator();


        indexCount = 0;
        while (It.MoveNext())
        {
            string count = (It.Current.Equals("-1")?"None": It.Current) + ":" + DistinctNodeCountsOnLongLat[It.Current];
            if(indexCount==0) NodeCount += count+"";
            else NodeCount += ", " + count;
            indexCount++;
        }
        NodeCount += "]";

        string Info = string.Format("{0}: Child Count:{1}, Node Count:{2}",SizeSettings.PlanetName, ChildCount, NodeCount);
        Debug.Log(Info);
    }
    
    public PlanetoidNode FindNearestNode(Vector3 LocalSpacePointOnSphere)
    {
        PlanetoidNodeLongLat SpherePoint = new PlanetoidNodeLongLat(LongLatScale, SizeSettings.Radius, LocalSpacePointOnSphere);
        int CenterXIndex = SpherePoint.LongXIndex;
        int CenterYIndex = SpherePoint.LatYIndex;
        int Boundry = 0;
        Sphere sphere = SizeSettings.Sphere;
        PlanetoidNode NearestNode = null;
        while(NearestNode==null)
        {
            int LeftX = CenterXIndex - Boundry;
            int RightX = CenterXIndex + Boundry;
            int UpY = CenterYIndex - Boundry;
            int DownY = CenterYIndex + Boundry;

            /*if (LongitudeXCount < (Boundry * 2))
            {
                LeftX = 0;
                RightX = LongitudeXCount - 1;
            }
            if (LatitudeYCount < (Boundry * 2))
            {
                UpY = 0;
                DownY = LatitudeYCount - 1;
            }
            if (LeftX < 0) LeftX = LeftX + LongitudeXCount;
            if (RightX > LongitudeXCount - 1) RightX = RightX - LongitudeXCount;
            if (UpY < 0) UpY = UpY + LatitudeYCount;
            if (DownY > LatitudeYCount - 1) DownY = DownY - LatitudeYCount;
            if (LeftX > RightX)
            {
                int Temp = LeftX;
                LeftX = RightX;
                RightX = Temp;
            }
            if (UpY > DownY)
            {
                int Temp = UpY;
                UpY = DownY;
                DownY = Temp;
            }*/


            LeftX = LeftX < 0 ? 0 : LeftX;
            RightX = RightX >= LongitudeXCount ? LongitudeXCount - 1 : RightX;
            UpY = UpY < 0 ? 0 : UpY;
            DownY = DownY >= LatitudeYCount ? LatitudeYCount - 1 : DownY;

            List<PlanetoidNode> NodesOnBoundary = new List<PlanetoidNode>();


            for (int y = UpY; y<= DownY; y++)
            {
                /*int yy = y < 0 ? y + LatitudeYCount : y >= LatitudeYCount ? y - LatitudeYCount : y;
                int lxx = LeftX < 0 ? LeftX + LongitudeXCount : LeftX >= LongitudeXCount ? LeftX - LongitudeXCount : LeftX;
                int rxx = RightX < 0 ? RightX + LongitudeXCount : RightX >= LongitudeXCount ? RightX - LongitudeXCount : RightX;
                if (AllNodes2dArr[yy, lxx] != null) NodesOnBoundary.AddRange(AllNodes2dArr[yy, lxx]);
                if (AllNodes2dArr[yy, rxx] != null) NodesOnBoundary.AddRange(AllNodes2dArr[yy, rxx]);*/
                if (AllNodes2dArr[y, LeftX] != null) NodesOnBoundary.AddRange(AllNodes2dArr[y, LeftX]);
                if (AllNodes2dArr[y, RightX] != null) NodesOnBoundary.AddRange(AllNodes2dArr[y, RightX]);
            }

            for (int x = LeftX+1; x <= RightX-1; x++)
            {
                /*int uyy = UpY < 0 ? UpY + LatitudeYCount : UpY >= LatitudeYCount ? UpY - LatitudeYCount : UpY;
                int dyy = DownY < 0 ? DownY + LatitudeYCount : DownY >= LatitudeYCount ? DownY - LatitudeYCount : DownY;
                int xx = x < 0 ? x + LongitudeXCount : x >= LongitudeXCount ? x - LongitudeXCount : x;
                if (AllNodes2dArr[uyy, xx] != null) NodesOnBoundary.AddRange(AllNodes2dArr[uyy, xx]);
                if (AllNodes2dArr[dyy, xx] != null) NodesOnBoundary.AddRange(AllNodes2dArr[dyy, xx]);*/
                if (AllNodes2dArr[UpY, x] != null) NodesOnBoundary.AddRange(AllNodes2dArr[UpY, x]);
                if (AllNodes2dArr[DownY, x] != null) NodesOnBoundary.AddRange(AllNodes2dArr[DownY, x]);
            }

            NodesOnBoundary.ForEach((PlanetoidNode Node) => {
                if(NearestNode == null || sphere.Distance(LocalSpacePointOnSphere,Node.LocalSpacePoint) < sphere.Distance(LocalSpacePointOnSphere, NearestNode.LocalSpacePoint))
                    //if(Node.Parent!=null)
                        NearestNode = Node; 
            });

            Boundry++;
        }


        return NearestNode;
    }

    public List<PlanetoidNode> FindNearestNodesWithinBlockMeters(PlanetoidNode NearestNode, int IndexRange)
    {
        List<PlanetoidNode> NearestNodes = new List<PlanetoidNode>();
        Sphere sphere = SizeSettings.Sphere;

        float CenterX = (float)NearestNode.LongLatPoint.LongXIndex;
        float CenterY = (float)NearestNode.LongLatPoint.LatYIndex;

        float Scale = Mathf.Abs(CenterY - ((float)LatitudeYCount/2f))/ ((float)LatitudeYCount / 2f);
        float ScaleX = Scale + 1f;
        float ScaleY = (1f - Scale);
        ScaleY *= 2f;
        ScaleY = 1f;
        float IndexRangeX = (float)IndexRange * ScaleX;
        float IndexRangeY = (float)IndexRange * ScaleY;

        float LeftXF = CenterX - IndexRangeX;
        float RightXF = CenterX + IndexRangeX;
        float UpYF = CenterY - IndexRangeY;
        float DownYF = CenterY + IndexRangeY;

        if(IndexRangeX * 2f>= LongitudeXCount)
        {
            LeftXF = 0;
            RightXF = LongitudeXCount - 1f;
        }
        else
        {
            /*if (LeftXF < 0) {
                RightXF = LeftXF + LongitudeXCount;
                LeftXF = RightXF - 
            }
            else if (RightXF > LongitudeXCount - 1) {
                RightXF = RightXF - LongitudeXCount;
            }


            if (LeftXF > RightXF)
            {
                float Temp = LeftXF;
                LeftXF = RightXF;
                RightXF = Temp;
            }*/
        }

        if (IndexRangeY * 2f >= LatitudeYCount)
        {
            UpYF = 0;
            DownYF = LatitudeYCount - 1f;
        }
        else
        {
            /*if (UpYF < 0) UpYF = UpYF + LatitudeYCount;
            if (DownYF > LatitudeYCount - 1) DownYF = DownYF - LatitudeYCount;

            if (UpYF > DownYF)
            {
                float Temp = UpYF;
                UpYF = DownYF;
                DownYF = Temp;
            }*/

            if(UpYF<0)
            {
                UpYF = 0;
                DownYF = UpYF + (IndexRangeY * 2f);
            }
            else if (DownYF> LatitudeYCount-1)
            {
                DownYF = LatitudeYCount - 1f;
                UpYF = DownYF - (IndexRangeY * 2f);
            }
        }



        //LeftXF = LeftXF < 0 ? 0 : LeftXF;
        //RightXF = RightXF >= LongitudeXCount ? LongitudeXCount - 1 : RightXF;
        //UpYF = UpYF < 0 ? 0 : UpYF;
        //DownYF = DownYF >= LatitudeYCount ? LatitudeYCount - 1 : DownYF;

        int LeftX = (int)LeftXF;
        int RightX = (int)RightXF;
        int UpY = (int)UpYF;
        int DownY = (int)DownYF;

        Debug.Log(LeftX + "," + UpY + "  " + RightX + "," + DownY + "  " + CenterX + "," + CenterY+"  "+(CenterX/((float)LongitudeXCount-1))+","+ (CenterY / ((float)LatitudeYCount - 1)));

        for (int y = UpY; y <= DownY; y++)
        {
            //int yy = y < 0 ? y + LatitudeYCount : y >= LatitudeYCount ? y - LatitudeYCount : y;

            for (int x = LeftX; x <= RightX; x++)
            {
                int xx = x < 0 ? x + LongitudeXCount : x >= LongitudeXCount ? x - LongitudeXCount : x;
                if (AllNodes2dArr[y, xx] != null)
                    NearestNodes.AddRange(AllNodes2dArr[y, xx]);
                    //NearestNodes.AddRange(AllNodes2dArr[y, xx].FindAll(v=>v.NodeType==PlanetoidNodeType.TREE));
            }
        }
               
            
        

        return NearestNodes;
    }

    public PlanetoidNode GetNodeAtIndex(int Index)
    {
        return AllNodeArr[Index];
    }

    /*private List<PlanetoidNodeData> InitRootNodeTrees(float MinDistance, List<int> NodeIndexList, PlanetoidNodeData ParentNodeData)
    {
        Sphere sphere = SizeSettings.Sphere;
        List<PlanetoidNodeData> RootNodeDataList = new List<PlanetoidNodeData>();

        if (MinDistance < MinDistanceBetweenTwoPoints)
        {
            MinDistance = MinDistanceBetweenTwoPoints;
        }


        for (int i = NodeIndexList.Count - 1; i >= 0; i--)
        {

            PlanetoidNodeData RootNodeData = null;
            PlanetoidNode CheckNode = AllNodeArr[NodeIndexList[i]];

            for (int r = RootNodeDataList.Count - 1; r >= 0; r--)
            {
                PlanetoidNode RootNode = AllNodeArr[RootNodeDataList[r].NodeIndex];

                if (sphere.Distance(RootNode.LocalSpacePoint, CheckNode.LocalSpacePoint) <= MinDistance)
                {
                    RootNodeData = RootNodeDataList[r];
                    break;
                }
            }

            if (RootNodeData == null)
            {
                RootNodeDataList.Add(new PlanetoidNodeData(NodeIndexList[i]));
            }
            else
            {
                RootNodeData.ChildrenNodeIndexList.Add(CheckNode.NodeIndex);
            }

            NodeIndexList.RemoveAt(i);
        }

        MinDistance = MinDistance / PlanetoidNode.GOLDEN_RATIO;

        if (RootNodeDataList.Count == 0)
        {
            if (ParentNodeData != null)
            {
                for (int i = NodeIndexList.Count - 1; i >= 0; i--)
                {
                    AllNodeArr[ParentNodeData.NodeIndex].AddChildNode(AllNodeArr[NodeIndexList[i]]);
                }
            }

        }
        else if (ParentNodeData != null)
        {
            for (int r = RootNodeDataList.Count - 1; r >= 0; r--)
            {
                PlanetoidNodeData RootNodeData = RootNodeDataList[r];
                AllNodeArr[ParentNodeData.NodeIndex].AddChildNode(AllNodeArr[RootNodeData.NodeIndex]);
                InitRootNodeTrees(MinDistance, RootNodeData.ChildrenNodeIndexList, RootNodeData);
            }
        }
        else
        {
            for (int r = RootNodeDataList.Count - 1; r >= 0; r--)
            {
                PlanetoidNodeData RootNodeData = RootNodeDataList[r];
                InitRootNodeTrees(MinDistance, RootNodeData.ChildrenNodeIndexList, RootNodeData);
            }
        }

        return RootNodeDataList;
    }*/
}
