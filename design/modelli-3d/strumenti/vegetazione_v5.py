#!/usr/bin/env python3
"""v5: vegetazione, muretti e staccionate low-poly (stile v3/v4) per Canolo.cs, un .obj per modello.

Uso: python3 vegetazione_v5.py  ->  design/modelli-3d/obj_v5/vegetazione/*.obj,.mtl,texture/  (+ copia in Assets/Resources/Models/v5/vegetazione, senza .meta)

Scrittura e convenzioni identiche a canolo_v5.py (scrivi(): vertici unificati, un submesh per materiale; l'importer OBJ di Unity specchia x).
Pivot: base del tronco / piede del modulo a y = 0, centrato in x e z. Rotazione di 180 gradi scritta nel file (come per gli edifici),
quindi in Unity il modello risulta (x, y, -z): i moduli lineari (muretto, staccionata) corrono lungo X, centrati, simmetrici in z.
Moduli: muretto 2 m, staccionata 3 m. Il basso LOD (_lod1) e' per gli alberi oltre ~150 m o nel bosco lontano.
"""
import math
import random
import shutil
from pathlib import Path

import canolo_v5 as c5
from geo import Mod, caja, esfera, prisma, tubo, tutte_le_parti

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
USCITA = RADICE / 'obj_v5' / 'vegetazione'
UNITY = RADICE.parent.parent / 'Assets' / 'Resources' / 'Models' / 'v5' / 'vegetazione'


def sasso_blob(cx, cy, cz, rx, ry, rz, rng, seg=7, anelli=4):
    """Sfera con i vertici sporcati radialmente: pietra irregolare."""
    v, t = esfera(cx, cy, cz, rx, ry, rz, seg, anelli)
    out = []
    for x, y, z in v:
        k = 1 + rng.uniform(-0.14, 0.14)
        out.append((cx + (x - cx) * k, cy + (y - cy) * k, cz + (z - cz) * k))
    return out, t


def abete_a():
    m = Mod('abete_a')
    m.add('corteccia', '#6a4a35', *prisma(0, 0, 0, 1.4, 0.2, 0.13, 5))
    for i, (y0, y1, r, t) in enumerate([(0.9, 2.7, 1.7, '#2f5b3a'), (1.9, 3.8, 1.45, '#2a5236'), (2.9, 4.8, 1.2, '#2f5b3a'),
                                        (3.9, 5.7, 0.95, '#244a30'), (4.8, 6.6, 0.7, '#2a5236')]):
        m.add('aghi', t, *prisma(0, 0, y0, y1, r, 0, 8, i * 0.4))
    return m


def abete_b():
    """Piu' slanciato e scuro, 4 piani a 7 lati."""
    m = Mod('abete_b')
    m.add('corteccia', '#5a3f2e', *prisma(0, 0, 0, 1.6, 0.18, 0.12, 5, 0.3))
    for i, (y0, y1, r, t) in enumerate([(1.1, 3.4, 1.35, '#244a30'), (2.5, 4.8, 1.1, '#1f4229'), (3.7, 6.1, 0.85, '#244a30'), (4.9, 7.4, 0.55, '#2a5236')]):
        m.add('aghi', t, *prisma(0, 0, y0, y1, r, 0, 7, i * 0.55))
    return m


def abete_lod1():
    """~40 triangoli: tronco a 4 lati e due coni a 6 lati."""
    m = Mod('abete_lod1')
    m.add('corteccia', '#6a4a35', *prisma(0, 0, 0, 1.2, 0.18, 0.13, 4))
    m.add('aghi', '#2a5236', *prisma(0, 0, 0.8, 3.6, 1.8, 0, 6))
    m.add('aghi', '#2f5b3a', *prisma(0, 0, 2.6, 6.6, 1.3, 0, 6, 0.5))
    return m


def castagno():
    m = Mod('castagno')
    m.add('corteccia', '#6e5238', *prisma(0, 0, 0, 2.4, 0.3, 0.2, 6))
    m.add('corteccia', '#6e5238', *tubo((0, 2.0, 0), (-0.9, 3.5, 0.3), 0.13, 0.07, 4))
    m.add('corteccia', '#6e5238', *tubo((0, 2.2, 0), (1.0, 3.4, -0.3), 0.12, 0.07, 4))
    rng = random.Random(11)
    for (x, y, z, rx, ry, rz, t) in [(0, 4.3, 0, 2.2, 1.7, 2.1, '#4a7a2a'), (-1.3, 3.6, 0.5, 1.5, 1.2, 1.5, '#3f6e2c'),
                                    (1.4, 3.5, -0.5, 1.5, 1.2, 1.5, '#4a7a2a'), (0.2, 5.3, 0.3, 1.3, 1.0, 1.3, '#5a8a32')]:
        m.add('erba', t, *sasso_blob(x, y, z, rx, ry, rz, rng))
    return m


def castagno_lod1():
    m = Mod('castagno_lod1')
    m.add('corteccia', '#6e5238', *prisma(0, 0, 0, 2.6, 0.28, 0.2, 4))
    m.add('erba', '#4a7a2a', *esfera(0, 4.0, 0, 2.3, 1.9, 2.2, 5, 3))
    m.add('erba', '#3f6e2c', *esfera(-0.8, 3.2, 0.4, 1.6, 1.2, 1.6, 5, 3))
    return m


def ulivo():
    m = Mod('ulivo')
    for p0, p1, r0, r1 in [((0, 0, 0), (0.25, 0.9, 0.1), 0.34, 0.26), ((0.25, 0.9, 0.1), (-0.1, 1.7, -0.15), 0.26, 0.2),
                           ((-0.1, 1.7, -0.15), (0.5, 2.5, 0.2), 0.2, 0.12), ((-0.1, 1.7, -0.15), (-0.8, 2.4, 0.0), 0.16, 0.09)]:
        m.add('corteccia', '#6b6455', *tubo(p0, p1, r0, r1, 5))
    rng = random.Random(23)
    for (x, y, z, rx, ry, rz, t) in [(0.4, 3.0, 0.2, 1.5, 0.8, 1.4, '#7d8a5a'), (-0.9, 2.9, 0.0, 1.2, 0.7, 1.2, '#6f7e50'),
                                    (0.0, 3.5, -0.3, 1.1, 0.6, 1.1, '#8a9866'), (0.9, 2.5, -0.6, 0.9, 0.55, 0.9, '#6f7e50')]:
        m.add('erba', t, *sasso_blob(x, y, z, rx, ry, rz, rng, 6, 3))
    return m


def cespuglio():
    m = Mod('cespuglio')
    rng = random.Random(5)
    for (x, y, z, rx, ry, rz, t) in [(0, 0.45, 0, 0.7, 0.55, 0.65, '#3f6e2c'), (0.55, 0.32, 0.2, 0.5, 0.4, 0.5, '#4a7a2a'), (-0.5, 0.3, -0.25, 0.5, 0.38, 0.5, '#365f26')]:
        m.add('erba', t, *sasso_blob(x, y, z, rx, ry, rz, rng, 6, 3))
    return m


def muretto():
    """Muretto a secco, modulo 2 m (x da -1 a 1), spessore ~0.5, alto 1.0 sopra il terreno, interrato di 0.3 per i pendii."""
    m = Mod('muretto')
    rng = random.Random(7)
    tinte = ['#9a9488', '#8a857a', '#a39d90', '#7f7a70']
    corsi = [(-0.3, 0.25, [0.65, 0.7, 0.65], 0.52), (0.25, 0.6, [0.5, 0.8, 0.7], 0.48), (0.6, 0.92, [0.7, 0.6, 0.7], 0.5), (0.92, 1.0, [1.0, 1.0], 0.54)]
    for y0, y1, lung, sp in corsi:
        x = -1.0
        for l in lung:
            d = sp / 2 + rng.uniform(-0.03, 0.03)
            m.add('pietra', rng.choice(tinte), *caja(x, y0, -d, x + l, y1 + rng.uniform(-0.03, 0.03), d))
            x += l
    return m


def staccionata():
    """Staccionata di legno, modulo 3 m: tre paletti e due traverse."""
    m = Mod('staccionata_v5')
    for x in (-1.4, 0.0, 1.4):
        m.add('legno', '#7a5a38', *caja(x - 0.06, -0.2, -0.06, x + 0.06, 1.1, 0.06))
    for y in (0.4, 0.8):
        m.add('legno', '#8a6a44', *caja(-1.5, y, -0.08, 1.5, y + 0.1, 0.08))
    return m


def sasso_a():
    m = Mod('sasso_a')
    rng = random.Random(31)
    m.add('pietra', '#8d8d86', *sasso_blob(0, 0.25, 0, 0.5, 0.32, 0.42, rng))
    m.add('pietra', '#7a7a74', *sasso_blob(0.4, 0.15, 0.2, 0.3, 0.2, 0.28, rng, 6, 3))
    return m


def sasso_b():
    m = Mod('sasso_b')
    rng = random.Random(47)
    m.add('pietra', '#95958d', *sasso_blob(0, 0.2, 0, 0.7, 0.28, 0.55, rng))
    m.add('pietra', '#84847d', *sasso_blob(-0.55, 0.12, 0.35, 0.35, 0.2, 0.3, rng, 6, 3))
    m.add('pietra', '#a0a098', *sasso_blob(0.45, 0.1, -0.4, 0.3, 0.17, 0.26, rng, 6, 3))
    return m


MODELLI = [abete_a, abete_b, abete_lod1, castagno, castagno_lod1, ulivo, cespuglio, muretto, staccionata, sasso_a, sasso_b]


def main():
    USCITA.mkdir(parents=True, exist_ok=True)
    for f in list(USCITA.glob('*.obj')) + list(USCITA.glob('*.mtl')):
        f.unlink()
    tot = 0
    for fn in MODELLI:
        m = fn()
        c5.scrivi(m.nome, USCITA, (math.pi, 1.0, 0.0, 0.0, 0.0))
        tri = sum(len(t) for _, _, t in tutte_le_parti(m.nome))
        tot += tri
        print('{:16s} {:4d} triangoli'.format(m.nome, tri))
    tex = USCITA / 'texture'
    tex.mkdir(exist_ok=True)
    usate = set()
    for f in USCITA.glob('*.mtl'):
        usate |= {r.split('/')[-1] for r in f.read_text().split() if r.startswith('texture/')}
    for t in usate:
        shutil.copy(RADICE / 'obj_v3' / 'texture' / t, tex / t)
    if UNITY.parent.exists():   # copia in Unity (i .meta li crea l'editor)
        shutil.copytree(USCITA, UNITY, dirs_exist_ok=True)   # sovrascrive, non cancella: i .meta dell'editor restano
    print('{} modelli, {} triangoli totali, texture: {}'.format(len(MODELLI), tot, ', '.join(sorted(usate))))


if __name__ == '__main__':
    main()
