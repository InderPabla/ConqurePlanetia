using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise : INoise
{
    private int Seed;
    private Noise _Noise;
    public PerlinNoise(int seed)
    {
        Seed = seed;
        _Noise = new Noise(Seed);
    }

    public float Noise(Vector3 Position)
    {
        return (_Noise.Evaluate(Position) + 1f) / 2f;
    }

    public float MaxNoise()
    {
        return 1f;
    }

    public float MinNoise()
    {
        return 0f;
    }
}
