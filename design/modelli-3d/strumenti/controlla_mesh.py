#!/usr/bin/env python3
"""Controlla un modello: facce complanari sovrapposte di parti diverse (z-fighting) e parti che galleggiano.

Uso: python3 controlla_mesh.py padre figlia parroco macellaio [--modulo modelli_v3]
"""
import importlib
import math
import sys
from collections import defaultdict

from geo import MATERIALI, REG, cross, dot, sub, tutte_le_parti

EPS_PIANO = 0.004   # 4 mm: due facce parallele più vicine di così litigano nello z-buffer
CELLA = 0.25


def tri_info(p):
    n = cross(sub(p[1], p[0]), sub(p[2], p[0]))
    ar = math.sqrt(dot(n, n)) / 2
    if ar < 1e-9:
        return None
    ln = 2 * ar
    n = (n[0] / ln, n[1] / ln, n[2] / ln)
    bb = [(min(q[i] for q in p), max(q[i] for q in p)) for i in range(3)]
    return n, ar, bb


def proietta(p, n):
    ax = max(range(3), key=lambda i: abs(n[i]))
    return [tuple(q[i] for i in range(3) if i != ax) for q in p]


def sovrapposti_2d(a, b):
    """True se i due triangoli 2D hanno area in comune (SAT con piccola tolleranza)."""
    for tri, altro in ((a, b), (b, a)):
        for i in range(3):
            p, q = tri[i], tri[(i + 1) % 3]
            ax = (-(q[1] - p[1]), q[0] - p[0])
            pa = [ax[0] * v[0] + ax[1] * v[1] for v in tri]
            pb = [ax[0] * v[0] + ax[1] * v[1] for v in altro]
            ln = math.hypot(*ax) or 1
            if max(pa) - min(pb) < 1e-6 * ln or max(pb) - min(pa) < 1e-6 * ln:
                return False
    return True


def controlla(nome):
    tris, etichette = [], {}
    for key, v, t in tutte_le_parti(nome):
        etichette[len(etichette)] = '{}:{}'.format(MATERIALI[key[0]][0], key[1])
        pid = len(etichette) - 1
        for a, b, c in t:
            p = (v[a], v[b], v[c])
            info = tri_info(p)
            if info and max(q[1] for q in p) > 0.002:   # ciò che sta sotto il suolo non si vede
                tris.append((pid, p, info))
    griglia = defaultdict(list)
    for k, (pid, p, (n, ar, bb)) in enumerate(tris):
        for ix in range(int(bb[0][0] // CELLA), int(bb[0][1] // CELLA) + 1):
            for iy in range(int(bb[1][0] // CELLA), int(bb[1][1] // CELLA) + 1):
                for iz in range(int(bb[2][0] // CELLA), int(bb[2][1] // CELLA) + 1):
                    griglia[(ix, iy, iz)].append(k)
    visti, coppie = set(), defaultdict(float)
    for celle in griglia.values():
        for i, a in enumerate(celle):
            for b in celle[i + 1:]:
                if (a, b) in visti or tris[a][0] == tris[b][0]:
                    continue
                visti.add((a, b))
                pa, Pa, (na, ara, bba) = tris[a]
                pb, Pb, (nb, arb, bbb) = tris[b]
                if dot(na, nb) < 0.995:  # solo facce che guardano dalla stessa parte (i contatti opposti sono interni e invisibili)
                    continue
                if abs(dot(na, sub(Pb[0], Pa[0]))) > EPS_PIANO:
                    continue
                if any(bba[i][1] < bbb[i][0] - 1e-4 or bbb[i][1] < bba[i][0] - 1e-4 for i in range(3)):
                    continue
                if sovrapposti_2d(proietta(Pa, na), proietta(Pb, na)):
                    coppie[tuple(sorted((etichette[pa], etichette[pb])))] += min(ara, arb)
    return len(tris), sorted(coppie.items(), key=lambda kv: -kv[1])


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    modulo = sys.argv[sys.argv.index('--modulo') + 1] if '--modulo' in sys.argv else 'modelli_v2'
    args = [a for a in args if a != modulo]
    mod = importlib.import_module(modulo)
    if hasattr(mod, 'personaggi'):
        mod.personaggi()
    for n in args:
        if n not in REG and hasattr(mod, n):
            getattr(mod, n)()
        nt, c = controlla(n)
        print('== {}: {} triangoli, {} coppie di parti con facce sovrapposte'.format(n, nt, len(c)))
        for (a, b), ar in c[:12]:
            print('   {:7.4f} m2  {}  <->  {}'.format(ar, a, b))
