using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode] // This attribute makes the script execute in edit mode
public class MusicSheetDataTester : MonoBehaviour
{

    private float mesureNumber;
    private float[] ElementsInMesure;
    private float[] NoteElements;
    private float[] NotesSpriteIdx;
    private float[] NotesHeight;

    [SerializeField]
    private Material material; // Reference to the material using the shader


    private void OnValidate()
    {
        mesureNumber = 2;
        //ElementsInMesure = new float[4] { 1, 4, 0, 0 };
        //NoteElements = new float[9] { 4, 1, 1, 1, 1, 4, 4, 4, 4 };
        //NotesSpriteIdx = new float[9] { 0, 1, 2, 3, 4, 1, 1, 1, 1 };
        //NotesHeight = new float[9] { 4, 4, 4, 4, 4, 99, 99, 99, 99 };
        ElementsInMesure = new float[4] { 1, 0, 0, 0 };
        NoteElements = new float[48];
        NotesSpriteIdx = new float[48];
        NotesHeight = new float[48];
        NotesHeight[0] = 4;
        NotesSpriteIdx[0] = 20;
        for (int i = 0; i < NoteElements.Length; i++)
        {
            NoteElements[i] = 1.0f;
        }
        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {
        if (material != null)
        {
            //Debug.Log("Setting material properties");
            // Update the material properties whenever the script is enabled or validated in the editor
            material.SetFloat("mesureNumber", mesureNumber);
            material.SetFloatArray("ElementsInMesure", ElementsInMesure);
            material.SetFloatArray("NoteElements", NoteElements);
            material.SetFloatArray("NotesSpriteIdx", NotesSpriteIdx);
            material.SetFloatArray("NotesHeight", NotesHeight);
        }
    }

}