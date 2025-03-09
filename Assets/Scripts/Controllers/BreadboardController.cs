using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Mirror
{
    public class BreadboardController : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private bool allowInstructor = false;

        [TextArea(10,20)]
        [SyncVar]
        public string breadboardState = "{ \"components\": {} }";
        private string previousBreadboardState;

        [SyncVar]
        public bool isInSimMode = false;
        private bool prevIsInSimMode = false;

        [SyncVar]
        public bool isPortraitMode = true;
        private bool prevIsPortraitMode = true;

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

        void Awake()
        {
            // Find the breadboard child transform
            breadboardTransform = FindBreadboardChild(transform);
            if (breadboardTransform == null)
            {
                Debug.LogError("Could not find 'Breadboard' child object. Using parent instead.");
                breadboardTransform = transform;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if(hasAuthority) BreadboardStateUtils.Instance.myBreadboardController = this;
            
            // Store initial positions and rotations
            idleBreadboardPosition = transform.localPosition;
            idleBreadboardRotation = transform.rotation;
            Debug.Log("Client start: " + idleBreadboardPosition);
            if (breadboardTransform != null)
            {
                idleChildRotation = breadboardTransform.localRotation;
            }

            // Set initial state tracking
            prevIsInSimMode = isInSimMode;
            prevIsPortraitMode = isPortraitMode;
            hasInitialized = true;

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
        void OnBreadboardStateChanged(string oldState, string newState)
        {
            Debug.Log("Changes Detected! Updating...");

            BreadboardStateUtils.Instance.VisualizeBreadboard(this);
        }


        void Update()
        {
            if (hasInitialized)
            {

                // Check for breadboard state changes
                if (breadboardState != previousBreadboardState)
                {
                    OnBreadboardStateChanged(previousBreadboardState, breadboardState);
                    previousBreadboardState = breadboardState;
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

            // Existing key-press checks for testing
            if (hasAuthority)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    CmdUpdateBreadboardState("{ new state }");
                }

                if (Input.GetKeyDown(KeyCode.G))
                {
                    CmdUpdateBreadboardState("{ asdfasdf state }");
                }
            }
        }

        [Command]
        public void CmdUpdateBreadboardState(string state)
        {
            breadboardState = state;
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
            if (!isInSimMode)
            {
                GameManager.Instance.SetInteractionMessage("Spectate Breadboard");
            }
        }

        void ClickOwner()
        {
            if (!isInSimMode)
            {
                CmdToggleSimMode(true);
                GameManager.Instance.ClearInteractionMessage();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasAuthority)
            {
                HoverOwner();
            }
            else
            {
                HoverOther();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            GameManager.Instance.ClearInteractionMessage();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hasAuthority)
            {
                ClickOwner();
            }
        }

        // Animation Helper Methods
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
}
