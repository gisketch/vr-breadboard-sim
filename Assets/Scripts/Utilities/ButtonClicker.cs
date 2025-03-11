using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonClicker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private bool isHovering;

    void Start()
    {
        button = GetComponent<Button>();
    }

    void Update()
    {
        if (isHovering && InputManager.Instance.GetPrimaryButtonDown())
        {
            button.onClick.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

}
