using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class BreadboardSimulator : MonoBehaviour
{
    public static BreadboardSimulator Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
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
    }

    // Main entry point that takes JSON state and returns simulation results
    public SimulationResult Run(string jsonState)
    {
        try
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

            // Debug the simulation results
            DebugSimulationResult(result);

            return result;
        }
        catch (Exception ex)
        {
            // Log any unexpected errors
            Debug.LogError($"Simulation error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

            return new SimulationResult
            {
                Errors = new List<BreadboardError> {
                    new BreadboardError {
                        ErrorType = "SimulationError",
                        Description = $"Unexpected error: {ex.Message}"
                    }
                }
            };
        }
    }

    // Helper method for debugging simulation results
    private void DebugSimulationResult(SimulationResult result)
    {
        // Log component states
        Debug.Log($"Component states ({result.ComponentStates.Count}):");
        foreach (var comp in result.ComponentStates)
        {
            Debug.Log($"  {comp.Key}: {JsonConvert.SerializeObject(comp.Value)}");
        }

        // Log nets
        // Debug.Log($"Electrical nets ({result.Nets.Count}):");
        // foreach (var net in result.Nets)
        // {
        //     string nodeList = string.Join(", ", net.Nodes.Take(5));
        //     if (net.Nodes.Count > 5) nodeList += $"... ({net.Nodes.Count - 5} more)";
        //     Debug.Log($"  Net {net.Id}: {nodeList} - State: {net.State}, Source: {net.Source}");
        // }

        // Log errors
        Debug.Log($"Errors ({result.Errors.Count}):");
        foreach (var error in result.Errors)
        {
            Debug.Log($"  {error.ErrorType}: {error.Description}");
            if (error.AffectedNodes.Count > 0)
            {
                Debug.Log($"    Affected nodes: {string.Join(", ", error.AffectedNodes)}");
            }
            if (error.InvolvedComponents.Count > 0)
            {
                Debug.Log($"    Involved components: {string.Join(", ", error.InvolvedComponents)}");
            }
        }
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
            // Handle DIP switches - only connect pins if switch is ON
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
                        Debug.Log($"DIP Switch {componentKey} is OFF, pins {pin1} and {pin2} are disconnected");
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
            if (property.Name != "type" && property.Name != "color" && property.Name != "isOn")
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
                
                // Skip wires and DIP switches (already processed in graph building)
                if (componentKey.StartsWith("wire") || componentKey.StartsWith("dipSwitch"))
                {
                    // For DIP switches, just store their state
                    if (componentKey.StartsWith("dipSwitch"))
                    {
                        bool isOn = componentValue["isOn"] != null ? 
                            componentValue["isOn"].Value<bool>() : false;
                            
                        componentStates[componentKey] = new { isOn = isOn };
                    }
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

        return new { isOn = isOn };
    }

    // Evaluate Seven-Segment Display
    private object EvaluateSevenSegment(Dictionary<string, int> connectedNets, List<Net> nets)
    {
        var segments = new Dictionary<string, bool>();

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

                // Segment is on when it's HIGH and GND is LOW
                isOn = nets[segNetId].State == NodeState.HIGH &&
                       nets[gndNetId].State == NodeState.LOW;
            }

            segments[segment] = isOn;
        }

        return new { segments = segments };
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
            return (false, new { 
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
            return (false, new { error = "Missing BCD input pins" });
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
            return (false, new { status = "Uninitialized inputs" });
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
            return (false, new { error = "Missing address pins" });
        }

        // Get address inputs (A0, A1, A2)
        bool a0 = nets[connectedNets["pin1"]].State == NodeState.HIGH;
        bool a1 = nets[connectedNets["pin2"]].State == NodeState.HIGH;
        bool a2 = nets[connectedNets["pin3"]].State == NodeState.HIGH;

        // Get enable inputs (E1, E2, E3) with defaults
        bool e1 = connectedNets.ContainsKey("pin4") && nets[connectedNets["pin4"]].State == NodeState.HIGH;
        bool e2 = !connectedNets.ContainsKey("pin5") || nets[connectedNets["pin5"]].State == NodeState.LOW;
        bool e3 = !connectedNets.ContainsKey("pin6") || nets[connectedNets["pin6"]].State == NodeState.LOW;

        // Calculate binary address (0-7)
        int address = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);

        // Initialize all outputs as HIGH (inactive)
        var outputs = new Dictionary<string, bool>();
        for (int i = 0; i < 8; i++)
        {
            outputs["O" + i] = true;  // Active LOW outputs, so HIGH means inactive
        }

        // Set active output if enabled (E1 high, E2 and E3 low)
        bool enabled = e1 && e2 && e3;
        if (enabled)
        {
            outputs["O" + address] = false;  // Selected output is LOW (active)
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
            a0 = a1 = a2 = true;   // All outputs HIGH (inactive)
            gs = true;             // GS HIGH (inactive)
            e0 = false;            // E0 LOW (active)
        }
        else
        {
            // Encode priority input to binary (complement of bits)
            a0 = (highestActive & 1) == 0;  // Bit 0
            a1 = (highestActive & 2) == 0;  // Bit 1
            a2 = (highestActive & 4) == 0;  // Bit 2
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
}
