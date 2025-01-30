using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class MusicSheetToShader : MonoBehaviour
{

    //not needed ?
    [SerializeField]
    private MusicSheetDataStoraging musicSheetStorage;
    [SerializeField]
    private Material MusicSheetMaterial;


    void Start()
    {

    }

    private void LateUpdate()
    {

        MusicSheetData activeSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet;

        MusicSheetMaterial.SetFloat("mesureNumber", activeSheet.mesureNumber);
        MusicSheetMaterial.SetFloatArray("ElementsInMesure", activeSheet.ElementsInMesure.ToArray());
        MusicSheetMaterial.SetFloatArray("NotesSpriteIdx", activeSheet.NotesSpriteIdx.ToArray());
        /// unused ?
        //MusicSheetMaterial.SetFloatArray("NoteElements", activeSheet.NoteElements.ToArray());
        MusicSheetMaterial.SetFloatArray("NotesHeight", activeSheet.NotesHeight.ToArray());

    }

}
