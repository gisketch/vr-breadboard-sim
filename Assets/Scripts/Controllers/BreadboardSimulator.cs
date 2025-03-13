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
    }

    // Breadboard graph representation
    public class BreadboardGraph
    {
        private Dictionary<string, List<string>> _adjacencyList = new Dictionary<string, List<string>>();

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
        DetermineInitialStates(nets);

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

        //Add button events
        prevExperiment.onClick.AddListener(() =>
        {
            BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
            PreviousExperiment();
        });

        //Add button events
        nextExperiment.onClick.AddListener(() =>
        {
            BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
            NextExperiment();
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
    private void DetermineInitialStates(List<Net> nets)
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

                // For DIP switches, apply pull-up behavior to any floating connections
                if (componentKey.StartsWith("dipSwitch"))
                {
                    bool isOn = componentValue["isOn"] != null ?
                        componentValue["isOn"].Value<bool>() : false;

                    // Get pin information
                    string pin1 = componentValue["pin1"] != null ? componentValue["pin1"].ToString() :
                                  componentValue["inputPin"]?.ToString();

                    string pin2 = componentValue["pin2"] != null ? componentValue["pin2"].ToString() :
                                  componentValue["outputPin"]?.ToString();

                    // If switch is OFF, we need to apply pull-up behavior
                    if (!isOn && !string.IsNullOrEmpty(pin1) && !string.IsNullOrEmpty(pin2))
                    {
                        // Check if pins are in nets
                        bool pin1InNet = nodeToNetMap.ContainsKey(pin1);
                        bool pin2InNet = nodeToNetMap.ContainsKey(pin2);

                        if (pin1InNet && pin2InNet)
                        {
                            int net1Id = nodeToNetMap[pin1];
                            int net2Id = nodeToNetMap[pin2];

                            // Check if either pin is already connected to a driven signal
                            bool net1Driven = nets[net1Id].State != NodeState.UNINITIALIZED;
                            bool net2Driven = nets[net2Id].State != NodeState.UNINITIALIZED;

                            // Apply pull-up to pin2 if pin1 is driven but pin2 is not
                            if (net1Driven && !net2Driven)
                            {
                                nets[net2Id].State = NodeState.HIGH;
                                nets[net2Id].Source = PowerSource.RAIL;
                                nets[net2Id].SourceComponent = componentKey;
                                changed = true;
                                Debug.Log($"Applied pull-up to pin2 ({pin2}) of {componentKey}");
                            }
                            // Apply pull-up to pin1 if pin2 is driven but pin1 is not
                            else if (!net1Driven && net2Driven)
                            {
                                nets[net1Id].State = NodeState.HIGH;
                                nets[net1Id].Source = PowerSource.RAIL;
                                nets[net1Id].SourceComponent = componentKey;
                                changed = true;
                                Debug.Log($"Applied pull-up to pin1 ({pin1}) of {componentKey}");
                            }
                            // If neither is driven, apply pull-up to both pins
                            else if (!net1Driven && !net2Driven)
                            {
                                nets[net1Id].State = NodeState.HIGH;
                                nets[net1Id].Source = PowerSource.RAIL;
                                nets[net1Id].SourceComponent = componentKey;

                                nets[net2Id].State = NodeState.HIGH;
                                nets[net2Id].Source = PowerSource.RAIL;
                                nets[net2Id].SourceComponent = componentKey;

                                changed = true;
                                Debug.Log($"Applied pull-up to both pins of {componentKey}");
                            }
                            // If both are driven, no action needed
                        }
                    }

                    componentStates[componentKey] = new { isOn = isOn };
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

        // LED is on when anode is HIGH and cathode is LOW
        bool isOn = nets[anodeNetId].State == NodeState.HIGH &&
                    nets[cathodeNetId].State == NodeState.LOW;

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
                int gndNetId;
                if (connectedNets.ContainsKey("nodeGnd1"))
                {
                    gndNetId = connectedNets["nodeGnd1"];
                }
                else
                {
                    gndNetId = connectedNets["nodeGnd2"];
                }

                isGrounded = nets[gndNetId].State == NodeState.LOW;

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
            return (false, new { type = "IC7448", status = "Uninitialized inputs" });
        }

        // Get lamp test, blanking and ripple blanking inputs
        bool lt = !connectedNets.ContainsKey("pin3") || nets[connectedNets["pin3"]].State == NodeState.HIGH;
        bool bi_rbo = !connectedNets.ContainsKey("pin4") || nets[connectedNets["pin4"]].State == NodeState.HIGH;
        bool rbi = !connectedNets.ContainsKey("pin5") || nets[connectedNets["pin5"]].State == NodeState.HIGH;

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
            TotalInstructions = 8
        };

        // Add instructions for inputs 000 to 111
        for (int i = 0; i < 8; i++)
        {
            string binary = Convert.ToString(i, 2).PadLeft(3, '0');
            decoderExperiment.InstructionDescriptions[i] = $"Set input to {binary}";
        }

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

            // Removed since optional
            // if (!gsActive)
            // {
            //     result.Messages.Add("IC 74148 GS pin must be set to LOW");
            //     result.IsSetupValid = false;
            // }

            // if (!e0Active)
            // {
            //     result.Messages.Add("IC 74148 E0 pin must be set to LOW");
            //     result.IsSetupValid = false;
            // }

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

            // for (int i = 0; i < 3; i++)
            // {
            //     actualBits[i] = ((highestPriority >> i) & 1) == 1; // converts the highest priority into its 3-bit binary rep.
            // }

            // for (int i = 0; i < 3; i++)
            // {
            //     if (actualBits[i] != expectedBits[i])
            //     {
            //         allMatch = false;
            //         break;
            //     }
            // }

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

        // Count components and validate types
        int ic74138Count = 0;
        int ledCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        List<string> mainLeds = new List<string>();

        // Get component data
        dynamic ic74138State = null;
        List<dynamic> ledStates = new List<dynamic>();

        foreach (var comp in simResult.ComponentStates)
        {
            if (comp.Key.StartsWith("ic"))
            {
                dynamic dynamicValue = comp.Value; // {type: "IC74138", address: {A0: bool, A1: bool, A2: bool}, ...}
                string typeValue = dynamicValue.type; // IC74138

                if (typeValue == "IC74138")
                {
                    ic74138Count++;
                    ic74138State = dynamicValue;
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
        if (ic74138Count != 1)
        {
            result.Messages.Add($"Expected exactly 1 IC 74138, found {ic74138Count}");
            result.IsSetupValid = false;
        }

        if (ledCount < 8)
        {
            result.Messages.Add($"Expected at least 8 LEDs, found {ledCount}");
            result.IsSetupValid = false;
        }

        if (dipSwitchCount < 3)
        {
            result.Messages.Add($"Expected at least 3 DIP switches, found {dipSwitchCount}");
            result.IsSetupValid = false;
        }

        // Only proceed with detailed checks if basic components are present
        if (ic74138State != null && ledCount > 7 && dipSwitchCount > 2)
        {

            // Check IC 74138 enable pins (e1, e2, e3)
            bool e1Active = false;
            bool e2Active = false;
            bool e3Active = false;

            try
            {
                e1Active = ic74138State.enable.E1;
                e2Active = ic74138State.enable.E2;
                e3Active = ic74138State.enable.E3;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot access IC 74138 enable pins");
                result.IsSetupValid = false;
            }

            // Add individual messages for each enable pin
            if (!e1Active)
            {
                result.Messages.Add("IC 74138 E1 pin must be set to LOW");
                result.IsSetupValid = false;
            }

            if (!e2Active)
            {
                result.Messages.Add("IC 74138 E2 pin must be set to LOW");
                result.IsSetupValid = false;
            }

            if (!e3Active)
            {
                result.Messages.Add("IC 74138 E3 pin must be set to HIGH");
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
                status = ic74138State.status;

                if (status == "Uninitialized inputs")
                {
                    result.Messages.Add("IC 74138 has uninitialized inputs. Check DIP switch connections.");
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
                "pin15", "pin14", "pin13", "pin12",
                "pin11", "pin10", "pin9", "pin7"
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

            if (ledsConnectedToIc.Count != 8) allConnected = false;

            if (!allConnected)
            {
                result.Messages.Add("IC74138 outputs are not properly connected to LEDs.");
                result.IsSetupValid = false;
            }
        }

        // Only evaluate the experiment if the setup is valid
        if (result.IsSetupValid)
        {
            // Get expected A0 A1 A2 input for current instruction (000 to 111)
            bool[] expectedBits = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                expectedBits[i] = ((CurrentInstructionIndex >> i) & 1) == 1;
            }

            // Get actual IC inputs
            bool inputA0 = false, inputA1 = false, inputA2 = false;
            try
            {
                inputA0 = ic74138State.address.A0;
                inputA1 = ic74138State.address.A1;
                inputA2 = ic74138State.address.A2;
            }
            catch (Exception)
            {
                result.Messages.Add("Cannot evaluate: IC inputs not found in simulation result");
                result.InstructionResults[CurrentInstructionIndex] = false;
                return result;
            }

            // Compare expected and actual inputs
            bool[] actualBits = new[] { inputA0, inputA1, inputA2 };
            bool allMatch = true;

            for (int i = 0; i < 3; i++)
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
