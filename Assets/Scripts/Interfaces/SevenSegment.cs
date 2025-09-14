using System.Collections.Generic;
using UnityEngine;

public class SevenSegment : MonoBehaviour
{
    private Node nodeA;
    private Node nodeB;
    private Node nodeC;
    private Node nodeD;
    private Node nodeE;
    private Node nodeF;
    private Node nodeG;
    private Node nodeDP;
    private Node nodeGnd1;
    private Node nodeGnd2;

    [SerializeField] private GameObject segmentA;
    [SerializeField] private GameObject segmentB;
    [SerializeField] private GameObject segmentC;
    [SerializeField] private GameObject segmentD;
    [SerializeField] private GameObject segmentE;
    [SerializeField] private GameObject segmentF;
    [SerializeField] private GameObject segmentG;
    [SerializeField] private GameObject segmentDP;

    private void Awake()
    {

    }

    public void Initialize(string nodeBref, Transform reference, Dictionary<string, bool> segments)
    {
        nodeB = FindNodeRecursively(reference, nodeBref);
        nodeA = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 1, 0, 1, 30, 'A', 'J'));
        nodeGnd1 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 2, 0, 1, 30, 'A', 'J'));
        nodeF = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 3, 0, 1, 30, 'A', 'J'));
        nodeG = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 4, 0, 1, 30, 'A', 'J'));

        nodeDP = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 0, 5, 1, 30, 'A', 'J'));
        nodeC = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 1, 5, 1, 30, 'A', 'J'));
        nodeGnd2 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 2, 5, 1, 30, 'A', 'J'));
        nodeD = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 3, 5, 1, 30, 'A', 'J'));
        nodeE = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(nodeBref, 4, 5, 1, 30, 'A', 'J'));

        //Set transform
        Vector3 nodeBLocalPos = reference.InverseTransformPoint(nodeB.transform.position);

        transform.localPosition = nodeBLocalPos + new Vector3(0,0, -0.001f);

        //Update segment lights
        UpdateSegmentLights(segments);
    }

    public void UpdateSegmentLights(Dictionary<string, bool> segments)
    {
        // Check if each segment should be active and update accordingly
        if (segments.ContainsKey("A"))
            segmentA.SetActive(segments["A"]);
            
        if (segments.ContainsKey("B"))
            segmentB.SetActive(segments["B"]);
            
        if (segments.ContainsKey("C"))
            segmentC.SetActive(segments["C"]);
            
        if (segments.ContainsKey("D"))
            segmentD.SetActive(segments["D"]);
            
        if (segments.ContainsKey("E"))
            segmentE.SetActive(segments["E"]);
            
        if (segments.ContainsKey("F"))
            segmentF.SetActive(segments["F"]);
            
        if (segments.ContainsKey("G"))
            segmentG.SetActive(segments["G"]);

        if (segments.ContainsKey("DP"))
            segmentDP.SetActive(segments["DP"]);
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
