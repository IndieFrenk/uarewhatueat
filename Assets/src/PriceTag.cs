using UnityEngine;
using TMPro;

/// <summary>
/// Cartellino del prezzo visuale su un oggetto
/// </summary>
public class PriceTag : MonoBehaviour
{
    [Header("Price Tag Settings")]
    [Tooltip("Text per mostrare il prezzo")]
    public TextMeshPro priceText;
    
    [Tooltip("Canvas del cartellino (opzionale, per World Space UI)")]
    public Canvas tagCanvas;
    
    [Tooltip("Scala del cartellino")]
    public float tagScale = 0.01f;

    private float currentPrice = 0f;

    void Start()
    {
        // Configura il canvas se presente
        if (tagCanvas != null)
        {
            tagCanvas.renderMode = RenderMode.WorldSpace;
        }

        // Applica la scala
        transform.localScale = Vector3.one * tagScale;
    }

    void Update()
    {
        // Fai in modo che il cartellino guardi sempre la camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                mainCam.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// Imposta il prezzo sul cartellino
    /// </summary>
    public void SetPrice(float price)
    {
        currentPrice = price;
        
        if (priceText != null)
        {
            priceText.text = $"€{price:F2}";
        }
    }

    /// <summary>
    /// Ottiene il prezzo corrente
    /// </summary>
    public float GetPrice()
    {
        return currentPrice;
    }
}