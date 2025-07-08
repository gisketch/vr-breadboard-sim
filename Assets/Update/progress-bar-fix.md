Trae

Used 1 context

docs-6-21.md

# Progress Bar Desync Bug Fix Documentation

## Problem Description

### Issue

- Progress Bar Desync Across Clients : When one student finished an experiment in the VR breadboard simulator, the UI updates (including progress bars) were incorrectly applied to ALL clients instead of just the student who completed the experiment.
- Backend vs Frontend Mismatch : The backend state was correctly maintained per-client, but the frontend UI was being shared across all connected clients.
- Root Cause : BreadboardSimulator.cs and BreadboardStateUtils.cs were implemented as singletons, meaning their instances were shared across all clients in the Mirror networking environment.

### Technical Root Causes

1. Singleton Pattern in BreadboardSimulator :

   ```
   public static BreadboardSimulator Instance { get; 
   private set; }
   ```

   - All clients shared the same simulator instance
   - UI updates affected all clients simultaneously

2. Global State in ExperimentDefinitions and ExperimentEvaluator :

   - Experiment progress was stored globally instead of per-client
   - Progress tracking was not isolated between different students

3. Shared UI Updates :

   - UpdateUI() method in BreadboardSimulator updated UI based on global state
   - Even with hasAuthority checks, the underlying data was shared

## Solution: Instance-Based Simulator Architecture

### Overview

The fix transforms the singleton-based architecture into an instance-based system where each BreadboardController (which represents an individual client's breadboard) maintains its own BreadboardSimulator instance.

### Why This Solution Works with Mirror Networking Mirror Networking Context

- NetworkBehaviour : BreadboardController inherits from NetworkBehaviour , making it network-aware
- hasAuthority : Each client has authority over their own BreadboardController instance
- SyncVar and SyncDictionary : Network state is properly synchronized per-client
- Client Isolation : Each client runs their own instance of the game, but shares network state Instance-Based Benefits

1. Per-Client State : Each client maintains its own simulator state
2. UI Isolation : UI updates only affect the client that triggered them
3. Network Compatibility : Works seamlessly with Mirror's client-server architecture
4. Authority Respect : Maintains proper network authority boundaries

### Implementation Details 1. Modified BreadboardSimulator.cs

```
public class BreadboardSimulator : MonoBehaviour
{
    // Removed singleton pattern
    // public static BreadboardSimulator Instance { get; 
    private set; }
    
    // Instance-based experiment management
    private ExperimentDefinitions experimentDefinitions;
    private ExperimentEvaluator experimentEvaluator;
    
    private void Awake()
    {
        // Initialize per-instance experiment management
        experimentDefinitions = new ExperimentDefinitions
        ();
        experimentEvaluator = new ExperimentEvaluator();
    }
    
    // All methods now operate on instance data
    public void RunSimulation(BreadboardController bc)
    {
        // Uses this instance's experimentDefinitions and 
        experimentEvaluator
        var result = experimentEvaluator.
        EvaluateExperiment(experimentDefinitions.
        GetCurrentExperimentId(), bc);
        
        if (bc.hasAuthority)
        {
            UpdateUI(bc, result); // Only updates THIS 
            client's UI
        }
    }
}
```

2.  Modified BreadboardController.cs

```
public class BreadboardController : NetworkBehaviour
{
    private BreadboardSimulator breadboardSimulator;
    
    private void Start()
    {
        // Each BreadboardController creates its own 
        simulator instance
        GameObject simulatorObj = new GameObject
        ("BreadboardSimulator_" + netId);
        breadboardSimulator = simulatorObj.
        AddComponent<BreadboardSimulator>();
        breadboardSimulator.Initialize(this);
    }
    
    public BreadboardSimulator GetSimulator()
    {
        return breadboardSimulator;
    }
}
```

3. Modified BreadboardStateUtils.cs

```
public static class BreadboardStateUtils
{
    public static void VisualizeSimulation
    (BreadboardController bc)
    {
        // Use the specific controller's simulator 
        instance
        BreadboardSimulator simulator = bc.GetSimulator();
        simulator.RunSimulation(bc);
    }
}
```

### Mirror Networking Integration How It Works with Mirror

1. Client Authority : Each client has authority over their own BreadboardController
2. Network Isolation : SyncVar and SyncDictionary ensure proper state synchronization
3. Instance Separation : Each client's simulator operates independently
4. UI Updates : Only the authoritative client updates its own UI Network State Management

```
public class BreadboardController : NetworkBehaviour
{
    [SyncVar] public bool isInSimMode;
    [SyncVar] public int studentId;
    [SyncVar] public float score;
    
    public SyncDictionary<string, BreadboardComponent> 
    breadboardComponents = 
        new SyncDictionary<string, BreadboardComponent>();
}
```

### Testing Results

- Before Fix : Progress bar updates affected all connected clients
- After Fix : Each client maintains independent progress tracking
- Network Performance : No impact on Mirror networking performance
- Memory Usage : Minimal increase (one simulator instance per client)

### Reference

- Commit : progress bar desync bug fix
