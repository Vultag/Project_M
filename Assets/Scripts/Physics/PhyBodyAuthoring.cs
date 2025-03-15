using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.VisualScripting;
using System;
using Unity.Transforms;
using Unity.Mathematics;





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

    //ADD STATIC BOOl FOR WALLS

}

public enum ShapeType // *
{
    Circle,
    Box,
}

public class PhyBodyAuthoring : MonoBehaviour
{

    public float Mass;
    public ShapeType shapeType;
    [SerializeField, HideInInspector]
    public Vector2 dimentions;
    [SerializeField,HideInInspector]
    public float radius;

    public bool HasDynamics = true;

    public bool IsTrigger = false;
    [SerializeField, HideInInspector]
    public TriggerType triggerType;


    public PhysicsUtilities.CollisionLayer collisionLayer;

    private void OnDrawGizmos()
    {
        if(shapeType == ShapeType.Circle)
            Gizmos.DrawWireSphere(this.transform.position,radius);
        if (shapeType == ShapeType.Box)
            Gizmos.DrawWireCube(this.transform.position, new Vector3(dimentions.x,dimentions.y,1));
    }


    class PhyBodyBaker : Baker<PhyBodyAuthoring>
    {
        public override void Bake(PhyBodyAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            //Entity entityManualOverride = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent(entity, new LocalTransform
            {
                Position = new float3(0, 0, 0),  // Set the initial position
                Rotation = quaternion.identity,  // Set the initial rotation
                Scale = 1      // Set the initial scale
            });

            if (authoring.HasDynamics)
            {
                AddComponent(entity, new PhyBodyData
                {
                    Mass = authoring.Mass,
                    LinearDamp = 0.015f,
                    AngularDamp = 0.05f
                });
            }
            if(authoring.IsTrigger)
            {
                AddComponent(entity, new TriggerData
                {
                    triggerType = authoring.triggerType
                });
            }
     
            AddComponent(entity, new TreeInsersionTag{});

            switch (authoring.shapeType) 
            { 
                case ShapeType.Circle:
                    AddComponent(entity, new CircleShapeData
                    {
                        radius = authoring.radius,
                        collisionLayer = authoring.collisionLayer,
                        Rotation = Quaternion.identity,
                        HasDynamics = authoring.HasDynamics,
                        IsTrigger = authoring.IsTrigger
                    });
                    break;
                case ShapeType.Box:
                    AddComponent(entity, new BoxShapeData
                    {
                        dimentions = authoring.dimentions,


                    });
                    break;

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

        private void OnEnable()
        {
            radius = serializedObject.FindProperty("radius");
            dimentions = serializedObject.FindProperty("dimentions");
            triggerType = serializedObject.FindProperty("triggerType");
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


            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
    #endregion





}