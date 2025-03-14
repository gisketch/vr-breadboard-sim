using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum UserRole { Instructor, Student }
    private UserRole currentRole;
    public UserRole CurrentRole => currentRole;

    [Header("Skybox Settings")]
    public Material gameSkybox;

    [Header("Scene References")]
    public GameObject classroom;
    public GameObject mainMenu;
    public Transform instructorPos;
    public Transform studentPos;
    public GameObject mainCam;
    public TMP_Text interactionMessage;
    public GameObject MainCam => mainCam;

    [Header("Interaction Message Settings")]
    [SerializeField] private float messageFadeInDuration = 0.1f;
    [SerializeField] private float messageFadeOutDuration = 0.1f;
    [SerializeField] private float messageFloatDistance = 30f;
    [SerializeField] private LeanTweenType messageFadeEase = LeanTweenType.easeOutQuad;
    [SerializeField] private LeanTweenType messageFloatEase = LeanTweenType.easeOutBack;
    [SerializeField] private float messageAutoHideDelay = 0f;

    private bool isGameStarted = false;
    public bool IsGameStarted => isGameStarted;
    private Vector2 interactionMessageOriginalPos;
    private int currentFadeTweenId = -1;
    private int currentMoveTweenId = -1;
    private int messageAutoHideDelayId = -1;

    public bool isTestMode = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        classroom.SetActive(false);

        if (interactionMessage != null)
        {
            interactionMessageOriginalPos = interactionMessage.rectTransform.anchoredPosition;
            interactionMessage.alpha = 0f;
        }
        else
        {
            interactionMessage = GameObject.Find("InteractionMessage").GetComponent<TMP_Text>();
        }
    }

    public void StartInstructor()
    {
        if (isGameStarted) return;

        currentRole = UserRole.Instructor;
        InitializeGame();

        mainMenu.SetActive(false);
    }

    public void StartStudent()
    {
        if (isGameStarted) return;

        currentRole = UserRole.Student;
        InitializeGame();

        mainMenu.SetActive(false);
    }

    private void InitializeGame()
    {
        isGameStarted = true;
        mainMenu.SetActive(true);

        // Common initialization
        if (gameSkybox != null)
        {
            RenderSettings.skybox = gameSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (classroom != null)
        {
            classroom.SetActive(true);
        }

        Debug.Log($"Game Started as {currentRole}");
    }

    public bool IsInstructor => currentRole == UserRole.Instructor;
    public bool IsStudent => currentRole == UserRole.Student;

    // Interaction Message Methods
    public void SetInteractionMessage(string message)
    {
        if (interactionMessage == null) return;

        // Cancel any existing message tweens
        CancelMessageTweens();

        // Set the message text
        interactionMessage.text = message;

        // Set starting position (below the original position)
        Vector2 startPos = interactionMessageOriginalPos - new Vector2(0, messageFloatDistance);
        interactionMessage.rectTransform.anchoredPosition = startPos;
        interactionMessage.alpha = 0f;

        // Create animation for fade in
        currentFadeTweenId = LeanTween.value(gameObject, 0f, 1f, messageFadeInDuration)
            .setEase(messageFadeEase)
            .setOnUpdate((float val) =>
            {
                interactionMessage.alpha = val;
            }).id;

        // Create animation for position
        currentMoveTweenId = LeanTween.move(interactionMessage.rectTransform,
                interactionMessageOriginalPos, messageFadeInDuration)
            .setEase(messageFloatEase).id;

        // Auto-hide after delay if enabled
        if (messageAutoHideDelay > 0)
        {
            messageAutoHideDelayId = LeanTween.delayedCall(
                gameObject, messageAutoHideDelay, () =>
                {
                    ClearInteractionMessage();
                    messageAutoHideDelayId = -1;
                }).id;
        }
    }

    public void ClearInteractionMessage()
    {
        if (interactionMessage == null || string.IsNullOrEmpty(interactionMessage.text)) return;

        // Cancel any active message tweens
        CancelMessageTweens();

        // Fade out animation
        currentFadeTweenId = LeanTween.value(gameObject, interactionMessage.alpha, 0f, messageFadeOutDuration)
            .setEase(messageFadeEase)
            .setOnUpdate((float val) =>
            {
                interactionMessage.alpha = val;
            })
            .setOnComplete(() =>
            {
                interactionMessage.text = "";
            }).id;
    }

    private void CancelMessageTweens()
    {
        // Cancel active fade animation
        if (currentFadeTweenId != -1)
        {
            LeanTween.cancel(currentFadeTweenId);
            currentFadeTweenId = -1;
        }

        // Cancel active move animation
        if (currentMoveTweenId != -1)
        {
            LeanTween.cancel(currentMoveTweenId);
            currentMoveTweenId = -1;
        }

        // Cancel auto-hide delay
        if (messageAutoHideDelayId != -1)
        {
            LeanTween.cancel(messageAutoHideDelayId);
            messageAutoHideDelayId = -1;
        }
    }

    public void ResetScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
