using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlanetoidSizeSetting
{
    public int EdgePower;
    public string PlanetName;
    public int MinEdgeTiles;

    public PlanetoidSizeSetting(string planetName, int edgePower, int minEdgeTiles)
    {
        EdgePower = edgePower;
        PlanetName = planetName;
        MinEdgeTiles = minEdgeTiles;

        if (MaxTreeDepth <= 0)
        {
            throw new System.Exception(string.Format("MinEdgeTiles: {0}, is >= to MaxEdgeTiles: {1}, yeilding in invalid MaxTreeDepth:{2}. MaxTreeDepth must be >=0.", MinEdgeTiles, MaxEdgeTiles, MaxTreeDepth));
        }

        Debug.Log(string.Format("PlanetName: {0}, EdgePower: {1}, MaxEdgeTiles: {2}, Circumference: {3}, Radius: {4}", PlanetName, EdgePower, MaxEdgeTiles, Circumference, Radius));
    }

    public int MaxEdgeTiles
    {
        get { return (int)Mathf.Pow(2, EdgePower); }
    }

    public float Circumference
    {
        get { return MaxEdgeTiles * 4; }
    }

    public float Radius
    {
        get { return Circumference / (2f * Mathf.PI); }
    }

    public int MaxTreeDepth
    {
        get { return (MaxEdgeTiles / MinEdgeTiles) - 1; }
    }

    public PlanetoidRenderType RenderTypeForDistanceToSurface(float Distance)
    {
        float radius = Radius;

        float linearRadiusMultiplier = 0.25f;

        if (Distance < radius * (linearRadiusMultiplier * 1f) * 1f) return PlanetoidRenderType.PLAYER_ON_PLANET;

        if (Distance < radius * (linearRadiusMultiplier * 2f) * 2f) return PlanetoidRenderType.PLAYER_VERY_CLOSE;

        if (Distance < radius * (linearRadiusMultiplier * 3f) * 3f) return PlanetoidRenderType.PLAYER_CLOSE;
 
        if (Distance < radius * (linearRadiusMultiplier * 4f) * 4f) return PlanetoidRenderType.PLAYER_FAR;
 
        if (Distance < radius * (linearRadiusMultiplier * 5f) * 5f) return PlanetoidRenderType.PLAYER_VERY_FAR;

        return PlanetoidRenderType.PLAYER_VERY_FAR;
    }
}

public enum PlanetoidRenderType
{
    PLAYER_ON_PLANET, PLAYER_VERY_CLOSE, PLAYER_CLOSE, PLAYER_FAR, PLAYER_VERY_FAR
}


[System.Serializable]
public struct SimpleNoiseSetting
{
    public Vector3 Offset;
    public int Seed;

    public int Octaves;
    public float Lacunarity;
    public float Scale;
    public float Persistence;
    public float Weight;

    public SimpleNoiseSetting(Vector3 offset, int seed)
    {
        Offset = offset;
        Seed = seed;

        Octaves = 1;
        Lacunarity = 2f;
        Scale = 1f;
        Persistence = 0.6f;
        Weight = 1f;

         Validate();
    }

    public SimpleNoiseSetting(Vector3 offset, int seed, int octaves, float lacunarity, float scale, float persistence, float weight)
        : this()
    {
        Seed = seed;
        Offset = offset;

        Octaves = octaves;
        Lacunarity = lacunarity;
        Scale = scale;
        Persistence = persistence;
        Weight = weight;

        Validate();
    }

    public SimpleNoiseSetting Validate()
    {
        Octaves = Mathf.Max(Octaves, 1);
        Lacunarity = Mathf.Max(Lacunarity, 1f);
        Scale = Mathf.Max(Scale, 0.01f);
        Persistence = Mathf.Clamp01(Persistence);
        Weight = Mathf.Clamp01(Weight);

        return this;
    }
}
