using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PlayerHealthBar : MonoBehaviour
{
    private EntityManager entityManager;
    private EntityQuery Player_query;

    private Transform healthBarTarget;
    private void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));
        healthBarTarget = this.transform.GetChild(0).transform;
    }
    private void LateUpdate()
    {
        var playerE = Player_query.GetSingletonEntity();
        var playerTrans = entityManager.GetComponentData<LocalToWorld>(playerE);
        var health = entityManager.GetComponentData<HealthData>(playerE).HP;
        float3 newHealthScale = healthBarTarget.localScale;
        newHealthScale.x = health/10f;
        healthBarTarget.localScale = newHealthScale;
        this.transform.position = playerTrans.Position;
    }
}
