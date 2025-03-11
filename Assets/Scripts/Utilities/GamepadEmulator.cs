using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GamepadEmulator : MonoBehaviour
{
    public string joystickButtonName = "joystick button 0";

    void Update()
    {
        if (Input.GetButtonDown(joystickButtonName))
        {
            // Simulate mouse click at current cursor position
            EmulateClickAtPosition(Input.mousePosition);
        }
    }

    void EmulateClickAtPosition(Vector2 position)
    {
        // Create pointer event data
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = position;
        pointerData.button = PointerEventData.InputButton.Left;

        // Raycast using the EventSystem to find UI elements
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Process the results
        if (results.Count > 0)
        {
            GameObject target = results[0].gameObject;

            // Trigger pointer events in sequence for a complete click interaction
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);

            // Optional: Play click sound or add haptic feedback
            Debug.Log("Joystick click emulated on: " + target.name);
        }
    }
}