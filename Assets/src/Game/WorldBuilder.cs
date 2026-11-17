using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Costruisce da codice negozio, casa, bar e piazza (solo primitive) e riempie WorldAnchors.
/// Luoghi separati nello spazio: negozio vicino all'origine, piazza a z negativo, casa a x=100, bar a x=200.
/// </summary>
public static class WorldBuilder
{
    const float ShopW = 6f, ShopD = 11.5f;
    static readonly Color Cream = new Color(.85f, .8f, .7f), Wood = new Color(.45f, .33f, .2f), Grey = new Color(.4f, .4f, .42f);
    static readonly Color Warm = new Color(1f, .9f, .7f), Glass = new Color(.6f, .8f, .9f);

    static readonly Dictionary<(Color, bool), Material> mats = new Dictionary<(Color, bool), Material>();
    static readonly List<(Material, Color)> emissiveMats = new List<(Material, Color)>();  // materiali ad emissione, colore originale
    static Shader lit;
    internal const float DoorX = 3.03f; // centro del varco d'ingresso del guscio v4 (x 2.5..3.57)
    static Transform root, plazaCasa, plazaBar;

    public static void Build()
    {
        mats.Clear();
        emissiveMats.Clear();
        root = new GameObject("Mondo").transform;
        var pt = new GameObject("Punti").transform;
        pt.SetParent(root);
        WorldAnchors.ShopEntrance = Pt("ShopEntrance", pt, DoorX, -1.5f, 180); // anche punto d'uscita in piazza
        WorldAnchors.ShopSpawn = Pt("ShopSpawn", pt, DoorX, .8f, 0);
        WorldAnchors.HomeSpawn = Pt("HomeSpawn", pt, 103.5f, 1.2f, 0);
        WorldAnchors.BarSpawn = Pt("BarSpawn", pt, 205f, 1.2f, 0);
        plazaCasa = Pt("PiazzaCasa", pt, -4f, -10f, 90);
        plazaBar = Pt("PiazzaBar", pt, 14f, -10f, 270);
        BuildShop();
        BuildHome();
        BuildBar();
        BuildPlaza();
        EnsurePlayer();
        VillageDressing.Build();
        root.gameObject.AddComponent<EmissionController>().Initialize(emissiveMats);
    }

    // ---------------- NEGOZIO ----------------

    // Altezza e tipo (0 muro, 1 scaffale, 2 banco/cassa, 3 altro) di ogni ostacolo di negozio_layout.json, nello stesso ordine.
    static readonly float[] ObsH = { 1.8f, 1.6f, 1.1f, 1f, 1.8f, .6f, 3f, 3f, 1.8f, 1.8f, .8f, .9f, 1.2f, 1.2f, 1f };
    static readonly int[] ObsKind = { 1, 1, 2, 2, 1, 3, 0, 0, 1, 1, 3, 2, 3, 3, 3 };

    static void BuildShop()
    {
        var p = Group("Negozio");
        Room(p, 0, 0, ShopW, ShopD, 3f, DoorX, Cream, new Color(.35f, .33f, .3f));
        var asset = Resources.Load<TextAsset>("negozio_layout");
        if (asset == null) { Debug.LogError("WorldBuilder: manca Resources/negozio_layout.json"); return; }

        // ostacoli_xz = [xmin,zmin,xmax,zmax]. Nel json +x è a sinistra di chi entra: lo specchio così la cassa sta a sinistra dell'ingresso.
        string s = asset.text;
        int a = s.IndexOf("ostacoli_xz"), b = s.IndexOf("istanze");
        var n = new List<float>();
        foreach (Match m in Regex.Matches(s.Substring(a, b - a), @"-?\d+(\.\d+)?")) n.Add(float.Parse(m.Value, CultureInfo.InvariantCulture));
        var O = new List<Rect>();
        for (int i = 0; i + 3 < n.Count; i += 4) O.Add(new Rect(ShopW - n[i + 2], n[i + 1], n[i + 2] - n[i], n[i + 3] - n[i + 1])); // y = z
        // la scrivania del magazzino (10) nel json sta a 20 cm dietro la porta e la chiude: la si sposta in fondo al corridoio tra i bancali e il congelatore
        if (O.Count > 10) O[10] = new Rect(1.3f, 10.4f, .9f, .7f);
        // varco della porta 1,1 m (era 0,9: con la capsula del giocatore si passava per 2 cm) e scaffale di fondo che non lo restringe
        if (O.Count > 6) O[6] = new Rect(1.15f, 7.9f, ShopW - 1.15f, .2f);
        if (O.Count > 4) O[4] = new Rect(1.3f, 7.45f, 4.1f, .45f);
        // il bancale 14 lasciava 50 cm tra sé e il 13: spostato a nord, il passo verso la zona di scarico diventa di 1,1 m
        if (O.Count > 14) O[14] = new Rect(2.4f, 10.4f, 1.2f, .7f);
        var cols = new[] { Cream, Wood, new Color(.8f, .8f, .78f), Grey };
        var boxes = new List<GameObject>();
        var slots = new List<Transform>();
        var sp = Group("ShelfSlots", p);
        for (int i = 0; i < O.Count && i < ObsH.Length; i++)
            boxes.Add(Box("Ostacolo" + i, p, new Vector3(O[i].center.x, ObsH[i] / 2, O[i].center.y), new Vector3(O[i].width, ObsH[i], O[i].height), cols[ObsKind[i]]));

        // modelli v4: guscio del negozio e del magazzino al posto di pavimento, muri e soffitto (restano solo i collider); l'esterno è di VillageDressing.
        // L'import .obj specchia già x: il modello sta a x = ShopW + x_modello, quindi senza rotazione combacia con lo specchio di WorldBuilder.
        var shell = Model("negozio_guscio", p, new Vector3(ShopW, 0, 0));
        var shell2 = Model("magazzino", p, new Vector3(ShopW, 0, 0));
        if (shell != null && shell2 != null)
        {
            foreach (var r in p.GetComponentsInChildren<Renderer>()) if (r.transform != shell.transform && r.transform != shell2.transform && !r.transform.IsChildOf(shell.transform) && !r.transform.IsChildOf(shell2.transform)) r.enabled = false;
            var js = asset.text;
            int ia = js.IndexOf('[', js.IndexOf("\"istanze\"")), ie = js.LastIndexOf(']');
            foreach (var it in JsonUtility.FromJson<Instances>("{\"i\":" + js.Substring(ia, ie - ia + 1) + "}").i) PlaceInstance(p, it, O, sp, slots);
        }
        else foreach (var bx in boxes) bx.GetComponent<Renderer>().enabled = true;

        // porta del magazzino a battente nel varco del muro divisorio (x 0.05..1.15, z 8)
        // cardine a ovest: aperta ruota di 90 gradi verso il corridoio e sta contro il muro, lasciando libero tutto il varco
        var hinge = Group("PortaMagazzino", p);
        hinge.position = new Vector3(.05f, 0, 8f);
        hinge.gameObject.AddComponent<SwingDoor>().label = "la porta del magazzino";
        var pannello = Box("Pannello", hinge, new Vector3(.6f, 1.1f, 8f), new Vector3(1.1f, 2.2f, .08f), Wood);
        var portaMod = shell != null ? Model("porta_magazzino", hinge, new Vector3(.9f, 0, 0)) : null; // foglio 0,9 m, cardine ad ovest (il modello ha il suo a est: solo la maniglia cambia lato)
        if (portaMod != null) pannello.GetComponent<Renderer>().enabled = false;
        var serr = Box("Serranda", p, new Vector3(3.8f, 1.1f, 11.45f), new Vector3(1.6f, 2.2f, .08f), Grey);
        if (shell != null && shell2 != null && p.Find("serranda") != null) serr.GetComponent<Renderer>().enabled = false;

        // porte verso la piazza: una per lato del varco
        MakeDoor(new Vector3(DoorX, 1.1f, -.05f), new Vector3(1, 2.2f, .1f), "Esci in piazza", WorldAnchors.ShopEntrance, Glass, p);
        MakeDoor(new Vector3(DoorX, 1.1f, -.15f), new Vector3(1, 2.2f, .1f), "Entra nel negozio", WorldAnchors.ShopSpawn, Glass, p);
        // porta a vetri del modello, chiusa nel varco del guscio (world x 2,5..3,57: il json la mette 50 cm a est del varco 2..3 della facciata esterna;
        // i due box sopra restano come collider e interazione, il giocatore passa teletrasportato dalla dissolvenza)
        if (shell != null && Model("porta_vetro", p, new Vector3(3.57f, 0, 0)) != null)
            foreach (var d in p.GetComponentsInChildren<Door>()) d.GetComponent<Renderer>().enabled = false;

        // cassa, fila, punti per i clienti
        WorldAnchors.Register = Pt("Register", p, 2.4f, 1.1f, 270);
        WorldAnchors.QueuePoints = new[] { Pt("Coda0", p, 2.4f, 1.1f, 270), Pt("Coda1", p, 2.4f, 1.8f, 270), Pt("Coda2", p, 2.7f, 2.5f, 270) };
        // consegne sul pavimento davanti alla serranda (zona di scarico, 1,7 x 1,5 m): ci si arriva tra i bancali 13 e 14. Il registro degli ordini sta sulla scrivania.
        WorldAnchors.StoreroomDrop = Pt("StoreroomDrop", p, 3.8f, 9.7f);
        WorldAnchors.OrderBook = Pt("PuntoRegistroOrdini", p, O[10].center.x, O[10].center.y);
        WorldAnchors.OrderBook.position += Vector3.up * ObsH[10];
        var spots = new[] { new Vector2(4.96f, 3), new Vector2(4.96f, 5), new Vector2(4.96f, 6.8f), new Vector2(3f, 3.2f), new Vector2(3f, 4.9f),
                            new Vector2(2f, 7f), new Vector2(3.5f, 7f), new Vector2(4.5f, 7f) };
        WorldAnchors.ShelfSpots = new Transform[spots.Length];
        for (int i = 0; i < spots.Length; i++) WorldAnchors.ShelfSpots[i] = Pt("Spot" + i, p, spots[i].x, spots[i].y);

        // scaffali: senza modelli, mensole e punti dei prodotti da primitive ([0] a muro, [1] isola a due facce, [4] fondo)
        if (slots.Count == 0)
        {
            float[] levels = { .45f, .95f, 1.45f };
            Shelf(sp, slots, levels, new Vector3(O[0].xMin - .08f, 0, 2f), new Vector3(O[0].xMin - .08f, 0, 7.2f), 8);
            Shelf(sp, slots, levels, new Vector3(O[1].xMin - .08f, 0, 2.9f), new Vector3(O[1].xMin - .08f, 0, 5.3f), 4);
            Shelf(sp, slots, levels, new Vector3(O[1].xMax + .08f, 0, 2.9f), new Vector3(O[1].xMax + .08f, 0, 5.3f), 4);
            Shelf(sp, slots, levels, new Vector3(1.4f, 0, O[4].yMin - .08f), new Vector3(5f, 0, O[4].yMin - .08f), 5);
        }
        WorldAnchors.ShelfSlots = slots.ToArray();

        // luci a soffitto
        var pos = new[] { new Vector3(3, 2.7f, 1.5f), new Vector3(3, 2.7f, 4), new Vector3(3, 2.7f, 6.5f), new Vector3(3, 2.3f, 9) };
        var lights = new Light[pos.Length];
        for (int i = 0; i < pos.Length; i++)
        {
            lights[i] = Lamp(p, pos[i], Warm);
            Box("Plafoniera", p, new Vector3(pos[i].x, 2.95f, pos[i].z), new Vector3(1, .05f, .3f), Warm, true);
        }
        WorldAnchors.ShopLights = lights;
    }

    [System.Serializable] class Inst { public string modello; public float x, y, z, rotY, scala; }
    [System.Serializable] class Instances { public Inst[] i; }

    // Posa un'istanza di negozio_layout.json con la specchiatura x_unity = ShopW - x_json, rotY -> -rotY (l'import .obj specchia già la mesh).
    // Scrivania e bancale c2 del magazzino stanno dove li ha messi il playtest 2 (O[10], O[14]) per tenere liberi i passaggi.
    static void PlaceInstance(Transform p, Inst it, List<Rect> O, Transform sp, List<Transform> slots)
    {
        string m = it.modello;
        if (m == "magazzino" || m.StartsWith("porta_")) return; // guscio già posato, le porte sono in BuildShop (a battente / chiusa nel varco)
        m = m == "bancale_c1" ? "bancale" : m == "rack_mag_dx1" ? "rack_mag_dx0" : m.StartsWith("cestone") ? "cestone" : m;
        var pos = new Vector3(ShopW - it.x, it.y, it.z);
        float yaw = -it.rotY;
        if (m == "scrivania_mag" && O.Count > 10) pos = new Vector3(O[10].xMax, 0, O[10].yMin);
        else if (m == "sgabello" && it.z > 8) pos = new Vector3(O[10].center.x, 0, O[10].yMin - .4f);
        else if (m == "bancale_c2" && O.Count > 14) pos = new Vector3(O[14].center.x, 0, O[14].center.y);
        var g = Model(m, p, pos, yaw, it.scala);
        if (g == null || !m.StartsWith("sc_")) return;
        // prodotti sui ripiani del modello: ripiani misurati dalle mesh a 0,60 / 1,05 / 1,50 (isola 1,55), profondità 0,23 dal retro, due punti per ripiano
        foreach (float y in new[] { .6f, 1.05f, m.StartsWith("sc_is") ? 1.55f : 1.5f })
            foreach (float x in new[] { -.375f, -1.125f })
            {
                var t = new GameObject("Slot").transform;
                t.SetParent(sp);
                t.position = g.transform.TransformPoint(new Vector3(x, y, .23f));
                slots.Add(t);
            }
    }

    // Ripiani lungo una linea: per ogni livello una mensola e n punti equidistanti.
    static void Shelf(Transform sp, List<Transform> slots, float[] levels, Vector3 from, Vector3 to, int n)
    {
        float len = (to - from).magnitude;
        bool alongZ = Mathf.Approximately(from.x, to.x);
        foreach (float y in levels)
        {
            Box("Ripiano", sp, (from + to) / 2 + Vector3.up * (y - .04f), alongZ ? new Vector3(.16f, .04f, len + .4f) : new Vector3(len + .4f, .04f, .16f), Wood);
            for (int i = 0; i < n; i++)
            {
                var t = new GameObject("Slot").transform;
                t.SetParent(sp);
                t.position = Vector3.Lerp(from, to, n == 1 ? .5f : i / (n - 1f)) + Vector3.up * y;
                slots.Add(t);
            }
        }
    }

    // ---------------- CASA ----------------

    static void BuildHome()
    {
        var p = Group("Casa");
        Room(p, 100, 0, 107, 5, 2.6f, 103.5f, new Color(.8f, .75f, .6f), new Color(.45f, .35f, .25f));
        MakeDoor(new Vector3(103.5f, 1.1f, -.05f), new Vector3(1, 2.2f, .1f), "Esci di casa", plazaCasa, Wood, p);
        Box("CucinaPiano", p, new Vector3(102, .45f, 4.65f), new Vector3(3, .9f, .6f), new Color(.8f, .8f, .75f));
        Box("Frigo", p, new Vector3(100.6f, .9f, 4.6f), new Vector3(.8f, 1.8f, .7f), new Color(.9f, .9f, .9f));
        Box("Tavolo", p, new Vector3(104.5f, .4f, 3.2f), new Vector3(1.6f, .8f, 1), Wood);
        foreach (var c in new[] { new Vector2(103.4f, 3.2f), new Vector2(105.6f, 3.2f), new Vector2(104.5f, 2.4f), new Vector2(104.5f, 4f) })
            Box("Sedia", p, new Vector3(c.x, .25f, c.y), new Vector3(.4f, .5f, .4f), Wood);
        // specchio (quad con cornice) sulla parete nord: qui il giocatore vede la propria fronte
        Box("CorniceSpecchio", p, new Vector3(105.8f, 1.5f, 4.97f), new Vector3(.95f, 1.35f, .04f), Wood);
        var m = GameObject.CreatePrimitive(PrimitiveType.Quad);
        m.name = "Specchio";
        m.transform.SetParent(p);
        m.transform.position = new Vector3(105.8f, 1.5f, 4.94f);
        m.transform.localScale = new Vector3(.8f, 1.2f, 1);
        m.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.75f, .82f, .85f));
        Box("MobileTV", p, new Vector3(100.35f, .25f, 2f), new Vector3(.5f, .5f, 1.4f), Wood);
        Box("TV", p, new Vector3(100.3f, .85f, 2f), new Vector3(.15f, .6f, 1f), new Color(.3f, .4f, .5f), true);
        Box("Letto", p, new Vector3(106.1f, .25f, 1.2f), new Vector3(1.4f, .5f, 2f), new Color(.5f, .3f, .3f));
        Box("Cuscino", p, new Vector3(106.1f, .57f, .4f), new Vector3(.9f, .15f, .4f), Color.white);
        Lamp(p, new Vector3(103.5f, 2.3f, 2.5f), Warm);
    }

    // ---------------- BAR ----------------

    static void BuildBar()
    {
        var p = Group("Bar");
        Room(p, 200, 0, 210, 7, 3f, 205f, new Color(.35f, .22f, .18f), new Color(.3f, .25f, .2f));
        MakeDoor(new Vector3(205, 1.1f, -.05f), new Vector3(1, 2.2f, .1f), "Esci dal bar", plazaBar, Wood, p);
        Box("Bancone", p, new Vector3(205, .55f, 5.6f), new Vector3(6, 1.1f, .8f), Wood);
        Box("Mensola", p, new Vector3(205, 1f, 6.85f), new Vector3(6, 2f, .3f), new Color(.25f, .18f, .12f));
        Box("StriscaLuce", p, new Vector3(205, 1.9f, 6.68f), new Vector3(5.5f, .05f, .05f), new Color(1f, .7f, .3f), true);
        var seats = new List<Transform>();
        foreach (float x in new[] { 203f, 204.3f, 205.7f, 207f }) Stool(p, seats, x, 4.7f, 0);          // al bancone
        foreach (float x in new[] { 202f, 208f })
        {
            Box("Tavolino", p, new Vector3(x, .4f, 2.2f), new Vector3(.9f, .4f, .9f), Wood, false, PrimitiveType.Cylinder);
            Stool(p, seats, x - .8f, 2.2f, 90);
            Stool(p, seats, x + .8f, 2.2f, 270);
        }
        WorldAnchors.BarSeats = seats.ToArray();
        Lamp(p, new Vector3(205, 2.6f, 2.5f), new Color(1f, .75f, .45f));
        Lamp(p, new Vector3(205, 2.6f, 5f), new Color(1f, .75f, .45f));
    }

    static void Stool(Transform p, List<Transform> seats, float x, float z, float yaw)
    {
        Box("Sgabello", p, new Vector3(x, .3f, z), new Vector3(.35f, .3f, .35f), Grey, false, PrimitiveType.Cylinder);
        seats.Add(Pt("Posto", p, x, z, yaw));
    }

    // ---------------- PIAZZA ----------------

    static void BuildPlaza()
    {
        var p = Group("Piazza");
        Box("Suolo", p, new Vector3(5, -.75f, -12), new Vector3(42, 1.5f, 24), new Color(.45f, .43f, .4f));
        // muretti bassi (0,8 m; il salto è oltre: VillageDressing li alza con collider invisibili; il lato nord è il negozio, con due tratti ai lati)
        Box("Muretto", p, new Vector3(-16, .4f, -12), new Vector3(.4f, .8f, 24), Grey);
        Box("Muretto", p, new Vector3(26, .4f, -12), new Vector3(.4f, .8f, 24), Grey);
        Box("Muretto", p, new Vector3(5, .4f, -24), new Vector3(42, .8f, .4f), Grey);
        Box("Muretto", p, new Vector3(-8, .4f, 0), new Vector3(16, .8f, .4f), Grey);
        Box("Muretto", p, new Vector3(16, .4f, 0), new Vector3(20, .8f, .4f), Grey);
        // edifici: casa e bar (con porta); il resto lo fa VillageDressing
        Box("EdificioCasa", p, new Vector3(-10, 3, -10), new Vector3(8, 6, 8), new Color(.7f, .6f, .5f));
        MakeDoor(new Vector3(-5.95f, 1.1f, -10), new Vector3(.1f, 2.2f, 1), "Entra in casa", WorldAnchors.HomeSpawn, Wood, p);
        Box("EdificioBar", p, new Vector3(20, 3, -10), new Vector3(8, 6, 8), new Color(.5f, .35f, .3f));
        MakeDoor(new Vector3(15.95f, 1.1f, -10), new Vector3(.1f, 2.2f, 1), "Entra al bar", WorldAnchors.BarSpawn, Wood, p);
        // lampioni
        foreach (var c in new[] { new Vector2(-3, -6), new Vector2(9, -6), new Vector2(-3, -18), new Vector2(9, -18) })
        {
            if (Model("lampione", p, new Vector3(c.x, 0, c.y), 0, 1, true) == null)
            {
                Box("Palo", p, new Vector3(c.x, 2, c.y), new Vector3(.12f, 2, .12f), Grey, false, PrimitiveType.Cylinder);
                Box("Lampada", p, new Vector3(c.x, 4.1f, c.y), new Vector3(.4f, .4f, .4f), Warm, true, PrimitiveType.Sphere);
            }
            Lamp(p, new Vector3(c.x, 4f, c.y), Warm, GlobalConfig.Instance.streetLampIntensity, 9f);
        }
    }

    // ---------------- GIOCATORE ----------------

    static void EnsurePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
        {
            go = new GameObject("Player") { tag = "Player" };
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2; cc.radius = .3f; cc.center = Vector3.up; cc.stepOffset = .3f;
            var cam = new GameObject("Camera") { tag = "MainCamera" };
            cam.transform.SetParent(go.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.65f, 0);
            var c = cam.AddComponent<Camera>();
            c.nearClipPlane = .05f;
            cam.AddComponent<AudioListener>();
            go.AddComponent<FPSController>().SetCamera(cam.transform);
            go.AddComponent<PlayerInteraction>().playerCamera = c;
        }
        WorldAnchors.Player = go.transform;
        Door.Place(WorldAnchors.HomeSpawn); // la partita comincia al mattino, a casa
    }

    // ---------------- ATTREZZI ----------------

    internal static Transform Group(string name, Transform parent = null)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent != null ? parent : root);
        return t;
    }

    static Transform Pt(string name, Transform parent, float x, float z, float yaw = 0)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent);
        t.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, yaw, 0));
        return t;
    }

    internal static Material Mat(Color c, bool emit = false)
    {
        if (!mats.TryGetValue((c, emit), out var m) || m == null)
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(lit) { color = c };
            if (emit) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2f); if (c == VillageDressing.Lit) emissiveMats.Add((m, c * 2f)); } // solo finestre: luci interne, insegne e lampioni restano accesi
            mats[(c, emit)] = m;
        }
        return m;
    }

    internal static GameObject Box(string name, Transform p, Vector3 center, Vector3 size, Color c, bool emit = false, PrimitiveType type = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(p);
        g.transform.position = center;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = Mat(c, emit);
        return g;
    }

    // Istanzia Resources/Models/v4/<name> (o v3) (pos e yaw locali a parent). center: pos = centro in pianta e piede del modello (altrimenti pos = origine).
    // collider: BoxCollider dai bounds. Ritorna null se il modello manca: il chiamante tiene la primitiva.
    internal static GameObject Model(string name, Transform parent, Vector3 pos, float yaw = 0, float scale = 1, bool collider = false, bool center = false)
    {
        var prefab = Resources.Load<GameObject>("Models/v4/" + name) ?? Resources.Load<GameObject>("Models/v3/" + name);
        if (prefab == null) return null;
        var g = Object.Instantiate(prefab, parent, false);
        g.name = name;
        foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        Bounds b = default;
        bool any = false;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one);
                var q = g.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (any) b.Encapsulate(q); else { b = new Bounds(q, Vector3.zero); any = true; }
            }
        }
        var rot = Quaternion.Euler(0, yaw, 0);
        g.transform.localScale = Vector3.one * scale;
        g.transform.localRotation = rot;
        g.transform.localPosition = center && any ? pos - rot * (new Vector3(b.center.x, b.min.y, b.center.z) * scale) : pos;
        if (collider && any) { var bc = g.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; }
        return g;
    }

    internal static Light Lamp(Transform p, Vector3 pos, Color c, float intensity = -1, float range = -1)
    {
        var l = new GameObject("Luce").AddComponent<Light>();
        l.transform.SetParent(p);
        l.transform.position = pos;
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity >= 0 ? intensity : GlobalConfig.Instance.shopLightIntensity;
        l.range = range >= 0 ? range : GlobalConfig.Instance.shopLightRange;
        l.shadows = LightShadows.None;
        return l;
    }

    static void MakeDoor(Vector3 center, Vector3 size, string label, Transform target, Color c, Transform p)
    {
        var d = Box("Porta", p, center, size, c).AddComponent<Door>();
        d.label = label;
        d.target = target;
    }

    // Stanza rettangolare (x0..x1, z0..z1) con pavimento spesso (serve al controllo "a terra" di FPSController), soffitto e muri.
    // Il muro sud (z0) ha un varco largo 1 m a doorX alto 2,2 m.
    static void Room(Transform p, float x0, float z0, float x1, float z1, float h, float doorX, Color wall, Color floor)
    {
        const float t = .2f;
        float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2, w = x1 - x0 + 2 * t, d = z1 - z0 + 2 * t;
        Box("Pavimento", p, new Vector3(cx, -.75f, cz), new Vector3(w, 1.5f, d), floor);
        Box("Soffitto", p, new Vector3(cx, h + t / 2, cz), new Vector3(w, t, d), wall);
        Box("MuroO", p, new Vector3(x0 - t / 2, h / 2, cz), new Vector3(t, h, d), wall);
        Box("MuroE", p, new Vector3(x1 + t / 2, h / 2, cz), new Vector3(t, h, d), wall);
        Box("MuroN", p, new Vector3(cx, h / 2, z1 + t / 2), new Vector3(w, h, t), wall);
        float a = doorX - .5f, b = doorX + .5f;
        Box("MuroS1", p, new Vector3((x0 - t + a) / 2, h / 2, z0 - t / 2), new Vector3(a - x0 + t, h, t), wall);
        Box("MuroS2", p, new Vector3((b + x1 + t) / 2, h / 2, z0 - t / 2), new Vector3(x1 + t - b, h, t), wall);
        Box("Architrave", p, new Vector3(doorX, (2.2f + h) / 2, z0 - t / 2), new Vector3(1, h - 2.2f, t), wall);
    }
}
