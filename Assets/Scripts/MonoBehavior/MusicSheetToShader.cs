using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class MusicSheetToShader : MonoBehaviour
{

    [SerializeField]
    private GameObject MusicSheetGB;
    [SerializeField]
    private Material MusicSheetMaterial;

    /// <summary>
    /// TESTING DATA
    /// </summary>
    float mesureNumber = 4;
    float[] ElementsInMesure = new float[4] { 1, 5, 2, 1 };
    float[] NoteElements = new float[9] { 4, 0.5f, 0.5f, 1, 1, 1, 2, 2, 4 };
    float[] NotesSpriteIdx = new float[9] { 0, 1, 1, 1, 1, 1, 1, 1, 1 };
    float[] NotesHeight = new float[9] { 4, 0, 1, 2, 3, 4, 5, 6, 7 };


    void Start()
    {

        MusicSheetMaterial.SetFloat("mesureNumber", mesureNumber);
        MusicSheetMaterial.SetFloatArray("ElementsInMesure", ElementsInMesure);
        MusicSheetMaterial.SetFloatArray("NoteElements", NoteElements);
        MusicSheetMaterial.SetFloatArray("NotesSpriteIdx", NotesSpriteIdx);
        MusicSheetMaterial.SetFloatArray("NotesHeight", NotesHeight);

        MusicSheetGB.SetActive(true);


    }

    private void LateUpdate()
    {

    }

}
