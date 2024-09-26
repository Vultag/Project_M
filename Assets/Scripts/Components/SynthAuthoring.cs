
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct ADSREnvelope
{
    public float Attack;
    public float Decay;
    public float Sustain;
    public float Release;

    public ADSREnvelope(float attack, float decay, float sustain, float release)
    {
        Attack = attack;
        Decay = decay;
        Sustain = sustain;
        Release = release;
    }
}
/* ADSR sample surface for optimized audio processing */
public struct ADSRlayouts
{
    public int AttackSamples;
    public int DecaySamples;    
    public int SustainSamples;
    public int ReleaseSamples;
}


//internal buffer capacity
public struct SustainedKeyBufferData: IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}
//internal buffer capacity
public struct ReleasedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}
public struct PlaybackSustainedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}
public struct PlaybackReleasedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}

unsafe
public struct SynthData : IComponentData
{
    // Default values initializer
    public static SynthData CreateDefault()
    {
        return new SynthData {
            amplitude = 0.2f,
            Osc1SinSawSquareFactor = new float3(0.5f,0,0),
            Osc2SinSawSquareFactor = new float3(0.5f, 0, 0),
            ADSR = new ADSREnvelope
            {
                Attack = 0.1f,
                Decay = 2,
                Sustain = 0.5f,
                Release = 1
            }
        };
    }
    public float amplitude;

    public float3 Osc1SinSawSquareFactor;
    public float Osc1Fine;
    public float3 Osc2SinSawSquareFactor;
    public float Osc2Fine;
    public float Osc2Semi;

    public ADSREnvelope ADSR;

}

public class SynthAuthoring : MonoBehaviour
{

    public float amplitude;
    public float frequency;

    public ADSREnvelope ADSR;

    class SynthBaker : Baker<SynthAuthoring>
    {

        public override void Bake(SynthAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddBuffer<SustainedKeyBufferData>(entity);
            AddBuffer<ReleasedKeyBufferData>(entity);


            AddComponent(entity, new SynthData
            {
                amplitude = authoring.amplitude,
                ADSR = authoring.ADSR,
                Osc1SinSawSquareFactor = new float3(0.5f, 0, 0),
                Osc2SinSawSquareFactor = new float3(0.5f, 0, 0),
                //SinFactor = 1 / 3f,
                //SawFactor = 1 / 3f,
                //SquareFactor = 1/3f,

            });

        }
    }
}