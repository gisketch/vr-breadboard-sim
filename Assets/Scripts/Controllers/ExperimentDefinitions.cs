using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExperimentDefinitions
{
    private Dictionary<int, ExperimentDefinition> _experiments;
    private Dictionary<int, HashSet<int>> _completedInstructions;
    private Dictionary<int, int> _lastInstructionIndices = new Dictionary<int, int>();

    public int CurrentExperimentId { get; set; } = 1;
    public int CurrentInstructionIndex { get; set; } = 0;

    public ExperimentDefinitions()
    {
        Debug.Log("ExperimentDefinitions constructor called");
        InitializeExperiments();
    }

    private void InitializeExperiments()
    {
        Debug.Log("InitializeExperiments started");
        _experiments = new Dictionary<int, ExperimentDefinition>();
        _completedInstructions = new Dictionary<int, HashSet<int>>();

        // Add experiment 1
        var decoderExperiment = new ExperimentDefinition
        {
            Id = 1,
            Name = "IC 74138 Decoder",
            Description = "Connect DIP switches to a 74138 IC to control 8 LEDs.",
            TotalInstructions = 35,
            InstructionDescriptions = new Dictionary<int, string>
            {
                [0] = "Place the IC 74138 on the breadboard",
                [1] = "Connect VCC (pin 16) to the positive power rail",
                [2] = "Connect GND (pin 8) to the negative power rail",
                [3] = "Connect enable pins G1 (pin 6) to HIGH",
                [4] = "Connect enable pins G2A (pin 4) and G2B (pin 5) to LOW",
                [5] = "Connect input A (pin 1) to DIP switch 1",
                [6] = "Connect input B (pin 2) to DIP switch 2",
                [7] = "Connect input C (pin 3) to DIP switch 3",
                [8] = "Connect LED 1 to output Y0 (pin 15) through a resistor",
                [9] = "Connect LED 2 to output Y1 (pin 14) through a resistor",
                [10] = "Connect LED 3 to output Y2 (pin 13) through a resistor",
                [11] = "Connect LED 4 to output Y3 (pin 12) through a resistor",
                [12] = "Connect LED 5 to output Y4 (pin 11) through a resistor",
                [13] = "Connect LED 6 to output Y5 (pin 10) through a resistor",
                [14] = "Connect LED 7 to output Y6 (pin 9) through a resistor",
                [15] = "Connect LED 8 to output Y7 (pin 7) through a resistor",
                [16] = "Test with DIP switch combination 000 (all switches OFF)",
                [17] = "Test with DIP switch combination 001",
                [18] = "Test with DIP switch combination 010",
                [19] = "Test with DIP switch combination 011",
                [20] = "Test with DIP switch combination 100",
                [21] = "Test with DIP switch combination 101",
                [22] = "Test with DIP switch combination 110",
                [23] = "Test with DIP switch combination 111 (all switches ON)",
                [24] = "Verify that only one LED lights up for each combination",
                [25] = "Verify LED 1 lights for input 000",
                [26] = "Verify LED 2 lights for input 001",
                [27] = "Verify LED 3 lights for input 010",
                [28] = "Verify LED 4 lights for input 011",
                [29] = "Verify LED 5 lights for input 100",
                [30] = "Verify LED 6 lights for input 101",
                [31] = "Verify LED 7 lights for input 110",
                [32] = "Verify LED 8 lights for input 111",
                [33] = "Check all connections are secure",
                [34] = "Experiment completed successfully!"
            }
        };
        _experiments[decoderExperiment.Id] = decoderExperiment;
        _completedInstructions[decoderExperiment.Id] = new HashSet<int>();
        Debug.Log($"Added experiment 1: {decoderExperiment.Name}");

        // Add experiment 2 (BCD)
        var bcdExperiment = new ExperimentDefinition
        {
            Id = 2,
            Name = "BCD to 7-Segment Display",
            Description = "Connect DIP switches to a 7448 IC to display decimal digits on a 7-segment display.",
            TotalInstructions = 39,
            InstructionDescriptions = new Dictionary<int, string>
            {
                [0] = "Place the IC 7448 on the breadboard",
                [1] = "Connect VCC (pin 16) to the positive power rail",
                [2] = "Connect GND (pin 8) to the negative power rail",
                [3] = "Place the 7-segment display on the breadboard",
                [4] = "Connect segment a (pin 7) of 7448 to segment a of display through resistor",
                [5] = "Connect segment b (pin 1) of 7448 to segment b of display through resistor",
                [6] = "Connect segment c (pin 2) of 7448 to segment c of display through resistor",
                [7] = "Connect segment d (pin 6) of 7448 to segment d of display through resistor",
                [8] = "Connect segment e (pin 13) of 7448 to segment e of display through resistor",
                [9] = "Connect segment f (pin 12) of 7448 to segment f of display through resistor",
                [10] = "Connect segment g (pin 11) of 7448 to segment g of display through resistor",
                [11] = "Connect common cathode of display to ground",
                [12] = "Connect BCD input A (pin 7) to DIP switch 1",
                [13] = "Connect BCD input B (pin 1) to DIP switch 2",
                [14] = "Connect BCD input C (pin 2) to DIP switch 3",
                [15] = "Connect BCD input D (pin 6) to DIP switch 4",
                [16] = "Connect LT (pin 3) to HIGH (disable lamp test)",
                [17] = "Connect RBI (pin 5) to HIGH (disable ripple blanking)",
                [18] = "Connect BI/RBO (pin 4) to HIGH (disable blanking)",
                [19] = "Test BCD input 0000 - should display '0'",
                [20] = "Test BCD input 0001 - should display '1'",
                [21] = "Test BCD input 0010 - should display '2'",
                [22] = "Test BCD input 0011 - should display '3'",
                [23] = "Test BCD input 0100 - should display '4'",
                [24] = "Test BCD input 0101 - should display '5'",
                [25] = "Test BCD input 0110 - should display '6'",
                [26] = "Test BCD input 0111 - should display '7'",
                [27] = "Test BCD input 1000 - should display '8'",
                [28] = "Test BCD input 1001 - should display '9'",
                [29] = "Verify digit '0' displays correctly",
                [30] = "Verify digit '1' displays correctly",
                [31] = "Verify digit '2' displays correctly",
                [32] = "Verify digit '3' displays correctly",
                [33] = "Verify digit '4' displays correctly",
                [34] = "Verify digit '5' displays correctly",
                [35] = "Verify digit '6' displays correctly",
                [36] = "Verify digit '7' displays correctly",
                [37] = "Verify digit '8' displays correctly",
                [38] = "Verify digit '9' displays correctly"
            }
        };
        _experiments[bcdExperiment.Id] = bcdExperiment;
        _completedInstructions[bcdExperiment.Id] = new HashSet<int>();
        Debug.Log($"Added experiment 2: {bcdExperiment.Name}");

        // Add experiment 3
        var encoderExperiment = new ExperimentDefinition
        {
            Id = 3,
            Name = "IC 74148 Encoder",
            Description = "Connect switches to a 74148 IC to encode 8 inputs to 3-bit binary output.",
            TotalInstructions = 8,
            InstructionDescriptions = new Dictionary<int, string>
            {
                [0] = "Place the IC 74148 on the breadboard",
                [1] = "Connect VCC (pin 16) to the positive power rail",
                [2] = "Connect GND (pin 8) to the negative power rail",
                [3] = "Connect input pins I0-I7 (pins 10,11,12,13,1,2,3,4) to DIP switches",
                [4] = "Connect output pins A0,A1,A2 (pins 6,7,9) to LEDs through resistors",
                [5] = "Connect EI (pin 5) to LOW to enable the encoder",
                [6] = "Test various input combinations",
                [7] = "Verify 3-bit binary output corresponds to highest priority input"
            }
        };
        _experiments[encoderExperiment.Id] = encoderExperiment;
        _completedInstructions[encoderExperiment.Id] = new HashSet<int>();
        Debug.Log($"Added experiment 3: {encoderExperiment.Name}");

        Debug.Log($"Total experiments initialized: {_experiments.Count}");
        Debug.Log($"Available experiment IDs: {string.Join(", ", _experiments.Keys)}");
    }

    public Dictionary<int, ExperimentDefinition> GetExperiments()
    {
        Debug.Log($"GetExperiments called - returning {_experiments.Count} experiments");
        return _experiments;
    }

    public ExperimentDefinition GetCurrentExperiment()
    {
        return _experiments.TryGetValue(CurrentExperimentId, out var experiment) ? experiment : null;
    }

    public Dictionary<int, HashSet<int>> GetCompletedInstructions()
    {
        return _completedInstructions;
    }

    public int GetCompletedInstructionsCount(int experimentId)
    {
        if (_completedInstructions.ContainsKey(experimentId))
        {
            return _completedInstructions[experimentId].Count;
        }
        return 0;
    }

    public int GetTotalInstructionsCount(int experimentId)
    {
        if (_experiments.ContainsKey(experimentId))
        {
            return _experiments[experimentId].TotalInstructions;
        }
        return 0;
    }

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