
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
//using UnityEditor.PackageManager;
using UnityEngine;

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


//internal buffer capacity
public struct SustainedKeyBufferData: IBufferElementData
{
    ///here: frequency; Delta
    public Vector2 Direction;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}
//internal buffer capacity
public struct ReleasedKeyBufferData : IBufferElementData
{
    ///here: frequency; Delta
    public Vector2 Direction;
    public float Delta;
    public float Phase;
    public float currentAmplitude;

}

unsafe
public struct SynthData : IComponentData
{
    //public NativeArray<float> AudioData;
    //public fixed float AudioData[2048];

    //USE THIS ??
    //public DynamicBuffer<KeyData> KeyBuffer;

    public float amplitude;

    //remove ?
    public float frequency;
    //remove ?
    public char Input_key;

    public float SinFactor;
    public float SawFactor;
    public float SquareFactor;

    public ADSREnvelope ADSR;
    /////In MS
    //public float Attack;
    /////In MS
    //public float Decay;
    /////From 0 to 1;
    //public float Sustain;
    /////In MS
    //public float Release;

    public float Delta;

}

//[InternalBufferCapacity(2048)]
//public struct AudioDataBufferElement : IBufferElementData
//{
//    public float value;
//}

public class SynthAuthoring : MonoBehaviour
{

    //public GameObject AudioOutputprefab;
    public float amplitude;
    public float frequency;

    public ADSREnvelope ADSR;
    //public float Attack;
    //public float Decay;
    //public float Sustain;
    //public float Release;

    //private GameObject audioprefab;


    class SynthBaker : Baker<SynthAuthoring>
    {

        public override void Bake(SynthAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddBuffer<SustainedKeyBufferData>(entity);
            AddBuffer<ReleasedKeyBufferData>(entity);

            //authoring.audioprefab = Instantiate(authoring.AudioOutputprefab);

            //authoring.audioprefab.GetComponent<AudioGenerator>().WeaponSynthEntity = entity;

            AddComponent(entity, new SynthData
            {
                //AudioData = new NativeArray<float>(2048, Allocator.Persistent),
                amplitude = authoring.amplitude,
                frequency = authoring.frequency,
                ADSR = authoring.ADSR,
                SinFactor = 1/3f,
                SawFactor = 1 / 3f,
                SquareFactor = 1/3f,
                //Attack = authoring.Attack,
                //Decay = authoring.Decay,
                //Sustain = authoring.Sustain,
                //Release = authoring.Release,
                //KeyBuffer = new DynamicBuffer<KeyData>()

            });
            //ComponentType.fixedArray

        }
    }
}