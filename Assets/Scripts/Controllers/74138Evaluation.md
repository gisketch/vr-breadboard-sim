# IC74138 Evaluation System Documentation

The Evaluate74138.cs file implements a comprehensive evaluation system for a VR breadboard simulator experiment involving the 74138 3-to-8 line decoder IC. Here's a complete overview of how the system works:

## Overview

This system evaluates a step-by-step breadboard construction experiment where users build a circuit with an IC74138 decoder connected to 8 LEDs. The evaluation ensures proper component placement, connections, and functionality testing.

## Core Architecture

### 1. Linear Step Progression (Steps 0-26)

The system uses a strict linear progression model where each step must be completed in order:

- Steps 0-2 : Basic resistor and switch setup with power connections
- Steps 3-10 : IC74138 placement and power connections (VCC/Ground)
- Steps 11-13 : Input pin connections (A0, A1, A2) to pull-up resistors
- Steps 14-16 : Enable pin connections (E1, E2, E3)
- Step 17 : LED placement (8 LEDs required)
- Step 18 : LED ground connections
- Steps 19-26 : IC output pins to LED connections

### 2. Reversible Progress System (Steps 27-34)

For the final testing phase, the system implements a sophisticated reversible progress tracking:

```
// Static dictionary to persist completed input 
patterns across evaluations
private static Dictionary<string, HashSet<int>> 
completedInputPatterns = new Dictionary<string, 
HashSet<int>>();
```

Key Features:

- Persistent Tracking : Completed input patterns are stored in a static dictionary that survives component removal/re-addition
- Non-Linear Completion : Users can complete patterns in any order
- Progress Preservation : If a user removes components and re-adds them, previously completed patterns remain marked as complete
- Pattern Validation : Each pattern (000-111 binary combinations) is independently verified

### 3. Conflict Detection System

The system includes comprehensive conflict detection for both input and output connections:
Input Conflict Detection

- Location : Integrated into CheckStep11, CheckStep12, CheckStep13 functions
- Purpose : Prevents multiple input pins (A0, A1, A2) from sharing the same electrical network
- Behavior : Returns false and prevents step progression if conflicts detected
- Message : "Input pins cannot share the same network connection." Output Conflict Detection
- Location : Integrated into CheckStepLEDConnection function
- Purpose : Prevents multiple output pins from connecting to the same electrical network
- Behavior : Returns false and prevents step progression if conflicts detected
- Message : "Output pins cannot share the same network connection."

### 4. Pattern Validation Logic

The CheckInputPattern function implements sophisticated binary pattern testing:

```
private bool CheckInputPattern(int stepIndex, 
int ic74138Count, dynamic ic74138State)
{
    // Get expected output index for this 
    instruction
    int expectedOutputIndex = stepIndex - 27; // 
    0-7 for patterns 000-111
    
    // Verify only the expected output is HIGH, 
    all others LOW
    for (int i = 0; i < 8; i++)
    {
        string outputKey = "O" + i;
        bool outputState = outputs[outputKey];
        
        if (i == expectedOutputIndex)
        {
            if (!outputState) return false; // 
            Expected output should be HIGH
        }
        else
        {
            if (outputState) return false;  // 
            All others should be LOW
        }
    }
}
```

Pattern Mapping:

- Step 27 → Pattern 000 → Output O0 active
- Step 28 → Pattern 001 → Output O1 active
- Step 29 → Pattern 010 → Output O2 active
- ...
- Step 34 → Pattern 111 → Output O7 active

### 5. State Management

The system manages multiple types of state:
Component State Tracking

- IC74138 State : Address inputs (A0,A1,A2), enable inputs (E1,E2,E3), outputs (O0-O7)
- LED States : Ground connections, power states
- Resistor States : Pin connections, power routing
- Switch States : Position and connectivity Progress State Management
- Linear Steps : Cleared and recalculated each evaluation
- Pattern Steps : Persistent across evaluations using static dictionary
- Current Step Calculation : Dynamically determined based on completion status

### 6. Error Handling and Validation

The system includes comprehensive error handling:

- Power Connection Validation : Ensures IC has proper VCC/Ground before functional testing
- Component Count Validation : Verifies minimum required components are present
- Connection Validation : Uses AreNodesConnected to verify electrical connectivity
- State Property Validation : Safely checks for required properties using reflection
- Exception Handling : Graceful degradation when component states are invalid

### 7. Network Connectivity Analysis

The AreNodesConnected function provides the foundation for all connectivity checking:

```
private bool AreNodesConnected(string node1, 
string node2, List<BreadboardSimulator.Net> nets)
{
    foreach (var net in nets)
    {
        if (net.Nodes.Contains(node1) && net.
        Nodes.Contains(node2))
            return true;
    }
    return false;
}
```

This function analyzes the breadboard's electrical networks to determine if two pins/nodes are electrically connected.

## Key Benefits

1. Educational Progression : Enforces proper learning sequence for circuit construction
2. Flexible Testing : Allows non-linear completion of functionality tests
3. Robust Validation : Prevents common wiring mistakes through conflict detection
4. Persistent Progress : Maintains user progress even during circuit modifications
5. Comprehensive Feedback : Provides specific error messages for different failure modes
   This system effectively balances structured learning with flexible experimentation, making it ideal for educational VR breadboard simulation.
