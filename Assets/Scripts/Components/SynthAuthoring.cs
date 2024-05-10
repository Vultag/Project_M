
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
//using UnityEditor.PackageManager;
using UnityEngine;

unsafe
public struct SynthData : IComponentData
{
    //public NativeArray<float> AudioData;
    //public fixed float AudioData[2048];

    public float amplitude;
    public float frequency;
    public char Input_key;

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

            //authoring.audioprefab = Instantiate(authoring.AudioOutputprefab);

            //authoring.audioprefab.GetComponent<AudioGenerator>().WeaponSynthEntity = entity;


            AddComponent(entity, new SynthData
            {
                //AudioData = new NativeArray<float>(2048, Allocator.Persistent),
                amplitude = authoring.amplitude,
                frequency = authoring.frequency

            });
            //ComponentType.fixedArray

        }
    }
}