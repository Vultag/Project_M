
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
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
    public Vector2 Direction;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}
//internal buffer capacity
public struct ReleasedKeyBufferData : IBufferElementData
{
    public Vector2 Direction;
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
            SinFactor = 1f / 3f,
            SawFactor = 1f / 3f,
            SquareFactor = 1f / 3f,
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

    public float SinFactor;
    public float SawFactor;
    public float SquareFactor;

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
                SinFactor = 1 / 3f,
                SawFactor = 1 / 3f,
                SquareFactor = 1/3f,

            });

        }
    }
}