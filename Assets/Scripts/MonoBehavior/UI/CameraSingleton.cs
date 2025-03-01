
using UnityEngine;


public class CameraSingleton : MonoBehaviour
{
    public static CameraSingleton Instance { get; private set; }
    public Camera MainCamera { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MainCamera = Camera.main;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}