using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PlaybackData : IComponentData, IEnableableComponent
{
    public int KeysPlayed;
    /// <summary>
    /// MOVE TO PlaybackIdentifyer ?
    /// </summary>
    public int PlaybackIndex;

    public int PlaybackKeyIndex;
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