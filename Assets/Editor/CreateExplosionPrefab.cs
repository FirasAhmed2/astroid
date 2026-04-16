// Editor-only script — creates ExplosionPrefab with 7 child ParticleSystems
// and wires it to the ParticleSpawner in the open scene.
// Run once via the CoPlay MCP execute_script tool, then delete if you like.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CreateExplosionPrefab
{
    public static void Execute()
    {
        // ----------------------------------------------------------------
        // 1. Load sprites
        // ----------------------------------------------------------------
        string spriteRoot = "Assets/Sprites/explosionVFX/";

        Sprite flashSprite     = LoadSprite(spriteRoot + "explosion_flash.svg");
        Sprite fireballSprite  = LoadSprite(spriteRoot + "explosion_fireball.svg");
        Sprite shockwaveSprite = LoadSprite(spriteRoot + "explosion_shockwave.svg");
        Sprite emberSprite     = LoadSprite(spriteRoot + "explosion_ember.svg");
        Sprite sparksSprite    = LoadSprite(spriteRoot + "explosion_sparks.svg");
        Sprite debrisSprite    = LoadSprite(spriteRoot + "explosion_debris.svg");
        Sprite smokeSprite     = LoadSprite(spriteRoot + "explosion_smoke.svg");

        // ----------------------------------------------------------------
        // 2. Build root GameObject
        // ----------------------------------------------------------------
        GameObject root = new GameObject("ExplosionPrefab");

        // ----------------------------------------------------------------
        // 3. Create all 7 child particle systems
        // ----------------------------------------------------------------
        CreatePS_Flash    (root, flashSprite);
        CreatePS_Fireball (root, fireballSprite);
        CreatePS_Shockwave(root, shockwaveSprite);
        CreatePS_Embers   (root, emberSprite);
        CreatePS_Sparks   (root, sparksSprite);
        CreatePS_Debris   (root, debrisSprite);
        CreatePS_Smoke    (root, smokeSprite);

        // ----------------------------------------------------------------
        // 4. Save as prefab
        // ----------------------------------------------------------------
        string prefabPath = "Assets/Prefabs/ExplosionPrefab.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (savedPrefab == null)
        {
            Debug.LogError("[CreateExplosionPrefab] Failed to save prefab at " + prefabPath);
            return;
        }

        Debug.Log("[CreateExplosionPrefab] Prefab saved to " + prefabPath);

        // ----------------------------------------------------------------
        // 5. Wire prefab to ParticleSpawner in the open scene
        // Create the GameObject if it doesn't exist yet
        // ----------------------------------------------------------------
        ParticleSpawner spawner = Object.FindFirstObjectByType<ParticleSpawner>();
        if (spawner == null)
        {
            Debug.Log("[CreateExplosionPrefab] No ParticleSpawner found — creating '_ParticleSpawner' GameObject.");
            GameObject spawnerGO = new GameObject("_ParticleSpawner");
            spawner = spawnerGO.AddComponent<ParticleSpawner>();
        }

        ParticleSystem prefabPS = savedPrefab.GetComponent<ParticleSystem>();
        if (prefabPS == null)
        {
            // Root has no PS itself — that's fine; ParticleSpawner just needs a
            // ParticleSystem component somewhere. Add a dummy one on the root so
            // the reference type matches the serialized field.
            prefabPS = savedPrefab.AddComponent<ParticleSystem>();
            var main = prefabPS.main;
            main.playOnAwake = false;
            main.loop = false;
            // Re-save after adding component
            savedPrefab = PrefabUtility.SaveAsPrefabAsset(savedPrefab, prefabPath);
            prefabPS = savedPrefab.GetComponent<ParticleSystem>();
        }

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty prop = so.FindProperty("explosionPrefab");
        prop.objectReferenceValue = prefabPS;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[CreateExplosionPrefab] Done — ParticleSpawner.explosionPrefab assigned and scene saved.");
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
            Debug.LogWarning("[CreateExplosionPrefab] Could not load sprite at: " + path);
        return s;
    }

    static ParticleSystem AddChildPS(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child.AddComponent<ParticleSystem>();
    }

    static void SetRenderer(ParticleSystem ps, Sprite sprite)
    {
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        if (sprite != null)
        {
            // Create a per-particle material using the sprite's texture so each
            // layer looks visually distinct
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Sprites/Default"));

            mat.mainTexture = sprite.texture;
            rend.material = mat;
        }
        else
        {
            // Fallback: use the built-in default particle material
            Material fallback = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (fallback != null) rend.material = fallback;
        }
    }

    // Shorthand for a simple 2-key color gradient (alpha 1 → 0)
    static Gradient FadeOutGradient(Color color)
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(1f, 0f),    new GradientAlphaKey(0f, 1f) }
        );
        return g;
    }

    // ----------------------------------------------------------------
    // Per-system builders
    // ----------------------------------------------------------------

    static void CreatePS_Flash(GameObject parent, Sprite sprite)
    {
        // Single bright burst — gone in 0.15 s, no loop
        ParticleSystem ps = AddChildPS(parent, "PS_Flash");

        var main = ps.main;
        main.duration        = 0.15f;
        main.loop            = false;
        main.startLifetime   = 0.15f;
        main.startSize       = 3.0f;
        main.startColor      = new Color(1f, 0.95f, 0.6f, 1f);
        main.playOnAwake     = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 1)
        });

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(1f, 0.95f, 0.6f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Fireball(GameObject parent, Sprite sprite)
    {
        // Big fireball that shrinks and fades — 0.4 s, no loop
        ParticleSystem ps = AddChildPS(parent, "PS_Fireball");

        var main = ps.main;
        main.duration      = 0.4f;
        main.loop          = false;
        main.startLifetime = 0.4f;
        main.startSize     = 2.5f;
        main.startColor    = new Color(1f, 0.5f, 0.1f, 1f);
        main.playOnAwake   = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 1)
        });

        // Shrink to nothing over lifetime
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve shrink = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(1f, 0.5f, 0.1f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Shockwave(GameObject parent, Sprite sprite)
    {
        // Ring that expands from 0.5 → 4.0 scale then vanishes — 0.3 s
        ParticleSystem ps = AddChildPS(parent, "PS_Shockwave");

        var main = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = 0.3f;
        main.startSize     = 0.5f;
        main.startColor    = new Color(1f, 0.8f, 0.4f, 0.8f);
        main.playOnAwake   = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 1)
        });

        // Grow from current size to 4 (expressed as a 0→1 multiplier, scaled by startSize)
        // We override via a curve that represents the size multiplier
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        // Curve: 0→1 maps size multiplier. We want 0.5→4, so multiplier 1→8
        AnimationCurve grow = AnimationCurve.Linear(0f, 1f, 1f, 8f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, grow);

        // Fade out toward end
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(1f, 0.8f, 0.4f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Embers(GameObject parent, Sprite sprite)
    {
        // 30–50 small embers flying outward with gravity — 0.8 s
        ParticleSystem ps = AddChildPS(parent, "PS_Embers");

        var main = ps.main;
        main.duration        = 0.8f;
        main.loop            = false;
        main.startLifetime   = 0.8f;
        main.startSize       = 0.3f;
        main.startColor      = new Color(1f, 0.4f, 0.05f, 1f);
        main.gravityModifier = -0.5f;
        main.playOnAwake     = true;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(30f, 50f))
        });

        var shape = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Sphere;
        shape.radius      = 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(1f, 0.4f, 0.05f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Sparks(GameObject parent, Sprite sprite)
    {
        // 8 quick sparks — 0.2 s, sphere spread
        ParticleSystem ps = AddChildPS(parent, "PS_Sparks");

        var main = ps.main;
        main.duration      = 0.2f;
        main.loop          = false;
        main.startLifetime = 0.2f;
        main.startSize     = 0.2f;
        main.startColor    = new Color(1f, 1f, 0.7f, 1f);
        main.playOnAwake   = true;
        main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 7f);

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 8)
        });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(1f, 1f, 0.7f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Debris(GameObject parent, Sprite sprite)
    {
        // 10–15 chunks with gravity and rotation — 1.0 s
        ParticleSystem ps = AddChildPS(parent, "PS_Debris");

        var main = ps.main;
        main.duration        = 1.0f;
        main.loop            = false;
        main.startLifetime   = 1.0f;
        main.startSize       = 0.25f;
        main.startColor      = new Color(0.6f, 0.35f, 0.1f, 1f);
        main.gravityModifier = 1.0f;
        main.playOnAwake     = true;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(10f, 15f))
        });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.15f;

        // Spin over lifetime
        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z       = new ParticleSystem.MinMaxCurve(180f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(0.6f, 0.35f, 0.1f)));

        SetRenderer(ps, sprite);
    }

    static void CreatePS_Smoke(GameObject parent, Sprite sprite)
    {
        // 5–8 drifting smoke puffs — 1.5 s, slow upward velocity
        ParticleSystem ps = AddChildPS(parent, "PS_Smoke");

        var main = ps.main;
        main.duration      = 1.5f;
        main.loop          = false;
        main.startLifetime = 1.5f;
        main.startSize     = 1.0f;
        main.startColor    = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        main.playOnAwake   = true;
        main.startSpeed    = 0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(5f, 8f))
        });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        // Drift upward
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space   = ParticleSystemSimulationSpace.World;
        velocity.y       = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);

        // Grow slightly and fade out
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve grow = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, grow);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = new ParticleSystem.MinMaxGradient(FadeOutGradient(new Color(0.4f, 0.4f, 0.4f)));

        SetRenderer(ps, sprite);
    }
}
