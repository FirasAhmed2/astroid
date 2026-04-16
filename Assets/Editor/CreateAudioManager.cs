// Editor-only — creates the _AudioManager GameObject in the open scene
// with three child AudioSource GameObjects wired to the AudioManager component.
// Run once via the CoPlay MCP execute_script tool.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CreateAudioManager
{
    public static void Execute()
    {
        // Don't create a second one if it already exists
        AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Debug.Log("[CreateAudioManager] _AudioManager already exists in the scene — skipping.");
            return;
        }

        // Root GameObject that holds the component and persists across scenes
        GameObject root = new GameObject("_AudioManager");
        AudioManager manager = root.AddComponent<AudioManager>();

        // Three child GameObjects each with their own AudioSource so volume,
        // pitch, and clip can be configured independently in the Inspector
        AudioSource bgSource        = CreateAudioChild(root, "BGMusic");
        AudioSource explosionSource = CreateAudioChild(root, "ExplosionSFX");
        AudioSource buttonSource    = CreateAudioChild(root, "ButtonClickSFX");

        // Configure the bg music source for looping right here so we don't
        // rely on AudioManager.StartBgMusic() to set loop before it's used
        bgSource.loop       = true;
        bgSource.playOnAwake = false; // AudioManager.StartBgMusic() calls Play() explicitly

        // One-shots don't need loop or playOnAwake
        explosionSource.loop       = false;
        explosionSource.playOnAwake = false;
        buttonSource.loop           = false;
        buttonSource.playOnAwake    = false;

        // Wire the three sources into the AudioManager serialized fields
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("bgMusic").objectReferenceValue        = bgSource;
        so.FindProperty("explosionSFX").objectReferenceValue   = explosionSource;
        so.FindProperty("buttonClickSFX").objectReferenceValue = buttonSource;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(root);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[CreateAudioManager] Done — _AudioManager created and scene saved. Assign AudioClips in the Inspector.");
    }

    static AudioSource CreateAudioChild(GameObject parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child.AddComponent<AudioSource>();
    }
}
