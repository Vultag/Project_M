using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIPlaybacksHolder : MonoBehaviour
{
    /// single for now -> array ? list ? stack ?
    public PlaybackHolder PBholder1;
    public PlaybackHolder PBholder2;
    public PlaybackHolder PBholder3;


    void Start()
    {
        
    }

    public void _AddSynthPlaybackContainer(PlaybackAudioBundle newAudioBundle,MusicSheetData newSheetData, short synthIdx)
    {
        switch (synthIdx)
        {
            case 0:
                PBholder1._AddContainer(newAudioBundle, newSheetData);
                break;
            case 1:
                PBholder2._AddContainer(newAudioBundle, newSheetData);
                break;
            case 2:
                PBholder3._AddContainer(newAudioBundle, newSheetData);
                break;

        }

    }




}
