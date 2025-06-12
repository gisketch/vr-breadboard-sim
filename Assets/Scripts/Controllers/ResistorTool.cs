using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class ResistorTool : MonoBehaviour, IComponentTool
{
    private List<Node> highlightedNodes = new List<Node>();
    
    public void Activate()
    {
        GameManager.Instance.SetInteractionMessage("Select a node in column A or J for resistor placement");
    }
    
    public void UpdateColors()
    {
        // Resistors don't have color variations
    }
    
    void Update()
    {
        if (InputManager.Instance.GetSecondaryButtonDown())
        {
            ClearResistor();
        }
    }
    
    void ClearResistor()
    {
        ClearHighlights();
    }
    
    public void OnNodeHover(Node node)
    {
        ClearHighlights();
        
        if (node == null || node.isOccupied)
        {
            if (node != null) node.SetHighlightColor(Node.HighlightColor.Red);
            return;
        }
        
        // Check if node is in column A or J
        Match match = Regex.Match(node.name, @"(\d+)([A-J])");
        if (!match.Success)
        {
            node.SetHighlightColor(Node.HighlightColor.Red);
            return;
        }
        
        string column = match.Groups[2].Value;
        if (column != "A" && column != "J")
        {
            node.SetHighlightColor(Node.HighlightColor.Red);
            return;
        }
        
        // Get the rail node to connect to
        string railNodeName = GetRailNodeName(node.name);
        Node railNode = FindRailNode(node, railNodeName);
        
        if (railNode != null && !railNode.isOccupied)
        {
            // Highlight both nodes in green
            node.SetHighlightColor(Node.HighlightColor.Green);
            highlightedNodes.Add(node);
            
            railNode.SetHighlightColor(Node.HighlightColor.Green);
            railNode.overrideHighlight = true;
            highlightedNodes.Add(railNode);
        }
        else
        {
            node.SetHighlightColor(Node.HighlightColor.Red);
        }
    }
    
    public void OnNodeClick(Node node)
    {
        if (node == null || node.isOccupied)
        {
            return;
        }
        
        // Check if node is in column A or J
        Match match = Regex.Match(node.name, @"(\d+)([A-J])");
        if (!match.Success)
        {
            return;
        }
        
        string column = match.Groups[2].Value;
        if (column != "A" && column != "J")
        {
            return;
        }
        
        // Get the rail node to connect to
        string railNodeName = GetRailNodeName(node.name);
        Node railNode = FindRailNode(node, railNodeName);
        
        if (railNode != null && !railNode.isOccupied)
        {
            // Place the resistor
            string pin1 = node.name;
            string pin2 = railNode.name;
            
            BreadboardStateUtils.Instance.AddResistor(pin1, pin2);
            
            // Mark nodes as occupied
            node.isOccupied = true;
            railNode.isOccupied = true;
            
            GameManager.Instance.ClearInteractionMessage();
            ClearResistor();
        }
    }
    
    private string GetRailNodeName(string nodeName)
    {
        Match match = Regex.Match(nodeName, @"(\d+)([A-J])");
        if (match.Success)
        {
            string rowNumber = match.Groups[1].Value;
            string column = match.Groups[2].Value;
            
            if (column == "A")
            {
                return rowNumber + "GND"; // Left rail ground (e.g., "1GND", "15GND")
            }
            else if (column == "J")
            {
                // For right rail, we need to map to the 31-60 range
                int leftRow = int.Parse(rowNumber);
                int rightRow = leftRow + 30; // Map 1-30 to 31-60
                return rightRow + "PWR"; // Right rail power (e.g., "31PWR", "45PWR")
            }
        }
        return null;
    }
    
    private Node FindRailNode(Node startNode, string railNodeName)
    {
        if (railNodeName == null) return null;
        
        // Navigate to the breadboard root
        Transform breadboard = startNode.transform.parent?.parent;
        if (breadboard == null) return null;
        
        // Determine which rail to search based on the node name
        Transform railParent = null;
        if (railNodeName.Contains("GND"))
        {
            railParent = breadboard.Find("PowerRailLeft");
        }
        else if (railNodeName.Contains("PWR"))
        {
            railParent = breadboard.Find("PowerRailRight");
        }
        
        if (railParent != null)
        {
            Transform railTransform = railParent.Find(railNodeName);
            if (railTransform != null)
            {
                return railTransform.GetComponent<Node>();
            }
        }
        
        return null;
    }
    
    private void ClearHighlights()
    {
        foreach (Node node in highlightedNodes)
        {
            if (node != null)
            {
                node.overrideHighlight = false;
                node.SetHighlightColor(Node.HighlightColor.Default);
            }
        }
        highlightedNodes.Clear();
    }
    
    public void Deactivate()
    {
        ClearHighlights();
        GameManager.Instance.ClearInteractionMessage();
    }
}