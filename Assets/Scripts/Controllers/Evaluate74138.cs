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
            MainInstruction = experiment.InstructionDescriptions[experimentDefinitions.CurrentInstructionIndex],
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };


        // Count components and validate types
        int ic74138Count = 0;
        int ledCount = 0;
        int resistorCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        List<string> mainLeds = new List<string>();

        // Get component data
        dynamic ic74138State = null;
        List<dynamic> ledStates = new List<dynamic>();

        // Debug log all component states
        UnityEngine.Debug.Log("Component States:");
        foreach (var state in simResult.ComponentStates)
        {
            UnityEngine.Debug.Log($"Component {state.Key}:");
            UnityEngine.Debug.Log(JToken.FromObject(state.Value).ToString(Newtonsoft.Json.Formatting.Indented));
        }

        foreach (var comp in simResult.ComponentStates)
        {
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
                dynamic dynamicValue = comp.Value;
                ledCount++;
                mainLeds.Add(comp.Key);
                ledStates.Add(comp.Value);
            }
            else if (comp.Key.StartsWith("resistor"))
            {
                resistorCount++;
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

        if (resistorCount < 3)
        {
            result.Messages.Add($"Expected at least 3 resistors for pull-up, found {resistorCount}");
            result.IsSetupValid = false;
        }

        if (dipSwitchCount < 3)
        {
            result.Messages.Add($"Expected at least 3 DIP switches, found {dipSwitchCount}");
            result.IsSetupValid = false;
        }

        if (!result.IsSetupValid)
        {
            return result;
        }

        // Get current switch states
        bool[] switchStates = new bool[3]; // A0, A1, A2
        foreach (JProperty componentProp in components)
        {
            string componentKey = componentProp.Name;
            JToken componentValue = componentProp.Value;

            if (componentKey.StartsWith("dipSwitch"))
            {
                bool isOn = componentValue["isOn"] != null ? componentValue["isOn"].Value<bool>() : false;

                // Extract switch number from key
                if (int.TryParse(componentKey.Replace("dipSwitch", ""), out int switchIndex))
                {
                    if (switchIndex >= 1 && switchIndex <= 3)
                    {
                        switchStates[switchIndex - 1] = isOn;
                    }
                }
            }
        }

        var completedInstructions = experimentDefinitions.GetCompletedInstructions();

        // Check basic setup instructions (0-26)
        // Instructions 0-8: Pull-up resistors and switches setup
        if (resistorCount >= 3 && dipSwitchCount >= 3)
        {
            for (int i = 0; i <= 8; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 9-16: IC connections
        if (ic74138State != null)
        {
            for (int i = 9; i <= 16; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 17-26: LED connections
        if (ledCount >= 8)
        {
            for (int i = 17; i <= 26; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 27-34: Test different input combinations
        if (ic74138State != null)
        {
            bool a0 = ic74138State.A0;
            bool a1 = ic74138State.A1;
            bool a2 = ic74138State.A2;

            // Convert switch states to input values (inverted because of pull-up)
            bool inputA0 = !switchStates[0];
            bool inputA1 = !switchStates[1];
            bool inputA2 = !switchStates[2];

            // Check each test pattern
            var testPatterns = new Dictionary<int, (bool a2, bool a1, bool a0)>
            {
                { 27, (false, false, false) }, // 000
                { 28, (false, false, true) },  // 001
                { 29, (false, true, false) },  // 010
                { 30, (false, true, true) },   // 011
                { 31, (true, false, false) },  // 100
                { 32, (true, false, true) },   // 101
                { 33, (true, true, false) },   // 110
                { 34, (true, true, true) }     // 111
            };

            foreach (var pattern in testPatterns)
            {
                int instructionIndex = pattern.Key;
                var expectedPattern = pattern.Value;

                if (inputA2 == expectedPattern.a2 && inputA1 == expectedPattern.a1 && inputA0 == expectedPattern.a0)
                {
                    completedInstructions[experiment.Id].Add(instructionIndex);
                    result.InstructionResults[instructionIndex] = true;
                }
                else
                {
                    result.InstructionResults[instructionIndex] = false;
                }
            }
        }

        // Count completed instructions
        result.CompletedInstructions = completedInstructions[experiment.Id].Count;

        // Add status messages
        if (ic74138State != null)
        {
            bool a0 = ic74138State.A0;
            bool a1 = ic74138State.A1;
            bool a2 = ic74138State.A2;
            bool e1 = ic74138State.E1;
            bool e2 = ic74138State.E2;
            bool e3 = ic74138State.E3;

            result.Messages.Add($"IC 74138 Inputs: A2={a2}, A1={a1}, A0={a0}, E1={e1}, E2={e2}, E3={e3}");

            // Calculate expected output
            int outputIndex = (a2 ? 4 : 0) + (a1 ? 2 : 0) + (a0 ? 1 : 0);
            bool enabled = !e1 && !e2 && e3;

            if (enabled)
            {
                result.Messages.Add($"Decoder enabled, output O{outputIndex} should be active (LOW)");
            }
            else
            {
                result.Messages.Add("Decoder disabled - check enable pins E1, E2, E3");
            }
        }

        // Add LED status
        for (int i = 0; i < ledStates.Count && i < 8; i++)
        {
            dynamic ledState = ledStates[i];
            bool isOn = ledState.isOn;
            result.Messages.Add($"LED O{i}: {(isOn ? "ON" : "OFF")}");
        }

        return result;
    }
}