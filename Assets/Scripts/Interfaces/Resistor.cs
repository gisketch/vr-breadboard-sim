using UnityEngine;
using System.Collections;
using System.Security.Authentication.ExtendedProtection;

public class Resistor : MonoBehaviour
{
    private Node firstPin;
    private Node secondPin;
    private Node pivotPin;
    
    [SerializeField] private MeshRenderer meshRenderer;
    
    public void Initialize(string pin1, string pin2, Transform reference)
    {
        if (meshRenderer == null)
        {
            Debug.LogError("Resistor object needs a MeshRenderer component!");
        }
        
        firstPin = FindNodeRecursively(reference, pin1);
        secondPin = FindNodeRecursively(reference, pin2);
        
        if (firstPin == null || secondPin == null)
        {
            Debug.LogError($"Could not find nodes for resistor: {pin1}, {pin2}");
            return;
        }

        // if first pin name has letter A, then use firstPin, else use secondPin
        pivotPin = firstPin.name.Contains("A") ? firstPin : secondPin;


        //Set transform
        Vector3 pin1LocalPos = reference.InverseTransformPoint(pivotPin.transform.position);

        transform.localPosition = pin1LocalPos;
    }
    
    private Node FindNodeRecursively(Transform parent, string nodeName)
    {
        // First check if the direct child has this name
        Transform child = parent.Find(nodeName);
        if (child != null)
            return child.GetComponent<Node>();

        // If not found, search through all children recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            Node foundNode = FindNodeRecursively(parent.GetChild(i), nodeName);
            if (foundNode != null)
                return foundNode;
        }

        return null;
    }
}