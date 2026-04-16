using UnityEngine;
using UnityEditor;

public class SetupAudioManager
{
    public static void Execute()
    {
        AssetDatabase.Refresh();

        // Load audio clips
        var bgClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sounds/696385__gis_sweden__minimal-tech-background-music-mtbm01.wav");
        var explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sounds/478277__joao_janz__8-bit-explosion-1_6.wav");
        var buttonClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sounds/506054__mellau__button-click-1.wav");

        if (bgClip == null) Debug.LogWarning("[Setup] BgMusic clip not found.");
        if (explosionClip == null) Debug.LogWarning("[Setup] Explosion clip not found.");
        if (buttonClip == null) Debug.LogWarning("[Setup] ButtonClick clip not found.");

        // Find GameObjects
        var audioManagerGO = GameObject.Find("AudioManager");
        if (audioManagerGO == null) { Debug.LogError("[Setup] AudioManager GameObject not found."); return; }

        var bgMusicGO     = GameObject.Find("AudioManager/BgMusic");
        var explosionGO   = GameObject.Find("AudioManager/ExplosionSFX");
        var buttonClickGO = GameObject.Find("AudioManager/ButtonClickSFX");

        // Assign clips to AudioSources
        var bgSource     = bgMusicGO?.GetComponent<AudioSource>();
        var expSource    = explosionGO?.GetComponent<AudioSource>();
        var btnSource    = buttonClickGO?.GetComponent<AudioSource>();

        if (bgSource != null)     { bgSource.clip = bgClip;         bgSource.loop = true;  bgSource.playOnAwake = false; }
        if (expSource != null)    { expSource.clip = explosionClip;  expSource.playOnAwake = false; }
        if (btnSource != null)    { btnSource.clip = buttonClip;     btnSource.playOnAwake = false; }

        // Wire AudioSources into AudioManager script
        var am = audioManagerGO.GetComponent<AudioManager>();
        if (am != null)
        {
            SerializedObject so = new SerializedObject(am);
            so.FindProperty("bgMusic").objectReferenceValue        = bgSource;
            so.FindProperty("explosionSFX").objectReferenceValue   = expSource;
            so.FindProperty("buttonClickSFX").objectReferenceValue = btnSource;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogError("[Setup] AudioManager script component not found.");
        }

        EditorUtility.SetDirty(audioManagerGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Setup] AudioManager wired up successfully.");
    }
}
