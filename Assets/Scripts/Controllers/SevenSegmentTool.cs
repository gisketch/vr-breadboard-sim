using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class SevenSegmentTool : MonoBehaviour, IComponentTool
{
    //ALWAYS B TOP LEFT, ALWAYS PLACING ANODE FIRST
    [SerializeField] private Node nodeB;

    private bool isAllowed = false;


    public void Activate()
    {
    }

    public void UpdateColors()
    {
        //... do nothing for seven segment
    }

    void Update()
    {

    }

    private bool CheckNodeAvailability(Node node)
    {
        if (node == null) return false; // Node doesn't exist.
        return !node.isOccupied;
    }

   private Node GetNodeOffset(Node startNode, int rowOffset, int columnOffset) {

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
        return  node.name.EndsWith("PWR") || node.name.EndsWith("GND") || node.name.EndsWith("A") || node.name.EndsWith("E") || node.name.EndsWith("F") || node.name.EndsWith("G") || node.name.EndsWith("H") || node.name.EndsWith("I") || node.name.EndsWith("J") || node.name.StartsWith("27") || node.name.StartsWith("28") || node.name.StartsWith("29") || node.name.StartsWith("30");
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
            //Calculates the other nodes based of of B
            Node nodeA = GetNodeOffset(node, 1, 0);
            Node nodeGnd1 = GetNodeOffset(node, 2, 0);
            Node nodeF = GetNodeOffset(node, 3, 0);
            Node nodeG = GetNodeOffset(node, 4, 0);

            Node nodeDP = GetNodeOffset(node, 0, 5);
            Node nodeC = GetNodeOffset(node, 1, 5);
            Node nodeGnd2 = GetNodeOffset(node, 2, 5);
            Node nodeD = GetNodeOffset(node, 3, 5);
            Node nodeE = GetNodeOffset(node, 4, 5);


            // Check if all required nodes are available.  Use the helper function.
            isAllowed = CheckNodeAvailability(node) &&
                        CheckNodeAvailability(nodeA) &&
                        CheckNodeAvailability(nodeGnd1) &&
                        CheckNodeAvailability(nodeF) &&
                        CheckNodeAvailability(nodeG) &&
                        CheckNodeAvailability(nodeDP) &&
                        CheckNodeAvailability(nodeC) &&
                        CheckNodeAvailability(nodeGnd2) &&
                        CheckNodeAvailability(nodeD) &&
                        CheckNodeAvailability(nodeE);

            //SET HIGHLIGHT FOR ALL NODES DEPENDING ON  AVAILABILITY
            SetNodeHighlightAndTrack(node, Node.HighlightColor.Green);
            SetNodeHighlightAndTrack(nodeA, CheckNodeAvailability(nodeA) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeC, CheckNodeAvailability(nodeC) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeD, CheckNodeAvailability(nodeD) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeE, CheckNodeAvailability(nodeE) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeF, CheckNodeAvailability(nodeF) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeG, CheckNodeAvailability(nodeG) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeDP, CheckNodeAvailability(nodeDP) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeGnd1, CheckNodeAvailability(nodeGnd1) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
            SetNodeHighlightAndTrack(nodeGnd2, CheckNodeAvailability(nodeGnd2) ? Node.HighlightColor.Green : Node.HighlightColor.Red);
        } else
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
            BreadboardStateUtils.Instance.AddSevenSegment(node.name);
            isAllowed = false;
        } else {
            Debug.Log("Cannot place: placement not allowed, or node is null.");
        }
    }

    public void Deactivate()
    {
    }
}
