using UnityEngine;
using System;

/// <summary>
/// Gestisce il ciclo giorno/notte e il passaggio del tempo
/// </summary>
public class TimeManager : MonoBehaviour
{
    private GlobalConfig config;
    private Light directionalLight;
    
    // Eventi
    public static event Action OnWakeUp;
    public static event Action OnShopOpenTime;
    public static event Action OnLunchCloseTime;
    public static event Action OnAfternoonOpenTime;
    public static event Action OnEveningCloseTime;
    public static event Action OnBarOpenTime;
    public static event Action OnNewDay;

    private bool hasWokenUp = false;
    private bool hasNotifiedMorningOpen = false;
    private bool hasNotifiedLunchClose = false;
    private bool hasNotifiedAfternoonOpen = false;
    private bool hasNotifiedEveningClose = false;
    private bool hasNotifiedBarOpen = false;

    void Start()
    {
        config = GlobalConfig.Instance;
        directionalLight = FindObjectOfType<Light>();
        
        if (directionalLight == null)
        {
            Debug.LogWarning("Nessuna Directional Light trovata nella scena!");
        }

        // Imposta l'orario di sveglia
        config.currentHour = config.wakeUpHour;
        config.currentMinute = config.wakeUpMinute;
        config.playerIsAwake = false;
    }

    void Update()
    {
        UpdateTime();
        UpdateLighting();
        CheckTimeEvents();
    }

    /// <summary>
    /// Aggiorna il tempo di gioco
    /// </summary>
    void UpdateTime()
    {
        float realSecondsPerGameHour = 3600f / config.timeScale;
        float gameMinutesPerFrame = (Time.deltaTime / realSecondsPerGameHour) * 60f;

        config.currentMinute += gameMinutesPerFrame;

        if (config.currentMinute >= 60f)
        {
            config.currentMinute -= 60f;
            config.currentHour += 1f;

            if (config.currentHour >= 24f)
            {
                config.currentHour -= 24f;
                config.NextDay();
                ResetDailyFlags();
                OnNewDay?.Invoke();
            }
        }
    }

    /// <summary>
    /// Aggiorna l'illuminazione in base all'ora
    /// </summary>
    void UpdateLighting()
    {
        if (directionalLight == null) return;

        float hour = config.currentHour + (config.currentMinute / 60f);
        
        // Calcola l'interpolazione tra giorno e notte
        float dayNightBlend = 0f;
        
        if (hour >= 6f && hour < 20f) // Giorno (6:00 - 20:00)
        {
            dayNightBlend = 1f;
        }
        else if (hour >= 20f && hour < 22f) // Tramonto (20:00 - 22:00)
        {
            dayNightBlend = 1f - ((hour - 20f) / 2f);
        }
        else if (hour >= 4f && hour < 6f) // Alba (4:00 - 6:00)
        {
            dayNightBlend = (hour - 4f) / 2f;
        }
        else // Notte
        {
            dayNightBlend = 0f;
        }

        // Applica il colore e l'intensità della luce
        directionalLight.color = Color.Lerp(config.nightLightColor, config.dayLightColor, dayNightBlend);
        directionalLight.intensity = Mathf.Lerp(config.nightLightIntensity, config.dayLightIntensity, dayNightBlend);

        // Rotazione del sole
        float sunAngle = (hour / 24f) * 360f - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
    }

    /// <summary>
    /// Controlla gli eventi temporali
    /// </summary>
    void CheckTimeEvents()
    {
        float currentTime = config.currentHour + (config.currentMinute / 60f);

        // Sveglia
        if (!hasWokenUp && currentTime >= config.wakeUpHour)
        {
            hasWokenUp = true;
            config.playerIsAwake = true;
            OnWakeUp?.Invoke();
            Debug.Log($"Buongiorno! Oggi è {config.GetDayName()}");
        }

        // Apertura mattutina supermercato
        if (!hasNotifiedMorningOpen && currentTime >= config.morningOpenTime)
        {
            hasNotifiedMorningOpen = true;
            OnShopOpenTime?.Invoke();
            Debug.Log("È ora di aprire il supermercato!");
        }

        // Chiusura pranzo
        if (!hasNotifiedLunchClose && currentTime >= config.lunchCloseTime)
        {
            hasNotifiedLunchClose = true;
            OnLunchCloseTime?.Invoke();
            Debug.Log("Orario di chiusura pranzo!");
        }

        // Riapertura pomeridiana
        if (!hasNotifiedAfternoonOpen && currentTime >= config.afternoonOpenTime)
        {
            hasNotifiedAfternoonOpen = true;
            OnAfternoonOpenTime?.Invoke();
            Debug.Log("È ora di riaprire il supermercato!");
        }

        // Chiusura serale
        if (!hasNotifiedEveningClose && currentTime >= config.eveningCloseTime)
        {
            hasNotifiedEveningClose = true;
            OnEveningCloseTime?.Invoke();
            Debug.Log("Orario di chiusura! Puoi andare al bar.");
        }

        // Apertura bar
        if (!hasNotifiedBarOpen && currentTime >= config.barOpenTime)
        {
            hasNotifiedBarOpen = true;
            config.isBarOpen = true;
            OnBarOpenTime?.Invoke();
            Debug.Log("Il bar è aperto!");
        }
    }

    /// <summary>
    /// Reset dei flag giornalieri
    /// </summary>
    void ResetDailyFlags()
    {
        hasWokenUp = false;
        hasNotifiedMorningOpen = false;
        hasNotifiedLunchClose = false;
        hasNotifiedAfternoonOpen = false;
        hasNotifiedEveningClose = false;
        hasNotifiedBarOpen = false;
        config.playerIsAwake = false;
        config.isShopOpen = false;
        config.isBarOpen = false;
    }

    /// <summary>
    /// Ritorna l'ora formattata come stringa
    /// </summary>
    public string GetFormattedTime()
    {
        int hour = Mathf.FloorToInt(config.currentHour);
        int minute = Mathf.FloorToInt(config.currentMinute);
        return $"{hour:D2}:{minute:D2}";
    }

    /// <summary>
    /// Imposta un nuovo orario di sveglia (da chiamare la sera prima)
    /// </summary>
    public void SetWakeUpTime(float hour, float minute)
    {
        config.wakeUpHour = hour;
        config.wakeUpMinute = minute;
        Debug.Log($"Sveglia impostata per le {hour:00}:{minute:00}");
    }
}