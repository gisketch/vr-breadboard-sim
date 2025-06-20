using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Evaluate74138
{
    public ExperimentResult Evaluate74138To8LED(
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

        // Count components and get component lists
        int ic74138Count = 0;
        int ledCount = 0;
        int resistorCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        List<string> resistorIds = new List<string>();
        List<string> switchIds = new List<string>();
        List<string> ledIds = new List<string>();

        // Get component data
        dynamic ic74138State = null;


        foreach (var comp in simResult.ComponentStates)
        {
            UnityEngine.Debug.Log($"Component States: {string.Join(", ", comp.Key + ":" + comp.Value)}");
            // Debug IC outputs
            if (comp.Value is Dictionary<string, bool> outputs)
            {
                UnityEngine.Debug.Log($"IC Outputs: {string.Join(", ", outputs.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
            }
            if (comp.Key.StartsWith("ic"))
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

        // Step 0: Add a resistor and connect it to any power terminal
        if (canProceed && CheckStep0(resistorCount, resistorIds, simResult))
        {
            completedInstructions[experiment.Id].Add(0);
            result.InstructionResults[0] = true;
            currentStep = 1;
        }
        else
        {
            canProceed = false;
        }

        // Step 1: Add a switch and connect it in series with the resistor
        if (canProceed && CheckStep1(resistorCount, dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(1);
            result.InstructionResults[1] = true;
            currentStep = 2;
        }
        else
        {
            canProceed = false;
        }

        // Step 2: Ground the switch
        if (canProceed && CheckStep2(dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(2);
            result.InstructionResults[2] = true;
            currentStep = 3;
        }
        else
        {
            canProceed = false;
        }

        // Step 3: Add a second resistor and connect it to any power terminal
        if (canProceed && CheckStep3(resistorCount, resistorIds, simResult))
        {
            completedInstructions[experiment.Id].Add(3);
            result.InstructionResults[3] = true;
            currentStep = 4;
        }
        else
        {
            canProceed = false;
        }

        // Step 4: Add a second switch and connect it in series with the second resistor
        if (canProceed && CheckStep4(resistorCount, dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(4);
            result.InstructionResults[4] = true;
            currentStep = 5;
        }
        else
        {
            canProceed = false;
        }

        // Step 5: Ground the second switch
        if (canProceed && CheckStep5(dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(5);
            result.InstructionResults[5] = true;
            currentStep = 6;
        }
        else
        {
            canProceed = false;
        }

        // Step 6: Add a third resistor and connect it to any power terminal
        if (canProceed && CheckStep6(resistorCount, resistorIds, simResult))
        {
            completedInstructions[experiment.Id].Add(6);
            result.InstructionResults[6] = true;
            currentStep = 7;
        }
        else
        {
            canProceed = false;
        }

        // Step 7: Add a third switch and connect it in series with the third resistor
        if (canProceed && CheckStep7(resistorCount, dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(7);
            result.InstructionResults[7] = true;
            currentStep = 8;
        }
        else
        {
            canProceed = false;
        }

        // Step 8: Ground the third switch
        if (canProceed && CheckStep8(dipSwitchCount, switchIds, simResult))
        {
            completedInstructions[experiment.Id].Add(8);
            result.InstructionResults[8] = true;
            currentStep = 9;
        }
        else
        {
            canProceed = false;
        }

        // Step 9: Ground IC74138 to Ground Breadboard
        if (canProceed && CheckStep9(ic74138Count, ic74138State))
        {
            completedInstructions[experiment.Id].Add(9);
            result.InstructionResults[9] = true;
            currentStep = 10;
        }
        else
        {
            canProceed = false;
        }

        // Step 10: VCC IC74138 to VCC Breadboard
        if (canProceed && CheckStep10(ic74138Count, ic74138State))
        {
            completedInstructions[experiment.Id].Add(10);
            result.InstructionResults[10] = true;
            currentStep = 11;
        }
        else
        {
            canProceed = false;
        }

        // Steps 11-13: IC74138 inputs to pull-up resistor inputs
        if (canProceed && CheckStep11(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult))
        {
            completedInstructions[experiment.Id].Add(11);
            result.InstructionResults[11] = true;
            currentStep = 12;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep12(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult))
        {
            completedInstructions[experiment.Id].Add(12);
            result.InstructionResults[12] = true;
            currentStep = 13;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep13(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult))
        {
            completedInstructions[experiment.Id].Add(13);
            result.InstructionResults[13] = true;
            currentStep = 14;
        }
        else
        {
            canProceed = false;
        }

        // Steps 14-16: Enable pins
        if (canProceed && CheckStep14(ic74138Count, ic74138State))
        {
            completedInstructions[experiment.Id].Add(14);
            result.InstructionResults[14] = true;
            currentStep = 15;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep15(ic74138Count, ic74138State))
        {
            completedInstructions[experiment.Id].Add(15);
            result.InstructionResults[15] = true;
            currentStep = 16;
        }
        else
        {
            canProceed = false;
        }

        if (canProceed && CheckStep16(ic74138Count, ic74138State))
        {
            completedInstructions[experiment.Id].Add(16);
            result.InstructionResults[16] = true;
            currentStep = 17;
        }
        else
        {
            canProceed = false;
        }

        // Step 17: Add 8 LEDs
        if (canProceed && CheckStep17(ledCount))
        {
            completedInstructions[experiment.Id].Add(17);
            result.InstructionResults[17] = true;
            currentStep = 18;
        }
        else
        {
            canProceed = false;
        }

        // Step 18: Connect LEDs to ground
        if (canProceed && CheckStep18(ledCount, ledIds, simResult))
        {
            completedInstructions[experiment.Id].Add(18);
            result.InstructionResults[18] = true;
            currentStep = 19;
        }
        else
        {
            canProceed = false;
        }

        // Steps 19-26: Connect IC outputs to LEDs
        for (int i = 19; i <= 26 && canProceed; i++)
        {
            if (CheckStepLEDConnection(i, ic74138Count, ledCount, components, mainIc, simResult))
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

        // Steps 27-34: Test input combinations
        for (int i = 27; i <= 34 && canProceed; i++)
        {
            if (CheckInputPattern(i, ic74138Count, ic74138State))
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

        // Only add IC state information, remove progress messages
        if (ic74138State != null)
        {
            try
            {
                // Check if the IC state has the required properties (proper power connections)
                var stateType = ic74138State.GetType();
                var hasAddressProperty = stateType.GetProperty("address") != null;
                var hasEnableProperty = stateType.GetProperty("enable") != null;

                if (hasAddressProperty && hasEnableProperty)
                {
                    bool a0 = ic74138State.address.A0;
                    bool a1 = ic74138State.address.A1;
                    bool a2 = ic74138State.address.A2;
                    bool e1 = ic74138State.enable.E1;
                    bool e2 = ic74138State.enable.E2;
                    bool e3 = ic74138State.enable.E3;

                    // result.Messages.Add($"IC 74138 Inputs: A2={a2}, A1={a1}, A0={a0}, E1={e1}, E2={e2}, E3={e3}");

                    // Calculate expected output
                    int outputIndex = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);
                    bool enabled = !e1 && !e2 && e3;
                }
                else
                {
                    // IC doesn't have proper power connections
                    string errorMsg = "";
                    try
                    {
                        errorMsg = ic74138State.error ?? "Unknown IC error";
                    }
                    catch
                    {
                        errorMsg = "IC power connection issue";
                    }
                    result.Messages.Add($"IC 74138 Error: {errorMsg}");
                }
            }
            catch (System.Exception ex)
            {
                result.Messages.Add($"IC 74138 Error: {ex.Message}");
            }
        }

        // Remove the progress message that was showing as warning text
        // result.Messages.Add($"Progress: Step {currentStep}/{experiment.TotalInstructions}");

        return result;
    }

    // Helper methods for checking each step
    private bool CheckStep0(int resistorCount, List<string> resistorIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep1(int resistorCount, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep2(int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep3(int resistorCount, List<string> resistorIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep4(int resistorCount, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep5(int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep6(int resistorCount, List<string> resistorIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep7(int resistorCount, int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep8(int dipSwitchCount, List<string> switchIds, BreadboardSimulator.SimulationResult simResult)
    {
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
    }

    private bool CheckStep9(int ic74138Count, dynamic ic74138State)
    {
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
    }

    private bool CheckStep10(int ic74138Count, dynamic ic74138State)
    {
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
    }

    private bool CheckStep11(int ic74138Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult)
    {
        return CheckICInputConnection(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult, "pin1");
    }

    private bool CheckStep12(int ic74138Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult)
    {
        return CheckICInputConnection(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult, "pin2");
    }

    private bool CheckStep13(int ic74138Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult)
    {
        return CheckICInputConnection(ic74138Count, resistorCount, dipSwitchCount, components, mainIc, simResult, "pin3");
    }

    private bool CheckICInputConnection(int ic74138Count, int resistorCount, int dipSwitchCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult, string inputPin)
    {
        if (ic74138Count >= 1 && resistorCount >= 3 && dipSwitchCount >= 3)
        {
            JToken icComp = components[mainIc];
            if (icComp != null)
            {
                string inputPinValue = icComp[inputPin]?.ToString();

                if (inputPinValue != null)
                {
                    foreach (JProperty componentProp in components)
                    {
                        if (componentProp.Name.StartsWith("resistor"))
                        {
                            if (simResult.ComponentStates.ContainsKey(componentProp.Name))
                            {
                                dynamic resistorState = simResult.ComponentStates[componentProp.Name];
                                string rPin1 = resistorState.pin1;
                                string rPin2 = resistorState.pin2;

                                if (AreNodesConnected(inputPinValue, rPin1, simResult.Nets) ||
                                    AreNodesConnected(inputPinValue, rPin2, simResult.Nets))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool CheckStep14(int ic74138Count, dynamic ic74138State)
    {
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
    }

    private bool CheckStep15(int ic74138Count, dynamic ic74138State)
    {
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
    }

    private bool CheckStep16(int ic74138Count, dynamic ic74138State)
    {
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
    }

    private bool CheckStep17(int ledCount)
    {
        return ledCount >= 8;
    }

    private bool CheckStep18(int ledCount, List<string> ledIds, BreadboardSimulator.SimulationResult simResult)
    {
        if (ledCount >= 8)
        {
            int groundedLeds = 0;
            foreach (string ledId in ledIds)
            {
                if (simResult.ComponentStates.ContainsKey(ledId))
                {
                    try
                    {
                        dynamic ledState = simResult.ComponentStates[ledId];

                        // Check if the LED state has the required properties
                        var stateType = ledState.GetType();
                        var hasGroundedProperty = stateType.GetProperty("grounded") != null;

                        if (hasGroundedProperty)
                        {
                            bool isGrounded = ledState.grounded ?? false;
                            if (isGrounded)
                            {
                                groundedLeds++;
                            }
                        }
                    }
                    catch
                    {
                        // Skip this LED if there's an error accessing its state
                        continue;
                    }
                }
            }
            return groundedLeds >= 8;
        }
        return false;
    }

    private bool CheckStepLEDConnection(int stepIndex, int ic74138Count, int ledCount, JToken components, string mainIc, BreadboardSimulator.SimulationResult simResult)
    {
        if (ic74138Count >= 1 && ledCount >= 8)
        {
            JToken icComp = components[mainIc];
            if (icComp != null)
            {
                string[] outputPins = new string[] {
                    "pin15", "pin14", "pin13", "pin12",
                    "pin11", "pin10", "pin9", "pin7"
                };

                int outputIndex = stepIndex - 19;
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
    }

    private bool CheckInputPattern(int stepIndex, int ic74138Count, dynamic ic74138State)
    {
        if (ic74138Count >= 1 && ic74138State != null)
        {
            try
            {
                // Check if the IC state has the required properties (proper power connections)
                var stateType = ic74138State.GetType();
                var hasOutputsProperty = stateType.GetProperty("outputs") != null;
                var hasEnabledProperty = stateType.GetProperty("enabled") != null;

                if (!hasOutputsProperty || !hasEnabledProperty)
                {
                    return false; // IC doesn't have proper power connections
                }

                // Get expected output index for this instruction
                int expectedOutputIndex = stepIndex - 27; // 0-7 for patterns 000-111

                // Check if IC is enabled
                bool icEnabled = ic74138State.enabled;
                if (!icEnabled)
                {
                    return false; // IC should be enabled for the pattern to work
                }

                // Get the outputs dictionary from IC state
                var outputs = ic74138State.outputs;

                // Check if the expected output is HIGH and all others are LOW
                for (int i = 0; i < 8; i++)
                {
                    string outputKey = "O" + i;
                    bool outputState = outputs[outputKey];

                    if (i == expectedOutputIndex)
                    {
                        // Expected output should be HIGH
                        if (!outputState)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // All other outputs should be LOW
                        if (outputState)
                        {
                            return false;
                        }
                    }
                }

                return true; // Correct pattern found
            }
            catch
            {
                return false;
            }
        }
        return false;
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