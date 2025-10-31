using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Unity.Entities;


public class ClientHUDManager : MonoBehaviour
{
    
    private TextField _nameField;
    private bool _inFocus = false;
    public GameObject HUDGameObject;
    public string message;
    
    
    void Awake()
    {
        Instance = this;
    }
    
    public static ClientHUDManager Instance { get; private set; }
    
    void OnEnable()
    {
        // getting UI element.
        var root = HUDGameObject.GetComponent<UIDocument>().rootVisualElement;
        _nameField = root.Q<TextField>("UserInputField");
        Debug.Log(_nameField);
        
        // turn off by default.
        TurnOff();
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (_inFocus)
                TurnOff();
            else
                TurnOn();
        }
    }
    
    private void TurnOn()
    {
        // show cursor, unlock cursor.
        _inFocus = true;
        _nameField.SetEnabled(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void TurnOff()
    {
        // hide cursor, lock cursor.
        _inFocus = false;
        _nameField.SetEnabled(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        message = _nameField.value;
        _nameField.value = string.Empty;
    }
}
