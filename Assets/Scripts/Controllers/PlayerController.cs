using UnityEngine;
using TMPro;

namespace Mirror
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 10f;
        public float acceleration = 50f;
        public float deceleration = 20f;
        public float verticalFlySpeed = 10f;
        public float gravity = 20f;

        [Header("Rotation Settings")]
        public float maxNeckAngle = 50f;
        public float minNeckAngle = -50f;

        [Header("Visibility Settings")]
        public string localPlayerLayer = "LocalPlayer";

        public GameObject playerModel;
        public Transform neckTransform;

        [HideInInspector]
        public bool isFlyMode = false;

        private CharacterController characterController;
        private Camera mainCamera;
        private Vector3 currentVelocity = Vector3.zero;
        private float verticalVelocity = 0f;
        private int localPlayerLayerIndex;

        private void Awake()
        {
            localPlayerLayerIndex = LayerMask.NameToLayer(localPlayerLayer);

            if (localPlayerLayerIndex == -1)
            {
                Debug.LogError("Layer '" + localPlayerLayer + "' does not exist. Please create this layer in the Unity Editor.");
            }
        }

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = transform.GetComponentInChildren<Camera>();

            if (mainCamera == null)
            {
                if (hasAuthority)
                {
                    Transform menuPlayer = GameObject.Find("MenuPlayer").transform;
                    Transform mainCamTransform = menuPlayer.GetChild(0);
                    mainCamTransform.SetParent(transform);
                    mainCamera = mainCamTransform.GetComponentInChildren<Camera>();
                }
            }
            else
            {
                if (!hasAuthority)
                {
                    Destroy(mainCamera.gameObject);
                }
            }

            // Configure layer visibility for the local player
            if (hasAuthority && mainCamera != null)
            {
                SetLayerRecursively(playerModel, localPlayerLayerIndex);

                mainCamera.cullingMask &= ~(1 << localPlayerLayerIndex);
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

        [Client]
        private void Update()
        {
            if (!hasAuthority) { return; }
            HandleMovement();
            HandleVerticalMovement();
            HandleRotation();

            if (!isFlyMode && BreadboardManager.Instance.IsSimulationMode)
            {
                isFlyMode = true;
            }

            if (isFlyMode && !BreadboardManager.Instance.IsSimulationMode)
            {
                isFlyMode = false;
            }



        }

        private void HandleRotation()
        {
            if (mainCamera != null)
            {
                Vector3 modelRotation = playerModel.transform.eulerAngles;
                modelRotation.y = mainCamera.transform.eulerAngles.y;
                playerModel.transform.eulerAngles = modelRotation;

                if (neckTransform != null)
                {
                    float xAngle = mainCamera.transform.eulerAngles.x;

                    if (xAngle > 180) xAngle -= 360;

                    xAngle = Mathf.Clamp(xAngle, minNeckAngle, maxNeckAngle);

                    Vector3 neckRotation = neckTransform.localEulerAngles;
                    neckRotation.x = xAngle;
                    neckTransform.localEulerAngles = neckRotation;
                }
            }
        }

        private void HandleMovement()
        {
            Vector2 input = InputManager.Instance.GetMovementInput();

            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = mainCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            Vector3 targetMoveDirection = (cameraForward * input.y + cameraRight * input.x);

            if (targetMoveDirection.magnitude > 1f)
                targetMoveDirection.Normalize();

            Vector3 targetVelocity = targetMoveDirection * moveSpeed;

            float accelRate = input.magnitude > 0.1f ? acceleration : deceleration;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, accelRate * Time.deltaTime);

            characterController.Move(currentVelocity * Time.deltaTime);
        }

        private void HandleVerticalMovement()
        {
            if (isFlyMode)
            {
                verticalVelocity = 0f;
                characterController.height = 4.5f;
                moveSpeed = 2.0f;
                verticalFlySpeed = 1.5f;

                if (InputManager.Instance.GetAscendButton())
                {
                    characterController.Move(Vector3.up * verticalFlySpeed * Time.deltaTime);
                }

                if (InputManager.Instance.GetDescendButton())
                {
                    characterController.Move(Vector3.down * verticalFlySpeed * Time.deltaTime);
                }
            }
            else
            {
                moveSpeed = 5.0f;
                verticalFlySpeed = 1.5f;
                characterController.height = 8.5f;

                if (!characterController.isGrounded)
                {
                    verticalVelocity -= gravity * Time.deltaTime;
                    characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                }
                else
                {
                    verticalVelocity = -0.5f;
                    characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                }
            }
        }

    }
}
