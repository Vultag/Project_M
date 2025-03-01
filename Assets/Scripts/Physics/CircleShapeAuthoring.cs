using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

//move to the phy body file ?
public struct CircleShapeData : IComponentData
{
    public Vector2 Position;
    public Quaternion Rotation;
    public Vector2 PreviousPosition;
    public Quaternion PreviousRotation;
    public float radius;
    public PhysicsUtilities.CollisionLayer collisionLayer;

}