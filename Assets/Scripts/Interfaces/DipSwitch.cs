using UnityEngine;
using UnityEngine.EventSystems;

public class DipSwitch : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler

{
    private Node pin1;
    private Node pin2;

    private bool isOn;

    [SerializeField] private GameObject onObj;
    [SerializeField] private GameObject offObj;
    [SerializeField] private MeshRenderer meshRenderer;

    private bool isHovering;

    private void Awake()
    {

    }

    public void Initialize(string pin1ref, bool state, Transform reference)
    {
        UpdateState(state);

        pin1 = FindNodeRecursively(reference, pin1ref);
        pin2 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 0, 1, 1, 30, 'A', 'J'));

        meshRenderer.material = Resources.Load<Material>("Materials/DipSwitch");

        //Set transform
        Vector3 pin1LocalPos = reference.InverseTransformPoint(pin1.transform.position);

        transform.localPosition = pin1LocalPos;
    }

    private void ToggleState()
    {
        // Find this switch in the breadboard components and toggle it
            foreach (var kvp in BreadboardStateUtils.Instance.myBreadboardController.breadboardComponents)
            {
                if (kvp.Value.type == "dipSwitch" && 
                    kvp.Value.pin1 == pin1.name && 
                    kvp.Value.pin2 == pin2.name)
                {
                    // Create updated component with toggled state
                    BreadboardComponentData updatedData = kvp.Value;
                    updatedData.isOn = !updatedData.isOn;
                    
                    // Update the component
                    BreadboardStateUtils.Instance.myBreadboardController.CmdAddComponent(kvp.Key, updatedData);
                    break;
                }
            }
    }

    private void UpdateState(bool state)
    {
        isOn = state;
        onObj.SetActive(isOn);
        offObj.SetActive(!isOn);
    }

    private Node FindNodeRecursively(Transform parent, string nodeName)
    {
        // First check if the direct child has this name
        Transform child = parent.Find(nodeName);
        if (child != null)
            return child.GetComponent<Node>();

        // If not found, search through all children recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            Node foundNode = FindNodeRecursively(parent.GetChild(i), nodeName);
            if (foundNode != null)
                return foundNode;
        }

        return null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        meshRenderer.material = Resources.Load<Material>("Materials/DipSwitchHighlight");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        meshRenderer.material = Resources.Load<Material>("Materials/DipSwitch");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHovering)
        {
            ToggleState();
        }
    }


    private void Update()
    {
    }

    public void Remove()
    {
    }

    private void OnDestroy()
    {
    }

}
