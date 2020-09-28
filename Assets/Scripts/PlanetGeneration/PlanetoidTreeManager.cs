using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PlanetoidTreeDiffType
{
     DIFF_NEW, DIFF_EXISTS, DIFF_NOT_EXIST
}

public struct FaceToFaceMap
{
    public PlanetoidFace Face;
    public Vector2 Point;

    public FaceToFaceMap(PlanetoidFace face, Vector2 point)
    {
        Face = face;
        Point = point;
    }
    public bool Equals(FaceToFaceMap Map)
    {
        return this.Face.Type == Map.Face.Type && this.Point.Equals(Map.Point);
    }
}

public delegate void PlanetoidTreeDiff(PlanetoidTreeManager NewManager, PlanetoidQuadTree Tree, PlanetoidTreeDiffType DiffType);
public delegate bool PlanetoidTreeForEachUpdate(PlanetoidQuadTree Tree, int Index);

public class PlanetoidTreeManager
{
    private PlanetoidQuadTree[] Roots;
    public PlanetoidSizeSetting SizeSetting;

    private List<PlanetoidQuadTree> CreateList;
    private List<PlanetoidQuadTree> UpdateList;
    private List<PlanetoidQuadTree> DeleteList;

    public PlanetoidTreeManager(PlanetoidSizeSetting sizeSetting)
    {
        SizeSetting = sizeSetting;

        Roots = new PlanetoidQuadTree[PlanetoidFace.FACES.Length];

        for (int i = 0; i < Roots.Length; i++)
        {
            Roots[i] = new PlanetoidQuadTree(this, PlanetoidFace.FACES[i], SizeSetting.MaxEdgeTiles);
        }

        CreateList = new List<PlanetoidQuadTree>();
        UpdateList = new List<PlanetoidQuadTree>();
        DeleteList = new List<PlanetoidQuadTree>();
    }

    public PlanetoidQuadTree CreateMaxLeafAtFaceAndLocation(PlanetoidFace Face, Vector2 LocationOnFace)
    {
        PlanetoidQuadTree Tree = GetRootTreeByFace(Face);
        
        PlanetoidQuadTree Leaf = Tree.CreateMaxLeafAtLocation(LocationOnFace,SizeSetting.MaxTreeDepth);

        if(Leaf==null)
        {
            //Debug.Log("Unable to find leaf on face and location:"+Face.Type+" "+LocationOnFace+". Tree start  and size:"+ Tree.Start+" "+ Tree.Size);
        }

        return Leaf;
    }

    private void ValidateTreeDepth(PlanetoidQuadTree Tree)
    {
        int MaxDepth = SizeSetting.MaxTreeDepth;
        if (Tree.Depth != SizeSetting.MaxTreeDepth)
            throw new System.Exception(string.Format("Except tree to create sibling of must be at max depth. Current Depth:{0}, Max Depth:{1}", Tree.Depth, MaxDepth));
    }

    public PlanetoidQuadTree CreateSiblingRelativeCenterOffset(PlanetoidQuadTree Tree, Vector2 CenterOffset)
    {
        ValidateTreeDepth(Tree);

        FaceToFaceMap InitMap = new FaceToFaceMap(Tree.Face, CenterOffset);
        FaceToFaceMap Mapped = FaceToFaceMapper(InitMap,SizeSetting.MaxEdgeTiles);

        PlanetoidQuadTree MappedTree =  CreateMaxLeafAtFaceAndLocation(Mapped.Face, Mapped.Point);
        if(MappedTree==null)
        {
           //Debug.Log(Tree.Face.Type+" TO "+Mapped.Face.Type+" Center:"+CenterOffset+" >>> "+Mapped.Point+" "+SizeSetting.MaxEdgeTiles);
        }

        return MappedTree;
    }

    public PlanetoidQuadTree FindNeighbourUp(PlanetoidQuadTree Tree)
    {
        FaceToFaceMap InitMap = new FaceToFaceMap(Tree.Face, Tree.SlightPositionUp);
        return FindNeighbourWithFaceToFace(InitMap);
    }

    public PlanetoidQuadTree FindNeighbourDown(PlanetoidQuadTree Tree)
    {
        FaceToFaceMap InitMap = new FaceToFaceMap(Tree.Face, Tree.SlightPositionDown);
        return FindNeighbourWithFaceToFace(InitMap);
    }

    public PlanetoidQuadTree FindNeighbourLeft(PlanetoidQuadTree Tree)
    {
        FaceToFaceMap InitMap = new FaceToFaceMap(Tree.Face, Tree.SlightPositionLeft);
        return FindNeighbourWithFaceToFace(InitMap);
    }
    public PlanetoidQuadTree FindNeighbourRight(PlanetoidQuadTree Tree)
    {
        FaceToFaceMap InitMap = new FaceToFaceMap(Tree.Face, Tree.SlightPositionRight);
        return FindNeighbourWithFaceToFace(InitMap);
    }

    public PlanetoidQuadTree FindNeighbourWithFaceToFace(FaceToFaceMap Map)
    {
        PlanetoidQuadTree Root = GetRootTreeByFace(Map.Face);
        return Root.FindTreeAtPoint(Map.Point);
    }

    public PlanetoidQuadTree CreateSiblingUp(PlanetoidQuadTree Tree)
    {
        ValidateTreeDepth(Tree);
        PlanetoidQuadTree Sib = Tree.CreateLocalUp();
        return Sib == null ? CreateSiblingRelativeCenterOffset(Tree, Tree.CenterPositionUp) : Sib;
    }

    public PlanetoidQuadTree CreateSiblingDown(PlanetoidQuadTree Tree)
    {
        ValidateTreeDepth(Tree);
        PlanetoidQuadTree Sib = Tree.CreateLocalDown();
        return Sib==null?CreateSiblingRelativeCenterOffset(Tree, Tree.CenterPositionDown) :Sib;
    }
    public PlanetoidQuadTree CreateSiblingLeft(PlanetoidQuadTree Tree)
    {
        ValidateTreeDepth(Tree);
        PlanetoidQuadTree Sib = Tree.CreateLocalLeft();
        return Sib == null ? CreateSiblingRelativeCenterOffset(Tree, Tree.CenterPositionLeft) : Sib;
    }

    public PlanetoidQuadTree CreateSiblingRight(PlanetoidQuadTree Tree)
    {
        ValidateTreeDepth(Tree);
        PlanetoidQuadTree Sib = Tree.CreateLocalRight();
        return Sib == null ? CreateSiblingRelativeCenterOffset(Tree, Tree.CenterPositionRight) : Sib; ;
    }

    /**
     *  this-> in this context is the Tree Manager which will ne created. 
     *  The other will be destryed
     */
    public void CreateMe(PlanetoidTreeManager RemoveMe, PlanetoidTreeDiff Diff)
    {
        if (RemoveMe == null)
        {
            foreach (PlanetoidQuadTree Root in Roots)
            {
                List<PlanetoidQuadTree> Leafs = Root.Leafs;
                Leafs.ForEach((PlanetoidQuadTree Leaf) => {
                    Diff(this, Leaf, PlanetoidTreeDiffType.DIFF_NEW);
                });
            }

            return;
        }

        //DiffByEachRoot(Other, Diff);
        DiffByAllLeafs(RemoveMe, Diff);
    }

    private static void DiffByLeafDictionary(PlanetoidTreeManager NewManager, Dictionary<string, PlanetoidQuadTree> ThisLeafsDict ,Dictionary<string, PlanetoidQuadTree> OtherLeafsDict,PlanetoidTreeDiff Diff)
    {
        IEnumerator<string> ThisKeysEnum = ThisLeafsDict.Keys.GetEnumerator();
        while (ThisKeysEnum.MoveNext())
        {
            string ThisKey = ThisKeysEnum.Current;

            if (OtherLeafsDict.ContainsKey(ThisKey))
            {
                Diff(NewManager, ThisLeafsDict[ThisKey], PlanetoidTreeDiffType.DIFF_EXISTS);
                OtherLeafsDict.Remove(ThisKey);
            }
            else
            {
                Diff(NewManager, ThisLeafsDict[ThisKey], PlanetoidTreeDiffType.DIFF_NEW);
            }
        }

        IEnumerator<string> OtherKeysEnum = OtherLeafsDict.Keys.GetEnumerator();
        while (OtherKeysEnum.MoveNext())
            Diff(NewManager, OtherLeafsDict[OtherKeysEnum.Current], PlanetoidTreeDiffType.DIFF_NOT_EXIST);
    }

    private void DiffByAllLeafs(PlanetoidTreeManager Other, PlanetoidTreeDiff Diff)
    {

        List<PlanetoidQuadTree> ThisLeafs = AllLeafs;
        List<PlanetoidQuadTree> OtherLeafs = Other.AllLeafs;

        Dictionary<string, PlanetoidQuadTree> ThisLeafsDict = TreeListToDict(ThisLeafs);
        Dictionary<string, PlanetoidQuadTree> OtherLeafsDict = TreeListToDict(OtherLeafs);

        DiffByLeafDictionary(this, ThisLeafsDict, OtherLeafsDict, Diff);
    }

    private void DiffByEachRoot(PlanetoidTreeManager Other, PlanetoidTreeDiff Diff)
    {
        for (int i = 0; i < Roots.Length; i++)
        {
            List<PlanetoidQuadTree> ThisLeafs = Roots[i].Leafs;
            List<PlanetoidQuadTree> OtherLeafs = Other.Roots[i].Leafs;

            Dictionary<string, PlanetoidQuadTree> ThisLeafsDict = TreeListToDict(ThisLeafs);
            Dictionary<string, PlanetoidQuadTree> OtherLeafsDict = TreeListToDict(OtherLeafs);

            DiffByLeafDictionary(this, ThisLeafsDict, OtherLeafsDict, Diff);
        }
    }

    public static Dictionary<string, PlanetoidQuadTree> TreeListToDict(List<PlanetoidQuadTree> Leafs)
    {
        Dictionary<string, PlanetoidQuadTree> Dict = new Dictionary<string, PlanetoidQuadTree>();
        Leafs.ForEach((PlanetoidQuadTree Tree) => {
            if(Dict.ContainsKey(Tree.Id))
            {
                throw new System.Exception(string.Format("Duplicate entry of PlanetoidQuadTree({0}) found in leaf list. They must all be unique.",Tree.Id));
            }

            Dict[Tree.Id] = Tree;
        });
        return Dict;
    }


    public List<PlanetoidQuadTree> AllLeafs
    {
        get
        {
            List<PlanetoidQuadTree> leafs = new List<PlanetoidQuadTree>();
            for (int i = 0; i < Roots.Length; i++)
            {
                leafs.AddRange(Roots[i].Leafs);
            }
            return leafs;
        }
    }

    public List<PlanetoidQuadTree> AllTrees
    {
        get
        {
            List<PlanetoidQuadTree> trees = new List<PlanetoidQuadTree>();
            for (int i = 0; i < Roots.Length; i++)
            {
                trees.AddRange(Roots[i].Trees);
            }
            return trees;
        }
    }

    private void RenderVeryFar()
    {
        for (int i = 0; i < Roots.Length; i++)
        {
            Roots[i].ToLeaf();
        }
    }

    public void SetMaxResolution()
    {
        AllTrees.ForEach((PlanetoidQuadTree Tree) => {
            Tree.Resolution = SizeSetting.MaxResolution;
        });
    }

    public void SetResolutionToBeSize()
    {
        AllTrees.ForEach((PlanetoidQuadTree Tree) => {
            Tree.Resolution = Tree.Size;
        });
    }

    public void SetResolution(int Resolution)
    {
        AllTrees.ForEach((PlanetoidQuadTree Tree) => {
            Tree.Resolution = Resolution;
        });
    }

    public PlanetoidQuadTree GetRootTreeByFace(PlanetoidFace Face)
    {
        foreach (PlanetoidQuadTree Tree in Roots) if (Face.Type == Tree.Face.Type) return Tree;
        return null;
    }

    public static FaceToFaceMap FaceToFaceMapper(FaceToFaceMap Map, float MaxEdge)
    {
        float Edge = MaxEdge;

        float _X = Map.Point.x;
        float _Y = Map.Point.y;

        Vector2 YX = new Vector2(Mathf.Abs(_Y), Mathf.Abs(_X));
        Vector2 EmY_XmE = new Vector2(Edge - _Y, _X - Edge);
        Vector2 YpE_EmX = new Vector2(_Y + Edge, Edge - _X);
        Vector2 YmE_EmX = new Vector2(_Y - Edge, Edge - _X);

        Vector2 EmY_XpE = new Vector2(Edge - _Y, _X + Edge);
        Vector2 Y_EEmX = new Vector2(_Y, (2 * Edge) - _X);
        Vector2 EEmY_X = new Vector2((2 * Edge) - _Y, _X);



        if (Map.Point.x >= 0
         && Map.Point.x <= Edge
         && Map.Point.y >= 0
         && Map.Point.y <= Edge)
        {
            return Map;
        }

        if (Map.Face.Type==PlanetoidFaceType.UP_FACE)
        {
            Vector2 XPoint = Vector2.zero;
            Vector2 YPoint = Vector2.zero;

            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.LEFT, YX);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.RIGHT, EmY_XmE);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.FORWARD, YpE_EmX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.BACKWARD, YmE_EmX);
        }

        if (Map.Face.Type== PlanetoidFaceType.DOWN_FACE)
        {
            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.RIGHT, EmY_XpE);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.LEFT, Y_EEmX);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.FORWARD, YX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.BACKWARD, EEmY_X);
        }

        if (Map.Face.Type== PlanetoidFaceType.LEFT_FACE)
        {
            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.FORWARD, EmY_XpE);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.BACKWARD, Y_EEmX);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.UP, YX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.DOWN, EEmY_X);
        }

        if (Map.Face.Type== PlanetoidFaceType.RIGHT_FACE)
        {
            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.BACKWARD, YX);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.FORWARD, EmY_XmE);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.UP, YpE_EmX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.DOWN, YmE_EmX);
        }

        if (Map.Face.Type== PlanetoidFaceType.FORWARD_FACE)
        {
            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.DOWN, YX);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.UP, EmY_XmE);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.RIGHT, YpE_EmX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.LEFT, YmE_EmX);
        }

        if (Map.Face.Type== PlanetoidFaceType.BACK_FACE)
        {
            if (Map.Point.x < 0)
                return new FaceToFaceMap(PlanetoidFace.UP, EmY_XpE);
            if (Map.Point.x > Edge)
                return new FaceToFaceMap(PlanetoidFace.DOWN, Y_EEmX);
            if (Map.Point.y < 0)
                return new FaceToFaceMap(PlanetoidFace.RIGHT, YX);
            if (Map.Point.y > Edge)
                return new FaceToFaceMap(PlanetoidFace.LEFT, EEmY_X);
        }

        throw new System.Exception("No Mapping Found For Face: " + Map.Face.Type + ", Vector: " + Map.Point);
    }

    public void AddCreateTree(PlanetoidQuadTree Tree)
    {
        CreateList.Add(Tree);
    }

    public void AddDeleteTree(PlanetoidQuadTree Tree)
    {
        DeleteList.Add(Tree);
    }

    public void AddUpdateTree(PlanetoidQuadTree Tree)
    {
        UpdateList.Add(Tree);
    }

    public bool IsAllQueueEmpty()
    {
        return CreateList.Count == 0 && UpdateList.Count == 0 && DeleteList.Count == 0;
    }

    public bool IsCreateAndUpdateQueueEmpty()
    {
        return CreateList.Count == 0 && UpdateList.Count == 0;
    }

    public void ForEachCreate(PlanetoidTreeForEachUpdate Callback)
    {
        for(int i = CreateList.Count-1; i>=0; i--)
        {
            if(Callback(CreateList[i],i))
            {
                CreateList.RemoveAt(i);
            }
        }
        
    }

    public void ForEachUpdate(PlanetoidTreeForEachUpdate Callback)
    {
        for (int i = UpdateList.Count - 1; i >= 0; i--)
        {
            if (Callback(UpdateList[i], i))
            {
                UpdateList.RemoveAt(i);
            }
        }
    }

    public void ForEachDelete(PlanetoidTreeForEachUpdate Callback)
    {
        for (int i = DeleteList.Count - 1; i >= 0; i--)
        {
            if (Callback(DeleteList[i], i))
            {
                DeleteList.RemoveAt(i);
            }
        }
    }
}
