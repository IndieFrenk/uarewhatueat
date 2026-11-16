"""Canolo Nuovo (RC) ricostruito dai dati reali: rilievo EU-DEM, edifici, strade, recinti, bosco, serra e laghetto di OpenStreetMap.
Dati: (c) OpenStreetMap contributors (ODbL). Quote: EU-DEM 25 m."""
import bisect
import json
import math
import random
from pathlib import Path

import edifici_v3 as ed
from canolo_geo import coordinate_griglia
from geo import Mod, REG, caja, cross, esfera, lastra, piano, prisma, sub, tubo

D = Path(__file__).resolve().parent.parent / 'dati'
LARGHEZZA_STRADA = {'residential': 4.6, 'tertiary': 6.0, 'unclassified': 4.8, 'service': 3.2, 'track': 3.0, 'footway': 1.6, 'path': 1.4}
INTONACI = ['#d9c9a5', '#e4dcc8', '#c9a98a', '#cfc9bd', '#b8b09a', '#d6a98a', '#e8d9b0', '#c8b690', '#bfae98']
TEGOLE = ['#8a4a3a', '#7a4034', '#6a3a30', '#9a5040', '#7d4a3c']
PERSIANE = ['#4b6a50', '#6a4a35', '#e8e2d0', '#3f5a6a', '#6a3a30']


class Terreno:
    """Rilievo a griglia non uniforme (fitta al centro); le quote sono relative al centro del paese."""

    def __init__(self):
        d = json.loads((D / 'canolo_quote.json').read_text())
        self.xs = coordinate_griglia()
        n = d['n']
        self.h = [[d['quote']['{},{}'.format(i, j)] for i in range(n)] for j in range(n)]
        self.h0 = self.quota(0, 0, assoluta=True)
        self.n = n

    def quota(self, x, z, assoluta=False):
        i = min(max(bisect.bisect_right(self.xs, x) - 1, 0), len(self.xs) - 2)
        j = min(max(bisect.bisect_right(self.xs, z) - 1, 0), len(self.xs) - 2)
        u = min(max((x - self.xs[i]) / (self.xs[i + 1] - self.xs[i]), 0), 1)
        v = min(max((z - self.xs[j]) / (self.xs[j + 1] - self.xs[j]), 0), 1)
        h00, h10, h01, h11 = self.h[j][i], self.h[j][i + 1], self.h[j + 1][i], self.h[j + 1][i + 1]
        y = h00 + (h10 - h00) * u + (h11 - h10) * v if u >= v else h00 + (h11 - h01) * u + (h01 - h00) * v
        return y if assoluta else y - self.h0

    def pendenza(self, x, z, r=15.0):
        gx = (self.quota(x + r, z) - self.quota(x - r, z)) / (2 * r)
        gz = (self.quota(x, z + r) - self.quota(x, z - r)) / (2 * r)
        return math.hypot(gx, gz)


def mesh_terreno(T):
    m = Mod('terreno_canolo')
    rng = random.Random(4)
    erbe = ['#4a7a2c', '#56803a', '#46722e', '#5a8240']
    n = T.n
    for j in range(n - 1):
        for i in range(n - 1):
            def P(a, b): return (T.xs[a], T.h[b][a] - T.h0, T.xs[b])
            for A, B, C in ((P(i, j), P(i + 1, j), P(i + 1, j + 1)), (P(i, j), P(i + 1, j + 1), P(i, j + 1))):
                nv = cross(sub(B, A), sub(C, A))
                if nv[1] < 0:   # la faccia deve guardare in su
                    B, C, nv = C, B, (-nv[0], -nv[1], -nv[2])
                ln = math.sqrt(nv[0] ** 2 + nv[1] ** 2 + nv[2] ** 2) or 1
                mat, tint = ('roccia', '#8a8c84') if nv[1] / ln < 0.78 else ('erba', erbe[rng.randrange(4)])
                m.add(mat, tint, [A, B, C], [(0, 1, 2)])
    return m


def poly_dentro(x, z, pts):
    dentro = False
    for k in range(len(pts) - 1):
        (x1, z1), (x2, z2) = pts[k], pts[k + 1]
        if (z1 > z) != (z2 > z) and x < (x2 - x1) * (z - z1) / (z2 - z1) + x1:
            dentro = not dentro
    return dentro


def obb(pts):
    p = pts[:-1] if pts[0] == pts[-1] else pts
    best = None
    for k in range(len(p)):
        ax, az = p[(k + 1) % len(p)][0] - p[k][0], p[(k + 1) % len(p)][1] - p[k][1]
        ln = math.hypot(ax, az)
        if ln < 1e-6:
            continue
        u, v = (ax / ln, az / ln), (-az / ln, ax / ln)
        us = [q[0] * u[0] + q[1] * u[1] for q in p]
        vs = [q[0] * v[0] + q[1] * v[1] for q in p]
        w, d = max(us) - min(us), max(vs) - min(vs)
        if best is None or w * d < best[0]:
            cu, cv = (max(us) + min(us)) / 2, (max(vs) + min(vs)) / 2
            best = (w * d, (u[0] * cu + v[0] * cv, u[1] * cu + v[1] * cv), u, v, w, d)
    return best[1:]


def strade_canolo(T, osm):
    m = Mod('strade_canolo')
    punti_strada = []
    ordine = sorted(osm['strade'], key=lambda s: -LARGHEZZA_STRADA.get(s['tag'].get('highway'), 3.0))
    for rango, s in enumerate(ordine):
        w = LARGHEZZA_STRADA.get(s['tag'].get('highway'), 3.0)
        mat, tint = ('terra', '#a08a6a') if s['tag'].get('highway') in ('track', 'path') else ('cemento', '#5b5e61')
        campioni = []
        pts = s['pts']
        for k in range(len(pts) - 1):
            (x1, z1), (x2, z2) = pts[k], pts[k + 1]
            L = math.hypot(x2 - x1, z2 - z1)
            for q in range(max(1, int(L / 5))):
                campioni.append((x1 + (x2 - x1) * q / max(1, int(L / 5)), z1 + (z2 - z1) * q / max(1, int(L / 5))))
        campioni.append(tuple(pts[-1]))
        punti_strada += campioni
        sinistra, destra = [], []
        for k, (x, z) in enumerate(campioni):
            a, b = campioni[max(k - 1, 0)], campioni[min(k + 1, len(campioni) - 1)]
            dx, dz = b[0] - a[0], b[1] - a[1]
            ln = math.hypot(dx, dz) or 1
            nx, nz = -dz / ln, dx / ln
            off = 0.10 + 0.012 * rango
            for lista, sg in ((sinistra, 1), (destra, -1)):
                px, pz = x + nx * w / 2 * sg, z + nz * w / 2 * sg
                lista.append((px, T.quota(px, pz) + off, pz))
        for k in range(len(campioni) - 1):
            quad = [sinistra[k], destra[k], destra[k + 1], sinistra[k + 1]]
            cx = sum(q[0] for q in quad) / 4
            cz = sum(q[2] for q in quad) / 4
            m.add(mat, tint, *piano(*quad, (cx, quad[0][1] + 10, cz)))
    return m, punti_strada


def dist_strada(x, z, punti):
    return min((math.hypot(x - a, z - b) for a, b in punti), default=1e9)


def fin_cache(w, h, persiane, tenda=None, cassetta=False, lamelle=True, luce='#e8d6a0', telaio='#e8e2d0'):
    nome = 'fc_{}_{}_{}_{}_{}_{}_{}_{}'.format(int(w * 100), int(h * 100), (persiane or 'x')[1:], (tenda or 'x')[1:], int(cassetta), int(lamelle), luce[1:], telaio[1:])
    return REG[nome] if nome in REG else ed.finestra(nome, w, h, persiane=persiane, tenda=tenda, cassetta=cassetta, lamelle=lamelle, luce=luce, telaio=telaio)


def por_cache(w, tint, vetro=False):
    nome = 'pc_{}_{}_{}'.format(int(w * 100), tint[1:], int(vetro))
    return REG[nome] if nome in REG else ed.porta(nome, w, 2.1, tint, vetro)


def crea_edificio(nome, Lf, Dd, livelli, rng, lod, fondazione=0.0, speciale=None):
    """Edificio con fronte a z=0 (guarda +Z), larghezza Lf lungo x e profondità Dd verso -z."""
    piccolo = Lf * Dd < 32
    h = 2.6 if piccolo else 3.1 * livelli + 0.25
    m = Mod(nome)
    parete = rng.choice(INTONACI)
    m.add('intonaco', parete, *caja(-Lf / 2, 0, -Dd, Lf / 2, h, 0))
    zoccolo = '#8d867a'
    m.add('pietra', zoccolo, *caja(-Lf / 2 - 0.04, -fondazione - 0.3, -Dd - 0.04, Lf / 2 + 0.04, 0.7, 0.04))
    m.add('pietra', zoccolo, *caja(-Lf / 2 - 0.05, 0.7, -Dd - 0.05, Lf / 2 + 0.05, 0.76, 0.05))
    rh = max(0.9, min(Lf, Dd) / 2 * (0.32 if not piccolo else 0.22))
    cresta = 'x' if Lf >= Dd else 'z'
    ed.tetto_a_falde(m, -Lf / 2 - 0.35, -Dd - 0.35, Lf / 2 + 0.35, 0.35, h - 0.08, h + rh, cresta, rng.choice(TEGOLE), '#5a3a30')
    if rng.random() < 0.55 and not piccolo:
        ed.comignolo(m, rng.uniform(-Lf / 4, Lf / 4), h + rh * 0.55, -Dd * rng.uniform(0.3, 0.7), 1.4)
    pers = rng.choice(PERSIANE)
    nfin = max(1, int(Lf / 3.2))
    porta_i = rng.randrange(nfin)
    sx = Lf / nfin
    if speciale == 'bar':
        pers = '#2f4a3a'
    if not piccolo:
        m.ins(por_cache(1.0, rng.choice(['#3b2a20', '#4b6a50', '#5b4030', '#6a3a30']), speciale == 'bar'), -Lf / 2 + sx * (porta_i + 0.5), 0, 0)
        if lod < 2:
            for k in range(livelli):
                for i in range(nfin):
                    if k == 0 and i == porta_i:
                        continue
                    opz = dict(persiane=pers if rng.random() < 0.85 else None, lamelle=lod == 0, cassetta=(k > 0 and rng.random() < 0.12),
                               tenda='#d9c9a0' if rng.random() < 0.3 else None, luce='#d6d2b8')
                    m.ins(fin_cache(1.0, 1.25, **opz), -Lf / 2 + sx * (i + 0.5), 3.1 * k + 1.0, 0)
            if lod <= 1:
                for k in range(livelli):   # retro della casa
                    for i in range(nfin):
                        if rng.random() < 0.75:
                            m.ins(fin_cache(1.0, 1.25, persiane=pers if rng.random() < 0.6 else None, lamelle=False, luce='#d6d2b8'), -Lf / 2 + sx * (i + 0.5), 3.1 * k + 1.0, -Dd, rot=180)
            if lod == 0:
                nl = int(Dd / 4.5)
                for k in range(livelli):
                    for q in range(nl):
                        for lato in (1, -1):
                            if rng.random() < 0.7:
                                m.ins(fin_cache(0.9, 1.2, persiane=pers, lamelle=False, luce='#d6d2b8'), lato * Lf / 2, 3.1 * k + 1.0, -Dd * (q + 1) / (nl + 1), rot=90 if lato > 0 else -90)
    else:
        m.ins(por_cache(0.9, '#5b4030'), rng.uniform(-Lf / 4, Lf / 4), 0, 0)
    return m, h


def costruisci_canolo():
    """Restituisce (elementi della scena, etichette, punto di vista). Costruisce i modelli nel registro."""
    T = Terreno()
    osm = json.loads((D / 'canolo_osm.json').read_text())
    items, etichette = [], []
    mesh_terreno(T)
    items.append({'m': 'terreno_canolo', 'p': [0, 0, 0]})
    mstr, pts_strada = strade_canolo(T, osm)
    items.append({'m': 'strade_canolo', 'p': [0, 0, 0]})
    punti = [(round(a, 1), round(b, 1)) for a, b in pts_strada]
    chiesa_p = next(p for p in osm['punti'] if p['tag'].get('amenity') == 'place_of_worship')
    bar_p = next((p for p in osm['punti'] if p['tag'].get('amenity') == 'bar'), None)
    speciali = {chiesa_p['id']: 'chiesa'}
    if bar_p:
        speciali[bar_p['id']] = 'bar'
    # il negozio, la macelleria, l'osteria e il negozio di piante della storia: gli edifici più vicini al centro
    cand = []
    for e in osm['edifici']:
        if e['id'] in speciali:
            continue
        (cx, cz), u, v, w, d = obb(e['pts'])
        if w * d >= 60:
            cand.append((math.hypot(cx, cz), e['id']))
    cand.sort()
    ruoli = dict(zip([c[1] for c in cand[:4]], ['negozio', 'macelleria', 'osteria', 'piante']))
    nomi_ruolo = {'negozio': 'Il negozio (proposta)', 'macelleria': 'Macelleria (proposta)', 'osteria': 'Osteria (proposta)', 'piante': 'Negozio di piante (proposta)'}
    n_ed = 0
    for e in osm['edifici']:
        rng = random.Random(e['id'])
        (cx, cz), u, v, w, d = obb(e['pts'])
        lati = [((cx + u[0] * w / 2, cz + u[1] * w / 2), u, d, w), ((cx - u[0] * w / 2, cz - u[1] * w / 2), (-u[0], -u[1]), d, w),
                ((cx + v[0] * d / 2, cz + v[1] * d / 2), v, w, d), ((cx - v[0] * d / 2, cz - v[1] * d / 2), (-v[0], -v[1]), w, d)]
        def dist_lato(l): return dist_strada(l[0][0], l[0][1], punti)
        lato = min(lati, key=lambda l: (round(dist_lato(l) / 4), -l[2]))
        (mx, mz), nf, Lf, Dd = lato
        th = math.atan2(nf[0], nf[1])
        ox, oz = cx + nf[0] * Dd / 2, cz + nf[1] * Dd / 2
        c, s = math.cos(th), math.sin(th)
        corn = [(ox + (a * c + b * s), oz + (-a * s + b * c)) for a in (-Lf / 2, Lf / 2) for b in (0, -Dd)] + [(cx, cz)]
        hs = [T.quota(x, z) for x, z in corn]
        y0 = sum(hs) / len(hs)
        fond = y0 - min(hs)
        dd = math.hypot(cx, cz)
        lod = 0 if dd < 170 else (1 if dd < 380 else 2)
        livelli = int(e['tag'].get('building:levels', rng.choice([1, 2, 2, 3])) or 2)
        sp = speciali.get(e['id']) or ruoli.get(e['id'])
        if sp in ('negozio', 'macelleria', 'osteria', 'piante', 'bar'):
            livelli = max(livelli, 2)
        if sp == 'chiesa':
            ch = REG['chiesa'] if 'chiesa' in REG else ed.chiesa()
            sc = min(Lf / 10.9, Dd / 14.0)
            items.append({'m': 'chiesa', 'p': [round(ox + c * 1.45 * sc, 2), round(y0, 2), round(oz - s * 1.45 * sc, 2)], 'r': round(math.degrees(th), 1), 's': round(sc, 3)})
            etichette.append({'t': 'Chiesa Madonna della Neve', 'p': [round(cx, 1), round(y0 + 14, 1), round(cz, 1)]})
            n_ed += 1
            continue
        m, h = crea_edificio('b%d' % e['id'], Lf, Dd, livelli, rng, lod, fond, 'bar' if sp == 'bar' else None)
        if sp in ('negozio', 'macelleria', 'osteria', 'piante', 'bar'):
            tinte = {'negozio': ('#3a5040', '#3f6a47', '#e6e0d0'), 'macelleria': ('#5a1a1a', '#a02a2a', '#f0ece0'), 'osteria': ('#3a2a20', '#c9503a', '#f0e6c8'),
                     'piante': ('#2f4a3a', '#3f6a47', '#e8f0d8'), 'bar': ('#2a1d14', '#a02a2a', '#e6e0d0')}[sp]
            ed.insegna(m, -min(Lf / 2 - 0.4, 2.4), min(Lf / 2 - 0.4, 2.4), 2.75, 3.3, 0.05, tinte[0], tinte[2])
            ed.tenda(m, -min(Lf / 2 - 0.2, 2.8), min(Lf / 2 - 0.2, 2.8), 2.15, 1.1, tinte[1], '#e6e0d0', 8)
            nome = nomi_ruolo.get(sp, 'Bar')
            etichette.append({'t': nome, 'p': [round(cx, 1), round(y0 + h + 6, 1), round(cz, 1)]})
        items.append({'m': m.nome, 'p': [round(ox, 2), round(y0, 2), round(oz, 2)], 'r': round(math.degrees(th), 1)})
        n_ed += 1
    # serra, laghetto, recinti
    for k, sr in enumerate(osm['serre']):
        (cx, cz), u, v, w, d = obb(sr['pts'])
        nm = 'serra_%d' % k
        ms = Mod(nm)
        h = 3.2
        ms.add('metallo', '#9aa1a6', *caja(-w / 2, 0, -d / 2, w / 2, 0.3, d / 2))
        for (a, b, c2, d2) in ((-w / 2, -d / 2, w / 2, -d / 2), (-w / 2, d / 2, w / 2, d / 2)):
            ms.add('vetro', '#bfd8c8', *caja(a, 0.3, b - 0.02, c2, h, b + 0.02), e=3)
        for (a, b, c2, d2) in ((-w / 2, -d / 2, -w / 2, d / 2), (w / 2, -d / 2, w / 2, d / 2)):
            ms.add('vetro', '#bfd8c8', *caja(a - 0.02, 0.3, b, a + 0.02, h, d2), e=3)
        ms.add('vetro', '#bfd8c8', *caja(-w / 2, h, -d / 2, w / 2, h + 0.1, d / 2), e=3)
        for i in range(int(w / 2.5) + 1):
            x = -w / 2 + i * w / max(1, int(w / 2.5))
            for zz in (-d / 2, d / 2):
                ms.add('metallo', '#e8e4d4', *caja(x - 0.03, 0.3, zz - 0.04, x + 0.03, h + 0.1, zz + 0.04))
        for i in range(int(w / 1.5)):
            for j in range(int(d / 3)):
                ms.add('erba', '#4f8a3a', *esfera(-w / 2 + 1 + i * 1.5, 0.45, -d / 2 + 1.5 + j * 3, 0.5, 0.45, 0.5, 6, 4))
        th = math.atan2(u[0], u[1])
        items.append({'m': nm, 'p': [round(cx, 1), round(T.quota(cx, cz) + 0.1, 2), round(cz, 1)], 'r': round(math.degrees(th) + 90, 1)})
        etichette.append({'t': 'Serra', 'p': [round(cx, 1), round(T.quota(cx, cz) + 9, 1), round(cz, 1)]})
    for k, ac in enumerate(osm['acque']):
        pts = ac['pts'][:-1]
        cx, cz = sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)
        yq = sum(T.quota(*p) for p in pts) / len(pts) + 0.25
        ma = Mod('acqua_%d' % k)
        for a in range(len(pts)):
            b = (a + 1) % len(pts)
            ma.add('acqua', '#3f6f82', *piano((cx, yq, cz), (pts[a][0], yq, pts[a][1]), (pts[b][0], yq, pts[b][1]), (pts[b][0], yq, pts[b][1]), (cx, yq + 5, cz)))
        items.append({'m': ma.nome, 'p': [0, 0, 0]})
        etichette.append({'t': 'Laghetto', 'p': [round(cx, 1), round(yq + 8, 1), round(cz, 1)]})
    mr = Mod('recinti_canolo')
    for rc in osm['recinti']:
        pts = rc['pts']
        if min(math.hypot(x, z) for x, z in pts) > 350:
            continue
        prec = None
        for k in range(len(pts) - 1):
            (x1, z1), (x2, z2) = pts[k], pts[k + 1]
            L = math.hypot(x2 - x1, z2 - z1)
            nn = max(1, int(L / 4))
            for q in range(nn + (1 if k == len(pts) - 2 else 0)):
                x, z = x1 + (x2 - x1) * q / nn, z1 + (z2 - z1) * q / nn
                y = T.quota(x, z)
                mr.add('legno', '#7a5a38', *caja(x - 0.05, y - 0.1, z - 0.05, x + 0.05, y + 1.0, z + 0.05))
                if prec:
                    dx, dz = x - prec[0], z - prec[2]
                    ln = math.hypot(dx, dz) or 1
                    nx, nz = -dz / ln, dx / ln
                    for dy in (0.5, 0.9):
                        quad = [(prec[0], prec[1] + dy - 0.05, prec[2]), (x, y + dy - 0.05, z), (x, y + dy + 0.05, z), (prec[0], prec[1] + dy + 0.05, prec[2])]
                        mx, my, mz = (prec[0] + x) / 2, (prec[1] + y) / 2 + dy, (prec[2] + z) / 2
                        for sg in (1, -1):
                            mr.add('legno', '#8a6a44', *piano(*quad, (mx + nx * 5 * sg, my, mz + nz * 5 * sg)))
                prec = (x, y, z)
    items.append({'m': 'recinti_canolo', 'p': [0, 0, 0]})
    # alberi: nel bosco mappato e sui versanti attorno
    abete = REG['abete'] if 'abete' in REG else ed.abete()
    lod_abete = Mod('abete_lod')
    lod_abete.add('corteccia', '#6a4a35', *prisma(0, 0, 0, 1.4, 0.2, 0.13, 5))
    lod_abete.add('aghi', '#2a5236', *prisma(0, 0, 0.8, 6.4, 1.6, 0, 6))
    rng = random.Random(21)
    pos_ed = [(i['p'][0], i['p'][2]) for i in items if i['m'].startswith('b') or i['m'] == 'chiesa']
    alberi = []
    for bo in osm['boschi']:
        xs_, zs_ = [p[0] for p in bo['pts']], [p[1] for p in bo['pts']]
        area = 0
        for _ in range(4000):
            if len(alberi) >= 600:
                break
            x, z = rng.uniform(min(xs_), max(xs_)), rng.uniform(min(zs_), max(zs_))
            if poly_dentro(x, z, bo['pts']) and dist_strada(x, z, punti) > 5:
                alberi.append((x, z))
    tent = 0
    while len(alberi) < 1500 and tent < 40000:
        tent += 1
        ang, r = rng.uniform(0, 2 * math.pi), 90 + 1500 * rng.random() ** 1.5
        x, z = r * math.cos(ang), r * math.sin(ang)
        if T.pendenza(x, z) > 0.9:
            continue
        if math.hypot(x, z) < 450 and dist_strada(x, z, punti) < 8:
            continue
        if any(abs(x - a) < 12 and abs(z - b) < 12 for a, b in pos_ed):
            continue
        if math.sin(x * 0.011) * math.cos(z * 0.013) + rng.random() * 0.6 < 0.15:
            continue
        alberi.append((x, z))
    for x, z in alberi:
        vicino = math.hypot(x, z) < 450
        items.append({'m': 'abete' if vicino else 'abete_lod', 'p': [round(x, 1), round(T.quota(x, z), 2), round(z, 1)], 'r': rng.randint(0, 359), 's': round(rng.uniform(0.8, 1.6), 2)})
    return items, etichette, T, n_ed
