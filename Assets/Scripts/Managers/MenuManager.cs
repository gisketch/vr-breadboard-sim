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

        [Header("Custom Events")]
        public UnityEvent onCreateLab;
        public UnityEvent onJoinLab;
        public UnityEvent onSettings;

        private Stack<MenuState> navigationStack = new Stack<MenuState>();
        private GameObject currentJoinMenu;
        private TMP_Text ipInputField;
        private Button joinButton;
        private bool isJoinMenuActive = false;

        private enum MenuState
        {
            Main,
            RoleSelect,
            Instructor,
            Student,
            JoinLab
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
                    CreateButton("Instructor", () => NavigateTo(MenuState.Instructor));
                    CreateButton("Student", () => NavigateTo(MenuState.Student));
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
