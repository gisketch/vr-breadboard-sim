using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDTool : MonoBehaviour, IComponentTool
{
    //ALWAYS VERTICAL, ALWAYS PLACING ANODE FIRST
    [SerializeField] private Node anodeSlot;
    [SerializeField] private Node cathodeSlot;

    private bool isPlacingCathode = false;

    private List<Node> placableNodes = new List<Node>();

    public void Activate()
    {

    }

    public void UpdateColors()
    {
    }

    void Update()
    {
        if(isPlacingCathode)
        {
            if(InputManager.Instance.GetSecondaryButtonDown())
            {
                ClearLED();
            }
        }
    }

    void ClearLED()
    {
        isPlacingCathode = false;
        anodeSlot = null;
        cathodeSlot = null;

        // Reset Highlight and overrideHighlight for placableNodes
        foreach (Node node in placableNodes)
        {
            if(node != null)
            {
                node.overrideHighlight = false;
                node.SetHighlightColor(Node.HighlightColor.Default);
            }
        }

        placableNodes.Clear();
    }

    public void OnNodeHover(Node node)
    {
        if (anodeSlot == null && cathodeSlot == null)
        {
            bool hasSpace = HasVerticalSpace(node);  // Call helper function

            if (node.name.Contains("PWR") || node.name.Contains("GND") || node.isOccupied || !hasSpace)
            {
                node.SetHighlightColor(Node.HighlightColor.Red);
            }
            else
            {
                node.SetHighlightColor(Node.HighlightColor.Green);
            }
        }
    }

    public void OnNodeClick(Node node)
    {
        if(node.isOccupied)
        {
            return;
        }

        bool hasSpace = HasVerticalSpace(node);

        if (anodeSlot == null && cathodeSlot == null)
        {
            if (node.name.Contains("PWR") || node.name.Contains("GND") || !hasSpace)
            {
                node.SetHighlightColor(Node.HighlightColor.Red);  // Highlight in red to indicate it's not valid
                return; // Return if conditions are not met, don't set anodeSlot
            }

            anodeSlot = node;
            isPlacingCathode = true;

            //Highlight above and below colors
            PlaceableNodeCheck(node);

        }

        if (isPlacingCathode)
        {
            if (placableNodes.Contains(node))
            {
                cathodeSlot = node;

                //ADD LED TO STATE UTILS
                BreadboardStateUtils.Instance.AddLED(anodeSlot.name, cathodeSlot.name, ComponentManager.Instance.currentColor.ToString());
                anodeSlot.isOccupied = true;
                cathodeSlot.isOccupied = true;

                //Reset Highlights
                ClearPlacableNodeHighLights();
                ClearLED();
            }
        }
    }

    private void PlaceableNodeCheck(Node node)
    {
        // Get the current node's name
        string nodeName = node.name;

        if (nodeName.Contains("PWR") || nodeName.Contains("GND")) return;

        // Extract the numerical part of the name (e.g., "3" from "3E")
        string prefixString = nodeName.Substring(0, nodeName.Length - 1);
        int prefix = 0;

        if (int.TryParse(prefixString, out prefix))
        {
            //Calculate top and below prefixes
            int topPrefix = prefix - 1;
            int belowPrefix = prefix + 1;

            //Build the potential node names
            string topNodeName = topPrefix.ToString() + nodeName.Substring(nodeName.Length - 1);
            string belowNodeName = belowPrefix.ToString() + nodeName.Substring(nodeName.Length - 1);

            //Find the Top Node
            if (topPrefix > 0 && topPrefix <= 30)
            {
                Transform parent = node.transform.parent;  // Get the parent transform
                if (parent != null)
                {
                    Transform topNodeTransform = parent.Find(topNodeName);  // Find using the full name
                    if (topNodeTransform != null)
                    {
                        Node topNode = topNodeTransform.GetComponent<Node>();
                        if (topNode != null && !topNode.isOccupied)
                        {
                            placableNodes.Add(topNode);
                            topNode.overrideHighlight = true;
                            topNode.SetHighlightColor(Node.HighlightColor.Blue);
                        }
                    }
                }
            }

            //Find the Bottom Node
            if (belowPrefix > 0 && belowPrefix <= 30)
            {
                Transform parent = node.transform.parent;
                if (parent != null)
                {
                    Transform belowNodeTransform = parent.Find(belowNodeName);
                    if (belowNodeTransform != null)
                    {
                        Node belowNode = belowNodeTransform.GetComponent<Node>();
                        if (belowNode != null && !belowNode.isOccupied)
                        {
                            placableNodes.Add(belowNode);
                            belowNode.overrideHighlight = true;
                            belowNode.SetHighlightColor(Node.HighlightColor.Blue);
                        }
                    }
                }
            }

        }
        else
        {
            Debug.LogError("Invalid node name format: " + nodeName);
        }
    }

    private void ClearPlacableNodeHighLights()
    {
        // Reset Highlight and overrideHighlight for placableNodes
        foreach (Node node in placableNodes)
        {
            if(node != null)
            {
                node.overrideHighlight = false;
                node.SetHighlightColor(Node.HighlightColor.Default);
            }
        }
    }

    // Helper method to check if a node has space above or below
    private bool HasVerticalSpace(Node node)
    {
        string nodeName = node.name;
        string prefixString = nodeName.Substring(0, nodeName.Length - 1);
        int prefix = 0;

        if (nodeName.Contains("PWR") || nodeName.Contains("GND")) return false;

        if (int.TryParse(prefixString, out prefix))
        {
            int topPrefix = prefix - 1;
            int belowPrefix = prefix + 1;

            string topNodeName = topPrefix.ToString() + nodeName.Substring(nodeName.Length - 1);
            string belowNodeName = belowPrefix.ToString() + nodeName.Substring(nodeName.Length - 1);

            Transform parent = node.transform.parent;
            bool hasTopSpace = false;
            bool hasBottomSpace = false;

            if (parent != null)
            {
                if (topPrefix > 0 && topPrefix <= 30)
                {
                    Transform topNodeTransform = parent.Find(topNodeName);
                    if (topNodeTransform != null)
                    {
                        Node topNode = topNodeTransform.GetComponent<Node>();
                        if (topNode != null && !topNode.isOccupied)
                        {
                            hasTopSpace = true;
                        }
                    }
                }

                if (belowPrefix > 0 && belowPrefix <= 30)
                {
                    Transform belowNodeTransform = parent.Find(belowNodeName);
                    if (belowNodeTransform != null)
                    {
                        Node belowNode = belowNodeTransform.GetComponent<Node>();
                        if (belowNode != null && !belowNode.isOccupied)
                        {
                            hasBottomSpace = true;
                        }
                    }
                }
            }

            return hasTopSpace || hasBottomSpace;
        }
        else
        {
            Debug.LogError("Invalid node name format: " + nodeName);
            return false; // Or some default value that makes sense for your situation
        }

    }

    public void Deactivate()
    {
        ClearPlacableNodeHighLights();
        ClearLED();
        placableNodes.Clear();
    }
}
