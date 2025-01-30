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
        //ElementsInMesure = new float[4] { 1, 10, 0, 0 };
        ///////NoteElements = new float[12] { 1, 1, 1, 1, 1, 4, 4, 4, 4, 4, 4, 4 };
        //NotesSpriteIdx = new float[12] { 20, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
        //NotesHeight = new float[12] { 4, 6, 4, 6.25f, 4, 6, 6, 6.25f, 4, 6.75f, 6, 6.75f };

        /// CREATES THE ARRAY FOR THE MAIN PROGRAM TOO !
        ElementsInMesure = new float[4] { 1, 0, 0, 0 };
        //NoteElements = new float[48];
        NotesSpriteIdx = new float[48];
        NotesHeight = new float[48];
        NotesHeight[0] = 4f;
        NotesSpriteIdx[0] = 20;
        //NotesHeight[1] = 4.25f;
        //NotesSpriteIdx[1] = 13.25f;
        //NotesHeight[2] = 4f;
        //NotesSpriteIdx[2] = 13.5f;
        //NotesHeight[3] = 4f;
        //NotesSpriteIdx[3] = 13;
        //NotesHeight[4] = 4f;
        //NotesSpriteIdx[4] = -13;
        //NotesHeight[5] = 4f;
        //NotesSpriteIdx[5] = -13;
        //NotesHeight[6] = 4f;
        //NotesSpriteIdx[6] = -13;

        //for (int i = 0; i < NoteElements.Length; i++)
        //{
        //    NoteElements[i] = 1.0f;
        //}
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
            //material.SetFloatArray("NoteElements", NoteElements);
            material.SetFloatArray("NotesSpriteIdx", NotesSpriteIdx);
            material.SetFloatArray("NotesHeight", NotesHeight);
        }
    }

}