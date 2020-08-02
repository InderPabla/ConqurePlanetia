using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PlanetoidTreeDiffType
{
     DIFF_NEW, DIFF_EXISTS, DIFF_NOT_EXIST
}

public delegate void PlanetoidTreeDiff(PlanetoidQuadTree Tree, PlanetoidTreeDiffType DiffType);

public class PlanetoidTreeManager
{
    private PlanetoidQuadTree[] Roots;
    private PlanetoidSizeSetting SizeSetting;

    public PlanetoidTreeManager(PlanetoidSizeSetting sizeSetting)
    {
        SizeSetting = sizeSetting;

        Roots = new PlanetoidQuadTree[PlanetoidFace.FACES.Length];

        for (int i = 0; i < Roots.Length; i++)
        {
            Roots[i] = new PlanetoidQuadTree(PlanetoidFace.FACES[i], SizeSetting.MaxEdgeTiles);
        }
    }

    public void Diff(PlanetoidTreeManager Other, PlanetoidTreeDiff Diff)
    {
        if(Other==null)
        {
            foreach (PlanetoidQuadTree Root in Roots)
            {
                List<PlanetoidQuadTree> Leafs = Root.Leafs;
                Leafs.ForEach((PlanetoidQuadTree Leaf) => {
                    Diff(Leaf, PlanetoidTreeDiffType.DIFF_NEW);
                });
            }

            return;
        }

        //DiffByEachRoot(Other, Diff);
        DiffByAllLeafs(Other, Diff);
    }

    private static void DiffByLeafDictionary(Dictionary<string, PlanetoidQuadTree> ThisLeafsDict
     ,Dictionary<string, PlanetoidQuadTree> OtherLeafsDict
     ,PlanetoidTreeDiff Diff)
    {
        IEnumerator<string> ThisKeysEnum = ThisLeafsDict.Keys.GetEnumerator();
        while (ThisKeysEnum.MoveNext())
        {
            string ThisKey = ThisKeysEnum.Current;

            if (OtherLeafsDict.ContainsKey(ThisKey))
            {
                Diff(ThisLeafsDict[ThisKey], PlanetoidTreeDiffType.DIFF_EXISTS);
                OtherLeafsDict.Remove(ThisKey);
            }
            else
            {
                Diff(ThisLeafsDict[ThisKey], PlanetoidTreeDiffType.DIFF_NEW);
            }
        }

        IEnumerator<string> OtherKeysEnum = OtherLeafsDict.Keys.GetEnumerator();
        while (OtherKeysEnum.MoveNext())
            Diff(OtherLeafsDict[OtherKeysEnum.Current], PlanetoidTreeDiffType.DIFF_NOT_EXIST);
    }

    private void DiffByAllLeafs(PlanetoidTreeManager Other, PlanetoidTreeDiff Diff)
    {

        List<PlanetoidQuadTree> ThisLeafs = AllLeafs;
        List<PlanetoidQuadTree> OtherLeafs = Other.AllLeafs;

        Dictionary<string, PlanetoidQuadTree> ThisLeafsDict = TreeListToDict(ThisLeafs);
        Dictionary<string, PlanetoidQuadTree> OtherLeafsDict = TreeListToDict(OtherLeafs);

        DiffByLeafDictionary(ThisLeafsDict, OtherLeafsDict, Diff);
    }

    private void DiffByEachRoot(PlanetoidTreeManager Other, PlanetoidTreeDiff Diff)
    {
        for (int i = 0; i < Roots.Length; i++)
        {
            List<PlanetoidQuadTree> ThisLeafs = Roots[i].Leafs;
            List<PlanetoidQuadTree> OtherLeafs = Other.Roots[i].Leafs;

            Dictionary<string, PlanetoidQuadTree> ThisLeafsDict = TreeListToDict(ThisLeafs);
            Dictionary<string, PlanetoidQuadTree> OtherLeafsDict = TreeListToDict(OtherLeafs);

            DiffByLeafDictionary(ThisLeafsDict, OtherLeafsDict, Diff);
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

    private void RenderVeryFar()
    {
        for (int i = 0; i < Roots.Length; i++)
        {
            Roots[i].ToLeaf();
        }
    }



}
