using UnityEngine;

public class Wire : MonoBehaviour
{
    private Node startNode;
    private Node endNode;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    public void Initialize(string start, string end, string color, Transform reference)
    {
        startNode = FindNodeRecursively(reference, start);
        endNode = FindNodeRecursively(reference, end);

        if (startNode == null || endNode == null)
        {
            Debug.LogError($"Could not find nodes: {(startNode == null ? start : "")} {(endNode == null ? end : "")}");
            return;
        }

        lineRenderer.material = Resources.Load<Material>("Materials/Wire" + color);

        // Draw the wire
        lineRenderer.SetPosition(0, startNode.transform.position);
        lineRenderer.SetPosition(1, endNode.transform.position);
    }

    private void Update()
    {
        if (startNode != null && endNode != null)
        {
            // Update the line positions to follow the nodes
            lineRenderer.SetPosition(0, startNode.transform.position);
            lineRenderer.SetPosition(1, endNode.transform.position);
        }
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
        //startNode.isOccupied = false;
        //endNode.isOccupied = false;
    }
}
