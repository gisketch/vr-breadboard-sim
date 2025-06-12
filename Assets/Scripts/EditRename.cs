using UnityEngine;
using UnityEditor;

public class EditRename : EditorWindow
{
    private GameObject targetParent;

    [MenuItem("Tools/Rename Children A-E Groups")]
    public static void ShowWindow()
    {
        GetWindow<EditRename>("Rename Children");
    }

    private void OnGUI()
    {
        GUILayout.Label("Rename Child Objects (Groups A-E)", EditorStyles.boldLabel);

        targetParent = (GameObject)EditorGUILayout.ObjectField("Parent Object", targetParent, typeof(GameObject), true);

        if (GUILayout.Button("Rename Children"))
        {
            if (targetParent != null)
                RenameChildren();
            else
                EditorUtility.DisplayDialog("Error", "Please select a parent object first.", "OK");
        }
    }

    private void RenameChildren()
    {
        Transform parent = targetParent.transform;
        int childCount = parent.childCount;
        int requiredChildren = 150; // 5 groups (A-E) of 30 children each

        if (childCount < requiredChildren)
        {
            EditorUtility.DisplayDialog("Error", $"Parent only has {childCount} children. It needs at least {requiredChildren}.", "OK");
            return;
        }

        Undo.RecordObject(targetParent, "Rename Children");

        // Define the group letters
        char[] groupLetters = { 'F', 'G', 'H', 'I', 'J' };

        // Rename children in each group
        for (int groupIndex = 0; groupIndex < groupLetters.Length; groupIndex++)
        {
            char groupLetter = groupLetters[groupIndex];
            int startIndex = groupIndex * 30;

            // Rename 30 children for current group
            for (int i = 0; i < 30; i++)
            {
                int childIndex = startIndex + i;
                Transform child = parent.GetChild(childIndex);
                string newName = $"{i + 1}{groupLetter}";
                Undo.RecordObject(child.gameObject, "Rename Child");
                child.name = newName;
            }
        }

        EditorUtility.DisplayDialog("Success", "Renamed 150 children successfully!", "OK");
    }
}