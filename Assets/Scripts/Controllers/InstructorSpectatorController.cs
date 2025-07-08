using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class InstructorSpectatorController : MonoBehaviour
{
    [Header("Spectator Settings")]
    public float spectatorHeight = -1.98f;
    public float spectatorDistance = -1.58f;
    public float transitionSpeed = 14f;

    [Header("Camera Offset Calibration")]
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, -1f); // Additional offset for fine-tuning
    public float heightOffset = 0.5f; // Extra height above the student
    public float backwardOffset = 1f; // Extra distance behind the student

    private enum SpectatorMode
    {
        FreeCamera,
        SpectatingStudent
    }

    private SpectatorMode currentMode = SpectatorMode.FreeCamera;
    private int currentSpectatedStudentIndex = -1;
    private List<PlayerController> studentPlayers = new List<PlayerController>();
    private Camera spectatorCamera;
    private PlayerController localPlayerController;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;
    private int originalCullingMask;

    // Store original camera settings for restoration
    private bool wasInSpectatorMode = false;

    // Property to check if currently spectating
    public bool IsSpectating => currentMode == SpectatorMode.SpectatingStudent;

    // Property to get the currently spectated student's breadboard
    public GameObject GetSpectatedStudentBreadboard()
    {
        if (!IsSpectating || currentSpectatedStudentIndex < 0 || currentSpectatedStudentIndex >= studentPlayers.Count)
            return null;

        PlayerController spectatedStudent = studentPlayers[currentSpectatedStudentIndex];
        if (spectatedStudent == null) return null;

        // Get the breadboard from NetworkManagerBreadboard
        NetworkManagerBreadboard networkManager = NetworkManager.singleton as NetworkManagerBreadboard;
        if (networkManager == null) return null;

        return networkManager.GetPlayerBreadboard(spectatedStudent);
    }

    // Property to get the currently spectated student
    public PlayerController GetSpectatedStudent()
    {
        if (!IsSpectating || currentSpectatedStudentIndex < 0 || currentSpectatedStudentIndex >= studentPlayers.Count)
            return null;

        return studentPlayers[currentSpectatedStudentIndex];
    }

    void Start()
    {
        // Only work for instructors and not on Android builds
#if !UNITY_ANDROID
        if (GameManager.Instance.CurrentRole != GameManager.UserRole.Instructor)
        {
            enabled = false;
            return;
        }

        // Find the local player controller
        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in allPlayers)
        {
            if (player.hasAuthority)
            {
                localPlayerController = player;
                break;
            }
        }

        if (localPlayerController == null)
        {
            Debug.LogError("InstructorSpectatorController: Could not find local player controller");
            enabled = false;
            return;
        }

        // Find the main camera
        spectatorCamera = localPlayerController.mainCamera;
        if (spectatorCamera == null)
        {
            Debug.LogError("InstructorSpectatorController: Could not find camera");
            enabled = false;
            return;
        }

        // Store original camera settings
        originalCameraPosition = spectatorCamera.transform.localPosition;
        originalCameraRotation = spectatorCamera.transform.localRotation;
        originalCameraParent = spectatorCamera.transform.parent;
        originalCullingMask = spectatorCamera.cullingMask;
#endif
    }

    void Update()
    {
#if !UNITY_ANDROID
        // Update student list periodically
        UpdateStudentList();

        // Handle input for spectator mode switching
        HandleSpectatorInput();

        // Monitor for experiment changes while spectating
        MonitorSpectatedExperimentChanges();

        // Update camera position if spectating
        if (currentMode == SpectatorMode.SpectatingStudent)
        {
            UpdateSpectatorCamera();
        }
#endif
    }

    void HandleSpectatorInput()
    {
        // Left click - cycle to previous student or enter spectator mode
        if (Input.GetMouseButtonDown(0))
        {
            CycleToPreviousTarget();
        }

        // Right click - cycle to next student
        if (Input.GetMouseButtonDown(1))
        {
            CycleToNextTarget();
        }
    }

    void CycleToPreviousTarget()
    {
        if (studentPlayers.Count == 0)
        {
            return;
        }

        if (currentMode == SpectatorMode.FreeCamera)
        {
            // Enter spectator mode with last student
            currentSpectatedStudentIndex = studentPlayers.Count - 1;
            EnterSpectatorMode();
        }
        else
        {
            // Cycle to previous student
            currentSpectatedStudentIndex--;
            if (currentSpectatedStudentIndex < 0)
            {
                // Loop back to free camera
                ExitSpectatorMode();
            }
            else
            {
                // Update spectating target and trigger UI update
                UpdateSpectatingTarget();
            }
        }

        Debug.Log($"Spectator Mode: {currentMode}, Student Index: {currentSpectatedStudentIndex}");
    }

    void CycleToNextTarget()
    {
        if (studentPlayers.Count == 0)
        {
            return;
        }

        if (currentMode == SpectatorMode.FreeCamera)
        {
            // Enter spectator mode with first student
            currentSpectatedStudentIndex = 0;
            EnterSpectatorMode();
        }
        else
        {
            // Cycle to next student
            currentSpectatedStudentIndex++;
            if (currentSpectatedStudentIndex >= studentPlayers.Count)
            {
                // Loop back to free camera
                ExitSpectatorMode();
            }
            else
            {
                // Update spectating target and trigger UI update
                UpdateSpectatingTarget();
            }
        }

        Debug.Log($"Spectator Mode: {currentMode}, Student Index: {currentSpectatedStudentIndex}");
    }

    void EnterSpectatorMode()
    {
        if (currentSpectatedStudentIndex < 0 || currentSpectatedStudentIndex >= studentPlayers.Count)
        {
            return;
        }

        currentMode = SpectatorMode.SpectatingStudent;
        wasInSpectatorMode = true;

        // Disable the local player's movement
        if (localPlayerController != null)
        {
            localPlayerController.enabled = false;
        }

        // Disable GvrEditorEmulator if it exists
        GvrEditorEmulator emulator = FindObjectOfType<GvrEditorEmulator>();
        if (emulator != null)
        {
            emulator.enabled = false;
        }

        UpdateSpectatingTarget();
    }

    void UpdateSpectatingTarget()
    {
        if (currentSpectatedStudentIndex < 0 || currentSpectatedStudentIndex >= studentPlayers.Count)
        {
            return;
        }

        PlayerController targetStudent = studentPlayers[currentSpectatedStudentIndex];

        // Update visibility: Hide the spectated student's model only from the instructor's view
        UpdatePlayerVisibility();

        // Initialize the last spectated experiment ID to sync with student's current experiment
        GameObject spectatedBreadboard = GetSpectatedStudentBreadboard();
        if (spectatedBreadboard != null)
        {
            BreadboardController bc = spectatedBreadboard.GetComponent<BreadboardController>();
            if (bc != null)
            {
                BreadboardSimulator simulator = bc.GetSimulatorInstance();
                if (simulator != null)
                {
                    ExperimentDefinitions experimentDefinitions = simulator.GetExperimentDefinitions();
                    if (experimentDefinitions != null)
                    {
                        // Sync the spectator's tracking with the student's current experiment
                        lastSpectatedExperimentId = experimentDefinitions.CurrentExperimentId;
                        Debug.Log($"Synced spectator to student's experiment ID: {lastSpectatedExperimentId}");
                    }
                }
            }
        }

        // Trigger UI update for the spectated student's breadboard
        TriggerSpectatedBreadboardUI();

        GameManager.Instance.SetInteractionMessage($"Spectating: {targetStudent.playerName}");
    }

    void UpdatePlayerVisibility()
    {
        // Reset all student visibility first
        foreach (var student in studentPlayers)
        {
            if (student.playerModel != null)
            {
                SetPlayerModelVisibility(student.playerModel, true);
            }
        }

        // Hide only the currently spectated student's model from instructor's view
        if (currentSpectatedStudentIndex >= 0 && currentSpectatedStudentIndex < studentPlayers.Count)
        {
            PlayerController spectatedStudent = studentPlayers[currentSpectatedStudentIndex];
            if (spectatedStudent.playerModel != null)
            {
                SetPlayerModelVisibility(spectatedStudent.playerModel, false);
            }
        }
    }

    void SetPlayerModelVisibility(GameObject playerModel, bool visible)
    {
        Renderer[] renderers = playerModel.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }

    void TriggerSpectatedBreadboardUI()
    {
        GameObject spectatedBreadboard = GetSpectatedStudentBreadboard();
        if (spectatedBreadboard != null)
        {
            // Notify BreadboardManager to switch to the spectated breadboard's UI
            BreadboardManager.Instance.NotifySimulationStarted(spectatedBreadboard);

            // Force a simulation update to refresh the UI
            BreadboardController bc = spectatedBreadboard.GetComponent<BreadboardController>();
            if (bc != null)
            {
                BreadboardSimulator simulator = bc.GetSimulatorInstance();
                if (simulator != null)
                {
                    // Use the proper state conversion method from BreadboardStateUtils
                    string currentState = BreadboardStateUtils.Instance.ConvertStateToJson(bc.breadboardComponents);
                    simulator.Run(currentState, bc);
                }
            }
        }
    }

    void ExitSpectatorMode()
    {
        currentMode = SpectatorMode.FreeCamera;
        currentSpectatedStudentIndex = -1;
        wasInSpectatorMode = false;

        // Reset the experiment tracking
        lastSpectatedExperimentId = -1;

        // Re-enable the local player's movement
        if (localPlayerController != null)
        {
            localPlayerController.enabled = true;
        }

        // Re-enable GvrEditorEmulator if it exists
        GvrEditorEmulator emulator = FindObjectOfType<GvrEditorEmulator>();
        if (emulator != null)
        {
            emulator.enabled = true;
        }

        // Restore all student model visibility
        foreach (var student in studentPlayers)
        {
            if (student.playerModel != null)
            {
                SetPlayerModelVisibility(student.playerModel, true);
            }
        }

        // Restore original camera position and culling mask
        if (spectatorCamera != null)
        {
            spectatorCamera.transform.SetParent(originalCameraParent);
            spectatorCamera.transform.localPosition = originalCameraPosition;
            spectatorCamera.transform.localRotation = originalCameraRotation;
            spectatorCamera.cullingMask = originalCullingMask;
        }

        // Return to instructor's own breadboard UI
        BreadboardManager.Instance.NotifySimulationExited();

        GameManager.Instance.ClearInteractionMessage();
    }

    void UpdateSpectatorCamera()
    {
        if (currentSpectatedStudentIndex < 0 || currentSpectatedStudentIndex >= studentPlayers.Count)
        {
            return;
        }

        PlayerController targetStudent = studentPlayers[currentSpectatedStudentIndex];
        if (targetStudent == null)
        {
            return;
        }

        // Calculate spectator position behind and above the student
        Vector3 studentPosition = targetStudent.transform.position;
        Vector3 lookDirection;

        // Use the student's forward direction for camera orientation
        if (targetStudent.neckTransform != null)
        {
            lookDirection = targetStudent.neckTransform.forward;
        }
        else
        {
            lookDirection = targetStudent.transform.forward;
        }

        // Calculate the spectator position
        Vector3 backwardDirection = -lookDirection;
        Vector3 spectatorPosition = studentPosition +
                                  backwardDirection * (spectatorDistance + backwardOffset) +
                                  Vector3.up * (spectatorHeight + heightOffset);

        // Apply additional camera offset
        Vector3 finalSpectatorPosition = spectatorPosition + cameraOffset;

        // Calculate rotation to look in the same direction as the student
        Quaternion spectatorRotation = Quaternion.LookRotation(lookDirection);

        // Smoothly move camera to spectator position with faster transition
        spectatorCamera.transform.position = Vector3.Lerp(
            spectatorCamera.transform.position,
            finalSpectatorPosition,
            transitionSpeed * Time.deltaTime
        );

        spectatorCamera.transform.rotation = Quaternion.Lerp(
            spectatorCamera.transform.rotation,
            spectatorRotation,
            transitionSpeed * Time.deltaTime
        );
    }

    void UpdateStudentList()
    {
        studentPlayers.Clear();

        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (var player in allPlayers)
        {
            // Only add students (not instructors) and not the local player
            if (player.playerName.Contains("Student") && !player.hasAuthority)
            {
                studentPlayers.Add(player);
            }
        }

        // Sort by student ID for consistent ordering
        studentPlayers.Sort((a, b) => a.id.CompareTo(b.id));

        // If we're spectating a student that no longer exists, exit spectator mode
        if (currentMode == SpectatorMode.SpectatingStudent &&
            (currentSpectatedStudentIndex >= studentPlayers.Count || studentPlayers.Count == 0))
        {
            ExitSpectatorMode();
        }
    }

    private int lastSpectatedExperimentId = -1;

    void MonitorSpectatedExperimentChanges()
    {
        if (currentMode == SpectatorMode.SpectatingStudent && currentSpectatedStudentIndex >= 0)
        {
            GameObject spectatedBreadboard = GetSpectatedStudentBreadboard();
            if (spectatedBreadboard != null)
            {
                BreadboardController bc = spectatedBreadboard.GetComponent<BreadboardController>();
                if (bc != null)
                {
                    BreadboardSimulator simulator = bc.GetSimulatorInstance();
                    if (simulator != null)
                    {
                        ExperimentDefinitions experimentDefinitions = simulator.GetExperimentDefinitions();
                        if (experimentDefinitions != null)
                        {
                            int currentExperimentId = experimentDefinitions.CurrentExperimentId;
                            if (lastSpectatedExperimentId != currentExperimentId)
                            {
                                lastSpectatedExperimentId = currentExperimentId;
                                TriggerSpectatedBreadboardUI();
                            }
                        }
                    }
                }
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layerIndex)
    {
        if (obj == null) return;

        obj.layer = layerIndex;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layerIndex);
        }
    }

    void OnDestroy()
    {
        // Make sure to restore camera if this script is destroyed while spectating
        if (wasInSpectatorMode)
        {
            ExitSpectatorMode();
        }
    }
}

