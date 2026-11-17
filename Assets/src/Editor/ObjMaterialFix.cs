using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// I .obj di Assets/Resources/Models arrivano senza texture (l'importer ignora map_Kd).
/// Il nome del materiale è "tipo_colore" (es. intonaco_c9bfa6): texture = texture/tipo.png, colore = Kd.
/// </summary>
class ObjMaterialFix : AssetPostprocessor
{
    void OnPreprocessMaterialDescription(MaterialDescription d, Material m, AnimationClip[] clips)
    {
        if (!assetPath.StartsWith("Assets/Resources/Models/") || !assetPath.EndsWith(".obj")) return;
        if (d.TryGetProperty("DiffuseColor", out Vector4 kd)) m.SetColor("_BaseColor", kd);
        string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string n = m.name.EndsWith("_e") ? m.name.Substring(0, m.name.Length - 2) : m.name;
        string kind = n.Substring(0, n.LastIndexOf('_') < 0 ? n.Length : n.LastIndexOf('_')); // "stampa_a_f4efd8" -> "stampa_a"
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/texture/{kind}.png");
        if (!tex) tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/texture/{kind.Split('_')[0]}.png"); // "piastrelle_muro" -> "piastrelle"
        if (tex) m.SetTexture("_BaseMap", tex);
        if (m.name.EndsWith("_e")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", kd); } // _e = acceso
    }
}
