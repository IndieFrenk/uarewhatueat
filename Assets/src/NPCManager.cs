using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clienti del negozio: ogni tanto entra un paesano non preso, gira tra gli scaffali, prende i prodotti
/// che gli piacciono (secondo le sue abitudini) se il prezzo è accettabile, fa la fila e paga alla cassa.
/// Movimento a waypoint (niente NavMesh). Contiene anche Customer e CashRegister.
/// </summary>
public class NPCManager : MonoBehaviour
{
    public readonly List<Customer> Queue = new List<Customer>();   // [0] = primo alla cassa
    public readonly List<Customer> Active = new List<Customer>();
    readonly HashSet<string> seen = new HashSet<string>();         // chi è già venuto in questa fase
    string seenKey = "";
    float nextSpawn;
    bool registerReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(NPCManager)).AddComponent<NPCManager>();

    /// <summary>Negozio aperto ai clienti: fase di negozio e apertura attiva.</summary>
    public static bool Open
    {
        get
        {
            var s = GameState.Current;
            return s != null && GlobalConfig.Instance != null && GlobalConfig.Instance.isShopOpen
                && (s.Phase == GamePhase.ShopMorning || s.Phase == GamePhase.ShopAfternoon);
        }
    }

    public static float R() => (float)GameState.Current.Rng.NextDouble();
    public static int RInt(int min, int max) => GameState.Current.Rng.Next(min, max);

    void Update()
    {
        if (!registerReady && WorldAnchors.Register != null)
        {
            registerReady = true;
            var go = WorldAnchors.Register.gameObject;
            if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>().isTrigger = true;
            go.AddComponent<CashRegister>().mgr = this;
            // dal lato banco l'unico collider che il raggio vede è la cassa: anche lei batte lo scontrino (il trigger davanti è coperto dal cliente stesso)
            var cassa = go.transform.parent != null ? go.transform.parent.Find("Ostacolo3") : null;
            if (cassa != null && cassa.GetComponent<CashRegister>() == null) cassa.gameObject.AddComponent<CashRegister>().mgr = this;
        }

        if (!Open) { nextSpawn = 0; return; }
        if (nextSpawn == 0) nextSpawn = Time.time + 3f;
        if (Time.time < nextSpawn) return;
        var cfg = GlobalConfig.Instance;
        nextSpawn = Time.time + Mathf.Lerp(cfg.customerSpawnMin, cfg.customerSpawnMax, R());
        TrySpawn(cfg);
    }

    void TrySpawn(GlobalConfig cfg)
    {
        var s = GameState.Current;
        if (WorldAnchors.ShopEntrance == null || Active.Count >= cfg.maxCustomersInShop) return;
        string key = s.Day + "/" + s.Phase;
        if (key != seenKey) { seenKey = key; seen.Clear(); }

        var candidates = s.Villagers.FindAll(v => !v.Taken && !seen.Contains(v.Id) && !Active.Exists(c => c.V == v));
        if (candidates.Count == 0) return;
        var villager = candidates[RInt(0, candidates.Count)];
        seen.Add(villager.Id);
        Active.Add(Customer.Create(villager, this));
    }
}

/// <summary>Un cliente: capsula + testa col marchio. Salutabile con E.</summary>
public class Customer : Interactable
{
    public Villager V;
    public bool Paid;
    NPCManager mgr;
    readonly List<ShopItem> basket = new List<ShopItem>();
    bool leaving;
    float groundY;
    static Texture2D markTex;

    public override string Prompt => $"Saluta {V.Name}";

    static readonly (string role, string[] lines)[] Quips =
    {
        ("parroc", new[] { "Il Signore provvede. Lei ci metta il resto.", "Dio vede tutto, anche lo scontrino." }),
        ("macell", new[] { "Oggi la carne la compro io, per cambiare.", "Mi sento forte come un toro." }),
        ("bar", new[] { "Stasera passo da me, il caffè lo faccio meglio io.", "Il bancone non si pulisce da solo." }),
        ("oste", new[] { "Il vino non lo prendo qui, non si offenda.", "Stasera chiudo tardi, come sempre." }),
        ("piant", new[] { "Le mie piante stanno meglio dei miei clienti.", "Qui dentro manca un po' di verde." }),
    };
    static readonly string[] Generic = { "Buona giornata.", "Strana aria, oggi.", "Si sente il freddo, eh?" };

    string Quip()
    {
        if (V.HasVisibleMark && NPCManager.R() < 0.25f) return "Mi guardi strano... ho qualcosa in faccia?";
        string role = (V.Role ?? "").ToLowerInvariant();
        foreach (var q in Quips)
            if (role.Contains(q.role)) return q.lines[NPCManager.RInt(0, q.lines.Length)];
        return Generic[NPCManager.RInt(0, Generic.Length)];
    }

    void Say(string text) => UIKit.Toast($"{V.Name}: \"{text}\"", 3f);

    public override void Interact()
    {
        string s = $"{V.Name}, {V.Role}.";
        if (V.HasVisibleMark) s += " Ha un segno sulla fronte.";
        UIKit.Toast($"{s}\n\"{Quip()}\"", 4f);
    }

    public static Customer Create(Villager v, NPCManager mgr)
    {
        var entrance = WorldAnchors.ShopEntrance;
        var root = new GameObject("Cliente " + v.Name);
        root.transform.position = entrance.position;
        var c = root.AddComponent<Customer>();
        c.V = v; c.mgr = mgr; c.groundY = entrance.position.y;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0, 0.85f, 0);
        body.transform.localScale = new Vector3(0.6f, 0.85f, 0.6f);
        body.GetComponent<Renderer>().material.color = Color.HSVToRGB(Mathf.Abs(v.Id.GetHashCode() % 100) / 100f, 0.4f, 0.6f);
        body.GetComponent<Collider>().isTrigger = true; // non blocca il giocatore, ma si può guardare per E

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0, 1.82f, 0);
        head.transform.localScale = Vector3.one * 0.35f;
        head.GetComponent<Renderer>().material.color = new Color(0.9f, 0.75f, 0.6f);
        Destroy(head.GetComponent<Collider>());

        // Marchio sulla fronte: quad scuro con simboli, alfa proporzionale a Mark. Visibile solo sopra soglia.
        var shader = Shader.Find("Sprites/Default");
        if (v.HasVisibleMark && shader != null)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.transform.SetParent(root.transform, false);
            mark.transform.localPosition = new Vector3(0, 1.9f, 0.15f);
            mark.transform.localRotation = Quaternion.Euler(0, 180, 0); // il quad guarda -Z, noi lo vogliamo avanti
            mark.transform.localScale = Vector3.one * 0.12f;
            Destroy(mark.GetComponent<Collider>());
            var mat = new Material(shader) { mainTexture = MarkTexture(), color = new Color(0.2f, 0.04f, 0.04f, Mathf.Clamp01(v.Mark)) };
            mark.GetComponent<Renderer>().material = mat;
        }

        c.StartCoroutine(c.Run());
        return c;
    }

    /// <summary>Simbolo segnaposto: anello con croce, procedurale.</summary>
    static Texture2D MarkTexture()
    {
        if (markTex != null) return markTex;
        const int n = 16;
        markTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                bool on = (r > 5.5f && r < 7.5f) || Mathf.Abs(dx) < 1f || Mathf.Abs(dy) < 1f;
                markTex.SetPixel(x, y, on && r < 7.5f ? Color.white : Color.clear);
            }
        markTex.Apply();
        return markTex;
    }

    void OnDestroy() { if (mgr != null) { mgr.Queue.Remove(this); mgr.Active.Remove(this); } }

    void Update()
    {
        // negozio chiuso mentre è dentro: rimette a posto la spesa ed esce
        if (!leaving && !NPCManager.Open)
        {
            StopAllCoroutines();
            ReturnBasket();
            StartCoroutine(Leave());
        }
    }

    // --- movimento ---

    void Step(Vector3 p)
    {
        var target = new Vector3(p.x, groundY, p.z);
        var d = target - transform.position;
        if (d.sqrMagnitude < 1e-4f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target, GlobalConfig.Instance.customerSpeed * Time.deltaTime);
    }

    bool Near(Vector3 p, float dist) => new Vector2(p.x - transform.position.x, p.z - transform.position.z).magnitude < dist;

    IEnumerator Walk(Vector3 p)
    {
        while (!Near(p, 0.2f)) { Step(p); yield return null; }
    }

    Vector3 QueuePos(int i)
    {
        var q = WorldAnchors.QueuePoints;
        if (q != null && q.Length > 0) return q[Mathf.Min(i, q.Length - 1)].position;
        var r = WorldAnchors.Register;
        return r != null ? r.position - r.forward * (1f + i * 0.8f) : transform.position;
    }

    /// <summary>È il primo in fila e sta davanti alla cassa.</summary>
    public bool AtFront => mgr.Queue.Count > 0 && mgr.Queue[0] == this && Near(QueuePos(0), 1f);

    // --- comportamento ---

    IEnumerator Run()
    {
        var cfg = GlobalConfig.Instance;
        var spots = WorldAnchors.ShelfSpots;
        if (WorldAnchors.ShopSpawn != null) yield return Walk(WorldAnchors.ShopSpawn.position); // prima dal varco della porta, non attraverso il muro
        if (spots != null && spots.Length > 0)
        {
            int visits = Mathf.Min(spots.Length, NPCManager.RInt(1, 4));
            for (int i = 0; i < visits; i++)
            {
                yield return Walk(spots[NPCManager.RInt(0, spots.Length)].position);
                yield return new WaitForSeconds(1f + NPCManager.R() * 1.5f);
                TryBuy(cfg);
            }
        }

        if (basket.Count == 0) { Say("Oggi non c'è niente che mi serva."); yield return Leave(); yield break; }
        if (mgr.Queue.Count >= cfg.maxQueueSize) { Say("Troppa gente, torno dopo."); ReturnBasket(); yield return Leave(); yield break; }

        mgr.Queue.Add(this);
        float patience = cfg.maxWaitTimeInQueue * 60f / Mathf.Max(1f, cfg.timeScale); // minuti di gioco -> secondi reali
        float t0 = Time.time;
        while (!Paid)
        {
            Step(QueuePos(mgr.Queue.IndexOf(this)));
            if (Time.time - t0 > patience)
            {
                Say("Non ho tempo da perdere!");
                ReturnBasket();
                yield return Leave();
                yield break;
            }
            yield return null;
        }
        yield return Leave();
    }

    IEnumerator Leave()
    {
        leaving = true;
        mgr.Queue.Remove(this);
        if (WorldAnchors.ShopSpawn != null) yield return Walk(WorldAnchors.ShopSpawn.position);
        yield return Walk(WorldAnchors.ShopEntrance.position);
        Destroy(gameObject);
    }

    /// <summary>Sceglie un prodotto in vendita (pesato sulle abitudini) e lo prende se il prezzo è giusto.</summary>
    void TryBuy(GlobalConfig cfg)
    {
        var pool = ShopItem.All.FindAll(i => i.isPlaced && i.HasPrice() && i.Product != null);
        var near = pool.FindAll(i => Near(i.transform.position, 4f));
        if (near.Count > 0) pool = near; // ponytail: se gli scaffali sono lontani dai punti ripiega su tutto il negozio
        if (pool.Count == 0) return;

        float total = 0;
        foreach (var i in pool) total += Weight(i);
        if (total <= 0) return;
        float r = NPCManager.R() * total;
        ShopItem pick = pool[pool.Count - 1];
        foreach (var i in pool) { r -= Weight(i); if (r <= 0) { pick = i; break; } }

        if (pick.assignedPrice > pick.Product.BasePrice * (1f + cfg.priceAcceptanceRange))
        {
            Say("Troppo caro!");
            return;
        }
        pick.gameObject.SetActive(false); // nel carrello: sparisce dallo scaffale e dallo stock
        basket.Add(pick);
        ShopStock.Refresh();
    }

    float Weight(ShopItem i) => V.Habits.TryGetValue(i.Product.Kind, out float w) ? Mathf.Max(0f, w) : 0.1f;

    void ReturnBasket()
    {
        foreach (var i in basket) if (i != null) i.gameObject.SetActive(true);
        basket.Clear();
        ShopStock.Refresh();
    }

    /// <summary>Alla cassa: incassa, la spesa finisce nella dieta del cliente.</summary>
    public void Pay()
    {
        var s = GameState.Current;
        int tot = 0;
        foreach (var i in basket)
        {
            tot += i.assignedPrice;
            var kind = i.Product.Kind;
            V.EatenToday.Add(kind);
            if (kind == FoodKind.Natural) s.NaturalSoldTotal++; else s.IndustrialSoldTotal++; // alcol e fumo = industriale
            Destroy(i.gameObject);
        }
        basket.Clear();
        s.Money.Earn(tot);
        Paid = true;
        ShopStock.Refresh();
        UIKit.Toast($"{V.Name} paga {tot} lire.\n\"{Quip()}\"", 4f);
    }
}

/// <summary>La cassa: il primo in fila paga.</summary>
public class CashRegister : Interactable
{
    public NPCManager mgr;
    public override string Prompt => "Batti lo scontrino";

    public override void Interact()
    {
        var first = mgr != null && mgr.Queue.Count > 0 ? mgr.Queue[0] : null;
        if (first == null || !first.AtFront) { UIKit.Toast("Nessuno alla cassa."); return; }
        first.Pay();
    }
}
