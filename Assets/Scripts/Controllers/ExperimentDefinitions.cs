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
                // 1.0 Create pull-up resistor inputs
                [0] = "Add a resistor and connect it to any power terminal",
                [1] = "Add a switch and connect it in series with the resistor",
                [2] = "Ground the switch",
                [3] = "Add a second resistor and connect it to any power terminal",
                [4] = "Add a second switch and connect it in series with the second resistor",
                [5] = "Ground the second switch",
                [6] = "Add a third resistor and connect it to any power terminal",
                [7] = "Add a third switch and connect it in series with the third resistor",
                [8] = "Ground the third switch",

                // 1.1 Connect IC Connections
                [9] = "Add IC74138 and Ground it to Ground Breadboard",
                [10] = "VCC IC74138 to VCC Breadboard",
                [11] = "IC74138 A0 to input 1 from pull-up resistor input",
                [12] = "IC74138 A1 to input 2 from pull-up resistor input",
                [13] = "IC74138 A2 to input 3 from pull-up resistor input",

                // 1.2 Connect IC74138 enable pins
                [14] = "E1 set to LOW",
                [15] = "E2 set to LOW",
                [16] = "E3 set to HIGH",

                // Add LEDs and connect to outputs
                [17] = "Add 8 LEDs",
                [18] = "Connect LEDs to ground",

                // 1.3 Connect IC74138 outputs to LEDs
                [19] = "Connect O0 to LED0",
                [20] = "Connect O1 to LED1",
                [21] = "Connect O2 to LED2",
                [22] = "Connect O3 to LED3",
                [23] = "Connect O4 to LED4",
                [24] = "Connect O5 to LED5",
                [25] = "Connect O6 to LED6",
                [26] = "Connect O7 to LED7",

                // 2. Display outputs on LEDs
                [27] = "Set inputs to 000 (A2=0, A1=0, A0=0)",
                [28] = "Set inputs to 001 (A2=0, A1=0, A0=1)",
                [29] = "Set inputs to 010 (A2=0, A1=1, A0=0)",
                [30] = "Set inputs to 011 (A2=0, A1=1, A0=1)",
                [31] = "Set inputs to 100 (A2=1, A1=0, A0=0)",
                [32] = "Set inputs to 101 (A2=1, A1=0, A0=1)",
                [33] = "Set inputs to 110 (A2=1, A1=1, A0=0)",
                [34] = "Set inputs to 111 (A2=1, A1=1, A0=1)"
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
            TotalInstructions = 47,
            InstructionDescriptions = new Dictionary<int, string>
            {
                // 1.0 Create pull-up resistor inputs
                [0] = "Add a resistor and connect it to any power terminal",
                [1] = "Add a switch and connect it in series with the resistor (input 1)",
                [2] = "Ground the switch",
                [3] = "Add a second resistor and connect it to any power terminal",
                [4] = "Add a second switch and connect it in series with the second resistor (input 2)",
                [5] = "Ground the second switch",
                [6] = "Add a third resistor and connect it to any power terminal",
                [7] = "Add a third switch and connect it in series with the third resistor (input 3)",
                [8] = "Ground the third switch",
                [9] = "Add a fourth resistor and connect it to any power terminal",
                [10] = "Add a fourth switch and connect it in series with the fourth resistor (input 4)",
                [11] = "Ground the fourth switch",

                // Add IC7448
                [12] = "Add IC7448 to the breadboard",

                // 1.1 Connect IC7448 connections
                [13] = "Ground IC7448 to Ground Breadboard",
                [14] = "VCC IC7448 to VCC Breadboard",
                [15] = "IC7448 A to input 1 from pull-up resistor input",
                [16] = "IC7448 B to input 2 from pull-up resistor input",
                [17] = "IC7448 C to input 3 from pull-up resistor input",
                [18] = "IC7448 D to input 4 from pull-up resistor input",

                // 1.2 Connect IC node connections
                [19] = "IC7448 LT set to HIGH",
                [20] = "IC7448 RBO set to HIGH",
                [21] = "IC7448 RBI set to HIGH",

                // Add 7-segment display
                [22] = "Add 7-segment display to the breadboard",
                [23] = "Ground 7-segment display (connect common cathode to ground)",

                // 1.3 Connect IC7448 to 7-segment connections
                [24] = "Connect IC7448 f PIN to 7-segment f PIN",
                [25] = "Connect IC7448 g PIN to 7-segment g PIN",
                [26] = "Connect IC7448 a PIN to 7-segment a PIN",
                [27] = "Connect IC7448 b PIN to 7-segment b PIN",
                [28] = "Connect IC7448 c PIN to 7-segment c PIN",
                [29] = "Connect IC7448 d PIN to 7-segment d PIN",
                [30] = "Connect IC7448 e PIN to 7-segment e PIN",

                // 2. Display outputs on 7-segment (TRACK)
                [31] = "Set inputs to 0000 to display 0",
                [32] = "Set inputs to 0001 to display 1",
                [33] = "Set inputs to 0010 to display 2",
                [34] = "Set inputs to 0011 to display 3",
                [35] = "Set inputs to 0100 to display 4",
                [36] = "Set inputs to 0101 to display 5",
                [37] = "Set inputs to 0110 to display 6",
                [38] = "Set inputs to 0111 to display 7",
                [39] = "Set inputs to 1000 to display 8",
                [40] = "Set inputs to 1001 to display 9",
                [41] = "Set inputs to 1010 to display a",
                [42] = "Set inputs to 1011 to display b",
                [43] = "Set inputs to 1100 to display c",
                [44] = "Set inputs to 1101 to display d",
                [45] = "Set inputs to 1110 to display e",
                [46] = "Set inputs to 1111 to display f"
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