using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlaybackContainer : MonoBehaviour
{

    public PlaybackAudioBundle playbackAudioBundle;
    public MusicSheetData musicSheet;

    [HideInInspector]
    public GameObject associatedPlaybackItem;

    /// <summary>
    /// Remplace with unique image to distiguish recordings
    /// </summary>
    [HideInInspector]
    public Color associatedItemColor;

}
