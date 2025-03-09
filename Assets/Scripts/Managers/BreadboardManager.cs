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
    private Transform leftPanel; // Added left panel
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

    // Notification method called from BreadboardController when sim mode is activated
    public void NotifySimulationStarted(GameObject bb)
    {
        isSimulationMode = true;
        currentBreadboard = bb;
        currentController = bb.GetComponent<Mirror.BreadboardController>();
        
        // Sync orientation state with controller
        isPortraitMode = currentController.isPortraitMode;
        
        // Find the panels
        leftPanel = FindLeftPanel(currentBreadboard.transform); // Find left panel
        rightPanel = FindRightPanel(currentBreadboard.transform);
        
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
        
        // Tell the controller to exit sim mode
        currentController.CmdToggleSimMode(false);
    }
    
    // Called when Orientation button is clicked
    public void ToggleOrientationMode()
    {
        if (!isSimulationMode || currentController == null) return;
        
        // Tell the controller to toggle orientation
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
            buttonText.text = isPortraitMode ? "Landscape View" : "Portrait View";
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
}
