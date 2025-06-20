## Overview

This system evaluates a step-by-step breadboard construction experiment where users build a circuit with an IC7448 BCD decoder connected to a 7-segment display. The evaluation ensures proper component placement, connections, and functionality testing with pull-up resistor logic.

## Core Architecture

### 1. Linear Step Progression (Steps 0-30)

The system uses a strict linear progression model where each step must be completed in order:

- Steps 0-11 : Pull-up resistor inputs (4 sets of resistor-switch-ground combinations)
  - Steps 0,3,6,9: Resistor connections to power
  - Steps 1,4,7,10: DIP switch connections in series with resistors
  - Steps 2,5,8,11: Ground connections for DIP switches
- Step 12 : IC7448 placement
- Steps 13-14 : IC power connections (VCC/Ground)
- Steps 15-18 : IC input connections (A, B, C, D) to pull-up resistor networks
- Steps 19-21 : IC control connections (LT, RBO, RBI)
- Step 22 : 7-segment display placement
- Step 23 : 7-segment display ground connection
- Steps 24-30 : IC to 7-segment connections (F, G, A, B, C, D, E segments)

### 2. Sequential Pattern Testing System (Steps 31-46)

For the final testing phase, the system implements a sophisticated sequential pattern validation with persistent tracking:

```
// Static dictionary to persist completed input 
patterns across evaluations
private static Dictionary<string, HashSet<int>> 
completedInputPatterns = new Dictionary<string, 
HashSet<int>>();
```

Key Features:

- Sequential Validation : Only checks the next expected pattern in sequence (prevents skipping)
- Persistent Tracking : Completed patterns are stored in a static dictionary that survives component removal/re-addition
- Linear Progression : Users must complete patterns 0-15 (steps 31-46) in order
- Progress Preservation : Previously completed patterns remain marked as complete even after circuit modifications

### 3. Pull-Up Resistor Logic Implementation

The system implements sophisticated pull-up resistor logic similar to the IC74138 evaluation:

```
// Enhanced input evaluation with pull-up logic
private bool GetInputValueWithPullUp(string 
icId, string pinName, BreadboardSimulator.
SimulationResult simResult)
{
    // Implements pull-up logic:
    // - PWR through resistor without GND = HIGH
    // - PWR through resistor with GND = LOW
    // - No connection = null
}
```

Pull-Up Logic Rules:

- Connected to PWR through resistor but not to GND → true (HIGH)
- Connected to PWR through resistor and to GND → false (LOW)
- Not connected → null (uninitialized)

### 4. Component State Management

The system manages multiple types of component states:

IC7448 State Tracking:

- BCD inputs (A, B, C, D)
- Control inputs (LT, RBI, BI/RBO)
- 7-segment outputs (a, b, c, d, e, f, g)
- Value property (0-15 decimal representation)
  Supporting Components:

- Resistor States: Pin connections, power routing
- DIP Switch States: Position, series connections, ground connections
- 7-Segment Display: Ground connections, segment states

### 5. Pattern Validation Logic

The CheckInputPattern function implements BCD pattern testing:

```
private bool CheckInputPattern(int stepIndex, 
int ic7448Count, int sevenSegmentCount, dynamic 
ic7448State, dynamic sevenSegmentState)
{
    int expectedValue = stepIndex - 31; // 0-15 
    for BCD patterns
    
    // Direct validation using IC's value 
    property
    if (ic7448State.value == expectedValue)
    {
        return true;
    }
    return false;
}
```

Pattern Mapping:

- Step 31 → BCD 0000 → Display "0"
- Step 32 → BCD 0001 → Display "1"
- Step 33 → BCD 0010 → Display "2"
- ...
- Step 46 → BCD 1111 → Display "F" (15)

### 6. Enhanced Input Conflict Detection

The system includes comprehensive conflict detection for input connections:

Input Conflict Prevention:

- Location: Integrated into CheckStep15-18 functions
- Purpose: Prevents multiple BCD input pins from sharing the same electrical network
- Behavior: Returns false and prevents step progression if conflicts detected
- Message: "Input pins cannot share the same network connection."

### 7. Sequential Step Enforcement

Unlike the IC74138 system, the BCD evaluation enforces strict sequential completion:

```
// Sequential validation for steps 31-46
int nextExpectedPattern = 31;
for (int i = 31; i <= 46; i++)
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
if (nextExpectedPattern <= 46)
{
    if (CheckInputPattern
    (nextExpectedPattern, ...))
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
- Connection Validation : Uses network analysis to verify electrical connectivity
- State Property Validation : Safely checks for required properties using reflection
- Null Input Handling : Gracefully handles unconnected input pins

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

Linear Steps (0-30):

- Cleared and recalculated each evaluation
- Must be completed in strict order
- No persistent tracking
  Pattern Steps (31-46):

- Persistent across evaluations using static dictionary
- Sequential completion enforced
- Progress preserved during circuit modifications
