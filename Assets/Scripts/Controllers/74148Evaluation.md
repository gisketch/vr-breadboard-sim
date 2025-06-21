# IC74148 Evaluation System Documentation

The Evaluate74148.cs file implements a 
comprehensive evaluation system for a VR 
breadboard simulator experiment involving the 
74148 8-to-3 line priority encoder IC. Here's a 
complete overview of how the system works:

## Overview

This system evaluates a step-by-step breadboard 
construction experiment where users build a 
circuit with an IC74148 priority encoder 
connected to 3 LEDs. The evaluation ensures 
proper component placement, connections, and 
functionality testing with pull-up resistor 
logic for 8 input lines.

## Core Architecture

### 1. Linear Step Progression (Steps 0-40)

The system uses a strict linear progression 
model where each step must be completed in order:

- Steps 0-23: Pull-up resistor inputs (8 sets of 
resistor-switch-ground combinations)
  - Steps 0,3,6,9,12,15,18,21: Resistor 
  connections to power
  - Steps 1,4,7,10,13,16,19,22: DIP switch 
  connections in series with resistors
  - Steps 2,5,8,11,14,17,20,23: Ground 
  connections for DIP switches
- Step 24: IC74148 placement
- Steps 25-26: IC power connections (Ground/VCC)
- Step 27: LED placement (3 LEDs required)
- Step 28: LED ground connections
- Steps 29-31: IC output pins to LED connections 
(A0, A1, A2)
- Step 32: Enable Input (EI) set to LOW
- Steps 33-40: IC input connections (I0-I7) to 
pull-up resistor networks

### 2. Sequential Pattern Testing System (Steps 
41-48)

For the final testing phase, the system 
implements a sophisticated sequential pattern 
validation with persistent tracking:

```csharp
// Static dictionary to persist completed input 
patterns across evaluations
private static Dictionary<string, HashSet<int>> 
completedInputPatterns = new Dictionary<string, 
HashSet<int>>();
```

Key Features:

- Sequential Validation : Only checks the next expected pattern in sequence (prevents skipping)
- Persistent Tracking : Completed patterns are stored in a static dictionary that survives component removal/re-addition
- Linear Progression : Users must complete patterns 0-7 (steps 41-48) in order
- Progress Preservation : Previously completed patterns remain marked as complete even after circuit modifications

### 3. Pull-Up Resistor Logic Implementation

The system implements sophisticated pull-up resistor logic for 8 input lines (I0-I7):

Pull-Up Logic Rules:

- Connected to PWR through resistor but not to GND → true (HIGH)
- Connected to PWR through resistor and to GND → false (LOW)
- Not connected → null (uninitialized)

### 4. Component State Management

The system manages multiple types of component states:

IC74148 State Tracking:

- Priority inputs (I0-I7) with active-LOW logic
- Enable input (EI) - must be LOW for operation
- Binary outputs (A0, A1, A2) representing highest priority active input
- Group Select (GS) and Enable Output (EO) status signals
- Power connections (hasVcc, hasGnd)
  Supporting Components:

- Resistor States: Pin connections, power routing
- DIP Switch States: Position, series connections, ground connections
- LED States: Ground connections, anode connections to IC outputs

### 5. Pattern Validation Logic

The CheckInputPattern function implements priority encoder pattern testing:

```
private bool CheckInputPattern(int stepIndex, 
int ic74148Count, dynamic ic74148State)
{
    int expectedPattern = stepIndex - 41; // 0-7 
    for input patterns
    
    // Check if IC has proper outputs
    if (ic74148State?.outputs != null)
    {
        dynamic outputs = ic74148State.outputs;
        bool a0 = outputs.A0;
        bool a1 = outputs.A1;
        bool a2 = outputs.A2;
        
        // Calculate binary output
        int binaryOutput = (a2 ? 4 : 0) + (a1 ? 
        2 : 0) + (a0 ? 1 : 0);
        
        // Verify output matches expected pattern
        return binaryOutput == expectedPattern;
    }
    return false;
}
```

Pattern Mapping:

- Step 41 → Input I0 active → Output 000 (binary 0)
- Step 42 → Input I1 active → Output 001 (binary 1)
- Step 43 → Input I2 active → Output 010 (binary 2)
- ...
- Step 48 → Input I7 active → Output 111 (binary 7)

### 6. Enhanced Conflict Detection System

The system includes comprehensive conflict detection for both input and output connections:

Input Conflict Detection:

- Location: Integrated into CheckInputConnectionStep functions
- Purpose: Prevents multiple input pins (I0-I7) from sharing the same electrical network
- Behavior: Returns false and prevents step progression if conflicts detected
- Message: "Input pins cannot share the same network connection."
  Output Conflict Detection:

- Location: Integrated into CheckLEDConnectionStep function
- Purpose: Prevents multiple output pins (A0, A1, A2) from connecting to the same electrical network
- Behavior: Returns false and prevents step progression if conflicts detected
- Message: "Output pins cannot share the same network connection."

### 7. Sequential Step Enforcement

The IC74148 evaluation enforces strict sequential completion for pattern testing:

```
// Sequential validation for steps 41-48
int nextExpectedPattern = 41;
for (int i = 41; i <= 48; i++)
{
    if (completedInputPatterns[experiment.Id.
    ToString()].Contains(i))
    {
        nextExpectedPattern = i + 1;
    }
    else
    {
        nextExpectedPattern = i;
        break;
    }
}

// Only check the next expected pattern
if (nextExpectedPattern <= 48)
{
    if (CheckInputPattern(nextExpectedPattern, 
    ic74148Count, ic74148State))
    {
        completedInputPatterns[experiment.Id.
        ToString()].Add(nextExpectedPattern);
    }
}
```

### 8. Error Handling and Validation

The system includes comprehensive error handling:

- Power Connection Validation : Ensures IC has proper VCC/Ground before functional testing
- Component Count Validation : Verifies minimum required components are present
- Connection Validation : Uses AreNodesConnected to verify electrical connectivity
- State Property Validation : Safely checks for required properties using reflection
- Enable Input Validation : Ensures EI pin is properly set to LOW for operation

### 9. Network Connectivity Analysis

The system uses sophisticated network analysis for connection validation:

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

### 10. Progress State Management

The system manages different types of progress:

Linear Steps (0-40):

- Cleared and recalculated each evaluation
- Must be completed in strict order
- No persistent tracking
  Pattern Steps (41-48):

- Persistent across evaluations using static dictionary
- Sequential completion enforced
- Progress preserved during circuit modifications

### 11. IC74148 Specific Features

Priority Encoding Logic:

- Highest priority input (I7) takes precedence over lower priority inputs
- Active-LOW inputs (LOW = active, HIGH = inactive)
- Binary output represents the highest priority active input
- Enable Input (EI) must be LOW for proper operation
  Output Mapping:

- A0, A1, A2: 3-bit binary representation of highest priority active input
- GS (Group Select): Indicates if any input is active
- EO (Enable Output): Cascading output for multiple IC74148s
