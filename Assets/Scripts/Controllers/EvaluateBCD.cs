using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class EvaluateBCD
{
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
            MainInstruction = experiment.InstructionDescriptions[experimentDefinitions.CurrentInstructionIndex],
            InstructionResults = new Dictionary<int, bool>(),
            Messages = new List<string>(),
            IsSetupValid = true
        };

        // Count components and validate types
        int ic7448Count = 0;
        int sevenSegmentCount = 0;
        int resistorCount = 0;
        int dipSwitchCount = 0;

        string mainIc = "";
        string mainDisplay = "";

        // Get component data
        dynamic ic7448State = null;
        dynamic sevenSegmentState = null;

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
            else if (comp.Key.StartsWith("sevenSegment"))
            {
                sevenSegmentCount++;
                sevenSegmentState = comp.Value;
                mainDisplay = comp.Key;
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
        if (ic7448Count != 1)
        {
            result.Messages.Add($"Expected exactly 1 IC 7448, found {ic7448Count}");
            result.IsSetupValid = false;
        }

        if (sevenSegmentCount != 1)
        {
            result.Messages.Add($"Expected exactly 1 7-segment display, found {sevenSegmentCount}");
            result.IsSetupValid = false;
        }

        if (resistorCount < 4)
        {
            result.Messages.Add($"Expected at least 4 resistors for pull-up, found {resistorCount}");
            result.IsSetupValid = false;
        }

        if (dipSwitchCount < 4)
        {
            result.Messages.Add($"Expected at least 4 DIP switches, found {dipSwitchCount}");
            result.IsSetupValid = false;
        }

        if (!result.IsSetupValid)
        {
            return result;
        }

        // Get current switch states
        bool[] switchStates = new bool[4]; // A, B, C, D
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
                    if (switchIndex >= 1 && switchIndex <= 4)
                    {
                        switchStates[switchIndex - 1] = isOn;
                    }
                }
            }
        }

        var completedInstructions = experimentDefinitions.GetCompletedInstructions();

        // Check basic setup instructions (0-27)
        // Instructions 0-11: Pull-up resistors and switches setup
        if (resistorCount >= 4 && dipSwitchCount >= 4)
        {
            for (int i = 0; i <= 11; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 12-20: IC connections
        if (ic7448State != null)
        {
            for (int i = 12; i <= 20; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 21-27: 7-segment connections
        if (sevenSegmentState != null)
        {
            for (int i = 21; i <= 27; i++)
            {
                completedInstructions[experiment.Id].Add(i);
                result.InstructionResults[i] = true;
            }
        }

        // Instructions 28-38: Test different BCD inputs
        if (ic7448State != null && sevenSegmentState != null)
        {
            bool a = ic7448State.A;
            bool b = ic7448State.B;
            bool c = ic7448State.C;
            bool d = ic7448State.D;

            // Convert switch states to input values (inverted because of pull-up)
            bool inputA = !switchStates[0];
            bool inputB = !switchStates[1];
            bool inputC = !switchStates[2];
            bool inputD = !switchStates[3];

            // Check each test pattern
            var testPatterns = new Dictionary<int, (bool d, bool c, bool b, bool a, int expectedDigit)>
            {
                { 28, (false, false, false, false, 0) }, // 0000 -> 0
                { 29, (false, false, false, true, 1) },  // 0001 -> 1
                { 30, (false, false, true, false, 2) },  // 0010 -> 2
                { 31, (false, false, true, true, 3) },   // 0011 -> 3
                { 32, (false, true, false, false, 4) },  // 0100 -> 4
                { 33, (false, true, false, true, 5) },   // 0101 -> 5
                { 34, (false, true, true, false, 6) },   // 0110 -> 6
                { 35, (false, true, true, true, 7) },    // 0111 -> 7
                { 36, (true, false, false, false, 8) },  // 1000 -> 8
                { 37, (true, false, false, true, 9) },   // 1001 -> 9
                { 38, (true, false, true, false, 10) }   // 1010 -> A (hex)
            };

            foreach (var pattern in testPatterns)
            {
                int instructionIndex = pattern.Key;
                var expectedPattern = pattern.Value;

                if (inputD == expectedPattern.d && inputC == expectedPattern.c &&
                    inputB == expectedPattern.b && inputA == expectedPattern.a)
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
        if (ic7448State != null)
        {
            bool a = ic7448State.A;
            bool b = ic7448State.B;
            bool c = ic7448State.C;
            bool d = ic7448State.D;
            bool lt = ic7448State.LT;
            bool rbi = ic7448State.RBI;
            bool rbo = ic7448State.RBO;

            result.Messages.Add($"IC 7448 Inputs: D={d}, C={c}, B={b}, A={a}, LT={lt}, RBI={rbi}, RBO={rbo}");

            // Calculate BCD value
            int bcdValue = (d ? 8 : 0) + (c ? 4 : 0) + (b ? 2 : 0) + (a ? 1 : 0);
            result.Messages.Add($"BCD Input Value: {bcdValue}");
        }

        // Add 7-segment display status
        if (sevenSegmentState != null)
        {
            dynamic segments = sevenSegmentState;
            bool segA = segments.a;
            bool segB = segments.b;
            bool segC = segments.c;
            bool segD = segments.d;
            bool segE = segments.e;
            bool segF = segments.f;
            bool segG = segments.g;

            result.Messages.Add($"7-Segment Display: a={segA}, b={segB}, c={segC}, d={segD}, e={segE}, f={segF}, g={segG}");
        }

        return result;
    }
}