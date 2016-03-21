using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public enum InputType
    {
        MouseAndKeyboard,
        Leap
    };

    public InputType CurrentInput;
    public Transform LeapPointerTransform;
    public Text DebugUi;
    public Dictionary<string, KeyCode> KeyMapping = new Dictionary<string, KeyCode>()
    {
        {"PlacePoint", KeyCode.Mouse0},
        {"RemovePoint", KeyCode.Mouse1},
        {"ConfirmBridge", KeyCode.Space}
    };
    public Dictionary<string, Func<bool>> LeapMapping = new Dictionary<string, Func<bool>>()
    {
        {"PlacePoint", () => false},
        {"RemovePoint", () => false},
        {"ConfirmBridge", () => false}
    }; 

    public Vector2 GetPointedInput()
    {
        if (CurrentInput == InputType.Leap && LeapPointerTransform == null)
            throw new LeapIOException("Leap input chosen, but not supplied to InputManager");

        switch (CurrentInput)
        {
            case InputType.MouseAndKeyboard:
                DebugUi.text = string.Format("Mouse input chosen ({0}, {1})", Input.mousePosition.x,
                    Input.mousePosition.y);
                return new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            case InputType.Leap:
                var transformedPosition = Camera.main.WorldToScreenPoint(LeapPointerTransform.position);
                DebugUi.text = string.Format("Leap input chosen ({0}, {1})", transformedPosition.x, transformedPosition.y);
                return new Vector2(transformedPosition.x, transformedPosition.y);
            default:
                DebugUi.text = "No input chosen!";
                return Vector2.zero; 
        }
    }

    public bool PlacePoint()
    {
        switch (CurrentInput)
        {
            case InputType.MouseAndKeyboard:
                DebugUi.text = string.Format("Keyboard PlacePoint => {0}", Input.GetKey(KeyMapping["PlacePoint"]));
                return Input.GetKey(KeyMapping["PlacePoint"]);
            case InputType.Leap:
                DebugUi.text = string.Format("Leap PlacePoint => {0}", LeapMapping["PlacePoint"]);
                return LeapMapping["PlacePoint"].Invoke();
            default:
                return false;
        }
    }

    public bool RemovePoint()
    {
        switch (CurrentInput)
        {
            case InputType.MouseAndKeyboard:
                DebugUi.text = string.Format("Keyboard RemovePoint => {0}", Input.GetKey(KeyMapping["RemovePoint"]));
                return Input.GetKey(KeyMapping["RemovePoint"]);
            case InputType.Leap:
                DebugUi.text = string.Format("Leap RemovePoint => {0}", LeapMapping["RemovePoint"]);
                return LeapMapping["RemovePoint"].Invoke();
            default:
                return false;
        }
    }

    public bool ConfirmBridge()
    {
        switch (CurrentInput)
        {
            case InputType.MouseAndKeyboard:
                DebugUi.text = string.Format("Keyboard ConfirmBridge => {0}", Input.GetKey(KeyMapping["ConfirmBridge"]));
                return Input.GetKey(KeyMapping["ConfirmBridge"]);
            case InputType.Leap:
                return LeapMapping["ConfirmBridge"].Invoke();
            default:
                return false;
        }
    }
}

public class LeapIOException : Exception
{
    public LeapIOException ()
    {
       
    }

    public LeapIOException(string message)
        : base(message)
    {
        
    }
}
