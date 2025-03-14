using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DoorQuit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private bool isHovering = false;

    void Update()
    {

        if (isHovering)
        {
            if (InputManager.Instance.GetPrimaryButton())
            {
                GameManager.Instance.ResetScene();
            }
        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        GameManager.Instance.SetInteractionMessage("Exit");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        GameManager.Instance.ClearInteractionMessage();
    }
}
