using UnityEngine;
using System;

/// <summary>
/// Configurazione globale del gioco - Tutte le variabili configurabili dall'Inspector
/// </summary>
public class GlobalConfig : MonoBehaviour
{
    public static GlobalConfig Instance { get; private set; }

    [Header("=== TEMPO E CICLO GIORNO/NOTTE ===")]
    [Tooltip("Velocità del tempo (1 = tempo reale, 60 = 1 minuto reale = 1 ora di gioco)")]
    public float timeScale = 60f;
    
    [Tooltip("Ora in cui il giocatore si sveglia (formato 24h)")]
    public float wakeUpHour = 7f;
    
    [Tooltip("Minuti di sveglia")]
    public float wakeUpMinute = 0f;

    [Header("=== ORARI SUPERMERCATO ===")]
    [Tooltip("Ora di apertura mattutina")]
    public float morningOpenTime = 8f;
    
    [Tooltip("Ora di chiusura pranzo")]
    public float lunchCloseTime = 12.5f; // 12:30
    
    [Tooltip("Ora di riapertura pomeridiana")]
    public float afternoonOpenTime = 15.5f; // 15:30
    
    [Tooltip("Ora di chiusura serale")]
    public float eveningCloseTime = 20f;

    [Header("=== ORARI BAR ===")]
    [Tooltip("Ora di apertura del bar")]
    public float barOpenTime = 20f;
    
    [Tooltip("Ora di chiusura del bar")]
    public float barCloseTime = 24f;

    [Header("=== CASSA E CLIENTI ===")]
    [Tooltip("Massimo numero di persone in fila alla cassa")]
    public int maxQueueSize = 3;
    
    [Tooltip("Tempo massimo di attesa prima di mettersi in fila (minuti di gioco)")]
    public float maxWaitTimeBeforeQueue = 2f;
    
    [Tooltip("Tempo massimo di attesa in fila (minuti di gioco)")]
    public float maxWaitTimeInQueue = 3f;
    
    [Tooltip("Distanza massima per interagire con NPC")]
    public float interactionDistance = 3f;
    
    [Tooltip("Range percentuale accettabile per il prezzo (es: 0.2 = ±20%)")]
    public float priceAcceptanceRange = 0.2f;

    [Header("=== NPC UNICI ===")]
    [Tooltip("Numero di NPC unici che entrano nel negozio")]
    public int uniqueNPCCount = 7;
    
    [Tooltip("Numero minimo di NPC al bar la sera")]
    public int minNPCsAtBar = 3;
    
    [Tooltip("Numero massimo di NPC al bar la sera")]
    public int maxNPCsAtBar = 4;

    [Header("=== GIORNO DELLA SETTIMANA ===")]
    [Tooltip("Giorno iniziale (0=Lunedì, 6=Domenica)")]
    public int startDayOfWeek = 0;

    [Header("=== OGGETTI E SCAFFALI ===")]
    [Tooltip("Distanza massima per afferrare oggetti")]
    public float grabDistance = 2.5f;
    
    [Tooltip("Velocità di rotazione degli oggetti tenuti")]
    public float objectRotationSpeed = 100f;
    
    [Tooltip("Offset dell'oggetto tenuto rispetto alla mano")]
    public Vector3 handOffset = new Vector3(0.3f, -0.3f, 0.5f);
    
    [Tooltip("Distanza minima scaffale per posizionare oggetti")]
    public float shelfPlacementDistance = 2f;
    
    [Tooltip("Distanza minima tra oggetti sullo scaffale")]
    public float minItemDistance = 0.2f;
    
    [Tooltip("Moltiplicatore per il prezzo suggerito (variazione casuale ±%)")]
    public float priceSuggestionVariation = 0.1f;
    
    [Tooltip("Forza del lancio degli oggetti")]
    public float throwForce = 10f;
    
    [Tooltip("Forza verso l'alto del lancio")]
    public float throwUpwardForce = 2f;

    [Header("=== PULIZIE (Venerdì) ===")]
    [Tooltip("Costo per chiamare la signora delle pulizie")]
    public float cleaningServiceCost = 50f;

    [Header("=== LIGHTING ===")]
    [Tooltip("Colore della luce diurna")]
    public Color dayLightColor = Color.white;
    
    [Tooltip("Intensità della luce diurna")]
    public float dayLightIntensity = 1f;
    
    [Tooltip("Colore della luce notturna")]
    public Color nightLightColor = new Color(0.2f, 0.2f, 0.4f);
    
    [Tooltip("Intensità della luce notturna")]
    public float nightLightIntensity = 0.3f;

    // VARIABILI RUNTIME (non modificabili dall'inspector)
    [HideInInspector] public float currentHour = 7f;
    [HideInInspector] public float currentMinute = 0f;
    [HideInInspector] public int currentDayOfWeek = 0;
    [HideInInspector] public bool isShopOpen = false;
    [HideInInspector] public bool isBarOpen = false;
    [HideInInspector] public bool playerIsAwake = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentDayOfWeek = startDayOfWeek;
        currentHour = wakeUpHour;
        currentMinute = wakeUpMinute;
    }

    /// <summary>
    /// Returns the current day of the week name
    /// </summary>
    public string GetDayName()
    {
        string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        return days[currentDayOfWeek];
    }

    /// <summary>
    /// Advances to the next day
    /// </summary>
    public void NextDay()
    {
        currentDayOfWeek = (currentDayOfWeek + 1) % 7;
    }

    /// <summary>
    /// Checks if it's Friday
    /// </summary>
    public bool IsFriday()
    {
        return currentDayOfWeek == 4;
    }
}