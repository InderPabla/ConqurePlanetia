using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlanetoidOperationUpdateTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationDeleteTree(PlanetoidQuadTree Tree);
public delegate void PlanetoidOperationCreateTree(PlanetoidQuadTree Tree);
public delegate bool PlanetoidOperationTreeIsCreated(PlanetoidQuadTree Tree);
public delegate Vector3 PlanetoidOperationWorldSpacePlayerLocation();
public delegate Vector3 PlanetoidOperationWorldSpacePlanetLocation();

public class PlanetoidMapEngineOperation
{
    public PlanetoidOperationUpdateTree UpdateTree;
    public PlanetoidOperationCreateTree CreateTree;
    public PlanetoidOperationDeleteTree DeleteTree;

    public PlanetoidOperationWorldSpacePlayerLocation WorldSpacePlayerLocation;
    public PlanetoidOperationWorldSpacePlanetLocation WorldSpacePlanetLocation;

    public PlanetoidOperationTreeIsCreated IsTreeCreated;

    public PlanetoidMapEngineOperation(PlanetoidOperationUpdateTree updateTree,PlanetoidOperationCreateTree createTree,PlanetoidOperationDeleteTree deleteTree
        ,PlanetoidOperationWorldSpacePlayerLocation worldSpacePlayerLocation,PlanetoidOperationWorldSpacePlanetLocation worldSpacePlanetLocation
        ,PlanetoidOperationTreeIsCreated isTreeCreated
    )
    {
        UpdateTree = updateTree;
        DeleteTree = deleteTree;
        CreateTree = createTree;

        WorldSpacePlayerLocation = worldSpacePlayerLocation;
        WorldSpacePlanetLocation = worldSpacePlanetLocation;

        IsTreeCreated = isTreeCreated;
    }
}

public class PlanetoidMapEngine 
{
    private PlanetoidSizeSetting SizeSetting;

    private PlanetoidRenderType RenderType;

    //private Dictionary<string, PlanetoidQuadTree> ActiveTreeDict;

    private PlanetoidMapEngineOperation Operation;

    private PlanetoidTreeManager TreeManager;

    public PlanetoidMapEngine(PlanetoidSizeSetting sizeSetting, PlanetoidMapEngineOperation operation)
    {
        SizeSetting = sizeSetting;
        Operation = operation;
        RenderType = PlanetoidRenderType.PLAYER_VERY_FAR;
        //ActiveTreeDict = new Dictionary<string, PlanetoidQuadTree>();
    }

    public bool Update()
    {
        Vector3 WorldSpacePlayerLocation = Operation.WorldSpacePlayerLocation();
        Vector3 WorldSpacePlanetLocation = Operation.WorldSpacePlanetLocation();
        Vector3 Direction = (WorldSpacePlayerLocation - WorldSpacePlanetLocation).normalized;

        float DistanceToSurface = Vector3.Distance(WorldSpacePlayerLocation, WorldSpacePlanetLocation) - SizeSetting.Radius;
        if (DistanceToSurface <= 0f) return false;

        PlanetoidRenderType NewRenderType = SizeSetting.RenderTypeForDistanceToSurface(DistanceToSurface);
        if (NewRenderType == RenderType) return false;

        RenderType = NewRenderType;

        PlanetoidFace FaceUnderPlayer = PlanetoidFace.FindFace(WorldSpacePlayerLocation, WorldSpacePlanetLocation);
        Vector2 FaceSpacePlayerLocation = PlanetoidFace.SphereToLocalGrid(FaceUnderPlayer, Direction);


        return true;
    }

   

    public void Init()
    {
        if (TreeManager != null)
            throw new System.Exception(string.Format("Calling Init on PlanetoidMapEngine has already been initized"));

        TreeManager = new PlanetoidTreeManager(SizeSetting);
        TreeManager.Diff(null, DiffAction);
    }

    private void DiffAction(PlanetoidQuadTree Tree, PlanetoidTreeDiffType DiffType)
    {
        if(DiffType == PlanetoidTreeDiffType.DIFF_NEW)
        {
            if (Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to create PlanetoidQuadTree({0}) which already exists.", Tree.Id));
            Operation.CreateTree(Tree);
        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_EXISTS)
        {
            if (!Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to update PlanetoidQuadTree({0}) which does not exist.", Tree.Id));
            Operation.UpdateTree(Tree);
        }
        else if (DiffType == PlanetoidTreeDiffType.DIFF_NOT_EXIST)
        {
            if (!Operation.IsTreeCreated(Tree))
                throw new System.Exception(string.Format("Attempting to delete PlanetoidQuadTree({0}) which does not exist.", Tree.Id));
            Operation.DeleteTree(Tree);
        }
    }
  
}

