using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleNoise : INoise
{
    public SimpleNoiseSetting NoiseSetting;
    private PerlinNoise Perlin;

    public SimpleNoise(SimpleNoiseSetting noiseSetting)
    {
        NoiseSetting = noiseSetting;
        Perlin = new PerlinNoise(noiseSetting.Seed);
    }

    public float Noise(Vector3 Position)
    {
        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;

        System.Random prng = new System.Random(NoiseSetting.Seed);

        for (int i = 0; i < NoiseSetting.Octaves; i++)
        {
            Vector3 octaveOffset = new Vector3(prng.Next(-100000, 100000) + NoiseSetting.Offset.x, prng.Next(-100000, 100000) - NoiseSetting.Offset.y, prng.Next(-100000, 100000) - NoiseSetting.Offset.z);
            //octaveOffset = Vector3.zero;
            Vector3 sample = (Position + octaveOffset) / NoiseSetting.Scale * frequency;
            float perlinValue = Perlin.Noise(sample) * 2f - 1f;
            noise += perlinValue * amplitude;
            amplitude *= NoiseSetting.Persistence;
            frequency *= NoiseSetting.Lacunarity;
        }

        return noise * (1f / NoiseSetting.Weight);
    }
    
    public float MinNoise ()
    {
        float noise = 0f;
        float amplitude = 1f;

        for (int i = 0; i < NoiseSetting.Octaves; i++)
        {
            noise += amplitude * Perlin.MinNoise();
            amplitude *= NoiseSetting.Persistence;
        }

        return noise * (1f / NoiseSetting.Weight);
    }

    public float MaxNoise()
    {
        float noise = 0f;
        float amplitude = 1f;

        for (int i = 0; i < NoiseSetting.Octaves; i++)
        {
            noise += amplitude * Perlin.MaxNoise();
            amplitude *= NoiseSetting.Persistence;
        }

        return noise * (1f / NoiseSetting.Weight);
    }
}


