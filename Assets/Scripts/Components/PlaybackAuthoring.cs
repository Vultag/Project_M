using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PlaybackData : IComponentData
{
    /// <summary>
    /// same as PlaybackElementIndex but used to catch up and actually trigger the elements ?
    /// redondant ?
    /// </summary>
    public int KeysPlayed;
    /// <summary>
    /// relative to weap/DM/... + container idx
    /// </summary>
    public int2 FullPlaybackIndex;
    /// <summary>
    /// Tracks element progression
    /// </summary>
    public int PlaybackElementIndex;
    public float PlaybackTime;
    //Vector2 GideReferenceDirection;
    //public float PlaybackDuration;

}

//public struct PlaybackIdentifyer : ISharedComponentData
//{

//}

//public class PlaybackAuthoring : MonoBehaviour
//{
//    //Vector2 OffsetFromPlayer;

//    class PlaybackBaker : Baker<PlaybackAuthoring>
//    {
//        public override void Bake(PlaybackAuthoring authoring)
//        {

//            Entity entity = GetEntity(TransformUsageFlags.None);

//            AddComponent(entity, new PlaybackData
//            {


//            });



//        }
//    }
//}