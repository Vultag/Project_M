using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class MusicSheetToShader : MonoBehaviour
{

    [SerializeField]
    private Material MusicSheetMaterial;


    void Start()
    {
        GetComponent<MusicSheetToShader>().enabled = false;
    }
    private void OnEnable()
    {
        this.GetComponent<Image>().material = MusicSheetMaterial;
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
