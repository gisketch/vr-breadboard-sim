using UnityEngine;
using System.Collections;

public class Resistor : MonoBehaviour
{
    private Node firstPin;
    private Node secondPin;
    
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
        
        // Position the resistor between the two nodes
        Vector3 midpoint = (firstPin.transform.position + secondPin.transform.position) / 2f;
        transform.position = midpoint;
        
        // Rotate to align with the connection
        Vector3 direction = (secondPin.transform.position - firstPin.transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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