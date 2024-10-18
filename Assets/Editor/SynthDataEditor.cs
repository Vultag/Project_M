using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynthAuthoring))]
public class SynthDataEditor : Editor
{

    SynthAuthoring SynthComponent;

    private void OnEnable()
    {
        SynthComponent = (SynthAuthoring)target;
    }

    //public override void OnInspectorGUI()
    //{

    //    //SynthAuthoring SynthComponent = (SynthAuthoring)target;

    //    SynthComponent.amplitude = EditorGUILayout.FloatField("amplitude", SynthComponent.amplitude);
    //    SynthComponent.frequency = EditorGUILayout.FloatField("frequency", SynthComponent.frequency);

    //    EditorGUILayout.LabelField("ADSR Envelope", EditorStyles.boldLabel);

    //    SynthComponent.ADSR.Attack = EditorGUILayout.FloatField("Attack", SynthComponent.ADSR.Attack);
    //    SynthComponent.ADSR.Decay = EditorGUILayout.FloatField("Decay", SynthComponent.ADSR.Decay);
    //    SynthComponent.ADSR.Sustain = EditorGUILayout.Slider("Sustain", SynthComponent.ADSR.Sustain, 0f, 1f);
    //    SynthComponent.ADSR.Release = EditorGUILayout.FloatField("Release", SynthComponent.ADSR.Release);


    //    if (GUI.changed)
    //    {
    //        EditorUtility.SetDirty(SynthComponent);
    //    }

    //}
}