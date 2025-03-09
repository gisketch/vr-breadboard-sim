using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentUIHandlers : MonoBehaviour
{   

    public void IC7488Click()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.IC);
        ComponentManager.Instance.SelectIC(ICType.IC7448);
    }
    public void IC74138Click()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.IC);
        ComponentManager.Instance.SelectIC(ICType.IC74138);
    }
    public void IC74148Click()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.IC);
        ComponentManager.Instance.SelectIC(ICType.IC74148);
    }

    public void SevenSegmentClick()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.SevenSegment);
    }

    public void WireClickRed()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.Wire);
        ComponentManager.Instance.SelectColor(ComponentColor.Red);
    }
    public void WireClickGreen()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.Wire);
        ComponentManager.Instance.SelectColor(ComponentColor.Green);
    }
    public void WireClickBlue()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.Wire);
        ComponentManager.Instance.SelectColor(ComponentColor.Blue);
    }

    public void LEDClickRed()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.LED);
        ComponentManager.Instance.SelectColor(ComponentColor.Red);
    }
    public void LEDClickGreen()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.LED);
        ComponentManager.Instance.SelectColor(ComponentColor.Green);
    }
    public void LEDClickBlue()
    {
        ComponentManager.Instance.SelectComponentType(ComponentType.LED);
        ComponentManager.Instance.SelectColor(ComponentColor.Blue);
    }

}
