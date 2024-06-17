using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct NativeListUtils
{
    // Function to select elements from the first NativeList<(T,U)>
    public static NativeList<T> SelectFirst<T,U>(NativeList<(T,U)> list) where T : unmanaged where U : unmanaged
    {
        // Create a new NativeList<T> to store elements 
        NativeList<T> selectedList = new NativeList<T>(list.Length, Allocator.Temp);

        // Iterate through list1 and add elements to selectedList
        for (int i = 0; i < list.Length; i++)
        {
            selectedList.Add(list[i].Item1);
        }

        return selectedList;


    }
    // Function to select elements from the second NativeList<(T,U)>
    public static NativeList<U> SelectSecond<T, U>(NativeList<(T, U)> list) where T : unmanaged where U : unmanaged
    {
        // Create a new NativeList<T> to store elements 
        NativeList<U> selectedList = new NativeList<U>(list.Length, Allocator.Temp);

        // Iterate through list1 and add elements to selectedList
        for (int i = 0; i < list.Length; i++)
        {
            selectedList.Add(list[i].Item2);
        }

        return selectedList;

    }
    public static void QuickSort(NativeList<float> list, int low, int high)
    {
        if (low < high)
        {
            float pivot = list[high];
            int i = low - 1;
            for (int j = low; j < high; j++)
            {
                if (list[j] < pivot)
                {
                    i++;
                    float temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }
            }
            float temp1 = list[i + 1];
            list[i + 1] = list[high];
            list[high] = temp1;

            int pivotIndex = i + 1;

            QuickSort(list, low, pivotIndex - 1);
            QuickSort(list, pivotIndex + 1, high);
        }
    }
    public static void QuickSort<T>(NativeList<(T,float)> list, int low, int high) where T : unmanaged
    {
        if (low < high)
        {
            float pivot = list[high].Item2;
            int i = low - 1;
            for (int j = low; j < high; j++)
            {
                if (list[j].Item2 < pivot)
                {
                    i++;
                    (T, float) temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }
            }
            (T, float) temp1 = list[i + 1];
            list[i + 1] = list[high];
            list[high] = temp1;

            int pivotIndex = i + 1;

            QuickSort(list, low, pivotIndex - 1);
            QuickSort(list, pivotIndex + 1, high);
        }
    }


}
