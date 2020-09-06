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
        if(noiseSetting.Effect.keys.Length!=5)
        {
            throw new System.Exception("AnimationCurve expects exactly 5 Keyframes");
        }
        else if(!(noiseSetting.Effect.keys[0].time==0 
               && noiseSetting.Effect.keys[noiseSetting.Effect.keys.Length-1].time == 1))
        {
            throw new System.Exception("AnimationCurve expect first Keyframe to start at Time=0 and last Keyframe at Time=1");
        }

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
            //Vector3 octaveOffset = new Vector3(prng.Next(-100000, 100000) + NoiseSetting.Offset.x, prng.Next(-100000, 100000) - NoiseSetting.Offset.y, prng.Next(-100000, 100000) - NoiseSetting.Offset.z);
            //octaveOffset = new Vector3(Perlin.Noise(Vector3.right)*NoiseSetting.Offset.x, Perlin.Noise(Vector3.up) * NoiseSetting.Offset.y, Perlin.Noise(Vector3.forward)*NoiseSetting.Offset.z);
            //octaveOffset = Vector3.zero;
            Vector3 octaveOffset = Vector3.zero;

            Vector3 sample = (Position + octaveOffset) / NoiseSetting.Scale * frequency;
            float perlinNoise = Perlin.Noise(sample);
  
            perlinNoise = perlinNoise * Mathf.Clamp(NoiseSetting.Effect.Evaluate(perlinNoise),0f,1f);

 
            noise += perlinNoise * amplitude;
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
            float perlinNoise = Perlin.MinNoise();
            perlinNoise = perlinNoise * Mathf.Clamp(NoiseSetting.Effect.Evaluate(perlinNoise),0f,1f);
            noise += amplitude * perlinNoise;
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
            float perlinNoise = Perlin.MaxNoise();
            perlinNoise = perlinNoise * Mathf.Clamp(NoiseSetting.Effect.Evaluate(perlinNoise),0f,1f);

            noise += amplitude * perlinNoise;
            amplitude *= NoiseSetting.Persistence;
        }

        return noise * (1f / NoiseSetting.Weight);
    }
}


