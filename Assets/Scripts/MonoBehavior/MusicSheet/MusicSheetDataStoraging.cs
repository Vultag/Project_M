using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

/// <summary>
/// RENAME FILE SheetDataStoraging
/// </summary>

public struct MusicSheetData
{
    public int mesureNumber;
    public NativeArray<float> ElementsInMesure;
    //public NativeArray<float> NoteElements;
    public NativeArray<float> NotesSpriteIdx;
    public NativeArray<float> NotesHeight;

    public static MusicSheetData CreateDefault()
    {
        var musicSheetData = new MusicSheetData
        {
            mesureNumber = 2,
            ElementsInMesure = new NativeArray<float>(4,Allocator.Persistent),
            //NoteElements = new NativeArray<float>(48, Allocator.Persistent),
            NotesSpriteIdx = new NativeArray<float>(48, Allocator.Persistent),
            NotesHeight = new NativeArray<float>(48, Allocator.Persistent),
        };

        musicSheetData.ElementsInMesure[0] = 1;

        for (int i = 0; i < 48; i++)
        {
            musicSheetData.NotesHeight[i] = 99f;
            //musicSheetData.NoteElements[i] = 4;
        }
        musicSheetData.NotesHeight[0] = 4f;
        musicSheetData.NotesSpriteIdx[0] = 20;

        return musicSheetData;  

    }

    public void _Dispose()
    {
        ElementsInMesure.Dispose();
        NotesSpriteIdx.Dispose();
        NotesHeight.Dispose();
    }
}
public struct DrumPadSheetData
{
    public ushort mesureNumber;
    /// <summary>
    /// Flatened 2D array for every possible subbeat DM element
    /// </summary>
    public NativeArray<bool> PadCheck;

    public static DrumPadSheetData CreateDefault()
    {
        var DrumPadSheetData = new DrumPadSheetData
        {
            mesureNumber = 1,
            // 192
            PadCheck = new NativeArray<bool>(4*4*2/* max mesure nb*/*6/*max num of intruments*/,Allocator.Persistent),
        };

        return DrumPadSheetData;
    }

    public void _Dispose()
    {
        PadCheck.Dispose();
    }
}

