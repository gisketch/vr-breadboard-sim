using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BreadboardManager : MonoBehaviour
{
    public static BreadboardManager Instance { get; private set; }

    private bool isSimulationMode = false;
    [SerializeField] public bool IsSimulationMode => isSimulationMode;

    private GameObject currentBreadboard;
    private Mirror.BreadboardController currentController;
    private bool isPortraitMode = true;

    [Header("UI Settings")]
    [SerializeField] private Button buttonPrefab;
    private Transform rightPanel;
    private Transform leftPanel;
    private Transform labMessages;
    private Transform experimentName;
    private Button orientationButton;
    private Button exitButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Check if instructor is spectating and should show spectated student's UI
        CheckForSpectatedBreadboardUI();
    }

    private void CheckForSpectatedBreadboardUI()
    {
        // Only check for instructors
        if (GameManager.Instance.CurrentRole != GameManager.UserRole.Instructor)
            return;

        InstructorSpectatorController spectatorController = FindObjectOfType<InstructorSpectatorController>();
        if (spectatorController == null || !spectatorController.IsSpectating)
            return;

        GameObject spectatedBreadboard = spectatorController.GetSpectatedStudentBreadboard();
        if (spectatedBreadboard == null)
            return;

        Mirror.BreadboardController spectatedController = spectatedBreadboard.GetComponent<Mirror.BreadboardController>();
        if (spectatedController == null || !spectatedController.isInSimMode)
        {
            // If spectated student is not in sim mode, hide UI
            if (isSimulationMode)
            {
                NotifySimulationExited();
            }
            return;
        }

        // If spectated student is in sim mode and we're not showing their UI yet
        if (!isSimulationMode || currentBreadboard != spectatedBreadboard)
        {
            // Exit current UI if any
            if (isSimulationMode)
            {
                NotifySimulationExited();
            }
            
            // Show spectated student's UI
            NotifySimulationStarted(spectatedBreadboard);
        }
    }

    // Notification method called from BreadboardController when sim mode is activated
    public void NotifySimulationStarted(GameObject bb)
    {
        isSimulationMode = true;
        currentBreadboard = bb;
        currentController = bb.GetComponent<Mirror.BreadboardController>();

        // Sync orientation state with controller
        isPortraitMode = currentController.isPortraitMode;

        // Find the panels
        leftPanel = FindLeftPanel(currentBreadboard.transform);
        rightPanel = FindRightPanel(currentBreadboard.transform);
        labMessages = FindLabMessages(currentBreadboard.transform);
        experimentName = FindExperimentName(currentBreadboard.transform);

        // Create UI buttons
        CreateUIButtons();
    }

    // Notification method called from BreadboardController when sim mode is deactivated
    public void NotifySimulationExited()
    {
        if (!isSimulationMode) return;

        isSimulationMode = false;
        DestroyUIButtons();
        currentBreadboard = null;
        currentController = null;
    }

    // Called when Exit button is clicked
    public void ExitSimulationMode()
    {
        if (!isSimulationMode || currentController == null) return;

        // Check if we're spectating - if so, don't exit the student's sim mode
        InstructorSpectatorController spectatorController = FindObjectOfType<InstructorSpectatorController>();
        if (spectatorController != null && spectatorController.IsSpectating)
        {
            // Just hide the UI for the instructor, don't affect the student's sim mode
            NotifySimulationExited();
            return;
        }

        // Tell the controller to exit sim mode (only if it's our own breadboard)
        currentController.CmdToggleSimMode(false);
    }

    // Called when Orientation button is clicked
    public void ToggleOrientationMode()
    {
        if (!isSimulationMode || currentController == null) return;

        // Check if we're spectating - if so, don't change the student's orientation
        InstructorSpectatorController spectatorController = FindObjectOfType<InstructorSpectatorController>();
        if (spectatorController != null && spectatorController.IsSpectating)
        {
            // Don't allow orientation changes when spectating
            return;
        }

        // Tell the controller to toggle orientation (only if it's our own breadboard)
        currentController.CmdToggleOrientation();
    }

    // Called from BreadboardController when orientation changes
    public void NotifyOrientationChanged(bool newIsPortraitMode)
    {
        isPortraitMode = newIsPortraitMode;
        UpdateOrientationButtonText();
    }

    private void UpdateOrientationButtonText()
    {
        if (orientationButton == null) return;

        TMP_Text buttonText = orientationButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isPortraitMode ? "Landscape" : "Portrait";
        }
    }

    // UI Button Management
    private void CreateUIButtons()
    {
        if (leftPanel != null)
        {
            leftPanel.gameObject.SetActive(true); // Activate left panel
        }
        else
        {
            Debug.LogWarning("LeftPanel not found.  Make sure LeftPanel gameobject is called 'LeftPanel'");
        }

        if (rightPanel == null)
        {
            Debug.LogError("Could not find RightPanel. UI buttons will not be created.");
            return;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("Button prefab not assigned. Please assign a button prefab in the inspector.");
            return;
        }

        //
        if (labMessages != null)
        {
            labMessages.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Lab Messages Transform not found");
        }

        //
        if (experimentName != null)
        {
            experimentName.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Experiment Name text not found");
        }

        // Create orientation toggle button
        orientationButton = Instantiate(buttonPrefab, rightPanel);
        TMP_Text orientationText = orientationButton.GetComponentInChildren<TMP_Text>();
        if (orientationText != null)
        {
            orientationText.text = isPortraitMode ? "Landscape View" : "Portrait View";
        }
        orientationButton.onClick.AddListener(ToggleOrientationMode);

        // Create exit button
        exitButton = Instantiate(buttonPrefab, rightPanel);
        TMP_Text exitText = exitButton.GetComponentInChildren<TMP_Text>();
        if (exitText != null)
        {
            exitText.text = "Exit";
        }
        exitButton.onClick.AddListener(ExitSimulationMode);

        // Position the buttons
        RectTransform orientationRT = orientationButton.GetComponent<RectTransform>();
        RectTransform exitRT = exitButton.GetComponent<RectTransform>();

        if (orientationRT != null && exitRT != null)
        {
            float buttonHeight = orientationRT.sizeDelta.y;
            float spacing = 10f;

            orientationRT.anchoredPosition = new Vector2(0, 0);
            exitRT.anchoredPosition = new Vector2(0, -(buttonHeight + spacing));
        }
    }

    private void DestroyUIButtons()
    {
        if (leftPanel != null)
        {
            leftPanel.gameObject.SetActive(false); // Deactivate left panel
        }

        if (labMessages != null)
        {
            labMessages.gameObject.SetActive(false);
        }

        if (experimentName != null)
        {
            experimentName.gameObject.SetActive(false);
        }

        if (orientationButton != null)
        {
            Destroy(orientationButton.gameObject);
            orientationButton = null;
        }

        if (exitButton != null)
        {
            Destroy(exitButton.gameObject);
            exitButton = null;
        }
    }

    private Transform FindLeftPanel(Transform root)
    {
        Transform leftPanel = root.Find("LeftPanel");
        if (leftPanel != null) return leftPanel;

        foreach (Transform child in root)
        {
            leftPanel = FindLeftPanelRecursive(child);
            if (leftPanel != null) return leftPanel;
        }

        Debug.LogWarning("LeftPanel not found. Creating one as a child of the breadboard.");

        // If no LeftPanel exists, create one
        GameObject panel = new GameObject("LeftPanel");
        panel.transform.SetParent(root, false);
        panel.AddComponent<RectTransform>();
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(-150, 0); // Different position
        rt.sizeDelta = new Vector2(200, 300);

        return panel.transform;
    }

    private Transform FindLeftPanelRecursive(Transform parent)
    {
        if (parent.name == "LeftPanel") return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindLeftPanelRecursive(child);
            if (found != null) return found;
        }

        return null;
    }

    private Transform FindRightPanel(Transform root)
    {
        Transform rightPanel = root.Find("RightPanel");
        if (rightPanel != null) return rightPanel;

        foreach (Transform child in root)
        {
            rightPanel = FindRightPanelRecursive(child);
            if (rightPanel != null) return rightPanel;
        }

        Debug.LogWarning("RightPanel not found. Creating one as a child of the breadboard.");

        // If no RightPanel exists, create one
        GameObject panel = new GameObject("RightPanel");
        panel.transform.SetParent(root, false);
        panel.AddComponent<RectTransform>();
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(150, 0);
        rt.sizeDelta = new Vector2(200, 300);

        return panel.transform;
    }

    private Transform FindRightPanelRecursive(Transform parent)
    {
        if (parent.name == "RightPanel") return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindRightPanelRecursive(child);
            if (found != null) return found;
        }

        return null;
    }


    private Transform FindLabMessages(Transform root)
    {
        Transform labMessages = root.Find("Lab Messages");
        if (labMessages != null) return labMessages;

        foreach (Transform child in root)
        {
            labMessages = FindLabMessagesRecursive(child);
            if (labMessages != null) return labMessages;
        }

        Debug.LogWarning("LabMessages not found. Creating one as a child of the breadboard.");

        // If no LabMessages exists, create one
        GameObject messages = new GameObject("LabMessages");
        messages.transform.SetParent(root, false);
        messages.AddComponent<RectTransform>();
        RectTransform rt = messages.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -100); // Adjust position as needed
        rt.sizeDelta = new Vector2(400, 300);

        return messages.transform;
    }

    private Transform FindLabMessagesRecursive(Transform parent)
    {
        if (parent.name == "Lab Messages") return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindLabMessagesRecursive(child);
            if (found != null) return found;
        }

        return null;
    }

    private Transform FindExperimentName(Transform root)
    {
        Transform experimentName = root.Find("Canvas/ExperimentName");
        if (experimentName != null) return experimentName;

        foreach (Transform child in root)
        {
            experimentName = FindExperimentNameRecursive(child);
            if (experimentName != null) return experimentName;
        }

        Debug.LogWarning("ExperimentName not found. Creating one as a child of the Canvas.");

        // Find or create Canvas first
        Transform canvas = root.Find("Canvas");
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(root, false);
            canvas = canvasObj.transform;
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create ExperimentName
        GameObject expName = new GameObject("ExperimentName");
        expName.transform.SetParent(canvas, false);
        expName.AddComponent<RectTransform>();
        RectTransform rt = expName.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 150); // Adjust position as needed
        rt.sizeDelta = new Vector2(400, 50);

        // Add TMP_Text component
        TMP_Text tmpText = expName.AddComponent<TMP_Text>();
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 24;
        tmpText.color = Color.white;

        return expName.transform;
    }

    private Transform FindExperimentNameRecursive(Transform parent)
    {
        if (parent.name == "ExperimentName") return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindExperimentNameRecursive(child);
            if (found != null) return found;
        }

        return null;
    }
}
