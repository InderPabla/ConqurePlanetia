using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INoise
{
    float Noise(Vector3 Position);

    float MaxNoise();

    float MinNoise();
}

public interface INoise2D : INoise
{
    Noise2DData Noise2D(PlanetoidSizeSetting Size, Vector2 StartPosition, int EdgeTiles, int Resolution, PlanetoidFace Face);
}

public class Noise2DData
{
    public Vector3[] Dirs;
    public float[] Noises;
    public int Verts;
    public int Edges;
    public float MinNoise;
    public float MaxNoise;
    public int Resolution;
  

    public Noise2DData(int verts, int edges, Vector3[] dirs, float[] noises, float minNoise, float maxNoise, int resolution)
    {
        Dirs = dirs;
        Noises = noises;
        Verts = verts;
        Edges = edges;
        MinNoise = minNoise;
        MaxNoise = maxNoise;
        Resolution = resolution;
    }
}

public class NoiseGenerator : INoise2D
{
    private SimpleNoise[] Noises;

    private float _MinNoise = -1;
    private float _MaxNoise = -1;

    public NoiseGenerator(SimpleNoiseSetting[] noiseSetting)
    {
        Noises = new SimpleNoise[noiseSetting.Length];
        for (int i = 0; i < noiseSetting.Length; i++)
        {
            noiseSetting[i] = noiseSetting[i].Validate();
            Noises[i] = new SimpleNoise(noiseSetting[i]);
        }

        InitMinMaxNoise();
    }

    public float Noise(Vector3 Position)
    {
        float noise = 0;

        for (int i = 0; i < Noises.Length; i++)
        {
            noise += Noises[i].Noise(Position);
        }

        return noise;
    }

    public Noise2DData Noise2D(PlanetoidSizeSetting Size, Vector2 StartPosition, int EdgeTiles, int Resolution, PlanetoidFace Face)
    {
        int Edges = EdgeTiles / Resolution;
        int Verts = Edges + 1;

        float[] noises = new float[Verts * Verts];
        Vector3[] dirs = new Vector3[Verts * Verts];

        float maxNoise = MaxNoise();
        float minNoise = MinNoise();

        for (int y = 0; y < Verts; y++)
        {
            for (int x = 0; x < Verts; x++)
            {
                int VertIndex = (y * Verts) + x;

                float X = (StartPosition.x / Resolution) + x;
                float Y = (StartPosition.y / Resolution) + y;

                Vector3 OnCube = PlanetoidFace.OnCube(Face, X, Y, Size.MaxEdgeTiles);
                Vector3 OnCircle = PlanetoidFace.OnSphereNormalized(OnCube);//OnCube.normalized;
                Vector3 OnShape = OnCircle;

                float onShapeNoise = Noise(OnShape);
                float onShapeNoiseNormalized = (onShapeNoise + 1) / (maxNoise / 0.9f);
                float noise = Mathf.Clamp(onShapeNoiseNormalized, 0, int.MaxValue);

                noises[VertIndex] = noise;
                dirs[VertIndex] = OnShape;
            }
        }

        return new Noise2DData(Verts, Edges,dirs, noises, minNoise, maxNoise, Resolution);
    }

    private void InitMinMaxNoise()
    {
        MinNoise();
        MaxNoise();
    }

    public float MinNoise()
    {
        if (_MinNoise != -1) return _MinNoise;

        _MinNoise = 0;

        for (int i = 0; i < Noises.Length; i++)
            _MinNoise += Noises[i].MinNoise();

        return _MinNoise;
    }

    public float MaxNoise()
    {
        if (_MaxNoise != -1) return _MaxNoise;

        _MaxNoise = 0;

        for (int i = 0; i < Noises.Length; i++)
            _MaxNoise += Noises[i].MaxNoise();

        return _MaxNoise;
    }
}

