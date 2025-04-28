
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


///NEEDS TO BE IN A FIXED UPDATE ?

public partial struct ApplyPhysicsSystem : ISystem
{

    private Vector2 gravity;

    public void OnCreate(ref SystemState state)
    {

        gravity = new Vector2(0f, -9.81f);


    }

    public void OnUpdate(ref SystemState state)
    {

        foreach (var (body, shape, trans) in SystemAPI.Query<RefRW<PhyBodyData>, RefRW<ShapeData>, RefRW<LocalTransform>>())
        {

            Vector2 lienarAcceleration = body.ValueRO.Force * body.ValueRO.InvMass;
            ///??
            ///Vector2 angularAcceleration = body.ValueRO.Force / body.ValueRO.Mass;

            body.ValueRW.Velocity += lienarAcceleration;
            body.ValueRW.Force = Vector2.zero;

            body.ValueRW.AngularVelocity += body.ValueRO.AngularForce;
            body.ValueRW.AngularForce = 0;

            ///specify linear dampening in physics component
            //float dampingFactor = math.exp(-0.05f);
            //body.ValueRW.Velocity *= dampingFactor;

            shape.ValueRW.PreviousPosition = shape.ValueRO.Position;
            shape.ValueRW.PreviousRotation = shape.ValueRO.Rotation;

            shape.ValueRW.Position += body.ValueRO.Velocity;
            shape.ValueRW.Rotation += body.ValueRO.AngularVelocity * Mathf.Rad2Deg;

            body.ValueRW.Velocity -= (body.ValueRO.Velocity * body.ValueRO.LinearDamp);
            body.ValueRW.AngularVelocity -= (body.ValueRO.AngularVelocity * body.ValueRO.AngularDamp);


        }

    }


}
