using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Movement Settings")]
    public string horizontalAxisName = "Horizontal";
    public string verticalAxisName = "Vertical";

    public string gamepadHorizontalAxisName = "MoveHorizontal";
    public string gamepadVerticalAxisName = "MoveVertical";

    private void Awake()
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

    public Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxis(horizontalAxisName);
        float vertical = Input.GetAxis(verticalAxisName);

        float gamepadHorizontal = Input.GetAxis(gamepadHorizontalAxisName);
        float gamepadVertical = Input.GetAxis(gamepadVerticalAxisName);

        Vector2 keyboardInput = new Vector2(horizontal, vertical);
        Vector2 gamepadInput = new Vector2(gamepadHorizontal, gamepadVertical);

        return keyboardInput.sqrMagnitude > gamepadInput.sqrMagnitude ? keyboardInput : gamepadInput;
    }

    public bool GetPrimaryButton()
    {
        return Input.GetKey(KeyCode.M) || Input.GetButton("Primary");
    }

    public bool GetPrimaryButtonDown()
    {
        return Input.GetKeyDown(KeyCode.M) || Input.GetButtonDown("Primary");
    }

    public bool GetSecondaryButton()
    {
        return Input.GetKey(KeyCode.Mouse1) || Input.GetButton("Secondary");
    }

    public bool GetSecondaryButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Mouse1) || Input.GetButtonDown("Secondary");
    }

    public bool GetAscendButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Q) || Input.GetButtonDown("Ascend");
    }

    public bool GetDescendButtonDown()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Descend");
    }
}
