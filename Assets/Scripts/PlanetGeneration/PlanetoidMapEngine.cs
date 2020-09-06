using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlanetoidOperationUpdateTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationDeleteTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationCreateTree(PlanetoidQuadTree Tree);
public delegate bool PlanetoidOperationTreeIsCreated(PlanetoidQuadTree Tree);

public delegate Vector3 PlanetoidOperationWorldSpacePlayerLocation();
public delegate Vector3 PlanetoidOperationWorldSpacePlanetLocation();
public delegate PlanetoidQuadTree PlanetoidOperationTreeUnderPlayer();

public delegate Vector3 SolutionCubeToSphere(PlanetoidFace Face, float X, float Y);

public class PlanetoidMapEngineOperation
{
    public PlanetoidOperationUpdateTree UpdateTree;
    public PlanetoidOperationCreateTree CreateTree;
    public PlanetoidOperationDeleteTree DeleteTree;

    public PlanetoidOperationWorldSpacePlayerLocation WorldSpacePlayerLocation;
    public PlanetoidOperationWorldSpacePlanetLocation WorldSpacePlanetLocation;

    public PlanetoidOperationTreeIsCreated IsTreeCreated;

    public PlanetoidOperationTreeUnderPlayer TreeUnderPlayer;

    public PlanetoidMapEngineOperation(PlanetoidOperationUpdateTree updateTree,PlanetoidOperationCreateTree createTree,PlanetoidOperationDeleteTree deleteTree
        ,PlanetoidOperationWorldSpacePlayerLocation worldSpacePlayerLocation,PlanetoidOperationWorldSpacePlanetLocation worldSpacePlanetLocation
        ,PlanetoidOperationTreeIsCreated isTreeCreated, PlanetoidOperationTreeUnderPlayer treeUnderPlayer
    )
    {
        UpdateTree = updateTree;
        DeleteTree = deleteTree;
        CreateTree = createTree;

        WorldSpacePlayerLocation = worldSpacePlayerLocation;
        WorldSpacePlanetLocation = worldSpacePlanetLocation;

        IsTreeCreated = isTreeCreated;

        TreeUnderPlayer = treeUnderPlayer;
    }
}

public class PlanetoidMapEngineSolution
{
    public SolutionCubeToSphere CubeToSphere;

    public PlanetoidMapEngineSolution (SolutionCubeToSphere cubeToSphere) {
        CubeToSphere = cubeToSphere;
    }
    
}

public class PlanetoidMapEngine 
{
    private PlanetoidSizeSetting SizeSetting;

    private PlanetoidRenderType RenderType;

    private PlanetoidMapEngineOperation Operation;

    private PlanetoidTreeManager TreeManager;

    public PlanetoidMapEngineSolution Solution;

    private PlanetoidQuadTree CurrentPlayerTree;


    public PlanetoidMapEngine(PlanetoidSizeSetting sizeSetting, PlanetoidMapEngineOperation operation)
    {
        SizeSetting = sizeSetting;
        Operation = operation;
        RenderType = PlanetoidRenderType.PLAYER_VERY_FAR;
        Solution = new PlanetoidMapEngineSolution(this.SolutionCubeToSphere);
    }

    public void Update()
    {
        if(TreeManager==null || TreeManager.IsAllQueueEmpty())
        {
            UpdateTreeManager();
        }

        UpdateTreeGeneration();
    }

    private void UpdateTreeGeneration()
    {
        int CreateEdgeCount = 128;
        int UpdateEdgeCount = 128;

        TreeManager.ForEachCreate((PlanetoidQuadTree Tree, int Index) => {
            int Count = Tree.Size / Tree.Resolution;
            if (Count<=CreateEdgeCount)
            {
                CreateEdgeCount -= Count;
                Operation.CreateTree(Tree);
                return true;
            }
            return false;
        });

        TreeManager.ForEachUpdate((PlanetoidQuadTree Tree, int Index) => {
            int Count = Tree.Size / Tree.Resolution;
            if (Count <= UpdateEdgeCount)
            {
                UpdateEdgeCount -= Count;
                Operation.UpdateTree(Tree);
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

    private void UpdateTreeManager()
    {
        int MaxTreeDepth = SizeSetting.MaxTreeDepth;
        Vector3 WorldSpacePlayerLocation = Operation.WorldSpacePlayerLocation();
        Vector3 WorldSpacePlanetLocation = Operation.WorldSpacePlanetLocation();
        Vector3 Direction = (WorldSpacePlayerLocation - WorldSpacePlanetLocation).normalized;

        float DistanceToSurface = Vector3.Distance(WorldSpacePlayerLocation, WorldSpacePlanetLocation) - SizeSetting.Radius;

        if (DistanceToSurface <= 0f) return;

        PlanetoidRenderType NewRenderType = SizeSetting.RenderTypeForDistanceToSurface(DistanceToSurface);
        PlanetoidFace FaceUnderPlayer = PlanetoidFace.FindFace(WorldSpacePlayerLocation, WorldSpacePlanetLocation);

        PlanetoidRenderType OldRenderType = RenderType;
        RenderType = NewRenderType;

        Vector2 FaceSpacePlayerLocation = SphereToLocalGrid(FaceUnderPlayer, Direction) * SizeSetting.MaxEdgeTiles;

        PlanetoidQuadTree PlayerTreeFromRaycast = Operation.TreeUnderPlayer();

        if (RenderType <= PlanetoidRenderType.PLAYER_ON_PLANET)
        {
            PlanetoidTreeManager NewTreeManger = new PlanetoidTreeManager(SizeSetting);
            NewTreeManger.SetMaxResolution();

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



            DiamondOnTree(PlayerTree, NewTreeManger, 12, SizeSetting.MinResolution, 2);

            if (CurrentPlayerTree != null)
            {
                if (PlayerTree.Equals(CurrentPlayerTree)) return;

                if (PlayerTree.Face == CurrentPlayerTree.Face)
                {
                    if (Vector2.Distance(PlayerTree.Start, CurrentPlayerTree.Start) <= SizeSetting.MinEdgeTiles * 0.5f)
                    {
                        return;
                    }
                }
            }

            Debug.Log((CurrentPlayerTree != null ? CurrentPlayerTree.Id : null) + " " + PlayerTree.Id + " are different. New Tree()");

            CurrentPlayerTree = PlayerTree;

            PlanetoidTreeManager OldTreeManager = TreeManager;
            TreeManager = NewTreeManger;
            TreeManager.CreateMe(OldTreeManager, DiffAction);

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
            Debug.Log(OldRenderType + " " + NewRenderType + " are different. Init()");
            CurrentPlayerTree = null;
            Init();
        }
    }

    private void DiamondOnTree(PlanetoidQuadTree PlayerTree, PlanetoidTreeManager NewTreeManger, int Size, int HighestResolution, int ResMulti)
    {

        PlayerTree.IsPlayerTree = true;
        PlayerTree.Resolution = HighestResolution;

        int Min = -Size;
        int Max = Size;

        Vector2 Center = PlayerTree.Center;

        float Radius = ((Mathf.Abs(Min) + Mathf.Abs(Max)) * SizeSetting.MinEdgeTiles) / 2;

        Circle Res1 = new Circle(Center, Radius / 1f);
        Circle Res2 = new Circle(Center, Radius / 2f);
        Circle Res3 = new Circle(Center, Radius / 3f);
       

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
                    if (Res3.ContainsInclusive(UpdatedCenter)) Tree.Resolution = HighestResolution;
                    else if (Res2.ContainsInclusive(UpdatedCenter)) Tree.Resolution = HighestResolution * ResMulti;
                    else if (Res1.ContainsInclusive(UpdatedCenter)) Tree.Resolution = HighestResolution * ResMulti * ResMulti;

                    else Tree.Resolution = SizeSetting.MaxResolution;
                }
            }
        }
    }

    private Vector3 SolutionCubeToSphere(PlanetoidFace Face, float X, float Y)
    {
        return OnSphereNormalized(OnCube(Face, X, Y, SizeSetting.MaxEdgeTiles));
    }

    public void Init()
    {
        PlanetoidTreeManager NewTreeManger = new PlanetoidTreeManager(SizeSetting);
        NewTreeManger.SetResolution(SizeSetting.MaxResolution/2);
        NewTreeManger.CreateMe(TreeManager, DiffAction);
        TreeManager = NewTreeManger;
    }



    private void DiffAction(PlanetoidTreeManager NewManager, PlanetoidQuadTree Tree, PlanetoidTreeDiffType DiffType)
    {
        if(DiffType == PlanetoidTreeDiffType.DIFF_NEW)
        {
            if (Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to create PlanetoidQuadTree({0}) which already exists.", Tree.Id));
            NewManager.AddCreateTree(Tree);
            //Operation.CreateTree(Tree);
        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_EXISTS)
        {
            if (!Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to update PlanetoidQuadTree({0}) which does not exist.", Tree.Id));
            NewManager.AddUpdateTree(Tree);
            //Operation.UpdateTree(Tree);
        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_NOT_EXIST)
        {
            if (!Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to delete PlanetoidQuadTree({0}) which does not exist.", Tree.Id));
            NewManager.AddDeleteTree(Tree);
            //Operation.DeleteTree(Tree);
        }
    }

    /*public static Vector3 CubifyFromSphereNormalized(Vector3 OnSphere)
    {
        Vector3 position = OnSphere;
        float x, y, z;
        x = position.x;
        y = position.y;
        z = position.z;

        float fx, fy, fz;
        fx = Mathf.Abs(x);
        fy = Mathf.Abs(y);
        fz = Mathf.Abs(z);

        const float inverseSqrt2 = 0.70710676908493042f;

        if (fy >= fx && fy >= fz)
        {
            float a2 = x * x * 2.0f;
            float b2 = z * z * 2.0f;
            float inner = -a2 + b2 - 3;
            float innersqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (x == 0.0 || x == -0.0)
            {
                position.x = 0.0f;
            }
            else
            {
                position.x = Mathf.Sqrt(innersqrt + a2 - b2 + 3.0f) * inverseSqrt2;
            }

            if (z == 0.0 || z == -0.0)
            {
                position.z = 0.0f;
            }
            else
            {
                position.z = Mathf.Sqrt(innersqrt - a2 + b2 + 3.0f) * inverseSqrt2;
            }

            if (position.x > 1.0) position.x = 1.0f;
            if (position.z > 1.0) position.z = 1.0f;

            if (x < 0) position.x = -position.x;
            if (z < 0) position.z = -position.z;

            if (y > 0)
            {
                // top face
                position.y = 1.0f;
            }
            else
            {
                // bottom face
                position.y = -1.0f;
            }
        }
        else if (fx >= fy && fx >= fz)
        {
            float a2 = y * y * 2.0f;
            float b2 = z * z * 2.0f;
            float inner = -a2 + b2 - 3f;
            float innersqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (y == 0.0f || y == -0.0f)
            {
                position.y = 0.0f;
            }
            else
            {
                position.y = Mathf.Sqrt(innersqrt + a2 - b2 + 3.0f) * inverseSqrt2;
            }

            if (z == 0.0f || z == -0.0f)
            {
                position.z = 0.0f;
            }
            else
            {
                position.z = Mathf.Sqrt(innersqrt - a2 + b2 + 3.0f) * inverseSqrt2;
            }

            if (position.y > 1.0f) position.y = 1.0f;
            if (position.z > 1.0f) position.z = 1.0f;

            if (y < 0) position.y = -position.y;
            if (z < 0) position.z = -position.z;

            if (x > 0)
            {
                // right face
                position.x = 1.0f;
            }
            else
            {
                // left face
                position.x = -1.0f;
            }
        }
        else
        {
            float a2 = x * x * 2.0f;
            float b2 = y * y * 2.0f;
            float inner = -a2 + b2 - 3;
            float innersqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (x == 0.0 || x == -0.0)
            {
                position.x = 0.0f;
            }
            else
            {
                position.x = Mathf.Sqrt(innersqrt + a2 - b2 + 3.0f) * inverseSqrt2;
            }

            if (y == 0.0f || y == -0.0f)
            {
                position.y = 0.0f;
            }
            else
            {
                position.y = Mathf.Sqrt(innersqrt - a2 + b2 + 3.0f) * inverseSqrt2;
            }

            if (position.x > 1.0) position.x = 1.0f;
            if (position.y > 1.0) position.y = 1.0f;

            if (x < 0) position.x = -position.x;
            if (y < 0) position.y = -position.y;

            if (z > 0)
            {
                // front face
                position.z = 1.0f;
            }
            else
            {
                // back face
                position.z = -1.0f;
            }
        }

        return position;
    }*/


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

