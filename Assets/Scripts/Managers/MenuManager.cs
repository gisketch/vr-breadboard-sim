using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Mirror
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private NetworkManagerBreadboard networkManager = null;

        [Header("UI References")]
        public TMP_Text headerText;
        public Transform buttonsContainer;
        public GameObject buttonPrefab;
        public GameObject joinMenuPrefab;
        public GameObject tutorialPrefab;

        [Header("Tutorial Content")]
        public Sprite instructorTutorialImage;
        public Sprite studentTutorialImage;
        
        [TextArea(10, 15)]
        public string instructorInstructions = "<b><size=32>Instructor Controls</size></b>\n\n\n\n<b><size=24>Spectator Mode:</size></b>\n\n• <b>Left Mouse Button</b> - Switch to next student\n\n• <b>Right Mouse Button</b> - Switch to previous student";
        
        [TextArea(10, 15)]
        public string studentInstructions = "<b><size=32>Student Mode Controls</size></b>\n\n\n\n<b><size=24>Movement & Navigation:</size></b>\n\n• <b>Joystick</b> - Move around the environment\n\n• <b>Head Movement</b> - Look around and navigate in VR space\n\n• <b>A Button</b> - Interact with objects and interface elements\n\n\n\n<b><size=24>Additional Controls:</size></b>\n\n• <b>Bumper</b> - [Function to be defined]\n\n• <b>Trigger</b> - [Function to be defined]\n\n• <b>B Button</b> - [Function to be defined]\n\n\n\n<b><size=32>Breadboard Mode Controls</size></b>\n\n\n\n<b><size=24>Movement & Navigation:</size></b>\n\n• <b>Joystick</b> - Move around the environment\n\n• <b>Head Movement</b> - Look around and navigate in VR space\n\n\n\n<b><size=24>Interaction:</size></b>\n\n• <b>A Button</b> - Interact with buttons and place components\n\n• <b>B Button</b> - Cancel actions or remove components\n\n• <b>C Button</b> - Toggle ascent\n\n• <b>D Button</b> - Toggle descent";

        [Header("Custom Events")]
        public UnityEvent onCreateLab;
        public UnityEvent onJoinLab;
        public UnityEvent onSettings;

        private Stack<MenuState> navigationStack = new Stack<MenuState>();
        private GameObject currentJoinMenu;
        private GameObject currentTutorialMenu;
        private TMP_Text ipInputField;
        private Button joinButton;
        private bool isJoinMenuActive = false;

        private enum MenuState
        {
            Main,
            RoleSelect,
            Instructor,
            Student,
            JoinLab,
            InstructorTutorial,
            StudentTutorial
        }

        void Start()
        {
            navigationStack.Push(MenuState.Main);
            networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerBreadboard>();
            UpdateUI(MenuState.Main);
        }

        private void ClearButtons()
        {
            foreach (Transform child in buttonsContainer)
            {
                Destroy(child.gameObject);
            }

            if (isJoinMenuActive)
            {
                CleanupJoinMenu();
            }
            
            if (currentTutorialMenu != null)
            {
                Destroy(currentTutorialMenu);
                currentTutorialMenu = null;
            }
        }

        private void CleanupJoinMenu()
        {
            if (isJoinMenuActive)
            {
                NetworkManagerBreadboard.OnClientConnected -= HandleClientConnected;
                NetworkManagerBreadboard.OnClientDisconnected -= HandleClientDisconnected;
                isJoinMenuActive = false;
                Debug.Log("Join menu cleaned up - Unsubscribed from network events");
            }
        }

        private void CreateButton(string label, UnityAction action)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonsContainer);
            newButton.GetComponentInChildren<TMP_Text>().text = label;
            newButton.GetComponent<Button>().onClick.AddListener(action);
        }

        private void UpdateUI(MenuState state)
        {
            ClearButtons();
            switch (state)
            {
                case MenuState.Main:
                    headerText.text = "Laboratory Experiment Simulator";
                    CreateButton("Start", () => NavigateTo(MenuState.RoleSelect));
                    break;

                case MenuState.RoleSelect:
                    headerText.text = "Role";
                    CreateButton("Instructor", () => NavigateTo(MenuState.InstructorTutorial));
                    CreateButton("Student", () => NavigateTo(MenuState.StudentTutorial));
                    CreateButton("Back", GoBack);
                    break;

                case MenuState.InstructorTutorial:
                    headerText.text = "Instructor Tutorial";
                    SetupTutorialMenu(instructorTutorialImage, instructorInstructions, () => NavigateTo(MenuState.Instructor));
                    CreateButton("Back", GoBack);
                    break;

                case MenuState.StudentTutorial:
                    headerText.text = "Student Tutorial";
                    SetupTutorialMenu(studentTutorialImage, studentInstructions, () => NavigateTo(MenuState.Student));
                    CreateButton("Back", GoBack);
                    break;

                case MenuState.Instructor:
                    headerText.text = "Logic Circuits and Switching Theory Experiment 6";
                    CreateButton("Create Lab", () => onCreateLab.Invoke());
                    CreateButton("Back", GoBack);
                    break;

                case MenuState.Student:
                    headerText.text = "Logic Circuits and Switching Theory Experiment 6";
                    CreateButton("Join Lab", () => NavigateTo(MenuState.JoinLab));
                    CreateButton("Back", GoBack);
                    break;

                case MenuState.JoinLab:
                    headerText.text = "Enter Lab IP";
                    SetupJoinLabMenu();
                    break;
            }
        }

        private void SetupTutorialMenu(Sprite tutorialImage, string instructions, UnityAction startAction)
        {
            // Instantiate the tutorial prefab
            currentTutorialMenu = Instantiate(tutorialPrefab, buttonsContainer);

            // Set up the image - looking in Container -> TutorialImage
            Transform container = currentTutorialMenu.transform.Find("Container");
            if (container != null)
            {
                Image imageComponent = container.Find("TutorialImage").GetComponent<Image>();
                if (imageComponent != null && tutorialImage != null)
                {
                    imageComponent.sprite = tutorialImage;
                }
                else
                {
                    Debug.LogWarning("TutorialImage component not found in Container or tutorial image is null");
                }

                // Set up the instruction text - looking in Container -> InstructionText
                TMP_Text instructionText = container.Find("InstructionText").GetComponent<TMP_Text>();
                if (instructionText != null)
                {
                    // Force enable all rich text settings
                    instructionText.richText = true;
                    instructionText.parseCtrlCharacters = true;
                    instructionText.enableWordWrapping = true;
                    instructionText.overflowMode = TextOverflowModes.Overflow;
                    
                    // Force the text to update
                    instructionText.text = instructions;
                    instructionText.ForceMeshUpdate();
                    
                    Debug.Log("Rich text enabled: " + instructionText.richText);
                    Debug.Log("Text content: " + instructionText.text);
                }
                else
                {
                    Debug.LogError("InstructionText component not found in Container");
                }
            }
            else
            {
                Debug.LogError("Container not found in tutorial prefab");
            }

            // Create the START button
            CreateButton("START", startAction);
        }

        private void SetupJoinLabMenu()
        {
            // Subscribe to network events
            NetworkManagerBreadboard.OnClientConnected += HandleClientConnected;
            NetworkManagerBreadboard.OnClientDisconnected += HandleClientDisconnected;
            isJoinMenuActive = true;

            // Instantiate the JoinMenu prefab
            currentJoinMenu = Instantiate(joinMenuPrefab, buttonsContainer);

            // Get reference to the IP input field
            ipInputField = currentJoinMenu.transform.Find("IPinput").GetComponent<TMP_Text>();
            if (ipInputField == null)
            {
                Debug.LogError("IPinput TMP_Text component not found in JoinMenu prefab");
                return;
            }

            // Clear the initial text
            ipInputField.text = "";

            // Setup buttons in Row123
            SetupNumpadRow("Row123", 1);

            // Setup buttons in Row456
            SetupNumpadRow("Row456", 4);

            // Setup buttons in Row789
            SetupNumpadRow("Row789", 7);

            // Setup buttons in Row0
            Transform row0 = currentJoinMenu.transform.Find("Row0");
            if (row0 != null)
            {
                // Delete button
                Button deleteBtn = row0.GetChild(0).GetComponent<Button>();
                if (deleteBtn != null)
                {
                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(DeleteLastCharacter);
                }

                // 0 button
                Button zeroBtn = row0.GetChild(1).GetComponent<Button>();
                if (zeroBtn != null)
                {
                    zeroBtn.onClick.RemoveAllListeners();
                    zeroBtn.onClick.AddListener(() => AddCharacter("0"));
                }

                // Dot button
                Button dotBtn = row0.GetChild(2).GetComponent<Button>();
                if (dotBtn != null)
                {
                    dotBtn.onClick.RemoveAllListeners();
                    dotBtn.onClick.AddListener(() => AddCharacter("."));
                }
            }

            // Setup buttons in RowBackJoin
            Transform rowBackJoin = currentJoinMenu.transform.Find("RowBackJoin");
            if (rowBackJoin != null)
            {
                // Back button
                Button backBtn = rowBackJoin.GetChild(0).GetComponent<Button>();
                if (backBtn != null)
                {
                    backBtn.onClick.RemoveAllListeners();
                    backBtn.onClick.AddListener(GoBack);
                }

                // Join button
                Button joinBtn = rowBackJoin.GetChild(1).GetComponent<Button>();
                if (joinBtn != null)
                {
                    joinBtn.onClick.RemoveAllListeners();
                    joinBtn.onClick.AddListener(JoinLabSession);
                    joinButton = joinBtn;
                }
            }

            Debug.Log("Join menu setup - Subscribed to network events");
        }

        private void SetupNumpadRow(string rowName, int startNumber)
        {
            Transform row = currentJoinMenu.transform.Find(rowName);
            if (row != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (row.childCount > i)
                    {
                        Button btn = row.GetChild(i).GetComponent<Button>();
                        if (btn != null)
                        {
                            int number = startNumber + i;
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(() => AddCharacter(number.ToString()));
                        }
                    }
                }
            }
        }

        private void AddCharacter(string character)
        {
            if (ipInputField != null)
            {
                ipInputField.text += character;
            }
        }

        private void DeleteLastCharacter()
        {
            if (ipInputField != null && ipInputField.text.Length > 0)
            {
                ipInputField.text = ipInputField.text.Substring(0, ipInputField.text.Length - 1);
            }
        }

        private void JoinLabSession()
        {
            if (ipInputField != null)
            {
                string ipAddress = ipInputField.text;
                Debug.Log("Joining lab with IP: " + ipAddress);

                if (networkManager != null)
                {
                    joinButton.interactable = false;
                    networkManager.networkAddress = ipInputField.text == "" ? "localhost" : ipAddress;
                    networkManager.StartClient();
                }
            }
        }

        private void NavigateTo(MenuState newState)
        {
            // If we're leaving the join lab state, clean up
            if (navigationStack.Count > 0 && navigationStack.Peek() == MenuState.JoinLab)
            {
                CleanupJoinMenu();
            }

            navigationStack.Push(newState);
            UpdateUI(newState);
        }

        public void GoBack()
        {
            if (navigationStack.Count <= 1) return;

            if (navigationStack.Peek() == MenuState.JoinLab)
            {
                CleanupJoinMenu();
            }

            navigationStack.Pop();
            UpdateUI(navigationStack.Peek());
        }

        public void StartInstructor()
        {
            networkManager.StartHost();
            GameManager.Instance.StartInstructor();
        }

        public void StartStudent()
        {
            GameManager.Instance.StartStudent();
        }

        private void HandleClientConnected()
        {
            joinButton.interactable = true;
            Debug.Log("Client connected to server!");
            onJoinLab?.Invoke();
        }

        private void HandleClientDisconnected()
        {
            joinButton.interactable = true;
            Debug.Log("Client disconnected from server!");
        }

        private void OnDestroy()
        {
            CleanupJoinMenu();
        }
    }
}
