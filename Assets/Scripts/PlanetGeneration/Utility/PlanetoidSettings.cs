using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlanetoidSizeSetting
{
    public int EdgePower;
    public string PlanetName;
    public int MinEdgeTiles;
    public int MinResolution;
    public Vector3 Position;
    public int PlanetSeed;
    public float CloudScaler;
    public float NodeScaler;
    public static float LINEAR_RADIUS_MULTIPLIER = 0.25f;

    public PlanetoidSizeSetting(string planetName, int edgePower, int minEdgeTiles, int minResolution, Vector3 position)
    {
        EdgePower = edgePower;
        PlanetName = planetName;
        MinEdgeTiles = minEdgeTiles;
        MinResolution = minResolution;
        Position = position;
        PlanetSeed = 0;
        CloudScaler = 1f;
        NodeScaler = 1200f;

        if (MaxTreeDepth <= 0)
        {
            throw new System.Exception(string.Format("MinEdgeTiles: {0}, is >= to MaxEdgeTiles: {1}, yeilding in invalid MaxTreeDepth:{2}. MaxTreeDepth must be >=0.", MinEdgeTiles, MaxEdgeTiles, MaxTreeDepth));
        }
    }

    public int Seed
    {
       get
       {
            return (int) Position.magnitude + PlanetSeed;
       }
    }

    public int SeedLarge
    {
        get
        {
            return (int) Position.sqrMagnitude + PlanetSeed;
        }
    }

    public int MaxEdgeTiles
    {
        get { return (int)Mathf.Pow(2, EdgePower); }
    }

    public float Circumference
    {
        get { return MaxEdgeTiles * 4; }
    }

    public float RadiansPerMeter
    {
        get { return (Mathf.PI*2f)/Circumference; }
    }

    public float Radius
    {
        get { return Circumference / (2f * Mathf.PI); }
    }

    public float SurfaceArea
    {
        get { return 4 * Mathf.PI * Mathf.Pow(Radius, 2); }
    }

    public float Mass
    {
        get
        {
            return (4f / 3f) * Mathf.PI * Mathf.Pow(Radius, 3f);
        }
    }

    public int MaxTreeDepth
    {
        get
        {
            int Edges = MaxEdgeTiles;
            int DepthCount = 0;
            while(Edges> MinEdgeTiles)
            {
                Edges = Edges / 2;
                DepthCount++;
            }

            return DepthCount;
        }
    }

    public Sphere Sphere
    {
        get
        {
            return new Sphere(Vector3.zero,Radius);
        }
    }

    public PlanetoidRenderType RenderTypeForDistanceToSurface(float Distance, float MaxHeight)
    {
        float radius = Radius;

        if (Distance <= RenderTypeRadius(PlanetoidRenderType.PLAYER_ON_PLANET, radius, MaxHeight))
            return PlanetoidRenderType.PLAYER_ON_PLANET;

        if (Distance <= RenderTypeRadius(PlanetoidRenderType.PLAYER_VERY_CLOSE, radius, MaxHeight))
            return PlanetoidRenderType.PLAYER_VERY_CLOSE;

        if (Distance <= RenderTypeRadius(PlanetoidRenderType.PLAYER_CLOSE, radius, MaxHeight))
            return PlanetoidRenderType.PLAYER_CLOSE;

        if (Distance <= RenderTypeRadius(PlanetoidRenderType.PLAYER_FAR, radius, MaxHeight))
            return PlanetoidRenderType.PLAYER_FAR;

        if (Distance <= RenderTypeRadius(PlanetoidRenderType.PLAYER_VERY_FAR, radius, MaxHeight))
            return PlanetoidRenderType.PLAYER_VERY_FAR;

        return PlanetoidRenderType.PLAYER_NOT_IN_RANGE;
    }

    public int MaxResolution
    {
        get
        {
            if (MaxEdgeTiles <= 64) return 4*2;
            else if (MaxEdgeTiles <= 256) return 16 * 2;
            return MaxEdgeTiles / (16 * 2);
        }
    }


    public static float RenderTypeRadius(PlanetoidRenderType Type, float Radius, float MaxHeight)
    {
        //return Radius + (LINEAR_RADIUS_MULTIPLIER * Mathf.Pow(((int)Type + 1), 2)* Radius);
        //return (Radius*2f) + (1 * ((int)Type + 1) * Radius * LINEAR_RADIUS_MULTIPLIER);
        //return (Radius + (Mathf.Log(Radius)* 30 * ((int)Type + 1)) + (Radius*Mathf.Log(Radius) * LINEAR_RADIUS_MULTIPLIER* LINEAR_RADIUS_MULTIPLIER));

        float RadiusAboveSurface = (Radius+ MaxHeight) + (30 * ((int)Type + 1) * Mathf.Log(Radius));
        return RadiusAboveSurface;
    }

    public static PlanetoidSizeSetting SmallPlanet
    {
        get
        {
            return new PlanetoidSizeSetting("Temp", 9, 32, 1, Vector3.zero);
        }
    }

    public static PlanetoidSizeSetting LargePlanet
    {
        get
        {
            return new PlanetoidSizeSetting("Temp", 13, 32, 1, Vector3.zero);
        }
    }


    override
    public string ToString()
    {
        return string.Format("[{0}] Name:{1}, EdgePower:{2}, MinEdgeTiles:{3}, MaxEdgeTiles:{4}, \n\tCircumference:{5}, Radius:{6}, Mass:{7}, MaxTreeDepth:{8}, MaxResolution:{9}", typeof(PlanetoidSizeSetting).Name,PlanetName,EdgePower,MinEdgeTiles,MaxEdgeTiles,Circumference,Radius,Mass,MaxTreeDepth,MaxResolution);
    }

}

public enum PlanetoidRenderType
{
    PLAYER_ON_PLANET, PLAYER_VERY_CLOSE, PLAYER_CLOSE, PLAYER_FAR, PLAYER_VERY_FAR, PLAYER_NOT_IN_RANGE
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
    public float MinValue;
    public bool Enabled;
    public float BaseLacunarity;
    public float StrengthMultiplier;

    public AnimationCurve Effect;
    

    public SimpleNoiseSetting(Vector3 offset, int seed)
    {
        Offset = offset;
        Seed = seed;

        Octaves = 1;
        Lacunarity = 2f;
        Scale = 1f;
        Persistence = 0.6f;
        Weight = 1f;
       
        BaseLacunarity = 1f;
        StrengthMultiplier = 1f;

        MinValue = 0f;
        Enabled = true;
        Effect = AnimationCurve.Constant(0f, 1f, 1f);

        Validate();
    }

    public SimpleNoiseSetting(Vector3 offset, int seed, int octaves, float lacunarity, float scale, float persistence, float weight, AnimationCurve effect)
        : this()
    {
        Seed = seed;
        Offset = offset;

        Octaves = octaves;
        Lacunarity = lacunarity;
        Scale = scale;
        Persistence = persistence;
        Weight = weight;

        Effect = effect;

        Validate();
    }

    public SimpleNoiseSetting Validate()
    {
        Octaves = Mathf.Max(Octaves, 1);
        Lacunarity = Mathf.Max(Lacunarity, 1f);
        Scale = Mathf.Max(Scale, 0.01f);
        Persistence = Mathf.Clamp01(Persistence);
        Weight = Mathf.Clamp01(Weight);
        MinValue = Mathf.Clamp01(MinValue);

        return this;
    }
}


public struct SimpleNoiseSettingCompute
{
    public Vector3 Offset;
    public int Seed;

    public int Octaves;
    public float Lacunarity;
    public float Scale;
    public float Persistence;
    public float Weight;
    public float MinValue;
    public float BaseLacunarity;
    public float StrengthMultiplier;

    public int Enabled;
    //public AnimationCurveCompute Effect;

    public KeyframeCompute Keyframe1;
    public KeyframeCompute Keyframe2;
    public KeyframeCompute Keyframe3;
    public KeyframeCompute Keyframe4;
    public KeyframeCompute Keyframe5;
}

public struct TriangleCompute
{
    public int A;
    public int B;
    public int C;
};

public struct KeyframeCompute
{
    public float Time;
    public float InTangent;
    public float OutTangent;
    public float Value;
    public float a, b, c, d;

    public void ComputeABCD(KeyframeCompute PreviousKeyframe)
    {
        float p1x = PreviousKeyframe.Time;
        float p1y = PreviousKeyframe.Value;
        float tp1 = PreviousKeyframe.OutTangent;
        float p2x = Time;
        float p2y = Value;
        float tp2 = InTangent;

        a = (p1x * tp1 + p1x * tp2 - p2x * tp1 - p2x * tp2 - 2 * p1y + 2 * p2y) / (p1x * p1x * p1x - p2x * p2x * p2x + 3 * p1x * p2x * p2x - 3 * p1x * p1x * p2x);
        b = ((-p1x * p1x * tp1 - 2 * p1x * p1x * tp2 + 2 * p2x * p2x * tp1 + p2x * p2x * tp2 - p1x * p2x * tp1 + p1x * p2x * tp2 + 3 * p1x * p1y - 3 * p1x * p2y + 3 * p1y * p2x - 3 * p2x * p2y) / (p1x * p1x * p1x - p2x * p2x * p2x + 3 * p1x * p2x * p2x - 3 * p1x * p1x * p2x));
        c = ((p1x * p1x * p1x * tp2 - p2x * p2x * p2x * tp1 - p1x * p2x * p2x * tp1 - 2 * p1x * p2x * p2x * tp2 + 2 * p1x * p1x * p2x * tp1 + p1x * p1x * p2x * tp2 - 6 * p1x * p1y * p2x + 6 * p1x * p2x * p2y) / (p1x * p1x * p1x - p2x * p2x * p2x + 3 * p1x * p2x * p2x - 3 * p1x * p1x * p2x));
        d = ((p1x * p2x * p2x * p2x * tp1 - p1x * p1x * p2x * p2x * tp1 + p1x * p1x * p2x * p2x * tp2 - p1x * p1x * p1x * p2x * tp2 - p1y * p2x * p2x * p2x + p1x * p1x * p1x * p2y + 3 * p1x * p1y * p2x * p2x - 3 * p1x * p1x * p2x * p2y) / (p1x * p1x * p1x - p2x * p2x * p2x + 3 * p1x * p2x * p2x - 3 * p1x * p1x * p2x));
    }
};

public struct HouseCompute
{
    public Vector3 DirectionOrientation;
    public Vector3 Position;
    public Vector3 Scale;
}

