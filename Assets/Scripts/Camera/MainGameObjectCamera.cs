

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameObjectCamera : MonoBehaviour
{
    public static Camera Instance;

    void Awake()
    {
        // also hide and lock cursor...
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Instance = GetComponent<UnityEngine.Camera>();
    }
}