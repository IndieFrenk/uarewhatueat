using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Il "Fuori" della piazza è Canolo Nuovo (RC) vero: rilievo EU-DEM come Terrain, edifici, strade, recinti, bosco, serra e laghetto di OpenStreetMap.
/// Dati in Resources/canolo_osm.json (metri locali: x est, z sud) e canolo_quote.json (griglia 81x81 non uniforme, fitta al centro).
/// TRASFORMAZIONE OSM -> Unity: sola traslazione, niente rotazione. x_unity = x_osm + TX, z_unity = TZ - z_osm (il nord resta +z), y = quota - quota del centro piazza.
/// Con TX=5, TZ=-1 la chiesa vera (osm -2,32) cade a (3,-33): dove sta la chiesa della piazza, sul suo lato sud.
/// Tutto combinato in mesh per (materiale, cella 400 m), senza collider: il giocatore resta dentro i muretti della piazza.
/// Dati: © OpenStreetMap contributors (ODbL); quote EU-DEM 25 m. Vedi CREDITS.md.
/// </summary>
public static class Canolo
{
    public const string Credit = "Mappa: © OpenStreetMap contributors (ODbL). Rilievo: EU-DEM.";

    const float TX = 5, TZ = -1;                   // traslazione OSM -> Unity
    const float Half = 2500, Blend = 70, PlateauY = -.4f, PitY = -3f, Cell = 400;
    const int N = 1025, A = 512;                   // heightmap e alphamap del Terrain (5 km di lato)
    // area giocabile (piazza, negozio, fila sud fino alla chiesa) e stanze casa/bar: niente edifici veri, terreno piatto o incavato
    static readonly Rect Plaza = Rect.MinMaxRect(-20, -45, 30, 16), Rooms = Rect.MinMaxRect(90, -15, 215, 20);
    static readonly Rect[] Pits = { Rect.MinMaxRect(94, -8, 113, 13), Rect.MinMaxRect(194, -8, 216, 15) };
    static readonly Vector2 Center = new Vector2(5, -12);

    static readonly Color[] Pines = { new Color(.1f, .25f, .12f), new Color(.12f, .28f, .14f), new Color(.08f, .22f, .13f) };
    static readonly Color[] Chests = { new Color(.22f, .38f, .12f), new Color(.28f, .42f, .15f), new Color(.3f, .36f, .12f) };
    static readonly Color Dirt = new Color(.5f, .42f, .3f), Water = new Color(.16f, .28f, .36f);

    class Poly { public long id; public Vector2[] p; public Dictionary<string, object> tag; public Rect bb; }

    // griglia del rilievo (come canolo_geo.py: coordinate_griglia) e quote relative al centro piazza
    static float[] gx;
    static float[,] gh;
    static float hRef;
    // heightmap finale (y in metri) del Terrain: la stessa posa edifici, strade e alberi
    static float[,] ht;
    static float x0, z0, step, yMin, yMax;
    static float[] waterY;
    static bool[,] forest;
    static System.Random rnd;
    static readonly Dictionary<(Material, int, int, bool), MB> meshes = new Dictionary<(Material, int, int, bool), MB>();
    static readonly List<Vector4> boxes = new List<Vector4>();   // ingombri edifici (minx, minz, maxx, maxz): niente alberi
    static readonly List<Vector3> roadPts = new List<Vector3>(); // x, z, raggio: niente alberi sulle strade
    static int nBuild, nTrees;

    public static void Build()
    {
        var osmA = Resources.Load<TextAsset>("canolo_osm");
        var qA = Resources.Load<TextAsset>("canolo_quote");
        if (osmA == null || qA == null) { Debug.LogError("Canolo: mancano Resources/canolo_osm.json o canolo_quote.json"); return; }
        rnd = new System.Random(1990);
        meshes.Clear(); boxes.Clear(); roadPts.Clear(); nBuild = nTrees = 0;
        LoadGrid(qA.text);
        var osm = (Dictionary<string, object>)Mini.Parse(osmA.text);
        var water = Polys(osm, "acque", true);
        var forests = Polys(osm, "boschi", true);
        var g = WorldBuilder.Group("Canolo");

        HeightMap(water);
        Terreno(g, forests);
        foreach (var b in Polys(osm, "edifici", true)) Edificio(b, false);
        foreach (var b in Polys(osm, "serre", true)) Edificio(b, true);
        Strade(Polys(osm, "strade", false));
        Recinti(Polys(osm, "recinti", false));
        for (int k = 0; k < water.Count; k++) Acqua(water[k], waterY[k]);
        Alberi();
        Flush(g);

        var cam = Camera.main; // il Terrain arriva a 2,5 km
        if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 6000f);
        Debug.Log($"Canolo: {nBuild} edifici, {nTrees} alberi, {meshes.Count} mesh combinate");
    }

    // ---------------- DATI ----------------

    static void LoadGrid(string json)
    {
        var d = (Dictionary<string, object>)Mini.Parse(json);
        int n = (int)(double)d["n"];
        var q = (Dictionary<string, object>)d["quote"];
        gx = new float[n];
        gh = new float[n, n];
        for (int i = 0; i < n; i++)
        {
            float t = -1f + 2f * i / (n - 1);
            gx[i] = 5000f * (.12f * t + .88f * t * Mathf.Abs(t));
            for (int j = 0; j < n; j++) gh[j, i] = (float)(double)q[i + "," + j]; // chiave "i,j": i su x, j su z
        }
        hRef = Quote(0, TZ - Center.y);
    }

    static int Idx(float v)
    {
        int i = System.Array.BinarySearch(gx, v);
        return Mathf.Clamp(i < 0 ? ~i - 1 : i, 0, gx.Length - 2);
    }

    // Quota assoluta nel punto OSM (x est, z sud): interpolazione sulla griglia non uniforme, due triangoli per cella.
    static float Quote(float x, float z)
    {
        int i = Idx(x), j = Idx(z);
        float u = Mathf.Clamp01((x - gx[i]) / (gx[i + 1] - gx[i])), v = Mathf.Clamp01((z - gx[j]) / (gx[j + 1] - gx[j]));
        float h00 = gh[j, i], h10 = gh[j, i + 1], h01 = gh[j + 1, i], h11 = gh[j + 1, i + 1];
        return u >= v ? h00 + (h10 - h00) * u + (h11 - h10) * v : h00 + (h11 - h01) * u + (h01 - h00) * v;
    }

    static List<Poly> Polys(Dictionary<string, object> d, string key, bool closed)
    {
        var r = new List<Poly>();
        foreach (Dictionary<string, object> o in (List<object>)d[key])
        {
            var pts = (List<object>)o["pts"];
            int n = pts.Count;
            var p = new Vector2[n];
            for (int i = 0; i < n; i++) { var a = (List<object>)pts[i]; p[i] = new Vector2((float)(double)a[0] + TX, TZ - (float)(double)a[1]); }
            if (closed && n > 1 && p[0] == p[n - 1]) System.Array.Resize(ref p, n - 1);
            var bb = Rect.MinMaxRect(p[0].x, p[0].y, p[0].x, p[0].y);
            foreach (var q in p) bb = Rect.MinMaxRect(Mathf.Min(bb.xMin, q.x), Mathf.Min(bb.yMin, q.y), Mathf.Max(bb.xMax, q.x), Mathf.Max(bb.yMax, q.y));
            r.Add(new Poly { id = (long)(double)o["id"], p = p, tag = (Dictionary<string, object>)o["tag"], bb = bb });
        }
        return r;
    }

    // ---------------- TERRENO ----------------

    static float Dist(Rect r, float x, float z)
    {
        float dx = Mathf.Max(Mathf.Max(r.xMin - x, x - r.xMax), 0), dz = Mathf.Max(Mathf.Max(r.yMin - z, z - r.yMax), 0);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // Vero se (x,z) sta entro m metri dalla piazza giocabile o dalle stanze incavate.
    static bool InArea(float x, float z, float m)
    {
        if (Dist(Plaza, x, z) < m) return true;
        foreach (var r in Pits) if (Dist(r, x, z) < m) return true;
        return false;
    }

    // Quota (m) del terreno: rilievo vero, piatto sotto la piazza con raccordo dolce, incavato sotto le stanze di casa e bar.
    static float Base(float X, float Z)
    {
        float h = Quote(X - TX, TZ - Z) - hRef;
        h = Mathf.Lerp(PlateauY, h, Mathf.SmoothStep(0, 1, Dist(Plaza, X, Z) / Blend));
        foreach (var r in Pits) h = Mathf.Lerp(PitY, h, Mathf.SmoothStep(0, 1, Dist(r, X, Z) / 25f));
        return h;
    }

    static void HeightMap(List<Poly> water)
    {
        x0 = TX - Half; z0 = TZ - Half; step = 2 * Half / (N - 1);
        waterY = new float[water.Count];
        for (int k = 0; k < water.Count; k++)
        {
            float m = float.MaxValue; // il pelo dell'acqua sta alla quota più bassa della riva
            foreach (var p in water[k].p) m = Mathf.Min(m, Base(p.x, p.y));
            waterY[k] = m - .2f;
        }
        ht = new float[N, N];
        yMin = float.MaxValue; yMax = float.MinValue;
        for (int zi = 0; zi < N; zi++)
            for (int xi = 0; xi < N; xi++)
            {
                float X = x0 + xi * step, Z = z0 + zi * step, h = Base(X, Z);
                for (int k = 0; k < water.Count; k++)
                    if (water[k].bb.Contains(new Vector2(X, Z)) && Inside(water[k].p, X, Z)) h = Mathf.Min(h, waterY[k] - 1.2f); // il laghetto è scavato
                ht[zi, xi] = h;
                yMin = Mathf.Min(yMin, h); yMax = Mathf.Max(yMax, h);
            }
    }

    // Quota del Terrain così come è disegnato (bilineare sulla heightmap).
    static float Ground(float X, float Z)
    {
        float fx = Mathf.Clamp((X - x0) / step, 0, N - 1.001f), fz = Mathf.Clamp((Z - z0) / step, 0, N - 1.001f);
        int i = (int)fx, j = (int)fz;
        float u = fx - i, v = fz - j;
        return Mathf.Lerp(Mathf.Lerp(ht[j, i], ht[j, i + 1], u), Mathf.Lerp(ht[j + 1, i], ht[j + 1, i + 1], u), v);
    }

    static bool Inside(Vector2[] p, float x, float z)
    {
        bool in_ = false;
        for (int a = 0, b = p.Length - 1; a < p.Length; b = a++)
            if ((p[a].y > z) != (p[b].y > z) && x < (p[b].x - p[a].x) * (z - p[a].y) / (p[b].y - p[a].y) + p[a].x) in_ = !in_;
        return in_;
    }

    // Maschera dei boschi sulla griglia del Terrain (riempimento a righe, pari-dispari).
    static void ForestMask(List<Poly> forests)
    {
        int M = N - 1;
        forest = new bool[M, M];
        var xs = new List<float>();
        foreach (var f in forests)
        {
            var p = f.p;
            int r0 = Mathf.Max(0, Mathf.FloorToInt((f.bb.yMin - z0) / step)), r1 = Mathf.Min(M - 1, Mathf.CeilToInt((f.bb.yMax - z0) / step));
            for (int r = r0; r <= r1; r++)
            {
                float z = z0 + (r + .5f) * step;
                xs.Clear();
                for (int a = 0, b = p.Length - 1; a < p.Length; b = a++)
                    if ((p[a].y > z) != (p[b].y > z)) xs.Add(p[a].x + (z - p[a].y) / (p[b].y - p[a].y) * (p[b].x - p[a].x));
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int c0 = Mathf.Max(0, Mathf.CeilToInt((xs[k] - x0) / step - .5f)), c1 = Mathf.Min(M - 1, Mathf.FloorToInt((xs[k + 1] - x0) / step - .5f));
                    for (int c = c0; c <= c1; c++) forest[r, c] = true;
                }
            }
        }
    }

    static bool InForest(float X, float Z)
    {
        int c = Mathf.FloorToInt((X - x0) / step), r = Mathf.FloorToInt((Z - z0) / step);
        return c >= 0 && r >= 0 && c < N - 1 && r < N - 1 && forest[r, c];
    }

    static Texture2D Noise(Color a, Color b)
    {
        var t = new Texture2D(16, 16) { wrapMode = TextureWrapMode.Repeat };
        for (int i = 0; i < 16; i++) for (int j = 0; j < 16; j++) t.SetPixel(i, j, Color.Lerp(a, b, (float)rnd.NextDouble()));
        t.Apply();
        return t;
    }

    // Terrain 5x5 km da ht. Tre strati: prato, roccia (pendii ripidi), sottobosco (dentro i poligoni bosco).
    static void Terreno(Transform p, List<Poly> forests)
    {
        ForestMask(forests);
        float range = yMax - yMin + 1;
        var td = new TerrainData { heightmapResolution = N, alphamapResolution = A };
        td.size = new Vector3(2 * Half, range, 2 * Half);
        var hm = new float[N, N];
        for (int zi = 0; zi < N; zi++) for (int xi = 0; xi < N; xi++) hm[zi, xi] = (ht[zi, xi] - yMin) / range;
        td.SetHeights(0, 0, hm);
        td.terrainLayers = new[]
        {
            new TerrainLayer { diffuseTexture = Noise(new Color(.25f, .38f, .17f), new Color(.33f, .45f, .2f)), tileSize = new Vector2(8, 8) },
            new TerrainLayer { diffuseTexture = Noise(new Color(.38f, .35f, .3f), new Color(.5f, .46f, .4f)), tileSize = new Vector2(10, 10) },
            new TerrainLayer { diffuseTexture = Noise(new Color(.12f, .22f, .1f), new Color(.18f, .3f, .14f)), tileSize = new Vector2(6, 6) },
        };
        var am = new float[A, A, 3];
        for (int zi = 0; zi < A; zi++)
            for (int xi = 0; xi < A; xi++)
            {
                int i = Mathf.Clamp(Mathf.RoundToInt((xi + .5f) * (N - 1) / A), 1, N - 2), j = Mathf.Clamp(Mathf.RoundToInt((zi + .5f) * (N - 1) / A), 1, N - 2);
                float sx = (ht[j, i + 1] - ht[j, i - 1]) / (2 * step), sz = (ht[j + 1, i] - ht[j - 1, i]) / (2 * step);
                float rock = Mathf.SmoothStep(0, 1, (Mathf.Sqrt(sx * sx + sz * sz) - .45f) / .5f);
                float f = forest[zi * 2 + 1, xi * 2 + 1] ? 1 - rock : 0;
                am[zi, xi, 0] = 1 - rock - f;
                am[zi, xi, 1] = rock;
                am[zi, xi, 2] = f;
            }
        td.SetAlphamaps(0, 0, am);
        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Rilievo";
        go.transform.SetParent(p);
        go.transform.position = new Vector3(x0, yMin, z0);
        Object.Destroy(go.GetComponent<TerrainCollider>()); // lontano e irraggiungibile
        var t = go.GetComponent<Terrain>();
        t.heightmapPixelError = 4;
        t.shadowCastingMode = ShadowCastingMode.Off;
        var sh = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (sh != null) t.materialTemplate = new Material(sh);
    }

    // ---------------- MESH COMBINATE ----------------

    // Triangoli a vertici non condivisi (normali piatte). Il verso di ogni faccia segue "want": il giro dei vertici non conta.
    class MB
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<int> t = new List<int>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 want)
        {
            var nm = Vector3.Cross(b - a, c - a); // in Unity il davanti è il giro orario: la normale è (b-a)x(c-a)
            if (nm.sqrMagnitude < 1e-10f) return;
            if (Vector3.Dot(nm, want) < 0) { var s = b; b = c; c = s; nm = -nm; }
            nm.Normalize();
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(nm); n.Add(nm); n.Add(nm);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want) { Tri(a, b, c, want); Tri(a, c, d, want); }
    }

    static MB M(Material m, float x, float z, bool shadow)
    {
        var k = (m, Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell), shadow);
        if (!meshes.TryGetValue(k, out var b)) meshes[k] = b = new MB();
        return b;
    }

    static MB M(Color c, float x, float z, bool emit = false, bool shadow = true) => M(WorldBuilder.Mat(c, emit), x, z, shadow);

    static void Flush(Transform g)
    {
        foreach (var kv in meshes)
        {
            var b = kv.Value;
            var mesh = new Mesh { indexFormat = b.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(b.v);
            mesh.SetNormals(b.n);
            mesh.SetTriangles(b.t, 0);
            mesh.RecalculateBounds();
            var go = new GameObject("Mesh");
            go.transform.SetParent(g, false);
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = kv.Key.Item1;
            r.shadowCastingMode = kv.Key.Item4 ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        meshes.Clear();
    }

    static float Hash(long a, int b)
    {
        unchecked
        {
            uint x = (uint)(a ^ (a >> 32)) * 2654435761u + (uint)b * 40503u + 12345u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }
    }

    static float SignedArea(Vector2[] p)
    {
        float s = 0;
        for (int a = 0, b = p.Length - 1; a < p.Length; b = a++) s += p[b].x * p[a].y - p[a].x * p[b].y;
        return s / 2;
    }

    // Rettangolo minimo orientato: centro, asse lungo u, lati w (lungo u) >= d.
    static void Obb(Vector2[] p, out Vector2 c, out Vector2 u, out float w, out float d)
    {
        float best = float.MaxValue;
        c = p[0]; u = Vector2.right; w = d = 1;
        Vector2 bv = Vector2.up;
        for (int k = 0; k < p.Length; k++)
        {
            var e = p[(k + 1) % p.Length] - p[k];
            float len = e.magnitude;
            if (len < 1e-4f) continue;
            var uu = e / len; var vv = new Vector2(-uu.y, uu.x);
            float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
            foreach (var q in p)
            {
                float a = Vector2.Dot(q, uu), b = Vector2.Dot(q, vv);
                u0 = Mathf.Min(u0, a); u1 = Mathf.Max(u1, a); v0 = Mathf.Min(v0, b); v1 = Mathf.Max(v1, b);
            }
            if ((u1 - u0) * (v1 - v0) < best)
            {
                best = (u1 - u0) * (v1 - v0);
                c = uu * (u0 + u1) / 2 + vv * (v0 + v1) / 2; u = uu; bv = vv; w = u1 - u0; d = v1 - v0;
            }
        }
        if (d > w) { var s = u; u = bv; bv = s; var t = w; w = d; d = t; }
    }

    // ---------------- EDIFICI ----------------

    // Sagoma estrusa (6-9 m in base all'area, deterministica per id), tetto a padiglione sul rettangolo minimo, finestre (alcune accese).
    static void Edificio(Poly b, bool serra)
    {
        var p = b.p;
        int n = p.Length;
        float sa = SignedArea(p), area = Mathf.Abs(sa);
        boxes.Add(new Vector4(b.bb.xMin - 3, b.bb.yMin - 3, b.bb.xMax + 3, b.bb.yMax + 3));
        if (n < 3 || area < 6 || b.bb.Overlaps(Plaza) || b.bb.Overlaps(Rooms)) return;
        nBuild++;

        float h = serra ? 2.8f : area < 25 ? 3.2f : Mathf.Clamp(6f + 3f * Mathf.SmoothStep(0, 1, (area - 40f) / 160f) + Hash(b.id, 1) - .5f, 6f, 9f); // capanne basse
        float yb = float.MaxValue, cx = 0, cz = 0;
        for (int i = 0; i < n; i++)
        {
            var q = p[(i + 1) % n];
            yb = Mathf.Min(yb, Mathf.Min(Ground(p[i].x, p[i].y), Ground((p[i].x + q.x) / 2, (p[i].y + q.y) / 2)));
            cx += p[i].x / n; cz += p[i].y / n;
        }
        yb = Mathf.Min(yb, Ground(cx, cz));
        var wallC = serra ? new Color(.7f, .8f, .72f) : VillageDressing.Walls[(int)(Hash(b.id, 2) * VillageDressing.Walls.Length)];
        var roofC = serra ? new Color(.78f, .85f, .8f) : VillageDressing.Roofs[(int)(Hash(b.id, 3) * VillageDressing.Roofs.Length)];
        var mw = M(wallC, cx, cz);
        var glassD = M(VillageDressing.Dark, cx, cz, false, false);
        var glassL = M(VillageDressing.Lit, cx, cz, true, false);
        var frame = M(VillageDressing.Frame, cx, cz, false, false);
        int fl = Mathf.Max(1, Mathf.RoundToInt(h / 3f));

        for (int i = 0; i < n; i++)
        {
            Vector2 a = p[i], c = p[(i + 1) % n], e = c - a;
            float L = e.magnitude;
            if (L < .05f) continue;
            var dir = e / L;
            var o2 = sa > 0 ? new Vector2(dir.y, -dir.x) : new Vector2(-dir.y, dir.x); // verso l'esterno
            var o = new Vector3(o2.x, 0, o2.y);
            mw.Quad(new Vector3(a.x, yb - .6f, a.y), new Vector3(c.x, yb - .6f, c.y), new Vector3(c.x, yb + h, c.y), new Vector3(a.x, yb + h, a.y), o);
            if (serra || area < 25) continue;
            int nw = Mathf.FloorToInt(L / 3.2f);
            var right = new Vector3(dir.x, 0, dir.y);
            for (int k = 0; k < nw; k++)
            {
                var w = a + dir * (L * (k + .5f) / nw);
                for (int f = 0; f < fl; f++)
                {
                    float y = yb + 1.6f + 3f * f;
                    if (y + .7f > yb + h - .3f || Ground(w.x, w.y) > y - .7f) continue; // sotto il tetto o interrata
                    var cw = new Vector3(w.x, y, w.y);
                    Win(frame, cw + o * .02f, right * .55f, .75f, o);
                    Win(Hash(b.id, i * 256 + k * 4 + f) < .2f ? glassL : glassD, cw + o * .035f, right * .45f, .65f, o);
                }
            }
        }

        // tetto a padiglione: il colmo corre lungo l'asse lungo del rettangolo minimo; ogni lato sale verso il punto più vicino del colmo
        Obb(p, out var oc, out var u, out float W, out float D);
        float rise = serra ? 1f : Mathf.Clamp(D * .28f, .8f, 3.2f), lh = Mathf.Max(0, (W - D) / 2), top = yb + h + rise;
        var mr = M(roofC, cx, cz);
        for (int i = 0; i < n; i++)
        {
            Vector2 a = p[i], c = p[(i + 1) % n];
            var ra = oc + u * Mathf.Clamp(Vector2.Dot(a - oc, u), -lh, lh);
            var rc = oc + u * Mathf.Clamp(Vector2.Dot(c - oc, u), -lh, lh);
            var A3 = new Vector3(a.x, yb + h, a.y); var C3 = new Vector3(c.x, yb + h, c.y);
            var RA = new Vector3(ra.x, top, ra.y); var RC = new Vector3(rc.x, top, rc.y);
            mr.Quad(A3, C3, RC, RA, Vector3.up);
        }
    }

    // Quadro (centro cw, semilarghezza dx, semialtezza) con la faccia verso o.
    static void Win(MB m, Vector3 cw, Vector3 dx, float hh, Vector3 o)
    {
        var up = Vector3.up * hh;
        m.Quad(cw - dx - up, cw + dx - up, cw + dx + up, cw - dx + up, o);
    }

    // ---------------- STRADE E RECINTI ----------------

    static List<Vector2> Resample(Vector2[] pts, float s)
    {
        var r = new List<Vector2>();
        for (int k = 0; k + 1 < pts.Length; k++)
        {
            int m = Mathf.Max(1, Mathf.RoundToInt((pts[k + 1] - pts[k]).magnitude / s));
            for (int q = 0; q < m; q++) r.Add(Vector2.Lerp(pts[k], pts[k + 1], (float)q / m));
        }
        r.Add(pts[pts.Length - 1]);
        return r;
    }

    // Normale (a sinistra) della spezzata nel campione k.
    static Vector2 Side(List<Vector2> s, int k)
    {
        var d = s[Mathf.Min(k + 1, s.Count - 1)] - s[Mathf.Max(k - 1, 0)];
        return d.sqrMagnitude < 1e-8f ? Vector2.up : new Vector2(-d.y, d.x).normalized;
    }

    static float RoadWidth(string hw)
    {
        switch (hw)
        {
            case "tertiary": return 6f;
            case "residential": return 4.6f;
            case "unclassified": return 4.8f;
            case "service": return 3.2f;
            case "track": return 3f;
            case "footway": return 1.6f;
            default: return 1.4f;
        }
    }

    // Nastri che seguono il terreno; i più larghi sotto (leggero rialzo crescente per non sovrapporsi).
    static void Strade(List<Poly> roads)
    {
        roads.Sort((a, b) => RoadWidth(Tag(b, "highway")).CompareTo(RoadWidth(Tag(a, "highway"))));
        for (int rank = 0; rank < roads.Count; rank++)
        {
            string hw = Tag(roads[rank], "highway");
            float w = RoadWidth(hw), off = .15f + .012f * rank;
            var s = Resample(roads[rank].p, 4f);
            var L = new Vector3[s.Count]; var R = new Vector3[s.Count];
            for (int k = 0; k < s.Count; k++)
            {
                var nl = Side(s, k) * w / 2;
                Vector2 pl = s[k] + nl, pr = s[k] - nl;
                L[k] = new Vector3(pl.x, Ground(pl.x, pl.y) + off, pl.y);
                R[k] = new Vector3(pr.x, Ground(pr.x, pr.y) + off, pr.y);
                roadPts.Add(new Vector3(s[k].x, s[k].y, w / 2 + 2));
            }
            for (int k = 0; k + 1 < s.Count; k++)
            {
                if (InArea(s[k].x, s[k].y, 2) || InArea(s[k + 1].x, s[k + 1].y, 2)) continue;
                M(hw == "track" || hw == "path" || hw == "footway" ? Dirt : VillageDressing.Asphalt, s[k].x, s[k].y, false, false).Quad(L[k], R[k], R[k + 1], L[k + 1], Vector3.up);
            }
        }
    }

    static string Tag(Poly p, string k) => p.tag.TryGetValue(k, out var v) && v is string s ? s : "";

    // Recinti come muretti bassi (1 m, spessore 24 cm) che seguono il terreno.
    static void Recinti(List<Poly> fences)
    {
        foreach (var f in fences)
        {
            var s = Resample(f.p, 3f);
            for (int k = 0; k + 1 < s.Count; k++)
            {
                if (InArea(s[k].x, s[k].y, 2) || InArea(s[k + 1].x, s[k + 1].y, 2)) continue;
                var m = M(VillageDressing.Stone, s[k].x, s[k].y, false, false);
                Vector2 na = Side(s, k) * .12f, nb = Side(s, k + 1) * .12f;
                Vector3 Pt(Vector2 q, Vector2 n, float dy) => new Vector3(q.x + n.x, Ground(q.x, q.y) + dy, q.y + n.y);
                Vector3 la = Pt(s[k], na, 1f), ra = Pt(s[k], -na, 1f), lb = Pt(s[k + 1], nb, 1f), rb = Pt(s[k + 1], -nb, 1f);
                Vector3 la0 = Pt(s[k], na, -.2f), ra0 = Pt(s[k], -na, -.2f), lb0 = Pt(s[k + 1], nb, -.2f), rb0 = Pt(s[k + 1], -nb, -.2f);
                m.Quad(la, lb, rb, ra, Vector3.up);
                m.Quad(la0, lb0, lb, la, new Vector3(na.x, 0, na.y));
                m.Quad(ra0, rb0, rb, ra, new Vector3(-na.x, 0, -na.y));
            }
        }
    }

    // Acqua: poligono piatto (triangolazione a orecchie, il laghetto è concavo).
    static void Acqua(Poly w, float y)
    {
        var p = w.p;
        var idx = new List<int>();
        for (int i = 0; i < p.Length; i++) idx.Add(i);
        float sa = SignedArea(p);
        var m = M(Water, p[0].x, p[0].y, false, false);
        Vector3 V(int i) => new Vector3(p[i].x, y, p[i].y);
        for (int guard = 0; idx.Count > 3 && guard < 1000; guard++)
        {
            bool cut = false;
            for (int k = 0; k < idx.Count && !cut; k++)
            {
                int a = idx[(k + idx.Count - 1) % idx.Count], b = idx[k], c = idx[(k + 1) % idx.Count];
                float cr = (p[b].x - p[a].x) * (p[c].y - p[b].y) - (p[b].y - p[a].y) * (p[c].x - p[b].x);
                if (sa > 0 ? cr <= 0 : cr >= 0) continue; // angolo concavo
                bool ok = true;
                foreach (int j in idx) if (j != a && j != b && j != c && InTri(p[j], p[a], p[b], p[c])) { ok = false; break; }
                if (!ok) continue;
                m.Tri(V(a), V(b), V(c), Vector3.up);
                idx.RemoveAt(k);
                cut = true;
            }
            if (!cut) break;
        }
        if (idx.Count == 3) m.Tri(V(idx[0]), V(idx[1]), V(idx[2]), Vector3.up);
    }

    static bool InTri(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sg(q, a, b), d2 = Sg(q, b, c), d3 = Sg(q, c, a);
        return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
    }

    static float Sg(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

    // ---------------- ALBERI ----------------

    static bool Blocked(float x, float z)
    {
        if (InArea(x, z, 8)) return true;
        foreach (var b in boxes) if (x > b.x && x < b.z && z > b.y && z < b.w) return true;
        foreach (var r in roadPts) { float dx = r.x - x, dz = r.y - z; if (dx * dx + dz * dz < r.z * r.z) return true; }
        return false;
    }

    // Tre anelli attorno alla piazza: bosco fitto (pini e castagni) fino a 350 m, bosco a coni singoli fino a 1,1 km, macchie sparse fuori dal bosco.
    static void Alberi()
    {
        var passes = new[] { (7f, 0f, 350f, 0), (20f, 350f, 1100f, 1), (12f, 0f, 500f, 2) };
        foreach (var (st, rmin, rmax, mode) in passes)
            for (float dx = -rmax; dx <= rmax; dx += st)
                for (float dz = -rmax; dz <= rmax; dz += st)
                {
                    float r2 = dx * dx + dz * dz;
                    if (r2 < rmin * rmin || r2 > rmax * rmax) continue;
                    float X = Center.x + dx + ((float)rnd.NextDouble() - .5f) * st * .9f, Z = Center.y + dz + ((float)rnd.NextDouble() - .5f) * st * .9f;
                    bool bosco = InForest(X, Z);
                    if (mode < 2 ? !bosco : bosco || Mathf.PerlinNoise(X * .02f + 7, Z * .02f) < .62f) continue; // sparsi: macchie dove il rumore è alto
                    if (Blocked(X, Z)) continue;
                    float y = Ground(X, Z);
                    if (Mathf.Abs(Ground(X + 3, Z) - y) > 2.4f || Mathf.Abs(Ground(X, Z + 3) - y) > 2.4f) continue; // troppo ripido
                    nTrees++;
                    float s = 1f + (float)rnd.NextDouble() * .7f;
                    int v = rnd.Next(3);
                    if (mode == 1) Cone(M(Pines[v], X, Z, false, false), X, y, Z, 1.8f * s, 6f * s, 5, false);
                    else if (rnd.NextDouble() < (mode == 2 ? .6 : .3)) Chestnut(X, y, Z, s, v);
                    else Pine(X, y, Z, s, v);
                }
    }

    static void Prism(MB m, float x, float y, float z, float r, float h, int sides)
    {
        for (int k = 0; k < sides; k++)
        {
            float a0 = k * 2 * Mathf.PI / sides, a1 = (k + 1) * 2 * Mathf.PI / sides;
            Vector3 b0 = new Vector3(x + Mathf.Cos(a0) * r, y, z + Mathf.Sin(a0) * r), b1 = new Vector3(x + Mathf.Cos(a1) * r, y, z + Mathf.Sin(a1) * r);
            m.Quad(b0, b1, b1 + Vector3.up * h, b0 + Vector3.up * h, new Vector3(Mathf.Cos((a0 + a1) / 2), 0, Mathf.Sin((a0 + a1) / 2)));
        }
    }

    // Cono con la base in y e l'apice a y+h (h negativo = capovolto).
    static void Cone(MB m, float x, float y, float z, float r, float h, int sides, bool cap)
    {
        var apex = new Vector3(x, y + h, z);
        float sg = Mathf.Sign(h);
        for (int k = 0; k < sides; k++)
        {
            float a0 = k * 2 * Mathf.PI / sides, a1 = (k + 1) * 2 * Mathf.PI / sides;
            Vector3 b0 = new Vector3(x + Mathf.Cos(a0) * r, y, z + Mathf.Sin(a0) * r), b1 = new Vector3(x + Mathf.Cos(a1) * r, y, z + Mathf.Sin(a1) * r);
            var o = new Vector3(Mathf.Cos((a0 + a1) / 2), 0, Mathf.Sin((a0 + a1) / 2));
            m.Tri(b0, b1, apex, o * Mathf.Abs(h) + Vector3.up * r * sg);
            if (cap) m.Tri(b0, b1, new Vector3(x, y, z), Vector3.down * sg);
        }
    }

    static void Pine(float x, float y, float z, float s, int v)
    {
        Prism(M(VillageDressing.WoodD, x, z, false, false), x, y, z, .17f * s, 1.6f * s, 5);
        var m = M(Pines[v], x, z, false, false);
        for (int j = 0; j < 3; j++) Cone(m, x, y + (1.1f + j * 1.3f) * s, z, (1.7f - j * .45f) * s, 2.1f * s, 6, true);
    }

    // Castagno: tronco e chioma a doppio cono.
    static void Chestnut(float x, float y, float z, float s, int v)
    {
        Prism(M(VillageDressing.WoodD, x, z, false, false), x, y, z, .25f * s, 2.4f * s, 5);
        var m = M(Chests[v], x, z, false, false);
        Cone(m, x, y + 2.8f * s, z, 2.3f * s, 2.2f * s, 7, false);
        Cone(m, x, y + 2.8f * s, z, 2.3f * s, -1.6f * s, 7, false);
    }

    // ---------------- JSON MINIMO ----------------

    // JsonUtility non legge dizionari né array di array: oggetti -> Dictionary, array -> List, numeri -> double.
    static class Mini
    {
        public static object Parse(string s) { int i = 0; return Val(s, ref i); }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>();
                i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i);
                    var k = (string)Val(s, ref i);
                    Ws(s, ref i); i++; // ':'
                    d[k] = Val(s, ref i);
                    Ws(s, ref i);
                    if (s[i++] == '}') return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Val(s, ref i));
                    Ws(s, ref i);
                    if (s[i++] == ']') return l;
                }
            }
            if (c == '"')
            {
                int j = ++i;
                while (s[i] != '"') { if (s[i] == '\\') i++; i++; }
                return s.Substring(j, i++ - j);
            }
            int a = i;
            while (i < s.Length && ",]} \t\r\n".IndexOf(s[i]) < 0) i++;
            string t = s.Substring(a, i - a);
            if (t == "true") return true;
            if (t == "false") return false;
            if (t == "null") return null;
            return double.Parse(t, CultureInfo.InvariantCulture);
        }
    }
}
