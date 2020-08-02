using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public Noise2DData NoiseData;
    public PlanetoidQuadTree Tree;
    public Mesh Mesh;
    public PlanetoidSizeSetting SizeSetting;

    public MeshGenerator(PlanetoidSizeSetting sizeSetting, Noise2DData noiseData, PlanetoidQuadTree tree)
    {
        NoiseData = noiseData;
        Tree = tree;
        SizeSetting = sizeSetting;
        Init();
    }

    public void Init()
    {
        int Edges = NoiseData.Edges;
        int Verts = NoiseData.Verts;

        int[] ChunkTriangles = new int[Edges * Edges * 6]; ;
        Vector2[] ChunkUVs = new Vector2[Verts * Verts];
        Color[] VertColors = new Color[Verts * Verts];

        int TriangleIndex = 0;

        for (int y = 0; y < Verts; y++)
        {
            for (int x = 0; x < Verts; x++)
            {
                int VectorIndex = (y * Verts) + x;
                float X = ((Tree.Start.x / NoiseData.Resolution)) + x;
                float Y = ((Tree.Start.y / NoiseData.Resolution)) + y;

 
                VertColors[VectorIndex] = Color.red;


                ChunkUVs[VectorIndex] = new Vector2((float)x / ((float)Edges), (float)y / ((float)Edges));

                if (x < Verts - 1 && y < Verts - 1)
                {
                    TriangleInSquareIndex(ChunkTriangles, TriangleIndex, VectorIndex, Verts);
                    TriangleIndex += 6;
                }

            }
        }

        Mesh = new Mesh();
        Mesh.Clear();
        Mesh.vertices = NoiseData.Dirs;
        Mesh.triangles = ChunkTriangles;
        Mesh.uv = ChunkUVs;
        Mesh.colors = VertColors;
        Mesh.RecalculateNormals();
    }

    public static void TriangleInSquareIndex(int[] TriangleList, int TriangleIndex, int VectorIndex, int VerticesInRow)
    {
        TriangleList[TriangleIndex] = VectorIndex;
        TriangleList[TriangleIndex + 1] = VectorIndex + 1 + VerticesInRow;
        TriangleList[TriangleIndex + 2] = VectorIndex + VerticesInRow;

        TriangleList[TriangleIndex + 3] = VectorIndex;
        TriangleList[TriangleIndex + 4] = VectorIndex + 1;
        TriangleList[TriangleIndex + 5] = VectorIndex + 1 + VerticesInRow;
    }
}
 