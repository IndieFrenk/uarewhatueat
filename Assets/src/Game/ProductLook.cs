using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aspetto dei prodotti: tabella id -> (forma, dimensioni, colori) e costruzione con sole primitive.
/// Industriale = colori sgargianti + etichetta col nome; naturale = colori terrosi, niente etichetta.
/// Il root ha solo il BoxCollider (misure reali del prodotto, pivot al centro); i pezzi sono figli senza collider.
/// </summary>
public static class ProductLook
{
    public enum Shape { Bottle, Can, Box, Bag, Log, Loaf, Ham, Crate, Eggs }

    // size = ingombro (larghezza x, altezza y, profondità z) in metri; k = collo (frazione di altezza) per Bottle, diametro frutti per Crate.
    // I ripiani hanno passo 0.5 m e profondità ~0.16 m: altezza <= 0.34, profondità <= 0.2.
    struct Look
    {
        public Shape S; public Vector3 Size; public Color Main, Accent; public string Label; public float K;
        public Look(Shape s, float w, float h, float d, Color main, Color accent, string label = null, float k = 0f)
        { S = s; Size = new Vector3(w, h, d); Main = main; Accent = accent; Label = label; K = k; }
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    static readonly Dictionary<string, Look> Table = new Dictionary<string, Look>
    {
        // naturali: colori terrosi, nessuna etichetta
        { "pane_forno",       new Look(Shape.Loaf,  .34f, .10f, .10f, C(.72f, .48f, .24f), C(.92f, .78f, .5f)) },
        { "uova_paese",       new Look(Shape.Eggs,  .17f, .08f, .12f, C(.9f, .85f, .72f), C(.6f, .55f, .45f), null, .045f) },
        { "mortadella",       new Look(Shape.Log,   .30f, .14f, .14f, C(.82f, .52f, .48f), C(.45f, .3f, .2f)) },
        { "prosciutto",       new Look(Shape.Ham,   .30f, .15f, .14f, C(.68f, .3f, .27f), C(.9f, .85f, .72f)) },
        { "formaggio_malga",  new Look(Shape.Can,   .20f, .10f, .20f, C(.85f, .68f, .3f), C(.93f, .8f, .45f)) },
        { "castagne",         new Look(Shape.Crate, .26f, .13f, .14f, C(.45f, .25f, .12f), C(.5f, .36f, .2f), null, .06f) },
        { "olio_oliva",       new Look(Shape.Bottle, .08f, .30f, .08f, C(.55f, .58f, .15f), C(.25f, .25f, .1f), null, .35f) },
        { "passata",          new Look(Shape.Bottle, .09f, .17f, .09f, C(.65f, .15f, .1f), C(.3f, .1f, .08f), null, .12f) },
        { "farina",           new Look(Shape.Box,   .12f, .20f, .08f, C(.88f, .84f, .74f), C(.6f, .5f, .36f)) },
        { "latte",            new Look(Shape.Bottle, .09f, .24f, .09f, C(.92f, .9f, .82f), C(.3f, .4f, .65f), null, .3f) },
        { "vino_rosso",       new Look(Shape.Bottle, .08f, .30f, .08f, C(.1f, .22f, .1f), C(.45f, .08f, .08f), null, .38f) },
        { "vino_sfuso",       new Look(Shape.Bottle, .14f, .26f, .14f, C(.55f, .5f, .2f), C(.4f, .3f, .1f), null, .3f) },

        // industriali: colori sgargianti + etichetta
        { "patatine",         new Look(Shape.Bag,   .22f, .30f, .07f, C(1f, .78f, .05f), C(.9f, .1f, .1f), "PATATINE") },
        { "brodo_dadi",       new Look(Shape.Box,   .10f, .12f, .06f, C(.95f, .8f, .1f), C(.85f, .1f, .1f), "DADI") },
        { "cracker",          new Look(Shape.Box,   .14f, .20f, .06f, C(.2f, .5f, .9f), C(1f, .9f, .3f), "CRACKER") },
        { "zucchero",         new Look(Shape.Box,   .10f, .18f, .07f, C(.92f, .94f, 1f), C(.15f, .35f, .85f), "ZUCCHERO") },
        { "lievito",          new Look(Shape.Box,   .08f, .05f, .03f, C(.9f, .2f, .15f), C(1f, 1f, 1f), "LIEVITO") },
        { "birra",            new Look(Shape.Bottle, .065f, .24f, .065f, C(.4f, .22f, .05f), C(.95f, .8f, .2f), "BIRRA", .35f) },
        { "amaro",            new Look(Shape.Bottle, .09f, .30f, .09f, C(.2f, .08f, .05f), C(.88f, .72f, .2f), "AMARO", .3f) },
        { "merendine",        new Look(Shape.Box,   .16f, .10f, .10f, C(.92f, .2f, .5f), C(1f, .85f, .1f), "MERENDINE") },
        { "biscotti",         new Look(Shape.Box,   .16f, .22f, .06f, C(.1f, .6f, .95f), C(1f, .85f, .1f), "BISCOTTI") },
        { "caramelle",        new Look(Shape.Bag,   .14f, .20f, .05f, C(.9f, .15f, .7f), C(.15f, .85f, .85f), "CARAMELLE") },
        { "cacao",            new Look(Shape.Can,   .10f, .14f, .10f, C(.55f, .25f, .1f), C(1f, .6f, .1f), "CACAO") },

        // tabacco
        { "sigarette",        new Look(Shape.Box,   .055f, .085f, .022f, C(.95f, .95f, .92f), C(.85f, .1f, .1f)) },
        { "sigari",           new Look(Shape.Box,   .16f, .05f, .10f, C(.4f, .25f, .12f), C(.85f, .7f, .25f)) },
    };

    static readonly Look Fallback = new Look(Shape.Box, .15f, .15f, .15f, C(.5f, .5f, .5f), C(.3f, .3f, .3f));

    /// <summary>Crea il prodotto: root con BoxCollider + pezzi visivi. pos = punto d'appoggio (base del prodotto).</summary>
    public static GameObject Build(Product p, Vector3 pos)
    {
        if (!Table.TryGetValue(p.Id, out var l)) l = Fallback;
        var root = new GameObject(p.Name);
        var box = root.AddComponent<BoxCollider>();
        box.size = l.Size;
        root.transform.position = pos + Vector3.up * (l.Size.y / 2f); // pivot al centro del collider
        float w = l.Size.x, h = l.Size.y, d = l.Size.z;
        var t = root.transform;

        switch (l.S)
        {
            case Shape.Bottle:
            {
                float cap = h * .07f, bodyH = h * (1f - l.K), neckH = h * l.K - cap;
                Part(t, PrimitiveType.Cylinder, new Vector3(0, -h / 2 + bodyH / 2, 0), new Vector3(w, bodyH / 2, d), l.Main);
                if (neckH > 0) Part(t, PrimitiveType.Cylinder, new Vector3(0, -h / 2 + bodyH + neckH / 2, 0), new Vector3(w * .35f, neckH / 2, d * .35f), l.Main);
                Part(t, PrimitiveType.Cylinder, new Vector3(0, h / 2 - cap / 2, 0), new Vector3(w * .4f, cap / 2, d * .4f), l.Accent);
                if (l.Label != null) Label(t, l, -h / 2 + bodyH * .5f, w * .8f, Mathf.Min(bodyH * .4f, .07f), w / 2);
                break;
            }
            case Shape.Can:
                Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(w, h / 2, d), l.Main);
                Part(t, PrimitiveType.Cylinder, new Vector3(0, h / 2, 0), new Vector3(w * .92f, .006f, d * .92f), l.Accent);
                if (l.Label != null) Label(t, l, 0, w * .75f, h * .4f, w / 2);
                break;
            case Shape.Box:
                Part(t, PrimitiveType.Cube, Vector3.zero, l.Size, l.Main);
                Part(t, PrimitiveType.Cube, new Vector3(0, h * .41f, 0), new Vector3(w * 1.02f, h * .18f, d * 1.02f), l.Accent);
                if (l.Label != null) Label(t, l, -h * .08f, w * .88f, h * .4f, d / 2 * 1.02f);
                break;
            case Shape.Bag: // sacchetto schiacciato con le due saldature
                Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(w, h * .92f, d), l.Main);
                foreach (float s in new[] { -1f, 1f })
                    Part(t, PrimitiveType.Cube, new Vector3(0, s * h * .475f, 0), new Vector3(w, h * .05f, d * .4f), l.Accent);
                if (l.Label != null) Label(t, l, 0, w * .8f, h * .28f, d / 2);
                break;
            case Shape.Log: // salume sdraiato lungo x, con la legatura
                Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(d, w / 2, d), l.Main, Z90);
                Part(t, PrimitiveType.Cylinder, new Vector3(w * .15f, 0, 0), new Vector3(d * 1.04f, .008f, d * 1.04f), l.Accent, Z90);
                break;
            case Shape.Loaf: // filone con tre tagli
                Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(h, w / 2, d), l.Main, Z90);
                for (int i = -1; i <= 1; i++)
                    Part(t, PrimitiveType.Cube, new Vector3(i * w * .22f, h * .42f, 0), new Vector3(.015f, .006f, d * .5f), l.Accent, new Vector3(0, 40, 0));
                break;
            case Shape.Ham: // coscia con l'osso
                Part(t, PrimitiveType.Capsule, new Vector3(-w * .08f, 0, 0), new Vector3(h * .85f, w * .36f, d * .85f), l.Main, Z90);
                Part(t, PrimitiveType.Cylinder, new Vector3(w * .38f, 0, 0), new Vector3(.03f, w * .1f, .03f), l.Accent, Z90);
                Part(t, PrimitiveType.Sphere, new Vector3(w * .48f, 0, 0), new Vector3(.05f, .05f, .05f), l.Accent);
                break;
            case Shape.Crate:
            case Shape.Eggs: // cassetta/vassoio con frutti a sfera
            {
                bool egg = l.S == Shape.Eggs;
                float trayH = egg ? h * .35f : h * .45f, b = l.K;
                Part(t, PrimitiveType.Cube, new Vector3(0, -h / 2 + trayH / 2, 0), new Vector3(w, trayH, d), l.Accent);
                int cols = Mathf.Max(1, Mathf.FloorToInt(w / b)), rows = Mathf.Max(1, Mathf.FloorToInt(d / b));
                for (int x = 0; x < cols; x++)
                    for (int z = 0; z < rows; z++)
                        Part(t, PrimitiveType.Sphere, new Vector3((x + .5f) * w / cols - w / 2, -h / 2 + trayH + b * (egg ? .6f : .4f), (z + .5f) * d / rows - d / 2),
                             new Vector3(b, b * (egg ? 1.3f : 1f), b), l.Main);
                break;
            }
        }
        return root;
    }

    static readonly Vector3 Z90 = new Vector3(0, 0, 90);

    // Pezzo visivo: primitiva senza collider, materiale condiviso dalla cache di WorldBuilder.
    static void Part(Transform root, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = type.ToString();
        g.transform.SetParent(root, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = WorldBuilder.Mat(c);
    }

    // Etichetta con il nome su fronte e retro (il prodotto non ha un "davanti" fisso sullo scaffale).
    // ponytail: nessun batching dei pezzi; ~5 oggetti per prodotto bastano per poche decine di pezzi a vista.
    static void Label(Transform root, Look l, float y, float plateW, float plateH, float zOff)
    {
        var f = UIKit.DefaultFont;
        bool dark = (l.Accent.r + l.Accent.g + l.Accent.b) / 3f > .5f;
        foreach (float s in new[] { -1f, 1f })
        {
            Part(root, PrimitiveType.Cube, new Vector3(0, y, s * zOff), new Vector3(plateW, plateH, .004f), l.Accent);
            var tm = new GameObject("Etichetta").AddComponent<TextMesh>();
            tm.transform.SetParent(root, false);
            tm.transform.localPosition = new Vector3(0, y, s * (zOff + .003f));
            tm.transform.localRotation = Quaternion.Euler(0, s < 0 ? 0 : 180, 0); // TextMesh si legge da -z
            tm.font = f;
            tm.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            tm.text = l.Label;
            tm.color = dark ? Color.black : Color.white;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.fontSize = 100;
            tm.characterSize = Mathf.Min(plateH * .7f, plateW * .9f / (l.Label.Length * .75f)) / 10f;
        }
    }
}
