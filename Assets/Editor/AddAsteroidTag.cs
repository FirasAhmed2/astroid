// Temporary editor script — adds the "Asteroid" tag to the TagManager and
// applies it to the Asteroid prefab. Run once via the MCP execute_script tool.

using UnityEditor;
using UnityEngine;

public class AddAsteroidTag
{
    public static void Execute()
    {
        const string tagName = "Asteroid";
        const string prefabPath = "Assets/Prefabs/Asteroid.prefab";

        // --- Step 1: add the tag if it isn't already there ---
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool tagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                tagExists = true;
                break;
            }
        }

        if (!tagExists)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[AddAsteroidTag] Tag '{tagName}' added to TagManager.");
        }
        else
        {
            Debug.Log($"[AddAsteroidTag] Tag '{tagName}' already exists — skipping TagManager edit.");
        }

        // --- Step 2: apply the tag to the prefab ---
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[AddAsteroidTag] Could not find prefab at '{prefabPath}'.");
            return;
        }

        if (prefab.tag == tagName)
        {
            Debug.Log($"[AddAsteroidTag] Prefab already tagged '{tagName}' — nothing to do.");
            return;
        }

        prefab.tag = tagName;
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddAsteroidTag] Prefab '{prefab.name}' tagged as '{tagName}'.");
    }
}
