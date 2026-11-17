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

    /// <summary>true = l'orologio non scorre (momenti di respiro: mattina, pranzo, sera, bar). Lo imposta GameFlow.</summary>
    public static bool Paused;
    static TimeManager instance;

    /// <summary>Porta l'orologio a quest'ora (24h decimale) senza far scattare gli eventi degli orari già passati.</summary>
    public static void SetClock(float hour)
    {
        var c = GlobalConfig.Instance;
        if (c == null) return;
        c.currentHour = Mathf.Floor(hour);
        c.currentMinute = (hour - c.currentHour) * 60f;
        if (instance == null) return;
        instance.hasWokenUp = false; // scatta al prossimo frame: messaggio di sveglia
        instance.hasNotifiedMorningOpen = hour >= c.morningOpenTime;
        instance.hasNotifiedLunchClose = hour >= c.lunchCloseTime;
        instance.hasNotifiedAfternoonOpen = hour >= c.afternoonOpenTime;
        instance.hasNotifiedEveningClose = hour >= c.eveningCloseTime;
        instance.hasNotifiedBarOpen = hour >= c.barOpenTime;
        c.playerIsAwake = false;
    }

    private bool hasWokenUp = false;
    private bool hasNotifiedMorningOpen = false;
    private bool hasNotifiedLunchClose = false;
    private bool hasNotifiedAfternoonOpen = false;
    private bool hasNotifiedEveningClose = false;
    private bool hasNotifiedBarOpen = false;

    void Start()
    {
        instance = this;
        config = GlobalConfig.Instance;
        // il sole è la luce direzionale: FindFirstObjectByType<Light>() prendeva una luce del negozio e il sole restava sempre al massimo
        directionalLight = RenderSettings.sun;
        if (directionalLight == null)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { directionalLight = l; break; }
        
        if (directionalLight == null)
        {
            Debug.LogWarning("No Directional Light found in the scene!");
        }

        // Set wake up time
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
        if (Paused) return;
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
        directionalLight.intensity = Mathf.Lerp(config.nightLightIntensity * config.nightSunMultiplier, config.dayLightIntensity, dayNightBlend);

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

        // Wake up
        if (!hasWokenUp && currentTime >= config.wakeUpHour)
        {
            hasWokenUp = true;
            config.playerIsAwake = true;
            OnWakeUp?.Invoke();
            Debug.Log($"Good morning! Today is {config.GetDayName()}");
        }

        // Morning shop opening
        if (!hasNotifiedMorningOpen && currentTime >= config.morningOpenTime)
        {
            hasNotifiedMorningOpen = true;
            OnShopOpenTime?.Invoke();
            Debug.Log("Time to open the supermarket!");
        }

        // Lunch closing
        if (!hasNotifiedLunchClose && currentTime >= config.lunchCloseTime)
        {
            hasNotifiedLunchClose = true;
            OnLunchCloseTime?.Invoke();
            Debug.Log("Lunch break closing time!");
        }

        // Afternoon reopening
        if (!hasNotifiedAfternoonOpen && currentTime >= config.afternoonOpenTime)
        {
            hasNotifiedAfternoonOpen = true;
            OnAfternoonOpenTime?.Invoke();
            Debug.Log("Time to reopen the supermarket!");
        }

        // Evening closing
        if (!hasNotifiedEveningClose && currentTime >= config.eveningCloseTime)
        {
            hasNotifiedEveningClose = true;
            OnEveningCloseTime?.Invoke();
            Debug.Log("Closing time! You can go to the bar now.");
        }

        // Bar opening
        if (!hasNotifiedBarOpen && currentTime >= config.barOpenTime)
        {
            hasNotifiedBarOpen = true;
            config.isBarOpen = true;
            OnBarOpenTime?.Invoke();
            Debug.Log("The bar is now open!");
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
    /// Returns formatted time as string
    /// </summary>
    public string GetFormattedTime()
    {
        int hour = Mathf.FloorToInt(config.currentHour);
        int minute = Mathf.FloorToInt(config.currentMinute);
        return $"{hour:D2}:{minute:D2}";
    }

    /// <summary>
    /// Sets a new wake up time (to be called the night before)
    /// </summary>
    public void SetWakeUpTime(float hour, float minute)
    {
        config.wakeUpHour = hour;
        config.wakeUpMinute = minute;
        Debug.Log($"Alarm set for {hour:00}:{minute:00}");
    }
}