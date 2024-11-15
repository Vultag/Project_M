
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
public struct Filter
{
    public float Cutoff;
    public float Resonance;

    public Filter(float cutoff, float resonance)
    {
        Cutoff = cutoff;
        Resonance = resonance;
    }
}
/// <summary>
/// PUT FILTER COEFFICIENT RELATED STUFF IN A SEPERATED FILE
/// </summary>
public struct FilterCoefficients
{

    public float b0;
    public float b1;
    public float b2;
    public float a1;
    public float a2;

    public FilterCoefficients BuildLowpassCoefficients(float normalizedCutoff, float normalizedResonance)
    {

        /// Map normalized cutoff (0 to 1) to frequency range (200 Hz to 24,000 Hz)
        /// tweaked to 23998 to prevend accasional artefacts happening on key release when cutoff>=24000 for a unknowed reason
        float minCutoff = 200.0f;
        float maxCutoff = 23998.0f;
        float cutoff = Mathf.Lerp(minCutoff, maxCutoff, normalizedCutoff);

        // Map normalized resonance (0 to 1) to Q factor range (0.707 to 10)
        float minQ = 0.707f;
        float maxQ = 5.0f;
        float q = Mathf.Lerp(minQ, maxQ, normalizedResonance);

        float omega = 2.0f * Mathf.PI * cutoff / 48000;
        float alpha = Mathf.Sin(omega) / (2.0f * q);
        float cosw = Mathf.Cos(omega);

        // Calculate low-pass filter coefficients
        b0 = (1.0f - cosw) / 2.0f;
        b1 = 1.0f - cosw;
        b2 = (1.0f - cosw) / 2.0f;
        float a0 = 1.0f + alpha;
        a1 = -2.0f * cosw;
        a2 = 1.0f - alpha;

        // Normalize the coefficients
        b0 /= a0;
        b1 /= a0;
        b2 /= a0;
        a1 /= a0;
        a2 /= a0;

        return this;
    }
    public FilterCoefficients BuildHighpassCoefficients(float normalizedCutoff, float normalizedResonance)
    {

        /// Map normalized cutoff (0 to 1) to frequency range (200 Hz to 8,000 Hz)
        float minCutoff = 200.0f;
        float maxCutoff = 8000.0f;
        float cutoff = Mathf.Lerp(minCutoff, maxCutoff, normalizedCutoff);

        // Map normalized resonance (0 to 1) to Q factor range (0.707 to 10)
        float minQ = 0.707f;
        float maxQ = 5.0f;
        float q = Mathf.Lerp(minQ, maxQ, normalizedResonance);

        float omega = 2.0f * Mathf.PI * cutoff / 48000;
        float alpha = Mathf.Sin(omega) / (2.0f * q);
        float cosw = Mathf.Cos(omega);

        // Calculate high-pass filter coefficients
        b0 = (1.0f + cosw) / 2.0f;
        b1 = -(1.0f + cosw);
        b2 = (1.0f + cosw) / 2.0f;
        float a0 = 1.0f + alpha;
        a1 = -2.0f * cosw;
        a2 = 1.0f - alpha;

        // Normalize the coefficients
        b0 /= a0;
        b1 /= a0;
        b2 /= a0;
        a1 /= a0;
        a2 /= a0;

        return this;
    }
    public FilterCoefficients BuildBandpassCoefficients(float normalizedCutoff, float normalizedResonance)
    {

        /// Map normalized cutoff (0 to 1) to frequency range (200 Hz to 18,000 Hz)
        float minCutoff = 200.0f;
        float maxCutoff = 18000.0f;
        float cutoff = Mathf.Lerp(minCutoff, maxCutoff, normalizedCutoff);

        // Map normalized resonance (0 to 1) to Q factor range (0.707 to 10)
        float minQ = 0.707f;
        float maxQ = 5.0f;
        float q = Mathf.Lerp(minQ, maxQ, normalizedResonance);

        float omega = 2.0f * Mathf.PI * cutoff / 48000;
        float alpha = Mathf.Sin(omega) / (2.0f * q);
        float cosw = Mathf.Cos(omega);

        // Calculate high-pass filter coefficients
        b0 = alpha;
        b1 = 0;
        b2 = -alpha;
        float a0 = 1.0f + alpha;
        a1 = -2.0f * cosw;
        a2 = 1.0f - alpha;

        // Normalize the coefficients
        b0 /= a0;
        b1 /= a0;
        b2 /= a0;
        a1 /= a0;
        a2 /= a0;

        return this;
    }
}
public struct FilterDelayElements
{
    public float x0, x1; // input delay elements
    public float y0, y1; // output delay elements
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
/// <summary>
/// Dont need phase here ??
/// </summary>
public struct SustainedKeyBufferData: IBufferElementData
{
    public Vector2 TargetDirLenght;
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;
    ///filter for coloring
    public Filter filter;

}
//internal buffer capacity
public struct ReleasedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;
    public float amplitudeAtRelease;
    ///filter for coloring
    public Filter filter;
    public float cutoffEnvelopeAtRelease;

}
public struct PlaybackSustainedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 StartDirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;
    ///filter for coloring
    public Filter filter;

}
public struct PlaybackReleasedKeyBufferData : IBufferElementData
{
    public Vector2 DirLenght;
    public Vector2 EffectiveDirLenght;
    public float Delta;
    public float Phase;
    public float currentAmplitude;
    public float amplitudeAtRelease;
    ///filter for coloring
    public Filter filter;
    public float cutoffEnvelopeAtRelease;

}

unsafe
public struct SynthData : IComponentData
{
    // Default values initializer
    public static SynthData CreateDefault()
    {
        return new SynthData {
            amplitude = 0.2f,
            Osc1SinSawSquareFactor = new float3(0.5f, 0, 0),
            Osc2SinSawSquareFactor = new float3(0.5f, 0, 0),
            Osc1PW = 0.25f,
            Osc2PW = 0.25f,
            ADSR = new ADSREnvelope
            {
                Attack = 0.1f,
                Decay = 2,
                Sustain = 0.5f,
                Release = 1
            },
            filter = new Filter
            {
                Cutoff = 1.0f,
                Resonance = 0.0f
            },
            filterType = 0,
            filterADSR = new ADSREnvelope
            {
                Attack = 0.1f,
                Decay = 2,
                Sustain = 0.5f,
                Release = 1
            },
            UnissonVoices = 1,
            UnissonDetune = 26f,
            UnissonSpread = 0,
            Portomento = 0f,
            Legato = false
        };
    }
    public float amplitude;

    public float3 Osc1SinSawSquareFactor;
    public float Osc1Fine;
    public float3 Osc2SinSawSquareFactor;
    public float Osc2Fine;
    public float Osc2Semi;
    public float Osc1PW;
    public float Osc2PW;

    public ADSREnvelope ADSR;
    public Filter filter;
    /// 0 = lowpass ; 1 = highpass ; 2 = bandpass ;
    public short filterType;
    public float filterEnvelopeAmount;
    public ADSREnvelope filterADSR;

    public short UnissonVoices;
    /// Clamped between 2f and 50f (semitones)
    public float UnissonDetune;
    public float UnissonSpread;

    /// Glide in seconds
    public float Portomento;
    public bool Legato;

}

/// <summary>
/// 
///  Only serve as a tag for now -> cleanup
/// 
/// </summary>
public class SynthAuthoring : MonoBehaviour
{

    //public float amplitude;
    //public float frequency;

    //public ADSREnvelope ADSR;

    class SynthBaker : Baker<SynthAuthoring>
    {

        public override void Bake(SynthAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            //AddBuffer<SustainedKeyBufferData>(entity);
            //AddBuffer<ReleasedKeyBufferData>(entity);


            AddComponent(entity, new SynthData
            {
                //amplitude = authoring.amplitude,
                //ADSR = authoring.ADSR,
                //Osc1SinSawSquareFactor = new float3(0.5f, 0, 0),
                //Osc2SinSawSquareFactor = new float3(0.5f, 0, 0),
                //SinFactor = 1 / 3f,
                //SawFactor = 1 / 3f,
                //SquareFactor = 1/3f,

            });

        }
    }
}