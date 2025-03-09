using UnityEngine;

public class LED : MonoBehaviour
{
    private Node anodeSlot;
    private Node cathodeSlot;

    [SerializeField] public bool isOn;

    private ComponentColor ledColor;

    [SerializeField] private MeshRenderer meshRenderer;

    private void Awake()
    {

    }

    public void Initialize(string anode, string cathode, string color, Transform reference, bool isLedOn)
    {
        if (meshRenderer == null)
        {
            Debug.LogError("LED object needs a MeshRenderer component!");
        }

        isOn = isLedOn;

        switch (color)
        {
            case "Red":
                ledColor = ComponentColor.Red;
                break;
            case "Blue":
                ledColor = ComponentColor.Blue;
                break;
            case "Green":
                ledColor = ComponentColor.Green;
                break;
            default:
                Debug.LogError("Invalid color specified for LED: " + color);
                break;
        }

        UpdateMaterial();

        anodeSlot = FindNodeRecursively(reference, anode);
        cathodeSlot = FindNodeRecursively(reference, cathode);

        if (anodeSlot == null || cathodeSlot == null)
        {
            Debug.LogError($"Could not find nodes: {(anodeSlot == null ? anode : "")} {(cathodeSlot == null ? cathode : "")}");
            return;
        }

        Vector3 anodeLocalPos = reference.InverseTransformPoint(anodeSlot.transform.position);
        Vector3 cathodeLocalPos = reference.InverseTransformPoint(cathodeSlot.transform.position);


        Vector3 midpointLocal = (anodeLocalPos + cathodeLocalPos) / 2f;

        transform.localPosition = midpointLocal + new Vector3(-0.0015f, 0, 0);
    }

    private void Update()
    {
        // If isOn changed, change to LEDRed/Green/Blue, if off, just use Off.
        UpdateMaterial();
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

    public void Remove()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
    }

    public void SetIsOn(bool value)
    {
        isOn = value;
        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        if (meshRenderer == null)
        {
            return;
        }

        if (isOn)
        {
            switch (ledColor)
            {
                case ComponentColor.Red:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDRed");
                    break;
                case ComponentColor.Green:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDGreen");
                    break;
                case ComponentColor.Blue:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDBlue");
                    break;
            }
        }
        else
        {
            switch (ledColor)
            {
                case ComponentColor.Red:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDRedOff");
                    break;
                case ComponentColor.Green:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDGreenOff");
                    break;
                case ComponentColor.Blue:
                    meshRenderer.material = Resources.Load<Material>("Materials/LEDBlueOff");
                    break;
            }
        }
    }
}
