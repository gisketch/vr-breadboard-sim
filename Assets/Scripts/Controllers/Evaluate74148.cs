using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Evaluate74148
{
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
            MainInstruction = experiment.InstructionDescriptions[experimentDefinitions.CurrentInstructionIndex],
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

        if (!result.IsSetupValid)
        {
            return result;
        }

        // Get current switch states
        bool[] switchStates = new bool[8];
        foreach (JProperty componentProp in components)
        {
            string componentKey = componentProp.Name;
            JToken componentValue = componentProp.Value;

            if (componentKey.StartsWith("dipSwitch"))
            {
                bool isOn = componentValue["isOn"] != null ? componentValue["isOn"].Value<bool>() : false;

                // Extract switch number from key (assuming format like "dipSwitch1", "dipSwitch2", etc.)
                if (int.TryParse(componentKey.Replace("dipSwitch", ""), out int switchIndex))
                {
                    if (switchIndex >= 1 && switchIndex <= 8)
                    {
                        switchStates[switchIndex - 1] = isOn; // Convert to 0-based index
                    }
                }
            }
        }

        // Check each instruction based on current experiment state
        var completedInstructions = experimentDefinitions.GetCompletedInstructions();

        // Instruction 0: Turn on all inputs (A0-A7)
        bool allInputsOn = switchStates.All(state => state);
        if (allInputsOn)
        {
            completedInstructions[experiment.Id].Add(0);
            result.InstructionResults[0] = true;
        }
        else
        {
            result.InstructionResults[0] = false;
        }

        // Instructions 1-7: Turn off only one specific input, keep others on
        for (int i = 1; i <= 7; i++)
        {
            int targetOffIndex = i - 1; // Convert to 0-based index
            bool correctPattern = true;

            for (int j = 0; j < 8; j++)
            {
                if (j == targetOffIndex)
                {
                    // This input should be OFF
                    if (switchStates[j])
                    {
                        correctPattern = false;
                        break;
                    }
                }
                else
                {
                    // All other inputs should be ON
                    if (!switchStates[j])
                    {
                        correctPattern = false;
                        break;
                    }
                }
            }

            if (correctPattern)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
            else
            {
                result.InstructionResults[i] = false;
            }
        }

        // Count completed instructions
        result.CompletedInstructions = completedInstructions[experiment.Id].Count;

        // Add status messages
        if (ic74148State != null)
        {
            // Get IC state information
            bool a0 = ic74148State.A0;
            bool a1 = ic74148State.A1;
            bool a2 = ic74148State.A2;
            bool gs = ic74148State.GS;
            bool eo = ic74148State.EO;

            result.Messages.Add($"IC 74148 Output: A2={a2}, A1={a1}, A0={a0}, GS={gs}, EO={eo}");

            // Check if outputs match expected values for current input pattern
            string currentPattern = string.Join("", switchStates.Select(s => s ? "1" : "0"));
            result.Messages.Add($"Current input pattern: {currentPattern}");
        }

        // Add LED status
        for (int i = 0; i < ledStates.Count && i < 3; i++)
        {
            dynamic ledState = ledStates[i];
            bool isOn = ledState.isOn;
            result.Messages.Add($"LED {i + 1}: {(isOn ? "ON" : "OFF")}");
        }

        return result;
    }
}