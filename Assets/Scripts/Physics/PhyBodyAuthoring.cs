using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.VisualScripting;
using System;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.UIElements;






#if UNITY_EDITOR
using UnityEditor;
#endif


public struct PhyBodyData : IComponentData
{
    public Vector2 Velocity;
    public Vector2 Force;
    public float AngularVelocity;
    public float AngularForce;
    public float LinearDamp;
    public float AngularDamp;

    public float Mass;
    public float InvMass;
    public float Restitution;
    public float Inertia;

    //?

    //ADD STATIC BOOl FOR WALLS

}

public struct CircleShapeData : IComponentData
{
    public float radius;
}
public struct ShapeData : IComponentData
{
    public Vector2 Position;
    public float Rotation; /// stored in degree for now
    public Vector2 PreviousPosition;
    public float PreviousRotation;
    public PhysicsUtilities.CollisionLayer collisionLayer;
    public ShapeType shapeType;

    public PhysicsUtilities.CollisionLayer dynamicsLayer;

    public bool IsTrigger;

}

public enum ShapeType // *
{
    Circle,
    Box,
}

public class PhyBodyAuthoring : MonoBehaviour
{

    public ShapeType shapeType;
    [SerializeField, HideInInspector]
    public Vector2 dimentions;
    [SerializeField,HideInInspector]
    public float radius;

    public bool IsStaticBody = false;
    [SerializeField, HideInInspector]
    public float Mass = 1;
    [Range(0.0f, 1f)]
    public float restitution = 0f;

    public bool IsTrigger = false;
    [SerializeField, HideInInspector]
    public TriggerType triggerType;


    public PhysicsUtilities.CollisionLayer collisionLayer;
    public PhysicsUtilities.CollisionLayer dynamicsLayer;

    private void OnDrawGizmos()
    {
        if(shapeType == ShapeType.Circle)
            Gizmos.DrawWireSphere(this.transform.position,radius);
        if (shapeType == ShapeType.Box)
        {
            Vector3 position = transform.position;
            Quaternion rotation = Quaternion.Euler(0, 0, transform.eulerAngles.z); // Only Z rotation for 2D
            Vector3 scale = new Vector3(dimentions.x, dimentions.y, 1f);

            // Save the old matrix
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Apply the new matrix (position * rotation * scale)
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            // Draw cube centered at origin, it will be transformed by the matrix
            Gizmos.DrawWireCube(Vector3.zero, scale);

            // Restore the old matrix
            Gizmos.matrix = oldMatrix;
        }
    }


    class PhyBodyBaker : Baker<PhyBodyAuthoring>
    {
        public override void Bake(PhyBodyAuthoring authoring)
        {

            float mass = authoring.IsStaticBody ? 0: authoring.Mass;
            float inertia = 0;

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            //Entity entityManualOverride = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent(entity, new LocalTransform
            {
                Position = new float3(0, 0, 0),  // Set the initial position
                Rotation = Quaternion.Euler(0, 0, authoring.transform.eulerAngles.z),  // Set the initial rotation
                Scale = 1      // Set the initial scale
            });

         
            if(authoring.IsTrigger)
            {
                AddComponent(entity, new TriggerData
                {
                    triggerType = authoring.triggerType
                });
            }
     
            AddComponent(entity, new TreeInsersionData{IsStaticBody = authoring.IsStaticBody });

            AddComponent(entity, new ShapeData
                {
                Position = authoring.transform.position,
                collisionLayer = authoring.collisionLayer,
                shapeType = authoring.shapeType,
                Rotation = authoring.transform.eulerAngles.z,
                dynamicsLayer = authoring.dynamicsLayer,
                IsTrigger = authoring.IsTrigger
            });

            switch (authoring.shapeType) 
            { 
                case ShapeType.Circle:
                    AddComponent(entity, new CircleShapeData
                    {
                        radius = authoring.radius,
                    });
                    inertia = 0.5f * mass * authoring.radius * authoring.radius;
                    break;
                case ShapeType.Box:
                    AddComponent(entity, new BoxShapeData
                    {
                        dimentions = authoring.dimentions,
                    });
                    inertia = (1f / 12f) * mass * (authoring.dimentions.x * authoring.dimentions.x + authoring.dimentions.y * authoring.dimentions.y);
                    break;

            }
            if (authoring.dynamicsLayer != 0)
            {
                //Debug.Log(inertia);
                AddComponent(entity, new PhyBodyData
                {
                    Mass = mass,
                    InvMass = mass>0? 1/mass: 0,
                    Inertia = inertia,
                    LinearDamp = 0.015f,
                    AngularDamp = 0.05f,
                    Restitution = authoring.restitution
                });
            }

        }
    }

    #region EDITOR
#if UNITY_EDITOR
    //[CustomEditor(typeof(PhyBodyAuthoring)), CanEditMultipleObjects]
    [CustomEditor(typeof(PhyBodyAuthoring))]
    public class PhyBodyEditor : Editor
    {
        SerializedProperty radius;
        SerializedProperty dimentions;
        SerializedProperty triggerType;
        SerializedProperty mass;

        private void OnEnable()
        {
            radius = serializedObject.FindProperty("radius");
            dimentions = serializedObject.FindProperty("dimentions");
            triggerType = serializedObject.FindProperty("triggerType");
            mass = serializedObject.FindProperty("Mass");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();


            PhyBodyAuthoring phyBody = (PhyBodyAuthoring)target;

            if (phyBody.shapeType == ShapeType.Circle)
            {
                EditorGUILayout.PropertyField(radius);
            }
            if (phyBody.shapeType == ShapeType.Box)
            {
                EditorGUILayout.PropertyField(dimentions);
            }
            if(phyBody.IsTrigger)
            {
                EditorGUILayout.PropertyField(triggerType);
            }
            if(!phyBody.IsStaticBody)
            {
                EditorGUILayout.PropertyField(mass);
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
    #endregion





}