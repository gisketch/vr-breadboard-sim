using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Tester : MonoBehaviour
{
    void Start()
    {
        
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Find all BreadboardController in scene and print in Debug all their breadboardState
            BreadboardController[] controllers = FindObjectsOfType<BreadboardController>();
            
            Debug.Log($"Found {controllers.Length} BreadboardController instances");
            
            foreach (BreadboardController controller in controllers)
            {
                Debug.Log($"Breadboard '{controller.name}' state: {controller.breadboardState}");
            }
        }
    }
}
