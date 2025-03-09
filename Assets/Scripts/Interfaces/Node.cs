using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;

public class Node : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public bool isOccupied = false;
    public string nodeId;

    private bool isHovering = false;
    public bool overrideHighlight = false;

    public enum HighlightColor { Default, Red, Yellow, Green, Blue }
    
    // Reference to any component connected to this node
    private IComponent connectedComponent;

    private void Awake()
    {
        nodeId = gameObject.name; 
    }

    private void Start()
    {
        BoxCollider col = GetComponent<BoxCollider>();

        col.size = new Vector3(2f, 2f, 1f);

    }

    void Update()
    {
        if (!isHovering) return;
        if (InputManager.Instance.GetSecondaryButtonDown())
        {
            if(isOccupied) BreadboardStateUtils.Instance.RemoveComponentWithNode(gameObject.name);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        ComponentManager.Instance.OnNodeHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (overrideHighlight) return;
        SetHighlightColor(HighlightColor.Default);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        ComponentManager.Instance.OnNodeClick(this);
    }
    
    public void SetHighlightColor(HighlightColor color)
    {
        string materialName = color.ToString();
        Material highlightMaterial = Resources.Load<Material>($"Materials/{materialName}");
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlightMaterial != null)
            {
                renderer.material = highlightMaterial;
            }
            else
            {
                Debug.LogWarning($"Material '{materialName}' not found in Resources/Materials folder.");
            }
        }
        else
        {
            Debug.LogWarning("No Renderer component found on this Node GameObject.");
        }
    }

    public void Occupy()
    {
        isOccupied = true;
    }

    public void ClearOccupancy()
    {
        isOccupied = false;
    }

    // Grabs a node from the same breadboard
    public Node GetNodeFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Node name cannot be null or empty.");
            return null; // Or throw an exception, depending on your error handling.
        }

        if (name.Contains("GND") || name.Contains("PWR"))
        {
            // Extract the numeric part from the name using regex
            Match match = Regex.Match(name, @"(\d+)");
            if (match.Success)
            {
                int railNumber;
                if (int.TryParse(match.Groups[1].Value, out railNumber))
                {
                    if (railNumber >= 1 && railNumber <= 30)
                    {
                        Transform powerRailLeft = transform.parent.parent.Find("PowerRailLeft");
                        if (powerRailLeft != null && powerRailLeft.Find(name) != null){
                            return powerRailLeft.Find(name).GetComponent<Node>();
                        } else {
                            Debug.LogError("PowerRailLeft: " + name + " Node not found: " + name + " on PowerRailLeft. Check Spelling.");
                            return null;
                        }


                    }
                    else if (railNumber >= 31 && railNumber <= 60)
                    {
                        Transform powerRailRight = transform.parent.parent.Find("PowerRailRight");
                        if (powerRailRight != null && powerRailRight.Find(name) != null){
                            return powerRailRight.Find(name).GetComponent<Node>();
                        } else {
                            Debug.LogError("PowerRailRight: " + name + " Node not found: " + name + " on PowerRailRight. Check Spelling.");
                            return null;
                        }
                    }
                    else
                    {
                        Debug.LogError("Power rail number (" + railNumber + ") is out of valid range (1-60). Name: " + name);
                        return null;
                    }
                }
                else
                {
                    Debug.LogError("Could not parse rail number from name: " + name);
                    return null;
                }
            }
            else
            {
                Debug.LogError("Invalid power rail name format.  Must include a number before GND/PWR.  Name: " + name);
                return null;
            }
        }
        else
        {
            // Standard node name: 1A, 2B, etc.  Check last character.

            char lastChar = name[name.Length - 1];
            Match match = Regex.Match(name, @"(\d+)");
            if (match.Success){
                int nodeNumber;
                if (int.TryParse(match.Groups[1].Value, out nodeNumber))
                {
                     if (nodeNumber >= 1 && nodeNumber <= 30)
                     {
                        if (lastChar >= 'A' && lastChar <= 'E')
                        {
                            Transform nodesLeft = transform.parent.parent.Find("NodesLeft");
                            if (nodesLeft != null && nodesLeft.Find(name) != null){
                                return nodesLeft.Find(name).GetComponent<Node>();
                            }
                            else {
                                Debug.LogError("NodesLeft: Node not found: " + name + " on NodesLeft. Check Spelling: Ensure caps lock of last char and name.");
                                return null;

                            }

                        }
                        else if (lastChar >= 'F' && lastChar <= 'J')
                        {
                            Transform nodesRight = transform.parent.parent.Find("NodesRight");
                            if (nodesRight != null && nodesRight.Find(name) != null){
                                return nodesRight.Find(name).GetComponent<Node>();
                            } else {
                                Debug.LogError("NodesRight: Node not found: " + name + " on NodesRight. Check Spelling: Ensure caps lock of last char and name.");
                                return null;
                            }
                        }
                        else
                        {
                            Debug.LogError("Last character: " + lastChar + " is not a valid row letter (A-E or F-J). Name: " + name);
                            return null;
                        }

                    } else {
                         Debug.LogError("Node Number: " + nodeNumber + " is not inside of range between 1-30. Check Spelling: Ensure caps lock of last char and name.");
                         return null;
                    }

                } else {
                           Debug.LogError("Could not parse Node number from name: " + name);
                            return null;
                }

            } else {
                  Debug.LogError("Invalid node name format.  Must include a number and LETTER.  Name: " + name);
                return null;

            }

        }
    }
}
