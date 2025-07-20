using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class EvaluateBCD
{
    // Instance dictionary to persist completed input patterns across evaluations
    private Dictionary<string, HashSet<int>> completedInputPatterns = new Dictionary<string, HashSet<int>>();

    public ExperimentResult EvaluateBCDTo7SegmentExperiment(
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
        int ic7448Count = 0;
        int sevenSegmentCount = 0;
        int resistorCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        string mainDisplay = "";
        List<string> resistorIds = new List<string>();
        List<string> switchIds = new List<string>();

        // Get component data
        dynamic ic7448State = null;
        dynamic sevenSegmentState = null;

        // Debug log component states for troubleshooting
        Debug.Log($"BCD Component States: {string.Join(", ", simResult.ComponentStates.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");

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
                sevenSegmentCount++;
                sevenSegmentState = comp.Value;
                mainDisplay = comp.Key;
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

        // Steps 0-11: Pull-up resistor inputs (4 sets of resistor-switch-ground)
        for (int i = 0; i < 12 && canProceed; i += 3)
        {
            int resistorStep = i;
            int switchStep = i + 1;
            int groundStep = i + 2;
            int inputNumber = (i / 3) + 1;

            if (canProceed && CheckResistorStep(resistorCount, resistorIds, simResult, inputNumber))
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

            if (canProceed && CheckSwitchStep(resistorCount, dipSwitchCount, switchIds, simResult, inputNumber))
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

            if (canProceed && CheckGroundStep(dipSwitchCount, switchIds, simResult, inputNumber))
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

        // Step 12: Add IC7448
        if (canProceed && CheckStep12(ic7448Count))
        {
            completedInstructions[experiment.Id].Add(12);
            result.InstructionResults[12] = true;
            currentStep = 13;
        }
        else
        {
            canProceed = false;
        }

        // Steps 13-14: IC power connections
        if (canProceed && CheckStep13(ic7448Count, ic7448State))
        {
            completedInstructions[experiment.Id].Add(13);
            result.InstructionResults[13] = true;
            currentStep = 14;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep14(ic7448Count, ic7448State))
        {
            completedInstructions[experiment.Id].Add(14);
            result.InstructionResults[14] = true;
            currentStep = 15;
        }
        else
        {
            canProceed = false;
        }

        // Steps 15-18: IC input connections (A, B, C, D) - individual step checking
        if (canProceed && CheckStep15_InputA(ic7448Count, resistorCount, dipSwitchCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(15);
            result.InstructionResults[15] = true;
            currentStep = 16;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep16_InputB(ic7448Count, resistorCount, dipSwitchCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(16);
            result.InstructionResults[16] = true;
            currentStep = 17;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep17_InputC(ic7448Count, resistorCount, dipSwitchCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(17);
            result.InstructionResults[17] = true;
            currentStep = 18;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep18_InputD(ic7448Count, resistorCount, dipSwitchCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(18);
            result.InstructionResults[18] = true;
            currentStep = 19;
        }
        else
        {
            canProceed = false;
        }

        // Steps 19-21: IC control connections (LT, RBO, RBI)
        for (int i = 19; i <= 21 && canProceed; i++)
        {
            if (CheckControlConnection(i, ic7448Count, ic7448State))
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

        // Step 22: Add 7-segment display
        if (canProceed && CheckStep22(sevenSegmentCount))
        {
            completedInstructions[experiment.Id].Add(22);
            result.InstructionResults[22] = true;
            currentStep = 23;
        }
        else
        {
            canProceed = false;
        }

        // Step 23: Ground 7-segment display
        if (canProceed && CheckStep23(sevenSegmentCount, sevenSegmentState))
        {
            completedInstructions[experiment.Id].Add(23);
            result.InstructionResults[23] = true;
            currentStep = 24;
        }
        else
        {
            canProceed = false;
        }

        // Steps 24-30: IC to 7-segment connections (F G A B C D E order)
        if (canProceed && CheckStep24_SegmentF(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(24);
            result.InstructionResults[24] = true;
            currentStep = 25;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep25_SegmentG(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(25);
            result.InstructionResults[25] = true;
            currentStep = 26;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep26_SegmentA(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(26);
            result.InstructionResults[26] = true;
            currentStep = 27;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep27_SegmentB(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(27);
            result.InstructionResults[27] = true;
            currentStep = 28;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep28_SegmentC(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(28);
            result.InstructionResults[28] = true;
            currentStep = 29;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep29_SegmentD(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(29);
            result.InstructionResults[29] = true;
            currentStep = 30;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep30_SegmentE(ic7448Count, sevenSegmentCount, components, mainIc, simResult, ic7448State, result))
        {
            completedInstructions[experiment.Id].Add(30);
            result.InstructionResults[30] = true;
            currentStep = 31;
        }
        else
        {
            canProceed = false;
        }

        // Steps 31-46: Test input combinations with sequential validation and persistent tracking
        if (canProceed)
        {
            // Check for new completed patterns and add them to persistent storage
            // But only check the next expected pattern in sequence
            int nextExpectedPattern = 31;
            
            // Find the next uncompleted pattern based on persistent storage
            for (int i = 31; i <= 46; i++)
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
            if (nextExpectedPattern <= 46)
            {
                if (CheckInputPattern(nextExpectedPattern, ic7448Count, sevenSegmentCount, ic7448State, sevenSegmentState))
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
                for (int i = 31; i <= 46; i++)
                {
                    if (!completedInputPatterns[experiment.Id.ToString()].Contains(i))
                    {
                        currentStep = i;
                        break;
                    }
                }
                // If all patterns completed
                if (completedPatternCount == 16)
                {
                    currentStep = 47; // Beyond last step
                }
            }
            else
            {
                currentStep = 31; // Start with first pattern
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

        // Add IC state information
        if (ic7448State != null)
        {
            var stateType = ic7448State.GetType();
            var hasInputsProperty = stateType.GetProperty("inputs") != null;
            var hasControlProperty = stateType.GetProperty("control") != null;

            if (hasInputsProperty && hasControlProperty)
            {
                // Check if inputs are actually initialized (not null)
                var inputsValue = ic7448State.inputs;
                var controlValue = ic7448State.control;

                if (inputsValue != null && controlValue != null)
                {
                    // Safely check each input for null before casting to bool
                    object aObj = ic7448State.inputs.A;
                    object bObj = ic7448State.inputs.B;
                    object cObj = ic7448State.inputs.C;
                    object dObj = ic7448State.inputs.D;
                    object ltObj = ic7448State.control.LT;
                    object rbiObj = ic7448State.control.RBI;
                    object rboObj = ic7448State.control.BI_RBO;

                    // Only process if all inputs are connected (not null)
                    if (aObj != null && bObj != null && cObj != null && dObj != null &&
                        ltObj != null && rbiObj != null && rboObj != null)
                    {
                        bool a = (bool)aObj;
                        bool b = (bool)bObj;
                        bool c = (bool)cObj;
                        bool d = (bool)dObj;
                        bool lt = (bool)ltObj;
                        bool rbi = (bool)rbiObj;
                        bool rbo = (bool)rboObj;

                        // Calculate BCD value
                        int bcdValue = (d ? 8 : 0) + (c ? 4 : 0) + (b ? 2 : 0) + (a ? 1 : 0);
                    }
                    // If any inputs are null (uninitialized), just continue without error
                }
                // If inputs or control are null (uninitialized), just continue without error
            }
            else
            {
                // Handle IC error states - but only if error property exists
                string errorMsg = "";
                try
                {
                    // Check if error property exists before accessing it
                    var errorProperty = stateType.GetProperty("error");
                    if (errorProperty != null)
                    {
                        var error = errorProperty.GetValue(ic7448State);
                        if (error != null)
                        {
                            errorMsg = error.ToString();
                            Debug.LogWarning($"IC 7448 reported error: {errorMsg}");
                        }
                    }
                    else
                    {
                        // No error property means this is likely an uninitialized state
                        // Check the status property instead
                        var statusProperty = stateType.GetProperty("status");
                        if (statusProperty != null)
                        {
                            var status = statusProperty.GetValue(ic7448State);
                            if (status != null && status.ToString() != "Uninitialized inputs")
                            {
                                // Only report non-uninitialized status issues
                                errorMsg = $"IC status: {status}";
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error accessing IC 7448 state: {ex.Message}");
                    Debug.LogException(ex);
                    // Don't set error message for reflection errors on uninitialized states
                }

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    // result.Messages.Add($"IC 7448 Error: {errorMsg}");
                }
            }
        }

        return result;
    }

    // Helper methods for checking each step
    private bool CheckResistorStep(int resistorCount, List<string> resistorIds, BreadboardSimulator.SimulationResult simResult, int inputNumber)
    {
        if (resistorCount >= inputNumber)
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
            return resistorsConnectedToPower >= inputNumber;
        }
        return false;
    }

    private bool CheckSwitchStep(int resistorCount, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult, int inputNumber)
    {
        if (resistorCount >= inputNumber && dipSwitchCount >= inputNumber)
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
            return switchesInSeries >= inputNumber;
        }
        return false;
    }

    private bool CheckGroundStep(int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult, int inputNumber)
    {
        if (dipSwitchCount >= inputNumber)
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
            return switchesGrounded >= inputNumber;
        }
        return false;
    }

    private bool CheckStep12(int ic7448Count)
    {
        return ic7448Count >= 1;
    }

    private bool CheckStep13(int ic7448Count, dynamic ic7448State)
    {
        if (ic7448Count >= 1 && ic7448State != null)
        {
            try
            {
                return ic7448State.hasGnd == true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep14(int ic7448Count, dynamic ic7448State)
    {
        if (ic7448Count >= 1 && ic7448State != null)
        {
            try
            {
                return ic7448State.hasVcc == true && ic7448State.hasGnd == true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep15_InputA(int ic7448Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for input conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasInputConflictProperty = stateType.GetProperty("inputHasConflict") != null;

                if (hasInputConflictProperty)
                {
                    bool inputHasConflict = ic7448State.inputHasConflict;
                    if (inputHasConflict)
                    {
                        result.Messages.Add("Input pins cannot share the same network connection.");
                        return false; // Fail the step if input conflict detected
                    }
                }

                // Check if input A is connected (not null)
                var hasInputsProperty = stateType.GetProperty("inputs") != null;
                if (hasInputsProperty)
                {
                    object inputA = ic7448State.inputs.A;
                    return inputA != null; // Input A should be connected (true or false, not null)
                }
            }
            catch { }
        }
        return false;
    }

    private bool CheckStep16_InputB(int ic7448Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for input conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasInputConflictProperty = stateType.GetProperty("inputHasConflict") != null;

                if (hasInputConflictProperty)
                {
                    bool inputHasConflict = ic7448State.inputHasConflict;
                    if (inputHasConflict)
                    {
                        result.Messages.Add("Input pins cannot share the same network connection.");
                        return false; // Fail the step if input conflict detected
                    }
                }

                // Check if input B is connected (not null)
                var hasInputsProperty = stateType.GetProperty("inputs") != null;
                if (hasInputsProperty)
                {
                    object inputB = ic7448State.inputs.B;
                    return inputB != null; // Input B should be connected (true or false, not null)
                }
            }
            catch { }
        }
        return false;
    }

    private bool CheckStep17_InputC(int ic7448Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for input conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasInputConflictProperty = stateType.GetProperty("inputHasConflict") != null;

                if (hasInputConflictProperty)
                {
                    bool inputHasConflict = ic7448State.inputHasConflict;
                    if (inputHasConflict)
                    {
                        result.Messages.Add("Input pins cannot share the same network connection.");
                        return false; // Fail the step if input conflict detected
                    }
                }

                // Check if input C is connected (not null)
                var hasInputsProperty = stateType.GetProperty("inputs") != null;
                if (hasInputsProperty)
                {
                    object inputC = ic7448State.inputs.C;
                    return inputC != null; // Input C should be connected (true or false, not null)
                }
            }
            catch { }
        }
        return false;
    }

    private bool CheckStep18_InputD(int ic7448Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for input conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasInputConflictProperty = stateType.GetProperty("inputHasConflict") != null;

                if (hasInputConflictProperty)
                {
                    bool inputHasConflict = ic7448State.inputHasConflict;
                    if (inputHasConflict)
                    {
                        result.Messages.Add("Input pins cannot share the same network connection.");
                        return false; // Fail the step if input conflict detected
                    }
                }

                // Check if input D is connected (not null)
                var hasInputsProperty = stateType.GetProperty("inputs") != null;
                if (hasInputsProperty)
                {
                    object inputD = ic7448State.inputs.D;
                    return inputD != null; // Input D should be connected (true or false, not null)
                }
            }
            catch { }
        }
        return false;
    }

    // Also update CheckControlConnection to handle null values properly
    private bool CheckControlConnection(int stepIndex, int ic7448Count, dynamic ic7448State)
    {
        if (ic7448Count >= 1 && ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasControlProperty = stateType.GetProperty("control") != null;
                if (hasControlProperty)
                {
                    // Check which control pin based on step index
                    if (stepIndex == 19) // LT
                    {
                        object lt = ic7448State.control.LT;
                        return lt != null && (bool)lt == true;
                    }
                    else if (stepIndex == 20) // RBO
                    {
                        object rbo = ic7448State.control.BI_RBO;
                        return rbo != null && (bool)rbo == true;
                    }
                    else if (stepIndex == 21) // RBI
                    {
                        object rbi = ic7448State.control.RBI;
                        return rbi != null && (bool)rbi == true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep22(int sevenSegmentCount)
    {
        return sevenSegmentCount >= 1;
    }

    private bool CheckStep23(int sevenSegmentCount, dynamic sevenSegmentState)
    {
        if (sevenSegmentCount >= 1 && sevenSegmentState != null)
        {
            try
            {
                return sevenSegmentState.grounded == true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private bool CheckStep24_SegmentF(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment F output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    Debug.Log("Checking IC7448 Segment F output");
                    bool? segmentF = ic7448State.outputs["F"];
                    Debug.Log($"Segment F value: {segmentF}");
                    Debug.Log("CHECKING SEGMENT F");
                    
                    if (segmentF == null)
                    {
                        return false; // Segment F should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 15 (F output) and 7-segment nodeF
                    return CheckSegmentConnection(components, mainIc, "pin15", "nodeF", simResult, result, "F");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep24_SegmentF: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep25_SegmentG(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment G output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentG = ic7448State.outputs["G"];
                    if (segmentG == null)
                    {
                        return false; // Segment G should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 14 (G output) and 7-segment nodeG
                    return CheckSegmentConnection(components, mainIc, "pin14", "nodeG", simResult, result, "G");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep25_SegmentG: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep26_SegmentA(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment A output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentA = ic7448State.outputs["A"];
                    if (segmentA == null)
                    {
                        return false; // Segment A should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 13 (A output) and 7-segment nodeA
                    return CheckSegmentConnection(components, mainIc, "pin13", "nodeA", simResult, result, "A");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep26_SegmentA: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep27_SegmentB(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment B output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentB = ic7448State.outputs["B"];
                    if (segmentB == null)
                    {
                        return false; // Segment B should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 12 (B output) and 7-segment nodeB
                    return CheckSegmentConnection(components, mainIc, "pin12", "nodeB", simResult, result, "B");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep27_SegmentB: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep28_SegmentC(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment C output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentC = ic7448State.outputs["C"];
                    if (segmentC == null)
                    {
                        return false; // Segment C should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 11 (C output) and 7-segment nodeC
                    return CheckSegmentConnection(components, mainIc, "pin11", "nodeC", simResult, result, "C");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep28_SegmentC: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep29_SegmentD(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment D output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentD = ic7448State.outputs["D"];
                    if (segmentD == null)
                    {
                        return false; // Segment D should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 10 (D output) and 7-segment nodeD
                    return CheckSegmentConnection(components, mainIc, "pin10", "nodeD", simResult, result, "D");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep29_SegmentD: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckStep30_SegmentE(int ic7448Count, int sevenSegmentCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, dynamic ic7448State, ExperimentResult result)
    {
        // Check for output conflicts first
        if (ic7448State != null)
        {
            try
            {
                var stateType = ic7448State.GetType();
                var hasOutputConflictProperty = stateType.GetProperty("outputHasConflict") != null;

                if (hasOutputConflictProperty)
                {
                    bool outputHasConflict = ic7448State.outputHasConflict;
                    if (outputHasConflict)
                    {
                        result.Messages.Add("Output pins cannot share the same network connection.");
                        return false; // Fail the step if output conflict detected
                    }
                }

                // Check if segment E output is connected (not null)
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                if (hasOutputsProperty)
                {
                    bool? segmentE = ic7448State.outputs["E"];
                    if (segmentE == null)
                    {
                        return false; // Segment E should be connected (true or false, not null)
                    }

                    // Now check actual connection between IC pin 9 (E output) and 7-segment nodeE
                    return CheckSegmentConnection(components, mainIc, "pin9", "nodeE", simResult, result, "E");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in CheckStep30_SegmentE: {ex.Message}");
            }
        }
        return false;
    }

    private bool CheckInputPattern(int stepIndex, int ic7448Count, int sevenSegmentCount, dynamic ic7448State, dynamic sevenSegmentState)
    {
        if (ic7448Count >= 1 && ic7448State != null)
        {
            try
            {
                // Get expected pattern for this step
                int patternIndex = stepIndex - 31; // 0-15 for patterns 0000-1111

                // Get current value directly from IC7448 state
                int currentValue = ic7448State.value;

                // Check if current value matches expected pattern
                return currentValue == patternIndex;
            }
            catch
            {
                return false;
            }
        }
        return false;
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
        value = Mathf.Max(0, Mathf.Min(15, value));

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

    // Method to reset completed input patterns (call this when starting a new experiment)
    public void ResetCompletedInputPatterns(string experimentId)
    {
        if (completedInputPatterns.ContainsKey(experimentId))
        {
            completedInputPatterns[experimentId].Clear();
        }
    }

    // Method to get completed input patterns count (for debugging/UI)
    public int GetCompletedInputPatternsCount(string experimentId)
    {
        if (completedInputPatterns.ContainsKey(experimentId))
        {
            return completedInputPatterns[experimentId].Count;
        }
        return 0;
    }

    // Helper method to check if IC output pin is properly connected to 7-segment input node
    private bool CheckSegmentConnection(JToken components, string mainIc, string icPin, string segmentNode, BreadboardSimulator.SimulationResult simResult, ExperimentResult result, string segmentName)
    {
        // Extract IC and 7-segment components
        JToken icComp = null;
        JToken sevenSegComp = null;
        string mainSevenSeg = "";

        foreach (JProperty componentProp in components)
        {
            if (componentProp.Name.Equals(mainIc))
            {
                icComp = componentProp.Value;
            }
            else if (componentProp.Name.StartsWith("sevenSeg"))
            {
                sevenSegComp = componentProp.Value;
                mainSevenSeg = componentProp.Name;
            }
        }

        // Check if required components are found
        if (icComp == null || sevenSegComp == null)
        {
            Debug.Log($"[Segment {segmentName}] Missing components - IC: {icComp != null}, Seven Segment: {sevenSegComp != null}");
            result.Messages.Add($"Missing IC or Seven Segment component for segment {segmentName} connection check.");
            return false;
        }

        // Get pin and node values
        string pinValue = icComp[icPin]?.ToString();
        string nodeValue = sevenSegComp[segmentNode]?.ToString();

        Debug.Log($"[Segment {segmentName}] Pin value: {pinValue}, Node value: {nodeValue}");

        if (string.IsNullOrEmpty(pinValue) || string.IsNullOrEmpty(nodeValue))
        {
            Debug.Log($"[Segment {segmentName}] Empty connections - Pin: {pinValue}, Node: {nodeValue}");
            result.Messages.Add($"IC {icPin} or 7-segment {segmentNode} not properly connected for segment {segmentName}.");
            return false;
        }

        // Check if nodes are electrically connected using the existing AreNodesConnected logic
        bool connected = AreNodesConnected(pinValue, nodeValue, simResult.Nets);
        Debug.Log($"[Segment {segmentName}] Connection check between {pinValue} and {nodeValue}: {connected}");

        if (!connected)
        {
            result.Messages.Add($"IC {icPin} output is not properly connected to 7-segment {segmentNode} input for segment {segmentName}.");
            return false;
        }

        return true;
    }

    // Helper method to check if two nodes are electrically connected (copied from BreadboardSimulator)
    private bool AreNodesConnected(string nodeA, string nodeB, List<BreadboardSimulator.Net> nets)
    {
        // Quick check - if nodes are identical, they're connected
        if (nodeA == nodeB)
        {
            return true;
        }

        // Build node-to-net lookup map
        var nodeToNetMap = new Dictionary<string, int>();
        for (int i = 0; i < nets.Count; i++)
        {
            foreach (var node in nets[i].Nodes)
            {
                nodeToNetMap[node] = i;
            }
        }

        // Check if both nodes exist in our nets
        if (!nodeToNetMap.ContainsKey(nodeA) || !nodeToNetMap.ContainsKey(nodeB))
        {
            return false;
        }

        // Check if nodes are in the same net
        return nodeToNetMap[nodeA] == nodeToNetMap[nodeB];
    }
}
