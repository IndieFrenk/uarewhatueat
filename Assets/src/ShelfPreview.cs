using UnityEngine;

/// <summary>
/// Gestisce la preview visiva degli oggetti sugli scaffali
/// </summary>
public class ShelfPreview : MonoBehaviour
{
    [Header("Preview Settings")]
    [Tooltip("Materiale per la preview (trasparente)")]
    public Material previewMaterial;
    
    [Tooltip("Colore quando la posizione è valida")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    
    [Tooltip("Colore quando la posizione non è valida")]
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Tooltip("Scala della preview rispetto all'originale")]
    public float previewScale = 1.05f;

    private GameObject previewObject;
    private bool isActive = false;

    /// <summary>
    /// Crea la preview da un oggetto esistente
    /// </summary>
    public void CreatePreview(GameObject sourceObject)
    {
        if (sourceObject == null) return;

        // Distruggi preview esistente
        DestroyPreview();

        // Crea il container della preview
        previewObject = new GameObject("ShelfPreview_" + sourceObject.name);
        previewObject.transform.localScale = Vector3.one * previewScale;

        // Copia la struttura mesh dell'oggetto originale
        CopyMeshStructure(sourceObject, previewObject);

        // Applica il materiale trasparente
        ApplyPreviewMaterial();

        previewObject.SetActive(false);
        isActive = false;
    }

    /// <summary>
    /// Copia la struttura delle mesh dall'oggetto originale
    /// </summary>
    void CopyMeshStructure(GameObject source, GameObject destination)
    {
        MeshFilter[] sourceMeshes = source.GetComponentsInChildren<MeshFilter>();
        
        foreach (MeshFilter sourceMesh in sourceMeshes)
        {
            // Crea un game object per questa parte della mesh
            GameObject meshPart = new GameObject(sourceMesh.name);
            meshPart.transform.SetParent(destination.transform);
            
            // Copia transform locale
            meshPart.transform.localPosition = GetLocalPositionRelativeTo(sourceMesh.transform, source.transform);
            meshPart.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * sourceMesh.transform.rotation;
            meshPart.transform.localScale = sourceMesh.transform.localScale;

            // Copia la mesh
            MeshFilter mf = meshPart.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMesh.sharedMesh;

            // Aggiungi renderer
            MeshRenderer mr = meshPart.AddComponent<MeshRenderer>();
        }
    }

    /// <summary>
    /// Ottiene la posizione locale relativa
    /// </summary>
    Vector3 GetLocalPositionRelativeTo(Transform child, Transform parent)
    {
        return parent.InverseTransformPoint(child.position);
    }

    /// <summary>
    /// Applica il materiale trasparente a tutti i renderer
    /// </summary>
    void ApplyPreviewMaterial()
    {
        if (previewObject == null) return;

        // Crea materiale se non esiste
        if (previewMaterial == null)
        {
            CreateDefaultPreviewMaterial();
        }

        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(previewMaterial);
            }
            renderer.materials = materials;
        }
    }

    /// <summary>
    /// Crea un materiale trasparente di default
    /// </summary>
    void CreateDefaultPreviewMaterial()
    {
        previewMaterial = new Material(Shader.Find("Standard"));
        
        // Configura per trasparenza
        previewMaterial.SetFloat("_Mode", 3);
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.DisableKeyword("_ALPHATEST_ON");
        previewMaterial.EnableKeyword("_ALPHABLEND_ON");
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        previewMaterial.renderQueue = 3000;
        previewMaterial.color = validColor;
    }

    /// <summary>
    /// Mostra la preview in una posizione specifica
    /// </summary>
    public void ShowPreview(Vector3 position, Quaternion rotation, bool isValidPosition)
    {
        if (previewObject == null) return;

        previewObject.transform.position = position;
        previewObject.transform.rotation = rotation;
        previewObject.SetActive(true);
        isActive = true;

        // Cambia colore in base alla validità
        SetPreviewColor(isValidPosition ? validColor : invalidColor);
    }

    /// <summary>
    /// Imposta il colore della preview
    /// </summary>
    void SetPreviewColor(Color color)
    {
        if (previewObject == null) return;

        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }

    /// <summary>
    /// Nasconde la preview
    /// </summary>
    public void HidePreview()
    {
        if (previewObject != null)
        {
            previewObject.SetActive(false);
            isActive = false;
        }
    }

    /// <summary>
    /// Distrugge la preview
    /// </summary>
    public void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            isActive = false;
        }
    }

    /// <summary>
    /// Controlla se la preview è attiva
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    void OnDestroy()
    {
        DestroyPreview();
    }
}