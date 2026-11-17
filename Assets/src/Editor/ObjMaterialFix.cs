using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// I .obj di Assets/Models arrivano senza texture (l'importer ignora map_Kd).
/// Il nome del materiale è "tipo_colore" (es. intonaco_c9bfa6): texture = texture/tipo.png, colore = Kd.
/// </summary>
class ObjMaterialFix : AssetPostprocessor
{
    void OnPreprocessMaterialDescription(MaterialDescription d, Material m, AnimationClip[] clips)
    {
        if (!assetPath.StartsWith("Assets/Models/") || !assetPath.EndsWith(".obj")) return;
        if (d.TryGetProperty("DiffuseColor", out Vector4 kd)) m.SetColor("_BaseColor", kd);
        string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string kind = m.name.Split('_')[0];
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/texture/{kind}.png");
        if (tex) m.SetTexture("_BaseMap", tex);
        if (m.name.EndsWith("_e")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", kd); } // _e = acceso
    }
}
