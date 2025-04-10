
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

        foreach (var (body, shape, trans) in SystemAPI.Query<RefRW<PhyBodyData>, RefRW<CircleShapeData>, RefRW<LocalTransform>>())
        {

            body.ValueRW.Velocity += body.ValueRO.Force;
            body.ValueRW.Force = Vector2.zero;

            body.ValueRW.AngularVelocity += body.ValueRO.AngularForce;
            body.ValueRW.AngularForce = 0;

            ///specify linear dampening in physics component
            //float dampingFactor = math.exp(-0.05f);
            //body.ValueRW.Velocity *= dampingFactor;
            body.ValueRW.Velocity -= (body.ValueRO.Velocity * body.ValueRO.LinearDamp);
            body.ValueRW.AngularVelocity -= (body.ValueRO.AngularVelocity * body.ValueRO.AngularDamp);

            shape.ValueRW.PreviousPosition = shape.ValueRO.Position;
            shape.ValueRW.PreviousRotation = shape.ValueRO.Rotation;

            shape.ValueRW.Position += body.ValueRO.Velocity;
            shape.ValueRW.Rotation = math.mul(shape.ValueRO.Rotation, quaternion.RotateZ(body.ValueRO.AngularVelocity));

            //trans.ValueRW.Position = new Vector3(shape.ValueRO.Position.x, shape.ValueRO.Position.y, trans.ValueRW.Position.z);
            //trans.ValueRW.Rotation = new Quaternion(shape.ValueRO.Rotation.x, shape.ValueRO.Rotation.y, shape.ValueRO.Rotation.z, shape.ValueRO.Rotation.w);


        }

    }


}
