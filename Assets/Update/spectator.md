# Instructor Spectator System Documentation

## Overview

The Instructor Spectator System allows instructors to observe students' VR breadboard experiments in real-time. This system provides seamless camera control, UI synchronization, and experiment tracking while maintaining network performance and user experience.

## Architecture Components

### Core Classes

- InstructorSpectatorController : Main spectator logic and camera control
- BreadboardController : Network synchronization with SyncVar for experiment IDs
- BreadboardSimulator : UI updates and experiment state management
- BreadboardManager : UI switching and breadboard management

## Key Features Implementation

### 1. GvrEditorEmulator Override

Problem : The Google VR Editor Emulator interfered with spectator camera control by continuing to process mouse input for VR simulation.

Solution : Disable GvrEditorEmulator when entering spectator mode:

```
void EnterSpectatorMode()
{
    if (currentSpectatedStudentIndex < 0 || 
    currentSpectatedStudentIndex >= studentPlayers.Count)
    {
        return;
    }

    currentMode = SpectatorMode.SpectatingStudent;
    wasInSpectatorMode = true;

    // Disable the local player's movement
    if (localPlayerController != null)
    {
        localPlayerController.enabled = false;
    }

    // Disable GvrEditorEmulator if it exists
    GvrEditorEmulator emulator = 
    FindObjectOfType<GvrEditorEmulator>();
    if (emulator != null)
    {
        emulator.enabled = false;
    }

    UpdateSpectatingTarget();
}
```

Why This Works : By disabling the emulator, we prevent conflicting input handling and allow the spectator system to have full control over camera movement.

### 2. Camera Transform and Rotation Copying

Implementation : The spectator camera smoothly follows the student's position and orientation:

```
void UpdateSpectatorCamera()
{
    if (currentSpectatedStudentIndex < 0 || 
    currentSpectatedStudentIndex >= studentPlayers.Count)
    {
        return;
    }

    PlayerController targetStudent = studentPlayers
    [currentSpectatedStudentIndex];
    if (targetStudent == null)
    {
        return;
    }

    // Calculate spectator position behind and above the 
    student
    Vector3 studentPosition = targetStudent.transform.
    position;
    Vector3 lookDirection;

    // Use the student's forward direction for camera 
    orientation
    if (targetStudent.neckTransform != null)
    {
        lookDirection = targetStudent.neckTransform.
        forward;
    }
    else
    {
        lookDirection = targetStudent.transform.forward;
    }

    // Calculate the spectator position
    Vector3 backwardDirection = -lookDirection;
    Vector3 spectatorPosition = studentPosition +
                              backwardDirection * 
                              (spectatorDistance + 
                              backwardOffset) +
                              Vector3.up * 
                              (spectatorHeight + 
                              heightOffset);

    // Apply additional camera offset
    Vector3 finalSpectatorPosition = spectatorPosition + 
    cameraOffset;

    // Calculate rotation to look in the same direction 
    as the student
    Quaternion spectatorRotation = Quaternion.LookRotation
    (lookDirection);

    // Smoothly move camera to spectator position with 
    faster transition
    spectatorCamera.transform.position = Vector3.Lerp(
        spectatorCamera.transform.position,
        finalSpectatorPosition,
        transitionSpeed * Time.deltaTime
    );

    spectatorCamera.transform.rotation = Quaternion.Lerp(
        spectatorCamera.transform.rotation,
        spectatorRotation,
        transitionSpeed * Time.deltaTime
    );
}
```

Key Features :

- Position Calculation : Places camera behind and above the student using configurable offsets
- Orientation Matching : Camera looks in the same direction as the student's head/neck
- Smooth Transitions : Uses Vector3.Lerp and Quaternion.Lerp for fluid camera movement
- Configurable Offsets : Allows fine-tuning of camera position through inspector values

### 3. Student Model Hiding

Problem : The spectated student's model would obstruct the instructor's view of the breadboard.

Solution : Selectively hide only the spectated student's model:

```
void UpdatePlayerVisibility()
{
    // Reset all student visibility first
    foreach (var student in studentPlayers)
    {
        if (student.playerModel != null)
        {
            SetPlayerModelVisibility(student.playerModel, 
            true);
        }
    }

    // Hide only the currently spectated student's model 
    from instructor's view
    if (currentSpectatedStudentIndex >= 0 && 
    currentSpectatedStudentIndex < studentPlayers.Count)
    {
        PlayerController spectatedStudent = studentPlayers
        [currentSpectatedStudentIndex];
        if (spectatedStudent.playerModel != null)
        {
            SetPlayerModelVisibility(spectatedStudent.
            playerModel, false);
        }
    }
}

void SetPlayerModelVisibility(GameObject playerModel, 
bool visible)
{
    Renderer[] renderers = playerModel.
    GetComponentsInChildren<Renderer>();
    foreach (Renderer renderer in renderers)
    {
        renderer.enabled = visible;
    }
}
```

Why This Approach :

- Only affects the instructor's view, not other clients
- Maintains network performance by not modifying network state
- Provides clear view of the breadboard without obstruction

### 4. Experiment ID Synchronization

Challenge : ExperimentDefinitions was not a NetworkBehaviour , so CurrentExperimentId wasn't synchronized across the network.

Solution : Added SyncVar to BreadboardController with network command:

```
[SyncVar(hook = nameof(OnCurrentExperimentIdChanged))]
public int currentExperimentId = 1;

void OnCurrentExperimentIdChanged(int oldValue, int 
newValue)
{
    Debug.Log($"Experiment ID changed from {oldValue} to 
    {newValue} on breadboard {studentId}");

    // Update the local experiment definitions
    if (simulatorInstance != null)
    {
        ExperimentDefinitions expDefs = simulatorInstance.
        GetExperimentDefinitions();
        if (expDefs != null)
        {
            expDefs.CurrentExperimentId = newValue;

            // Force a UI update if this is the spectated 
            breadboard
            InstructorSpectatorController 
            spectatorController = 
            FindObjectOfType<InstructorSpectatorController
            >();
            if (spectatorController != null && 
            spectatorController.IsSpectating)
            {
                // Trigger UI refresh
                string currentState = 
                BreadboardStateUtils.Instance.
                ConvertStateToJson(breadboardComponents);
                simulatorInstance.Run(currentState, this);
            }
        }
    }
}

[Command]
public void CmdUpdateExperimentId(int newExperimentId)
{
    currentExperimentId = newExperimentId;
}
```

Integration with ExperimentDefinitions :

```
public void NextExperiment()
{
    // Save current instruction index for the current 
    experiment
    _lastInstructionIndices[CurrentExperimentId] = 
    CurrentInstructionIndex;
    
    if (CurrentExperimentId < _experiments.Count)
    {
        CurrentExperimentId++;
        
        // Restore the last instruction index for this 
        experiment, or default to 0
        CurrentInstructionIndex = _lastInstructionIndices.
        ContainsKey(CurrentExperimentId)
            ? _lastInstructionIndices[CurrentExperimentId]
            : 0;
            
        // Use the associated controller instead of 
        FindObjectOfType
        if (_associatedController != null && 
        _associatedController.hasAuthority)
        {
            _associatedController.CmdUpdateExperimentId
            (CurrentExperimentId);
        }
        
        Debug.Log($"Moved to experiment 
        {CurrentExperimentId}");
    }
}
```

### 5. UI Synchronization

Problem : Instructor needed to see the student's UI state in real-time, including experiment instructions and progress.

Solution : Multi-layered UI synchronization system:

```
void TriggerSpectatedBreadboardUI()
{
    GameObject spectatedBreadboard = 
    GetSpectatedStudentBreadboard();
    if (spectatedBreadboard != null)
    {
        // Notify BreadboardManager to switch to the 
        spectated breadboard's UI
        BreadboardManager.Instance.NotifySimulationStarted
        (spectatedBreadboard);

        // Force a simulation update to refresh the UI
        BreadboardController bc = spectatedBreadboard.
        GetComponent<BreadboardController>();
        if (bc != null)
        {
            BreadboardSimulator simulator = bc.
            GetSimulatorInstance();
            if (simulator != null)
            {
                // Use the proper state conversion method 
                from BreadboardStateUtils
                string currentState = 
                BreadboardStateUtils.Instance.
                ConvertStateToJson(bc.
                breadboardComponents);
                simulator.Run(currentState, bc);
            }
        }
    }
}

void MonitorSpectatedExperimentChanges()
{
    if (currentMode == SpectatorMode.SpectatingStudent && 
    currentSpectatedStudentIndex >= 0)
    {
        GameObject spectatedBreadboard = 
        GetSpectatedStudentBreadboard();
        if (spectatedBreadboard != null)
        {
            BreadboardController bc = spectatedBreadboard.
            GetComponent<BreadboardController>();
            if (bc != null)
            {
                BreadboardSimulator simulator = bc.
                GetSimulatorInstance();
                if (simulator != null)
                {
                    ExperimentDefinitions 
                    experimentDefinitions = simulator.
                    GetExperimentDefinitions();
                    if (experimentDefinitions != null)
                    {
                        int currentExperimentId = 
                        experimentDefinitions.
                        CurrentExperimentId;
                        if (lastSpectatedExperimentId != 
                        currentExperimentId)
                        {
                            lastSpectatedExperimentId = 
                            currentExperimentId;
                            TriggerSpectatedBreadboardUI
                            ();
                        }
                    }
                }
            }
        }
    }
}
```

BreadboardSimulator Integration :

```
public void UpdateUI(BreadboardController bc, 
ExperimentResult result)
{
    // Check if we're spectating and need to use the 
    student's experiment data
    InstructorSpectatorController spectatorController = 
    FindObjectOfType<InstructorSpectatorController>();
    if (spectatorController != null && 
    spectatorController.IsSpectating &&
        spectatorController.GetSpectatedStudentBreadboard
        () == bc.gameObject)
    {
        // Use the student's experiment definitions and 
        UI elements
        targetExperimentDefinitions = bc.
        GetSimulatorInstance().GetExperimentDefinitions();
        currentExperimentId = bc.currentExperimentId; // 
        Use the synchronized experiment ID
        
        // Try to find UI elements within the spectated 
        breadboard's transform
        labMessagesTransform = FindLabMessagesInBreadboard
        (bc.transform);
        if (labMessagesTransform == null)
        {
            labMessagesTransform = this.
            labMessagesTransform; // Fallback to 
            instructor's
        }
        
        Debug.Log($"Spectating: Using student's UI 
        elements from breadboard {bc.studentId}, 
        experiment {currentExperimentId}");
    }
    else
    {
        // Use this instance's experiment definitions
        targetExperimentDefinitions = this.
        experimentDefinitions;
        currentExperimentId = targetExperimentDefinitions.
        CurrentExperimentId;
        labMessagesTransform = this.labMessagesTransform;
    }
    
    // Rest of UI update logic...
}
```

## Technical Challenges and Solutions

### Challenge 1: Multiple Controller References

Problem : Using GameObject.FindObjectOfType<BreadboardController>() returned random controllers in multi-student environments.

Solution : Pass specific controller references through the architecture:

- ExperimentDefinitions constructor now takes BreadboardController parameter
- Each instance maintains reference to its associated controller
- Eliminates random controller selection

### Challenge 2: Network State Synchronization

Problem : ExperimentDefinitions wasn't a NetworkBehaviour , so experiment changes weren't synchronized.

Solution :

- Added [SyncVar] to BreadboardController for currentExperimentId
- Implemented [Command] method for network updates
- Added hook to update local state when network state changes

### Challenge 3: UI Instance Isolation

Problem : Singleton pattern in BreadboardSimulator caused UI updates to affect all clients.

Solution :

- Converted to instance-based architecture
- Each BreadboardController has its own BreadboardSimulator
- UI updates only affect the appropriate client

### Challenge 4: Camera Control Conflicts

Problem : VR emulator and player controller interfered with spectator camera.

Solution :

- Disable conflicting components during spectator mode
- Store original camera state for restoration
- Implement smooth camera transitions

## Usage

Controls :

- Left Mouse Button : Switch to next student
- Right Mouse Button : Switch to previous student
- Automatic : Exit spectator mode when no students available
  Features :

- Real-time experiment tracking
- Synchronized UI updates
- Smooth camera following
- Student model hiding for clear view

## Platform Compatibility

- Desktop : Full functionality
- Android : Disabled (performance considerations)
- VR : Compatible with instructor mode only
