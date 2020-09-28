using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlanetoidOperationUpdateTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationDeleteTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationCreateTree(PlanetoidQuadTree Tree);
public delegate PlanetoidQuadTree PlanetoidOperationCurrentTree(PlanetoidQuadTree Tree);
public delegate bool PlanetoidOperationTreeIsCreated(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationReplaceTree(PlanetoidQuadTree Tree);

public delegate Vector3 PlanetoidOperationWorldSpacePlayerLocation();
public delegate Vector3 PlanetoidOperationWorldSpacePlanetLocation();
public delegate PlanetoidQuadTree PlanetoidOperationTreeUnderPlayer();

public delegate RaycastHit PlanetoidOperationRaycastOnPlanet(Vector3 LocalPointOnSphere);

public delegate Vector3 SolutionCubeToSphere(PlanetoidFace Face, float X, float Y);
public delegate PlanetoidNode SolutionFindNearestNodeToPlayer();
public delegate List<PlanetoidNode> SolutionFindNearestNodeToPlayerWithinIndexRange(int IndexRange);

public class PlanetoidMapEngineOperation
{
    public PlanetoidOperationUpdateTree UpdateTree;
    public PlanetoidOperationCreateTree CreateTree;
    public PlanetoidOperationDeleteTree DeleteTree;
    public PlanetoidOperationCurrentTree CurrentTree;
    public PlanetoidOperationReplaceTree ReplaceTree;

    public PlanetoidOperationWorldSpacePlayerLocation WorldSpacePlayerLocation;
    public PlanetoidOperationWorldSpacePlanetLocation WorldSpacePlanetLocation;

    public PlanetoidOperationTreeIsCreated IsTreeCreated;

    public PlanetoidOperationTreeUnderPlayer TreeUnderPlayer;
    public PlanetoidOperationRaycastOnPlanet RaycastOnPlanet;

    public PlanetoidMapEngineOperation(PlanetoidOperationUpdateTree updateTree, PlanetoidOperationCreateTree createTree, PlanetoidOperationDeleteTree deleteTree
        , PlanetoidOperationReplaceTree replaceTree, PlanetoidOperationCurrentTree currentTree
        , PlanetoidOperationWorldSpacePlayerLocation worldSpacePlayerLocation, PlanetoidOperationWorldSpacePlanetLocation worldSpacePlanetLocation
        , PlanetoidOperationTreeIsCreated isTreeCreated, PlanetoidOperationTreeUnderPlayer treeUnderPlayer
        , PlanetoidOperationRaycastOnPlanet raycastOnPlanet
    )
    {
        UpdateTree = updateTree;
        DeleteTree = deleteTree;
        CreateTree = createTree;
        CurrentTree = currentTree;
        ReplaceTree = replaceTree;

        WorldSpacePlayerLocation = worldSpacePlayerLocation;
        WorldSpacePlanetLocation = worldSpacePlanetLocation;

        IsTreeCreated = isTreeCreated;

        TreeUnderPlayer = treeUnderPlayer;
        RaycastOnPlanet = raycastOnPlanet;
    }
}

public class PlanetoidMapEngineSolution
{
    public SolutionCubeToSphere CubeToSphere;
    public SolutionFindNearestNodeToPlayer FindNearestNodeToPlayer;
    public SolutionFindNearestNodeToPlayerWithinIndexRange FindNearestNodeToPlayerWithinIndexRange;

    public PlanetoidMapEngineSolution(SolutionCubeToSphere cubeToSphere, SolutionFindNearestNodeToPlayer findNearestNodeToPlayer,
            SolutionFindNearestNodeToPlayerWithinIndexRange findNearestNodeToPlayerWithinIndexRange)
    {
        CubeToSphere = cubeToSphere;
        FindNearestNodeToPlayer = findNearestNodeToPlayer;
        FindNearestNodeToPlayerWithinIndexRange = findNearestNodeToPlayerWithinIndexRange;
    }

}

public class PlanetoidMapEngine
{
    private PlanetoidSizeSetting SizeSetting;

    public PlanetoidRenderType RenderType;

    private PlanetoidMapEngineOperation Operation;

    private PlanetoidTreeManager TreeManager;

    private PlanetoidNodeManager NodeManager;

    public PlanetoidMapEngineSolution Solution;

    private Vector3 CurrentLocalSpacePlayerLocationOnSphere = Vector3.positiveInfinity;

    private bool CreateImmediate = false;

    public NoiseGenerator GenNoise;
    public ChunkMeshGenerator GenMesh;

    public PlanetoidMapEngine(PlanetoidSizeSetting sizeSetting, PlanetoidMapEngineOperation operation, Gradient ColorGradient, SimpleNoiseSetting[] NoiseSettings, bool IgnoreNoiseSetting, ComputeShader MeshCompute)
    {
        SizeSetting = sizeSetting;
        Operation = operation;
        RenderType = PlanetoidRenderType.PLAYER_VERY_FAR;
        Solution = new PlanetoidMapEngineSolution(this.SolutionCubeToSphere,this.FindNearestNodeToPlayer,this.FindNearestNodeToPlayerWithinIndexRange);

        GenNoise = new NoiseGenerator(NoiseSettings, Solution, IgnoreNoiseSetting);
        GenMesh = new ChunkMeshGenerator(SizeSetting, GenNoise, Solution, MeshCompute, ColorGradient);
    }

    public bool Update()
    {
        bool UpdateOccured = false;
        if (TreeManager == null || TreeManager.IsAllQueueEmpty())
        {
            UpdateOccured = UpdateTreeManager();
        }

        UpdateTreeGeneration();

        return UpdateOccured;
    }

    private void UpdateTreeGeneration()
    {
        int CreateEdgeCount = 128;
        int UpdateEdgeCount = 128;

        TreeManager.ForEachCreate((PlanetoidQuadTree Tree, int Index) => {
            if (Tree.ToCreate == false) throw new System.Exception(string.Format("Attempting to create PlanetoidQuadTree({0}) with ToCreate false.", Tree.Id));

            int Count = Tree.Size / Tree.Resolution;
            if (Count <= CreateEdgeCount)
            {
                CreateEdgeCount -= Count;
                Operation.CreateTree(Tree);
                return true;
            }
            return false;
        });

        TreeManager.ForEachUpdate((PlanetoidQuadTree NewTree, int Index) => {
            if (NewTree.ToCreate == false) throw new System.Exception(string.Format("Attempting to update PlanetoidQuadTree({0}) with ToCreate false.", NewTree.Id));

            int Count = NewTree.Size / NewTree.Resolution;
            if (Count <= UpdateEdgeCount)
            {
                PlanetoidQuadTree OldTree = Operation.CurrentTree(NewTree);

                PlanetoidQuadTree UpNew = NewTree.TreeManager.FindNeighbourUp(NewTree);
                PlanetoidQuadTree DownNew = NewTree.TreeManager.FindNeighbourDown(NewTree);
                PlanetoidQuadTree LeftNew = NewTree.TreeManager.FindNeighbourLeft(NewTree);
                PlanetoidQuadTree RightNew = NewTree.TreeManager.FindNeighbourRight(NewTree);

                PlanetoidQuadTree UpOld = OldTree.TreeManager.FindNeighbourUp(OldTree);
                PlanetoidQuadTree DownOld = OldTree.TreeManager.FindNeighbourDown(OldTree);
                PlanetoidQuadTree LeftOld = OldTree.TreeManager.FindNeighbourLeft(OldTree);
                PlanetoidQuadTree RightOld = OldTree.TreeManager.FindNeighbourRight(OldTree);


                if (OldTree.Resolution == NewTree.Resolution
                    && (PlanetoidQuadTree.EqualsWithResolution(UpNew, UpOld)
                        && PlanetoidQuadTree.EqualsWithResolution(DownNew, DownOld)
                        && PlanetoidQuadTree.EqualsWithResolution(LeftNew, LeftOld)
                        && PlanetoidQuadTree.EqualsWithResolution(RightNew, RightOld)
                       )
                )
                {
                    Operation.ReplaceTree(NewTree);
                    return true;
                }



                UpdateEdgeCount -= Count;
                Operation.UpdateTree(NewTree);
                return true;
            }
            return false;
        });


        if (TreeManager.IsCreateAndUpdateQueueEmpty())
        {
            TreeManager.ForEachDelete((PlanetoidQuadTree Tree, int Index) => {
                Operation.DeleteTree(Tree);
                return true;
            });
        }
    }

    private bool UpdateTreeManager()
    {
        bool UpdateOccured = false;

        int MaxTreeDepth = SizeSetting.MaxTreeDepth;
        Vector3 WorldSpacePlayerLocation = Operation.WorldSpacePlayerLocation();
        Vector3 WorldSpacePlanetLocation = Operation.WorldSpacePlanetLocation();
        Vector3 Direction = (WorldSpacePlayerLocation - WorldSpacePlanetLocation).normalized;
        Vector3 LocalSpacePlayerLocationOnSphere = Direction * SizeSetting.Radius;
        Sphere sphere = SizeSetting.Sphere;


        float DistanceToSurface = Vector3.Distance(WorldSpacePlayerLocation, WorldSpacePlanetLocation);

        if (DistanceToSurface <= 0f) return false;
        
        PlanetoidRenderType NewRenderType = SizeSetting.RenderTypeForDistanceToSurface(DistanceToSurface-50,GenNoise._MaxNoise);
 
        PlanetoidFace FaceUnderPlayer = PlanetoidFace.FindFace(WorldSpacePlayerLocation, WorldSpacePlanetLocation);

        PlanetoidRenderType OldRenderType = RenderType;
        RenderType = NewRenderType;

        Vector2 FaceSpacePlayerLocation = SphereToLocalGrid(FaceUnderPlayer, Direction) * SizeSetting.MaxEdgeTiles;

        PlanetoidQuadTree PlayerTreeFromRaycast = Operation.TreeUnderPlayer();

        if (RenderType <= PlanetoidRenderType.PLAYER_ON_PLANET)
        {

            if (sphere.Distance(LocalSpacePlayerLocationOnSphere, CurrentLocalSpacePlayerLocationOnSphere) <= 400f) return false;

            PlanetoidTreeManager NewTreeManger = new PlanetoidTreeManager(SizeSetting);
            //NewTreeManger.SetResolutionToBeSize();

            PlanetoidQuadTree PlayerTree = null;
            PlayerTreeFromRaycast = null;

            if (PlayerTreeFromRaycast != null && PlayerTreeFromRaycast.Depth == MaxTreeDepth)
            {
                PlayerTree = NewTreeManger.CreateMaxLeafAtFaceAndLocation(PlayerTreeFromRaycast.Face, PlayerTreeFromRaycast.Center);
            }
            else
            {
                PlayerTree = NewTreeManger.CreateMaxLeafAtFaceAndLocation(FaceUnderPlayer, FaceSpacePlayerLocation);
            }


            DiamondOnTree(PlayerTree, NewTreeManger, 18, SizeSetting.MinResolution, 2);


            CreateImmediate = false;
            NewTreeManger.CreateMe(TreeManager, DiffAction);
            TreeManager = NewTreeManger;

            CurrentLocalSpacePlayerLocationOnSphere = LocalSpacePlayerLocationOnSphere;
            UpdateOccured = true;
        }
        /*else if (RenderType <= PlanetoidRenderType.PLAYER_VERY_FAR)
        {
            PlanetoidTreeManager NewTreeManger = new PlanetoidTreeManager(SizeSetting);
            int Res = SizeSetting.MaxResolution / 8;
            Res = Res == 0 ? 1 : Res;
            NewTreeManger.SetResolution(Res);
            NewTreeManger.CreateMe(TreeManager, DiffAction);
            TreeManager = NewTreeManger;
            CurrentPlayerTree = null;
        }*/
        else if (OldRenderType != NewRenderType)
        {
            //Debug.Log(OldRenderType + " " + NewRenderType + " are different. Init()");
            //CurrentPlayerTree = null;

            Init();
            UpdateOccured = true;
        }

        return UpdateOccured;
    }

    private void DiamondOnTree(PlanetoidQuadTree PlayerTree, PlanetoidTreeManager NewTreeManger, int Size, int HighestResolution, int ResMulti)
    {
        //PlayerTree.IsPlayerTree = true;
        PlayerTree.Resolution = HighestResolution;
        PlayerTree.ToCreate = true;

        int Min = -Size;
        int Max = Size;

        Vector2 Center = PlayerTree.Center;

        float Radius = ((Mathf.Abs(Min) + Mathf.Abs(Max)) * SizeSetting.MinEdgeTiles) / 2;

        Circle Res1 = new Circle(Center, Radius / 1f);
        Circle Res2 = new Circle(Center, Radius / 2f);
        Circle Res3 = new Circle(Center, Radius / 12f);

        int TreeRes1 = HighestResolution * ResMulti * ResMulti * ResMulti * ResMulti;
        int TreeRes2 = HighestResolution * ResMulti * ResMulti * ResMulti;
        int TreeRes3 = HighestResolution * ResMulti;

        for (int y = Min; y <= Max; y++)
        {
            for (int x = Min; x <= Max; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2 UpdatedCenter = Center + new Vector2(PlayerTree.Size * x, PlayerTree.Size * y);
                PlanetoidQuadTree Tree = NewTreeManger.CreateSiblingRelativeCenterOffset(PlayerTree, UpdatedCenter);

                if (Tree != null)
                {
                    Tree.ToCreate = true;
                    if (Res3.ContainsInclusive(UpdatedCenter)) Tree.Resolution = HighestResolution;
                    else if (Res2.ContainsInclusive(UpdatedCenter)) Tree.Resolution = TreeRes3;
                    else if (Res1.ContainsInclusive(UpdatedCenter)) Tree.Resolution = TreeRes2;
                    else {
                        //Tree.Resolution = SizeSetting.MaxResolution;
                        Tree.Resolution = TreeRes1;
                       //Tree.ToCreate = false;
                    }
                }
            }
        }
    }

    private Vector3 SolutionCubeToSphere(PlanetoidFace Face, float X, float Y)
    {
        return OnSphereNormalized(OnCube(Face, X, Y, SizeSetting.MaxEdgeTiles));
    }

    private PlanetoidNode FindNearestNodeToPlayer()
    {
        Vector3 PointOnSphereNorm = (Operation.WorldSpacePlayerLocation() - Operation.WorldSpacePlanetLocation()).normalized;
        Vector3 PointOnSphere = PointOnSphereNorm * SizeSetting.Radius;
        return NodeManager.FindNearestNode(PointOnSphere);
    }

    public List<PlanetoidNode> FindNearestNodeToPlayerWithinIndexRange(int IndexRange)
    {
        Vector3 PointOnSphereNorm = (Operation.WorldSpacePlayerLocation() - Operation.WorldSpacePlanetLocation()).normalized;
        Vector3 PointOnSphere = PointOnSphereNorm * SizeSetting.Radius;

        PlanetoidNode NearestNode = FindNearestNodeToPlayer();

        return NodeManager.FindNearestNodesWithinBlockMeters(NearestNode, IndexRange);
    }

    public void Init()
    {
        CurrentLocalSpacePlayerLocationOnSphere = Vector3.positiveInfinity;

        ChunkDataStorage Storage = ChunkDataStorage.GetInstance();

        Storage.StorageDeleteGrassPlanet(SizeSetting.PlanetName);
        Storage.StorageDeleteChunkDataPlanet(SizeSetting.PlanetName);

        CreateImmediate = true;
        PlanetoidTreeManager NewTreeManger = new PlanetoidTreeManager(SizeSetting);
        NewTreeManger.SetResolution(SizeSetting.MaxResolution);
        NewTreeManger.AllLeafs.ForEach(v => v.ToCreate = true);

        NewTreeManger.CreateMe(TreeManager, DiffAction);
        TreeManager = NewTreeManger;
        CreateImmediate = false;

        if(NodeManager==null)
        {
            NodeManager = new PlanetoidNodeManager(SizeSetting, Operation);
            NodeManager.InitNodes();
        }
    }

    private void DiffAction(PlanetoidTreeManager NewManager, PlanetoidQuadTree Tree, PlanetoidTreeDiffType DiffType)
    {
        if (DiffType == PlanetoidTreeDiffType.DIFF_NEW)
        {
            if (Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to create PlanetoidQuadTree({0}) which already exists.", Tree.Id));

            if (Tree.ToCreate == false) return;

            if (CreateImmediate) Operation.CreateTree(Tree);
            else NewManager.AddCreateTree(Tree);
        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_EXISTS)
        {
            if (Tree.ToCreate == false) return;

            if (!Operation.IsTreeCreated(Tree))
            {
                if (CreateImmediate) Operation.CreateTree(Tree);
                else NewManager.AddCreateTree(Tree);
            }
            else
            {
                if (CreateImmediate) Operation.UpdateTree(Tree);
                else NewManager.AddUpdateTree(Tree);
            }
            //if (!Operation.IsTreeCreated(Tree))
            //throw new System.Exception(string.Format("Attempting to update PlanetoidQuadTree({0}) which does not exist.", Tree.Id));

        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_NOT_EXIST)
        {
            if (!Operation.IsTreeCreated(Tree)) return;
            //throw new System.Exception(string.Format("Attempting to delete PlanetoidQuadTree({0}) which does not exist.", Tree.Id));
            if (CreateImmediate) Operation.DeleteTree(Tree);
            else NewManager.AddDeleteTree(Tree);
        }
    }



    //https://forum.unity.com/threads/get-point-from-equally-mapped-quad-sphere-to-plane-coordinates.721631/
    //https://github.com/kurtdekker/makegeo/tree/master/makegeo/Assets

    public static Vector3 CubifyFromSphereNormalized(Vector3 OnSphere)
    {
        Vector3 s = OnSphere;

        float x2 = Mathf.Sqrt(Mathf.Abs(s.x));
        float y2 = Mathf.Sqrt(Mathf.Abs(s.y));
        float z2 = Mathf.Sqrt(Mathf.Abs(s.z));

        Vector3 v;
        v.x = s.x / (Mathf.Sqrt(Mathf.Abs(1f - (y2 / 2f) - (z2 / 2f) + (y2 * z2 / 3f))));
        v.y = s.y / (Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (z2 / 2f) + (x2 * z2 / 3f))));
        v.z = s.z / (Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (y2 / 2f) + (x2 * y2 / 3f))));

        return v;
    }

    public static Vector3 OnSphereNormalized(Vector3 OnCube)
    {
        Vector3 v = OnCube;
        float x2 = v.x * v.x;
        float y2 = v.y * v.y;
        float z2 = v.z * v.z;

        Vector3 s;
        s.x = v.x * Mathf.Sqrt(Mathf.Abs(1f - (y2 / 2f) - (z2 / 2f) + (y2 * z2 / 3f)));
        s.y = v.y * Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (z2 / 2f) + (x2 * z2 / 3f)));
        s.z = v.z * Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (y2 / 2f) + (x2 * y2 / 3f)));

        return s;
    }

    public static Vector3 OnCube(PlanetoidFace Face, float X, float Y, float Edges)
    {
        return OnCube(Face.DirVec, Face.DirVecAxisA, Face.DirVecAxisB, X, Y, Edges);
    }

    public static Vector3 OnCube(Vector3 FaceDir, Vector3 AxisA, Vector3 AxisB, float X, float Y, float Edges)
    {
        Vector2 Percent = new Vector2(X, Y) / (Edges);
        Vector3 OnCube = FaceDir + ((Percent.x - 0.5f) * 2 * AxisA) + ((Percent.y - 0.5f) * 2 * AxisB);
        return OnCube;
    }


    public static Vector2 SphereToLocalGrid(Vector3 WorldSpacePlayerLocation, Vector3 WorldSpacePlanetLocation)
    {
        Vector3 Direction = (WorldSpacePlayerLocation - WorldSpacePlanetLocation).normalized;
        return SphereToLocalGrid(PlanetoidFace.FindFace(WorldSpacePlayerLocation, WorldSpacePlanetLocation), Direction);
    }

    public static Vector2 SphereToLocalGrid(PlanetoidFace Face, Vector3 Direction)
    {
        Vector3 ToCube = (CubifyFromSphereNormalized(Direction) + Vector3.one) / 2f;
        Vector2 ToLocalCenterTransformed;

        switch (Face.Type)
        {
            case PlanetoidFaceType.UP_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.x, 1f - ToCube.z);
                break;
            case PlanetoidFaceType.DOWN_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.x, 1f - ToCube.z);
                break;
            case PlanetoidFaceType.RIGHT_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.z, 1f - ToCube.y);
                break;
            case PlanetoidFaceType.LEFT_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.z, 1f - ToCube.y);
                break;
            case PlanetoidFaceType.FORWARD_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.y, 1f - ToCube.x);
                break;
            case PlanetoidFaceType.BACK_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.y, 1f - ToCube.x);
                break;
            default:
                throw new System.Exception("Invalid PlanetoidFaceType provided. Value: " + Face.Type.ToString());
        }

        return ToLocalCenterTransformed;
    }
}

