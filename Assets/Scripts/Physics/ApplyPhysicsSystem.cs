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

            //Debug.Log(shape.ValueRO.radius);

            //Vector2 new_force = body.ValueRO.Mass * gravity;
            //Vector2 new_vel = new_force / body.ValueRO.Mass;
            //Vector2 new_pos = new_vel * SystemAPI.Time.DeltaTime;

            //body.ValueRW.Force += body.ValueRO.Mass * gravity * SystemAPI.Time.DeltaTime;
            //body.ValueRW.Velocity += body.ValueRO.Force / body.ValueRO.Mass * SystemAPI.Time.DeltaTime;
            shape.ValueRW.Position += body.ValueRO.Velocity;

            ///specify linear dampening in physics component
            body.ValueRW.Velocity -= (body.ValueRO.Velocity*0.1f);
            //body.ValueRW.Velocity = Vector2.zero;

            //Debug.Log(body.ValueRO.Velocity);
            //body.ValueRW.Force = Vector2.zero;
            //Debug.Log(new_force / body.ValueRO.Mass);
            //Debug.Log(new_force / body.ValueRO.Mass * SystemAPI.Time.DeltaTime);

            //apply trasform
            trans.ValueRW.Position = new Vector3(shape.ValueRO.Position.x, shape.ValueRO.Position.y, trans.ValueRW.Position.z);
            //Debug.Log(body.ValueRO.Position);



        }

    }


}
