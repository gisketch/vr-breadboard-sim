using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

        // Hide student player models from instructor's view
        int studentLayer = LayerMask.NameToLayer("StudentPlayer");
        if (studentLayer != -1)
        {
            spectatorCamera.cullingMask &= ~(1 << studentLayer);
        }

        PlayerController targetStudent = studentPlayers[currentSpectatedStudentIndex];
        GameManager.Instance.SetInteractionMessage($"Spectating: {targetStudent.playerName}");
    }

    void ExitSpectatorMode()
    {
        currentMode = SpectatorMode.FreeCamera;
        currentSpectatedStudentIndex = -1;
        wasInSpectatorMode = false;

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

        // Restore original camera position and culling mask
        if (spectatorCamera != null)
        {
            spectatorCamera.transform.SetParent(originalCameraParent);
            spectatorCamera.transform.localPosition = originalCameraPosition;
            spectatorCamera.transform.localRotation = originalCameraRotation;
            spectatorCamera.cullingMask = originalCullingMask;
        }

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

        // Use the student's player model rotation instead of camera rotation
        // The player model rotation is synchronized across the network
        Vector3 targetPosition = targetStudent.transform.position;
        Vector3 studentForward = targetStudent.transform.forward;

        // Get the student's neck transform for head rotation if available
        Transform studentNeck = targetStudent.neckTransform;
        Vector3 lookDirection = studentForward;

        if (studentNeck != null)
        {
            // Use the neck's forward direction for more accurate head tracking
            lookDirection = studentNeck.forward;
        }

        // Calculate base position behind and above the student
        Vector3 baseSpectatorPosition = targetPosition - (lookDirection * (spectatorDistance + backwardOffset)) + (Vector3.up * (spectatorHeight + heightOffset));

        // Apply additional calibration offset
        Vector3 rightDirection = Vector3.Cross(Vector3.up, lookDirection).normalized;
        Vector3 upDirection = Vector3.up;

        Vector3 finalSpectatorPosition = baseSpectatorPosition +
            (rightDirection * cameraOffset.x) +
            (upDirection * cameraOffset.y) +
            (lookDirection * cameraOffset.z);

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

                // Set student player models to StudentPlayer layer for culling
                if (player.playerModel != null)
                {
                    int studentLayer = LayerMask.NameToLayer("StudentPlayer");
                    if (studentLayer == -1)
                    {
                        // Create the layer if it doesn't exist (this should be done in Unity Editor)
                        Debug.LogWarning("StudentPlayer layer not found. Please create this layer in Unity Editor.");
                    }
                    else
                    {
                        SetLayerRecursively(player.playerModel, studentLayer);
                    }
                }
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