#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Crea la scena di gioco minima: tutto il resto lo costruisce GameBootstrap/WorldBuilder da codice.</summary>
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
}
#endif
