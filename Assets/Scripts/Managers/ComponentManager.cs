using UnityEngine;

public enum ComponentType { None, Wire, LED, SevenSegment, IC }
public enum ComponentColor { Red, Green, Blue }
public enum ICType { IC7448, IC74138, IC74148 }

public class ComponentManager : MonoBehaviour
{
    public static ComponentManager Instance { get; private set; }
    
    [Header("Component Tools")]
    [SerializeField] private WireTool wireTool;
    [SerializeField] private LEDTool ledTool;
    [SerializeField] private SevenSegmentTool sevenSegmentTool;
    [SerializeField] private ICTool icTool;
    // Add other tools as needed
    
    [SerializeField] private ComponentType currentComponentType = ComponentType.Wire;
    public ComponentColor currentColor = ComponentColor.Red;
    public ICType currentICType = ICType.IC7448;

    private IComponentTool currentTool;
    
    private void Awake()
    {
        Instance = this;
        DeactivateAllTools();
    }

    public void SelectColor(ComponentColor color)
    {
        currentColor = color;
        currentTool.UpdateColors();
    }
    public void SelectIC(ICType icType)
    {
        currentICType = icType;
    }

    public void SelectComponentType(ComponentType type)
    {
        // Deactivate current tool
        if (currentTool != null)
        {
            currentTool.Deactivate();
        }
        
        currentComponentType = type;
        
        // Activate the new tool
        switch (type)
        {
            case ComponentType.Wire:
                currentTool = wireTool;
                break;
            case ComponentType.LED:
                currentTool = ledTool;
                break;
            case ComponentType.SevenSegment:
                currentTool = sevenSegmentTool;
                break;
            case ComponentType.IC:
                currentTool = icTool;
                break;
            case ComponentType.None:
                currentTool = null;
                break;
            // Add other cases as needed
        }
        
        if (currentTool != null)
        {
            currentTool.Activate();
        }
    }
    
    public void OnNodeHover(Node node)
    {
        if (!BreadboardManager.Instance.IsSimulationMode) return;
        
        if (currentTool != null)
        {
            currentTool.OnNodeHover(node);
        }
    }
    
    public void OnNodeClick(Node node)
    {
        if (!BreadboardManager.Instance.IsSimulationMode) return;
        
        if (currentTool != null)
        {
            currentTool.OnNodeClick(node);
        }
    }
    
    private void DeactivateAllTools()
    {
        wireTool.Deactivate();
        //ledTool.Deactivate();
        //sevenSegmentTool.Deactivate();
        // Deactivate other tools
    }
}
