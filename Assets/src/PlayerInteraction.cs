using UnityEngine;

/// <summary>
/// Simple and robust player interaction system for grabbing, placing, and throwing items
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPosition;

    [Header("Interaction Settings")]
    public float grabDistance = 3f;
    public float placeDistance = 3f;
    public KeyCode grabKey = KeyCode.E;
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode throwKey = KeyCode.Mouse1;
    public KeyCode rotateKey = KeyCode.R;
    public KeyCode setPriceKey = KeyCode.P;

    [Header("Physics")]
    public float throwForce = 10f;
    public float throwUpForce = 2f;
    public float rotationSpeed = 100f;

    [Header("Preview")]
    public bool showPreview = true;
    public Material previewMaterial;
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Layers")]
    public LayerMask itemLayer;
    public LayerMask placementLayer; // What surfaces can we place on

    [Header("Debug")]
    public bool debugMode = false;

    // Private
    private ShopItem heldItem;
    private GameObject previewObject;
    private float currentRotation = 0f;
    private bool canPlace = false;
    private Vector3 targetPlacePosition;
    private Quaternion targetPlaceRotation;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (holdPosition == null)
        {
            GameObject holdPoint = new GameObject("HoldPosition");
            holdPosition = holdPoint.transform;
            holdPosition.SetParent(playerCamera.transform);
            holdPosition.localPosition = new Vector3(0.3f, -0.3f, 0.5f);
        }

        // Setup layers if not set
        if (itemLayer == 0)
            itemLayer = LayerMask.GetMask("ShopItem");
        
        if (placementLayer == 0)
            placementLayer = ~0; // Everything
    }

    void Update()
    {
        // Handle input first
        HandleInput();

        // Then update state based on what we're holding
        if (heldItem != null && heldItem.isBeingHeld)
        {
            HandleHeldItem();
        }
        else if (heldItem == null)
        {
            CheckForGrabbableItems();
        }
    }

    /// <summary>
    /// Check for items in front of player
    /// </summary>
    void CheckForGrabbableItems()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            if (item != null && !item.isBeingHeld)
            {
                if (debugMode)
                    Debug.DrawRay(ray.origin, ray.direction * grabDistance, Color.green);
            }
        }
    }

    /// <summary>
    /// Handle item currently held
    /// </summary>
    void HandleHeldItem()
    {
        if (heldItem == null) return;

        // CRITICAL: Only move item if it's actually being held (not just placed)
        if (!heldItem.isBeingHeld)
        {
            // Item was placed, don't move it
            return;
        }

        // Keep item at hold position
        heldItem.transform.position = holdPosition.position;
        heldItem.transform.rotation = holdPosition.rotation * Quaternion.Euler(0f, currentRotation, 0f);

        // Check where we can place
        CheckPlacement();
    }

    /// <summary>
    /// Check valid placement position
    /// </summary>
    void CheckPlacement()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance, placementLayer))
        {
            // Found a surface to place on
            targetPlacePosition = hit.point + hit.normal * 0.05f; // Slightly above surface
            targetPlaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, currentRotation, 0f);
            
            // Check if position is valid (not overlapping other objects)
            canPlace = IsPositionValid(targetPlacePosition);
            
            if (showPreview)
            {
                ShowPreview(targetPlacePosition, targetPlaceRotation, canPlace);
            }

            if (debugMode && canPlace)
            {
                Debug.DrawLine(playerCamera.transform.position, targetPlacePosition, Color.green);
            }
        }
        else
        {
            canPlace = false;
            HidePreview();
        }
    }

    /// <summary>
    /// Check if placement position is valid
    /// </summary>
    bool IsPositionValid(Vector3 position)
    {
        // Check for overlapping items
        Collider[] overlaps = Physics.OverlapSphere(position, 0.2f, itemLayer);
        
        foreach (Collider col in overlaps)
        {
            ShopItem otherItem = col.GetComponent<ShopItem>();
            if (otherItem != null && otherItem != heldItem && otherItem.isPlaced)
            {
                return false; // Too close to another placed item
            }
        }
        
        return true;
    }

    /// <summary>
    /// Handle player input
    /// </summary>
    void HandleInput()
    {
        // Grab
        if (Input.GetKeyDown(grabKey))
        {
            if (heldItem == null)
                TryGrabItem();
        }

        // Place
        if (Input.GetKeyDown(placeKey) && heldItem != null)
        {
            if (canPlace)
                PlaceItem();
        }

        // Throw
        if (Input.GetKeyDown(throwKey) && heldItem != null)
        {
            ThrowItem();
        }

        // Rotate
        if (Input.GetKey(rotateKey) && heldItem != null)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
        }

        // Set price
        if (Input.GetKeyDown(setPriceKey) && heldItem != null)
        {
            float suggestedPrice = heldItem.GetSuggestedPrice();
            PriceUI.ShowPriceMenu?.Invoke(heldItem, suggestedPrice);
        }
    }

    /// <summary>
    /// Try to grab an item
    /// </summary>
    void TryGrabItem()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, grabDistance);

        // Sort by distance
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            
            if (item != null && !item.isBeingHeld)
            {
                GrabItem(item);
                return;
            }
        }

        if (debugMode)
            Debug.Log("No item found to grab");
    }

    /// <summary>
    /// Grab an item
    /// </summary>
    void GrabItem(ShopItem item)
    {
        heldItem = item;
        currentRotation = 0f;
        
        item.transform.SetParent(holdPosition);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        item.OnGrab();

        if (debugMode)
            Debug.Log($"Grabbed: {item.itemName}");
    }

    /// <summary>
    /// Place the held item
    /// </summary>
    void PlaceItem()
    {
        if (heldItem == null || !canPlace) return;

        // Store references
        ShopItem itemToPlace = heldItem;
        Vector3 placePos = targetPlacePosition;
        Quaternion placeRot = targetPlaceRotation;

        if (debugMode)
        {
            Debug.Log($"=== PLACING ITEM ===");
            Debug.Log($"Item: {itemToPlace.itemName}");
            Debug.Log($"Current position: {itemToPlace.transform.position}");
            Debug.Log($"Target position: {placePos}");
            Debug.Log($"Target rotation: {placeRot.eulerAngles}");
        }

        // STEP 1: Clear held item FIRST so Update() doesn't move it
        heldItem = null;

        // STEP 2: Remove from parent immediately
        itemToPlace.transform.SetParent(null);

        // STEP 3: Force position and rotation to target
        itemToPlace.transform.position = placePos;
        itemToPlace.transform.rotation = placeRot;
        
        // STEP 4: Call OnPlace to set physics state
        itemToPlace.OnPlace();

        // STEP 5: Force position again after OnPlace (in case physics moved it)
        itemToPlace.transform.position = placePos;
        itemToPlace.transform.rotation = placeRot;

        // STEP 6: Use coroutine to force position in next frame as safety measure
        StartCoroutine(EnsurePlacementPosition(itemToPlace, placePos, placeRot));

        if (debugMode)
        {
            Debug.Log($"Final position: {itemToPlace.transform.position}");
            Debug.Log($"===================");
        }

        // Cleanup
        HidePreview();
        DestroyPreview();
        canPlace = false;
        currentRotation = 0f;
    }

    /// <summary>
    /// Ensures item stays at placement position for a few frames
    /// </summary>
    System.Collections.IEnumerator EnsurePlacementPosition(ShopItem item, Vector3 targetPos, Quaternion targetRot)
    {
        // Wait for end of frame
        yield return new WaitForEndOfFrame();

        // Force position for 3 frames to be absolutely sure
        for (int i = 0; i < 3; i++)
        {
            if (item != null && item.isPlaced)
            {
                item.transform.position = targetPos;
                item.transform.rotation = targetRot;

                if (debugMode && i == 0)
                {
                    Debug.Log($"[Frame {i}] Enforced position: {item.transform.position}");
                }
            }
            yield return null;
        }

        if (debugMode)
        {
            Debug.Log($"Placement secured at: {item.transform.position}");
        }
    }

    /// <summary>
    /// Throw the held item
    /// </summary>
    void ThrowItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);
        
        Vector3 throwDirection = playerCamera.transform.forward;
        Vector3 throwVelocity = throwDirection * throwForce + Vector3.up * throwUpForce;
        
        heldItem.OnThrow(throwVelocity);

        if (debugMode)
            Debug.Log($"Threw: {heldItem.itemName}");

        CleanupAfterRelease();
    }

    /// <summary>
    /// Cleanup after releasing item
    /// </summary>
    void CleanupAfterRelease()
    {
        HidePreview();
        DestroyPreview();
        heldItem = null;
        canPlace = false;
        currentRotation = 0f;
    }

    /// <summary>
    /// Show placement preview
    /// </summary>
    void ShowPreview(Vector3 position, Quaternion rotation, bool isValid)
    {
        if (heldItem == null) return;

        // Create preview if needed
        if (previewObject == null)
        {
            CreatePreviewObject();
        }

        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = position;
            previewObject.transform.rotation = rotation;

            // Update color
            Color color = isValid ? validColor : invalidColor;
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    mat.color = color;
                }
            }
        }
    }

    /// <summary>
    /// Hide preview
    /// </summary>
    void HidePreview()
    {
        if (previewObject != null)
            previewObject.SetActive(false);
    }

    /// <summary>
    /// Create preview object
    /// </summary>
    void CreatePreviewObject()
    {
        if (heldItem == null) return;

        previewObject = new GameObject("Preview");

        // Create material if needed
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.SetFloat("_Mode", 3);
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.renderQueue = 3000;
        }

        // Copy mesh structure
        MeshFilter[] meshes = heldItem.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshes)
        {
            if (mf.sharedMesh == null) continue;

            GameObject part = new GameObject(mf.name);
            part.transform.SetParent(previewObject.transform);
            part.transform.localPosition = mf.transform.localPosition;
            part.transform.localRotation = mf.transform.localRotation;
            part.transform.localScale = mf.transform.localScale;

            MeshFilter newMf = part.AddComponent<MeshFilter>();
            newMf.sharedMesh = mf.sharedMesh;

            MeshRenderer newMr = part.AddComponent<MeshRenderer>();
            Material[] mats = new Material[mf.GetComponent<MeshRenderer>().sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(previewMaterial);
            }
            newMr.materials = mats;
        }

        previewObject.SetActive(false);
    }

    /// <summary>
    /// Destroy preview object
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
    /// Check if holding an item
    /// </summary>
    public bool IsHoldingItem()
    {
        return heldItem != null;
    }

    /// <summary>
    /// Get held item
    /// </summary>
    public ShopItem GetHeldItem()
    {
        return heldItem;
    }

    /// <summary>
    /// Can place at current position
    /// </summary>
    public bool CanPlace()
    {
        return canPlace;
    }

    void OnDrawGizmos()
    {
        if (!debugMode || playerCamera == null) return;

        // Draw grab range
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * grabDistance);
    }
}