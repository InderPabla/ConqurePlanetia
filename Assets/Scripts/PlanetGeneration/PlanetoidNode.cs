using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlanetoidNodeLongLat
{
    public int LongXIndex;
    public int LatYIndex;
    public float LongLatScale;
    public float Radius;
    public float TrueLong;
    public float TrueLat;
    public Vector3 Point;
    public static float GOLDEN_RATIO = (1f + Mathf.Sqrt(5f)) / 2f;

    public PlanetoidNodeLongLat(float longLatScale, float radius, Vector3 point)
    {
        LongLatScale = longLatScale;
        Radius = radius;
        Point = point;
        TrueLong = -1;
        TrueLat = -1;
        LongXIndex = -1;
        LatYIndex = -1;

        this.Init();
    }

    public PlanetoidNodeLongLat(float longLatScale, float radius, float nodeIndex, float totalNodes)
    {
 
        float PhiRatio = (2f * nodeIndex) / totalNodes;

        float Phi = Mathf.Acos(1f - PhiRatio);
        float Theta = 2 * (Mathf.PI * nodeIndex) / GOLDEN_RATIO;

        float CosTheta = Mathf.Cos(Theta);
        float SinTheta = Mathf.Sin(Theta);
        float CosPhi = Mathf.Cos(Phi);
        float SinPhi = Mathf.Sin(Phi);

        Vector3 point = new Vector3(CosTheta * SinPhi, SinTheta * SinPhi, CosPhi) * radius;
        
        LongLatScale = longLatScale;
        Radius = radius;
        Point = point;
        TrueLong = -1;
        TrueLat = -1;
        LongXIndex = -1;
        LatYIndex = -1;

        this.Init();
    }

    private void Init()
    {
        TrueLong = Mathf.Atan2(Point.z, Point.x) * Mathf.Rad2Deg;
        TrueLat = 90 - (Mathf.Acos(Point.y / Radius) * Mathf.Rad2Deg);

        LongXIndex = (int)((TrueLong + 180) * LongLatScale);
        LatYIndex = (int)((TrueLat + 90) * LongLatScale);
    }

    override
    public string ToString()
    {
        return string.Format("[PlanetoidNodeLongLat] Scale:{0}, Radius:{1}, Index Long,Lat:{2},{3}, True Long,Lat:{4},{5}, Point:{6}", LongLatScale, Radius, LongXIndex, LatYIndex, TrueLong, TrueLat, Point);
    }
}

public enum PlanetoidNodeType
{
    EMPTY, FIR_TREE, OAK_TREE, GRASS
}

public class PlanetoidNode 
{
    public PlanetoidNodeManager NodeManager;
    public PlanetoidNode Parent;
    public Vector3 LocalSpacePoint;
    public Vector3 InitialHitNormal;
    public Vector3 InitialLocalSpaceHitPoint;
    public int NodeIndex;

    //public List<PlanetoidNode> Children;
    public PlanetoidNodeLongLat LongLatPoint;

    public PlanetoidNodeType NodeType;

    public PlanetoidNodeLongLat[] LongLatPointsNearIndex;

    public PlanetoidNode(PlanetoidNodeManager nodeManager, int nodeIndex, PlanetoidNodeLongLat longLatPoint, Vector3 localSpacePoint, Vector3 initialLocalSpaceHitPoint, Vector3 initialHitNormal, PlanetoidNodeType nodeType)
    {
        NodeManager = nodeManager;
        LocalSpacePoint = localSpacePoint;
        InitialHitNormal = initialHitNormal;
        InitialLocalSpaceHitPoint = initialLocalSpaceHitPoint;
        NodeIndex = nodeIndex;
        //Children = new List<PlanetoidNode>();
        LongLatPoint = longLatPoint;
        NodeType = nodeType;

        InitNearPoints();
    }
    static int kk = 0;
    private void InitNearPoints()
    {
        //Total Nodes = 3
        //0: 0.1, 0.2, 0.3, 0.4, 0.5 = 4
        //1: 0.6, 0.7, 0.8, 0.9, 1.1, 1.2, 1.3, 1.4, 1.5
        //2: 1.6, 1.7, 1.8, 1.9               
        float Radius = NodeManager.SizeSettings.Radius;
        float StartNodeIndexIncr = 0;
        float NodeIncr = 0.1f;
        float NodeIndexF = NodeIndex;

        int ItterationCount = -1;
        int IgnoreItterationCount = -1;
        int Index = 0;

        

        if (NodeIndexF == 0)
        {
            LongLatPointsNearIndex = new PlanetoidNodeLongLat[5];
            StartNodeIndexIncr = NodeIndexF + 0.1f;
            ItterationCount = 5;
            IgnoreItterationCount = -1;
        }
        else if(NodeIndexF == NodeManager.TotalNodes-1)
        {
            LongLatPointsNearIndex = new PlanetoidNodeLongLat[4];
            StartNodeIndexIncr = NodeIndexF - 0.4f;
            ItterationCount = 4;
            IgnoreItterationCount = -1;
        }
        else
        {
            LongLatPointsNearIndex = new PlanetoidNodeLongLat[9];
            StartNodeIndexIncr = NodeIndexF - 0.4f;
            ItterationCount = 10;
            IgnoreItterationCount = 5;
        }


        for (int Ittr = 1; Ittr<= ItterationCount; Ittr++)
        {
            float CurrentNodeIndex = ((float)(Ittr - 1) * NodeIncr) + StartNodeIndexIncr;
            if(Ittr != IgnoreItterationCount)
            {
                LongLatPointsNearIndex[Index] = new PlanetoidNodeLongLat(NodeManager.LongLatScale, Radius, CurrentNodeIndex, NodeManager.TotalNodes);
                Index++;
            }
        }

    }

    /*public void AddChildNode(PlanetoidNode ChildNode)
    {
        Children.Add(ChildNode);
        ChildNode.Parent = this;
    }*/

    /*public int ChildCount
    {
        get {
            return Children.Count;
        }
    }*/

    public bool Equals(PlanetoidNode Node)
    {
        return Node.LongLatPoint.LongXIndex == LongLatPoint.LongXIndex && Node.LongLatPoint.LatYIndex == LongLatPoint.LatYIndex;
    }

    public static PlanetoidNode GenNode(PlanetoidNodeManager Manager, int NodeIndex, int TotalNodes, float Radius, float LongLatScale, Vector3 PlanetCenter, PlanetoidNodeType NodeType)
    {
        /*float PhiRatio = (2f * (float)NodeIndex) / (float)TotalNodes;

        float Phi = Mathf.Acos(1f - PhiRatio);
        float Theta = 2 * (Mathf.PI * NodeIndex) / GOLDEN_RATIO;

        float CosTheta = Mathf.Cos(Theta);
        float SinTheta = Mathf.Sin(Theta);
        float CosPhi = Mathf.Cos(Phi);
        float SinPhi = Mathf.Sin(Phi);

        Vector3 LocalSpacePoint = new Vector3(CosTheta * SinPhi, SinTheta * SinPhi, CosPhi) * Radius;

        PlanetoidNodeLongLat LongLatPoint = new PlanetoidNodeLongLat(LongLatScale, Radius, LocalSpacePoint);*/

        

        PlanetoidNodeLongLat LongLatPoint = new PlanetoidNodeLongLat(LongLatScale, Radius, NodeIndex, TotalNodes);
        Vector3 LocalSpacePoint = LongLatPoint.Point;
        RaycastHit Hit = Manager.Operations.RaycastOnPlanet(LocalSpacePoint);
        Vector3 LocalSpaceInitialHitPoint = Hit.collider ? Hit.point - PlanetCenter : LocalSpacePoint;
        Vector3 LocalSpaceInitialHitNormal = Hit.collider ? Hit.normal : LocalSpacePoint.normalized;
        return new PlanetoidNode(Manager, NodeIndex, LongLatPoint, LocalSpacePoint, LocalSpaceInitialHitPoint, LocalSpaceInitialHitNormal, NodeType);
    }

}
