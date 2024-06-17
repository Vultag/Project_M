
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
//using UnityEditor.PackageManager;
using UnityEngine;

//internal buffer capacity
public struct KeyBufferData: IBufferElementData
{
    public int test;
    ///here: frequency; Delta
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

    //private GameObject audioprefab;

    private void Awake()
    {

    }
 

    class SynthBaker : Baker<SynthAuthoring>
    {

        public override void Bake(SynthAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddBuffer<KeyBufferData>(entity);

            //authoring.audioprefab = Instantiate(authoring.AudioOutputprefab);

            //authoring.audioprefab.GetComponent<AudioGenerator>().WeaponSynthEntity = entity;


            AddComponent(entity, new SynthData
            {
                //AudioData = new NativeArray<float>(2048, Allocator.Persistent),
                amplitude = authoring.amplitude,
                frequency = authoring.frequency,

                //KeyBuffer = new DynamicBuffer<KeyData>()

        });
            //ComponentType.fixedArray

        }
    }
}