using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.EventSystems.EventTrigger;


[UpdateInGroup(typeof(GameSimulationSystemGroup))]
//[UpdateAfter(typeof(PhyResolutionSystem))]
public partial class MachineDrumSystem : SystemBase
{

    /// Useless ? active synth index always 0 ?
    //public static short activeSynthEntityindex;

    /// Moved to input manager
    //public float BeatCooldown;
    //private float BeatProximity;
    //private float BeatProximityThreshold;

    EntityCommandBuffer ECB;

    public static bool PadJustPressed;
    public static bool PadJustReleased;

    private int PlayedKeyIndex;
    //public Vector2 mousepos;
    private bool IsShooting;

    //private bool IsRecording;
    //private NativeList<PlaybackKey> PlayKeys;

    //private Vector2 mouseDirection;
    public static Vector2 GideReferenceDirection;
    /// 0 = legato inactive;
    private float activeLegatoFz;

    //private MaterialPropertyBlock _propertyBlock;

    private EntityQuery damageEventEntityQuery;
    private EntityQuery ActiveDMachineEntityQuery;

    private EntityQuery StunManagerEntityQuery;

    protected override void OnCreate()
    {
        /// should work but AudioLayoutStorage.activeSynthIdx is changed before ActiveSynthTag removal
        //RequireForUpdate<ActiveSynthTag>();
        //AudioLayoutStorage.activeSynthIdx = -1;

        RequireForUpdate<ActiveDMachineTag>();

        damageEventEntityQuery = EntityManager.CreateEntityQuery(typeof(GlobalDamageEvent));
        ActiveDMachineEntityQuery = EntityManager.CreateEntityQuery(typeof(ActiveDMachineTag));

        StunManagerEntityQuery = EntityManager.CreateEntityQuery(typeof(StunEffectData));

    }


    protected override void OnUpdate()
    {
        ECB = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        /// OPTI ? allocation each frame
        NativeList<(ushort, float)> requests = new NativeList<(ushort,float)>(3, Allocator.Temp);

        //Debug.Break();
        //var mainWeaponEntity = ActiveDMachineEntityQuery.GetSingletonEntity();

        var MainWeapon = SystemAPI.GetSingletonEntity<ControledWeaponTag>();
        var Wtrans = EntityManager.GetComponentData<LocalToWorld>(MainWeapon);
        var trans = EntityManager.GetComponentData<LocalTransform>(MainWeapon);

        var parentEntity = SystemAPI.GetComponent<Parent>(MainWeapon).Value;
        var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);

        Vector2 mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);

        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);
        //Debug.LogWarning(BeatProximity);

        //bool CanPlayKey = (InputManager.CanPressKey && !UIInput.MouseOverUI);

        Entity activeDMachineEntity = ActiveDMachineEntityQuery.GetSingletonEntity();
        DrumMachineData newDMachineData = EntityManager.GetComponentData<DrumMachineData>(activeDMachineEntity);

        Vector2 mouseDirection = mousepos - new Vector2(Wtrans.Position.x, Wtrans.Position.y);
        ///Vector2 localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;

        /// Do game logic and store audio call in a nativestack ?

        if (PadJustPressed && !UIInput.MouseOverUI)
        {
            //Debug.Log(newDMachineData.InstrumentAddOrder[0]);

            //store it ?
            int numberOfInstruments = math.countbits((int)newDMachineData.machineDrumContent)+1;  // Counts the number of set bits

            float mouseRadian = PhysicsUtilities.DirectionToRadians(mouseDirection);
            float side = Mathf.Sign(mouseRadian);
            mouseRadian = mouseRadian * side + (side * 0.5f + 0.5f) * Mathf.PI;
            float normalizedRadian = (mouseRadian / Mathf.PI)*0.5f;

            int PadIdx = Mathf.FloorToInt(normalizedRadian * numberOfInstruments);

            int InstrumentsIdx = newDMachineData.InstrumentAddOrder[PadIdx];

            //Debug.Log("play");
            //Debug.Log(newDMachineData.InstrumentAddOrder[0]);


            switch (InstrumentsIdx)
            {
                case 0:
                    //Debug.Log("BaseDrum");

                    //Debug.DrawLine(Wtrans.Position, Wtrans.Position+new float3(0,3f,0),Color.red,5);
                    var KickColList = PhysicsCalls.CircleOverlapNode(new CircleShapeData { 
                        Position = Wtrans.Position.xy, 
                        radius = 3f, 
                        collisionLayer = PhysicsUtilities.CollisionLayer.MonsterLayer 
                    });

                    for (int i = 0; i < KickColList.Length; i++)
                    {
                        var entity = KickColList[i];
                        var newPhyBody = EntityManager.GetComponentData<PhyBodyData>(entity);
                        /// Redondant -> OPTI
                        var kickDirLenght = (EntityManager.GetComponentData<CircleShapeData>(entity).Position - (Vector2)Wtrans.Position.xy);
                        /// establish how much the effect denpends on the distance from the explosion
                        float explosionConsentrationFactor = 0.45f;

                        /// Disfonctional way to modify physics mid-frame ? overrides and gets overriden untill applyied
                        newPhyBody.Force += ((kickDirLenght.normalized * (1-explosionConsentrationFactor)) + (kickDirLenght.normalized * explosionConsentrationFactor * Mathf.Min(1,1- kickDirLenght.magnitude/3))) * 0.22f;
                        ECB.SetComponent<PhyBodyData>(entity,newPhyBody);
                    }

                    KickColList.Dispose();
                    requests.Add((0,0));
                    break;
                case 1:
                    //Debug.Log("SnareDrum");

                    Vector2 snareDir = Vector2.up;

                    var SnareColList = PhysicsCalls.CircleOverlapNode(new CircleShapeData
                    {
                        Position = Wtrans.Position.xy,
                        radius = 3.5f,
                        collisionLayer = PhysicsUtilities.CollisionLayer.MonsterLayer
                    });
                    if(SnareColList.Length>0)
                    {
                        var mainSlotPos = EntityManager.GetComponentData<LocalToWorld>(SnareColList[0]).Value.Translation();

                        snareDir = ((Vector2)(mainSlotPos.xy - Wtrans.Position.xy)).normalized;
                     
                        var SnareBaguetteColList = PhysicsCalls.CircleOverlapNode(new CircleShapeData
                        {
                            Position = (Vector2)Wtrans.Position.xy + (snareDir * 3.5f),
                            radius = 0.25f,
                            collisionLayer = PhysicsUtilities.CollisionLayer.MonsterLayer
                        });

                        var damageEventEntity = damageEventEntityQuery.GetSingletonEntity();

                        //Debug.DrawRay((Vector2)Wtrans.Position.xy + (snareDir * 3.5f), new Vector2(0, 0.25f), Color.red, 0.5f);
                        //Debug.Log(SnareBaguetteColList.Length);
                        for (int i = 0; i < SnareBaguetteColList.Length; i++)
                        {
                            SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity).Add(new GlobalDamageEvent
                            {
                                Target = SnareBaguetteColList[i],
                                DamageValue = 6
                            });
                        }

                        SnareBaguetteColList.Dispose();

                        ////var playerInverseRad = Quaternion.Inverse(parentTransform.Value.Rotation()).eulerAngles.z * Mathf.Deg2Rad;
                        //////Debug.Log(playerInverseRad);
                        /////// rotate to be relative to player
                        ////float cos = Mathf.Cos(playerInverseRad);
                        ////float sin = Mathf.Sin(playerInverseRad);
                        ////snareDir = new Vector2(
                        ////    snareDir.x * cos - snareDir.y * sin,
                        ////    snareDir.x * sin + snareDir.y * cos
                        ////);

                    }
                    SnareColList.Dispose();

                    ///float localRadian = Mathf.Abs(Mathf.Atan2(snareDir.x, snareDir.y)/Mathf.PI + 1)*0.5f;
                    float radian = Mathf.Abs(Mathf.Atan2(snareDir.x, snareDir.y)/Mathf.PI + 1)*0.5f;

                    requests.Add((1, radian));
                    break;
                case 2:
                    //Debug.Log("HighHat");

                    var HitHatColList = PhysicsCalls.CircleOverlapNode(new CircleShapeData
                    {
                        Position = Wtrans.Position.xy,
                        radius = 3.5f,
                        collisionLayer = PhysicsUtilities.CollisionLayer.MonsterLayer
                    });

                    for (int i = 0; i < HitHatColList.Length; i++)
                    {
                        var entity = HitHatColList[i];
                        EffectUtils.ApplyStun(StunManagerEntityQuery.GetSingletonEntity(), EntityManager, ECB, entity,2);

                    }
                    HitHatColList.Dispose();

                    requests.Add((2, 0));
                    break;
            }

            PadJustPressed = false;
        }


        AudioManager.Instance.PlayRequestedDMachineSounds(requests);
        //Debug.Log(requests[0].Item1);
        UIManager.Instance.PlayRequestedDMachineEffects(requests);

        requests.Dispose();

    }
}
