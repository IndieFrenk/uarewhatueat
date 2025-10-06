using UnityEngine;

/// <summary>
/// Gestisce le interazioni del giocatore con oggetti e scaffali
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Camera del giocatore")]
    public Camera playerCamera;
    
    [Tooltip("Layer degli oggetti afferrabili")]
    public LayerMask itemLayer;
    
    [Tooltip("Layer degli scaffali")]
    public LayerMask shelfLayer;
    
    [Tooltip("Transform dove tenere l'oggetto (mano destra)")]
    public Transform rightHandTransform;

    [Header("Throw Settings")]
    [Tooltip("Forza del lancio")]
    public float throwForce = 10f;
    
    [Tooltip("Forza verso l'alto del lancio")]
    public float throwUpwardForce = 2f;

    [Header("Input Settings")]
    [Tooltip("Tasto per afferrare oggetti")]
    public KeyCode grabKey = KeyCode.E;
    
    [Tooltip("Tasto per posizionare oggetti")]
    public KeyCode placeKey = KeyCode.Mouse0; // Click sinistro
    
    [Tooltip("Tasto per lanciare oggetti")]
    public KeyCode throwKey = KeyCode.Mouse1; // Click destro
    
    [Tooltip("Tasto per ruotare oggetti")]
    public KeyCode rotateKey = KeyCode.R;
    
    [Tooltip("Tasto per assegnare prezzo")]
    public KeyCode setPriceKey = KeyCode.P;

    [Header("Preview Settings")]
    [Tooltip("Materiale per la preview dell'oggetto")]
    public Material previewMaterial;
    
    [Tooltip("Colore preview valida (verde)")]
    public Color validPreviewColor = new Color(0f, 1f, 0f, 0.5f);
    
    [Tooltip("Colore preview invalida (rosso)")]
    public Color invalidPreviewColor = new Color(1f, 0f, 0f, 0.5f);

    private GlobalConfig config;
    private ShopItem heldItem = null;
    private Shelf highlightedShelf = null;
    private GameObject previewObject = null;
    private bool isHoldingItem = false;
    private bool canPlaceOnShelf = false;
    private Vector3 targetPlacementPosition = Vector3.zero;
    private float currentRotation = 0f;

    void Start()
    {
        config = GlobalConfig.Instance;
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Crea la mano destra se non esiste
        if (rightHandTransform == null)
        {
            GameObject rightHand = new GameObject("RightHand");
            rightHandTransform = rightHand.transform;
            rightHandTransform.SetParent(playerCamera.transform);
            rightHandTransform.localPosition = new Vector3(0.3f, -0.3f, 0.5f);
            rightHandTransform.localRotation = Quaternion.Euler(0f, -15f, 0f);
        }

        // Crea materiale preview se non esiste
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.SetFloat("_Mode", 3); // Transparent mode
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
        }
    }

    void Update()
    {
        if (isHoldingItem)
        {
            HandleHeldItem();
        }
        else
        {
            CheckForInteractables();
        }

        HandleInput();
    }

    /// <summary>
    /// Controlla oggetti afferrabili davanti al giocatore
    /// </summary>
    void CheckForInteractables()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, config.grabDistance, itemLayer))
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            if (item != null && !item.IsBeingHeld())
            {
                Debug.DrawRay(ray.origin, ray.direction * config.grabDistance, Color.green);
            }
        }
    }

    /// <summary>
    /// Gestisce l'oggetto tenuto in mano
    /// </summary>
    void HandleHeldItem()
    {
        if (heldItem == null) return;

        // Mantieni l'oggetto nella mano destra
        heldItem.transform.position = rightHandTransform.position;
        heldItem.transform.rotation = rightHandTransform.rotation * Quaternion.Euler(0f, currentRotation, 0f);

        // Cerca scaffali per il posizionamento e mostra preview
        CheckShelfPlacement();
    }

    /// <summary>
    /// Controlla il posizionamento sullo scaffale e mostra preview
    /// </summary>
    void CheckShelfPlacement()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        // Rimuovi evidenziazione precedente
        if (highlightedShelf != null)
        {
            highlightedShelf.Highlight(false);
            highlightedShelf = null;
        }

        canPlaceOnShelf = false;

        if (Physics.Raycast(ray, out hit, config.shelfPlacementDistance, shelfLayer))
        {
            Shelf shelf = hit.collider.GetComponent<Shelf>();
            if (shelf != null && shelf.HasSpace())
            {
                highlightedShelf = shelf;
                highlightedShelf.Highlight(true);

                // Ottieni la posizione dello slot più vicino
                bool hasSlot;
                targetPlacementPosition = shelf.GetNearestAvailableSlot(hit.point, out hasSlot);

                if (hasSlot)
                {
                    canPlaceOnShelf = true;
                    ShowPreview(targetPlacementPosition, true);
                }
                else
                {
                    ShowPreview(hit.point, false);
                }
            }
            else
            {
                HidePreview();
            }
        }
        else
        {
            HidePreview();
        }
    }

    /// <summary>
    /// Mostra la preview dell'oggetto sullo scaffale
    /// </summary>
    void ShowPreview(Vector3 position, bool isValid)
    {
        if (heldItem == null) return;

        // Crea preview se non esiste
        if (previewObject == null)
        {
            previewObject = new GameObject("PreviewObject");
            
            // Copia tutti i MeshFilter e MeshRenderer dall'oggetto originale
            MeshFilter[] originalMeshes = heldItem.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter originalMesh in originalMeshes)
            {
                GameObject previewPart = new GameObject(originalMesh.name);
                previewPart.transform.SetParent(previewObject.transform);
                previewPart.transform.localPosition = originalMesh.transform.localPosition;
                previewPart.transform.localRotation = originalMesh.transform.localRotation;
                previewPart.transform.localScale = originalMesh.transform.localScale;

                MeshFilter mf = previewPart.AddComponent<MeshFilter>();
                mf.mesh = originalMesh.sharedMesh;

                MeshRenderer mr = previewPart.AddComponent<MeshRenderer>();
                Material[] materials = new Material[originalMesh.GetComponent<MeshRenderer>().sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new Material(previewMaterial);
                }
                mr.materials = materials;
            }
        }

        // Posiziona la preview
        previewObject.transform.position = position;
        previewObject.transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
        previewObject.SetActive(true);

        // Colora la preview in base alla validità
        Color previewColor = isValid ? validPreviewColor : invalidPreviewColor;
        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = previewColor;
            }
        }
    }

    /// <summary>
    /// Nasconde la preview
    /// </summary>
    void HidePreview()
    {
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gestisce l'input del giocatore
    /// </summary>
    void HandleInput()
    {
        // Afferrare oggetto (E)
        if (Input.GetKeyDown(grabKey) && !isHoldingItem)
        {
            TryGrabItem();
        }

        // Posizionare oggetto (Click Sinistro)
        if (Input.GetKeyDown(placeKey) && isHoldingItem)
        {
            PlaceItem();
        }

        // Lanciare oggetto (Click Destro)
        if (Input.GetKeyDown(throwKey) && isHoldingItem)
        {
            ThrowItem();
        }

        // Ruotare oggetto (R)
        if (Input.GetKey(rotateKey) && isHoldingItem)
        {
            currentRotation += config.objectRotationSpeed * Time.deltaTime;
        }

        // Assegnare prezzo (P)
        if (Input.GetKeyDown(setPriceKey) && isHoldingItem)
        {
            OpenPriceMenu();
        }
    }

    /// <summary>
    /// Tenta di afferrare un oggetto
    /// </summary>
    void TryGrabItem()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, config.grabDistance, itemLayer))
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            
            if (item != null && !item.IsBeingHeld())
            {
                GrabItem(item);
            }
        }
    }

    /// <summary>
    /// Afferra un oggetto
    /// </summary>
    void GrabItem(ShopItem item)
    {
        heldItem = item;
        isHoldingItem = true;
        currentRotation = 0f;
        
        // Rimuovi dallo scaffale se era posizionato
        if (item.isOnShelf)
        {
            item.RemoveFromShelf();
        }
        
        item.OnGrabbed();
        item.transform.SetParent(rightHandTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"Afferrato: {item.itemName}");
    }

    /// <summary>
    /// Posiziona l'oggetto
    /// </summary>
    void PlaceItem()
    {
        if (heldItem == null) return;

        // Controlla se c'è uno scaffale valido
        if (canPlaceOnShelf && highlightedShelf != null)
        {
            // Posiziona sullo scaffale
            heldItem.transform.SetParent(null);
            highlightedShelf.AddItem(heldItem, targetPlacementPosition);
            highlightedShelf.Highlight(false);
            
            Debug.Log($"Posizionato {heldItem.itemName} su scaffale");
        }
        else
        {
            // Posiziona a terra davanti al giocatore
            heldItem.transform.SetParent(null);
            
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 3f))
            {
                heldItem.transform.position = hit.point + Vector3.up * 0.1f;
            }
            else
            {
                heldItem.transform.position = playerCamera.transform.position + playerCamera.transform.forward * 2f;
            }
            
            heldItem.OnReleased();
            Debug.Log($"Posizionato {heldItem.itemName} a terra");
        }

        // Reset stato
        HidePreview();
        DestroyPreview();
        heldItem = null;
        isHoldingItem = false;
        canPlaceOnShelf = false;
    }

    /// <summary>
    /// Lancia l'oggetto
    /// </summary>
    void ThrowItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);
        heldItem.OnReleased();

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDirection = playerCamera.transform.forward;
            Vector3 throwVelocity = throwDirection * throwForce + Vector3.up * throwUpwardForce;
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
        }

        Debug.Log($"Lanciato: {heldItem.itemName}");

        // Reset stato
        HidePreview();
        DestroyPreview();
        heldItem = null;
        isHoldingItem = false;
        canPlaceOnShelf = false;
    }

    /// <summary>
    /// Distrugge l'oggetto preview
    /// </summary>
    void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    /// <summary>
    /// Apre il menu per assegnare il prezzo
    /// </summary>
    void OpenPriceMenu()
    {
        if (heldItem == null) return;

        float suggestedPrice = heldItem.GetSuggestedPrice();
        Debug.Log($"Prezzo suggerito per {heldItem.itemName}: €{suggestedPrice:F2}");
        
        // Invia evento per aprire UI prezzo
        PriceUI.ShowPriceMenu?.Invoke(heldItem, suggestedPrice);
    }

    /// <summary>
    /// Ottiene l'oggetto attualmente tenuto
    /// </summary>
    public ShopItem GetHeldItem()
    {
        return heldItem;
    }

    /// <summary>
    /// Controlla se il giocatore sta tenendo un oggetto
    /// </summary>
    public bool IsHoldingItem()
    {
        return isHoldingItem;
    }

    /// <summary>
    /// Controlla se può posizionare sullo scaffale
    /// </summary>
    public bool CanPlaceOnShelf()
    {
        return canPlaceOnShelf;
    }
}