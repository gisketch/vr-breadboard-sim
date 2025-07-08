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
    public GameObject componentText;

    // Each simulator instance has its own experiment management
    private ExperimentDefinitions experimentDefinitions;
    private ExperimentEvaluator experimentEvaluator;

    // Reference to the breadboard controller this simulator belongs to
    private BreadboardController associatedController;

    // Initialize this simulator instance for a specific controller
    public void Initialize(BreadboardController controller)
    {
        associatedController = controller;

        // Load prefabs from Resources folder
        warningText = Resources.Load<GameObject>("Warning Text");
        taskText = Resources.Load<GameObject>("Task Text");
        componentText = Resources.Load<GameObject>("Component Text");

        Debug.Log($"BreadboardSimulator initialized for controller {controller.studentId}");
        experimentDefinitions = new ExperimentDefinitions();
        experimentEvaluator = new ExperimentEvaluator(experimentDefinitions);
        Debug.Log($"Current experiment ID after initialization: {experimentDefinitions.CurrentExperimentId}");
        Debug.Log($"Available experiments: {experimentDefinitions.GetExperiments().Count}");
    }

    // Add getter for experiment definitions (for external access)
    public ExperimentDefinitions GetExperimentDefinitions()
    {
        return experimentDefinitions;
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
        // Ensure this simulator is only used by its associated controller
        if (bc != associatedController)
        {
            Debug.LogError($"Simulator mismatch: Expected controller {associatedController?.studentId}, got {bc.studentId}");
            return new SimulationResult
            {
                Errors = new List<BreadboardError> {
                    new BreadboardError {
                        ErrorType = "SimulatorError",
                        Description = "Simulator instance mismatch"
                    }
                }
            };
        }

        // Debug the input JSON
        Debug.Log($"Running simulation for student {bc.studentId} with JSON: {jsonState}");

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

        // Evaluate experiment using this instance's experiment system
        result.ExperimentResult = experimentEvaluator.EvaluateExperiment(result, components);

        // Debug logging for UI update conditions
        InstructorSpectatorController spectatorController = FindObjectOfType<InstructorSpectatorController>();
        
        Debug.Log($"[DEBUG] SimulateBreadboard called for bc.studentId: {bc.studentId}");
        Debug.Log($"[DEBUG] bc.hasAuthority: {bc.hasAuthority}");
        Debug.Log($"[DEBUG] bc == associatedController: {bc == associatedController}");
        Debug.Log($"[DEBUG] spectatorController found: {spectatorController != null}");
        
        if (spectatorController != null)
        {
            Debug.Log($"[DEBUG] spectatorController.IsSpectating: {spectatorController.IsSpectating}");
            Debug.Log($"[DEBUG] Current role: {GameManager.Instance.CurrentRole}");
            Debug.Log($"[DEBUG] Is instructor: {GameManager.Instance.CurrentRole == GameManager.UserRole.Instructor}");
        }

        // UI Updates - check for authority OR if instructor is spectating this breadboard
        bool hasAuthority = bc.hasAuthority && bc == associatedController;
        bool isSpectatingThisBreadboard = spectatorController != null && 
                                         spectatorController.IsSpectating &&
                                         GameManager.Instance.CurrentRole == GameManager.UserRole.Instructor &&
                                         bc == associatedController;
        
        Debug.Log($"[DEBUG] hasAuthority condition: {hasAuthority}");
        Debug.Log($"[DEBUG] isSpectatingThisBreadboard condition: {isSpectatingThisBreadboard}");
        Debug.Log($"[DEBUG] Will call UpdateUI: {hasAuthority || isSpectatingThisBreadboard}");
        
        if (hasAuthority || isSpectatingThisBreadboard)
        {
            Debug.Log($"[DEBUG] Calling UpdateUI for bc.studentId: {bc.studentId}");
            UpdateUI(bc, result);
        }
        else
        {
            Debug.Log($"[DEBUG] Skipping UpdateUI for bc.studentId: {bc.studentId}");
        }

        // Debug component states
        foreach (var kvp in result.ComponentStates)
        {
            Debug.Log($"Component {kvp.Key}: {JsonConvert.SerializeObject(kvp.Value, Formatting.Indented)}");
        }
        return result;
    }

    private void UpdateUI(BreadboardController bc, SimulationResult result)
    {
        Debug.Log($"[DEBUG] UpdateUI called for bc.studentId: {bc.studentId}");
        
        // Get the correct experiment definitions - use the ones from the breadboard controller's simulator
        ExperimentDefinitions targetExperimentDefinitions;

        // Check if we're spectating and need to use the student's experiment data
        InstructorSpectatorController spectatorController = FindObjectOfType<InstructorSpectatorController>();
        if (spectatorController != null && spectatorController.IsSpectating &&
            GameManager.Instance.CurrentRole == GameManager.UserRole.Instructor)
        {
            Debug.Log($"[DEBUG] Using student's experiment definitions from bc.studentId: {bc.studentId}");
            // Use the student's experiment definitions from their breadboard's simulator
            targetExperimentDefinitions = bc.GetSimulatorInstance().GetExperimentDefinitions();
            Debug.Log($"[DEBUG] Student's current experiment ID: {targetExperimentDefinitions.CurrentExperimentId}");
            Debug.Log($"[DEBUG] Student's completed instructions: {targetExperimentDefinitions.GetCompletedInstructionsCount(targetExperimentDefinitions.CurrentExperimentId)}/{targetExperimentDefinitions.GetTotalInstructionsCount(targetExperimentDefinitions.CurrentExperimentId)}");
        }
        else
        {
            Debug.Log($"[DEBUG] Using own experiment definitions");
            // Use our own experiment definitions
            targetExperimentDefinitions = this.experimentDefinitions;
            Debug.Log($"[DEBUG] Own current experiment ID: {targetExperimentDefinitions.CurrentExperimentId}");
        }

        //Clear messages first
        foreach (Transform child in bc.labMessagesTransform)
        {
            Destroy(child.gameObject);
        }

        // Add component guide first
        AddComponentGuideForExperiment(bc, targetExperimentDefinitions.CurrentExperimentId);

        //Add main instruction
        GameObject taskMsg = Instantiate(taskText);

        bool isCompleted = ((float)targetExperimentDefinitions.GetCompletedInstructionsCount(targetExperimentDefinitions.CurrentExperimentId) / targetExperimentDefinitions.GetTotalInstructionsCount(targetExperimentDefinitions.CurrentExperimentId)) == 1f;

        taskMsg.transform.SetParent(bc.labMessagesTransform);
        taskMsg.transform.localScale = Vector3.one;
        taskMsg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        taskMsg.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
        taskMsg.transform.Find("Message").GetComponent<TMP_Text>().text = isCompleted ? $"Experiment {targetExperimentDefinitions.CurrentExperimentId} Completed!" : result.ExperimentResult.MainInstruction;

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
        switch (targetExperimentDefinitions.CurrentExperimentId)
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
        float targetFillAmount = (float)targetExperimentDefinitions.GetCompletedInstructionsCount(targetExperimentDefinitions.CurrentExperimentId) / targetExperimentDefinitions.GetTotalInstructionsCount(targetExperimentDefinitions.CurrentExperimentId);
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
        completionText.text = $"COMPLETED {targetExperimentDefinitions.GetCompletedInstructionsCount(targetExperimentDefinitions.CurrentExperimentId)}/{targetExperimentDefinitions.GetTotalInstructionsCount(targetExperimentDefinitions.CurrentExperimentId)}";

        //Clear button events here
        Button prevExperiment = experimentName.transform.Find("PrevExperiment").GetComponent<Button>();
        Button nextExperiment = experimentName.transform.Find("NextExperiment").GetComponent<Button>();

        prevExperiment.onClick.RemoveAllListeners();
        nextExperiment.onClick.RemoveAllListeners();

        Debug.Log($"Updating score from simulator for student {bc.studentId}");
        bc.CmdUpdateScore($@"Student {bc.studentId}
 - experiment 1 : {targetExperimentDefinitions.GetCompletedInstructionsCount(1)}/{targetExperimentDefinitions.GetTotalInstructionsCount(1)}
 - experiment 2 : {targetExperimentDefinitions.GetCompletedInstructionsCount(2)}/{targetExperimentDefinitions.GetTotalInstructionsCount(2)}
 - experiment 3 : {targetExperimentDefinitions.GetCompletedInstructionsCount(3)}/{targetExperimentDefinitions.GetTotalInstructionsCount(3)}");

        // Only add button events if this is not a spectating instructor
        if (!(spectatorController != null && spectatorController.IsSpectating &&
              GameManager.Instance.CurrentRole == GameManager.UserRole.Instructor))
        {
            //Add button events
            prevExperiment.onClick.AddListener(() =>
            {
                targetExperimentDefinitions.PreviousExperiment();
                BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
            });

            //Add button events
            nextExperiment.onClick.AddListener(() =>
            {
                targetExperimentDefinitions.NextExperiment();
                BreadboardStateUtils.Instance.VisualizeBreadboard(bc);
            });
        }
    }

    private void AddComponentGuide(BreadboardController bc)
    {
        GameObject componentMsg = Instantiate(componentText);
        componentMsg.transform.SetParent(bc.labMessagesTransform);
        componentMsg.transform.localScale = Vector3.one;
        componentMsg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        componentMsg.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        string componentGuideText = GetComponentGuideText(experimentDefinitions.CurrentExperimentId);
        componentMsg.transform.Find("Message").GetComponent<TMP_Text>().text = componentGuideText;
    }

    private void AddComponentGuideForExperiment(BreadboardController bc, int experimentId)
    {
        GameObject componentMsg = Instantiate(componentText);
        componentMsg.transform.SetParent(bc.labMessagesTransform);
        componentMsg.transform.localScale = Vector3.one;
        componentMsg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        componentMsg.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        string componentGuideText = GetComponentGuideText(experimentId);
        componentMsg.transform.Find("Message").GetComponent<TMP_Text>().text = componentGuideText;
    }

    private string GetComponentGuideText(int experimentId)
    {
        switch (experimentId)
        {
            case 1: // IC 74138 Decoder
                return "Required Components:\n- 3 DIP Switches\n- 8 LEDs\n- 1 IC 74138";

            case 2: // BCD to 7-Segment Display
                return "Required Components:\n- 4 DIP Switches\n- 1 7-Segment Display\n- 1 IC 7448";

            case 3: // IC 74148 Encoder
                return "Required Components:\n- 8 DIP Switches\n- 3 LEDs\n- 1 IC 74148";

            default:
                return "Required Components:\n- Unknown experiment";
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
    }

    // Detect electrical errors in the circuit
    private List<BreadboardError> DetectErrors(List<Net> nets)
    {
        var errors = new List<BreadboardError>();

        foreach (var net in nets)
        {
            // Check for short circuits (PWR and GND in same net)
            bool hasPower = net.Nodes.Any(n => n.Contains("PWR"));
            bool hasGround = net.Nodes.Any(n => n.Contains("GND"));

            if (hasPower && hasGround)
            {
                errors.Add(new BreadboardError
                {
                    ErrorType = "ShortCircuit",
                    Description = "Power and ground are directly connected",
                    AffectedNodes = net.Nodes.ToList()
                });
            }
        }

        return errors;
    }

    // Build a lookup map from node to net ID
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

    // Get connected nets for a component
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

    // Check if two nodes are electrically connected
    private bool AreNodesConnected(string nodeA, string nodeB, List<Net> nets)
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

                    // Helper function to check for resistors on a specific pin
                    bool CheckPinHasResistor(string pinNode)
                    {
                        // Check all resistor components to see if any connect to this pin
                        foreach (JProperty resistorProp in components)
                        {
                            if (resistorProp.Name.StartsWith("resistor"))
                            {
                                JToken resistorValue = resistorProp.Value;
                                string resistorPin1 = resistorValue["pin1"]?.ToString();
                                string resistorPin2 = resistorValue["pin2"]?.ToString();

                                if (resistorPin1 == pinNode || resistorPin2 == pinNode)
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }

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
                            pin1 = pin1,
                            pin2 = pin2,
                            pin1State = nets[net1Id].State.ToString(),
                            pin2State = nets[net2Id].State.ToString(),
                            pin1HasResistor = nets[net1Id].SourceComponents.Any(c => c.StartsWith("resistor")),
                            pin2HasResistor = nets[net2Id].SourceComponents.Any(c => c.StartsWith("resistor"))
                        };
                    }
                    else
                    {
                        // Even when switch is off and pins are not in nets, 
                        // we should still check for resistors on individual pins
                        switchState = new
                        {
                            isOn = isOn,
                            isGrounded = false,
                            pin1 = pin1,
                            pin2 = pin2,
                            pin1State = "UNCONNECTED",
                            pin2State = "UNCONNECTED",
                            pin1HasResistor = CheckPinHasResistor(pin1),
                            pin2HasResistor = CheckPinHasResistor(pin2)
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

        if (!hasVcc || !hasGnd)
        {
            return (false, new
            {
                type = icType,
                error = "Missing power connections",
                hasVcc = hasVcc,
                hasGnd = hasGnd
            });
        }

        // Delegate to specific IC evaluation methods
        switch (icType)
        {
            case "IC7448":
                return EvaluateIC7448(connectedNets, nets, componentId);
            case "IC74138":
                return EvaluateIC74138(connectedNets, nets, componentId);
            case "IC74148":
                return EvaluateIC74148(connectedNets, nets, componentId, hasVcc, hasGnd);
            default:
                return (false, new { type = icType, error = "Unsupported IC type" });
        }
    }

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

        // Detect input conflicts - check if multiple input pins share the same net
        bool inputHasConflict = false;
        var inputPins = new[] { "pin7", "pin1", "pin2", "pin6", "pin3", "pin4", "pin5" }; // A, B, C, D, LT, BI/RBO, RBI
        var inputNetIds = new List<int>();

        foreach (var pin in inputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (inputNetIds.Contains(netId))
                {
                    inputHasConflict = true;
                    break;
                }
                inputNetIds.Add(netId);
            }
        }

        // Detect output conflicts - check if multiple output pins share the same net
        bool outputHasConflict = false;
        var outputPins = new[] { "pin13", "pin12", "pin11", "pin10", "pin9", "pin15", "pin14" }; // A, B, C, D, E, F, G segments
        var outputNetIds = new List<int>();

        foreach (var pin in outputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (outputNetIds.Contains(netId))
                {
                    outputHasConflict = true;
                    break;
                }
                outputNetIds.Add(netId);
            }
        }

        // Helper function to get input value with proper pull-up logic
        object GetInputValueWithPullUp(string pin)
        {
            if (!connectedNets.ContainsKey(pin))
                return null;

            int netId = connectedNets[pin];
            Net net = nets[netId];

            // Check if this net has PWR and GND connections
            bool hasPower = net.Nodes.Any(n => n.Contains("PWR"));
            bool hasGround = net.Nodes.Any(n => n.Contains("GND"));
            bool hasResistor = net.SourceComponents.Any(c => c.StartsWith("resistor"));

            // If connected to PWR through resistor but no GND → DIP switch OFF → HIGH (pull-up)
            if (hasPower && hasResistor && !hasGround)
            {
                return true; // HIGH
            }

            // If connected to PWR through resistor AND GND → DIP switch ON → LOW (pulled to ground)
            if (hasPower && hasResistor && hasGround)
            {
                return false; // LOW
            }

            // If not connected to anything or uninitialized
            if (net.State == NodeState.UNINITIALIZED)
                return null;

            // Fallback to actual node state for other cases
            return net.State == NodeState.HIGH;
        }

        // Helper function for control inputs with default values
        object GetControlValueWithPullUp(string pin, bool defaultValue)
        {
            if (!connectedNets.ContainsKey(pin))
                return defaultValue;

            int netId = connectedNets[pin];
            Net net = nets[netId];

            // Check if this net has PWR and GND connections
            bool hasPower = net.Nodes.Any(n => n.Contains("PWR"));
            bool hasGround = net.Nodes.Any(n => n.Contains("GND"));
            bool hasResistor = net.SourceComponents.Any(c => c.StartsWith("resistor"));

            // If connected to PWR through resistor but no GND → DIP switch OFF → HIGH (pull-up)
            if (hasPower && hasResistor && !hasGround)
            {
                return true; // HIGH
            }

            // If connected to PWR through resistor AND GND → DIP switch ON → LOW (pulled to ground)
            if (hasPower && hasResistor && hasGround)
            {
                return false; // LOW
            }

            // If not connected to anything or uninitialized
            if (net.State == NodeState.UNINITIALIZED)
                return null;

            // Fallback to actual node state for other cases
            return net.State == NodeState.HIGH;
        }

        // Get BCD input values using proper pull-up logic
        object inputA = GetInputValueWithPullUp("pin7");
        object inputB = GetInputValueWithPullUp("pin1");
        object inputC = GetInputValueWithPullUp("pin2");
        object inputD = GetInputValueWithPullUp("pin6");

        // Get lamp test, blanking and ripple blanking inputs
        object lt = GetControlValueWithPullUp("pin3", true);   // Default HIGH (not active)
        object bi_rbo = GetControlValueWithPullUp("pin4", true); // Default HIGH (not active)
        object rbi = GetControlValueWithPullUp("pin5", true);   // Default HIGH (not active)

        // Check if any inputs are null (uninitialized)
        if (inputA == null || inputB == null || inputC == null || inputD == null)
        {
            return (false, new
            {
                type = "IC7448",
                status = "Uninitialized inputs",
                inputs = new { A = inputA, B = inputB, C = inputC, D = inputD },
                control = new { LT = lt, BI_RBO = bi_rbo, RBI = rbi },
                hasVcc = true,
                hasGnd = true,
                inputHasConflict = inputHasConflict,
                outputHasConflict = outputHasConflict
            });
        }

        // Convert to bool for calculations (we know they're not null here)
        bool boolA = (bool)inputA;
        bool boolB = (bool)inputB;
        bool boolC = (bool)inputC;
        bool boolD = (bool)inputD;
        bool boolLt = lt == null ? true : (bool)lt;
        bool boolBiRbo = bi_rbo == null ? true : (bool)bi_rbo;
        bool boolRbi = rbi == null ? true : (bool)rbi;

        // Convert binary to decimal (0-15)
        int value = (boolD ? 8 : 0) + (boolC ? 4 : 0) + (boolB ? 2 : 0) + (boolA ? 1 : 0);

        // Get the 7-segment output pattern for this value
        var segments = Get7SegmentPattern(value);

        // Apply blanking if needed
        if (!boolLt || !boolBiRbo || (value == 0 && !boolRbi))
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
            hasVcc = true,
            hasGnd = true,
            value = value,
            inputHasConflict = inputHasConflict,
            outputHasConflict = outputHasConflict
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

        // Detect input conflicts - check if multiple input pins share the same net
        bool inputHasConflict = false;
        var inputPins = new[] { "pin1", "pin2", "pin3" }; // A0, A1, A2
        var inputNetIds = new List<int>();

        foreach (var pin in inputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (inputNetIds.Contains(netId))
                {
                    inputHasConflict = true;
                    break;
                }
                inputNetIds.Add(netId);
            }
        }

        // Detect output conflicts - check if multiple output pins share the same net
        bool outputHasConflict = false;
        var outputPins = new[] { "pin15", "pin14", "pin13", "pin12", "pin11", "pin10", "pin9", "pin7" }; // O0-O7
        var outputNetIds = new List<int>();

        foreach (var pin in outputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (outputNetIds.Contains(netId))
                {
                    outputHasConflict = true;
                    break;
                }
                outputNetIds.Add(netId);
            }
        }

        // Get address inputs (A0, A1, A2) using connection analysis
        bool a0 = GetInputState(connectedNets["pin1"], nets);
        bool a1 = GetInputState(connectedNets["pin2"], nets);
        bool a2 = GetInputState(connectedNets["pin3"], nets);

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
        var outputPinMapping = new Dictionary<string, string> {
            {"O0", "pin15"}, {"O1", "pin14"}, {"O2", "pin13"}, {"O3", "pin12"},
            {"O4", "pin11"}, {"O5", "pin10"}, {"O6", "pin9"}, {"O7", "pin7"}
        };

        // Update output pins
        foreach (var kvp in outputPinMapping)
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
            enabled = enabled,
            inputHasConflict = inputHasConflict,
            outputHasConflict = outputHasConflict
        });
    }

    // Helper method to determine input state based on connections
    private bool GetInputState(int netId, List<Net> nets)
    {
        Net net = nets[netId];

        // Check if this net has PWR and GND connections
        bool hasPower = net.Nodes.Any(n => n.Contains("PWR"));
        bool hasGround = net.Nodes.Any(n => n.Contains("GND"));
        bool hasResistor = net.SourceComponents.Any(c => c.StartsWith("resistor"));

        // If connected to PWR through resistor but no GND → DIP switch OFF → HIGH (pull-up)
        if (hasPower && hasResistor && !hasGround)
        {
            return true; // HIGH
        }

        // If connected to PWR through resistor AND GND → DIP switch ON → LOW (pulled to ground)
        if (hasPower && hasResistor && hasGround)
        {
            return false; // LOW
        }

        // Fallback to actual node state for other cases
        return net.State == NodeState.HIGH;
    }

    // Evaluate 74148 8-to-3 priority encoder IC
    private (bool statesChanged, object state) EvaluateIC74148(
        Dictionary<string, int> connectedNets, List<Net> nets, string componentId, bool hasVcc = true, bool hasGnd = true)
    {
        bool statesChanged = false;

        // Detect input conflicts - check if multiple input pins share the same net
        bool inputHasConflict = false;
        var inputPins = new[] { "pin10", "pin11", "pin12", "pin13", "pin1", "pin2", "pin3", "pin4", "pin5" }; // I0-I7, EI
        var inputNetIds = new List<int>();

        foreach (var pin in inputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (inputNetIds.Contains(netId))
                {
                    inputHasConflict = true;
                    break;
                }
                inputNetIds.Add(netId);
            }
        }

        // Detect output conflicts - check if multiple output pins share the same net
        bool outputHasConflict = false;
        var outputPins = new[] { "pin9", "pin7", "pin6", "pin14", "pin15" }; // A0, A1, A2, GS, E0
        var outputNetIds = new List<int>();

        foreach (var pin in outputPins)
        {
            if (connectedNets.ContainsKey(pin))
            {
                int netId = connectedNets[pin];
                if (outputNetIds.Contains(netId))
                {
                    outputHasConflict = true;
                    break;
                }
                outputNetIds.Add(netId);
            }
        }

        // Get enable input (EI) - active LOW
        bool ei = !connectedNets.ContainsKey("pin5") ||
                  nets[connectedNets["pin5"]].State == NodeState.LOW;

        // Initialize inputs array (all HIGH/inactive by default)
        bool[] inputs = new bool[8];
        for (int i = 0; i < 8; i++)
        {
            inputs[i] = true;  // Default to inactive (HIGH)
        }

        // Get input states using GetInputState (active LOW)
        // Input 0 (pin 10)
        if (connectedNets.ContainsKey("pin10"))
            inputs[0] = !GetInputState(connectedNets["pin10"], nets);

        // Input 1 (pin 11)
        if (connectedNets.ContainsKey("pin11"))
            inputs[1] = !GetInputState(connectedNets["pin11"], nets);

        // Input 2 (pin 12)
        if (connectedNets.ContainsKey("pin12"))
            inputs[2] = !GetInputState(connectedNets["pin12"], nets);

        // Input 3 (pin 13)
        if (connectedNets.ContainsKey("pin13"))
            inputs[3] = !GetInputState(connectedNets["pin13"], nets);

        // Input 4 (pin 1)
        if (connectedNets.ContainsKey("pin1"))
            inputs[4] = !GetInputState(connectedNets["pin1"], nets);

        // Input 5 (pin 2)
        if (connectedNets.ContainsKey("pin2"))
            inputs[5] = !GetInputState(connectedNets["pin2"], nets);

        // Input 6 (pin 3)
        if (connectedNets.ContainsKey("pin3"))
            inputs[6] = !GetInputState(connectedNets["pin3"], nets);

        // Input 7 (pin 4)
        if (connectedNets.ContainsKey("pin4"))
            inputs[7] = !GetInputState(connectedNets["pin4"], nets);

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
            hasVcc = true,
            hasGnd = true,
            outputs = new { A0 = a0, A1 = a1, A2 = a2, GS = gs, E0 = e0 },
            inputHasConflict = inputHasConflict,
            outputHasConflict = outputHasConflict
        });
    }
}