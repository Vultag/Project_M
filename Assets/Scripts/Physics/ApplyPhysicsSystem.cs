using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;


///NEEDS TO BE IN A FIXED UPDATE ?

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderLast = true)]
public partial struct ApplyPhysicsSystem : ISystem
{

    private Vector2 gravity;

    void OnCreate(ref SystemState state)
    {

        gravity = new Vector2(0f, -9.81f);


    }

    void OnStartRunning(ref SystemState state)
    {

    }


    void OnUpdate(ref SystemState state)
    {

        foreach (var (body, shape, trans) in SystemAPI.Query<RefRW<PhyBodyData>, RefRW<CircleShapeData>, RefRW<LocalTransform>>())
        {

            body.ValueRW.Velocity += body.ValueRO.Force;
            body.ValueRW.Force = Vector2.zero;

            shape.ValueRW.Position += body.ValueRO.Velocity;

            ///specify linear dampening in physics component
            body.ValueRW.Velocity -= (body.ValueRO.Velocity*0.01f);

            //apply trasform
            trans.ValueRW.Position = new Vector3(shape.ValueRO.Position.x, shape.ValueRO.Position.y, trans.ValueRW.Position.z);


        }

    }


}
