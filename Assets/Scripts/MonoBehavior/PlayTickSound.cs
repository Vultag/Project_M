using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class PlayTickSound : MonoBehaviour
{
    private AudioSource tick;
    void Start()
    {
        tick = GetComponent<AudioSource>();
    }

    public void Play_Tick()
    {
        tick.Play();
    }
}
