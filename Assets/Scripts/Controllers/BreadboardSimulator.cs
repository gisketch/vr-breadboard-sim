using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class BreadboardSimulator : MonoBehaviour
{

    public GameObject warningText;
    public GameObject taskText;

    public static BreadboardSimulator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        InitializeExperiments();
    }

    public enum NodeState
    {
        LOW,           // Ground/0V
        HIGH,          // Powered/5V 
        UNINITIALIZED  // Not connected to power or ground
    }

    // Distinguishes power sources
    public enum PowerSource
    {
        RAIL,  // From power rails
        IC,    // From IC output pins
        NONE   // No power source
    }

    // Represents a group of electrically connected nodes
    public class Net
    {
        public int Id;
        public List<string> Nodes = new List<string>();
        public NodeState State = NodeState.UNINITIALIZED;
        public PowerSource Source = PowerSource.NONE;
        public string SourceComponent;  // Which IC is driving this net, if any
        public List<string> SourceComponents = new List<string>();

        // Add a method to add source components
        public void AddSourceComponent(string component)
        {
            if (!SourceComponents.Contains(component))
            {
                SourceComponents.Add(component);
            }
        }
    }

    // Breadboard graph representation
    public class BreadboardGraph
    {
        private Dictionary<string, List<string>> _adjacencyList = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _resistorConnections = new Dictionary<string, List<string>>();

        public void AddNode(string node)
        {
            if (!_adjacencyList.ContainsKey(node))
            {
                _adjacencyList[node] = new List<string>();
            }
        }

        public void AddEdge(string node1, string node2)
        {
            // Ensure nodes exist
            AddNode(node1);
            AddNode(node2);

            // Add bidirectional connection
            if (!_adjacencyList[node1].Contains(node2))
            {
                _adjacencyList[node1].Add(node2);
            }
            if (!_adjacencyList[node2].Contains(node1))
            {
                _adjacencyList[node2].Add(node1);
            }
        }

        public void MarkAsResistor(string node1, string node2, string resistorId)
        {
            if (!_resistorConnections.ContainsKey(node1))
            {
                _resistorConnections[node1] = new List<string>();
            }
            if (!_resistorConnections.ContainsKey(node2))
            {
                _resistorConnections[node2] = new List<string>();
            }

            _resistorConnections[node1].Add(resistorId);
            _resistorConnections[node2].Add(resistorId);
        }

        public List<string> GetResistorsForNode(string node)
        {
            if (_resistorConnections.ContainsKey(node))
            {
                return _resistorConnections[node];
            }
            return new List<string>();
        }

        public List<string> GetNeighbors(string node)
        {
            if (_adjacencyList.ContainsKey(node))
            {
                return _adjacencyList[node];
            }
            return new List<string>();
        }

        public IEnumerable<string> GetNodes()
        {
            return _adjacencyList.Keys;
        }
    }

    // Breadboard error representation
    public class BreadboardError
    {
        public string ErrorType;
        public string Description;
        public List<string> AffectedNodes = new List<string>();
        public List<string> InvolvedComponents = new List<string>();
    }

    // Result of simulation
    public class SimulationResult
    {
        public Dictionary<string, object> ComponentStates = new Dictionary<string, object>();
        public List<Net> Nets = new List<Net>();
        public List<BreadboardError> Errors = new List<BreadboardError>();
        public ExperimentResult ExperimentResult { get; set; }
    }

    // Main entry point that takes JSON state and returns simulation results
    public SimulationResult Run(string jsonState, BreadboardController bc)
    {

        // Debug the input JSON
        Debug.Log($"Running simulation with JSON: {jsonState}");

        // 1. Parse JSON (safely)
        JObject parsedJson;
        try
        {
            parsedJson = JObject.Parse(jsonState);

            // Check if "components" exists in the parsed JSON
            if (parsedJson == null || !parsedJson.ContainsKey("components"))
            {
                Debug.LogError("Invalid JSON format: missing 'components' key");
                return new SimulationResult
                {
                    Errors = new List<BreadboardError> {
                            new BreadboardError {
                                ErrorType = "ParseError",
                                Description = "Missing 'components' key in JSON"
                            }
                        }
                };
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON parsing error: {ex.Message}");
            return new SimulationResult
            {
                Errors = new List<BreadboardError> {
                        new BreadboardError {
                            ErrorType = "ParseError",
                            Description = $"Error parsing JSON: {ex.Message}"
                        }
                    }
            };
        }

        var components = parsedJson["components"];

        // 2. Build electrical network
        var graph = BuildGraph(components);
        var nets = IdentifyNets(graph);
        DetermineInitialStates(nets, graph, components);

        // 3. Run simulation
        var errors = DetectErrors(nets);
        var componentStates = EvaluateComponents(nets, components);

        // 4. Return results with component states and errors
        var result = new SimulationResult
        {
            ComponentStates = componentStates,
            Nets = nets,
            Errors = errors
        };

        // Evaluate experiment
        result.ExperimentResult = EvaluateExperiment(result, components);

        //Clear messages first
        foreach (Transform child in bc.labMessagesTransform)
        {
            Destroy(child.gameObject);
        }

        //Add main instruction
        GameObject taskMsg = Instantiate(taskText);

        bool isCompleted = ((float)_completedInstructions[CurrentExperimentId].Count / _experiments[CurrentExperimentId].TotalInstructions) == 1f;

        taskMsg.transform.SetParent(bc.labMessagesTransform);
        taskMsg.transform.localScale = Vector3.one;
        taskMsg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        taskMsg.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
        taskMsg.transform.Find("Message").GetComponent<TMP_Text>().text = isCompleted ? $"Experiment {CurrentExperimentId} Completed!" : _experiments[CurrentExperimentId].InstructionDescriptions[CurrentInstructionIndex];

        //Append messages
        foreach (string msg in result.ExperimentResult.Messages)
        {
            GameObject warningMsg = Instantiate(warningText);
            warningMsg.transform.SetParent(bc.labMessagesTransform);
            warningMsg.transform.Find("Message").GetComponent<TMP_Text>().text = msg;
            warningMsg.transform.localScale = Vector3.one;
            warningMsg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            warningMsg.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
        }

        //Update Experiment Name
        TMP_Text experimentName = bc.transform.Find("Canvas").Find("ExperimentName").GetComponent<TMP_Text>();
        experimentName.text = result.ExperimentResult.ExperimentName;


        // Activate appropriate diagram based on current experiment
        Transform leftPanel = bc.transform.Find("Canvas").Find("LeftPanel");
        GameObject diagram7448 = leftPanel.Find("7448Diagram").gameObject;
        GameObject diagram74138 = leftPanel.Find("74138Diagram").gameObject;
        GameObject diagram74148 = leftPanel.Find("74148Diagram").gameObject;

        // Deactivate all diagrams first
        diagram7448.SetActive(false);
        diagram74138.SetActive(false);
        diagram74148.SetActive(false);

        // Activate the appropriate diagram based on current experiment
        switch (CurrentExperimentId)
        {
            case 1: // IC 74138 Decoder experiment
                diagram74138.SetActive(true);
                break;
            case 2: // BCD to 7-Segment Display experiment (uses IC 7448)
                diagram7448.SetActive(true);
                break;
            case 3: // IC 74148 Encoder experiment
                diagram74148.SetActive(true);
                break;
        }

        //Update Slider (child of experiment name)
        GameObject taskSlider = experimentName.transform.Find("Task Completion").gameObject;
        RectTransform fillSlider = taskSlider.transform.Find("Background").Find("Image").GetComponent<RectTransform>();
        float maxWidthSlider = 160f;

        // Calculate the target fill amount (0 to 1)
        float targetFillAmount = (float)_completedInstructions[CurrentExperimentId].Count / _experiments[CurrentExperimentId].TotalInstructions;
        float currentWidth = fillSlider.sizeDelta.x;
        float targetWidth = maxWidthSlider * targetFillAmount;

        // Animate the fill width
        LeanTween.value(fillSlider.gameObject, currentWidth, targetWidth, 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((float val) =>
            {
                fillSlider.sizeDelta = new Vector2(val, fillSlider.sizeDelta.y);
            });

        //Update slider text
        TMP_Text completionText = experimentName.transform.Find("Task Completion Text").GetComponent<TMP_Text>();
        completionText.text = $"COMPLETED {_completedInstructions[CurrentExperimentId].Count}/{_experiments[CurrentExperimentId].TotalInstructions}";

        //Clear button events here
        Button prevExperiment = experimentName.transform.Find("PrevExperiment").GetComponent<Button>();
        Button nextExperiment = experimentName.transform.Find("NextExperiment").GetComponent<Button>();

        prevExperiment.onClick.RemoveAllListeners();
        nextExperiment.onClick.RemoveAllListeners();

        Debug.Log($"Updating score from simulator");
        bc.CmdUpdateScore($@"Student {bc.studentId}
 - experiment 1 : {_completedInstructions[1].Count}/{_experiments[1].TotalInstructions}
 - experiment 2 : {_completedInstructions[2].Count}/{_experiments[2].TotalInstructions}
 - experiment 3 : {_completedInstructions[3].Count}/{_experiments[3].TotalInstructions}
        ");

        //Add button events
        prevExperiment.onClick.AddListener(() =>
        {
            PreviousExperiment();
            BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
        });

        //Add button events
        nextExperiment.onClick.AddListener(() =>
        {
            NextExperiment();
            BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
        });

        // Render experiment messages
        foreach (var kvp in result.ComponentStates)
        {
            Debug.Log($"Component {kvp.Key}: {Newtonsoft.Json.JsonConvert.SerializeObject(kvp.Value, Newtonsoft.Json.Formatting.Indented)}");
        }
        return result;

    }

    // Build the graph representation of the breadboard
    private BreadboardGraph BuildGraph(JToken components)
    {
        var graph = new BreadboardGraph();

        // Add all breadboard nodes
        AddBreadboardNodes(graph);

        // Connect nodes based on breadboard structure
        ConnectBreadboardRows(graph);

        // Add wire connections from components
        ConnectWires(graph, components);

        return graph;
    }

    // Add all possible breadboard nodes
    private void AddBreadboardNodes(BreadboardGraph graph)
    {
        // Left power rail (1PWR to 30PWR)
        for (int i = 1; i <= 30; i++)
        {
            graph.AddNode(i + "PWR");
            graph.AddNode(i + "GND");
        }

        // Right power rail (31PWR to 60PWR)
        for (int i = 31; i <= 60; i++)
        {
            graph.AddNode(i + "PWR");
            graph.AddNode(i + "GND");
        }

        // Left nodes (1A-1E to 30A-30E)
        for (int i = 1; i <= 30; i++)
        {
            foreach (char col in "ABCDE")
            {
                graph.AddNode(i + col.ToString());
            }
        }

        // Right nodes (1F-1J to 30F-30J)
        for (int i = 1; i <= 30; i++)
        {
            foreach (char col in "FGHIJ")
            {
                graph.AddNode(i + col.ToString());
            }
        }
    }

    // Connect nodes based on breadboard internal structure
    private void ConnectBreadboardRows(BreadboardGraph graph)
    {
        // Connect left side rows (A-E in same row)
        for (int i = 1; i <= 30; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                char col1 = "ABCDE"[j];
                char col2 = "ABCDE"[j + 1];
                graph.AddEdge(i + col1.ToString(), i + col2.ToString());
            }
        }

        // Connect right side rows (F-J in same row)
        for (int i = 1; i <= 30; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                char col1 = "FGHIJ"[j];
                char col2 = "FGHIJ"[j + 1];
                graph.AddEdge(i + col1.ToString(), i + col2.ToString());
            }
        }
    }

    // Connect wires from component definitions
    private void ConnectWires(BreadboardGraph graph, JToken components)
    {
        foreach (JProperty componentProp in components)
        {
            string componentKey = componentProp.Name;
            JToken componentValue = componentProp.Value;

            // Handle wires directly
            if (componentKey.StartsWith("wire"))
            {
                // Get values from JObject/JToken
                string startNode = componentValue["startNode"]?.ToString();
                string endNode = componentValue["endNode"]?.ToString();

                if (!string.IsNullOrEmpty(startNode) && !string.IsNullOrEmpty(endNode))
                {
                    graph.AddEdge(startNode, endNode);
                }
                else
                {
                    Debug.LogError($"Wire {componentKey} missing startNode or endNode");
                }
            }
            else if (componentKey.StartsWith("dipSwitch"))
            {
                string pin1 = componentValue["pin1"] != null ? componentValue["pin1"].ToString() :
                              componentValue["inputPin"]?.ToString();  // Support both naming conventions

                string pin2 = componentValue["pin2"] != null ? componentValue["pin2"].ToString() :
                              componentValue["outputPin"]?.ToString(); // Support both naming conventions

                bool isOn = componentValue["isOn"] != null ? componentValue["isOn"].Value<bool>() : false;

                if (!string.IsNullOrEmpty(pin1) && !string.IsNullOrEmpty(pin2))
                {
                    // Make sure both pins exist in the graph
                    graph.AddNode(pin1);
                    graph.AddNode(pin2);

                    // Only connect the pins if the switch is ON
                    if (isOn)
                    {
                        Debug.Log($"DIP Switch {componentKey} is ON, connecting {pin1} to {pin2}");
                        graph.AddEdge(pin1, pin2);
                    }
                    else
                    {
                        Debug.Log($"DIP Switch {componentKey} is OFF, pins {pin1} and {pin2} are disconnected (pull-up applied later)");
                        // We'll handle the pull-up behavior in the evaluation phase
                    }
                }
                else
                {
                    Debug.LogError($"DIP Switch {componentKey} missing pin1/inputPin or pin2/outputPin");
                }
            }
            else if (componentKey.StartsWith("resistor"))
            {
                string pin1 = componentValue["pin1"]?.ToString();
                string pin2 = componentValue["pin2"]?.ToString();

                if (!string.IsNullOrEmpty(pin1) && !string.IsNullOrEmpty(pin2))
                {
                    // Make sure both pins exist in the graph
                    graph.AddNode(pin1);
                    graph.AddNode(pin2);

                    // Connect the resistor pins
                    graph.AddEdge(pin1, pin2);

                    // Mark this connection as a resistor
                    graph.MarkAsResistor(pin1, pin2, componentKey);

                    // Note: We'll track resistors in nets during DetermineInitialStates
                }
                else
                {
                    Debug.LogError($"Resistor {componentKey} missing pin1 or pin2");
                }
            }
            // Handle other components by their pin connections
            else
            {
                // For each property in the component
                foreach (JProperty property in componentValue.Children<JProperty>())
                {
                    if (property.Name != "type" && property.Name != "color" && property.Name != "isOn")
                    {
                        string node = property.Value.ToString();
                        if (!string.IsNullOrEmpty(node))
                        {
                            graph.AddNode(node);
                        }
                    }
                }
            }
        }
    }

    // Find all connected components (nets) in the graph
    private List<Net> IdentifyNets(BreadboardGraph graph)
    {
        var nets = new List<Net>();
        var visited = new HashSet<string>();

        foreach (var node in graph.GetNodes())
        {
            if (!visited.Contains(node))
            {
                var net = new Net { Id = nets.Count };
                net.Nodes = PerformDFS(graph, node, visited);
                nets.Add(net);
            }
        }

        return nets;
    }

    // Depth-first search to find all connected nodes
    private List<string> PerformDFS(BreadboardGraph graph, string startNode, HashSet<string> visited)
    {
        var connectedNodes = new List<string>();
        var stack = new Stack<string>();

        stack.Push(startNode);
        visited.Add(startNode);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();
            connectedNodes.Add(currentNode);

            foreach (var neighbor in graph.GetNeighbors(currentNode))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    stack.Push(neighbor);
                }
            }
        }

        return connectedNodes;
    }

    // Determine initial electrical states based on power and ground connections
    private void DetermineInitialStates(List<Net> nets, BreadboardGraph graph, JToken components)
    {
        foreach (var net in nets)
        {
            // A net with PWR nodes is HIGH
            if (net.Nodes.Any(n => n.Contains("PWR")))
            {
                net.State = NodeState.HIGH;
                net.Source = PowerSource.RAIL;
            }
            // A net with GND nodes is LOW
            else if (net.Nodes.Any(n => n.Contains("GND")))
            {
                net.State = NodeState.LOW;
                net.Source = PowerSource.RAIL;
            }
            else
            {
                net.State = NodeState.UNINITIALIZED;
                net.Source = PowerSource.NONE;
            }

            // Track resistors in this net
            foreach (var node in net.Nodes)
            {
                foreach (var resistorId in graph.GetResistorsForNode(node))
                {
                    net.AddSourceComponent(resistorId);
                }
            }
        }

        // Also track other components in nets
        foreach (JProperty componentProp in components)
        {
            string componentKey = componentProp.Name;
            JToken componentValue = componentProp.Value;

            if (componentKey.StartsWith("resistor"))
            {
                // Resistors are already tracked above
                continue;
            }

            // For each pin in the component
            foreach (JProperty property in componentValue.Children<JProperty>())
            {
                if (property.Name != "type" && property.Name != "color" && property.Name != "isOn")
                {
                    string node = property.Value.ToString();
                    if (!string.IsNullOrEmpty(node))
                    {
                        // Find which net this node belongs to
                        foreach (var net in nets)
                        {
                            if (net.Nodes.Contains(node))
                            {
                                net.AddSourceComponent(componentKey);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    // Detect electrical errors like short circuits
    private List<BreadboardError> DetectErrors(List<Net> nets)
    {
        var errors = new List<BreadboardError>();

        foreach (var net in nets)
        {
            // Detect short circuits (PWR connected to GND)
            bool hasPwr = net.Nodes.Any(n => n.Contains("PWR"));
            bool hasGnd = net.Nodes.Any(n => n.Contains("GND"));

            if (hasPwr && hasGnd)
            {
                errors.Add(new BreadboardError
                {
                    ErrorType = "ShortCircuit",
                    Description = "Power rail connected to ground rail",
                    AffectedNodes = net.Nodes.Where(n => n.Contains("PWR") || n.Contains("GND")).ToList()
                });

                // In case of short circuit, set to LOW (GND dominates for safety)
                net.State = NodeState.LOW;
            }

            // Detect multiple drivers conflict
            if (net.Source == PowerSource.RAIL && net.SourceComponent != null)
            {
                errors.Add(new BreadboardError
                {
                    ErrorType = "MultipleDrivers",
                    Description = "Both power rail and IC driving the same net",
                    AffectedNodes = net.Nodes,
                    InvolvedComponents = new List<string> { net.SourceComponent }
                });
            }
        }

        return errors;
    }

    // Build a mapping from node names to net IDs
    private Dictionary<string, int> BuildNodeToNetMap(List<Net> nets)
    {
        var nodeToNetMap = new Dictionary<string, int>();

        for (int i = 0; i < nets.Count; i++)
        {
            foreach (var node in nets[i].Nodes)
            {
                nodeToNetMap[node] = i;
            }
        }

        return nodeToNetMap;
    }

    // Get which nets a component's pins are connected to
    private Dictionary<string, int> GetConnectedNets(JToken component, Dictionary<string, int> nodeToNetMap)
    {
        var connectedNets = new Dictionary<string, int>();

        foreach (JProperty property in component.Children<JProperty>())
        {
            if (property.Name != "type" && property.Name != "color" && property.Name != "isOn" && property.Name != "ledId")
            {
                string node = property.Value.ToString();
                if (!string.IsNullOrEmpty(node) && nodeToNetMap.ContainsKey(node))
                {
                    connectedNets[property.Name] = nodeToNetMap[node];
                }
            }
        }

        return connectedNets;
    }

    // Check if two nodes are electrically connected (in the same net)
    public bool AreNodesConnected(string nodeA, string nodeB, List<Net> nets)
    {
        // Quick check - if nodes are identical, they're connected
        if (nodeA == nodeB)
        {
            return true;
        }

        // Build node-to-net lookup map
        var nodeToNetMap = BuildNodeToNetMap(nets);

        // Check if both nodes exist in our nets
        if (!nodeToNetMap.ContainsKey(nodeA) || !nodeToNetMap.ContainsKey(nodeB))
        {
            return false;
        }

        // Check if nodes are in the same net
        return nodeToNetMap[nodeA] == nodeToNetMap[nodeB];
    }

    // Evaluate all components in the circuit
    private Dictionary<string, object> EvaluateComponents(List<Net> nets, JToken components)
    {
        var componentStates = new Dictionary<string, object>();
        bool changed = true;
        var nodeToNetMap = BuildNodeToNetMap(nets);

        // Repeat until no more changes
        int iterationLimit = 10; // Prevent infinite loops
        int iterations = 0;

        while (changed && iterations < iterationLimit)
        {
            changed = false;
            iterations++;

            foreach (JProperty componentProp in components.Children<JProperty>())
            {
                string componentKey = componentProp.Name;
                JToken componentValue = componentProp.Value;

                if (componentKey.StartsWith("wire"))
                {
                    continue;
                }

                // Add resistor handling
                if (componentKey.StartsWith("resistor"))
                {
                    // Get pin information
                    string pin1 = componentValue["pin1"]?.ToString();
                    string pin2 = componentValue["pin2"]?.ToString();

                    // Find which nets the resistor pins connect to
                    bool pin1InNet = nodeToNetMap.ContainsKey(pin1);
                    bool pin2InNet = nodeToNetMap.ContainsKey(pin2);

                    object resistorState;
                    if (pin1InNet && pin2InNet)
                    {
                        int net1Id = nodeToNetMap[pin1];
                        int net2Id = nodeToNetMap[pin2];

                        resistorState = new
                        {
                            pin1 = pin1,
                            pin2 = pin2,
                            pin1State = nets[net1Id].State.ToString(),
                            pin2State = nets[net2Id].State.ToString(),
                            isConnected = true
                        };
                    }
                    else
                    {
                        resistorState = new
                        {
                            pin1 = pin1,
                            pin2 = pin2,
                            pin1State = "UNCONNECTED",
                            pin2State = "UNCONNECTED",
                            isConnected = false
                        };
                    }

                    componentStates[componentKey] = resistorState;
                    continue;
                }

                // For DIP switches, handle realistic behavior(no automatic pull-up)
                if (componentKey.StartsWith("dipSwitch"))
                {
                    bool isOn = componentValue["isOn"] != null ?
                        componentValue["isOn"].Value<bool>() : false;

                    // Get pin information
                    string pin1 = componentValue["pin1"] != null ? componentValue["pin1"].ToString() :
                                  componentValue["inputPin"]?.ToString();

                    string pin2 = componentValue["pin2"] != null ? componentValue["pin2"].ToString() :
                                  componentValue["outputPin"]?.ToString();

                    // DIP switch behavior:
                    // - When ON: pins are connected (handled in BuildGraph)
                    // - When OFF: pins are isolated, no automatic pull-up
                    // - Users must add physical resistors for pull-up/pull-down behavior

                    // Check if pins are in nets for state reporting
                    bool pin1InNet = nodeToNetMap.ContainsKey(pin1);
                    bool pin2InNet = nodeToNetMap.ContainsKey(pin2);

                    object switchState;
                    if (pin1InNet && pin2InNet)
                    {
                        int net1Id = nodeToNetMap[pin1];
                        int net2Id = nodeToNetMap[pin2];

                        // Check if either net is grounded (contains GND nodes)
                        bool pin1IsGrounded = nets[net1Id].Nodes.Any(n => n.Contains("GND"));
                        bool pin2IsGrounded = nets[net2Id].Nodes.Any(n => n.Contains("GND"));
                        bool isGrounded = pin1IsGrounded || pin2IsGrounded;

                        switchState = new
                        {
                            isOn = isOn,
                            isGrounded = isGrounded,
                            pin1State = nets[net1Id].State.ToString(),
                            pin2State = nets[net2Id].State.ToString(),
                            pin1HasResistor = nets[net1Id].SourceComponents.Any(c => c.StartsWith("resistor")),
                            pin2HasResistor = nets[net2Id].SourceComponents.Any(c => c.StartsWith("resistor"))
                        };
                    }
                    else
                    {
                        switchState = new
                        {
                            isOn = isOn,
                            isGrounded = false,
                            pin1State = "UNCONNECTED",
                            pin2State = "UNCONNECTED",
                            pin1HasResistor = false,
                            pin2HasResistor = false
                        };
                    }

                    componentStates[componentKey] = switchState;
                    continue;
                }

                // Find which nets this component's pins connect to
                var connectedNets = GetConnectedNets(componentValue, nodeToNetMap);

                // Evaluate component based on type
                if (componentKey.StartsWith("led"))
                {
                    componentStates[componentKey] = EvaluateLED(connectedNets, nets);
                }
                else if (componentKey.StartsWith("sevenSeg"))
                {
                    componentStates[componentKey] = EvaluateSevenSegment(connectedNets, nets);
                }
                else if (componentKey.StartsWith("ic"))
                {
                    // ICs can change net states
                    var result = EvaluateIC(componentValue, connectedNets, nets, componentKey);
                    if (result.statesChanged)
                    {
                        changed = true;
                    }
                    componentStates[componentKey] = result.state;
                }
            }
        }

        return componentStates;
    }

    // Evaluate LED component
    private object EvaluateLED(Dictionary<string, int> connectedNets, List<Net> nets)
    {
        // Check if both anode and cathode are connected
        if (!connectedNets.ContainsKey("anode") || !connectedNets.ContainsKey("cathode"))
        {
            return new { isOn = false, error = "Missing connection" };
        }

        // Get nets connected to anode and cathode
        int anodeNetId = connectedNets["anode"];
        int cathodeNetId = connectedNets["cathode"];

        // Check if the LED has proper voltage and ground connections
        bool hasProperVoltage = nets[anodeNetId].State == NodeState.HIGH &&
                               nets[cathodeNetId].State == NodeState.LOW;

        // Check if there's a resistor in the same net as the LED
        bool hasResistor = false;

        // Look for resistors in the components list
        foreach (var component in nets[anodeNetId].SourceComponents)
        {
            if (component.StartsWith("resistor"))
            {
                hasResistor = true;
                break;
            }
        }

        // If no resistor in anode net, check cathode net
        if (!hasResistor)
        {
            foreach (var component in nets[cathodeNetId].SourceComponents)
            {
                if (component.StartsWith("resistor"))
                {
                    hasResistor = true;
                    break;
                }
            }
        }

        // Check if power comes from an IC (which provides current limiting)
        bool poweredByIC = nets[anodeNetId].Source == PowerSource.IC;

        // LED is on when it has proper voltage AND (a resistor OR powered by IC)
        bool isOn = hasProperVoltage && (hasResistor || poweredByIC);

        // Return error message if missing resistor and not powered by IC
        if (hasProperVoltage && !hasResistor && !poweredByIC)
        {
            return new
            {
                isOn = false,
                grounded = nets[cathodeNetId].State == NodeState.LOW,
                error = "LED requires a resistor to function properly when not powered by IC"
            };
        }

        return new
        {
            isOn = isOn,
            grounded = nets[cathodeNetId].State == NodeState.LOW
        };
    }

    // Evaluate Seven-Segment Display
    private object EvaluateSevenSegment(Dictionary<string, int> connectedNets, List<Net> nets)
    {
        var segments = new Dictionary<string, bool>();
        bool isGrounded = false;

        // For each segment (A-G)
        foreach (var segment in new[] { "A", "B", "C", "D", "E", "F", "G", "DP" })
        {
            string nodeKey = "node" + segment;
            bool isOn = false;

            // Check if the segment node and ground nodes exist
            if (connectedNets.ContainsKey(nodeKey) &&
               (connectedNets.ContainsKey("nodeGnd1") || connectedNets.ContainsKey("nodeGnd2")))
            {
                int segNetId = connectedNets[nodeKey];

                // Find connected ground
                if (connectedNets.ContainsKey("nodeGnd1"))
                {
                    int gndNetId1 = connectedNets["nodeGnd1"];
                    isGrounded = nets[gndNetId1].State == NodeState.LOW;
                }
                if (connectedNets.ContainsKey("nodeGnd2"))
                {
                    int gndNetId2 = connectedNets["nodeGnd2"];
                    isGrounded = isGrounded || nets[gndNetId2].State == NodeState.LOW;
                }


                // Segment is on when it's HIGH and GND is LOW
                isOn = nets[segNetId].State == NodeState.HIGH && isGrounded;
            }

            segments[segment] = isOn;
        }

        return new { segments = segments, grounded = isGrounded };
    }

    // Evaluate IC component
    private (bool statesChanged, object state) EvaluateIC(JToken ic,
        Dictionary<string, int> connectedNets, List<Net> nets, string componentId)
    {
        string icType = ic["type"]?.ToString();

        if (string.IsNullOrEmpty(icType))
        {
            return (false, new { status = "Missing IC type" });
        }

        // Verify power connections
        bool hasVcc = connectedNets.ContainsKey("pin16") &&
                     nets[connectedNets["pin16"]].State == NodeState.HIGH;

        bool hasGnd = connectedNets.ContainsKey("pin8") &&
                     nets[connectedNets["pin8"]].State == NodeState.LOW;

        // If power connections aren't correct, the IC doesn't function
        if (!hasVcc || !hasGnd)
        {
            return (false, new
            {
                type = icType,
                hasGnd = hasGnd,
                hasVcc = hasVcc,
                status = "Inactive",
                error = "Power connection issue",
                details = $"IC requires proper power connections. Vcc (pin16): {hasVcc}, GND (pin8): {hasGnd}"
            });
        }

        switch (icType)
        {
            case "IC7448":
                return EvaluateIC7448(connectedNets, nets, componentId);
            case "IC74138":
                return EvaluateIC74138(connectedNets, nets, componentId);
            case "IC74148":
                return EvaluateIC74148(connectedNets, nets, componentId);
            default:
                return (false, new { type = icType, status = "unknown" });
        }
    }

    // Evaluate 7448 BCD to 7-segment decoder IC
    private (bool statesChanged, object state) EvaluateIC7448(
        Dictionary<string, int> connectedNets, List<Net> nets, string componentId)
    {
        bool statesChanged = false;

        // Check if BCD input pins are connected
        if (!connectedNets.ContainsKey("pin7") ||  // A input
            !connectedNets.ContainsKey("pin1") ||  // B input
            !connectedNets.ContainsKey("pin2") ||  // C input
            !connectedNets.ContainsKey("pin6"))    // D input
        {
            return (false, new { type = "IC7448", error = "Missing BCD input pins" });
        }

        // Get lamp test, blanking and ripple blanking inputs
        bool lt = !connectedNets.ContainsKey("pin3") || nets[connectedNets["pin3"]].State == NodeState.HIGH;
        bool bi_rbo = !connectedNets.ContainsKey("pin4") || nets[connectedNets["pin4"]].State == NodeState.HIGH;
        bool rbi = !connectedNets.ContainsKey("pin5") || nets[connectedNets["pin5"]].State == NodeState.HIGH;

        // Get BCD input values
        bool inputA = nets[connectedNets["pin7"]].State == NodeState.HIGH;
        bool inputB = nets[connectedNets["pin1"]].State == NodeState.HIGH;
        bool inputC = nets[connectedNets["pin2"]].State == NodeState.HIGH;
        bool inputD = nets[connectedNets["pin6"]].State == NodeState.HIGH;

        // Only proceed if inputs are initialized
        if (nets[connectedNets["pin7"]].State == NodeState.UNINITIALIZED ||
            nets[connectedNets["pin1"]].State == NodeState.UNINITIALIZED ||
            nets[connectedNets["pin2"]].State == NodeState.UNINITIALIZED ||
            nets[connectedNets["pin6"]].State == NodeState.UNINITIALIZED)
        {
            return (false, new
            {
                type = "IC7448",
                status = "Uninitialized inputs",
                control = new { LT = lt, BI_RBO = bi_rbo, RBI = rbi },
            });
        }

        // Convert binary to decimal (0-15)
        int value = (inputD ? 8 : 0) + (inputC ? 4 : 0) + (inputB ? 2 : 0) + (inputA ? 1 : 0);

        // Get the 7-segment output pattern for this value
        var segments = Get7SegmentPattern(value);

        // Apply blanking if needed
        if (!lt || !bi_rbo || (value == 0 && !rbi))
        {
            // Blank the display (all segments off)
            segments = new Dictionary<string, bool>
            {
                {"A", false}, {"B", false}, {"C", false},
                {"D", false}, {"E", false}, {"F", false}, {"G", false}
            };
        }

        // Map segments to IC pins
        var pinMap = new Dictionary<string, string> {
            {"A", "pin13"}, {"B", "pin12"}, {"C", "pin11"},
            {"D", "pin10"}, {"E", "pin9"}, {"F", "pin15"}, {"G", "pin14"}
        };

        // Update output pins
        foreach (var kvp in pinMap)
        {
            string segment = kvp.Key;
            string pin = kvp.Value;

            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                NodeState newState = segments[segment] ? NodeState.HIGH : NodeState.LOW;

                // Only update if state changed
                if (nets[netId].State != newState)
                {
                    nets[netId].State = newState;
                    nets[netId].Source = PowerSource.IC;
                    nets[netId].SourceComponent = componentId;
                    statesChanged = true;
                }
            }
        }

        return (statesChanged, new
        {
            type = "IC7448",
            status = "Initialized",
            inputs = new { A = inputA, B = inputB, C = inputC, D = inputD },
            control = new { LT = lt, BI_RBO = bi_rbo, RBI = rbi },
            outputs = segments,
            value = value
        });
    }

    // Evaluate 74138 3-to-8 line decoder IC
    private (bool statesChanged, object state) EvaluateIC74138(
        Dictionary<string, int> connectedNets, List<Net> nets, string componentId)
    {
        bool statesChanged = false;

        // Check if address pins are connected
        if (!connectedNets.ContainsKey("pin1") ||  // A0
            !connectedNets.ContainsKey("pin2") ||  // A1
            !connectedNets.ContainsKey("pin3"))    // A2
        {
            return (false, new { type = "IC74138", error = "Missing address pins" });
        }

        // Get address inputs (A0, A1, A2)
        bool a0 = nets[connectedNets["pin1"]].State == NodeState.HIGH;
        bool a1 = nets[connectedNets["pin2"]].State == NodeState.HIGH;
        bool a2 = nets[connectedNets["pin3"]].State == NodeState.HIGH;

        // Get enable inputs (E1, E2, E3) with defaults
        bool e1 = !connectedNets.ContainsKey("pin4") || nets[connectedNets["pin4"]].State == NodeState.LOW;
        bool e2 = !connectedNets.ContainsKey("pin5") || nets[connectedNets["pin5"]].State == NodeState.LOW;
        bool e3 = connectedNets.ContainsKey("pin6") && nets[connectedNets["pin6"]].State == NodeState.HIGH;

        // Calculate binary address (0-7)
        int address = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);

        // Initialize all outputs as HIGH
        var outputs = new Dictionary<string, bool>();
        for (int i = 0; i < 8; i++)
        {
            outputs["O" + i] = false;
        }

        // Set active output if enabled (E1 high, E2 and E3 low)
        bool enabled = e1 && e2 && e3;
        if (enabled)
        {
            outputs["O" + address] = true;
        }

        // Output pin mapping
        var outputPins = new Dictionary<string, string> {
            {"O0", "pin15"}, {"O1", "pin14"}, {"O2", "pin13"}, {"O3", "pin12"},
            {"O4", "pin11"}, {"O5", "pin10"}, {"O6", "pin9"}, {"O7", "pin7"}
        };

        // Update output pins
        foreach (var kvp in outputPins)
        {
            string output = kvp.Key;
            string pin = kvp.Value;

            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                NodeState newState = outputs[output] ? NodeState.HIGH : NodeState.LOW;

                // Only update if state changed
                if (nets[netId].State != newState)
                {
                    nets[netId].State = newState;
                    nets[netId].Source = PowerSource.IC;
                    nets[netId].SourceComponent = componentId;
                    statesChanged = true;
                }
            }
        }

        return (statesChanged, new
        {
            type = "IC74138",
            status = "Initialized",
            hasGnd = true,
            hasVcc = true,
            address = new { A0 = a0, A1 = a1, A2 = a2 },
            enable = new { E1 = e1, E2 = e2, E3 = e3 },
            outputs = outputs,
            enabled = enabled
        });
    }

    // Evaluate 74148 8-to-3 priority encoder IC
    private (bool statesChanged, object state) EvaluateIC74148(
        Dictionary<string, int> connectedNets, List<Net> nets, string componentId)
    {
        bool statesChanged = false;

        // Get enable input (EI) - active LOW
        bool ei = !connectedNets.ContainsKey("pin5") ||
                  nets[connectedNets["pin5"]].State == NodeState.LOW;

        // Initialize inputs array (all HIGH/inactive by default)
        bool[] inputs = new bool[8];
        for (int i = 0; i < 8; i++)
        {
            inputs[i] = true;  // Default to inactive (HIGH)
        }

        // Get input states (active LOW)
        // Input 0 (pin 10)
        if (connectedNets.ContainsKey("pin10"))
            inputs[0] = nets[connectedNets["pin10"]].State == NodeState.LOW;

        // Input 1 (pin 11)
        if (connectedNets.ContainsKey("pin11"))
            inputs[1] = nets[connectedNets["pin11"]].State == NodeState.LOW;

        // Input 2 (pin 12)
        if (connectedNets.ContainsKey("pin12"))
            inputs[2] = nets[connectedNets["pin12"]].State == NodeState.LOW;

        // Input 3 (pin 13)
        if (connectedNets.ContainsKey("pin13"))
            inputs[3] = nets[connectedNets["pin13"]].State == NodeState.LOW;

        // Input 4 (pin 1)
        if (connectedNets.ContainsKey("pin1"))
            inputs[4] = nets[connectedNets["pin1"]].State == NodeState.LOW;

        // Input 5 (pin 2)
        if (connectedNets.ContainsKey("pin2"))
            inputs[5] = nets[connectedNets["pin2"]].State == NodeState.LOW;

        // Input 6 (pin 3)
        if (connectedNets.ContainsKey("pin3"))
            inputs[6] = nets[connectedNets["pin3"]].State == NodeState.LOW;

        // Input 7 (pin 4)
        if (connectedNets.ContainsKey("pin4"))
            inputs[7] = nets[connectedNets["pin4"]].State == NodeState.LOW;

        // Find highest priority active input (7 is highest, 0 is lowest)
        int highestActive = -1;
        for (int i = 7; i >= 0; i--)
        {
            if (!inputs[i])  // Active LOW, so !input means active
            {
                highestActive = i;
                break;
            }
        }

        // Calculate outputs (all active LOW)
        bool a0, a1, a2, gs, e0;

        // If EI is inactive (HIGH) or no input is active
        if (!ei || highestActive == -1)
        {
            a0 = a1 = a2 = false;
            gs = true;
            e0 = false;
        }
        else
        {
            // Encode priority input to binary (complement of bits)
            a0 = (highestActive & 1) != 0;  // Bit 0
            a1 = (highestActive & 2) != 0;  // Bit 1
            a2 = (highestActive & 4) != 0;  // Bit 2
            gs = false;                     // GS LOW (active)
            e0 = true;                      // E0 HIGH (inactive)
        }

        // Output pins
        var outputs = new Dictionary<string, (string pin, bool value)>
        {
            {"A0", ("pin9", a0)},
            {"A1", ("pin7", a1)},
            {"A2", ("pin6", a2)},
            {"GS", ("pin14", gs)},
            {"E0", ("pin15", e0)}
        };

        // Update output pins
        foreach (var kvp in outputs)
        {
            string pinName = kvp.Value.pin;
            bool outputValue = kvp.Value.value;

            if (connectedNets.ContainsKey(pinName))
            {
                int netId = connectedNets[pinName];
                NodeState newState = outputValue ? NodeState.HIGH : NodeState.LOW;

                // Only update if state changed
                if (nets[netId].State != newState)
                {
                    nets[netId].State = newState;
                    nets[netId].Source = PowerSource.IC;
                    nets[netId].SourceComponent = componentId;
                    statesChanged = true;
                }
            }
        }

        return (statesChanged, new
        {
            type = "IC74148",
            inputsState = inputs,
            enableIn = ei,
            highestPriority = highestActive,
            outputs = new { A0 = a0, A1 = a1, A2 = a2, GS = gs, E0 = e0 }
        });
    }

    // Get 7-segment pattern for a given value (0-15)
    private Dictionary<string, bool> Get7SegmentPattern(int value)
    {
        // Patterns for 0-9 and A-F
        string[][] patterns = new string[][] {
            // 0-9
            new string[] {"A", "B", "C", "D", "E", "F"},      // 0
            new string[] {"B", "C"},                          // 1
            new string[] {"A", "B", "D", "E", "G"},           // 2
            new string[] {"A", "B", "C", "D", "G"},           // 3
            new string[] {"B", "C", "F", "G"},                // 4
            new string[] {"A", "C", "D", "F", "G"},           // 5
            new string[] {"A", "C", "D", "E", "F", "G"},      // 6
            new string[] {"A", "B", "C"},                     // 7
            new string[] {"A", "B", "C", "D", "E", "F", "G"}, // 8
            new string[] {"A", "B", "C", "D", "F", "G"},      // 9
            // A-F
            new string[] {"A", "B", "C", "E", "F", "G"},      // A
            new string[] {"C", "D", "E", "F", "G"},           // b
            new string[] {"A", "D", "E", "F"},                // C
            new string[] {"B", "C", "D", "E", "G"},           // d
            new string[] {"A", "D", "E", "F", "G"},           // E
            new string[] {"A", "E", "F", "G"}                 // F
        };

        // Ensure value is in range
        value = Math.Max(0, Math.Min(15, value));

        // Build the pattern
        var result = new Dictionary<string, bool>
        {
            {"A", false}, {"B", false}, {"C", false},
            {"D", false}, {"E", false}, {"F", false}, {"G", false}
        };

        foreach (string segment in patterns[value])
        {
            result[segment] = true;
        }

        return result;
    }

    // Current experiment state
    public int CurrentExperimentId { get; set; } = 1;
    public int CurrentInstructionIndex { get; set; } = 0;

    // Track completed instructions
    private Dictionary<int, HashSet<int>> _completedInstructions = new Dictionary<int, HashSet<int>>();

    // Experiment definitions
    private Dictionary<int, ExperimentDefinition> _experiments;

    private void InitializeExperiments()
    {
        _experiments = new Dictionary<int, ExperimentDefinition>();

        // Decoder experiment (Experiment 1)
        var decoderExperiment = new ExperimentDefinition
        {
            Id = 1,
            Name = "IC 74138 Decoder",
            Description = "Lol",
            TotalInstructions = 35
        };

        // Detailed step-by-step instructions for IC 74138
        decoderExperiment.InstructionDescriptions[0] = "Add a resistor and connect it to any power terminal";
        decoderExperiment.InstructionDescriptions[1] = "Add a switch and connect it in series with the resistor (note: dedicate a node between resistor and switch - this will act as input 1)";
        decoderExperiment.InstructionDescriptions[2] = "Ground the switch";
        decoderExperiment.InstructionDescriptions[3] = "Add a second resistor and connect it to any power terminal";
        decoderExperiment.InstructionDescriptions[4] = "Add a second switch and connect it in series with the second resistor (note: dedicate a node between resistor and switch - this will act as input 2)";
        decoderExperiment.InstructionDescriptions[5] = "Ground the second switch";
        decoderExperiment.InstructionDescriptions[6] = "Add a third resistor and connect it to any power terminal";
        decoderExperiment.InstructionDescriptions[7] = "Add a third switch and connect it in series with the third resistor (note: dedicate a node between resistor and switch - this will act as input 3)";
        decoderExperiment.InstructionDescriptions[8] = "Ground the third switch";
        decoderExperiment.InstructionDescriptions[9] = "Ground IC74138 to Ground Breadboard";
        decoderExperiment.InstructionDescriptions[10] = "VCC IC74138 to VCC Breadboard";
        decoderExperiment.InstructionDescriptions[11] = "IC74138 A0 to input 1 from pull-up resistor input";
        decoderExperiment.InstructionDescriptions[12] = "IC74138 A1 to input 2 from pull-up resistor input";
        decoderExperiment.InstructionDescriptions[13] = "IC74138 A2 to input 3 from pull-up resistor input";
        decoderExperiment.InstructionDescriptions[14] = "E1 set to LOW";
        decoderExperiment.InstructionDescriptions[15] = "E2 set to LOW";
        decoderExperiment.InstructionDescriptions[16] = "E3 set to HIGH";
        decoderExperiment.InstructionDescriptions[17] = "Add 8 LEDs";
        decoderExperiment.InstructionDescriptions[18] = "Connect LEDs cathode to ground";
        decoderExperiment.InstructionDescriptions[19] = "Connect O0 to a LED";
        decoderExperiment.InstructionDescriptions[20] = "Connect O1 to a LED";
        decoderExperiment.InstructionDescriptions[21] = "Connect O2 to a LED";
        decoderExperiment.InstructionDescriptions[22] = "Connect O3 to a LED";
        decoderExperiment.InstructionDescriptions[23] = "Connect O4 to a LED";
        decoderExperiment.InstructionDescriptions[24] = "Connect O5 to a LED";
        decoderExperiment.InstructionDescriptions[25] = "Connect O6 to a LED";
        decoderExperiment.InstructionDescriptions[26] = "Connect O7 to a LED";
        decoderExperiment.InstructionDescriptions[27] = "Set inputs to 000 (A2=0, A1=0, A0=0)";
        decoderExperiment.InstructionDescriptions[28] = "Set inputs to 001 (A2=0, A1=0, A0=1)";
        decoderExperiment.InstructionDescriptions[29] = "Set inputs to 010 (A2=0, A1=1, A0=0)";
        decoderExperiment.InstructionDescriptions[30] = "Set inputs to 011 (A2=0, A1=1, A0=1)";
        decoderExperiment.InstructionDescriptions[31] = "Set inputs to 100 (A2=1, A1=0, A0=0)";
        decoderExperiment.InstructionDescriptions[32] = "Set inputs to 101 (A2=1, A1=0, A0=1)";
        decoderExperiment.InstructionDescriptions[33] = "Set inputs to 110 (A2=1, A1=1, A0=0)";
        decoderExperiment.InstructionDescriptions[34] = "Set inputs to 111 (A2=1, A1=1, A0=1)";

        _experiments[decoderExperiment.Id] = decoderExperiment;
        _completedInstructions[decoderExperiment.Id] = new HashSet<int>();

        _experiments[decoderExperiment.Id] = decoderExperiment;
        _completedInstructions[decoderExperiment.Id] = new HashSet<int>();


        // BCD to 7-Segment experiment (Experiment 2)
        var bcdExperiment = new ExperimentDefinition
        {
            Id = 2,
            Name = "BCD to 7-Segment Display",
            Description = "Connect DIP switches to a 7448 IC to display decimal digits on a 7-segment display.",
            TotalInstructions = 16
        };

        // Add instructions for displaying 0-9 and A-F
        for (int i = 0; i < 16; i++)
        {
            string binary = Convert.ToString(i, 2).PadLeft(4, '0');
            string display = i < 10 ? i.ToString() : ((char)('A' + (i - 10))).ToString();
            bcdExperiment.InstructionDescriptions[i] = $"Set BCD input to {binary} to display {display}";
        }

        _experiments[bcdExperiment.Id] = bcdExperiment;
        _completedInstructions[bcdExperiment.Id] = new HashSet<int>();

        // Encoder Experiment (Experiment 3)
        var encoderExperiment = new ExperimentDefinition
        {
            Id = 3,
            Name = "IC 74148 Encoder",
            Description = "Lmao",
            TotalInstructions = 8
        };

        // Add instructions for each input test
        encoderExperiment.InstructionDescriptions[0] = "Turn on all inputs (A0-A7)";
        encoderExperiment.InstructionDescriptions[1] = "Turn off only input A1, keep others on";
        encoderExperiment.InstructionDescriptions[2] = "Turn off only input A2, keep others on";
        encoderExperiment.InstructionDescriptions[3] = "Turn off only input A3, keep others on";
        encoderExperiment.InstructionDescriptions[4] = "Turn off only input A4, keep others on";
        encoderExperiment.InstructionDescriptions[5] = "Turn off only input A5, keep others on";
        encoderExperiment.InstructionDescriptions[6] = "Turn off only input A6, keep others on";
        encoderExperiment.InstructionDescriptions[7] = "Turn off only input A7, keep others on";

        _experiments[encoderExperiment.Id] = encoderExperiment;
        _completedInstructions[encoderExperiment.Id] = new HashSet<int>();
    }

    public ExperimentDefinition GetCurrentExperiment()
    {
        return _experiments.TryGetValue(CurrentExperimentId, out var experiment) ? experiment : null;
    }

    // Dictionary to store the last instruction index for each experiment
    private Dictionary<int, int> _lastInstructionIndices = new Dictionary<int, int>();

    public void NextExperiment()
    {
        // Save current instruction index for the current experiment
        _lastInstructionIndices[CurrentExperimentId] = CurrentInstructionIndex;

        // Find the next valid experiment ID
        int nextExperimentId = CurrentExperimentId + 1;
        while (nextExperimentId <= _experiments.Keys.Max() && !_experiments.ContainsKey(nextExperimentId))
        {
            nextExperimentId++;
        }

        // If we found a valid next experiment
        if (_experiments.ContainsKey(nextExperimentId))
        {
            CurrentExperimentId = nextExperimentId;

            // Restore the last instruction index for this experiment, or default to 0
            CurrentInstructionIndex = _lastInstructionIndices.ContainsKey(CurrentExperimentId)
                ? _lastInstructionIndices[CurrentExperimentId]
                : 0;
        }
    }

    public void PreviousExperiment()
    {
        // Save current instruction index for the current experiment
        _lastInstructionIndices[CurrentExperimentId] = CurrentInstructionIndex;

        // Find the previous valid experiment ID
        int prevExperimentId = CurrentExperimentId - 1;
        while (prevExperimentId >= _experiments.Keys.Min() && !_experiments.ContainsKey(prevExperimentId))
        {
            prevExperimentId--;
        }

        // If we found a valid previous experiment
        if (_experiments.ContainsKey(prevExperimentId))
        {
            CurrentExperimentId = prevExperimentId;

            // Restore the last instruction index for this experiment, or default to 0
            CurrentInstructionIndex = _lastInstructionIndices.ContainsKey(CurrentExperimentId)
                ? _lastInstructionIndices[CurrentExperimentId]
                : 0;
        }
    }

    public void NextInstruction()
    {
        var experiment = GetCurrentExperiment();
        if (experiment != null && CurrentInstructionIndex < experiment.TotalInstructions - 1)
        {
            CurrentInstructionIndex++;
        }
    }

    public void PreviousInstruction()
    {
        if (CurrentInstructionIndex > 0)
        {
            CurrentInstructionIndex--;
        }
    }

    public ExperimentResult EvaluateExperiment(SimulationResult simResult, JToken components)
    {
        var experiment = GetCurrentExperiment();
        if (experiment == null)
        {
            return new ExperimentResult
            {
                ExperimentId = CurrentExperimentId,
                Messages = new List<string> { $"Unknown experiment ID: {CurrentExperimentId}" },
                MainInstruction = "Error: Invalid experiment selected",
                IsSetupValid = false
            };
        }

        switch (CurrentExperimentId)
        {
            case 1:
                return Evaluate74138To8LED(simResult, experiment, components);
            case 2:
                return EvaluateBCDTo7SegmentExperiment(simResult, experiment, components);
            case 3:
                return Evaluate74148To3LED(simResult, experiment, components);
            default:
                return new ExperimentResult
                {
                    ExperimentId = CurrentExperimentId,
                    Messages = new List<string> { $"No evaluation implemented for experiment {CurrentExperimentId}" },
                    MainInstruction = "Error: Experiment not implemented",
                    IsSetupValid = false
                };
        }
    }

    private ExperimentResult Evaluate74148To3LED(
    SimulationResult simResult,
    ExperimentDefinition experiment,
    JToken components)
    {
        var result = new ExperimentResult
        {
            ExperimentName = experiment.Name,
            ExperimentId = experiment.Id,
            TotalInstructions = experiment.TotalInstructions,
            MainInstruction = experiment.InstructionDescriptions[CurrentInstructionIndex],
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };

        // Count components and validate types
        int ic74148Count = 0;
        int ledCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        List<string> mainLeds = new List<string>();

        // Get component data
        dynamic ic74148State = null;
        List<dynamic> ledStates = new List<dynamic>();

        foreach (var comp in simResult.ComponentStates)
        {
            if (comp.Key.StartsWith("ic"))
            {
                dynamic dynamicValue = comp.Value;
                string typeValue = dynamicValue.type;

                if (typeValue == "IC74148")
                {
                    ic74148Count++;
                    ic74148State = dynamicValue;
                    mainIc = comp.Key;
                }
            }
            else if (comp.Key.StartsWith("led"))
            {
                dynamic dynamicValue = comp.Value;
                ledCount++;
                mainLeds.Add(comp.Key);
                ledStates.Add(comp.Value);
            }
            else if (comp.Key.StartsWith("dipSwitch"))
            {
                dipSwitchCount++;
            }
        }

        // Validate component counts
        if (ic74148Count != 1)
        {
            result.Messages.Add($"Expected exactly 1 IC 74148, found {ic74148Count}");
            result.IsSetupValid = false;
        }

        if (ledCount < 3)
        {
            result.Messages.Add($"Expected at least 3 LEDs, found {ledCount}");
            result.IsSetupValid = false;
        }

        if (dipSwitchCount < 8)
        {
            result.Messages.Add($"Expected at least 8 DIP switches, found {dipSwitchCount}");
            result.IsSetupValid = false;
        }

        // Only proceed with detailed checks if basic components are present
        if (ic74148State != null && ledCount > 2 && dipSwitchCount > 7)
        {

            // Check IC 74148 control pins (ei, gs, e0)
            bool eiActive = false;
            bool gsActive = false;
            bool e0Active = false;

            try
            {
                eiActive = ic74148State.enableIn;
                gsActive = ic74148State.outputs.GS;
                e0Active = ic74148State.outputs.E0;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot access IC 74148 enable pins");
                result.IsSetupValid = false;
            }

            // Add individual messages for each enable pin
            if (!eiActive)
            {
                result.Messages.Add("IC 74148 EI pin must be set to LOW");
                result.IsSetupValid = false;
            }

            // Check if all LEDs are grounded
            bool ledsGrounded = true;
            foreach (dynamic ledState in ledStates)
            {
                if (!ledState.grounded)
                {
                    ledsGrounded = false;
                    break;
                }
            }


            if (!ledsGrounded)
            {
                result.Messages.Add("LEDS are not properly grounded");
                result.IsSetupValid = false;
            }

            // Check if IC has uninitialized inputs
            string status = "";
            try
            {
                status = ic74148State.status;

                if (status == "Uninitialized inputs")
                {
                    result.Messages.Add("IC 74148 has uninitialized inputs. Check DIP switch connections.");
                    result.IsSetupValid = false;
                }
            }
            catch (Exception)
            {
                Debug.Log("No status");
            }

            // Extract ic
            JToken icComp = null;
            List<JToken> ledComps = new List<JToken>();
            List<JToken> ledsConnectedToIc = new List<JToken>();

            foreach (JProperty componentProp in components)
            {
                if (componentProp.Name.Equals(mainIc))
                {
                    icComp = componentProp.Value;
                }
                else if (componentProp.Name.StartsWith("led"))
                {
                    ledComps.Add(componentProp.Value);
                }
            }

            // Check if required components are found
            if (icComp == null)
            {
                result.Messages.Add("Missing IC");
                result.IsSetupValid = false;
            }

            string[] outputPins = new string[] {
                "pin6", "pin7", "pin9"
            };

            // Check all required connections
            bool allConnected = true;

            foreach (string pin in outputPins)
            {
                foreach (JToken ledComp in ledComps)
                {
                    string pinValue = icComp[pin]?.ToString();
                    string anodeValue = ledComp["anode"]?.ToString();

                    bool isConnected = AreNodesConnected(pinValue, anodeValue, simResult.Nets);
                    if (isConnected)
                    {
                        ledsConnectedToIc.Add(ledComp["ledId"]?.ToString());
                    }
                }
            }

            if (ledsConnectedToIc.Count != 3) allConnected = false;

            if (!allConnected)
            {
                result.Messages.Add("IC74148 outputs are not properly connected to LEDs.");
                result.IsSetupValid = false;
            }
        }

        // Only evaluate the experiment if the setup is valid
        if (result.IsSetupValid)
        {
            bool[] inputsState = ic74148State.inputsState;
            int iState = 1;

            foreach (bool state in inputsState)
            {
                Debug.Log($"{iState}: {state}");
                iState++;
            }

            int expectedValue = -1;

            for (int i = 7; i >= 0; i--)
            {
                if (inputsState[i])  // Active LOW, so input means active
                {
                    expectedValue = i;
                    break;
                }
            }
            bool[] expectedBits = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                expectedBits[i] = ((expectedValue >> i) & 1) == 1; // converts the highest priority into its 3-bit binary rep.
            }


            // Get actual IC inputs
            int highestPriority = -1;
            try
            {
                highestPriority = ic74148State.highestPriority;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot evaluate: IC inputs not found in simulation result");
                result.InstructionResults[CurrentInstructionIndex] = false;
                return result;
            }

            // // Compare expected and actual inputs
            // bool[] actualBits = new bool[3];
            bool allMatch = false;

            Debug.Log($"highestPriority: {highestPriority}, CurrentInstructionIndex: {CurrentInstructionIndex}");
            if (CurrentInstructionIndex == 0 && highestPriority == -1) allMatch = true;
            if (CurrentInstructionIndex == highestPriority && CurrentExperimentId != 0) allMatch = true;

            result.InstructionResults[CurrentInstructionIndex] = allMatch;

            if (allMatch)
            {
                Debug.Log("PASSED!!!");
                // Mark instruction as completed
                _completedInstructions[experiment.Id].Add(CurrentInstructionIndex);
                result.CompletedInstructions = _completedInstructions[experiment.Id].Count;

                NextInstruction();
            }
        }

        return result;
    }

    private ExperimentResult EvaluateBCDTo7SegmentExperiment(
        SimulationResult simResult,
        ExperimentDefinition experiment,
        JToken components)
    {
        var result = new ExperimentResult
        {
            ExperimentName = experiment.Name,
            ExperimentId = experiment.Id,
            TotalInstructions = experiment.TotalInstructions,
            MainInstruction = experiment.InstructionDescriptions[CurrentInstructionIndex],
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };

        // Count components and validate types
        int ic7448Count = 0;
        int sevenSegCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        string mainSevenSeg = "";

        // Get component data
        dynamic ic7448State = null;
        dynamic sevenSegState = null;

        foreach (var comp in simResult.ComponentStates)
        {
            if (comp.Key.StartsWith("ic"))
            {
                dynamic dynamicValue = comp.Value;
                string typeValue = dynamicValue.type;

                if (typeValue == "IC7448")
                {
                    ic7448Count++;
                    ic7448State = dynamicValue;
                    mainIc = comp.Key;
                }
            }
            else if (comp.Key.StartsWith("sevenSeg"))
            {
                sevenSegCount++;
                sevenSegState = comp.Value;
                mainSevenSeg = comp.Key;
            }
            else if (comp.Key.StartsWith("dipSwitch"))
            {
                dipSwitchCount++;
            }
        }

        // Validate component counts
        if (ic7448Count != 1)
        {
            result.Messages.Add($"Expected exactly 1 IC 7448, found {ic7448Count}");
            result.IsSetupValid = false;
        }

        if (sevenSegCount != 1)
        {
            result.Messages.Add($"Expected exactly 1 seven-segment display, found {sevenSegCount}");
            result.IsSetupValid = false;
        }

        if (dipSwitchCount < 4)
        {
            result.Messages.Add($"Expected at least 4 DIP switches, found {dipSwitchCount}");
            result.IsSetupValid = false;
        }

        // Only proceed with detailed checks if basic components are present
        if (ic7448State != null && sevenSegState != null)
        {
            // Check if 7-segment is grounded
            bool sevenSegGrounded = false;
            try
            {
                sevenSegGrounded = sevenSegState.grounded;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot access seven-segment grounding status");
                result.IsSetupValid = false;
            }

            if (!sevenSegGrounded)
            {
                result.Messages.Add("Seven-segment display is not properly grounded");
                result.IsSetupValid = false;
            }

            // Check IC 7448 control pins (LT, BI_RBO, RBI)
            bool ltActive = false;
            bool biRboActive = false;
            bool rbiActive = false;

            try
            {
                ltActive = ic7448State.control.LT;
                biRboActive = ic7448State.control.BI_RBO;
                rbiActive = ic7448State.control.RBI;
            }
            catch (Exception)
            {
                result.IsSetupValid = false;
            }

            // Add individual messages for each control pin
            if (!ltActive)
            {
                result.Messages.Add("IC 7448 LT pin must be set to HIGH");
                result.IsSetupValid = false;
            }

            if (!biRboActive)
            {
                result.Messages.Add("IC 7448 BI_RBO pin must be set to HIGH");
                result.IsSetupValid = false;
            }

            if (!rbiActive)
            {
                result.Messages.Add("IC 7448 RBI pin must be set to HIGH");
                result.IsSetupValid = false;
            }

            // Check if IC has uninitialized inputs
            string status = "";
            try
            {
                status = ic7448State.status;

                if (status == "Uninitialized inputs")
                {
                    result.Messages.Add("IC 7448 has uninitialized inputs. Check DIP switch connections.");
                    result.IsSetupValid = false;
                }
            }
            catch (Exception)
            {
                Debug.Log("No status");
            }

            // Extract ic1 and sevenSeg1 objects
            JToken icComp = null;
            JToken sevenSegComp = null;

            foreach (JProperty componentProp in components)
            {
                if (componentProp.Name.Equals(mainIc))
                {
                    icComp = componentProp.Value;
                }
                else if (componentProp.Name.Equals(mainSevenSeg))
                {
                    sevenSegComp = componentProp.Value;
                }
            }

            // Check if required components are found
            if (icComp == null || sevenSegComp == null)
            {
                result.Messages.Add("Missing IC and Seven Segment components.");
                result.IsSetupValid = false;
            }

            // Define all the connections that need to be checked
            Dictionary<string, string> connectionChecks = new Dictionary<string, string>
                {
                    { "pin13", "nodeA" },
                    { "pin12", "nodeB" },
                    { "pin11", "nodeC" },
                    { "pin10", "nodeD" },
                    { "pin9", "nodeE" },
                    { "pin14", "nodeG" },
                    { "pin15", "nodeF" }
                };

            // Check all required connections
            bool allConnected = true;

            foreach (var check in connectionChecks)
            {
                string pinKey = check.Key;
                string nodeKey = check.Value;

                string pinValue = icComp[pinKey]?.ToString();
                string nodeValue = sevenSegComp[nodeKey]?.ToString();

                if (string.IsNullOrEmpty(pinValue) || string.IsNullOrEmpty(nodeValue))
                {
                    allConnected = false;
                    continue;
                }

                if (!AreNodesConnected(pinValue, nodeValue, simResult.Nets))
                {
                    allConnected = false;
                }
            }

            if (!allConnected)
            {
                result.Messages.Add("IC7448 outputs are not properly connected to Seven Segment inputs.");
                result.IsSetupValid = false;
            }
        }

        // Only evaluate the experiment if the setup is valid
        if (result.IsSetupValid)
        {
            // Get expected BCD input for current instruction (0000 to 1111)
            bool[] expectedBits = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                expectedBits[i] = ((CurrentInstructionIndex >> i) & 1) == 1;
            }

            // Get actual IC inputs
            bool inputA = false, inputB = false, inputC = false, inputD = false;
            try
            {
                inputA = ic7448State.inputs.A;
                inputB = ic7448State.inputs.B;
                inputC = ic7448State.inputs.C;
                inputD = ic7448State.inputs.D;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot evaluate: IC inputs not found in simulation result");
                result.InstructionResults[CurrentInstructionIndex] = false;
                return result;
            }

            // Compare expected and actual inputs
            bool[] actualBits = new[] { inputA, inputB, inputC, inputD };
            bool allMatch = true;

            for (int i = 0; i < 4; i++)
            {
                if (actualBits[i] != expectedBits[i])
                {
                    allMatch = false;
                    break;
                }
            }

            result.InstructionResults[CurrentInstructionIndex] = allMatch;

            if (allMatch)
            {
                // Mark instruction as completed
                _completedInstructions[experiment.Id].Add(CurrentInstructionIndex);
                result.CompletedInstructions = _completedInstructions[experiment.Id].Count;

                NextInstruction();
            }

        }

        return result;
    }

    private ExperimentResult Evaluate74138To8LED(
        SimulationResult simResult,
        ExperimentDefinition experiment,
        JToken components)
    {
        var result = new ExperimentResult
        {
            ExperimentName = experiment.Name,
            ExperimentId = experiment.Id,
            TotalInstructions = experiment.TotalInstructions,
            MainInstruction = experiment.InstructionDescriptions[CurrentInstructionIndex],
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };

        // Count components for validation
        int resistorCount = 0;
        int dipSwitchCount = 0;
        int ic74138Count = 0;
        int ledCount = 0;

        // Component references
        string mainIc = "";
        dynamic ic74138State = null;
        List<string> resistorIds = new List<string>();
        List<string> switchIds = new List<string>();
        List<string> ledIds = new List<string>();

        Debug.Log($"Component states: {JsonConvert.SerializeObject(simResult.ComponentStates, Formatting.Indented)}");

        // Count and collect components
        foreach (var comp in simResult.ComponentStates)
        {
            Debug.Log($"Component: {comp}");
            if (comp.Key.StartsWith("resistor"))
            {
                resistorCount++;
                resistorIds.Add(comp.Key);
            }
            else if (comp.Key.StartsWith("dipSwitch"))
            {
                dipSwitchCount++;
                switchIds.Add(comp.Key);
            }
            else if (comp.Key.StartsWith("ic"))
            {
                dynamic dynamicValue = comp.Value;
                string typeValue = dynamicValue.type;
                if (typeValue == "IC74138")
                {
                    ic74138Count++;
                    ic74138State = dynamicValue;
                    mainIc = comp.Key;
                }
            }
            else if (comp.Key.StartsWith("led"))
            {
                ledCount++;
                ledIds.Add(comp.Key);
            }
        }

        // Helper function to check if instruction requirements are met
        bool CheckInstructionRequirements(int instructionIndex)
        {
            switch (instructionIndex)
            {
                case 0: // "Add a resistor and connect it to any power terminal"
                    if (resistorCount >= 1)
                    {
                        foreach (string resistorId in resistorIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(resistorId))
                            {
                                dynamic resistorState = simResult.ComponentStates[resistorId];
                                string pin1 = resistorState.pin1;
                                string pin2 = resistorState.pin2;
                                if ((pin1 != null && pin1.Contains("PWR")) || (pin2 != null && pin2.Contains("PWR")))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;

                case 1: // "Add a switch and connect it in series with the resistor"
                    if (resistorCount >= 1 && dipSwitchCount >= 1)
                    {
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool pin1HasResistor = switchState.pin1HasResistor ?? false;
                                bool pin2HasResistor = switchState.pin2HasResistor ?? false;
                                if (pin1HasResistor || pin2HasResistor)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;

                case 2: // "Ground the switch"
                    if (dipSwitchCount >= 1)
                    {
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool isGrounded = switchState.isGrounded ?? false;
                                if (isGrounded)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;

                case 3: // "Add a second resistor and connect it to any power terminal"
                    if (resistorCount >= 2)
                    {
                        int resistorsConnectedToPower = 0;
                        foreach (string resistorId in resistorIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(resistorId))
                            {
                                dynamic resistorState = simResult.ComponentStates[resistorId];
                                string pin1 = resistorState.pin1;
                                string pin2 = resistorState.pin2;
                                if ((pin1 != null && pin1.Contains("PWR")) || (pin2 != null && pin2.Contains("PWR")))
                                {
                                    resistorsConnectedToPower++;
                                }
                            }
                        }
                        return resistorsConnectedToPower >= 2;
                    }
                    return false;

                case 4: // "Add a second switch and connect it in series with the second resistor"
                    if (resistorCount >= 2 && dipSwitchCount >= 2)
                    {
                        int switchesInSeries = 0;
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool pin1HasResistor = switchState.pin1HasResistor ?? false;
                                bool pin2HasResistor = switchState.pin2HasResistor ?? false;
                                if (pin1HasResistor || pin2HasResistor)
                                {
                                    switchesInSeries++;
                                }
                            }
                        }
                        return switchesInSeries >= 2;
                    }
                    return false;

                case 5: // "Ground the second switch"
                    if (dipSwitchCount >= 2)
                    {
                        int switchesGrounded = 0;
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool isGrounded = switchState.isGrounded ?? false;
                                if (isGrounded)
                                {
                                    switchesGrounded++;
                                }
                            }
                        }
                        return switchesGrounded >= 2;
                    }
                    return false;

                case 6: // "Add a third resistor and connect it to any power terminal"
                    if (resistorCount >= 3)
                    {
                        int resistorsConnectedToPower = 0;
                        foreach (string resistorId in resistorIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(resistorId))
                            {
                                dynamic resistorState = simResult.ComponentStates[resistorId];
                                string pin1 = resistorState.pin1;
                                string pin2 = resistorState.pin2;
                                if ((pin1 != null && pin1.Contains("PWR")) || (pin2 != null && pin2.Contains("PWR")))
                                {
                                    resistorsConnectedToPower++;
                                }
                            }
                        }
                        return resistorsConnectedToPower >= 3;
                    }
                    return false;

                case 7: // "Add a third switch and connect it in series with the third resistor"
                    if (resistorCount >= 3 && dipSwitchCount >= 3)
                    {
                        int switchesInSeries = 0;
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool pin1HasResistor = switchState.pin1HasResistor ?? false;
                                bool pin2HasResistor = switchState.pin2HasResistor ?? false;
                                if (pin1HasResistor || pin2HasResistor)
                                {
                                    switchesInSeries++;
                                }
                            }
                        }
                        return switchesInSeries >= 3;
                    }
                    return false;

                case 8: // "Ground the third switch"
                    if (dipSwitchCount >= 3)
                    {
                        int switchesGrounded = 0;
                        foreach (string switchId in switchIds)
                        {
                            if (simResult.ComponentStates.ContainsKey(switchId))
                            {
                                dynamic switchState = simResult.ComponentStates[switchId];
                                bool isGrounded = switchState.isGrounded ?? false;
                                if (isGrounded)
                                {
                                    switchesGrounded++;
                                }
                            }
                        }
                        return switchesGrounded >= 3;
                    }
                    return false;

                case 9: // "Ground IC74138 to Ground Breadboard"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            bool hasGnd = ic74138State.hasGnd;
                            return hasGnd;
                        }
                        catch { return false; }
                    }
                    return false;

                case 10: // "VCC IC74138 to VCC Breadboard"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            bool hasVcc = ic74138State.hasVcc;
                            return hasVcc;
                        }
                        catch { return false; }
                    }
                    return false;

                case 11: // "IC74138 A0 to input 1 from pull-up resistor input"
                case 12: // "IC74138 A1 to input 2 from pull-up resistor input"
                case 13: // "IC74138 A2 to input 3 from pull-up resistor input"
                    if (ic74138Count >= 1 && resistorCount >= 3 && dipSwitchCount >= 3)
                    {
                        JToken icComp = components[mainIc];
                        if (icComp != null)
                        {
                            string[] inputPins = { "pin1", "pin2", "pin3" };
                            int inputIndex = instructionIndex - 11;
                            string inputPin = icComp[inputPins[inputIndex]]?.ToString();

                            if (inputPin != null)
                            {
                                foreach (string resistorId in resistorIds)
                                {
                                    if (simResult.ComponentStates.ContainsKey(resistorId))
                                    {
                                        dynamic resistorState = simResult.ComponentStates[resistorId];
                                        string rPin1 = resistorState.pin1;
                                        string rPin2 = resistorState.pin2;

                                        if (AreNodesConnected(inputPin, rPin1, simResult.Nets) ||
                                            AreNodesConnected(inputPin, rPin2, simResult.Nets))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return false;

                case 14: // "E1 set to LOW"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            bool e1Enabled = ic74138State.enable.E1;
                            return e1Enabled;
                        }
                        catch { return false; }
                    }
                    return false;

                case 15: // "E2 set to LOW"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            bool e2Enabled = ic74138State.enable.E2;
                            return e2Enabled;
                        }
                        catch { return false; }
                    }
                    return false;

                case 16: // "E3 set to HIGH"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            bool e3Enabled = ic74138State.enable.E3;
                            return e3Enabled;
                        }
                        catch { return false; }
                    }
                    return false;

                case 17: // "Add 8 LEDs"
                    return ledCount >= 8;

                case 18: // "Connect LEDs to IC74138 outputs"
                    if (ic74138Count >= 1 && ledCount >= 8)
                    {
                        JToken icComp = components[mainIc];
                        if (icComp != null)
                        {
                            string[] outputPins = new string[] {
                            "pin15", "pin14", "pin13", "pin12",
                            "pin11", "pin10", "pin9", "pin7"
                        };

                            int connectedLeds = 0;
                            foreach (string pin in outputPins)
                            {
                                foreach (JProperty componentProp in components)
                                {
                                    if (componentProp.Name.StartsWith("led"))
                                    {
                                        JToken ledComp = componentProp.Value;
                                        string pinValue = icComp[pin]?.ToString();
                                        string anodeValue = ledComp["anode"]?.ToString();

                                        if (AreNodesConnected(pinValue, anodeValue, simResult.Nets))
                                        {
                                            connectedLeds++;
                                            break;
                                        }
                                    }
                                }
                            }
                            return connectedLeds >= 8;
                        }
                    }
                    return false;

                case 19: // "Connect O0 to a LED"
                case 20: // "Connect O1 to a LED"
                case 21: // "Connect O2 to a LED"
                case 22: // "Connect O3 to a LED"
                case 23: // "Connect O4 to a LED"
                case 24: // "Connect O5 to a LED"
                case 25: // "Connect O6 to a LED"
                case 26: // "Connect O7 to a LED"
                    if (ic74138Count >= 1 && ledCount >= 8)
                    {
                        JToken icComp = components[mainIc];
                        if (icComp != null)
                        {
                            string[] outputPins = new string[] {
                                "pin15", "pin14", "pin13", "pin12",
                                "pin11", "pin10", "pin9", "pin7"
                            };

                            int outputIndex = instructionIndex - 19;
                            string targetPin = outputPins[outputIndex];

                            foreach (JProperty componentProp in components)
                            {
                                if (componentProp.Name.StartsWith("led"))
                                {
                                    JToken ledComp = componentProp.Value;
                                    string pinValue = icComp[targetPin]?.ToString();
                                    string anodeValue = ledComp["anode"]?.ToString();

                                    if (AreNodesConnected(pinValue, anodeValue, simResult.Nets))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;

                case 27: // "Set inputs to 000 (A2=0, A1=0, A0=0)"
                case 28: // "Set inputs to 001 (A2=0, A1=0, A0=1)"
                case 29: // "Set inputs to 010 (A2=0, A1=1, A0=0)"
                case 30: // "Set inputs to 011 (A2=0, A1=1, A0=1)"
                case 31: // "Set inputs to 100 (A2=1, A1=0, A0=0)"
                case 32: // "Set inputs to 101 (A2=1, A1=0, A0=1)"
                case 33: // "Set inputs to 110 (A2=1, A1=1, A0=0)"
                case 34: // "Set inputs to 111 (A2=1, A1=1, A0=1)"
                    if (ic74138Count >= 1 && ic74138State != null)
                    {
                        try
                        {
                            // Get expected input pattern for this instruction
                            int inputPattern = instructionIndex - 27; // 0-7 for 000-111
                            bool expectedA0 = (inputPattern & 1) == 1;
                            bool expectedA1 = (inputPattern & 2) == 2;
                            bool expectedA2 = (inputPattern & 4) == 4;

                            // Get actual IC inputs from simResult.ComponentStates
                            bool actualA0 = ic74138State.address.A0;
                            bool actualA1 = ic74138State.address.A1;
                            bool actualA2 = ic74138State.address.A2;

                            return (actualA0 == expectedA0) && (actualA1 == expectedA1) && (actualA2 == expectedA2);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        // Check all instructions from current to end to detect future completed ones
        List<int> completedInstructions = new List<int>();
        for (int i = CurrentInstructionIndex; i < experiment.TotalInstructions; i++)
        {
            if (CheckInstructionRequirements(i))
            {
                completedInstructions.Add(i);
            }
            else
            {
                break; // Stop at first incomplete instruction to maintain linear progression
            }
        }

        // Validate current instruction step and provide specific feedback
        bool instructionCompleted = completedInstructions.Contains(CurrentInstructionIndex);

        if (!instructionCompleted)
        {
            // Provide specific feedback for current instruction
            switch (CurrentInstructionIndex)
            {
                case 0:
                    if (resistorCount == 0)
                        result.Messages.Add("Add a resistor component");
                    else
                        result.Messages.Add("Resistor must be connected to a power terminal (PWR)");
                    break;

                case 1:
                    if (dipSwitchCount == 0)
                        result.Messages.Add("Add a switch component");
                    else if (resistorCount == 0)
                        result.Messages.Add("Need a resistor first");
                    else
                        result.Messages.Add("Switch must be connected in series with the resistor");
                    break;

                case 2:
                    if (dipSwitchCount == 0)
                        result.Messages.Add("Add a switch component first");
                    else
                        result.Messages.Add("Switch must be connected to ground (GND)");
                    break;

                case 3:
                    result.Messages.Add($"Need at least 2 resistors connected to power, currently have {resistorCount}");
                    break;

                case 4:
                    if (resistorCount < 2)
                        result.Messages.Add("Need at least 2 resistors first");
                    else if (dipSwitchCount < 2)
                        result.Messages.Add("Need at least 2 switches");
                    else
                        result.Messages.Add("Need at least 2 switches connected in series with resistors");
                    break;

                case 5:
                    if (dipSwitchCount < 2)
                        result.Messages.Add("Need at least 2 switches first");
                    else
                        result.Messages.Add("Need at least 2 switches connected to ground");
                    break;

                case 6:
                    result.Messages.Add($"Need at least 3 resistors connected to power, currently have {resistorCount}");
                    break;

                case 7:
                    if (resistorCount < 3)
                        result.Messages.Add("Need at least 3 resistors first");
                    else if (dipSwitchCount < 3)
                        result.Messages.Add("Need at least 3 switches");
                    else
                        result.Messages.Add("Need at least 3 switches connected in series with resistors");
                    break;

                case 8:
                    if (dipSwitchCount < 3)
                        result.Messages.Add("Need at least 3 switches first");
                    else
                        result.Messages.Add("Need at least 3 switches connected to ground");
                    break;

                case 9:
                    if (ic74138Count == 0)
                        result.Messages.Add("Add IC74138 component first");
                    else
                        result.Messages.Add("IC74138 pin 8 (GND) must be connected to ground rail");
                    break;

                case 10:
                    if (ic74138Count == 0)
                        result.Messages.Add("Add IC74138 component first");
                    else
                        result.Messages.Add("IC74138 pin 16 (VCC) must be connected to power rail");
                    break;

                case 11:
                case 12:
                case 13:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else if (resistorCount < 3)
                        result.Messages.Add("Need 3 resistors first");
                    else if (dipSwitchCount < 3)
                        result.Messages.Add("Need 3 switches first");
                    else
                    {
                        string[] pinNames = { "A0", "A1", "A2" };
                        int inputIndex = CurrentInstructionIndex - 11;
                        result.Messages.Add($"IC74138 {pinNames[inputIndex]} must be connected to pull-up resistor network");
                    }
                    break;

                case 14:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else
                        result.Messages.Add("IC74138 E1 pin must be set to LOW");
                    break;

                case 15:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else
                        result.Messages.Add("IC74138 E2 pin must be set to LOW");
                    break;

                case 16:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else
                        result.Messages.Add("IC74138 E3 pin must be set to HIGH");
                    break;

                case 17:
                    result.Messages.Add($"Need at least 8 LEDs, currently have {ledCount}");
                    break;

                case 18:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else if (ledCount < 8)
                        result.Messages.Add("Need 8 LEDs first");
                    else
                        result.Messages.Add("Connect LEDs to IC74138 output pins");
                    break;
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else if (ledCount < 8)
                        result.Messages.Add("Need 8 LEDs first");
                    else
                    {
                        string[] outputNames = { "O0", "O1", "O2", "O3", "O4", "O5", "O6", "O7" };
                        int outputIndex = CurrentInstructionIndex - 19;
                        result.Messages.Add($"Connect IC74138 {outputNames[outputIndex]} to a LED anode");
                    }
                    break;

                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                    if (ic74138Count == 0)
                        result.Messages.Add("Need IC74138 first");
                    else if (dipSwitchCount < 3)
                        result.Messages.Add("Need 3 DIP switches to control inputs");
                    else
                    {
                        int inputPattern = CurrentInstructionIndex - 27;
                        string binaryPattern = Convert.ToString(inputPattern, 2).PadLeft(3, '0');
                        result.Messages.Add($"Set IC74138 inputs to {binaryPattern} (A2={binaryPattern[0]}, A1={binaryPattern[1]}, A0={binaryPattern[2]})");
                    }
                    break;
            }
        }
        else
        {
            // Mark all completed instructions
            foreach (int completedIndex in completedInstructions)
            {
                result.InstructionResults[completedIndex] = true;
                if (!_completedInstructions[experiment.Id].Contains(completedIndex))
                {
                    _completedInstructions[experiment.Id].Add(completedIndex);
                }
            }

            result.CompletedInstructions = _completedInstructions[experiment.Id].Count;

            // Advance to the next incomplete instruction
            if (completedInstructions.Count > 0)
            {
                int nextIncompleteInstruction = completedInstructions.Max() + 1;
                if (nextIncompleteInstruction < experiment.TotalInstructions)
                {
                    CurrentInstructionIndex = nextIncompleteInstruction;
                    result.MainInstruction = experiment.InstructionDescriptions[CurrentInstructionIndex];
                }
                else
                {
                    // All instructions completed
                    NextInstruction();
                }
            }
        }

        return result;
    }
}


// Support classes for experiment evaluation
public class ExperimentDefinition
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int TotalInstructions { get; set; }
    public Dictionary<int, string> InstructionDescriptions { get; set; } = new Dictionary<int, string>();
}

// Update the ExperimentResult class to support multiple messages
public class ExperimentResult
{
    public string ExperimentName { get; set; }
    public int ExperimentId { get; set; }
    public Dictionary<int, bool> InstructionResults { get; set; } = new Dictionary<int, bool>();
    public int CompletedInstructions { get; set; }
    public int TotalInstructions { get; set; }
    public List<string> Messages { get; set; } = new List<string>();
    public string MainInstruction { get; set; }
    public bool IsSetupValid { get; set; }
}
