#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Crea la scena di gioco minima (il mondo lo costruisce WorldBuilder in Play) e, a richiesta, ci cuoce dentro il mondo per vederlo e modificarlo nell'editor.</summary>
public static class GameSceneMenu
{
    const string Path = "Assets/Scenes/Game.unity";

    [MenuItem("uarewhatueat/Crea scena di gioco")]
    static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        new GameObject("GlobalConfig").AddComponent<GlobalConfig>();
        var sun = new GameObject("Directional Light");
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        RenderSettings.sun = l;
        EditorSceneManager.SaveScene(scene, Path);

        // in testa alle Build Settings
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Path, true) };
        foreach (var s in EditorBuildSettings.scenes) if (s.path != Path) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("Scena creata: " + Path);
    }
    const string MondoAsset = "Assets/Scenes/Game_Mondo.asset";

    // Costruisce il mondo in edit mode dentro Game.unity: mesh, materiali, texture e terreno generati finiscono in Game_Mondo.asset.
    // In Play GameBootstrap trova WorldAnchorsData e non lo ricostruisce. Rilanciare dopo aver cambiato WorldBuilder (sovrascrive le modifiche a mano).
    [MenuItem("uarewhatueat/Cuoci il mondo nella scena")]
    static void Bake()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(Path);
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Mondo" || go.CompareTag("Player")) Object.DestroyImmediate(go);
        var cfg = Object.FindAnyObjectByType<GlobalConfig>();
        if (cfg == null) { Debug.LogError("Cuoci il mondo: manca GlobalConfig in " + Path); return; }
        var inst = typeof(GlobalConfig).GetProperty("Instance");
        inst.SetValue(null, cfg); // Awake non gira in edit mode
        try { WorldBuilder.Build(); }
        finally { inst.SetValue(null, null); }

        // oggetti creati da codice -> sotto-asset di un unico file, altrimenti finirebbero dentro la scena
        var made = new List<Object>();
        var seen = new HashSet<Object>();
        void Keep(Object o) { if (o != null && !EditorUtility.IsPersistent(o) && seen.Add(o)) made.Add(o); }
        void KeepMat(Material m)
        {
            Keep(m);
            if (m != null) foreach (var n in m.GetTexturePropertyNames()) Keep(m.GetTexture(n));
        }
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var f in go.GetComponentsInChildren<MeshFilter>(true)) Keep(f.sharedMesh);
            foreach (var c in go.GetComponentsInChildren<MeshCollider>(true)) Keep(c.sharedMesh);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) KeepMat(m);
            foreach (var t in go.GetComponentsInChildren<Terrain>(true))
            {
                KeepMat(t.materialTemplate);
                if (t.terrainData == null) continue;
                foreach (var l in t.terrainData.terrainLayers) { Keep(l); if (l != null) Keep(l.diffuseTexture); }
                Keep(t.terrainData);
            }
        }
        AssetDatabase.DeleteAsset(MondoAsset);
        if (made.Count > 0)
        {
            AssetDatabase.CreateAsset(made[0], MondoAsset);
            for (int i = 1; i < made.Count; i++) AssetDatabase.AddObjectToAsset(made[i], MondoAsset);
            AssetDatabase.SaveAssets();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Mondo cotto in {Path} ({made.Count} oggetti generati in {MondoAsset})");
    }

}
#endif
