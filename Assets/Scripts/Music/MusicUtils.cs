using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicNamespace
{

    public struct MusicUtils
    {

        //should not be here
        static public int BPM = 60;

        // Array of frequencies for each musical key
        static readonly float[] keyFrequencies = {
        32.7032f,  // C1
        34.6478f,  // C#1
        36.7081f,  // D1
        38.8909f,  // D#1
        41.2034f,  // E1
        43.6535f,  // F1
        46.2493f,  // F#1
        48.9994f,  // G1
        51.9131f,  // G#1
        55.0f,     // A1
        58.2705f,  // A#1
        61.7354f,  // B1
        65.4064f,  // C2
        69.2957f,  // C#2
        73.4162f,  // D2
        77.7817f,  // D#2
        82.4069f,  // E2
        87.3071f,  // F2
        92.4986f,  // F#2
        97.9989f,  // G2
        103.826f,  // G#2
        110.0f,    // A2
        116.541f,  // A#2
        123.471f,  // B2
        130.813f,  // C3
        138.591f,  // C#3
        146.832f,  // D3
        155.563f,  // D#3
        164.814f,  // E3
        174.614f,  // F3
        184.997f,  // F#3
        195.998f,  // G3
        207.652f,  // G#3
        220.0f,    // A3
        233.082f,  // A#3
        246.942f,  // B3
        261.626f,  // C4
        277.183f,  // C#4
        293.665f,  // D4
        311.127f,  // D#4
        329.628f,  // E4
        349.228f,  // F4
        369.994f,  // F#4
        391.995f,  // G4
        415.305f,  // G#4
        440.0f,    // A4
        466.164f,  // A#4
        493.883f,  // B4
        523.251f,  // C5
        554.365f,  // C#5
        587.33f,   // D5
        622.254f,  // D#5
        659.255f,  // E5
        698.456f,  // F5
        739.989f,  // F#5
        783.991f,  // G5
        830.609f,  // G#5
        880.0f,    // A5
        932.328f,  // A#5
        987.767f,  // B5
        1046.5f,   // C6
        1108.73f,  // C#6
        1174.66f,  // D6
        1244.51f,  // D#6
        1318.51f,  // E6
        1396.91f,  // F6
        1479.98f,  // F#6
        1567.98f,  // G6
        1661.22f,  // G#6
        1760.0f,   // A6
        1864.66f,  // A#6
        1975.53f,  // B6
        2093.0f,   // C7
    };

        /*Could be optimized considering we are fetching an ordered list -> early exit OPTI*/
        public static float getNearestKey(float value)
        {
            float Nearsetkey = keyFrequencies[0];
            float delta = Math.Abs(value - Nearsetkey);

            foreach (float key in keyFrequencies)
            {
                float difference = Math.Abs(value - key);
                if (difference < delta)
                {
                    Nearsetkey = key;
                    delta = difference;
                }
            }

            return Nearsetkey;
        }


    }


}

