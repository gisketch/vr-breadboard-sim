using UnityEngine;

public class WireTool : MonoBehaviour, IComponentTool
{
    [SerializeField] private GameObject wirePrefab;

    //FLOW
    [SerializeField] private Node startNode;
    [SerializeField] private Node endNode;

    private bool isPlacingWire;
    [SerializeField] private LineRenderer previewLine;
    private Camera mainCamera;
    
    private void Awake()
    {
        previewLine.positionCount = 2;
        previewLine.startWidth = 0.02f;
        previewLine.endWidth = 0.02f;
        previewLine.material = Resources.Load<Material>("Materials/WireRed");
        previewLine.enabled = false;
        
        mainCamera = Camera.main;
    }
    
    public void Activate()
    {
        gameObject.SetActive(true);
        ClearNodes();
    }

    public void UpdateColors()
    {
        switch(ComponentManager.Instance.currentColor)
        {
            case ComponentColor.Red:
                previewLine.material = Resources.Load<Material>("Materials/WireRed");
                break;
            case ComponentColor.Green:
                previewLine.material = Resources.Load<Material>("Materials/WireGreen");
                break;
            case ComponentColor.Blue:
                previewLine.material = Resources.Load<Material>("Materials/WireBlue");
                break;
        }
    }
    
    public void Deactivate()
    {
        isPlacingWire = false;
        previewLine.enabled = false;
        ClearNodes();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if(isPlacingWire && startNode != null)
        {
            // Show the preview line from start node to mouse position
            previewLine.enabled = true;
            previewLine.SetPosition(0, startNode.transform.position);
            
            
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelWirePlacement();
            }
        }
    }
    
    public void OnNodeHover(Node node)
    {
        if (node.isOccupied || node == startNode || node == endNode)
        {
            node.SetHighlightColor(Node.HighlightColor.Red);
            return;
        }

        // IF START AND END = NULL meaning wire has not been placed yet
        if (startNode == null && endNode == null)
        {
            node.SetHighlightColor(Node.HighlightColor.Green);
        }
        // START NODE PRESSED
        else if (startNode != null && endNode == null)
        {
            node.SetHighlightColor(Node.HighlightColor.Green);
            
            // Update preview end position to show where the wire would connect
            if (isPlacingWire)
            {
                previewLine.SetPosition(1, node.transform.position);
            }
        }
    }
    
    public void OnNodeClick(Node node)
    {
        if (node.isOccupied || node == startNode || node == endNode)
        {
            return;
        }

        if (startNode == null && endNode == null)
        {
            // Start wire placement
            startNode = node;
            isPlacingWire = true;
            previewLine.SetPosition(0, startNode.transform.position);
            previewLine.SetPosition(1, startNode.transform.position);
        } 
        else if (startNode != null && endNode == null)
        {
            // Don't allow connecting to the same node
            if (node == startNode)
            {
                return;
            }
            
            // Complete wire placement
            endNode = node;
            isPlacingWire = false;
            previewLine.enabled = false;

            //Update state
            BreadboardStateUtils.Instance.AddWire(startNode.name, endNode.name, ComponentManager.Instance.currentColor.ToString());

            // Reset for next wire placement
            ClearNodes();
        }
    }

    void ClearNodes()
    {
        startNode = null;
        endNode = null;
    }
    
    void CancelWirePlacement()
    {
        isPlacingWire = false;
        previewLine.enabled = false;
        ClearNodes();
    }
}
