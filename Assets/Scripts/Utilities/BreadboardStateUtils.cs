using System;
using UnityEngine;
using Mirror;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
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
    private int dipSwitchCounter = 0;
    private int resistorCounter = 0;

    [SerializeField] private GameObject wireComponent;
    [SerializeField] private GameObject ledComponent;
    [SerializeField] private GameObject sevenSegmentComponent;
    [SerializeField] private GameObject icComponent;
    [SerializeField] private GameObject dipSwitchComponent;
    [SerializeField] private GameObject resistorComponent;

    private void Awake()
    {
        Instance = this;
    }

    // Wire: startNode, endNode, color
    public void AddWire(string startNode, string endNode, string color)
    {

        wireCounter++;
        string wireId = $"wire{wireCounter}";

        BreadboardComponentData wire = new BreadboardComponentData
        {
            type = "wire",
            startNode = startNode,
            endNode = endNode,
            color = color
        };

        myBreadboardController.CmdAddComponent(wireId, wire);

    }

    // LED: anode, cathode, color
    public void AddLED(string anode, string cathode, string color)
    {
        try
        {
            ledCounter++;
            string ledId = $"led{ledCounter}";

            BreadboardComponentData led = new BreadboardComponentData
            {
                type = "led",
                ledId = ledId,
                anode = anode,
                cathode = cathode,
                color = color
            };

            myBreadboardController.CmdAddComponent(ledId, led);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding LED: {e.Message}");
        }
    }

    // Seven Segment: nodeB
    public void AddSevenSegment(string nodeB)
    {
        try
        {
            sevenSegCounter++;
            string sevenSegId = $"sevenSeg{sevenSegCounter}";

            BreadboardComponentData sevenSeg = new BreadboardComponentData
            {
                type = "sevenSeg",
                nodeB = nodeB,
                nodeA = GetNodeNameOffset(nodeB, 1, 0),
                nodeGnd1 = GetNodeNameOffset(nodeB, 2, 0),
                nodeF = GetNodeNameOffset(nodeB, 3, 0),
                nodeG = GetNodeNameOffset(nodeB, 4, 0),
                nodeDP = GetNodeNameOffset(nodeB, 0, 5),
                nodeC = GetNodeNameOffset(nodeB, 1, 5),
                nodeGnd2 = GetNodeNameOffset(nodeB, 2, 5),
                nodeD = GetNodeNameOffset(nodeB, 3, 5),
                nodeE = GetNodeNameOffset(nodeB, 4, 5)
            };

            myBreadboardController.CmdAddComponent(sevenSegId, sevenSeg);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding seven segment: {e.Message}");
        }
    }

    // IC: pin1, type
    public void AddIC(string pin9, string type)
    {
        try
        {
            icCounter++;
            string icId = $"ic{icCounter}";

            BreadboardComponentData ic = new BreadboardComponentData
            {
                type = "ic",
                icType = type,
                pin9 = pin9,
                pin10 = GetNodeNameOffset(pin9, 1, 0),
                pin11 = GetNodeNameOffset(pin9, 2, 0),
                pin12 = GetNodeNameOffset(pin9, 3, 0),
                pin13 = GetNodeNameOffset(pin9, 4, 0),
                pin14 = GetNodeNameOffset(pin9, 5, 0),
                pin15 = GetNodeNameOffset(pin9, 6, 0),
                pin16 = GetNodeNameOffset(pin9, 7, 0),
                pin1 = GetNodeNameOffset(pin9, 7, 1),
                pin2 = GetNodeNameOffset(pin9, 6, 1),
                pin3 = GetNodeNameOffset(pin9, 5, 1),
                pin4 = GetNodeNameOffset(pin9, 4, 1),
                pin5 = GetNodeNameOffset(pin9, 3, 1),
                pin6 = GetNodeNameOffset(pin9, 2, 1),
                pin7 = GetNodeNameOffset(pin9, 1, 1),
                pin8 = GetNodeNameOffset(pin9, 0, 1)
            };

            myBreadboardController.CmdAddComponent(icId, ic);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding IC: {e.Message}");
        }
    }

    // DipSwitch: pin1
    public void AddDipSwitch(string pin1, bool initialState = false)
    {
        try
        {
            dipSwitchCounter++;
            string dipSwitchId = $"dipSwitch{dipSwitchCounter}";

            BreadboardComponentData dipSwitch = new BreadboardComponentData
            {
                type = "dipSwitch",
                pin1 = pin1,
                pin2 = GetNodeNameOffset(pin1, 0, 1),
                isOn = initialState,
            };

            myBreadboardController.CmdAddComponent(dipSwitchId, dipSwitch);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding Dip Switch: {e.Message}");
        }
    }


    public void RemoveComponentWithNode(string node)
    {
        try
        {
            List<string> componentsToRemove = new List<string>();
            HashSet<string> nodesToClear = new HashSet<string>();

            // Check each component's properties for the target node
            foreach (var kvp in myBreadboardController.breadboardComponents)
            {
                string componentId = kvp.Key;
                BreadboardComponentData component = kvp.Value;
                bool shouldRemove = false;

                // Check if this component uses the target node
                if (NodeMatchesComponent(node, component))
                {
                    shouldRemove = true;
                    // Collect all nodes from this component to clear occupancy
                    HashSet<string> componentNodes = GetAllComponentNodes(component);
                    foreach (string nodeToAdd in componentNodes)
                    {
                        if (!string.IsNullOrEmpty(nodeToAdd))
                        {
                            nodesToClear.Add(nodeToAdd);
                        }
                    }
                }

                if (shouldRemove)
                {
                    componentsToRemove.Add(componentId);
                }
            }

            // Remove identified components
            foreach (string componentId in componentsToRemove)
            {
                myBreadboardController.CmdRemoveComponent(componentId);
            }

            // Clear occupancy of affected nodes
            ClearOccupancyOfNodes(myBreadboardController, nodesToClear);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error removing components with node {node}: {e.Message}");
        }
    }

    // Check if a node is used by a component
    private bool NodeMatchesComponent(string node, BreadboardComponentData component)
    {
        // Check based on component type
        switch (component.type)
        {
            case "wire":
                return component.startNode == node || component.endNode == node;

            case "led":
                return component.anode == node || component.cathode == node;

            case "sevenSeg":
                return component.nodeA == node || component.nodeB == node ||
                       component.nodeC == node || component.nodeD == node ||
                       component.nodeE == node || component.nodeF == node ||
                       component.nodeG == node || component.nodeDP == node ||
                       component.nodeGnd1 == node || component.nodeGnd2 == node;

            case "ic":
                return component.pin1 == node || component.pin2 == node ||
                       component.pin3 == node || component.pin4 == node ||
                       component.pin5 == node || component.pin6 == node ||
                       component.pin7 == node || component.pin8 == node ||
                       component.pin9 == node || component.pin10 == node ||
                       component.pin11 == node || component.pin12 == node ||
                       component.pin13 == node || component.pin14 == node ||
                       component.pin15 == node || component.pin16 == node;
            case "dipSwitch":
                return component.pin1 == node || component.pin2 == node;
            case "resistor":
                return component.resistorPin1 == node || component.resistorPin2 == node;

            default:
                return false;
        }
    }

    // Get all nodes used by a component
    private HashSet<string> GetAllComponentNodes(BreadboardComponentData component)
    {
        HashSet<string> nodes = new HashSet<string>();

        switch (component.type)
        {
            case "wire":
                if (!string.IsNullOrEmpty(component.startNode)) nodes.Add(component.startNode);
                if (!string.IsNullOrEmpty(component.endNode)) nodes.Add(component.endNode);
                break;

            case "led":
                if (!string.IsNullOrEmpty(component.anode)) nodes.Add(component.anode);
                if (!string.IsNullOrEmpty(component.cathode)) nodes.Add(component.cathode);
                break;

            case "sevenSeg":
                if (!string.IsNullOrEmpty(component.nodeA)) nodes.Add(component.nodeA);
                if (!string.IsNullOrEmpty(component.nodeB)) nodes.Add(component.nodeB);
                if (!string.IsNullOrEmpty(component.nodeC)) nodes.Add(component.nodeC);
                if (!string.IsNullOrEmpty(component.nodeD)) nodes.Add(component.nodeD);
                if (!string.IsNullOrEmpty(component.nodeE)) nodes.Add(component.nodeE);
                if (!string.IsNullOrEmpty(component.nodeF)) nodes.Add(component.nodeF);
                if (!string.IsNullOrEmpty(component.nodeG)) nodes.Add(component.nodeG);
                if (!string.IsNullOrEmpty(component.nodeDP)) nodes.Add(component.nodeDP);
                if (!string.IsNullOrEmpty(component.nodeGnd1)) nodes.Add(component.nodeGnd1);
                if (!string.IsNullOrEmpty(component.nodeGnd2)) nodes.Add(component.nodeGnd2);
                break;

            case "ic":
                if (!string.IsNullOrEmpty(component.pin1)) nodes.Add(component.pin1);
                if (!string.IsNullOrEmpty(component.pin2)) nodes.Add(component.pin2);
                if (!string.IsNullOrEmpty(component.pin3)) nodes.Add(component.pin3);
                if (!string.IsNullOrEmpty(component.pin4)) nodes.Add(component.pin4);
                if (!string.IsNullOrEmpty(component.pin5)) nodes.Add(component.pin5);
                if (!string.IsNullOrEmpty(component.pin6)) nodes.Add(component.pin6);
                if (!string.IsNullOrEmpty(component.pin7)) nodes.Add(component.pin7);
                if (!string.IsNullOrEmpty(component.pin8)) nodes.Add(component.pin8);
                if (!string.IsNullOrEmpty(component.pin9)) nodes.Add(component.pin9);
                if (!string.IsNullOrEmpty(component.pin10)) nodes.Add(component.pin10);
                if (!string.IsNullOrEmpty(component.pin11)) nodes.Add(component.pin11);
                if (!string.IsNullOrEmpty(component.pin12)) nodes.Add(component.pin12);
                if (!string.IsNullOrEmpty(component.pin13)) nodes.Add(component.pin13);
                if (!string.IsNullOrEmpty(component.pin14)) nodes.Add(component.pin14);
                if (!string.IsNullOrEmpty(component.pin15)) nodes.Add(component.pin15);
                if (!string.IsNullOrEmpty(component.pin16)) nodes.Add(component.pin16);
                break;
            case "dipSwitch":
                if (!string.IsNullOrEmpty(component.pin1)) nodes.Add(component.pin1);
                if (!string.IsNullOrEmpty(component.pin2)) nodes.Add(component.pin2);
                break;
            case "resistor":
                if (!string.IsNullOrEmpty(component.resistorPin1)) nodes.Add(component.resistorPin1);
                if (!string.IsNullOrEmpty(component.resistorPin2)) nodes.Add(component.resistorPin2);
                break;
        }

        return nodes;
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

    public void ClearAllNodeOccupanciesForController(BreadboardController bc)
    {
        ClearAllNodeOccupancies(bc);
    }

    // Visualization Methods
    // In the VisualizeBreadboard method, replace the simulator access:

    // Visualize entry point: no yields here (avoids CS1624)
    public void VisualizeBreadboard(BreadboardController bc)
    {
        StartCoroutine(VisualizeBreadboardCoroutine(bc));
    }

    // Async visualization: uses the simulator's coroutine to avoid frame hitching
    private IEnumerator VisualizeBreadboardCoroutine(BreadboardController bc)
    {
        Debug.Log("Visualizing breadboard");

        // Create/find Components parent
        Transform componentsParent = GetOrCreateComponentsParent(bc);

        // Clear all node occupancies
        ClearAllNodeOccupancies(bc);

        // Clear existing visualization
        ClearComponentsParent(componentsParent);

        // Yield after cleanup operations
        yield return null;

        // Always run simulation to ensure UI is updated, even with no components
        var simulator = bc.GetSimulatorInstance();
        if (simulator != null)
        {
            // Convert SyncDictionary to JSON for simulation
            string breadboardStateJson = ConvertStateToJson(bc.breadboardComponents);

            Debug.Log($"Running breadboard simulation for student {bc.studentId} (async)...");
            BreadboardSimulator.SimulationResult result = null;

            // Run the simulation asynchronously to avoid blocking the frame
            yield return StartCoroutine(simulator.RunCoroutine(breadboardStateJson, bc, r => result = r));

            // Handle the result
            if (result != null)
            {
                if (result.Errors.Count > 0)
                {
                    Debug.LogWarning($"Simulation completed with {result.Errors.Count} errors");
                }
                else
                {
                    Debug.Log("Simulation completed successfully");
                }

                // Only create visual components if there are components to visualize
                if (bc.breadboardComponents.Count > 0)
                {
                    // Create visual components
                    foreach (var kvp in bc.breadboardComponents)
                    {
                        string componentKey = kvp.Key;
                        BreadboardComponentData component = kvp.Value;
                        HandleComponentVisualization(componentKey, component, componentsParent, result.ComponentStates);
                    }
                }
                else
                {
                    Debug.Log("No components to visualize, but UI has been updated");
                }
            }
            else
            {
                Debug.LogError("Simulation failed to produce a result.");
            }
        }
        else
        {
            Debug.LogError($"BreadboardSimulator instance not found for controller {bc.studentId}!");
        }
    }

    // Convert SyncDictionary to JSON format expected by simulator
    public string ConvertStateToJson(SyncDictionary<string, BreadboardComponentData> components)
    {
        JObject state = new JObject();
        JObject componentsObj = new JObject();

        foreach (var kvp in components)
        {
            string componentKey = kvp.Key;
            BreadboardComponentData component = kvp.Value;

            JObject componentObj = new JObject();

            switch (component.type)
            {
                case "wire":
                    componentObj["startNode"] = component.startNode;
                    componentObj["endNode"] = component.endNode;
                    componentObj["color"] = component.color;
                    break;

                case "led":
                    componentObj["anode"] = component.anode;
                    componentObj["cathode"] = component.cathode;
                    componentObj["color"] = component.color;
                    break;

                case "sevenSeg":
                    componentObj["nodeA"] = component.nodeA;
                    componentObj["nodeB"] = component.nodeB;
                    componentObj["nodeC"] = component.nodeC;
                    componentObj["nodeD"] = component.nodeD;
                    componentObj["nodeE"] = component.nodeE;
                    componentObj["nodeF"] = component.nodeF;
                    componentObj["nodeG"] = component.nodeG;
                    componentObj["nodeDP"] = component.nodeDP;
                    componentObj["nodeGnd1"] = component.nodeGnd1;
                    componentObj["nodeGnd2"] = component.nodeGnd2;
                    break;

                case "ic":
                    componentObj["type"] = component.icType;
                    componentObj["pin1"] = component.pin1;
                    componentObj["pin2"] = component.pin2;
                    componentObj["pin3"] = component.pin3;
                    componentObj["pin4"] = component.pin4;
                    componentObj["pin5"] = component.pin5;
                    componentObj["pin6"] = component.pin6;
                    componentObj["pin7"] = component.pin7;
                    componentObj["pin8"] = component.pin8;
                    componentObj["pin9"] = component.pin9;
                    componentObj["pin10"] = component.pin10;
                    componentObj["pin11"] = component.pin11;
                    componentObj["pin12"] = component.pin12;
                    componentObj["pin13"] = component.pin13;
                    componentObj["pin14"] = component.pin14;
                    componentObj["pin15"] = component.pin15;
                    componentObj["pin16"] = component.pin16;
                    break;
                case "resistor":
                    componentObj["pin1"] = component.resistorPin1;
                    componentObj["pin2"] = component.resistorPin2;
                    break;
                case "dipSwitch":
                    componentObj["pin1"] = component.pin1;
                    componentObj["pin2"] = component.pin2;
                    componentObj["isOn"] = component.isOn;
                    break;
            }

            componentsObj[componentKey] = componentObj;
        }

        state["components"] = componentsObj;
        return state.ToString(Newtonsoft.Json.Formatting.None);
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

    private void HandleComponentVisualization(string componentKey, BreadboardComponentData component, Transform componentsParent, Dictionary<string, object> componentStates)
    {
        switch (component.type)
        {
            case "wire":
                CreateWireComponent(component, componentsParent, componentKey);
                break;

            case "led":
                bool isLedOn = false;

                // Try to get LED state from simulation results
                if (componentStates.TryGetValue(componentKey, out object ledState))
                {
                    // Use reflection to get the 'isOn' property from the anonymous type
                    if (ledState != null)
                    {
                        var prop = ledState.GetType().GetProperty("isOn");
                        if (prop != null)
                        {
                            isLedOn = (bool)prop.GetValue(ledState);
                        }
                    }
                }

                CreateLEDComponent(component, componentsParent, componentKey, isLedOn);
                break;

            case "sevenSeg":
                Dictionary<string, bool> segments = new Dictionary<string, bool>();

                try
                {
                    if (componentStates.TryGetValue(componentKey, out object segState))
                    {
                        // Convert the state to JObject for easier access
                        JObject segmentState = JObject.FromObject(segState);

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
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing seven segment state: {ex.Message}");
                }

                CreateSevenSegmentComponent(component, componentsParent, componentKey, segments);
                break;

            case "ic":
                CreateICComponent(component, componentsParent, componentKey);
                break;
            case "dipSwitch":
                CreateDipSwitchComponent(component, componentsParent, componentKey);
                break;
            case "resistor":
                CreateResistorComponent(component, componentsParent, componentKey);
                break;

            default:
                Debug.LogWarning($"Unhandled component type: {component.type} for {componentKey}");
                break;
        }
    }

    private void CreateWireComponent(BreadboardComponentData wire, Transform parent, string name)
    {
        if (string.IsNullOrEmpty(wire.startNode) || string.IsNullOrEmpty(wire.endNode))
        {
            Debug.LogWarning("Invalid wire configuration");
            return;
        }

        GameObject newWire = Instantiate(wireComponent, parent);
        newWire.name = name;
        Wire wireComponentScript = newWire.GetComponent<Wire>();

        if (wireComponentScript != null)
        {
            wireComponentScript.Initialize(wire.startNode, wire.endNode, wire.color, parent.parent);
        }
        else
        {
            Debug.LogError("Wire prefab is missing Wire component");
            Destroy(newWire);
        }
    }

    private void CreateLEDComponent(BreadboardComponentData led, Transform parent, string name, bool isOn)
    {
        if (string.IsNullOrEmpty(led.anode) || string.IsNullOrEmpty(led.cathode))
        {
            Debug.LogWarning("Invalid LED configuration");
            return;
        }

        GameObject newLed = Instantiate(ledComponent, parent);
        LED ledComponentScript = newLed.GetComponent<LED>();
        newLed.name = name;

        if (ledComponentScript != null)
        {
            ledComponentScript.Initialize(led.anode, led.cathode, led.color, parent.parent, isOn);
        }
        else
        {
            Debug.LogError("LED prefab is missing LED component");
            Destroy(newLed);
        }
    }

    private void CreateSevenSegmentComponent(BreadboardComponentData sevenSeg, Transform parent, string name, Dictionary<string, bool> segments)
    {
        if (string.IsNullOrEmpty(sevenSeg.nodeB))
        {
            Debug.LogWarning("Invalid SEVEN SEGMENT configuration");
            return;
        }

        GameObject newSevenSegment = Instantiate(sevenSegmentComponent, parent);
        newSevenSegment.name = name;
        SevenSegment sevenSegmentScript = newSevenSegment.GetComponent<SevenSegment>();

        if (sevenSegmentScript != null)
        {
            sevenSegmentScript.Initialize(sevenSeg.nodeB, parent.parent, segments);
        }
        else
        {
            Debug.LogError("seven segment prefab is missing SevenSegment component");
            Destroy(newSevenSegment);
        }
    }

    private void CreateICComponent(BreadboardComponentData ic, Transform parent, string name)
    {
        if (string.IsNullOrEmpty(ic.pin9) || string.IsNullOrEmpty(ic.icType))
        {
            Debug.LogWarning("Invalid IC configuration");
            return;
        }

        GameObject newIC = Instantiate(icComponent, parent);
        newIC.name = name;
        IC icScript = newIC.GetComponent<IC>();

        if (icScript != null)
        {
            icScript.Initialize(ic.pin9, ic.icType, parent.parent);
        }
        else
        {
            Debug.LogError("IC prefab is missing IC component");
            Destroy(newIC);
        }
    }

    private void CreateDipSwitchComponent(BreadboardComponentData dipSwitch, Transform parent, string name)
    {
        if (string.IsNullOrEmpty(dipSwitch.pin1))
        {
            Debug.LogWarning("Invalid dip switch configuration");
            return;
        }

        GameObject newDipSwitch = Instantiate(dipSwitchComponent, parent);
        newDipSwitch.name = name;
        DipSwitch dipSwitchScript = newDipSwitch.GetComponent<DipSwitch>();

        if (dipSwitchScript != null)
        {
            dipSwitchScript.Initialize(dipSwitch.pin1, dipSwitch.isOn, parent.parent);
        }
        else
        {
            Debug.LogError("DipSwitch prefab is missing DipSwitch component");
            Destroy(newDipSwitch);
        }
    }

    // Node Utils
    private void ClearAllNodeOccupancies(BreadboardController bc)
    {
        Debug.Log($"Clearing all node occupancies for controller {bc.studentId}");
        Transform breadboardTransform = bc.transform.Find("Breadboard");
        if (breadboardTransform == null)
        {
            Debug.LogError("Breadboard transform not found!");
            return;
        }

        // Clear all power rail nodes
        ClearNodeGroupOccupancies(breadboardTransform.Find("PowerRailLeft"));
        ClearNodeGroupOccupancies(breadboardTransform.Find("PowerRailRight"));

        // Clear all component nodes
        ClearNodeGroupOccupancies(breadboardTransform.Find("NodesLeft"));
        ClearNodeGroupOccupancies(breadboardTransform.Find("NodesRight"));

        Debug.Log("Finished clearing all node occupancies");
    }

    private void ClearNodeGroupOccupancies(Transform nodeGroup)
    {
        if (nodeGroup == null) 
        {
            Debug.LogWarning($"Node group is null: {nodeGroup}");
            return;
        }
    
        int clearedCount = 0;
        int totalNodesFound = 0;
        
        // Check if nodes are direct children or nested in rows
        Node[] directNodes = nodeGroup.GetComponentsInChildren<Node>();
        
        foreach (Node node in directNodes)
        {
            totalNodesFound++;
            if (node.isOccupied)
            {
                Debug.Log($"Clearing occupied node: {node.nodeId}");
                node.ClearOccupancy();
                clearedCount++;
            }
        }
        
        Debug.Log($"Found {totalNodesFound} total nodes in group {nodeGroup.name}, cleared {clearedCount} occupied nodes");
    }

    private HashSet<string> CollectOccupiedNodes(SyncDictionary<string, BreadboardComponentData> components)
    {
        HashSet<string> nodeNames = new HashSet<string>();

        foreach (var kvp in components)
        {
            BreadboardComponentData component = kvp.Value;
            HashSet<string> componentNodes = GetAllComponentNodes(component);

            foreach (string nodeName in componentNodes)
            {
                if (!string.IsNullOrEmpty(nodeName))
                {
                    nodeNames.Add(nodeName);
                }
            }
        }

        return nodeNames;
    }

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

    // Utils
    private string GetNodeNameOffset(string nodeName, int rowOffset, int columnOffset)
    {
        return GetStringNameOffset(nodeName, rowOffset, columnOffset, 1, 30, 'A', 'J');
    }

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

    // Resistor: pin1, pin2 (220Ω fixed)
    public void AddResistor(string pin1, string pin2)
    {
        try
        {
            resistorCounter++;
            string resistorId = $"resistor{resistorCounter}";
            
            BreadboardComponentData resistor = new BreadboardComponentData
            {
                type = "resistor",
                resistorPin1 = pin1,
                resistorPin2 = pin2
            };
            
            myBreadboardController.CmdAddComponent(resistorId, resistor);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding resistor: {e.Message}");
        }
    }

    private void CreateResistorComponent(BreadboardComponentData resistor, Transform parent, string name)
    {
        if (string.IsNullOrEmpty(resistor.resistorPin1) || string.IsNullOrEmpty(resistor.resistorPin2))
        {
            Debug.LogWarning("Invalid resistor configuration");
            return;
        }
    
        GameObject newResistor = Instantiate(resistorComponent, parent);
        newResistor.name = name;
        Resistor resistorScript = newResistor.GetComponent<Resistor>();
    
        if (resistorScript != null)
        {
            resistorScript.Initialize(resistor.resistorPin1, resistor.resistorPin2, parent.parent);
        }
        else
        {
            Debug.LogError("Resistor prefab is missing Resistor component");
            Destroy(newResistor);
        }
    }
}
