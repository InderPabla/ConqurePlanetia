using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlanetoidQuadTreeOperation(PlanetoidQuadTree Tree, LocalTreeId localId);

public struct LocalTreeId
{
    public int X, Y;
    public static LocalTreeId Zero { get { return new LocalTreeId(0,0); } }
    public LocalTreeId(int x, int y) { X = x; Y = y; }
}

public class PlanetoidQuadTree
{
    public string Id;
    public int Depth;
    public Vector2 Start;
    public int Size;
    public PlanetoidFace Face;

    private LocalTreeId LocalId;
    private PlanetoidQuadTree Parent;
    private PlanetoidQuadTree[,] Children;

    public PlanetoidQuadTree(PlanetoidFace face, int size)
    {
        Face = face;
        Depth = 0;
        Start = Vector2.zero;
        Size = size;
        LocalId = LocalTreeId.Zero;
        Id = GenLocalIdZ();
    }

    public PlanetoidQuadTree(PlanetoidQuadTree parent, LocalTreeId localId, Vector2 start, int size)
    {
        Face = parent.Face;
        Parent = parent;
        Depth = parent.Depth + 1;
        Start = start;
        Size = size;
        LocalId = localId;
        Id = GenLocalIdZ();
    }

    public PlanetoidQuadTree(PlanetoidQuadTree copy)
    {
        Face = copy.Face;
        Depth = copy.Depth;
        Start = copy.Start;
        Size = copy.Size;
        LocalId = copy.LocalId;
        Id = copy.Id;

        if (copy.Parent != null) Parent = new PlanetoidQuadTree(copy.Parent);
        if (copy.Children !=null)
        {
            Children = new PlanetoidQuadTree[2, 2];
            ForEachChild((PlanetoidQuadTree Tree, LocalTreeId localId) => {
                Children[localId.Y, localId.X] = new PlanetoidQuadTree(copy.Children[localId.Y, localId.X]);
            });
        }
    }

    public void ToNode()
    {
        if (IsNode) return;
        Children = new PlanetoidQuadTree[2, 2];
        int size = Size / 2;
        ForEachChild((PlanetoidQuadTree Tree, LocalTreeId localId) => {
            Vector2 start = Start + new Vector2(localId.X * size, localId.Y* size);
            Children[localId.Y, localId.X] = new PlanetoidQuadTree(this, new LocalTreeId(localId.X, localId.Y), start, size);
        });
    }

    public void ToLeaf()
    {
        if (IsLeaf) return;
        Children = null;
    }

    public void ForEachChild(PlanetoidQuadTreeOperation Callback)
    {
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                Callback(Children[y, x], new LocalTreeId(x,y));
    }

    public bool IsRoot { get { return Parent == null && Depth == 0; } }
    public bool IsLeaf { get { return Children == null; } }
    public bool IsNode { get { return !IsLeaf; } }

    public List<PlanetoidQuadTree> Leafs
    {
        get {
            List<PlanetoidQuadTree> leafs = new List<PlanetoidQuadTree>();

            if (IsLeaf)
            {
                leafs.Add(this);
                return leafs;
            }

            ForEachChild((PlanetoidQuadTree Tree, LocalTreeId localId) => {
                leafs.AddRange(Tree.Leafs);
            });

            return leafs;
        }
    }

    private string GenLocalIdXY()
    {
        string localStr = string.Format("{0}{1}", LocalId.X, LocalId.Y);
        if (IsRoot) return string.Format("{0}-{1}", Face.Type, localStr);
        return string.Format("{0}-{1}", Parent.Id, localStr);
    }

    private string GenLocalIdZ()
    {
        int Z = (LocalId.Y + LocalId.X);
        string localStr = Z.ToString();
        if (IsRoot) return string.Format("{0}-{1}", Face.Type, localStr);
        return string.Format("{0}-{1}", Parent.Id, localStr);
    }
}
