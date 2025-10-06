using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestisce lo stato del supermercato (apertura/chiusura)
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [Tooltip("Transform della porta del negozio")]
    public Transform shopDoorTransform;
    
    [Tooltip("Distanza massima dalla porta per aprire")]
    public float doorInteractionDistance = 3f;
    
    [Tooltip("UI Panel per notifica apertura/chiusura")]
    public GameObject notificationPanel;
    
    [Tooltip("Text per le notifiche")]
    public TextMeshProUGUI notificationText;
    
    [Tooltip("Durata notifica in secondi")]
    public float notificationDuration = 3f;

    private GlobalConfig config;
    private Transform playerTransform;
    private bool canOpenShop = false;
    private bool shopOpenedMorning = false;
    private bool shopOpenedAfternoon = false;
    private float notificationTimer = 0f;
    private bool showingNotification = false;

    void Start()
    {
        config = GlobalConfig.Instance;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }

        // Sottoscrivi agli eventi temporali
        TimeManager.OnShopOpenTime += OnMorningOpenTime;
        TimeManager.OnLunchCloseTime += OnLunchClose;
        TimeManager.OnAfternoonOpenTime += OnAfternoonOpenTime;
        TimeManager.OnEveningCloseTime += OnEveningClose;
        TimeManager.OnNewDay += ResetDailyShopFlags;
    }

    void OnDestroy()
    {
        TimeManager.OnShopOpenTime -= OnMorningOpenTime;
        TimeManager.OnLunchCloseTime -= OnLunchClose;
        TimeManager.OnAfternoonOpenTime -= OnAfternoonOpenTime;
        TimeManager.OnEveningCloseTime -= OnEveningClose;
        TimeManager.OnNewDay -= ResetDailyShopFlags;
    }

    void Update()
    {
        CheckPlayerNearDoor();
        HandleShopInteraction();
        UpdateNotification();
    }

    /// <summary>
    /// Controlla se il giocatore è vicino alla porta
    /// </summary>
    void CheckPlayerNearDoor()
    {
        if (playerTransform == null || shopDoorTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, shopDoorTransform.position);
        canOpenShop = distance <= doorInteractionDistance;
    }

    /// <summary>
    /// Gestisce l'interazione per aprire/chiudere il negozio
    /// </summary>
    void HandleShopInteraction()
    {
        if (!canOpenShop || playerTransform == null) return;

        float currentTime = config.currentHour + (config.currentMinute / 60f);

        // Apertura mattutina (8:00)
        if (Input.GetKeyDown(KeyCode.E) && !config.isShopOpen && 
            currentTime >= config.morningOpenTime && currentTime < config.lunchCloseTime &&
            !shopOpenedMorning)
        {
            OpenShop();
            shopOpenedMorning = true;
            ShowNotification("Supermercato aperto!");
        }
        // Chiusura pranzo (12:30)
        else if (Input.GetKeyDown(KeyCode.E) && config.isShopOpen && 
                 currentTime >= config.lunchCloseTime && shopOpenedMorning)
        {
            CloseShop();
            ShowNotification("Supermercato chiuso per pausa pranzo!");
        }
        // Riapertura pomeridiana (15:30)
        else if (Input.GetKeyDown(KeyCode.E) && !config.isShopOpen && 
                 currentTime >= config.afternoonOpenTime && currentTime < config.eveningCloseTime &&
                 !shopOpenedAfternoon)
        {
            OpenShop();
            shopOpenedAfternoon = true;
            ShowNotification("Supermercato riaperto!");
        }
        // Chiusura serale (20:00)
        else if (Input.GetKeyDown(KeyCode.E) && config.isShopOpen && 
                 currentTime >= config.eveningCloseTime)
        {
            CloseShop();
            ShowNotification("Supermercato chiuso per la giornata!");
        }
    }

    /// <summary>
    /// Apre il negozio
    /// </summary>
    void OpenShop()
    {
        config.isShopOpen = true;
        Debug.Log("Negozio APERTO");
        // Qui puoi aggiungere animazioni della porta, attivare luci, ecc.
    }

    /// <summary>
    /// Chiude il negozio
    /// </summary>
    void CloseShop()
    {
        config.isShopOpen = false;
        Debug.Log("Negozio CHIUSO");
        // Qui puoi aggiungere animazioni della porta, spegnere luci, ecc.
    }

    /// <summary>
    /// Eventi temporali
    /// </summary>
    void OnMorningOpenTime()
    {
        if (!config.isShopOpen)
        {
            ShowNotification("È ora di aprire il supermercato! (Premi E vicino alla porta)");
        }
    }

    void OnLunchClose()
    {
        if (config.isShopOpen)
        {
            ShowNotification("È ora di chiudere per pranzo!");
        }
    }

    void OnAfternoonOpenTime()
    {
        if (!config.isShopOpen)
        {
            ShowNotification("È ora di riaprire il supermercato! (Premi E vicino alla porta)");
        }
    }

    void OnEveningClose()
    {
        if (config.isShopOpen)
        {
            ShowNotification("È ora di chiudere! Puoi andare al bar dopo.");
        }
    }

    void ResetDailyShopFlags()
    {
        shopOpenedMorning = false;
        shopOpenedAfternoon = false;
    }

    /// <summary>
    /// Mostra una notifica
    /// </summary>
    void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            showingNotification = true;
            notificationTimer = notificationDuration;
        }
    }

    /// <summary>
    /// Aggiorna il timer delle notifiche
    /// </summary>
    void UpdateNotification()
    {
        if (showingNotification)
        {
            notificationTimer -= Time.deltaTime;
            
            if (notificationTimer <= 0f)
            {
                if (notificationPanel != null)
                {
                    notificationPanel.SetActive(false);
                }
                showingNotification = false;
            }
        }
    }
}