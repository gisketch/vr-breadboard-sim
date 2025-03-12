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
        return Input.GetKey(KeyCode.Mouse0) || Input.GetKey("joystick button 0");
    }

    public bool GetPrimaryButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown("joystick button 0");
    }

    public bool GetSecondaryButton()
    {
        return Input.GetKey(KeyCode.Mouse1) || Input.GetKey("josytick button 3");
    }

    public bool GetSecondaryButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown("joystick button 3");
    }

    public bool GetAscendButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown("joystick button 2");
    }

    public bool GetDescendButtonDown()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown("joystick button 1");
    }
}
