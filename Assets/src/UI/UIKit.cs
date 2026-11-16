using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI costruita da codice (niente prefab, niente TMP). Un canvas, notifiche e menu a scelte.
/// </summary>
public class UIKit : MonoBehaviour
{
    public static UIKit I { get; private set; }
    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public Canvas Canvas { get; private set; }
    Text toast;
    float toastUntil;
    GameObject menu;

    public static UIKit Ensure()
    {
        if (I != null) return I;
        var go = new GameObject("UIKit");
        I = go.AddComponent<UIKit>();
        I.Canvas = go.AddComponent<Canvas>();
        I.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        I.Canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        I.toast = Label(I.Canvas.transform, "", 32, TextAnchor.LowerCenter);
        Place(I.toast.rectTransform, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.25f));
        return I;
    }

    public static bool MenuOpen => I != null && I.menu != null;

    void Update()
    {
        if (toast.text != "" && Time.unscaledTime > toastUntil) toast.text = "";
    }

    /// <summary>Notifica in basso per qualche secondo.</summary>
    public static void Toast(string msg, float seconds = 4f)
    {
        Ensure();
        I.toast.text = msg;
        I.toastUntil = Time.unscaledTime + seconds;
    }

    /// <summary>Menu modale: titolo, testo e bottoni. Sblocca il cursore e blocca il giocatore finché non si sceglie.</summary>
    public static void Choices(string title, string body, string[] options, Action<int> onPick)
    {
        Ensure();
        CloseMenu();
        I.menu = Panel(I.Canvas.transform, new Color(0, 0, 0, 0.85f));
        Place((RectTransform)I.menu.transform, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f));
        var t = Label(I.menu.transform, title, 40, TextAnchor.UpperCenter);
        Place(t.rectTransform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));
        var b = Label(I.menu.transform, body, 28, TextAnchor.UpperLeft);
        Place(b.rectTransform, new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.84f));
        float h = 0.4f / Mathf.Max(options.Length, 4);
        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            var btn = Button(I.menu.transform, options[i], () => { CloseMenu(); onPick?.Invoke(idx); });
            float top = 0.42f - i * h;
            Place((RectTransform)btn.transform, new Vector2(0.1f, top - h + 0.01f), new Vector2(0.9f, top));
        }
        FPSController.InputBlocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void CloseMenu()
    {
        if (I == null || I.menu == null) return;
        Destroy(I.menu);
        I.menu = null;
        FPSController.InputBlocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- mattoncini ---

    public static Text Label(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = DefaultFont; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static GameObject Panel(Transform parent, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    public static GameObject Button(Transform parent, string text, Action onClick)
    {
        var go = Panel(parent, new Color(1, 1, 1, 0.12f));
        go.name = "Button";
        go.AddComponent<Button>().onClick.AddListener(() => onClick());
        var t = Label(go.transform, text, 26, TextAnchor.MiddleCenter);
        Place(t.rectTransform, Vector2.zero, Vector2.one);
        return go;
    }

    /// <summary>Ancora un rect in coordinate normalizzate dello schermo/genitore.</summary>
    public static void Place(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
