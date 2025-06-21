using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Evaluate74148
{
    // Static dictionary to persist completed input patterns across evaluations
    private static Dictionary<string, HashSet<int>> completedInputPatterns = new Dictionary<string, HashSet<int>>();

    public ExperimentResult Evaluate74148To3LED(
        BreadboardSimulator.SimulationResult simResult,
        ExperimentDefinition experiment,
        JToken components,
        ExperimentDefinitions experimentDefinitions)
    {
        var result = new ExperimentResult
        {
            ExperimentName = experiment.Name,
            ExperimentId = experiment.Id,
            TotalInstructions = experiment.TotalInstructions,
            MainInstruction = "", // Will be set based on current step
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };

        // Initialize completed input patterns tracking for this experiment if not exists
        if (!completedInputPatterns.ContainsKey(experiment.Id.ToString()))
        {
            completedInputPatterns[experiment.Id.ToString()] = new HashSet<int>();
        }

        // Count components and get component lists
        int ic74148Count = 0;
        int ledCount = 0;
        int resistorCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        List<string> resistorIds = new List<string>();
        List<string> switchIds = new List<string>();
        List<string> ledIds = new List<string>();

        // Get component data
        dynamic ic74148State = null;

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
                ledCount++;
                ledIds.Add(comp.Key);
            }
            else if (comp.Key.StartsWith("resistor"))
            {
                resistorCount++;
                resistorIds.Add(comp.Key);
            }
            else if (comp.Key.StartsWith("dipSwitch"))
            {
                dipSwitchCount++;
                switchIds.Add(comp.Key);
            }
        }

        var completedInstructions = experimentDefinitions.GetCompletedInstructions();

        // Clear all completed instructions first for linear progression
        completedInstructions[experiment.Id].Clear();

        // Linear progression checking - each step must be completed in order
        int currentStep = 0;
        bool canProceed = true;

        // Steps 0-23: Create 8 pull-up resistor inputs (resistor + switch + ground for each)
        for (int i = 0; i < 8 && canProceed; i++)
        {
            int resistorStep = i * 3;
            int switchStep = i * 3 + 1;
            int groundStep = i * 3 + 2;

            // Check resistor step
            if (canProceed && CheckResistorStep(i + 1, resistorCount, resistorIds, simResult))
            {
                completedInstructions[experiment.Id].Add(resistorStep);
                result.InstructionResults[resistorStep] = true;
                currentStep = switchStep;
            }
            else
            {
                canProceed = false;
                break;
            }

            // Check switch step
            if (canProceed && CheckSwitchStep(i + 1, resistorCount, dipSwitchCount, switchIds, simResult))
            {
                completedInstructions[experiment.Id].Add(switchStep);
                result.InstructionResults[switchStep] = true;
                currentStep = groundStep;
            }
            else
            {
                canProceed = false;
                break;
            }

            // Check ground step
            if (canProceed && CheckGroundStep(i + 1, dipSwitchCount, switchIds, simResult))
            {
                completedInstructions[experiment.Id].Add(groundStep);
                result.InstructionResults[groundStep] = true;
                currentStep = groundStep + 1;
            }
            else
            {
                canProceed = false;
                break;
            }
        }

        // Step 24: Add IC74148
        if (canProceed && CheckStep24(ic74148Count))
        {
            completedInstructions[experiment.Id].Add(24);
            result.InstructionResults[24] = true;
            currentStep = 25;
        }
        else
        {
            canProceed = false;
        }

        // Step 25: Ground IC74148
        if (canProceed && CheckStep25(ic74148Count, ic74148State))
        {
            completedInstructions[experiment.Id].Add(25);
            result.InstructionResults[25] = true;
            currentStep = 26;
        }
        else
        {
            canProceed = false;
        }

        // Step 26: VCC IC74148
        if (canProceed && CheckStep26(ic74148Count, ic74148State))
        {
            completedInstructions[experiment.Id].Add(26);
            result.InstructionResults[26] = true;
            currentStep = 27;
        }
        else
        {
            canProceed = false;
        }

        // Step 27: Add 3 LEDs
        if (canProceed && CheckStep27(ledCount))
        {
            completedInstructions[experiment.Id].Add(27);
            result.InstructionResults[27] = true;
            currentStep = 28;
        }
        else
        {
            canProceed = false;
        }

        // Step 28: Ground LEDs
        if (canProceed && CheckStep28(ledCount, ledIds, simResult))
        {
            completedInstructions[experiment.Id].Add(28);
            result.InstructionResults[28] = true;
            currentStep = 29;
        }
        else
        {
            canProceed = false;
        }

        // Steps 29-31: Connect IC outputs to LEDs (with output conflict detection)
        for (int i = 29; i <= 31 && canProceed; i++)
        {
            if (CheckLEDConnectionStep(i, ic74148Count, ledCount, components, mainIc, simResult, ic74148State, result))
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
                currentStep = i + 1;
            }
            else
            {
                canProceed = false;
            }
        }

        // Step 32: EI set to LOW
        if (canProceed && CheckStep32(ic74148Count, ic74148State))
        {
            completedInstructions[experiment.Id].Add(32);
            result.InstructionResults[32] = true;
            currentStep = 33;
        }
        else
        {
            canProceed = false;
        }

        // Steps 33-40: Connect IC inputs to switches (with input conflict detection)
        for (int i = 33; i <= 40 && canProceed; i++)
        {
            if (CheckInputConnectionStep(i, ic74148Count, dipSwitchCount, components, mainIc, simResult, ic74148State, result))
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
                currentStep = i + 1;
            }
            else
            {
                canProceed = false;
            }
        }

        // Steps 41-48: Test input combinations with sequential validation and persistent tracking
        if (canProceed)
        {
            // Check for new completed patterns and add them to persistent storage
            // But only check the next expected pattern in sequence
            int nextExpectedPattern = 41;
            
            // Find the next uncompleted pattern based on persistent storage
            for (int i = 41; i <= 48; i++)
            {
                if (completedInputPatterns[experiment.Id.ToString()].Contains(i))
                {
                    nextExpectedPattern = i + 1;
                }
                else
                {
                    nextExpectedPattern = i;
                    break;
                }
            }
            
            // Only check the next expected pattern (sequential validation)
            if (nextExpectedPattern <= 48)
            {
                if (CheckInputPattern(nextExpectedPattern, ic74148Count, ic74148State))
                {
                    // Add to persistent storage
                    completedInputPatterns[experiment.Id.ToString()].Add(nextExpectedPattern);
                }
            }
            
            // Add all previously completed patterns to current session
            foreach (int completedPattern in completedInputPatterns[experiment.Id.ToString()])
            {
                completedInstructions[experiment.Id].Add(completedPattern);
                result.InstructionResults[completedPattern] = true;
            }
            
            // Determine current step based on completed patterns
            int completedPatternCount = completedInputPatterns[experiment.Id.ToString()].Count;
            if (completedPatternCount > 0)
            {
                // Find the next uncompleted pattern
                for (int i = 41; i <= 48; i++)
                {
                    if (!completedInputPatterns[experiment.Id.ToString()].Contains(i))
                    {
                        currentStep = i;
                        break;
                    }
                }
                // If all patterns completed
                if (completedPatternCount == 8)
                {
                    currentStep = 49; // Beyond last step
                }
            }
            else
            {
                currentStep = 41; // Start with first pattern
            }
        }

        // Count completed instructions
        result.CompletedInstructions = completedInstructions[experiment.Id].Count;

        // Set the MainInstruction to the current step instruction
        if (currentStep < experiment.TotalInstructions)
        {
            result.MainInstruction = experiment.InstructionDescriptions[currentStep];
        }
        else
        {
            result.MainInstruction = "Experiment completed! All steps finished.";
        }

        // Add IC state information (without conflict detection here)
        if (ic74148State != null)
        {
            try
            {
                // Check if the IC state has the required properties
                var stateType = ic74148State.GetType();
                var hasOutputProperty = stateType.GetProperty("outputs") != null;

                if (hasOutputProperty)
                {
                    // Access outputs through the outputs object
                    dynamic outputs = ic74148State.outputs;
                    bool a0 = outputs.A0;
                    bool a1 = outputs.A1;
                    bool a2 = outputs.A2;
                    bool gs = outputs.GS;
                    bool e0 = outputs.E0;

                    // Calculate expected output based on highest priority input
                    int binaryOutput = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);
                    // result.Messages.Add($"IC 74148 Output: A2={a2}, A1={a1}, A0={a0} (Binary: {binaryOutput:D3})");
                }
                else
                {
                    // IC doesn't have proper power connections
                    string errorMsg = "";
                    try
                    {
                        errorMsg = ic74148State.error ?? "Unknown IC error";
                    }
                    catch
                    {
                        errorMsg = "IC power connection issue";
                    }
                    result.Messages.Add($"IC 74148 Error: {errorMsg}");
                }
            }
            catch (System.Exception ex)
            {
                result.Messages.Add($"IC 74148 Error: {ex.Message}");
            }
        }

        return result;
    }

    // Helper methods for checking each step
    private bool CheckResistorStep(int resistorNumber, int resistorCount, List<string> resistorIds, BreadboardSimulator.SimulationResult simResult)
    {
        if (resistorCount >= resistorNumber)
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
            return resistorsConnectedToPower >= resistorNumber;
        }
        return false;
    }

    private bool CheckSwitchStep(int switchNumber, int resistorCount, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
        if (resistorCount >= switchNumber && dipSwitchCount >= switchNumber)
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
            return switchesInSeries >= switchNumber;
        }
        return false;
    }

    private bool CheckGroundStep(int switchNumber, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
        if (dipSwitchCount >= switchNumber)
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
            return switchesGrounded >= switchNumber;
        }
        return false;
    }

    private bool CheckStep24(int ic74148Count)
    {
        return ic74148Count >= 1;
    }

    private bool CheckStep25(int ic74148Count, dynamic ic74148State)
    {
        if (ic74148Count >= 1 && ic74148State != null)
        {
            try
            {
                bool hasGnd = ic74148State.hasGnd ?? false;
                return hasGnd;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep26(int ic74148Count, dynamic ic74148State)
    {
        if (ic74148Count >= 1 && ic74148State != null)
        {
            try
            {
                bool hasVcc = ic74148State.hasVcc ?? false;
                return hasVcc;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep27(int ledCount)
    {
        return ledCount >= 3;
    }

    private bool CheckStep28(int ledCount, List<string> ledIds, BreadboardSimulator.SimulationResult simResult)
    {
        if (ledCount >= 3)
        {
            int ledsGrounded = 0;
            foreach (string ledId in ledIds)
            {
                if (simResult.ComponentStates.ContainsKey(ledId))
                {
                    dynamic ledState = simResult.ComponentStates[ledId];
                    bool isGrounded = ledState.grounded ?? false;
                    if (isGrounded)
                    {
                        ledsGrounded++;
                    }
                }
            }
            return ledsGrounded >= 3;
        }
        return false;
    }

    private bool CheckLEDConnectionStep(int stepIndex, int ic74148Count, int ledCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic74148State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic74148State != null)
        {
            try
            {
                var stateType = ic74148State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic74148State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }
            }
            catch { }
        }

        if (ic74148Count >= 1 && ledCount >= 3)
        {
            JToken icComp = components[mainIc];
            if (icComp != null)
            {
                // Get the output pin name based on step (29=A0, 30=A1, 31=A2)
                string targetPin = "";
                switch (stepIndex)
                {
                    case 29: targetPin = "pin9"; break;  // A0
                    case 30: targetPin = "pin7"; break;  // A1
                    case 31: targetPin = "pin6"; break;  // A2
                    default: return false;
                }

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
    }

    private bool CheckStep32(int ic74148Count, dynamic ic74148State)
    {
        if (ic74148Count >= 1 && ic74148State != null)
        {
            try
            {
                bool enableIn = ic74148State.enableIn ?? false; // Default to true if not found
                return enableIn; // EI should be LOW (false)
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckInputConnectionStep(int stepIndex, int ic74148Count, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic74148State, ExperimentResult result)

    {
        // Check for input conflicts first
        if (ic74148State != null)
        {
            try
            {
                var stateType = ic74148State.GetType();
                var hasInputConflictProperty = stateType.GetProperty("inputHasConflict") != null;

                if (hasInputConflictProperty)
                {
                    bool inputHasConflict = ic74148State.inputHasConflict;
                    if (inputHasConflict)
                    {
                        result.Messages.Add("Input pins cannot share the same network connection.");
                        return false; // Fail the step if input conflict detected
                    }
                }
            }
            catch { }
        }

        if (ic74148Count >= 1 && dipSwitchCount >= 8)
        {
            JToken icComp = components[mainIc];
            if (icComp != null)
            {
                // Get the input pin name based on step (33=I0, 34=I1, etc.)
                string inputPin = "";
                switch (stepIndex)
                {
                    case 33: inputPin = "pin10"; break; // I0
                    case 34: inputPin = "pin11"; break; // I1
                    case 35: inputPin = "pin12"; break; // I2
                    case 36: inputPin = "pin13"; break; // I3
                    case 37: inputPin = "pin1"; break;  // I4
                    case 38: inputPin = "pin2"; break;  // I5
                    case 39: inputPin = "pin3"; break;  // I6
                    case 40: inputPin = "pin4"; break;  // I7
                    default: return false;
                }

                string icInputPin = icComp[inputPin]?.ToString();

                foreach (JProperty componentProp in components)
                {
                    if (componentProp.Name.StartsWith("resistor"))
                    {
                        JToken resistorComp = componentProp.Value;
                        string pin1Value = resistorComp["pin1"]?.ToString();
                        string pin2Value = resistorComp["pin2"]?.ToString();

                        if (AreNodesConnected(icInputPin, pin1Value, simResult.Nets) ||
                            AreNodesConnected(icInputPin, pin2Value, simResult.Nets))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool CheckInputPattern(int stepIndex, int ic74148Count, dynamic ic74148State)
    {
        if (ic74148Count < 1 || ic74148State == null)
            return false;

        try
        {
            // Get expected output pattern for this step
            int expectedPattern = stepIndex - 41; // 0-7 for steps 41-48

            // Get current IC outputs through the outputs object
            var stateType = ic74148State.GetType();
            var hasOutputProperty = stateType.GetProperty("outputs") != null;

            if (hasOutputProperty)
            {
                dynamic outputs = ic74148State.outputs;
                bool a0 = outputs.A0;
                bool a1 = outputs.A1;
                bool a2 = outputs.A2;

                // Convert to binary value
                int currentOutput = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);

                return currentOutput == expectedPattern;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    // Method to reset completed input patterns (call this when starting a new experiment)
    public static void ResetCompletedInputPatterns(string experimentId)
    {
        if (completedInputPatterns.ContainsKey(experimentId))
        {
            completedInputPatterns[experimentId].Clear();
        }
    }

    // Method to get completed input patterns count (for debugging/UI)
    public static int GetCompletedInputPatternsCount(string experimentId)
    {
        if (completedInputPatterns.ContainsKey(experimentId))
        {
            return completedInputPatterns[experimentId].Count;
        }
        return 0;
    }

    private bool AreNodesConnected(string node1, string node2, List<BreadboardSimulator.Net> nets)
    {
        if (string.IsNullOrEmpty(node1) || string.IsNullOrEmpty(node2))
            return false;

        foreach (var net in nets)
        {
            if (net.Nodes.Contains(node1) && net.Nodes.Contains(node2))
            {
                return true;
            }
        }
        return false;
    }
}