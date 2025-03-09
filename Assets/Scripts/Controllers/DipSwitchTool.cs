using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class DipSwitchTool : MonoBehaviour, IComponentTool
{
    [SerializeField] private Node pin1;

    private bool isAllowed = false;


    public void Activate()
    {
    }

    public void UpdateColors()
    {

    }

    void Update()
    {

    }

    private bool CheckNodeAvailability(Node node)
    {
        if (node == null) return false; // Node doesn't exist.
        return !node.isOccupied;
    }

    private Node GetNodeOffset(Node startNode, int rowOffset, int columnOffset)
    {

        // Use regular expression to extract the number and letter
        Match match = Regex.Match(startNode.name, @"(\d+)([A-J])");

        if (match.Success)
        {
            int nodeNumber = int.Parse(match.Groups[1].Value);
            char nodeLetter = match.Groups[2].Value[0]; // Get the letter

            // Calculate the new row number and column character
            int newNumber = nodeNumber + rowOffset;
            char newLetter = (char)(nodeLetter + columnOffset);

            // Validate the new number and letter
            if (newNumber >= 1 && newNumber <= 30 && newLetter >= 'A' && newLetter <= 'J')
            {
                return startNode.GetNodeFromName(newNumber.ToString() + newLetter.ToString());
            }
            else
            {
                Debug.LogWarning("Calculated node coordinates are out of range: " + newNumber + newLetter);
                return null; // Indicate out of range
            }
        }
        else
        {
            Debug.LogError("Invalid node name format for offset calculation: " + startNode.name);
            return null;
        }
    }

    private bool isNodeRestricted(Node node)
    {
        if (node == null)
        {
            return true;

        }

        string nodeName = node.name;

        if (nodeName == null)
        {
            return true;
        }

        return !nodeName.EndsWith("E");
    }

    private List<Node> _highlightedNodes = new List<Node>();

    public void OnNodeHover(Node node)
    {
        ClearNodeHighlights(); // Clear previous highlights

        if (isNodeRestricted(node))
        {
            node.SetHighlightColor(Node.HighlightColor.Red);
            isAllowed = false;
            return;
        }

        if (node != null && !node.isOccupied)
        {
            //Calculates the other nodes based of of pin 1
            Node node2 = GetNodeOffset(node, 0, 1);



            // Check if all required nodes are available.  Use the helper function.
            isAllowed = CheckNodeAvailability(node) &&
                        CheckNodeAvailability(node2);

            //SET HIGHLIGHT FOR ALL NODES DEPENDING ON AVAILABILITY
            SetNodeHighlightAndTrack(node, Node.HighlightColor.Green);

            Node[] nodesToCheck = { node2 };

            foreach (Node nodeToCheck in nodesToCheck)
            {
                // Null check is important, especially if the node list isn't always fully populated.
                if (nodeToCheck != null)
                {
                    Node.HighlightColor highlightColor = CheckNodeAvailability(nodeToCheck) ? Node.HighlightColor.Green : Node.HighlightColor.Red;
                    SetNodeHighlightAndTrack(nodeToCheck, highlightColor);
                }
                else
                {
                    //Handle null;  Log an error, skip it, or take other appropriate action.
                    Debug.LogError("Null node encountered while setting highlights.  Ensure your node list is correctly populated.");
                }
            }
        }
        else
        {
            isAllowed = false;
        }

    }

    private void ClearNodeHighlights()
    {
        foreach (var node in _highlightedNodes)
        {
            if (node != null)
            {
                node.SetHighlightColor(Node.HighlightColor.Default);
            }
        }
        _highlightedNodes.Clear();
    }

    private void SetNodeHighlightAndTrack(Node node, Node.HighlightColor color)
    {
        if (node != null)
        {
            node.SetHighlightColor(color);
            _highlightedNodes.Add(node);
        }
    }

    public void OnNodeClick(Node node)
    {
        if (isAllowed && node != null)
        {
            //STATE!!
            BreadboardStateUtils.Instance.AddDipSwitch(node.name);
            isAllowed = false;
        }
        else
        {
            Debug.Log("Cannot place: placement not allowed, or node is null.");
        }
    }

    public void Deactivate()
    {
    }
}
