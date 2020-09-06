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
    float[] GenerateNoise(Vector2 StartPosition, int Edges, int Vert, int EdgeRes, int VertRes, int Resolution, PlanetoidFace Face);
}

public class NoiseGenerator : INoise2D
{
    private SimpleNoise[] Noises;

    public float _MinNoise = -999;
    public float _MaxNoise = -999;

    private PlanetoidMapEngineSolution Solution;
    private bool IgnoreNosieSettings;

    public SimpleNoiseSettingCompute[] SimpleNoiseSettingsCompute;

    public NoiseGenerator(SimpleNoiseSetting[] noiseSetting, PlanetoidMapEngineSolution solution, bool ignoreNoiseSettings)
    {
        Noises = new SimpleNoise[noiseSetting.Length];
        Solution = solution;
        IgnoreNosieSettings = ignoreNoiseSettings;

        for (int i = 0; i < noiseSetting.Length; i++)
        {
            noiseSetting[i] = noiseSetting[i].Validate();
            if (IgnoreNosieSettings) noiseSetting[i].Enabled = false;
            Noises[i] = new SimpleNoise(noiseSetting[i]);
        }

        InitMinMaxNoise();
        SetupSimpleComputeSettings();
    }


    private void SetupSimpleComputeSettings()
    {
        SimpleNoiseSettingsCompute = new SimpleNoiseSettingCompute[Noises.Length];

        float MinNoise = 0f;
        float MaxNoise = 0f;

        for (int i = 0; i < SimpleNoiseSettingsCompute.Length; i++)
        {
            SimpleNoiseSettingCompute Compute = new SimpleNoiseSettingCompute();
            SimpleNoiseSetting SN = Noises[i].NoiseSetting;

            Compute.Lacunarity = SN.Lacunarity;
            Compute.Octaves = SN.Octaves;
            Compute.Offset = SN.Offset;
            Compute.Persistence = SN.Persistence;
            Compute.Scale = SN.Scale;
            Compute.Seed = SN.Seed;
            Compute.Weight = SN.Weight;
            Compute.BaseLacunarity = SN.BaseLacunarity;
            Compute.StrengthMultiplier = SN.StrengthMultiplier;
            Compute.MinValue = SN.MinValue;
            Compute.Enabled = SN.Enabled == true ? 1 : 0 ;

            Keyframe[] Keys = SN.Effect.keys;

            int k = 0;
            Compute.Keyframe1.InTangent = Keys[k].inTangent;
            Compute.Keyframe1.OutTangent = Keys[k].outTangent;
            Compute.Keyframe1.Time = Keys[k].time;
            Compute.Keyframe1.Value = Keys[k].value;
            k++;

            Compute.Keyframe2.InTangent = Keys[k].inTangent;
            Compute.Keyframe2.OutTangent = Keys[k].outTangent;
            Compute.Keyframe2.Time = Keys[k].time;
            Compute.Keyframe2.Value = Keys[k].value;
            Compute.Keyframe2.ComputeABCD(Compute.Keyframe1);
            k++;

            Compute.Keyframe3.InTangent = Keys[k].inTangent;
            Compute.Keyframe3.OutTangent = Keys[k].outTangent;
            Compute.Keyframe3.Time = Keys[k].time;
            Compute.Keyframe3.Value = Keys[k].value;
            Compute.Keyframe3.ComputeABCD(Compute.Keyframe2);
            k++;

            Compute.Keyframe4.InTangent = Keys[k].inTangent;
            Compute.Keyframe4.OutTangent = Keys[k].outTangent;
            Compute.Keyframe4.Time = Keys[k].time;
            Compute.Keyframe4.Value = Keys[k].value;
            Compute.Keyframe4.ComputeABCD(Compute.Keyframe3);
            k++;

            Compute.Keyframe5.InTangent = Keys[k].inTangent;
            Compute.Keyframe5.OutTangent = Keys[k].outTangent;
            Compute.Keyframe5.Time = Keys[k].time;
            Compute.Keyframe5.Value = Keys[k].value;
            Compute.Keyframe5.ComputeABCD(Compute.Keyframe4);
            k++;

            SimpleNoiseSettingsCompute[i] = Compute;

            //Calculate Max Height Value
            if (Compute.Enabled==0) continue;

            float frequency = Compute.BaseLacunarity;
            float amplitude = 1f;
            float noiseVal = 0f;
            float weight = 1f;

            float maxPerlinNoiseType1 = 1f;

            for (int o = 0; o < Compute.Octaves; o++)
            {

                //float v = ((maxPerlinNoiseType1 + 1.0f) / 2.0f);
                //v = 1f - Mathf.Abs(maxPerlinNoiseType2);
                //v *= v;
                float v = (maxPerlinNoiseType1 + 1f) * 0.5f;

                weight = Mathf.Max(Mathf.Min(v * Compute.Weight, 1f), 0f);

                v *= weight;

                noiseVal += v * amplitude;
                amplitude *= Compute.Persistence;
                frequency *= Compute.Lacunarity;
            }

            noiseVal = Mathf.Max(0, noiseVal - Compute.MinValue);
            noiseVal *= Compute.StrengthMultiplier;

            MaxNoise += noiseVal;   
        }

       
        _MinNoise = MinNoise;
        _MaxNoise = MaxNoise; 
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

    public float[] GenerateNoise(Vector2 StartPosition, int Edges, int Vert, int EdgeRes, int VertRes, int Resolution, PlanetoidFace Face)
    {

        float[] noises = new float[VertRes * VertRes];

        float maxNoise = MaxNoise();
        float minNoise = MinNoise();

        for (int y = 0; y < VertRes; y++)
        {
            for (int x = 0; x < VertRes; x++)
            {
                int VertIndex = (y * VertRes) + x;

                float X = StartPosition.x + (x*Resolution);
                float Y = StartPosition.y + (y*Resolution);

                Vector3 OnCircle = Solution.CubeToSphere(Face, X, Y);
                
                float onShapeNoise = Noise(OnCircle);
                float onShapeNoiseNormalized = (onShapeNoise + 1) / (maxNoise / 0.9f);
                float noise = Mathf.Clamp(onShapeNoiseNormalized, 0, int.MaxValue);
                noise = (onShapeNoise-minNoise)/(maxNoise-minNoise);

                noises[VertIndex] = noise;
            }
        }

        return noises;
    }

    private void InitMinMaxNoise()
    {
        MinNoise();
        MaxNoise();
    }

    public float MinNoise()
    {
        if (_MinNoise != -999) return _MinNoise;

        _MinNoise = 0;

        for (int i = 0; i < Noises.Length; i++)
            _MinNoise += Noises[i].MinNoise();

        return _MinNoise;
    }

    public float MaxNoise()
    {
        if (_MaxNoise != -999) return _MaxNoise;

        _MaxNoise = 0;

        for (int i = 0; i < Noises.Length; i++)
            _MaxNoise += Noises[i].MaxNoise();

        return _MaxNoise;
    }

    public SimpleNoise[] GetSimpleNoises()
    {
        return Noises;
    }
}

