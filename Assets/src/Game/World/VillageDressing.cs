using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Veste la piazza (solo primitive): facciate e arredo. Il "Fuori" (rilievo, edifici, strade, bosco) lo costruisce Canolo.
/// Tutto statico e senza collider, tranne l'arredo che il giocatore può urtare e le barriere invisibili sopra i muretti.
/// Si chiama in fondo a WorldBuilder.Build(); usa Box/Mat/Group/Lamp di WorldBuilder (cache materiali condivisa).
/// Stima draw call: ~40 materiali, gruppi combinati con StaticBatchingUtility (Facciate, Arredo) + insegne + Canolo.
/// </summary>
public static class VillageDressing
{
    internal static readonly Color Stone = new Color(.55f, .53f, .5f), Frame = new Color(.88f, .85f, .78f), Dark = new Color(.1f, .12f, .16f),
        Lit = new Color(.35f, .26f, .12f), Brick = new Color(.45f, .2f, .15f), WoodD = new Color(.28f, .18f, .1f), WoodL = new Color(.5f, .38f, .25f),
        Terra = new Color(.65f, .35f, .22f), Leaf = new Color(.2f, .42f, .2f), Slate = new Color(.32f, .3f, .32f), Tile = new Color(.62f, .27f, .2f),
        Asphalt = new Color(.2f, .2f, .22f), Paving = new Color(.5f, .48f, .44f), Red = new Color(.7f, .12f, .12f), Cream = new Color(.92f, .9f, .8f);
    static readonly Color[] Shut = { new Color(.2f, .35f, .22f), new Color(.4f, .25f, .15f), new Color(.3f, .2f, .15f) };
    internal static readonly Color[] Walls = { new Color(.78f, .7f, .58f), new Color(.72f, .62f, .5f), new Color(.8f, .76f, .68f), new Color(.62f, .55f, .5f), new Color(.85f, .8f, .7f), new Color(.75f, .6f, .5f) };
    internal static readonly Color[] Roofs = { new Color(.5f, .25f, .2f), new Color(.42f, .22f, .18f), new Color(.35f, .3f, .3f) };

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
        Barriere(arr);
        Facciate(fac);
        Arredo(arr);
        StaticBatchingUtility.Combine(fac.gameObject);
        StaticBatchingUtility.Combine(arr.gameObject);
        Canolo.Build(); // il "Fuori": Canolo Nuovo vero (rilievo, edifici, strade, bosco)
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

        Bottega(p);

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

    // Esterno della bottega (volume x 0..6, z 0..12, porta a x 2..3 su z=0): casa a due piani, abitazione sopra il negozio.
    // Muri sottili senza collider attaccati alle pareti interne (x -0,2..6,2; z -0,2..12,2): niente entra nella stanza, il varco della porta resta libero.
    // Coordinate locali del gruppo (yaw 180): +z guarda la piazza, lx = 3 - x mondo, lz = 6 - z mondo; la porta sta a lx 0..1.
    static void Bottega(Transform p)
    {
        var keep = rnd;
        rnd = new System.Random(5); // le finestre accese non spostano la sequenza casuale del resto del paese
        const float W = 6.9f, D = 12.9f, Ht = 6.3f, T = .25f;
        float hw = W / 2, hd = D / 2;
        var plaster = new Color(.88f, .72f, .48f);
        var brass = new Color(.72f, .55f, .2f);
        var g = House(p, V(3, 0, 6), 180, W, D, Ht, plaster, Roofs[0], wins: false, body: false, ridgeZ: true);

        // intonaco: lati, retro, facciata con varco (porta alta 2,2) e architrave
        float sc = hw - T / 2, cy = 2.65f, sh = 7.3f;
        foreach (int s in new[] { -1, 1 }) C(g, V(s * sc, cy, 0), V(T, sh, D - 2 * T), plaster);
        C(g, V(0, cy, -hd + T / 2), V(W, sh, T), plaster);
        C(g, V(-hw / 2, cy, hd - T / 2), V(hw, sh, T), plaster);
        C(g, V((1 + hw) / 2, cy, hd - T / 2), V(hw - 1, sh, T), plaster);
        C(g, V(.5f, 4.25f, hd - T / 2), V(1, 4.1f, T), plaster);
        // zoccolo in pietra (y -0,6..0,9)
        foreach (int s in new[] { -1, 1 }) C(g, V(s * (sc + .03f), .15f, 0), V(T + .06f, 1.5f, D + .06f), Stone);
        C(g, V(0, .15f, -hd + T / 2 - .03f), V(W - 2 * T, 1.5f, T + .06f), Stone);
        C(g, V(-(hw - T) / 2, .15f, hd - T / 2 + .03f), V(hw - T, 1.5f, T + .06f), Stone);
        C(g, V((1 + hw - T) / 2, .15f, hd - T / 2 + .03f), V(hw - T - 1, 1.5f, T + .06f), Stone);
        // porta: stipiti, architrave in legno, gradino
        foreach (float x in new[] { -.06f, 1.06f }) C(g, V(x, 1.1f, hd + .02f), V(.12f, 2.3f, .12f), WoodD);
        C(g, V(.5f, 2.26f, hd + .02f), V(1.3f, .12f, .14f), WoodD);
        C(g, V(.5f, .05f, hd + .3f), V(1.3f, .1f, .6f), Stone);

        // vetrine accese ai lati della porta, con merce sul ripiano
        void Vetrina(float x, float w)
        {
            C(g, V(x, 1.35f, hd + .03f), V(w + .3f, 1.7f, .06f), Frame);
            C(g, V(x, 1.35f, hd + .07f), V(w, 1.5f, .04f), Lit, null, true);
            C(g, V(x, 1.35f, hd + .1f), V(.05f, 1.5f, .03f), Frame);
            C(g, V(x, .55f, hd + .14f), V(w + .4f, .1f, .3f), Stone);
            C(g, V(x, .95f, hd + .13f), V(w, .04f, .2f), WoodL);
            var goods = new[] { Red, Leaf, new Color(.85f, .7f, .2f), new Color(.25f, .4f, .6f) };
            for (int i = 0; i < (int)(w / .3f); i++) C(g, V(x - w / 2 + .2f + i * .3f, 1.07f, hd + .13f), V(.18f, .24f, .14f), goods[i % goods.Length]);
        }
        Vetrina(2.45f, 1f);
        Vetrina(-2f, 1.8f);

        // tendone, insegna, cartello APERTO acceso, campanello con targa, cassetta della posta
        Awning(g, 0, 2.65f, hd, W - .4f, new Color(.2f, .4f, .25f), Cream);
        Sign(g, "ALIMENTARI", V(0, 3.55f, hd + .1f), 4.4f, new Color(.25f, .4f, .3f), Cream, .7f);
        Sign(g, "APERTO", V(1.45f, 1.55f, hd + .1f), .48f, new Color(.5f, .1f, .1f), Cream, .22f);
        C(g, V(1.45f, 1.4f, hd + .06f), V(.5f, .03f, .03f), new Color(.3f, .9f, .4f), null, true);
        C(g, V(-.3f, 1.3f, hd + .03f), V(.14f, .34f, .04f), brass);
        foreach (float y in new[] { 1.38f, 1.24f }) C(g, V(-.3f, y, hd + .06f), V(.06f, .06f, .03f), Cream);
        C(g, V(-.65f, 1.25f, hd + .07f), V(.34f, .28f, .14f), Red);
        C(g, V(-.65f, 1.3f, hd + .15f), V(.2f, .02f, .02f), Dark);
        Bici(g, -2f, hd + .5f);

        // piano di sopra: tre finestre con cassette di fiori sulla facciata, due per lato e sul retro, una nel frontone
        foreach (float x in new[] { -1.6f, .5f, 2.6f })
        {
            Win(g, V(x, 5, hd), 0, false);
            C(g, V(x, 4.4f, hd + .2f), V(1.2f, .25f, .3f), WoodL);
            C(g, V(x, 4.55f, hd + .2f), V(1.1f, .1f, .24f), Leaf);
            for (int i = 0; i < 4; i++) C(g, V(x - .4f + i * .27f, 4.65f, hd + .2f), V(.12f, .12f, .12f), i % 2 == 0 ? Red : Cream);
        }
        foreach (float z in new[] { -3.5f, 0, 3.5f })
            foreach (float y in new[] { 1.7f, 5 })
            {
                Win(g, V(hw, y, z), 90, false);
                Win(g, V(-hw, y, z), -90, false);
            }
        foreach (float x in new[] { -2f, 0, 2f })
            foreach (float y in new[] { 1.7f, 5 }) Win(g, V(x, y, -hd), 180, true);
        C(g, V(0, 7.1f, hd + .03f), V(.7f, .8f, .06f), Frame);
        C(g, V(0, 7.1f, hd + .07f), V(.5f, .6f, .04f), Lit, null, true);

        // antenna TV anni 90 sul colmo
        C(g, V(0, 9.6f, 2.5f), V(.05f, 2.4f, .05f), Slate);
        for (int i = 0; i < 3; i++) C(g, V(0, 9.4f + i * .35f, 2.5f), V(1f - i * .25f, .04f, .04f), Slate);

        taken.Add(V(3, 6, 12));
        rnd = keep;
    }

    // Bici appoggiata al muro (asse lungo lx), inclinata verso la facciata.
    static void Bici(Transform g, float x, float z)
    {
        var b = G(g, "Bici", x, 0, z);
        b.localRotation = E(-10, 0, 0);
        var fr = new Color(.15f, .4f, .5f);
        foreach (float wx in new[] { -.45f, .45f })
        {
            C(b, V(wx, .35f, 0), V(.7f, .03f, .7f), Dark, E(90, 0, 0), false, PrimitiveType.Cylinder);
            C(b, V(wx, .35f, 0), V(.58f, .036f, .58f), Frame, E(90, 0, 0), false, PrimitiveType.Cylinder);
        }
        void Bar(float x0, float y0, float x1, float y1, float t)
        {
            float dx = x1 - x0, dy = y1 - y0;
            C(b, V((x0 + x1) / 2, (y0 + y1) / 2, 0), V(Mathf.Sqrt(dx * dx + dy * dy), t, t), fr, E(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg));
        }
        Bar(-.45f, .35f, -.02f, .3f, .035f);
        Bar(-.02f, .3f, -.2f, .82f, .035f);
        Bar(-.2f, .82f, .33f, .78f, .035f);
        Bar(-.02f, .3f, .33f, .78f, .035f);
        Bar(.33f, .78f, .45f, .35f, .035f);
        Bar(-.45f, .35f, -.2f, .82f, .03f);
        Bar(.33f, .78f, .36f, .92f, .035f);
        C(b, V(.36f, .92f, 0), V(.04f, .04f, .5f), Dark);
        C(b, V(-.22f, .86f, 0), V(.26f, .05f, .12f), Dark);
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
}
