using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Movement Settings")]
    public string horizontalAxisName = "Horizontal";
    public string verticalAxisName = "Vertical";
    
    [Header("Button Mappings")]
    public KeyCode primaryActionKey = KeyCode.Mouse0;
    public KeyCode secondaryActionKey = KeyCode.Mouse1;
    public KeyCode ascendKey = KeyCode.Q;
    public KeyCode descendKey = KeyCode.E;
    
    [Header("Gamepad Button Mappings")]
    public string primaryActionButtonName = "Fire1";
    public string secondaryActionButtonName = "Fire2";
    public string ascendButtonName = "LBKABLKA";
    public string descendButtonName = "BLABLAB";

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
        
        return new Vector2(horizontal, vertical);
    }

    public bool GetPrimaryButton()
    {
        return Input.GetKey(primaryActionKey) || Input.GetButton(primaryActionButtonName);
    }
    
    public bool GetPrimaryButtonDown()
    {
        return Input.GetKeyDown(primaryActionKey) || Input.GetButtonDown(primaryActionButtonName);
    }

    public bool GetSecondaryButton()
    {
        return Input.GetKey(secondaryActionKey) || Input.GetButton(secondaryActionButtonName);
    }
    
    public bool GetSecondaryButtonDown()
    {
        return Input.GetKeyDown(secondaryActionKey) || Input.GetButtonDown(secondaryActionButtonName);
    }

    public bool GetAscendButton()
    {
        return Input.GetKey(ascendKey) || Input.GetButton(ascendButtonName);
    }

    public bool GetDescendButton()
    {
        return Input.GetKey(descendKey) || Input.GetButton(descendButtonName);
    }
}
