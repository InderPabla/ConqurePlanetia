using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlanetoidQuadTreeOperation(PlanetoidQuadTree Tree, LocalTreeId localId);

public struct LocalTreeId
{
    public int X, Y;

    public static int UP_ID = 0;
    public static int DOWN_ID = 1;
    public static int LEFT_ID = 0;
    public static int RIGHT_ID = 1;

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

    private int _Resolution = 1;

    private bool _IsPlayerTree = false;

    private PlanetoidTreeManager Manager;

    public PlanetoidQuadTree(PlanetoidTreeManager manager, PlanetoidFace face, int size)
    {
        Face = face;
        Depth = 0;
        Start = Vector2.zero;
        Size = size;
        LocalId = LocalTreeId.Zero;
        Id = GenLocalIdXY();
        Resolution = 1;
        Manager = manager;

        ValidateResolution();
    }

    public PlanetoidQuadTree(PlanetoidQuadTree parent, LocalTreeId localId, Vector2 start, int size)
    {
        Face = parent.Face;
        Parent = parent;
        Depth = parent.Depth + 1;
        Start = start;
        Size = size;
        LocalId = localId;
        Id = GenLocalIdXY();
        Resolution = parent.Resolution;
        Manager = parent.Manager;

        ValidateResolution();
    }

    public PlanetoidQuadTree CreateLocalUp()
    {
        if (IsRoot) return null;
        if (LocalId.Y == LocalTreeId.DOWN_ID) return Parent.Children[LocalTreeId.UP_ID, LocalId.X];
        PlanetoidQuadTree ParentSib = Parent.CreateLocalUp();
        if (ParentSib != null)
        {
            ParentSib.ToNode();
            return ParentSib.Children[LocalTreeId.DOWN_ID, LocalId.X];
        }
        return null;
    }

    public PlanetoidQuadTree CreateLocalDown()
    {
        if (IsRoot) return null;
        if (LocalId.Y == LocalTreeId.UP_ID) return Parent.Children[LocalTreeId.DOWN_ID, LocalId.X];
        PlanetoidQuadTree ParentSib = Parent.CreateLocalDown();
        if (ParentSib != null)
        {
            ParentSib.ToNode();
            return ParentSib.Children[LocalTreeId.UP_ID, LocalId.X];
        }
        return null;
    }

    public PlanetoidQuadTree CreateLocalLeft()
    {
        if (IsRoot) return null;
        if (LocalId.X == LocalTreeId.RIGHT_ID) return Parent.Children[LocalId.Y, LocalTreeId.LEFT_ID];
        PlanetoidQuadTree ParentSib = Parent.CreateLocalLeft();
        if (ParentSib != null)
        {
            ParentSib.ToNode();
            return ParentSib.Children[LocalId.Y, LocalTreeId.RIGHT_ID];
        }
        return null;
    }

    public PlanetoidQuadTree CreateLocalRight()
    {
        if (IsRoot) return null;
        if (LocalId.X == LocalTreeId.LEFT_ID) return Parent.Children[LocalId.Y, LocalTreeId.RIGHT_ID];
        PlanetoidQuadTree ParentSib = Parent.CreateLocalRight();
        if (ParentSib != null)
        {
            ParentSib.ToNode();
            return ParentSib.Children[LocalId.Y, LocalTreeId.LEFT_ID];
        }
        return null;
    }

    public PlanetoidQuadTree CreateMaxLeafAtLocation(Vector2 Location, int MaxDepth)
    {
        if (InsideBounds(Location) == false)
        { 
            return null;
        }
            
        if(Depth==MaxDepth)
        {
            ToLeaf();
            return this;
        }

        ToNode();

        for(int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                PlanetoidQuadTree MaxLeaf = Children[y, x].CreateMaxLeafAtLocation(Location, MaxDepth);       
                if(MaxLeaf != null) return MaxLeaf; 
            }
        }

        throw new System.Exception("Inside Bound, but not found");
    }

    public PlanetoidQuadTree FindTreeAtPoint(Vector2 Point)
    {
        if (!InsideBounds(Point)) return null;
        if (IsLeaf) return this;

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                PlanetoidQuadTree InsideLeaf = Children[y, x].FindTreeAtPoint(Point);
                if (InsideLeaf != null) return InsideLeaf;
            }
        }

        return null;
    }


    public bool InsideBounds(Vector2 Location)
    {
        return Location.x >= Start.x && Location.x <= (Start.x+Size) && Location.y >= Start.y && Location.y <= (Start.y + Size);
        //return new Rect(Start,new Vector2(Size,Size)).Contains(Location);
    }

    public int Resolution {
        get
        {
            return _Resolution;
        }
        set
        {
            _Resolution = value;
            if (_Resolution > Size || _Resolution==Size) _Resolution = Size/2;
            ValidateResolution();
        }
    }

    public bool IsPlayerTree
    {
        get
        {
            return _IsPlayerTree;
        }
        set
        {
            _IsPlayerTree = value;
        }
    }

    public Vector2 Center
    {
        get
        {
            return Start + new Vector2(HalfSize, HalfSize);
        }
    }

    public Vector2 CenterPositionUp
    {
        get
        {
            return Center + new Vector2(0,-HalfSize);
        }
    }

    public Vector2 CenterPositionDown
    {
        get
        {
            return Center + new Vector2(0, HalfSize);
        }
    }

    public Vector2 CenterPositionRight
    {
        get
        {
            return Center + new Vector2(HalfSize, 0);
        }
    }

    public Vector2 CenterPositionLeft
    {
        get
        {
            return Center + new Vector2(-HalfSize, 0);
        }
    }

    public Vector2 SlightPositionUp
    {
        get
        {
            return Start + new Vector2(0.1f, -0.1f);
        }
    }

    public Vector2 SlightPositionDown
    {
        get
        {
            return Start + new Vector2(0.1f, Size+0.1f);
        }
    }

    public Vector2 SlightPositionRight
    {
        get
        {
            return Start + new Vector2(Size+0.1f, 0.1f);
        }
    }

    public Vector2 SlightPositionLeft
    {
        get
        {
            return Start + new Vector2(-0.1f, 0.1f);
        }
    }

    public int HalfSize
    {
        get
        {
            return Size/2;
        }
    }

    private void ValidateResolution()
    {
        if(Resolution>Size)
        {
            throw new System.Exception(string.Format("Invalid - Resolution({0}) > Size({1})",Resolution,Size));
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

    public PlanetoidTreeManager TreeManager
    {
        get
        {
            return Manager;
        }
    }

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

    public List<PlanetoidQuadTree> Trees
    {
        get
        {
            List<PlanetoidQuadTree> trees = new List<PlanetoidQuadTree>();
            trees.Add(this);

            if (IsLeaf)
            {
                return trees;
            }

            ForEachChild((PlanetoidQuadTree Tree, LocalTreeId localId) => {
                trees.AddRange(Tree.Trees);
            });

            return trees;
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

    public bool Equals(PlanetoidQuadTree Tree)
    {
        return Id.Equals(Tree.Id);
    }

    public static bool EqualsWithResolution(PlanetoidQuadTree Tree1, PlanetoidQuadTree Tree2)
    {
        if (Tree1 != null && Tree2 != null) return Tree1.Equals(Tree2) && Tree1.Resolution==Tree2.Resolution;
        if (Tree1 == null && Tree2 == null) return true;
        return false;
    }


    public override string ToString()
    {
        return string.Format("Id:{0}, Size:{1}, Start:{2}, Depth:{3}, Resolution:{4}",Id,Size,Start,Depth,Resolution);
    }
}
