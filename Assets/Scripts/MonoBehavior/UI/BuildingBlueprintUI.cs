using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.InputSystem.InputAction;

public class BuildingBlueprintUI : MonoBehaviour
{

    PlayerControls.ConstructionMapActions Construction_Controls;

    //private Entity preInstancedBuildingEntity;
    [HideInInspector]
    public BuildingInfo buildingInfo;

    /// Put in buildingInfo ?
    [HideInInspector]
    public CircleShapeData CurrentCircle;
    [HideInInspector]
    public BoxShapeData CurrentBox;

    private bool validBuildingPlacement;
    private bool tryToPlaceBuilding;

    public EndSimulationEntityCommandBufferSystem endSimulationECBSystem;
    public EntityQuery Core_query;
    public EntityQuery preInstancedTurretWeapon_query;

    void Awake()
    {
        Construction_Controls = InputManager.playerControls.ConstructionMap;
        Construction_Controls.TryPlaceBuilding.performed += OnTryPlaceBuilding;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        endSimulationECBSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        Core_query = entityManager.CreateEntityQuery(typeof(MCoreData));

        ////preInstancedTurretWeapon_query = entityManager.CreateEntityQuery(typeof(TurretWeaponTag), typeof(Disabled));

        Entity Core_entity = Core_query.GetSingletonEntity();
        var turretPrefab = entityManager.GetComponentData<MCoreData>(Core_entity).TurretPrefab;
        var weaponPrefab = entityManager.GetComponentData<TurretData>(turretPrefab).weaponPrefabEntity;

        Entity nextPreInstanced = entityManager.Instantiate(weaponPrefab);
        entityManager.AddComponent<Disabled>(nextPreInstanced);
    }
    private void OnEnable()
    {
        Construction_Controls.Enable();
    }
    private void OnDisable()
    {
        Construction_Controls.Disable();
    }

    void Update()
    {
        if (UIInput.MouseOverUI)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Vector2 mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);

        this.transform.position = mousepos;

        Color blueprintColor = Color.green;
        validBuildingPlacement = true;
        //BuildingPlacementpos = mousepos;

        if (PhysicsCalls.IsShapeOverlaping((float2)mousepos,CurrentCircle,PhysicsUtilities.CollisionLayer.DynamicObstacleLayer,entityManager))
        {
            blueprintColor = Color.yellow;
            validBuildingPlacement = false;
        }
        if (PhysicsCalls.IsShapeOverlaping((float2)mousepos, CurrentCircle, PhysicsUtilities.CollisionLayer.StaticObstacleLayer, entityManager))
        {
            blueprintColor = Color.red;
            validBuildingPlacement = false;
        }
        this.GetComponent<SpriteRenderer>().material.color = blueprintColor;

        if(tryToPlaceBuilding)
        {
            if (validBuildingPlacement)
            {
                PlaceBuilding(entityManager, mousepos, buildingInfo);
            }
            tryToPlaceBuilding = false;
            InputManager.playerControls.PlayerMap.Enable();
            this.gameObject.SetActive(false);
        }

    }

    private void OnTryPlaceBuilding(CallbackContext context)
    {
        tryToPlaceBuilding = true;
    }


    private void PlaceBuilding(EntityManager entityManager,Vector2 pos, BuildingInfo buildingInfo)
    {
        var ecb = endSimulationECBSystem.CreateCommandBuffer();

        Entity Core_entity = Core_query.GetSingletonEntity();
        var turretPrefab = entityManager.GetComponentData<MCoreData>(Core_entity).TurretPrefab;
        var weaponPrefab = entityManager.GetComponentData<TurretData>(turretPrefab).weaponPrefabEntity;

        /// instanciate turret base
        var turretLTW = LocalTransform.FromPosition(new float3(pos.x, pos.y, 0));

        Entity new_turret = ecb.Instantiate(turretPrefab);

        ecb.SetComponent(new_turret, turretLTW);

        /// permutate the pooledEntity and remplace it
        Entity new_turret_weapon = preInstancedTurretWeapon_query.GetSingletonEntity();
        ecb.RemoveComponent<Disabled>(new_turret_weapon);
        Entity nextPreInstanced = ecb.Instantiate(weaponPrefab);
        ecb.AddComponent<Disabled>(nextPreInstanced);

        ///AudioManager.TurretEquipmentEntities[buildingInfo.buildingIdx].Add(new_turret_weapon);

        // Parent weapon to turret
        ecb.AddComponent(new_turret_weapon, new Parent { Value = new_turret });

        switch (buildingInfo.equipmentCategory)
        {
            case EquipmentCategory.Weapon:
                switch (buildingInfo.weaponType)
                {
                    case WeaponType.Raygun:
                        ecb.AddComponent(new_turret_weapon, new RayData
                        {

                        });
                        break;
                    case WeaponType.Canon:
                        ecb.AddComponent(new_turret_weapon, new WeaponAmmoData
                        {
                            Damage = 6f,
                            Speed = 0.05f,
                            LifeTime = 3f,
                            penetrationCapacity = 1
                        });
                        break;
                }
                ecb.AddComponent(new_turret_weapon, new WeaponData
                {
                    WeaponIdx = buildingInfo.buildingIdx,
                    weaponClass = buildingInfo.weaponClass,
                    weaponType = buildingInfo.weaponType,
                });
                ecb.AddComponent(new_turret_weapon, new PlaybackData
                {
                    PlaybackIndex = buildingInfo.buildingIdx,
                });
                ///ecb.SetComponentEnabled<PlaybackData>(new_turret_weapon, false);
                ecb.AddBuffer<PlaybackSustainedKeyBufferData>(new_turret_weapon);
                ecb.AddBuffer<PlaybackReleasedKeyBufferData>(new_turret_weapon);
                break;
            case EquipmentCategory.DrumMachine:
                Debug.LogError("not implemented yet");
                break;
        }

   


    }

}

