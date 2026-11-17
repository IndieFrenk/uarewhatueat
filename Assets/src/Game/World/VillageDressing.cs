using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Veste il paese attorno alla piazza (solo primitive): facciate, arredo, case su terreno in salita, pini e corona di montagne (Terrain da heightmap Perlin).
/// Tutto statico e senza collider, tranne l'arredo che il giocatore può urtare e le barriere invisibili sopra i muretti.
/// Si chiama in fondo a WorldBuilder.Build(); usa Box/Mat/Group/Lamp di WorldBuilder (cache materiali condivisa).
/// Stima draw call: ~40 materiali, gruppi combinati con StaticBatchingUtility (Facciate, Arredo, Fuori) + terreno + insegne.
/// </summary>
public static class VillageDressing
{
    // terreno: 300x300 m centrato sulla piazza, base a y=-12, altezza max 120
    const float TX = -150, TZ = -165, TS = 300, TB = -12, TH = 120;

    static readonly Color Stone = new Color(.55f, .53f, .5f), Frame = new Color(.88f, .85f, .78f), Dark = new Color(.1f, .12f, .16f),
        Lit = new Color(.35f, .26f, .12f), Brick = new Color(.45f, .2f, .15f), WoodD = new Color(.28f, .18f, .1f), WoodL = new Color(.5f, .38f, .25f),
        Terra = new Color(.65f, .35f, .22f), Leaf = new Color(.2f, .42f, .2f), Slate = new Color(.32f, .3f, .32f), Tile = new Color(.62f, .27f, .2f),
        Asphalt = new Color(.2f, .2f, .22f), Paving = new Color(.5f, .48f, .44f), Red = new Color(.7f, .12f, .12f), Cream = new Color(.92f, .9f, .8f);
    static readonly Color[] Shut = { new Color(.2f, .35f, .22f), new Color(.4f, .25f, .15f), new Color(.3f, .2f, .15f) };
    static readonly Color[] Walls = { new Color(.78f, .7f, .58f), new Color(.72f, .62f, .5f), new Color(.8f, .76f, .68f), new Color(.62f, .55f, .5f), new Color(.85f, .8f, .7f), new Color(.75f, .6f, .5f) };
    static readonly Color[] Roofs = { new Color(.5f, .25f, .2f), new Color(.42f, .22f, .18f), new Color(.35f, .3f, .3f) };

    static System.Random rnd;
    static Transform signs;
    static readonly List<Vector3> taken = new List<Vector3>(); // x, z, raggio: dove non piantare alberi

    public static void Build()
    {
        rnd = new System.Random(1990);
        taken.Clear();
        signs = WorldBuilder.Group("Insegne"); // fuori dai gruppi combinati: TextMesh
        var fac = WorldBuilder.Group("Facciate");
        var arr = WorldBuilder.Group("Arredo");
        var fuo = WorldBuilder.Group("Fuori");
        Barriere(arr);
        Facciate(fac);
        Arredo(arr);
        Terreno(fuo);
        Strada(fuo);
        Case(fuo);
        Pini(fuo);
        StaticBatchingUtility.Combine(fac.gameObject);
        StaticBatchingUtility.Combine(arr.gameObject);
        StaticBatchingUtility.Combine(fuo.gameObject);
    }

    // ---------------- ATTREZZI ----------------

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Quaternion E(float x, float y, float z) => Quaternion.Euler(x, y, z);

    // Primitiva in coordinate locali di p, senza collider.
    static GameObject C(Transform p, Vector3 pos, Vector3 size, Color c, Quaternion? q = null, bool emit = false, PrimitiveType t = PrimitiveType.Cube)
    {
        var g = WorldBuilder.Box("d", p, Vector3.zero, size, c, emit, t);
        g.transform.localPosition = pos;
        g.transform.localRotation = q ?? Quaternion.identity;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static Transform G(Transform p, string name, float x, float y, float z, float yaw = 0)
    {
        var t = WorldBuilder.Group(name, p);
        t.localPosition = V(x, y, z);
        t.localRotation = E(0, yaw, 0);
        return t;
    }

    // Ingombro invisibile (collider) in coordinate locali di p.
    static void Blocker(Transform p, Vector3 pos, Vector3 size)
    {
        var t = new GameObject("Ingombro").transform;
        t.SetParent(p, false);
        t.localPosition = pos;
        t.gameObject.AddComponent<BoxCollider>().size = size;
    }

    // Altezza del terreno: la stessa riempie la heightmap e posa case e alberi.
    // Piazza e facciate stanno su un'area piatta (-0,4); fuori sale a 0,16 m/m (sud scende lungo la strada), poi le montagne da r>40.
    static float H(float x, float z)
    {
        float dx = Mathf.Max(Mathf.Abs(x - 5) - 21, 0);
        float dz = z > -12 ? Mathf.Max(z - 14, 0) : Mathf.Max(-42 - z, 0);
        float r = Mathf.Sqrt(dx * dx + dz * dz);
        float rate = Mathf.Lerp(.16f, -.07f, Mathf.InverseLerp(-4f, -30f, z));
        float h = -.4f + rate * r;
        h += Mathf.SmoothStep(0, 1, (r - 40) / 100f) * (45 + 45 * Mathf.PerlinNoise(x * .011f + 40, z * .011f + 7) + 8 * Mathf.PerlinNoise(x * .05f, z * .05f));
        h += Mathf.Clamp01(r / 20) * 1.2f * (Mathf.PerlinNoise(x * .09f, z * .09f) - .5f); // dossi
        // la stanza di casa (x 100..107, z 0..5) è chiusa: il terreno non deve entrarci
        float d = Mathf.Sqrt(Mathf.Pow(Mathf.Max(Mathf.Abs(x - 103.5f) - 8, 0), 2) + Mathf.Pow(Mathf.Max(Mathf.Abs(z - 2.5f) - 5, 0), 2));
        return Mathf.Lerp(-2f, h, Mathf.SmoothStep(0, 1, d / 30f));
    }

    // ---------------- BARRIERE ----------------

    // Il salto del giocatore è 2 m: sopra i muretti (0,8 m) un collider invisibile fino a 4 m.
    static void Barriere(Transform p)
    {
        var b = new[] { (V(-16, 2.4f, -12), V(.4f, 3.2f, 24)), (V(26, 2.4f, -12), V(.4f, 3.2f, 24)), (V(5, 2.4f, -24), V(42, 3.2f, .4f)),
                        (V(-8, 2.4f, 0), V(16, 3.2f, .4f)), (V(16, 2.4f, 0), V(20, 3.2f, .4f)) };
        foreach (var (c, s) in b) Blocker(p, c, s);
    }

    // ---------------- EDIFICI ----------------

    static void Win(Transform g, Vector3 pos, float yaw, bool far)
    {
        var q = E(0, yaw, 0);
        bool lit = rnd.NextDouble() < .25;
        C(g, pos + q * V(0, 0, .04f), V(1.1f, 1.5f, .08f), Frame, q);
        C(g, pos + q * V(0, 0, .09f), V(.78f, 1.2f, .04f), lit ? Lit : Dark, q, lit);
        if (far) return;
        C(g, pos + q * V(0, -.78f, .14f), V(1.3f, .1f, .3f), Stone, q);
        var sc = Shut[rnd.Next(Shut.Length)];
        foreach (int s in new[] { -1, 1 }) C(g, pos + q * V(s * .8f, 0, .09f), V(.5f, 1.45f, .05f), sc, q);
    }

    // Tetto a falde (due lastre ruotate), colmo e frontoni a triangolo (rombo schiacciato in un gruppo scalato); camino.
    static void Roof(Transform g, float y, float W, float D, Color roof, Color wall, bool ridgeZ, bool chim)
    {
        var q = E(0, ridgeZ ? 90 : 0, 0);
        float rise = D * .3f, half = D / 2 + .5f, a = Mathf.Atan2(rise, D / 2) * Mathf.Rad2Deg;
        float len = half / Mathf.Cos(a * Mathf.Deg2Rad), cy = y + rise * (.5f - .5f / D) + .07f;
        foreach (int s in new[] { 1, -1 })
            C(g, V(0, cy, 0) + q * V(0, 0, s * half / 2), V(W + .6f, .14f, len), roof, q * E(s * a, 0, 0));
        C(g, V(0, y + rise + .06f, 0), V(W + .6f, .12f, .35f), Slate, q);
        foreach (int s in new[] { 1, -1 })
        {
            var gp = G(g, "Frontone", 0, y, 0);
            gp.localPosition = V(0, y, 0) + q * V(s * (W / 2 - .1f), 0, 0);
            gp.localRotation = q;
            gp.localScale = V(1, rise / (D / 2), 1);
            C(gp, Vector3.zero, V(.2f, D / 1.4142f, D / 1.4142f), wall, E(45, 0, 0));
        }
        if (!chim) return;
        C(g, V(0, y, 0) + q * V(W * .28f, rise * .55f + .4f, -D * .12f), V(.55f, 1.6f, .55f), Brick);
        C(g, V(0, y, 0) + q * V(W * .28f, rise * .55f + 1.25f, -D * .12f), V(.7f, .12f, .7f), Slate);
    }

    // Edificio con la facciata verso +z locale (pos = centro dell'impronta a terra). far: finestre semplificate.
    static Transform House(Transform p, Vector3 pos, float yaw, float w, float d, float h, Color wall, Color roof, bool far = false, bool wins = true,
        bool sides = true, bool ridgeZ = false, bool body = true, bool ground = true, bool chim = true)
    {
        var g = G(p, "Edificio", pos.x, pos.y, pos.z, yaw);
        if (body)
        {
            C(g, V(0, h / 2 - 1.5f, 0), V(w, h + 3, d), wall);
            C(g, V(0, .3f, 0), V(w + .1f, .6f, d + .1f), Stone); // zoccolo
        }
        Roof(g, h, ridgeZ ? d : w, ridgeZ ? w : d, roof, wall, ridgeZ, chim);
        if (!wins) return g;
        int nx = Mathf.Max(2, 2 * Mathf.RoundToInt(w / 6f)), nz = Mathf.Max(1, Mathf.RoundToInt(d / 3.5f)), fl = Mathf.Max(1, Mathf.RoundToInt(h / 3f));
        for (int f = 0; f < fl; f++)
        {
            float y = 1.7f + 3 * f;
            for (int i = 0; i < nx; i++)
                if (f > 0 || ground) Win(g, V((i + .5f) * w / nx - w / 2, y, d / 2), 0, far);
            if (sides)
                for (int i = 0; i < nz; i++)
                {
                    float z = (i + .5f) * d / nz - d / 2;
                    Win(g, V(w / 2, y, z), 90, far);
                    Win(g, V(-w / 2, y, z), -90, far);
                }
        }
        if (fl > 1 && !far) // porta-finestra con balcone sopra l'ingresso
        {
            Win(g, V(0, 4.7f, d / 2), 0, false);
            C(g, V(0, 3.85f, d / 2 + .45f), V(1.9f, .12f, .9f), Stone);
            C(g, V(0, 4.3f, d / 2 + .88f), V(1.9f, .6f, .04f), Dark);
            C(g, V(0, 4.65f, d / 2 + .88f), V(1.9f, .06f, .06f), Dark);
            foreach (int s in new[] { -1, 1 }) C(g, V(s * .93f, 4.3f, d / 2 + .45f), V(.05f, .9f, .9f), Dark);
        }
        return g;
    }

    // Tendone a strisce (due colori alternati), inclinato verso fuori.
    static void Awning(Transform g, float x, float y, float z, float w, Color a, Color b)
    {
        for (int i = 0; i < 6; i++)
            C(g, V(x + (i + .5f) * w / 6 - w / 2, y, z + .55f), V(w / 6, .05f, 1.15f), i % 2 == 0 ? a : b, E(15, 0, 0));
    }

    // Insegna: tavola + TextMesh (leggibile da +z della facciata). Si adatta alla tavola in base ai caratteri (stima, da rifinire nell'editor).
    static void Sign(Transform g, string text, Vector3 lp, float w, Color board, Color ink, float h = .9f)
    {
        var s = WorldBuilder.Group("Insegna", signs);
        s.position = g.TransformPoint(lp);
        s.rotation = g.rotation;
        Object.DestroyImmediate(WorldBuilder.Box("Tavola", s, s.position, V(w, h, .1f), board).GetComponent<Collider>());
        var t = new GameObject("Testo").AddComponent<TextMesh>();
        t.transform.SetParent(s, false);
        t.transform.localPosition = V(0, 0, .06f);
        t.transform.localRotation = E(0, 180, 0);
        var f = UIKit.DefaultFont;
        t.font = f;
        t.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        t.text = text;
        t.color = ink;
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.fontStyle = FontStyle.Bold;
        t.fontSize = 100;
        t.characterSize = Mathf.Min(h * .7f, w * .9f / (text.Length * .75f)) / 10f;
    }

    // Negozio al piano terra: porta finta, due vetrine accese, tendone, insegna.
    static Transform Shop(Transform p, Vector3 pos, float yaw, float w, float d, float h, Color wall, Color roof, string text, Color board, Color ink, Color a1, Color a2)
    {
        var g = House(p, pos, yaw, w, d, h, wall, roof, ground: false, sides: false);
        float z = d / 2;
        C(g, V(0, 1.15f, z + .03f), V(1.2f, 2.3f, .1f), WoodD);
        foreach (int s in new[] { -1, 1 })
        {
            C(g, V(s * w * .28f, 1.35f, z + .02f), V(w * .2f + .3f, 1.7f, .05f), Frame);
            C(g, V(s * w * .28f, 1.35f, z + .06f), V(w * .2f, 1.5f, .04f), Lit, null, true);
        }
        Awning(g, 0, 2.65f, z, w * .8f, a1, a2);
        Sign(g, text, V(0, 3.35f, z + .1f), w * .6f, board, ink);
        return g;
    }

    // ---------------- FACCIATE ----------------

    static void Facciate(Transform p)
    {
        // marciapiedi tra i muretti e le facciate (nord e sud)
        C(p, V(5, -.25f, 1), V(42, .5f, 1.6f), Paving);
        C(p, V(5, -.25f, -24.9f), V(42, .5f, 1.4f), Paving);

        // casa e bar: tetto, finestre, balconi, insegna sugli edifici esistenti (la porta resta dov'è)
        var casa = House(p, V(-10, 0, -10), 90, 8, 8, 6, new Color(.7f, .6f, .5f), Roofs[0], body: false);
        Sign(casa, "CASA", V(0, 3f, 4.1f), 2f, WoodD, Cream, .6f);
        var bar = House(p, V(20, 0, -10), 270, 8, 8, 6, new Color(.5f, .35f, .3f), Roofs[1], body: false);
        Awning(bar, 0, 2.65f, 4, 4f, new Color(.15f, .3f, .5f), Cream);
        Sign(bar, "BAR SPORT", V(0, 3.4f, 4.1f), 5.5f, new Color(.45f, .08f, .08f), Cream, 1f);

        // negozio: insegna sopra la porta
        Sign(WorldBuilder.Group("Ancora", p), "ALIMENTARI", V(3, 2.6f, -.3f), 4f, new Color(.25f, .4f, .3f), Cream, .7f);

        // fila nord (facce a sud, facciata a z=1,8)
        var mac = Shop(p, V(-9, 0, 5.8f), 180, 12, 8, 6.5f, new Color(.82f, .7f, .64f), Roofs[0], "MACELLERIA", new Color(.45f, .1f, .1f), Cream, Red, Cream);
        var fio = Shop(p, V(12, 0, 5.8f), 180, 8, 8, 6.5f, new Color(.7f, .78f, .65f), Roofs[2], "FIORI", new Color(.15f, .35f, .2f), Cream, new Color(.2f, .45f, .25f), Cream);
        Shop(p, V(21.5f, 0, 5.8f), 180, 8, 8, 6.5f, new Color(.85f, .78f, .6f), Roofs[1], "TABACCHI", new Color(.12f, .12f, .2f), Cream, new Color(.15f, .3f, .5f), Cream);
        // macelleria: bancone di marmo con panni rossi; fioraio: bancale con vasi e piante
        C(p, V(-9, .45f, 1), V(4, .9f, .55f), Cream);
        C(p, V(-9, .92f, 1), V(4.1f, .05f, .6f), Red);
        C(p, V(12, .25f, 1), V(7, .5f, .8f), WoodL);
        for (int i = 0; i < 7; i++) Pot(p, 8.9f + i, .5f, 1f, .42f);
        foreach (float x in new[] { 8.3f, 15.7f }) Pot(p, x, 0, 1f, .8f);

        // fila sud: chiesa al centro, osteria a destra, canonica a sinistra
        Chiesa(p);
        Shop(p, V(18, 0, -30.1f), 0, 12, 9, 6.5f, new Color(.8f, .7f, .55f), Roofs[1], "OSTERIA", new Color(.3f, .2f, .12f), Cream, Red, Cream);
        House(p, V(-12.5f, 0, -29.6f), 0, 7, 8, 6.5f, Walls[0], Roofs[2]);
        for (int i = 0; i < 3; i++) // tavolini fuori dall'osteria
        {
            float x = 14 + i * 3.5f;
            C(p, V(x, .36f, -24.9f), V(.08f, .72f, .08f), WoodD);
            C(p, V(x, .73f, -24.9f), V(.8f, .04f, .8f), WoodL, null, false, PrimitiveType.Cylinder);
            foreach (int s in new[] { -1, 1 }) C(p, V(x + s * .6f, .25f, -24.9f), V(.35f, .5f, .35f), WoodD);
        }
        foreach (var o in new[] { V(-9, 0, 5.8f), V(12, 0, 5.8f), V(21.5f, 0, 5.8f), V(3, 0, -33), V(18, 0, -30.1f), V(-12.5f, 0, -29.6f) }) taken.Add(V(o.x, o.z, 12));
    }

    static void Pot(Transform p, float x, float y, float z, float s)
    {
        C(p, V(x, y + s * .4f, z), V(s, s * .4f, s), Terra, null, false, PrimitiveType.Cylinder);
        C(p, V(x, y + s * 1.1f, z), V(s * 1.2f, s * .9f, s * 1.2f), Leaf, null, false, PrimitiveType.Sphere);
        var fc = new[] { new Color(.8f, .15f, .2f), new Color(.9f, .8f, .2f), new Color(.6f, .3f, .7f) };
        for (int i = 0; i < 3; i++)
            C(p, V(x + (float)(rnd.NextDouble() - .5) * s * .8f, y + s * (1.2f + (float)rnd.NextDouble() * .4f), z + (float)(rnd.NextDouble() - .5) * s * .6f), V(s * .3f, s * .3f, s * .3f), fc[rnd.Next(3)], null, false, PrimitiveType.Sphere);
    }

    // Chiesa: facciata a capanna sul lato sud della piazza (z=-25), campanile con orologio a sinistra, guglia a gradini.
    static void Chiesa(Transform p)
    {
        var wall = new Color(.82f, .8f, .72f);
        var g = House(p, V(3, 0, -33), 0, 10, 16, 7, wall, Roofs[1], wins: false, ridgeZ: true, chim: false);
        C(g, V(0, 1.8f, 8.03f), V(2.4f, 3.6f, .12f), WoodD);
        C(g, V(0, 3.7f, 8.05f), V(2.9f, .3f, .2f), Stone);
        C(g, V(0, .1f, 8.4f), V(3.4f, .2f, .8f), Stone); // gradino
        C(g, V(0, 7.9f, 8.03f), V(2f, .05f, 2f), Lit, E(90, 0, 0), true, PrimitiveType.Cylinder); // rosone
        foreach (int s in new[] { -1, 1 })
        {
            C(g, V(s * 3.3f, 3.3f, 8.03f), V(.9f, 2.4f, .06f), Dark);
            foreach (float z in new[] { -4f, 0f, 4f }) Win(g, V(s * 5, 3.8f, z), s * 90, false);
        }
        // campanile
        float tx = -6.6f, tz = 6.4f;
        C(g, V(tx, 6, tz), V(3.2f, 18, 3.2f), wall);
        C(g, V(tx, 15.15f, tz), V(3.6f, .3f, 3.6f), Stone);
        foreach (int s in new[] { -1, 1 })
        {
            C(g, V(tx, 12.8f, tz + s * 1.62f), V(1.2f, 1.8f, .06f), Dark);
            C(g, V(tx + s * 1.62f, 12.8f, tz), V(.06f, 1.8f, 1.2f), Dark);
        }
        for (int i = 0; i < 4; i++) C(g, V(tx, 15.8f + i * .85f, tz), V(2.8f - i * .75f, .9f, 2.8f - i * .75f), Slate, E(0, 45 * (i % 2), 0));
        C(g, V(tx, 19.5f, tz), V(.1f, 1f, .1f), Slate);
        C(g, V(tx, 19.7f, tz), V(.5f, .1f, .1f), Slate);
        // orologio: quadrante, lancette
        C(g, V(tx, 11, tz + 1.62f), V(2.1f, .05f, 2.1f), Dark, E(90, 0, 0), false, PrimitiveType.Cylinder);
        C(g, V(tx, 11, tz + 1.65f), V(1.9f, .05f, 1.9f), Cream, E(90, 0, 0), false, PrimitiveType.Cylinder);
        C(g, V(tx, 11.35f, tz + 1.69f), V(.06f, .7f, .02f), Dark);
        C(g, V(tx + .25f, 11, tz + 1.69f), V(.5f, .07f, .02f), Dark);
    }

    // ---------------- ARREDO PIAZZA ----------------

    static void Arredo(Transform p)
    {
        // pavimentazione: lastre 2,8 m di quattro grigi su fughe scure (il Suolo)
        var shades = new[] { new Color(.5f, .48f, .44f), new Color(.46f, .44f, .41f), new Color(.52f, .5f, .46f), new Color(.43f, .42f, .4f) };
        for (int i = 0; i < 14; i++)
            for (int j = 0; j < 8; j++)
                C(p, V(-14.3f + 3 * i, .015f, -1.5f - 3 * j), V(2.8f, .03f, 2.8f), shades[rnd.Next(4)]);

        // fontana (fuori dalla linea ingresso negozio - porte di casa e bar)
        var f = G(p, "Fontana", 5, 0, -15);
        C(f, V(0, .25f, 0), V(4.6f, .25f, 4.6f), Stone, null, false, PrimitiveType.Cylinder);
        C(f, V(0, .53f, 0), V(3.9f, .03f, 3.9f), new Color(.25f, .42f, .5f), null, false, PrimitiveType.Cylinder);
        C(f, V(0, 1.1f, 0), V(.7f, .6f, .7f), Stone, null, false, PrimitiveType.Cylinder);
        C(f, V(0, 1.7f, 0), V(1.9f, .1f, 1.9f), Stone, null, false, PrimitiveType.Cylinder);
        C(f, V(0, 1.8f, 0), V(1.6f, .02f, 1.6f), new Color(.25f, .42f, .5f), null, false, PrimitiveType.Cylinder);
        C(f, V(0, 2.2f, 0), V(.45f, .45f, .45f), Stone, null, false, PrimitiveType.Sphere);
        Blocker(f, V(0, .5f, 0), V(4.6f, 1f, 4.6f));

        // monumento ai caduti
        var m = G(p, "Monumento", -9.5f, 0, -19.5f);
        var sc = new Color(.7f, .68f, .62f);
        for (int i = 0; i < 3; i++) C(m, V(0, .15f + i * .3f, 0), V(3.4f - i * .6f, .3f, 3.4f - i * .6f), sc);
        C(m, V(0, 2.7f, 0), V(.9f, 3.6f, .9f), sc);
        C(m, V(0, 4.7f, 0), V(.6f, .6f, .6f), sc, E(0, 45, 0));
        C(m, V(0, 2.2f, .47f), V(.65f, .9f, .04f), Dark);
        Blocker(m, V(0, 1.5f, 0), V(3.4f, 3f, 3.4f));

        // panchine: sedile, schienale, due fianchi in ghisa
        foreach (var b in new[] { V(-1, -21.5f, 0), V(8, -21.5f, 0), V(22.5f, -17, 270), V(-13.5f, -18, 90), V(-12, -3, 180), V(10, -3, 180) })
        {
            var g = G(p, "Panchina", b.x, 0, b.y, b.z);
            C(g, V(0, .45f, 0), V(1.8f, .08f, .5f), WoodL);
            C(g, V(0, .8f, -.24f), V(1.8f, .5f, .06f), WoodL, E(-8, 0, 0));
            foreach (int s in new[] { -1, 1 }) C(g, V(s * .8f, .22f, 0), V(.08f, .45f, .45f), Dark);
            Blocker(g, V(0, .45f, 0), V(1.8f, .9f, .6f));
        }

        // cabina telefonica SIP: telaio, pannelli, vetri, apparecchio, tettuccio
        var c = G(p, "CabinaSIP", 22.5f, 0, -3.6f, 180);
        var sip = new Color(.4f, .45f, .43f);
        foreach (int s in new[] { -1, 1 }) foreach (int t in new[] { -1, 1 }) C(c, V(s * .5f, 1.15f, t * .5f), V(.07f, 2.3f, .07f), sip);
        C(c, V(0, 2.35f, 0), V(1.15f, .12f, 1.15f), sip);
        C(c, V(0, .45f, -.48f), V(1, .9f, .04f), sip);
        foreach (int s in new[] { -1, 1 })
        {
            C(c, V(s * .48f, .45f, 0), V(.04f, .9f, 1), sip);
            C(c, V(s * .48f, 1.5f, 0), V(.03f, 1.3f, .92f), new Color(.5f, .65f, .7f), null, true);
        }
        C(c, V(0, 1.5f, -.48f), V(.92f, 1.3f, .03f), new Color(.5f, .65f, .7f), null, true);
        C(c, V(0, 1.3f, -.4f), V(.3f, .4f, .15f), Dark);
        C(c, V(0, 2.1f, .5f), V(1, .25f, .05f), new Color(.9f, .75f, .15f));
        Blocker(c, V(0, 1.15f, 0), V(1.1f, 2.3f, 1.1f));

        // bacheca con manifesti
        var bk = G(p, "Bacheca", -13.5f, 0, -4.5f, 90);
        foreach (int s in new[] { -1, 1 }) C(bk, V(s * 1f, 1f, 0), V(.1f, 2f, .1f), WoodD);
        C(bk, V(0, 1.5f, 0), V(2.2f, 1.2f, .08f), WoodL);
        C(bk, V(0, 2.2f, 0), V(2.5f, .1f, .35f), WoodD);
        var pc = new[] { Cream, new Color(.8f, .7f, .35f), new Color(.7f, .3f, .25f), new Color(.8f, .8f, .85f) };
        for (int i = 0; i < 6; i++) C(bk, V(-.8f + (i % 3) * .8f, 1.2f + (i / 3) * .55f, .05f), V(.55f, .45f, .02f), pc[rnd.Next(4)], E(0, 0, (float)(rnd.NextDouble() * 8 - 4)));
        Blocker(bk, V(0, 1.1f, 0), V(2.4f, 2.2f, .4f));

        // fioriere: ai lati dell'ingresso del negozio, della casa e del bar
        foreach (var q in new[] { V(.8f, -1, 0), V(4.2f, -1, 0), V(-6.7f, -6.5f, 90), V(-6.7f, -13.5f, 90), V(16.7f, -6.5f, 90), V(16.7f, -13.5f, 90) })
            Planter(p, q.x, q.y, 1.1f, q.z);

        // utilitaria vecchia, parcheggiata di lato lungo il muretto sud
        Auto(G(p, "Utilitaria", 17.5f, 0, -21.3f, 90));

        // cassonetti
        var cs = G(p, "Cassonetti", -15f, 0, -21.5f, 90);
        foreach (float x in new[] { -.7f, .7f })
        {
            C(cs, V(x, .55f, 0), V(1.1f, 1.1f, .9f), new Color(.2f, .45f, .25f));
            C(cs, V(x, 1.14f, 0), V(1.15f, .08f, .95f), Dark);
        }
        Blocker(cs, V(0, .6f, 0), V(2.6f, 1.2f, .9f));
    }

    static void Planter(Transform p, float x, float z, float len, float yaw)
    {
        var g = G(p, "Fioriera", x, 0, z, yaw);
        C(g, V(0, .25f, 0), V(len, .5f, .55f), new Color(.55f, .5f, .45f));
        C(g, V(0, .5f, 0), V(len - .1f, .04f, .45f), WoodD);
        for (int i = 0; i < 3; i++) Pot(g, (i - 1) * len * .3f, .5f, 0, .35f);
        Blocker(g, V(0, .45f, 0), V(len, .9f, .6f));
    }

    // Auto vecchia (asse lungo = z locale): scocca, abitacolo, vetri, ruote, fari.
    static void Auto(Transform g)
    {
        var body = new Color(.55f, .6f, .5f);
        C(g, V(0, .55f, 0), V(1.55f, .6f, 3.6f), body);
        C(g, V(0, 1.1f, -.1f), V(1.4f, .5f, 1.9f), body);
        C(g, V(0, 1.12f, -.1f), V(1.42f, .36f, 1.55f), Dark);
        C(g, V(0, 1.4f, -.1f), V(1.3f, .08f, 1.7f), body);
        foreach (int s in new[] { -1, 1 })
            foreach (int t in new[] { -1, 1 }) C(g, V(s * .8f, .3f, t * 1.15f), V(.62f, .11f, .62f), Dark, E(0, 0, 90), false, PrimitiveType.Cylinder);
        foreach (int s in new[] { -1, 1 })
        {
            C(g, V(s * .5f, .62f, 1.82f), V(.3f, .2f, .05f), Cream, null, true);
            C(g, V(s * .5f, .62f, -1.82f), V(.3f, .15f, .05f), Red);
        }
        C(g, V(0, .3f, 1.83f), V(1.5f, .12f, .08f), Stone);
        C(g, V(0, .3f, -1.83f), V(1.5f, .12f, .08f), Stone);
        Blocker(g, V(0, .7f, 0), V(1.6f, 1.4f, 3.7f));
    }

    // ---------------- FUORI DAI MURETTI ----------------

    static Texture2D Noise(Color a, Color b)
    {
        var t = new Texture2D(16, 16) { wrapMode = TextureWrapMode.Repeat };
        for (int i = 0; i < 16; i++) for (int j = 0; j < 16; j++) t.SetPixel(i, j, Color.Lerp(a, b, (float)rnd.NextDouble()));
        t.Apply();
        return t;
    }

    // Corona di montagne: Terrain da codice, heightmap = H(). Tre strati (prato, roccia, neve) per altezza.
    static void Terreno(Transform p)
    {
        const int N = 257, A = 128;
        var td = new TerrainData { heightmapResolution = N, alphamapResolution = A };
        td.size = V(TS, TH, TS);
        var hm = new float[N, N];
        for (int zi = 0; zi < N; zi++)
            for (int xi = 0; xi < N; xi++) hm[zi, xi] = (H(TX + xi * TS / (N - 1), TZ + zi * TS / (N - 1)) - TB) / TH;
        td.SetHeights(0, 0, hm);
        td.terrainLayers = new[]
        {
            new TerrainLayer { diffuseTexture = Noise(new Color(.25f, .38f, .17f), new Color(.33f, .45f, .2f)), tileSize = new Vector2(8, 8) },
            new TerrainLayer { diffuseTexture = Noise(new Color(.38f, .35f, .3f), new Color(.5f, .46f, .4f)), tileSize = new Vector2(10, 10) },
            new TerrainLayer { diffuseTexture = Noise(new Color(.85f, .86f, .9f), new Color(.95f, .95f, 1f)), tileSize = new Vector2(10, 10) },
        };
        var am = new float[A, A, 3];
        for (int zi = 0; zi < A; zi++)
            for (int xi = 0; xi < A; xi++)
            {
                float x = TX + (xi + .5f) * TS / A, z = TZ + (zi + .5f) * TS / A, h = H(x, z) + 6 * (Mathf.PerlinNoise(x * .07f, z * .07f) - .5f);
                float rock = Mathf.SmoothStep(0, 1, (h - 14) / 14f), snow = Mathf.SmoothStep(0, 1, (h - 52) / 10f);
                am[zi, xi, 0] = (1 - rock) * (1 - snow);
                am[zi, xi, 1] = rock * (1 - snow);
                am[zi, xi, 2] = snow;
            }
        td.SetAlphamaps(0, 0, am);
        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Montagne";
        go.transform.SetParent(p);
        go.transform.position = V(TX, TB, TZ);
        Object.Destroy(go.GetComponent<TerrainCollider>()); // lontano e irraggiungibile
        var t = go.GetComponent<Terrain>();
        t.heightmapPixelError = 10;
        t.shadowCastingMode = ShadowCastingMode.Off;
        var sh = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (sh != null) t.materialTemplate = new Material(sh);
    }

    // Strada a est della piazza (x=29,5) che scende verso sud seguendo il terreno.
    static void Strada(Transform p)
    {
        for (float z = 20; z > -104; z -= 4)
        {
            float ya = H(29.5f, z), yb = H(29.5f, z - 4);
            C(p, V(29.5f, (ya + yb) / 2 + .06f, z - 2), V(4.5f, .1f, 4.2f), Asphalt, E(-Mathf.Atan2(ya - yb, 4) * Mathf.Rad2Deg, 0, 0));
        }
    }

    // File di case sul pendio, girate verso la piazza (4 file per lato; solo la prima con le finestre complete).
    static void Case(Transform p)
    {
        for (int k = 0; k < 4; k++)
        {
            float off = 12 * k;
            for (float z = -30; z < 14; z += 10.5f) { Casa(p, -22 - off, z, 90, k); Casa(p, 37 + off, z, 270, k); }
            for (float x = -16; x < 24; x += 10.5f) Casa(p, x, 22 + off, 180, k);
            for (float x = -22; x < 36; x += 11) if (Mathf.Abs(x - 29.5f) > 5) Casa(p, x, -50 - off, 0, k);
        }
    }

    static void Casa(Transform p, float x, float z, float yaw, int k)
    {
        x += (float)(rnd.NextDouble() - .5) * 3;
        z += (float)(rnd.NextDouble() - .5) * 3;
        float w = 6 + (float)rnd.NextDouble() * 3, d = 6 + (float)rnd.NextDouble() * 2, h = rnd.Next(2) == 0 ? 6.2f : 9.2f, r = Mathf.Max(w, d) / 2;
        float y = Mathf.Min(Mathf.Min(H(x - r, z - r), H(x + r, z - r)), Mathf.Min(H(x - r, z + r), H(x + r, z + r)));
        House(p, V(x, y, z), yaw + (float)(rnd.NextDouble() - .5) * 16, w, d, h, Walls[rnd.Next(Walls.Length)], Roofs[rnd.Next(Roofs.Length)], far: k > 0, sides: k < 2, chim: rnd.Next(3) > 0);
        taken.Add(V(x, z, 7));
    }

    // Pini: tronco + tre gradini di cilindri. Macchie di bosco (Perlin), sotto il limite della vegetazione, lontano da case, strada e piazza.
    static void Pini(Transform p)
    {
        int n = 0;
        var cols = new[] { new Color(.1f, .25f, .12f), new Color(.12f, .28f, .14f), new Color(.08f, .22f, .13f) };
        for (int i = 0; i < 1500 && n < 220; i++)
        {
            float x = Mathf.Lerp(-120, 125, (float)rnd.NextDouble()), z = Mathf.Lerp(-150, 110, (float)rnd.NextDouble());
            if (x > -30 && x < 45 && z > -55 && z < 30) continue;
            if (Mathf.Abs(x - 29.5f) < 4 && z < 22) continue;
            if (x > 80 && x < 125 && z > -30 && z < 40) continue;
            if (Mathf.PerlinNoise(x * .03f + 11, z * .03f) < .45f) continue;
            float y = H(x, z);
            if (y > 38) continue;
            bool ok = true;
            foreach (var t in taken) if ((new Vector2(t.x - x, t.y - z)).sqrMagnitude < t.z * t.z) { ok = false; break; }
            if (!ok) continue;
            n++;
            float s = 1.2f + (float)rnd.NextDouble();
            var g = G(p, "Pino", x, y, z);
            var col = cols[rnd.Next(cols.Length)];
            C(g, V(0, s * .8f, 0), V(.35f * s, .8f * s, .35f * s), WoodD, null, false, PrimitiveType.Cylinder);
            for (int j = 0; j < 3; j++) C(g, V(0, (2.2f + j * 1.3f) * s, 0), V((3.2f - j * .9f) * s, .6f * s, (3.2f - j * .9f) * s), col, null, false, PrimitiveType.Cylinder);
        }
    }
}
