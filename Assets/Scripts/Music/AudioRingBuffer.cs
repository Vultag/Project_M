using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct KeyData
{
    public float frequency;
    public float OCS1phase;
    public float OCS2phase;
    public float delta;
    /* at 0, key is pressed. != 0, the key is released */
    public float amplitudeAtRelease;

}
public struct KeysBuffer
{
    public NativeArray<float> keyFrenquecies;
    //public short KeyNumber;
    public NativeArray<short> KeyNumber;

}
public struct PlaybackKey
{
    //public float frequency;
    public Vector2 dir;
    public float time;
    public float lenght;

}
public struct PlaybackAudioBundle
{
    //make sure to dispose
    public NativeArray<PlaybackKey> PlaybackKeys;
    //public int PlaybackKeyStartIndex;
    public float PlaybackDuration;
    public bool IsLooping;
    // not needed ?
    //public bool IsPlaying;
}
public struct PlaybackAudioBundleContext
{
    public int PlaybackKeyStartIndex;
    public float PlaybackTime;
}


public class AudioRingBuffer<T>
{

    private KeysBuffer[] buffer;
    private int head;
    private int tail;
    private int bufferSize;
    //private readonly object lockObject = new();

    public AudioRingBuffer(int size)
    {
        bufferSize = size;
        buffer = new KeysBuffer[size];
        head = 0;
        tail = 0;
    }

    public bool IsFull
    {
        get
        {
            //lock (lockObject)
            {
                return (head + 1) % bufferSize == tail;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            //lock (lockObject)
            {
                return head == tail;
            }
        }
    }

    public int GetCount()
    {
        //lock (lockObject)
        {
            if (head >= tail)
                return head - tail;
            else
                return bufferSize - tail + head;
        }
    }
    public void InitializeBuffer(int size)
    {
        //lock (lockObject)
        {
            for (int i = 0; i < size; i++)
            {
                buffer[i] = (new KeysBuffer { keyFrenquecies = new NativeArray<float>(12, Allocator.Persistent), KeyNumber = new NativeArray<short>(1, Allocator.Persistent) });
            }
        }
    }
    public void ResetSize()
    {
       //lock (lockObject)
        {
            head = (head + 1) % bufferSize;
        }
    }

    public void Write(KeysBuffer item)
    {
        //lock (lockObject)
        {
            buffer[head].keyFrenquecies.CopyFrom(item.keyFrenquecies);
            buffer[head].KeyNumber.CopyFrom(item.KeyNumber);
            head = (head + 1) % bufferSize;
        }
    }

    public KeysBuffer Read()
    {
        //lock (lockObject)
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Tried to read when the Buffer is empty");
            }
            var item = buffer[tail];
            tail = (tail + 1) % bufferSize;
            return item;
        }
    }
    public KeysBuffer RecycleLastElement()
    {
        return buffer[(bufferSize+tail - 1) % bufferSize];
    }



}
