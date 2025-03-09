using System;
using UnityEngine;
using Mirror;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class BreadboardStateUtils : MonoBehaviour
{
    public static BreadboardStateUtils Instance { get; private set; }
    public BreadboardController myBreadboardController;

    private int wireCounter = 0;
    private int ledCounter = 0;
    private int sevenSegCounter = 0;
    private int icCounter = 0;

    private void Awake()
    {
        Instance = this;
    }

    //wire
    //startNode
    //endNode
    //color
    public void AddWire(string startNode, string endNode, string color)
    {
        try
        {
            // Get the current breadboard state JSON string
            string currentState = myBreadboardController.breadboardState;

            // Initialize with default state if empty
            if (string.IsNullOrEmpty(currentState))
                currentState = @"{ ""components"": {} }";


            // Parse the JSON structure
            JObject state = JObject.Parse(currentState);

            // Ensure components object exists
            JObject components = (JObject)state["components"] ?? new JObject();
            state["components"] = components;

            // Generate unique wire ID
            wireCounter++;
            string wireId = $"wire{wireCounter}";

            // Create new wire entry
            JObject wireEntry = new JObject
            {
                ["startNode"] = startNode,
                ["endNode"] = endNode,
                ["color"] = color
            };

            // Add wire to components
            components[wireId] = wireEntry;
            state["components"] = components;

            // Convert back to JSON string
            string updatedState = state.ToString(Newtonsoft.Json.Formatting.None);


            // Update the breadboard state
            myBreadboardController.CmdUpdateBreadboardState(updatedState);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding wire: {e.Message}");
        }
    }


    //led
    //anode
    //cathode
    //color
    public void AddLED(string anode, string cathode, string color)
    {
        try
        {
            // Get the current breadboard state JSON string
            string currentState = myBreadboardController.breadboardState;

            // Initialize with default state if empty
            if (string.IsNullOrEmpty(currentState))
                currentState = @"{ ""components"": {} }";


            // Parse the JSON structure
            JObject state = JObject.Parse(currentState);

            // Ensure components object exists
            JObject components = (JObject)state["components"] ?? new JObject();
            state["components"] = components;

            ledCounter++;
            string ledId = $"led{ledCounter}";

            // Create new wire entry
            JObject ledEntry = new JObject
            {
                ["anode"] = anode,
                ["cathode"] = cathode,
                ["color"] = color
            };

            // Add wire to components
            components[ledId] = ledEntry;
            state["components"] = components;

            // Convert back to JSON string
            string updatedState = state.ToString(Newtonsoft.Json.Formatting.None);


            // Update the breadboard state
            myBreadboardController.CmdUpdateBreadboardState(updatedState);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding led: {e.Message}");
        }
    }


    //seven segment
    //nodeB
    // will always be allowed when called
    public void AddSevenSegment(string nodeB)
    {
        try
        {
            // Get the current breadboard state JSON string
            string currentState = myBreadboardController.breadboardState;

            // Initialize with default state if empty
            if (string.IsNullOrEmpty(currentState))
                currentState = @"{ ""components"": {} }";


            // Parse the JSON structure
            JObject state = JObject.Parse(currentState);

            // Ensure components object exists
            JObject components = (JObject)state["components"] ?? new JObject();
            state["components"] = components;

            sevenSegCounter++;
            string sevenSegId = $"sevenSeg{sevenSegCounter}";

            // Create new wire entry
            JObject sevenSegEntry = new JObject
            {
                ["nodeB"] = nodeB,
                ["nodeA"] = GetNodeNameOffset(nodeB, 1, 0),
                ["nodeGnd1"] = GetNodeNameOffset(nodeB, 2, 0),
                ["nodeF"] = GetNodeNameOffset(nodeB, 3, 0),
                ["nodeG"] = GetNodeNameOffset(nodeB, 4, 0),

                ["nodeDP"] = GetNodeNameOffset(nodeB, 0, 5),
                ["nodeC"] = GetNodeNameOffset(nodeB, 1, 5),
                ["nodeGnd2"] = GetNodeNameOffset(nodeB, 2, 5),
                ["nodeD"] = GetNodeNameOffset(nodeB, 3, 5),
                ["nodeE"] = GetNodeNameOffset(nodeB, 4, 5),
            };

            // Add wire to components
            components[sevenSegId] = sevenSegEntry;
            state["components"] = components;

            // Convert back to JSON string
            string updatedState = state.ToString(Newtonsoft.Json.Formatting.None);

            // Update the breadboard state
            myBreadboardController.CmdUpdateBreadboardState(updatedState);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding seven segment: {e.Message}");
        }
    }

    // ic
    // nodeB
    // will always be allowed when called
    public void AddIC(string pin1, string type)
    {
        try
        {
            // Get the current breadboard state JSON string
            string currentState = myBreadboardController.breadboardState;

            // Initialize with default state if empty
            if (string.IsNullOrEmpty(currentState))
                currentState = @"{ ""components"": {} }";


            // Parse the JSON structure
            JObject state = JObject.Parse(currentState);

            // Ensure components object exists
            JObject components = (JObject)state["components"] ?? new JObject();
            state["components"] = components;

            icCounter++;
            string icId = $"ic{icCounter}";

            // Create new wire entry
            JObject icEntry = new JObject
            {
                ["type"] = type,

                ["pin1"] = pin1,
                ["pin2"] = GetNodeNameOffset(pin1, 1, 0),
                ["pin3"] = GetNodeNameOffset(pin1, 2, 0),
                ["pin4"] = GetNodeNameOffset(pin1, 3, 0),
                ["pin5"] = GetNodeNameOffset(pin1, 4, 0),
                ["pin6"] = GetNodeNameOffset(pin1, 5, 0),
                ["pin7"] = GetNodeNameOffset(pin1, 6, 0),
                ["pin8"] = GetNodeNameOffset(pin1, 7, 0),

                ["pin9"] = GetNodeNameOffset(pin1, 7, 1),
                ["pin10"] = GetNodeNameOffset(pin1, 6, 1),
                ["pin11"] = GetNodeNameOffset(pin1, 5, 1),
                ["pin12"] = GetNodeNameOffset(pin1, 4, 1),
                ["pin13"] = GetNodeNameOffset(pin1, 3, 1),
                ["pin14"] = GetNodeNameOffset(pin1, 2, 1),
                ["pin15"] = GetNodeNameOffset(pin1, 1, 1),
                ["pin16"] = GetNodeNameOffset(pin1, 0, 1),
            };

            // Add wire to components
            components[icId] = icEntry;
            state["components"] = components;

            // Convert back to JSON string
            string updatedState = state.ToString(Newtonsoft.Json.Formatting.None);

            // Update the breadboard state
            myBreadboardController.CmdUpdateBreadboardState(updatedState);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding IC: {e.Message}");
        }
    }

    public void RemoveComponentWithNode(string node)
    {
        try
        {
            // Get current breadboard state
            string currentState = myBreadboardController.breadboardState;
            if (string.IsNullOrEmpty(currentState))
                return;

            // Parse JSON structure
            JObject state = JObject.Parse(currentState);
            JObject components = (JObject)state["components"];

            if (components == null)
                return;

            List<string> componentsToRemove = new List<string>();
            // Collect nodes to clear occupancy
            HashSet<string> nodesToClear = new HashSet<string>();

            // Check each component's properties for the target node
            foreach (JProperty componentProp in components.Properties().ToList())
            {
                JObject component = (JObject)componentProp.Value;
                bool shouldRemove = false;

                foreach (JProperty prop in component.Properties())
                {
                    // Check if property value matches the target node
                    if (prop.Value.Type == JTokenType.String && (string)prop.Value == node)
                    {
                        shouldRemove = true;
                        break; // No need to check other properties of this component
                    }
                }

                // If this component should be removed, collect all its nodes
                if (shouldRemove)
                {
                    componentsToRemove.Add(componentProp.Name);

                    // Collect all nodes from this component
                    foreach (JProperty prop in component.Properties())
                    {
                        if (prop.Value.Type == JTokenType.String && prop.Name != "color" && prop.Name != "type")
                        {
                            string nodeName = (string)prop.Value;
                            if (!string.IsNullOrEmpty(nodeName))
                            {
                                nodesToClear.Add(nodeName);
                            }
                        }
                    }
                }
            }

            // Remove identified components
            foreach (string componentName in componentsToRemove)
            {
                components.Remove(componentName);
            }

            state["components"] = components;

            // Update breadboard state
            string updatedState = state.ToString(Newtonsoft.Json.Formatting.None);
            myBreadboardController.CmdUpdateBreadboardState(updatedState);

            // Clear occupancy of affected nodes
            ClearOccupancyOfNodes(myBreadboardController, nodesToClear);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error removing components with node {node}: {e.Message}");
        }
    }

    private void ClearOccupancyOfNodes(BreadboardController bc, HashSet<string> nodeNames)
    {
        Transform breadboardTransform = bc.transform.Find("Breadboard");
        if (breadboardTransform == null) return;

        foreach (string nodeName in nodeNames)
        {
            // Find the node
            Node node = FindNodeByName(breadboardTransform, nodeName);
            if (node != null)
            {
                node.ClearOccupancy();
            }
        }
    }

    /////////////////////////////////////////////
    ///VISUALIZATION
    /////////////////////////////////////////////
    ///

    [SerializeField] private GameObject wireComponent;
    [SerializeField] private GameObject ledComponent;
    [SerializeField] private GameObject sevenSegmentComponent;
    [SerializeField] private GameObject icComponent;

    //called onstatechanged or manually by spectators
    public void VisualizeBreadboard(BreadboardController bc)
    {
        // Parse the breadboard state
        string currentState = bc.breadboardState;
        if (string.IsNullOrEmpty(currentState))
        {
            currentState = @"{ ""components"": {} }";
        }

        Debug.Log($"Visualizing breadboard with state: {currentState}");

        JObject state = JObject.Parse(currentState);
        JToken components = state["components"] ?? new JObject();

        // Create/find Components parent
        Transform componentsParent = GetOrCreateComponentsParent(bc);

        // Clear all node occupancies
        ClearAllNodeOccupancies(bc);

        // Clear existing visualization
        ClearComponentsParent(componentsParent);

        // Collect all nodes used by components
        HashSet<string> occupiedNodes = CollectOccupiedNodes(components);
        MarkOccupiedNodes(bc, occupiedNodes);


        // Run simulation
        var simulator = BreadboardSimulator.Instance;
        if (simulator != null)
        {
            Debug.Log("Running breadboard simulation...");
            var result = simulator.Run(currentState);

            // Handle the result
            if (result.Errors.Count > 0)
            {
                Debug.LogWarning($"Simulation completed with {result.Errors.Count} errors");
            }
            else
            {
                Debug.Log("Simulation completed successfully");
            }

            //After simulation

            //Update LED
            // Create new visual representations by type
            foreach (JProperty componentProp in components)
            {
                HandleComponentVisualization(componentProp, componentsParent, result.ComponentStates);
            }

        }
        else
        {
            Debug.LogError("BreadboardSimulator instance not found!");
        }
    }


    private Transform GetOrCreateComponentsParent(BreadboardController bc)
    {
        Transform parent = bc.transform.Find("Breadboard").transform.Find("Components");
        if (parent == null)
        {
            parent = new GameObject("Components").transform;
            parent.SetParent(bc.transform.Find("Breadboard").transform);
            parent.localPosition = Vector3.zero;
            parent.localRotation = Quaternion.identity;
            parent.localScale = Vector3.one;
        }
        return parent;
    }

    private void ClearComponentsParent(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void HandleComponentVisualization(JProperty componentProp, Transform componentsParent, Dictionary<string, object> componentStates)
    {
        string componentKey = componentProp.Name;
        JObject componentData = (JObject)componentProp.Value;

        if (componentKey.StartsWith("wire"))
        {
            CreateWireComponent(componentData, componentsParent, componentKey);
        }
        else if (componentKey.StartsWith("led"))
        {
            var ledState = componentStates[componentKey];
            bool isLedOn = false;

            // Use reflection to get the 'isOn' property from the anonymous type
            if (ledState != null)
            {
                var prop = ledState.GetType().GetProperty("isOn");
                if (prop != null)
                {
                    isLedOn = (bool)prop.GetValue(ledState);
                }
            }

            CreateLEDComponent(componentData, componentsParent, componentKey, isLedOn);
        }
        else if (componentKey.StartsWith("sevenSeg"))
        {
            Dictionary<string, bool> segments = new Dictionary<string, bool>();

            try
            {
                // Convert the state to JObject for easier access
                JObject segmentState = JObject.FromObject(componentStates[componentKey]);

                // Access the segments property
                JObject segmentsObj = segmentState["segments"] as JObject;

                if (segmentsObj != null)
                {
                    // Extract each segment's boolean value
                    foreach (var prop in segmentsObj.Properties())
                    {
                        segments[prop.Name] = prop.Value.Value<bool>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing seven segment state: {ex.Message}");
            }

            Debug.Log($"Segments: {string.Join(", ", segments.Select(kv => $"{kv.Key}={kv.Value}"))}");

            // Create the seven segment component
            CreateSevenSegmentComponent(componentData, componentsParent, componentKey, segments);
        }
        else if (componentKey.StartsWith("ic"))
        {
            CreateICComponent(componentData, componentsParent, componentKey);
        }

        else
        {
            Debug.LogWarning($"Unhandled component type: {componentKey}");
        }
    }

    private void CreateWireComponent(JObject componentData, Transform parent, string name)
    {
        string startNode = componentData.Value<string>("startNode");
        string endNode = componentData.Value<string>("endNode");
        string color = componentData.Value<string>("color");

        if (string.IsNullOrEmpty(startNode) || string.IsNullOrEmpty(endNode))
        {
            Debug.LogWarning("Invalid wire configuration");
            return;
        }

        GameObject newWire = Instantiate(wireComponent, parent);
        newWire.name = name;
        Wire wireComponentScript = newWire.GetComponent<Wire>();

        if (wireComponentScript != null)
        {
            wireComponentScript.Initialize(startNode, endNode, color, parent.parent);
        }
        else
        {
            Debug.LogError("Wire prefab is missing Wire component");
            Destroy(newWire);
        }
    }

    private void CreateLEDComponent(JObject componentData, Transform parent, string name, bool isOn)
    {
        string anode = componentData.Value<string>("anode");
        string cathode = componentData.Value<string>("cathode");
        string color = componentData.Value<string>("color");

        if (string.IsNullOrEmpty(anode) || string.IsNullOrEmpty(cathode))
        {
            Debug.LogWarning("Invalid LED configuration");
            return;
        }

        GameObject newLed = Instantiate(ledComponent, parent);
        LED ledComponentScript = newLed.GetComponent<LED>();
        newLed.name = name;

        if (ledComponentScript != null)
        {
            ledComponentScript.Initialize(anode, cathode, color, parent.parent, isOn);
        }
        else
        {
            Debug.LogError("LED prefab is missing LED component");
            Destroy(newLed);
        }
    }

    private void CreateSevenSegmentComponent(JObject componentData, Transform parent, string name, Dictionary<string, bool> segments)
    {
        Debug.Log("CREATED 7 SEG");
        string nodeB = componentData.Value<string>("nodeB");

        if (string.IsNullOrEmpty(nodeB))
        {
            Debug.LogWarning("Invalid SEVEN SEGMENT configuration");
            return;
        }

        GameObject newSevenSegment = Instantiate(sevenSegmentComponent, parent);
        newSevenSegment.name = name;
        SevenSegment sevenSegmentScript = newSevenSegment.GetComponent<SevenSegment>();

        if (sevenSegmentScript != null)
        {
            sevenSegmentScript.Initialize(nodeB, parent.parent, segments);
        }
        else
        {
            Debug.LogError("seven segment prefab is missing SevenSegment component");
            Destroy(newSevenSegment);
        }
    }

    private void CreateICComponent(JObject componentData, Transform parent, string name)
    {
        Debug.Log("CREATED IC component");
        string type = componentData.Value<string>("type");
        string pin1 = componentData.Value<string>("pin1");

        if (string.IsNullOrEmpty(pin1) || string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("Invalid IC configuration");
            return;
        }

        GameObject newIC = Instantiate(icComponent, parent);
        newIC.name = name;
        IC icScript = newIC.GetComponent<IC>();

        if (icScript != null)
        {
            icScript.Initialize(pin1, type, parent.parent);
        }
        else
        {
            Debug.LogError("IC prefab is missing IC component");
            Destroy(newIC);
        }
    }



    private string GetNodeNameOffset(string nodeName, int rowOffset, int columnOffset)
    {
        return GetStringNameOffset(nodeName, rowOffset, columnOffset, 1, 30, 'A', 'J');
    }


    // NODE UTILS

    // Method to clear all node occupancies
    private void ClearAllNodeOccupancies(BreadboardController bc)
    {
        Transform breadboardTransform = bc.transform.Find("Breadboard");
        if (breadboardTransform == null) return;

        // Clear all power rail nodes
        ClearNodeGroupOccupancies(breadboardTransform.Find("PowerRailLeft"));
        ClearNodeGroupOccupancies(breadboardTransform.Find("PowerRailRight"));

        // Clear all component nodes
        ClearNodeGroupOccupancies(breadboardTransform.Find("NodesLeft"));
        ClearNodeGroupOccupancies(breadboardTransform.Find("NodesRight"));
    }

    // Helper to clear node groups
    private void ClearNodeGroupOccupancies(Transform nodeGroup)
    {
        if (nodeGroup == null) return;

        foreach (Transform row in nodeGroup)
        {
            foreach (Transform nodeTransform in row)
            {
                Node node = nodeTransform.GetComponent<Node>();
                if (node != null)
                {
                    node.ClearOccupancy();
                }
            }
        }
    }

    // Method to collect all node names from components
    private HashSet<string> CollectOccupiedNodes(JToken components)
    {
        HashSet<string> nodeNames = new HashSet<string>();

        foreach (JProperty componentProp in components)
        {
            JObject componentData = (JObject)componentProp.Value;

            // Collect all string property values as potential node names
            foreach (JProperty prop in componentData.Properties())
            {
                if (prop.Value.Type == JTokenType.String && prop.Name != "color" && prop.Name != "type")
                {
                    string nodeName = componentData.Value<string>(prop.Name);
                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        nodeNames.Add(nodeName);
                    }
                }
            }
        }

        return nodeNames;
    }

    //Method to mark all occupied nodes
    private void MarkOccupiedNodes(BreadboardController bc, HashSet<string> nodeNames)
    {
        Transform breadboardTransform = bc.transform.Find("Breadboard");
        if (breadboardTransform == null) return;

        foreach (string nodeName in nodeNames)
        {
            // Find and mark the node
            Node node = FindNodeByName(breadboardTransform, nodeName);
            if (node != null)
            {
                node.Occupy();
            }
        }
    }

    // Helper to find a node by its name
    private Node FindNodeByName(Transform breadboardTransform, string nodeName)
    {
        // Determine if it's a power rail node or a regular node
        bool isPowerNode = nodeName.Contains("PWR") || nodeName.Contains("GND");

        // Determine if it's on the left or right side
        bool isRightSide = isPowerNode ?
            (nodeName.StartsWith("3") || nodeName.StartsWith("4") || nodeName.StartsWith("5") || nodeName.StartsWith("6")) :
            (nodeName.EndsWith("F") || nodeName.EndsWith("G") || nodeName.EndsWith("H") || nodeName.EndsWith("I") || nodeName.EndsWith("J"));

        // Get the appropriate parent transform
        Transform parentTransform;
        if (isPowerNode)
        {
            parentTransform = isRightSide ?
                breadboardTransform.Find("PowerRailRight") :
                breadboardTransform.Find("PowerRailLeft");
        }
        else
        {
            parentTransform = isRightSide ?
                breadboardTransform.Find("NodesRight") :
                breadboardTransform.Find("NodesLeft");
        }

        if (parentTransform == null) return null;

        Transform nodeTransform = parentTransform.Find(nodeName);
        if (nodeTransform != null)
        {
            return nodeTransform.GetComponent<Node>();
        }

        return null;
    }


    // UTILITIES


    /// <summary>
    /// Calculates a new string name based on a starting string name and row/column offsets.
    /// Assumes the string name follows the format "NumberLetter" (e.g., "1A", "15B").
    /// </summary>
    /// <param name="startName">The starting string name.</param>
    /// <param name="rowOffset">The offset to apply to the number part of the name.</param>
    /// <param name="columnOffset">The offset to apply to the letter part of the name.</param>
    /// <param name="minNumber">The minimum allowed numerical value.</param>
    /// <param name="maxNumber">The maximum allowed numerical value.</param>
    /// <param name="minLetter">The minimum allowed letter (char) value.</param>
    /// <param name="maxLetter">The maximum allowed letter (char) value.</param>
    /// <returns>The new string name, or null if the calculation results in an invalid name.</returns>
    public static string GetStringNameOffset(string startName, int rowOffset, int columnOffset, int minNumber, int maxNumber, char minLetter, char maxLetter)
    {
        // Use regular expression to extract the number and letter
        Match match = Regex.Match(startName, @"(\d+)([A-Za-z])"); // Allow lowercase letters

        if (match.Success)
        {
            int nameNumber = int.Parse(match.Groups[1].Value);
            char nameLetter = char.ToUpper(match.Groups[2].Value[0]); // Get the letter and convert to uppercase

            // Calculate the new row number and column character
            int newNumber = nameNumber + rowOffset;
            char newLetter = (char)(nameLetter + columnOffset);

            // Validate the new number and letter
            if (newNumber >= minNumber && newNumber <= maxNumber && newLetter >= minLetter && newLetter <= maxLetter)
            {
                return newNumber.ToString() + newLetter.ToString();
            }
            else
            {
                Debug.LogWarning("Calculated string name coordinates are out of range: " + newNumber + newLetter);
                return null; // Indicate out of range
            }
        }
        else
        {
            Debug.LogError("Invalid string name format for offset calculation: " + startName);
            return null;
        }
    }

}
