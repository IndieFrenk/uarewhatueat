using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestisce la UI per visualizzare ora e giorno della settimana
/// </summary>
public class TimeUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text per visualizzare l'orologio")]
    public TextMeshProUGUI clockText;
    
    [Tooltip("Text per visualizzare il giorno della settimana")]
    public TextMeshProUGUI dayText;
    
    [Tooltip("Panel per il messaggio di sveglia")]
    public GameObject wakeUpPanel;
    
    [Tooltip("Text per il messaggio di sveglia")]
    public TextMeshProUGUI wakeUpMessageText;
    
    [Tooltip("Durata del messaggio di sveglia in secondi")]
    public float wakeUpMessageDuration = 3f;

    private GlobalConfig config;
    private TimeManager timeManager;
    private float wakeUpMessageTimer = 0f;
    private bool showingWakeUpMessage = false;

    void Start()
    {
        config = GlobalConfig.Instance;
        timeManager = FindObjectOfType<TimeManager>();

        if (wakeUpPanel != null)
        {
            wakeUpPanel.SetActive(false);
        }

        // Sottoscrivi all'evento di sveglia
        TimeManager.OnWakeUp += ShowWakeUpMessage;
    }

    void OnDestroy()
    {
        TimeManager.OnWakeUp -= ShowWakeUpMessage;
    }

    void Update()
    {
        UpdateClockDisplay();
        UpdateDayDisplay();
        UpdateWakeUpMessage();
    }

    /// <summary>
    /// Aggiorna la visualizzazione dell'orologio
    /// </summary>
    void UpdateClockDisplay()
    {
        if (clockText != null && timeManager != null)
        {
            clockText.text = timeManager.GetFormattedTime();
        }
    }

    /// <summary>
    /// Aggiorna la visualizzazione del giorno
    /// </summary>
    void UpdateDayDisplay()
    {
        if (dayText != null && config != null)
        {
            dayText.text = config.GetDayName();
        }
    }

    /// <summary>
    /// Mostra il messaggio di sveglia
    /// </summary>
    void ShowWakeUpMessage()
    {
        if (wakeUpPanel != null && wakeUpMessageText != null)
        {
            wakeUpMessageText.text = $"Buongiorno!\nOggi è {config.GetDayName()}";
            wakeUpPanel.SetActive(true);
            showingWakeUpMessage = true;
            wakeUpMessageTimer = wakeUpMessageDuration;
        }
    }

    /// <summary>
    /// Gestisce il timer del messaggio di sveglia
    /// </summary>
    void UpdateWakeUpMessage()
    {
        if (showingWakeUpMessage)
        {
            wakeUpMessageTimer -= Time.deltaTime;
            
            if (wakeUpMessageTimer <= 0f)
            {
                if (wakeUpPanel != null)
                {
                    wakeUpPanel.SetActive(false);
                }
                showingWakeUpMessage = false;
            }
        }
    }
}