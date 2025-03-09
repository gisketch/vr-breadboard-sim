using UnityEngine;

public class IC : MonoBehaviour
{
    private Node pin1;
    private Node pin2;
    private Node pin3;
    private Node pin4;
    private Node pin5;
    private Node pin6;
    private Node pin7;
    private Node pin8;
    private Node pin9;
    private Node pin10;
    private Node pin11;
    private Node pin12;
    private Node pin13;
    private Node pin14;
    private Node pin15;
    private Node pin16;

    private ICType icType;

    [SerializeField] private MeshRenderer meshRenderer;

    private void Awake()
    {

    }

    public void Initialize(string pin1ref, string type, Transform reference)
    {
        if (type == "IC7448") icType = ICType.IC7448;
        if (type == "IC74138") icType = ICType.IC74138;
        if (type == "IC74148") icType = ICType.IC74148;

        UpdateMaterialType();

        pin1 = FindNodeRecursively(reference, pin1ref);
        pin2 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 1, 0, 1, 30, 'A', 'J'));
        pin3 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 2, 0, 1, 30, 'A', 'J'));
        pin4 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 3, 0, 1, 30, 'A', 'J'));
        pin5 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 4, 0, 1, 30, 'A', 'J'));
        pin6 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 5, 0, 1, 30, 'A', 'J'));
        pin7 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 6, 0, 1, 30, 'A', 'J'));
        pin8 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 7, 0, 1, 30, 'A', 'J'));

        pin9 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 7, 1, 1, 30, 'A', 'J'));
        pin10 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 6, 1, 1, 30, 'A', 'J'));
        pin11 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 5, 1, 1, 30, 'A', 'J'));
        pin12 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 4, 1, 1, 30, 'A', 'J'));
        pin13 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 3, 1, 1, 30, 'A', 'J'));
        pin14 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 2, 1, 1, 30, 'A', 'J'));
        pin15 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 1, 1, 1, 30, 'A', 'J'));
        pin16 = FindNodeRecursively(reference, BreadboardStateUtils.GetStringNameOffset(pin1ref, 0, 1, 1, 30, 'A', 'J'));

        //Set transform
        Vector3 pin1LocalPos = reference.InverseTransformPoint(pin1.transform.position);

        transform.localPosition = pin1LocalPos;
    }

    private void UpdateMaterialType()
    {
        switch (icType)
        {
            case ICType.IC7448:
                meshRenderer.material = Resources.Load<Material>("Materials/IC7448");
                break;
            case ICType.IC74138:
                meshRenderer.material = Resources.Load<Material>("Materials/IC74138");
                break;
            case ICType.IC74148:
                meshRenderer.material = Resources.Load<Material>("Materials/IC74148");
                break;
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
