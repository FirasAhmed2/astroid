using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FixTrailMaterials
{
    public static void Execute()
    {
        string matPath = "Assets/Materials/TrailPreview.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Debug.LogError($"[FixTrailMaterials] Material not found at {matPath}");
            return;
        }

        FixScene("Assets/Scenes/CustomizationScene.unity", "ShipPreview/TrailPreview", mat);
        FixScene("Assets/Scenes/SampleScene.unity", "Player", mat);
    }

    private static void FixScene(string scenePath, string goPath, Material mat)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find the TrailRenderer — either directly on the named object or a child
        var go = GameObject.Find(goPath);
        if (go == null)
        {
            // GameObject.Find doesn't support paths with '/' for children — walk manually
            string[] parts = goPath.Split('/');
            go = GameObject.Find(parts[0]);
            for (int i = 1; i < parts.Length && go != null; i++)
                go = go.transform.Find(parts[i])?.gameObject;
        }

        if (go == null)
        {
            Debug.LogError($"[FixTrailMaterials] Could not find '{goPath}' in {scenePath}");
            return;
        }

        var trail = go.GetComponentInChildren<TrailRenderer>();
        if (trail == null)
        {
            Debug.LogError($"[FixTrailMaterials] No TrailRenderer on '{goPath}' in {scenePath}");
            return;
        }

        trail.sharedMaterial = mat;
        EditorUtility.SetDirty(trail);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[FixTrailMaterials] Assigned '{mat.name}' to TrailRenderer on '{go.name}' in {scenePath}");
    }
}
