using UnityEngine;
using UnityEditor;

public class FixCameraClipPlanes
{
    public static string Execute()
    {
        var camGO = GameObject.Find("Main Camera");
        if (camGO == null) return "ERROR: Main Camera not found";

        var cam = camGO.GetComponent<Camera>();
        if (cam == null) return "ERROR: Camera component not found";

        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        EditorUtility.SetDirty(cam);

        return $"Camera fixed: near={cam.nearClipPlane} far={cam.farClipPlane}";
    }
}
