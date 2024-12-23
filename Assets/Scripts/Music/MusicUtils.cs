using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MusicNamespace
{

    public struct MusicUtils
    {

        //should not be here?
        static public float BPM = 60f;
        /// move away ?
        static public float time = 0;

        private readonly static int[] intervals = { 2, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 2, 2, 1}; // Whole and half steps for Ionian mode over two octave

        #region KEY FREQUENCIES

        const float C1 = 32.7032f;
        const float Cs1 = 34.6478f;
        const float D1 = 36.7081f;
        const float Ds1 = 38.8909f;
        const float E1 = 41.2034f;
        const float F1 = 43.6535f;
        const float Fs1 = 46.2493f;
        const float G1 = 48.9994f;
        const float Gs1 = 51.9131f;
        const float A1 = 55.0f;
        const float As1 = 58.2705f;
        const float B1 = 61.7354f;
        const float C2 = 65.4064f;
        const float Cs2 = 69.2957f;
        const float D2 = 73.4162f;
        const float Ds2 = 77.7817f;
        const float E2 = 82.4069f;
        const float F2 = 87.3071f;
        const float Fs2 = 92.4986f;
        const float G2 = 97.9989f;
        const float Gs2 = 103.826f;
        const float A2 = 110.0f;
        const float As2 = 116.541f;
        const float B2 = 123.471f;
        const float C3 = 130.813f;
        const float Cs3 = 138.591f;
        const float D3 = 146.832f;
        const float Ds3 = 155.563f;
        const float E3 = 164.814f;
        const float F3 = 174.614f;
        const float Fs3 = 184.997f;
        const float G3 = 195.998f;
        const float Gs3 = 207.652f;
        const float A3 = 220.0f;
        const float As3 = 233.082f;
        const float B3 = 246.942f;
        const float C4 = 261.626f; //MIDDLE C
        const float Cs4 = 277.183f;
        const float D4 = 293.665f;
        const float Ds4 = 311.127f;
        const float E4 = 329.628f;
        const float F4 = 349.228f;
        const float Fs4 = 369.994f;
        const float G4 = 391.995f;
        const float Gs4 = 415.305f;
        const float A4 = 440.0f;
        const float As4 = 466.164f;
        const float B4 = 493.883f;
        const float C5 = 523.251f;
        const float Cs5 = 554.365f;
        const float D5 = 587.33f;
        const float Ds5 = 622.254f;
        const float E5 = 659.255f;
        const float F5 = 698.456f;
        const float Fs5 = 739.989f;
        const float G5 = 783.991f;
        const float Gs5 = 830.609f;
        const float A5 = 880.0f;
        const float As5 = 932.328f;
        const float B5 = 987.767f;
        const float C6 = 1046.5f;
        const float Cs6 = 1108.73f;
        const float D6 = 1174.66f;
        const float Ds6 = 1244.51f;
        const float E6 = 1318.51f;
        const float F6 = 1396.91f;
        const float Fs6 = 1479.98f;
        const float G6 = 1567.98f;
        const float Gs6 = 1661.22f;
        const float A6 = 1760.0f;
        const float As6 = 1864.66f;
        const float B6 = 1975.53f;
        const float C7 = 2093.0f;
        #endregion


        // Array of frequencies for each musical key
        static readonly float[] keyFrequencies = {
            C1, Cs1, D1, Ds1, E1, F1, Fs1, G1, Gs1, A1, As1, B1, C2, Cs2,
            D2, Ds2, E2, F2, Fs2, G2, Gs2, A2, As2, B2, C3, Cs3, D3, Ds3,
            E3, F3, Fs3, G3, Gs3, A3, As3, B3, C4, Cs4, D4, Ds4, E4, F4,
            Fs4, G4, Gs4, A4, As4, B4, C5, Cs5, D5, Ds5, E5, F5, Fs5, G5,
            Gs5, A5, As5, B5, C6, Cs6, D6, Ds6, E6, F6, Fs6, G6, Gs6, A6,
            As6, B6, C7
        };

        #region MODES
        //counting from the steps from the Tonic
        static readonly int[] Ionian =
        {
            2,4,5,7,9,11,12
        };
        static readonly int[] Dorian =
        {
            2,3,5,7,9,10,12
        };
        static readonly int[] Phrygian =
        {
            1,3,5,7,8,10,12
        };
        static readonly int[] Lydian =
        {
            2,4,6,7,9,11,12
        };
        static readonly int[] Myxolidian =
        {
            2,4,5,7,9,10,12
        };
        static readonly int[] Aeolian =
        {
            2,3,5,7,8,10,12
        };
        static readonly int[] Locrian =
        {
            1,3,5,6,8,10,12
        };
        #endregion

        public enum MusicalMode
        {
            Ionian,
            Dorian,
            Phrygian,
            Lydian,
            Mixolydian,
            Aeolian,
            Locrian
        }

        const int NoteNumInCircle = 12;
        readonly static float[] OctaveRadianWeights = 
        { 
            (4f / 43f) * Mathf.PI, 
            (3f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
            (3f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
            (3f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
            (3f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
            (3f / 43f) * Mathf.PI,
            (4f / 43f) * Mathf.PI,
        };

        /*Could be optimized considering we are fetching an ordered list -> early exit OPTI*/
        //remove??
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

        public static int radiansToNote(float radians)
        {
            int currentIndex = 0;
            for (; radians > OctaveRadianWeights[currentIndex];)
            {
                radians -= OctaveRadianWeights[currentIndex];
                currentIndex++;
            }
            return currentIndex;

        }

        /// mode is not used in the curernt setup -> implement later
        public static float noteToFrequency(int localnote,MusicalMode mode)
        {
            int tempoctave = 3 * 12;

            return keyFrequencies[localnote + tempoctave]; //+ ocatave

        }
        public static float DirectionToFrequency(Vector2 dir)
        {
            return noteToFrequency(radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(dir))),WeaponSystem.mode);
        }
        /// Center a direction according to the key splitting of the circle
        /// OPTI
        public static Vector2 CenterDirection(Vector2 dir)
        {

            float RadDir = PhysicsUtilities.DirectionToRadians(dir);
            float newRadDir = OctaveRadianWeights[0] * 0.5f;
            short idx = 0;
            while (Mathf.Abs(RadDir) > OctaveRadianWeights[idx])
            {
                RadDir -= OctaveRadianWeights[idx]* Mathf.Sign(RadDir);
                newRadDir += OctaveRadianWeights[idx]*0.5f + OctaveRadianWeights[idx+1]*0.5f;
                idx++;
            }

            //newRadDir = (Mathf.Round(newRadDir / (Mathf.PI / NoteNumInCircle))) * (Mathf.PI / NoteNumInCircle);

            return PhysicsUtilities.RadianToDirection(newRadDir * Mathf.Sign(RadDir));

        }

        public static float Sin(float phase)
        {
            return Mathf.Sin(phase * 2 * Mathf.PI);
        }
        public static float Saw(float phase)
        {
            //??
            return (((phase)%1)-0.5f)*2f;
        }
        public static float Square(float phase, float pulseWidth)
        {
            //return (Mathf.Round((phase) % 1)-0.5f) *2f ;
            return ((phase) % 1)> pulseWidth ? 1f:-1f;
        }

    }


}

