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

    public PlanetoidNodeLongLat(float longLatScale, float radius, Vector3 point)
    {
        LongLatScale = longLatScale;
        Radius = radius;
        Point = point;

        TrueLong = Mathf.Atan2(Point.z,Point.x) * Mathf.Rad2Deg;
        TrueLat = 90 - (Mathf.Acos(Point.y / Radius) * Mathf.Rad2Deg);

        LongXIndex = (int)((TrueLong + 180)*LongLatScale);
        LatYIndex = (int)((TrueLat + 90)*LongLatScale);
    }

    override
    public string ToString()
    {
        return string.Format("[PlanetoidNodeLongLat] Scale:{0}, Radius:{1}, Index Long,Lat:{2},{3}, True Long,Lat:{4},{5}, Point:{6}", LongLatScale, Radius, LongXIndex, LatYIndex, TrueLong, TrueLat, Point);
    }
}

public enum PlanetoidNodeType
{
    EMPTY, TREE
}

public class PlanetoidNode 
{
    public PlanetoidNodeManager NodeManager;
    public PlanetoidNode Parent;
    public Vector3 LocalSpacePoint;
    public Vector3 InitialHitNormal;
    public Vector3 InitialLocalSpaceHitPoint;
    public int NodeIndex;

    public List<PlanetoidNode> Children;
    public PlanetoidNodeLongLat LongLatPoint;
    public static float GOLDEN_RATIO = (1f + Mathf.Sqrt(5f)) / 2f;
    public PlanetoidNodeType NodeType;

    public PlanetoidNode(PlanetoidNodeManager nodeManager, int nodeIndex, PlanetoidNodeLongLat longLatPoint, Vector3 localSpacePoint, Vector3 initialLocalSpaceHitPoint, Vector3 initialHitNormal, PlanetoidNodeType nodeType)
    {
        NodeManager = nodeManager;
        LocalSpacePoint = localSpacePoint;
        InitialHitNormal = initialHitNormal;
        InitialLocalSpaceHitPoint = initialLocalSpaceHitPoint;
        NodeIndex = nodeIndex;
        Children = new List<PlanetoidNode>();
        LongLatPoint = longLatPoint;
        NodeType = nodeType;
    }

    public void AddChildNode(PlanetoidNode ChildNode)
    {
        Children.Add(ChildNode);
        ChildNode.Parent = this;
    }

    public int ChildCount
    {
        get {
            return Children.Count;
        }
    }

    public bool Equals(PlanetoidNode Node)
    {
        return Node.LongLatPoint.LongXIndex == LongLatPoint.LongXIndex && Node.LongLatPoint.LatYIndex == LongLatPoint.LatYIndex;
    }

    public static PlanetoidNode GenNode(PlanetoidNodeManager Manager, int NodeIndex, int TotalNodes, float Radius, float LongLatScale, Vector3 PlanetCenter, PlanetoidNodeType NodeType)
    {
        float PhiRatio = (2f * (float)NodeIndex) / (float)TotalNodes;

        float Phi = Mathf.Acos(1f - PhiRatio);
        float Theta = 2 * (Mathf.PI * NodeIndex) / GOLDEN_RATIO;

        float CosTheta = Mathf.Cos(Theta);
        float SinTheta = Mathf.Sin(Theta);
        float CosPhi = Mathf.Cos(Phi);
        float SinPhi = Mathf.Sin(Phi);

        Vector3 LocalSpacePoint = new Vector3(CosTheta * SinPhi, SinTheta * SinPhi, CosPhi) * Radius;
        PlanetoidNodeLongLat LongLatPoint = new PlanetoidNodeLongLat(LongLatScale, Radius, LocalSpacePoint);
        RaycastHit Hit = Manager.Operations.RaycastOnPlanet(LocalSpacePoint);

        Vector3 LocalSpaceInitialHitPoint = Hit.collider ? Hit.point - PlanetCenter : LocalSpacePoint;
        Vector3 LocalSpaceInitialHitNormal = Hit.collider ? Hit.normal : LocalSpacePoint.normalized;

        return new PlanetoidNode(Manager, NodeIndex, LongLatPoint, LocalSpacePoint, LocalSpaceInitialHitPoint, LocalSpaceInitialHitNormal, NodeType);
        

    }
}
