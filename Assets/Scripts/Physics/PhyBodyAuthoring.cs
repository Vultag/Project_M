using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.VisualScripting;
using System;



#if UNITY_EDITOR
using UnityEditor;
#endif


public struct PhyBodyData : IComponentData
{
    //moved to shape
    //public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Force;
    public float Mass;

    // 0 = circle ; 1 = square;
    //public int ShapeType;

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

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new PhyBodyData
            {

                //Position = Vector2.zero,
                Mass = authoring.Mass,

            });

            switch (authoring.shapeType) 
            { 
                case ShapeType.Circle:
                    AddComponent(entity, new CircleShapeData
                    {
                        radius = authoring.radius,
                        collisionLayer = authoring.collisionLayer
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

        private void OnEnable()
        {
            radius = serializedObject.FindProperty("radius");
            dimentions = serializedObject.FindProperty("dimentions");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();


            PhyBodyAuthoring phyBody = (PhyBodyAuthoring)target;

            if (phyBody.shapeType == ShapeType.Circle)
            {
                EditorGUILayout.PropertyField(radius);
                //Debug.Log("test");
                //Handles.DrawSolidDisc(target.GameObject().transform.position, Vector3.up, phyBody.radius);

            }
            if (phyBody.shapeType == ShapeType.Box)
            {
                EditorGUILayout.PropertyField(dimentions);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
    #endregion





}