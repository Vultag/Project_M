using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif


public struct WeaponCollectibleData : IComponentData
{
    public WeaponClass weaponClass;
    public WeaponType weaponType;
}
public struct DrumMachineCollectibleData : IComponentData
{

}


public enum CollectibleType:byte
{
    WeaponItem,
    DrumMachineItem
}

/// <summary>
/// DO custom add logic depending on the collectible type
/// </summary>

public class CollectibleAuthoring : MonoBehaviour
{
    /// <summary>
    /// to do
    /// </summary>
    public CollectibleType collectibleType;

    [SerializeField, HideInInspector]
    public WeaponClass weaponClass;
    [SerializeField, HideInInspector]
    public WeaponType weaponType;

    class CollectibleBaker : Baker<CollectibleAuthoring>
    {
        public override void Bake(CollectibleAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            switch (authoring.collectibleType)
            {
                case CollectibleType.WeaponItem:
                    AddComponent(entity, new WeaponCollectibleData
                    {
                        weaponClass = authoring.weaponClass,
                        weaponType = authoring.weaponType
                    });
                    break;
                case CollectibleType.DrumMachineItem:
                    AddComponent(entity, new DrumMachineCollectibleData
                    {

                    });
                    break;

            }



        }
    }


    #region EDITOR
#if UNITY_EDITOR
    [CustomEditor(typeof(CollectibleAuthoring))]
    public class CollectibleEditor : Editor
    {
        SerializedProperty weaponClass;
        SerializedProperty weaponType;

        private void OnEnable()
        {
            weaponClass = serializedObject.FindProperty("weaponClass");
            weaponType = serializedObject.FindProperty("weaponType");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();


            CollectibleAuthoring collectible = (CollectibleAuthoring)target;

            if (collectible.collectibleType == CollectibleType.WeaponItem)
            {
                EditorGUILayout.PropertyField(weaponClass);
                EditorGUILayout.PropertyField(weaponType);
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    #endregion

}