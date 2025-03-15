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

        [Header("Breadboard")]
        public GameObject breadboardPrefab;
        private BreadboardController myBreadboard;

        public GameObject playerModel;
        public Transform neckTransform;

        [HideInInspector]
        public bool isFlyMode = false;

        private CharacterController characterController;
        private Camera mainCamera;
        private Vector3 currentVelocity = Vector3.zero;
        private float verticalVelocity = 0f;
        private int localPlayerLayerIndex;

        // Toggle state variables
        private bool isAscending = false;
        private bool isDescending = false;

        [SyncVar(hook = nameof(OnPlayerNameChange))]
        public string playerName = "Player";
        [SerializeField] private TMP_Text playerText;

        public int id = -1;

        private void Awake()
        {
            localPlayerLayerIndex = LayerMask.NameToLayer(localPlayerLayer);

            if (localPlayerLayerIndex == -1)
            {
                Debug.LogError("Layer '" + localPlayerLayer + "' does not exist. Please create this layer in the Unity Editor.");
            }
        }


        void OnPlayerNameChange(string oldName, string newName)
        {
            playerText.text = newName;
            if (newName == "Instructor") transform.Find("Host").gameObject.SetActive(true);
        }

        public int ReturnId()
        {
            if (hasAuthority)
            {
                return id;
            }
            else
            {
                return -1;
            }
        }


        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = transform.GetComponentInChildren<Camera>();

            if (hasAuthority)
            {
                ClassroomManager cm = GameObject.Find("chalkboard").GetComponent<ClassroomManager>();

                if (GameManager.Instance.CurrentRole == GameManager.UserRole.Instructor)
                {
                    CmdUpdateName("Instructor");
                }
                else
                {
                    CmdUpdateName($"Student {cm.currentStudentId}");
                    id = cm.currentStudentId;
                    cm.CmdIncrementStudentId();
                }
            }

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

        [Command]
        public void CmdUpdateName(string name)
        {
            playerName = name;
            playerText.text = playerName;
        }

        [Client]
        private void Update()
        {
            if (!hasAuthority) { return; }

            if (Input.GetKeyDown(KeyCode.L))
            {
            }

            HandleMovement();
            HandleVerticalMovement();
            HandleRotation();
            HandleVerticalToggle();

            if (!isFlyMode && BreadboardManager.Instance.IsSimulationMode)
            {
                isFlyMode = true;
                // Reset vertical movement when entering fly mode
                isAscending = false;
                isDescending = false;
                GameManager.Instance.ClearInteractionMessage();
            }

            if (isFlyMode && !BreadboardManager.Instance.IsSimulationMode)
            {
                isFlyMode = false;
                // Reset vertical movement when exiting fly mode
                isAscending = false;
                isDescending = false;
                GameManager.Instance.ClearInteractionMessage();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Teleport")
            {
                //Go back to spawn
                Transform spawnToGoBack = GameObject.Find("InstructorPos").transform;
                characterController.enabled = false;
                transform.position = spawnToGoBack.position;
                characterController.enabled = true;
            }
        }

        private void HandleVerticalToggle()
        {
            if (!isFlyMode) return;

            // Toggle descending state when the descend button is pressed
            if (InputManager.Instance.GetDescendButtonDown())
            {
                if (isAscending)
                {
                    // If currently ascending, stop ascending first
                    isAscending = false;
                    GameManager.Instance.ClearInteractionMessage();
                }

                isDescending = !isDescending;

                // Update the interaction message
                if (isDescending)
                {
                    GameManager.Instance.SetInteractionMessage("Descending. Press D to stop.");
                }
                else
                {
                    GameManager.Instance.ClearInteractionMessage();
                }
            }
            // Toggle ascending state when the ascend button is pressed
            if (InputManager.Instance.GetAscendButtonDown() || Input.GetKeyDown(KeyCode.C))
            {
                if (isDescending)
                {
                    // If currently descending, stop descending first
                    isDescending = false;
                    GameManager.Instance.ClearInteractionMessage();
                }

                isAscending = !isAscending;

                // Update the interaction message
                if (isAscending)
                {
                    GameManager.Instance.SetInteractionMessage("Ascending. Press C to stop.");
                }
                else
                {
                    GameManager.Instance.ClearInteractionMessage();
                }
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

                // Apply vertical movement based on toggle state
                if (isAscending)
                {
                    characterController.Move(Vector3.up * verticalFlySpeed * Time.deltaTime);
                }
                else if (isDescending) // Changed to else if to ensure mutual exclusivity
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

        void OnDestroy()
        {
            Debug.Log($"{playerName} quits the server.");

            int ownId = int.Parse(playerName[playerName.Length - 1].ToString());

            if (ownId > 0)
            {
                Debug.Log("Finding Classroom");
                ClassroomManager scoreManager = GameObject.Find("chalkboard").GetComponent<ClassroomManager>();
                if (scoreManager != null)
                {
                    Debug.Log("Removing Myself from the Classroom");
                    scoreManager.CmdRemoveStudent(ownId);
                }
            }

        }

    }
}
