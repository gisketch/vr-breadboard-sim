using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Newtonsoft.Json.Linq;

namespace Mirror
{
    public class BreadboardController : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private bool allowInstructor = false;

        public readonly SyncDictionary<string, BreadboardComponentData> breadboardComponents = new SyncDictionary<string, BreadboardComponentData>();

        [SyncVar]
        public bool isInSimMode = false;
        private bool prevIsInSimMode = false;

        [SyncVar]
        public bool isPortraitMode = true;
        private bool prevIsPortraitMode = true;

        [SyncVar(hook = nameof(OnStudentIdChanged))]
        public int studentId = 0;
        [TextArea(10, 20)]
        [SyncVar]
        public string score = "";

        private BreadboardSimulator simulatorInstance;

        // Animation related fields
        private Transform breadboardTransform;
        [SerializeField] private Vector3 idleBreadboardPosition;
        private Quaternion idleBreadboardRotation;
        private Quaternion idleChildRotation;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;
        private int currentPositionTweenId = -1;
        private int currentRotationTweenId = -1;
        private int childRotationTweenId = -1;

        private bool hasInitialized = false;

        private bool isHovering = false;

        public Transform labMessagesTransform;

        private int id = -1;

        void Awake()
        {
            // Find the breadboard child transform
            breadboardTransform = FindBreadboardChild(transform);
            if (breadboardTransform == null)
            {
                Debug.LogError("Could not find 'Breadboard' child object. Using parent instead.");
                breadboardTransform = transform;
            }

            // Create and initialize the simulator instance for this controller
            CreateSimulatorInstance();
        }

        private void CreateSimulatorInstance()
        {
            // Find or create a BreadboardSimulator component
            simulatorInstance = GetComponent<BreadboardSimulator>();
            if (simulatorInstance == null)
            {
                simulatorInstance = gameObject.AddComponent<BreadboardSimulator>();
            }

            // Initialize the simulator for this specific controller
            simulatorInstance.Initialize(this);
            Debug.Log($"Created simulator instance for controller {studentId}");
        }

        // Add getter for the simulator instance
        public BreadboardSimulator GetSimulatorInstance()
        {
            return simulatorInstance;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (hasAuthority) BreadboardStateUtils.Instance.myBreadboardController = this;

            // Store initial positions and rotations
            idleBreadboardPosition = transform.localPosition;
            idleBreadboardRotation = transform.rotation;
            if (breadboardTransform != null)
            {
                idleChildRotation = breadboardTransform.localRotation;
            }

            // Set initial state tracking
            prevIsInSimMode = isInSimMode;
            prevIsPortraitMode = isPortraitMode;
            hasInitialized = true;

            // Register for SyncDictionary changes using Callback instead of OnChange
            breadboardComponents.Callback += OnBreadboardComponentsChanged;

            // Apply current state (if already in sim mode when joining)
            if (isInSimMode)
            {
                ApplySimulationMode();

                // Only create UI if this is our own breadboard
                if (hasAuthority)
                {
                    BreadboardManager.Instance.NotifySimulationStarted(gameObject);
                }
            }
        }

        void Start()
        {
            if (hasAuthority)
            {
                Debug.Log("I have authority, starting udpate!");
                if (GameManager.Instance.CurrentRole == GameManager.UserRole.Student)
                {
                    ClassroomManager cm = GameObject.Find("chalkboard").GetComponent<ClassroomManager>();
                    Debug.Log("Updating Breadboard Id!");
                    CmdUpdateId(cm.currentBreadboardId);
                    id = cm.currentBreadboardId;
                    cm.CmdIncrementBreadboardId();
                }
            }
        }

        void OnStudentIdChanged(int oldValue, int newValue)
        {
            // Only update the score if we have a valid student ID
            if (newValue > 0)
            {
                CmdUpdateScore($@"Student {newValue}
 - experiment 1 : 0/8
 - experiment 2 : 0/16
 - experiment 3 : 0/8
        ");
            }
        }

        [Command]
        public void CmdUpdateScore(string scoreStr)
        {
            score = scoreStr;

            // // Also update the score in the classroom manager
            if (studentId > 0)
            {
                ClassroomManager scoreManager = GameObject.Find("chalkboard").GetComponent<ClassroomManager>();
                if (scoreManager != null)
                {
                    scoreManager.CmdUpdateStudentScore(studentId, scoreStr);
                }
            }
        }

        public override void OnStopClient()
        {
            // Remove handler when client stops
            breadboardComponents.Callback -= OnBreadboardComponentsChanged;
        }


        [Command(ignoreAuthority = false)]
        public void CmdUpdateId(int toChange)
        {
            studentId = toChange;
            Debug.Log($"Updated ID to {studentId}");
        }

        void OnBreadboardComponentsChanged(SyncIDictionary<string, BreadboardComponentData>.Operation op, string key, BreadboardComponentData item)
        {
            BreadboardStateUtils.Instance.VisualizeBreadboard(this);
        }

        private void SaveBreadboardState(int slot)
        {
            if (!hasAuthority) return;

            // Convert SyncDictionary to JSON
            Dictionary<string, BreadboardComponentData> regularDict = new Dictionary<string, BreadboardComponentData>();
            foreach (var kvp in breadboardComponents)
            {
                regularDict[kvp.Key] = kvp.Value;
            }

            string jsonState = JsonUtility.ToJson(new SerializableBreadboardState(regularDict));
            PlayerPrefs.SetString($"BreadboardState_Slot{slot}", jsonState);
            PlayerPrefs.Save();
            Debug.Log($"Saved breadboard state to slot {slot}");
        }

        private void LoadBreadboardState(int slot)
        {
            if (!hasAuthority) return;

            string jsonState = PlayerPrefs.GetString($"BreadboardState_Slot{slot}", "");
            if (string.IsNullOrEmpty(jsonState))
            {
                Debug.Log($"No saved state found in slot {slot}");
                return;
            }

            SerializableBreadboardState loadedState = JsonUtility.FromJson<SerializableBreadboardState>(jsonState);

            // Clear current components
            CmdClearAllComponents();

            // Add loaded components
            foreach (var kvp in loadedState.components)
            {
                CmdAddComponent(kvp.Key, kvp.Value);
            }

            Debug.Log($"Loaded breadboard state from slot {slot}");
        }

        void Update()
        {
            if (hasInitialized)
            {
                if (hasAuthority)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) LoadBreadboardState(1);
                    if (Input.GetKeyDown(KeyCode.Alpha2)) LoadBreadboardState(2);
                    if (Input.GetKeyDown(KeyCode.Alpha3)) LoadBreadboardState(3);
                    if (Input.GetKeyDown(KeyCode.Alpha4)) LoadBreadboardState(4);

                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (Input.GetKeyDown(KeyCode.Z)) SaveBreadboardState(1);
                        if (Input.GetKeyDown(KeyCode.X)) SaveBreadboardState(2);
                        if (Input.GetKeyDown(KeyCode.C)) SaveBreadboardState(3);
                        if (Input.GetKeyDown(KeyCode.D)) SaveBreadboardState(4);
                    }
                }

                if (isHovering && hasAuthority && InputManager.Instance.GetPrimaryButtonDown())
                {
                    ClickOwner();
                }
                // Check for sim mode change
                if (prevIsInSimMode != isInSimMode)
                {
                    if (isInSimMode)
                    {
                        idleBreadboardPosition = transform.position;
                        ApplySimulationMode();

                        // Only create UI if this is our own breadboard
                        if (hasAuthority)
                        {
                            BreadboardManager.Instance.NotifySimulationStarted(gameObject);
                        }
                    }
                    else
                    {
                        ApplyIdleMode();

                        // Only remove UI if this is our own breadboard
                        if (hasAuthority)
                        {
                            BreadboardManager.Instance.NotifySimulationExited();
                        }
                    }
                    prevIsInSimMode = isInSimMode;
                }

                // Check for orientation change
                if (prevIsPortraitMode != isPortraitMode && isInSimMode)
                {
                    Quaternion childTargetRotation = CalculateChildRotation();
                    RotateBreadboardChild(childTargetRotation);

                    // Only update UI if this is our own breadboard
                    if (hasAuthority)
                    {
                        BreadboardManager.Instance.NotifyOrientationChanged(isPortraitMode);
                    }
                    prevIsPortraitMode = isPortraitMode;
                }
            }

            // Debug key-press checks
            if (hasAuthority)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    var wireData = new BreadboardComponentData
                    {
                        type = "wire",
                        startNode = "1A",
                        endNode = "1B",
                        color = "red"
                    };
                    CmdAddComponent("wire1", wireData);
                }

                if (Input.GetKeyDown(KeyCode.G))
                {
                    CmdRemoveComponent("wire1");
                }
            }
        }

        [Command]
        public void CmdAddComponent(string id, BreadboardComponentData component)
        {
            breadboardComponents[id] = component;
        }

        [Command]
        public void CmdRemoveComponent(string id)
        {
            breadboardComponents.Remove(id);
        }

        [Command]
        public void CmdClearAllComponents()
        {
            breadboardComponents.Clear();
        }

        [Command]
        public void CmdToggleSimMode(bool active)
        {
            isInSimMode = active;
        }

        [Command]
        public void CmdToggleOrientation()
        {
            isPortraitMode = !isPortraitMode;
        }

        // Animation methods remain unchanged
        // Called when entering simulation mode
        private void ApplySimulationMode()
        {
            CancelOngoingTweens();

            // Calculate target positions and rotations
            Vector3 targetPosition = CalculateSimulationPosition();
            Quaternion parentTargetRotation = CalculateParentRotation();
            Quaternion childTargetRotation = CalculateChildRotation();

            // Animate to simulation mode
            AnimateBreadboard(targetPosition, parentTargetRotation, childTargetRotation);
        }

        // Called when exiting simulation mode
        private void ApplyIdleMode()
        {
            CancelOngoingTweens();

            // Animate back to idle position
            AnimateBreadboard(idleBreadboardPosition, idleBreadboardRotation, idleChildRotation);
        }

        void HoverOwner()
        {
            if (!isInSimMode)
            {
                GameManager.Instance.SetInteractionMessage("Your Breadboard");
            }
        }

        void HoverOther()
        {
        }

        void ClickOwner()
        {
            if (!isInSimMode)
            {
                CmdToggleSimMode(true);
                GameManager.Instance.ClearInteractionMessage();
                BreadboardStateUtils.Instance.VisualizeBreadboard(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasAuthority)
            {
                HoverOwner();
                isHovering = true;
            }
            else
            {
                HoverOther();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            if (!isInSimMode) GameManager.Instance.ClearInteractionMessage();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hasAuthority)
            {
                // Check if instructor is in spectator mode and prevent interaction
                InstructorSpectatorController spectatorController = GetComponent<InstructorSpectatorController>();
                if (spectatorController != null && spectatorController.IsSpectating)
                {
                    return; // Don't allow breadboard interaction while spectating
                }

                ClickOwner();
            }
        }

        // Animation Helper Methods - unchanged
        private Transform FindBreadboardChild(Transform parent)
        {
            // Look for a child named "Breadboard"
            Transform child = parent.Find("Breadboard");
            if (child != null) return child;

            // If not found at top level, search deeper
            foreach (Transform t in parent)
            {
                if (t.name.Contains("Breadboard"))
                    return t;
            }

            return null;
        }

        private Vector3 CalculateSimulationPosition()
        {
            return new Vector3(
                transform.position.x,
                transform.position.y + 2.5f,
                transform.position.z
            );
        }

        private Quaternion CalculateParentRotation()
        {
            Vector3 eulerAngles;
            eulerAngles.x = 0f;
            eulerAngles.y = 180f;
            eulerAngles.z = 0f;

            return Quaternion.Euler(eulerAngles);
        }

        private Quaternion CalculateChildRotation()
        {
            // Adjust Z rotation based on orientation mode
            if (isPortraitMode)
            {
                return Quaternion.Euler(0, 0, -90f);
            }
            else
            {
                return Quaternion.Euler(0, 0, 0f);
            }
        }

        private void AnimateBreadboard(Vector3 targetPosition, Quaternion parentTargetRotation, Quaternion childTargetRotation)
        {
            MoveBreadboard(targetPosition);
            RotateBreadboardParent(parentTargetRotation.eulerAngles);
            RotateBreadboardChild(childTargetRotation);
        }

        private void MoveBreadboard(Vector3 targetPosition)
        {
            currentPositionTweenId = LeanTween.move(gameObject, targetPosition, transitionDuration)
                .setEase(easeType)
                .id;
        }

        private void RotateBreadboardParent(Vector3 targetEulerAngles)
        {
            currentRotationTweenId = LeanTween.rotate(gameObject, targetEulerAngles, transitionDuration)
                .setEase(easeType)
                .id;
        }

        private void RotateBreadboardChild(Quaternion targetRotation)
        {
            if (breadboardTransform == null) return;

            // Cancel any ongoing child rotation tween
            if (childRotationTweenId != -1)
            {
                LeanTween.cancel(childRotationTweenId);
            }

            // Animate the child's local rotation
            childRotationTweenId = LeanTween.rotateLocal(breadboardTransform.gameObject, targetRotation.eulerAngles, transitionDuration)
                .setEase(easeType)
                .id;
        }

        private void CancelOngoingTweens()
        {
            if (currentPositionTweenId != -1)
            {
                LeanTween.cancel(currentPositionTweenId);
                currentPositionTweenId = -1;
            }

            if (currentRotationTweenId != -1)
            {
                LeanTween.cancel(currentRotationTweenId);
                currentRotationTweenId = -1;
            }

            if (childRotationTweenId != -1)
            {
                LeanTween.cancel(childRotationTweenId);
                childRotationTweenId = -1;
            }
        }
    }

    [Serializable]
    public class SerializableBreadboardState
    {
        public SerializableKeyValuePair[] components;

        public SerializableBreadboardState(Dictionary<string, BreadboardComponentData> dict)
        {
            components = new SerializableKeyValuePair[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                components[i] = new SerializableKeyValuePair
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                };
                i++;
            }
        }
    }

    [Serializable]
    public class SerializableKeyValuePair
    {
        public string Key;
        public BreadboardComponentData Value;
    }
}
