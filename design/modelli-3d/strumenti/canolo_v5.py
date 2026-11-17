#!/usr/bin/env python3
"""v5: gli edifici di Canolo Nuovo in stile v4 (crea_edificio di canolo_v4), un .obj per edificio, pronti per Unity.

Uso: python3 canolo_v5.py        ->  design/modelli-3d/obj_v5/canolo/{b<id OSM>.obj,.mtl,texture/,edifici.json}

Formato scelto: UN OBJ PER EDIFICIO (nome = id OSM) + edifici.json con la posa. Motivo: Canolo.cs posa ogni edificio sul Terrain vero
(Ground(), diverso dalle quote Python vicino alla piazza), quindi la quota va decisa a runtime; le mesh combinate per cella non lo permetterebbero.
Draw call: si tengono bassi con i LOD (dettaglio alto <170 m dalla piazza, medio <380 m, basso oltre) e, in Canolo.cs, con i materiali condivisi per nome.

Convenzioni dell'obj (importatore OBJ di Unity: x_unity = -x_obj, z_unity = z_obj):
  il modello Python ha il fronte a z=0 che guarda +z e sta in z in [-Dd, 0]; qui il pivot e' il centro della sagoma a quota 0.
  coordinate scritte = (-a, y, -(b + Dd/2)) con (a, y, b) il modello Python, cioe' una rotazione di 180 gradi (niente specchio nel file);
  in Unity il modello risulta (a, y, -b): lo specchio z Python -> Unity del mondo (z_unity = TZ - z_osm) e' gia' incluso.
  Yaw in Unity = -r, con r = 'r' del json (gradi, convenzione Python). Posizione Unity = (cx + TX, y, TZ - cz).
"""
import json
import math
import random
import shutil
from pathlib import Path

import canolo_v4 as cn
import edifici_v4 as ed
from geo import MATERIALI, REG, Mod, caja, cross, esfera, sub, tutte_le_parti

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
USCITA = RADICE / 'obj_v5' / 'canolo'

# stessa traslazione OSM -> Unity e stesse aree escluse di Assets/src/Game/World/Canolo.cs (rettangoli Unity: xmin, zmin, xmax, zmax)
TX, TZ = 5.0, -1.0
PIAZZA = (-20, -45, 30, 16)
STANZE = (90, -15, 215, 20)
CENTRO_PIAZZA = (0.0, 11.0)   # (5,-12) in Unity, in coordinate OSM
LOD_DIST = (90, 250)         # LOD 0 / 1 / 2


def sovrappone(bb, r):
    return bb[2] > r[0] and bb[0] < r[2] and bb[3] > r[1] and bb[1] < r[3]


def bb_unity(pts):
    xs = [x + TX for x, z in pts]
    zs = [TZ - z for x, z in pts]
    return min(xs), min(zs), max(xs), max(zs)


def area(pts):
    return abs(sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))) / 2


# finestre: la v4 le accende tutte (e=1); qui una su cinque e' accesa, le altre sono vetro scuro (come Canolo.cs)
_n_fin = [0]
_lod = [0]


def finestra_lite(nome, w, h, persiane, luce, telaio, e):
    """Infisso a 5 scatole per il LOD 1 (vetro, telaio, davanzale, persiane): l'infisso completo pesa 5 volte tanto."""
    m = Mod(nome)
    m.add('vetro', luce, *caja(-w / 2, 0, -0.04, w / 2, h, -0.005), e=e)
    m.add('legno', telaio, *caja(-w / 2 - 0.05, -0.04, -0.02, w / 2 + 0.05, h + 0.05, 0.03))
    m.add('pietra', '#b8b2a0', *caja(-w / 2 - 0.1, -0.085, -0.02, w / 2 + 0.1, -0.03, 0.15))
    if persiane:
        for sx in (-1, 1):
            cx = sx * (w / 2 + 0.06 + w / 4)
            m.add('legno', persiane, *caja(cx - w / 4, -0.03, 0.0, cx + w / 4, h + 0.03, 0.035))
    return m


def fin_cache(w, h, persiane, tenda=None, cassetta=False, lamelle=True, luce='#d6d2b8', telaio='#e8e2d0'):
    _n_fin[0] += 1
    acc = _n_fin[0] % 5 == 0
    luce = '#e8d2a0' if acc else '#2a3238'
    nome = 'fc5_{}_{}_{}_{}_{}_{}_{}_{}'.format(int(w * 100), int(h * 100), (persiane or 'x')[1:], (tenda or 'x')[1:], int(cassetta), int(lamelle), luce[1:], telaio[1:])
    if _lod[0] == 1:
        nome = 'lite_' + nome
        return REG[nome] if nome in REG else finestra_lite(nome, w, h, persiane, luce, telaio, int(acc))
    return REG[nome] if nome in REG else ed.finestra(nome, w, h, persiane=persiane, tenda=tenda, cassetta=cassetta, lamelle=lamelle, luce=luce, telaio=telaio, e=int(acc))


cn.fin_cache = fin_cache   # crea_edificio la cerca tra i globali del suo modulo


def scrivi(nome, cartella, tr):
    """.obj compatto: vertici a 1 cm unificati, UV (proiezione sull'asse dominante, come geo.scrivi_obj) senza duplicati."""
    mtl, usati, facce = [], set(), {}   # un solo oggetto con un submesh per materiale: Unity crea un MeshFilter solo
    vi, vv, vts, vtv = {}, [], {}, []
    for k, (key, v, t) in enumerate(tutte_le_parti(nome, tr)):
        mat, tinta, e, tag = key
        mn, gen, sc, _, _ = MATERIALI[mat]
        nm = '{}_{}{}'.format(mn, tinta[1:], '_e' if e else '')
        if nm not in usati:
            usati.add(nm)
            kd = ' '.join('{:.3f}'.format(int(tinta[i:i + 2], 16) / 255) for i in (1, 3, 5))
            mtl += ['newmtl ' + nm, 'Kd ' + kd]
            if gen != 'liscio':
                mtl.append('map_Kd texture/{}.png'.format(gen))
            if e:
                mtl.append('Ke ' + kd)
        righe = facce.setdefault(nm, [])
        for a, b, c in t:
            A, B, C = v[a], v[b], v[c]
            n = cross(sub(B, A), sub(C, A))
            ax = max(range(3), key=lambda i: abs(n[i]))
            f = []
            for p in (A, B, C):
                q = tuple(round(x, 2) for x in p)
                if q not in vi:
                    vi[q] = len(vv) + 1
                    vv.append(q)
                uv = (p[2], p[1]) if ax == 0 else (p[0], p[2]) if ax == 1 else (p[0], p[1])
                u = (round(uv[0] / sc, 3), round(uv[1] / sc, 3))
                if u not in vts:
                    vts[u] = len(vtv) + 1
                    vtv.append(u)
                f.append('{}/{}'.format(vi[q], vts[u]))
            righe.append('f ' + ' '.join(f))
    testa = ['# {} - v5, 1 unita = 1 metro, pivot al centro della sagoma'.format(nome), 'mtllib {}.mtl'.format(nome), 'o ' + nome]
    testa += ['v {:g} {:g} {:g}'.format(*p) for p in vv] + ['vt {:g} {:g}'.format(*u) for u in vtv]
    for nm, fs in facce.items():
        testa += ['usemtl ' + nm] + fs
    (cartella / (nome + '.obj')).write_text(chr(10).join(testa) + chr(10))
    (cartella / (nome + '.mtl')).write_text('\n'.join(mtl) + '\n')
    return len(vv)


def main():
    osm = json.loads((cn.D / 'canolo_osm.json').read_text())
    T = cn.Terreno()
    punti = [(round(a, 1), round(b, 1)) for a, b in cn.strade_canolo(T, osm)[1]]
    bar_p = next((p for p in osm['punti'] if p['tag'].get('amenity') == 'bar'), None)
    USCITA.mkdir(parents=True, exist_ok=True)
    for f in USCITA.glob('*.obj'):
        f.unlink()
    for f in USCITA.glob('*.mtl'):
        f.unlink()
    out, tri_tot = [], 0
    for e in osm['edifici']:
        pts = e['pts'][:-1] if e['pts'][0] == e['pts'][-1] else e['pts']
        if len(pts) < 3 or area(pts) < 6:
            continue
        bb = bb_unity(pts)
        if sovrappone(bb, PIAZZA) or sovrappone(bb, STANZE):
            continue
        rng = random.Random(e['id'])
        (cx, cz), u, v, w, d = cn.obb(e['pts'])
        lati = [((cx + u[0] * w / 2, cz + u[1] * w / 2), u, d, w), ((cx - u[0] * w / 2, cz - u[1] * w / 2), (-u[0], -u[1]), d, w),
                ((cx + v[0] * d / 2, cz + v[1] * d / 2), v, w, d), ((cx - v[0] * d / 2, cz - v[1] * d / 2), (-v[0], -v[1]), w, d)]
        lato = min(lati, key=lambda l: (round(cn.dist_strada(l[0][0], l[0][1], punti) / 4), -l[2]))   # il fronte guarda la strada piu' vicina
        (mx, mz), nf, Lf, Dd = lato
        th = math.atan2(nf[0], nf[1])
        ox, oz = cx + nf[0] * Dd / 2, cz + nf[1] * Dd / 2
        c, s = math.cos(th), math.sin(th)
        corn = [(ox + (a * c + b * s), oz + (-a * s + b * c)) for a in (-Lf / 2, Lf / 2) for b in (0, -Dd)] + [(cx, cz)]
        hs = [T.quota(x, z) for x, z in corn]
        fond = sum(hs) / len(hs) - min(hs)
        dd = math.hypot(cx - CENTRO_PIAZZA[0], cz - CENTRO_PIAZZA[1])
        lod = 0 if dd < LOD_DIST[0] else (1 if dd < LOD_DIST[1] else 2)
        livelli = int(e['tag'].get('building:levels', rng.choice([1, 2, 2, 3])) or 2)
        bar = bar_p is not None and e['id'] == bar_p['id']
        if bar:
            livelli = max(livelli, 2)
        # fondazione piu' profonda del calcolato: a runtime la quota viene dal Terrain di Unity, che puo' stare qualche metro sotto
        _lod[0] = lod
        m, h = cn.crea_edificio('b%d' % e['id'], Lf, Dd, livelli, rng, lod, fond + 1.0, 'bar' if bar else None)
        scrivi(m.nome, USCITA, (math.pi, 1.0, 0.0, 0.0, -Dd / 2))
        out.append({'id': e['id'], 'cx': round(cx, 2), 'cz': round(cz, 2), 'r': round(math.degrees(th), 2), 'w': round(Lf, 2), 'd': round(Dd, 2), 'lod': lod, 'h': round(h, 2)})
    # serra: stesso modello della v4 (struttura metallica, vetri, piante)
    for k, sr in enumerate(osm['serre']):
        (cx, cz), u, v, w, d = cn.obb(sr['pts'])
        nm = 'serra_%d' % sr['id']
        ms = Mod(nm)
        h = 3.2
        ms.add('metallo', '#9aa1a6', *caja(-w / 2, 0, -d / 2, w / 2, 0.3, d / 2))
        for (a, b) in ((-w / 2, -d / 2), (-w / 2, d / 2)):
            ms.add('vetro', '#bfd8c8', *caja(a, 0.3, b - 0.02, a + w, h, b + 0.02))
        for (a, b) in ((-w / 2, -d / 2), (w / 2, -d / 2)):
            ms.add('vetro', '#bfd8c8', *caja(a - 0.02, 0.3, b, a + 0.02, h, b + d))
        ms.add('vetro', '#bfd8c8', *caja(-w / 2, h, -d / 2, w / 2, h + 0.1, d / 2))
        for i in range(int(w / 2.5) + 1):
            x = -w / 2 + i * w / max(1, int(w / 2.5))
            for zz in (-d / 2, d / 2):
                ms.add('metallo', '#e8e4d4', *caja(x - 0.03, 0.3, zz - 0.04, x + 0.03, h + 0.1, zz + 0.04))
        for i in range(int(w / 1.5)):
            for j in range(int(d / 3)):
                ms.add('erba', '#4f8a3a', *esfera(-w / 2 + 1 + i * 1.5, 0.45, -d / 2 + 1.5 + j * 3, 0.5, 0.45, 0.5, 6, 4))
        scrivi(nm, USCITA, (math.pi, 1.0, 0.0, 0.0, 0.0))
        out.append({'id': sr['id'], 'cx': round(cx, 2), 'cz': round(cz, 2), 'r': round(math.degrees(math.atan2(u[0], u[1])) + 90, 2), 'w': round(w, 2), 'd': round(d, 2), 'lod': 2, 'h': h, 'serra': 1})
    tex = USCITA / 'texture'
    tex.mkdir(exist_ok=True)
    usate = set()
    for f in USCITA.glob('*.mtl'):
        usate |= {r.split('/')[-1] for r in f.read_text().split() if r.startswith('texture/')}
    for t in usate:
        shutil.copy(RADICE / 'obj_v3' / 'texture' / t, tex / t)
    (USCITA / 'edifici.json').write_text(json.dumps({'unita': 'metri', 'edifici': out}, separators=(',', ':')))
    kb = sum(f.stat().st_size for f in USCITA.rglob('*') if f.is_file()) // 1024
    print('{} oggetti ({} serre), {} KB totali in {}'.format(len(out), len(osm['serre']), kb, USCITA))


if __name__ == '__main__':
    main()
