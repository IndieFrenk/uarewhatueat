"""Base comune della v2: materiali con texture, modelli a istanze, primitive in più, esportazione .obj con UV."""
import math
from pathlib import Path

from genera import _orienta, caja, esfera, piano, prisma, sub, cross, dot, tetto, tronco, lastra  # noqa: F401

R = math.pi / 180

# nome, generatore di texture (in JS), metri per ripetizione, riflesso, rilievo
MATERIALI = [
    ('liscio', 'liscio', 1.0, 0.05, 0.0), ('intonaco', 'intonaco', 1.4, 0.04, 0.5), ('mattone', 'mattone', 1.0, 0.05, 1.0),
    ('pietra', 'pietra', 1.6, 0.10, 1.2), ('legno', 'legno', 0.9, 0.15, 0.6), ('piastrelle', 'piastrelle', 0.6, 0.35, 0.9),
    ('piastrelle_muro', 'piastrelle', 0.4, 0.45, 0.8), ('cemento', 'cemento', 1.5, 0.05, 0.5), ('metallo', 'metallo', 0.5, 0.65, 0.15),
    ('tessuto', 'tessuto', 0.55, 0.02, 0.25), ('pelle', 'pelle', 0.4, 0.12, 0.3), ('capelli', 'capelli', 0.15, 0.30, 0.5),
    ('cartone', 'cartone', 0.3, 0.02, 0.7), ('stampa_a', 'stampa_a', 0.25, 0.25, 0.2), ('stampa_b', 'stampa_b', 0.25, 0.25, 0.2),
    ('erba', 'erba', 1.6, 0.02, 0.8), ('selciato', 'selciato', 1.8, 0.10, 1.4), ('tegole', 'tegole', 1.2, 0.10, 1.2),
    ('corteccia', 'corteccia', 0.7, 0.03, 1.2), ('aghi', 'aghi', 0.9, 0.04, 1.0), ('roccia', 'roccia', 8.0, 0.08, 1.5),
    ('vetro', 'vetro', 1.0, 0.85, 0.0), ('terra', 'terra', 1.4, 0.02, 0.9), ('gomma', 'gomma', 0.3, 0.15, 0.4),
    ('insegna', 'insegna', 1.0, 0.15, 0.5), ('crosta', 'crosta', 0.35, 0.10, 0.8), ('acqua', 'vetro', 3.0, 0.9, 0.0),
]
MI = {m[0]: i for i, m in enumerate(MATERIALI)}
GEN = sorted({m[1] for m in MATERIALI}, key=[m[1] for m in MATERIALI].index)

REG = {}


def add3(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def mul3(a, k): return (a[0] * k, a[1] * k, a[2] * k)


def norm3(a):
    ln = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / ln, a[1] / ln, a[2] / ln)


def tubo(p0, p1, r0, r1, n=8):
    """Cilindro/tronco di cono tra due punti qualsiasi (arti, tubi, dischi)."""
    a = norm3(sub(p1, p0))
    u = norm3(cross(a, (0, 1, 0) if abs(a[1]) < 0.9 else (1, 0, 0)))
    w = cross(a, u)
    v = []
    for p, r in ((p0, r0), (p1, max(r1, 1e-5))):
        for i in range(n):
            g = 2 * math.pi * i / n
            v.append(add3(p, add3(mul3(u, r * math.cos(g)), mul3(w, r * math.sin(g)))))
    v += [p0, p1]
    t = []
    for i in range(n):
        j = (i + 1) % n
        t += [(i, j, n + j), (i, n + j, n + i), (i, j, 2 * n), (n + i, n + j, 2 * n + 1)]
    return v, _orienta(v, t)


def obliquo(x0, y0, z0, x1, y1, z1, dx=0.0, dz=0.0):
    """Parallelepipedo con la faccia alta spostata di (dx, dz): lamelle di persiane, falde."""
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1),
         (x0 + dx, y1, z0 + dz), (x1 + dx, y1, z0 + dz), (x1 + dx, y1, z1 + dz), (x0 + dx, y1, z1 + dz)]
    t = []
    for q in [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]:
        t += [(q[0], q[1], q[2]), (q[0], q[2], q[3])]
    return v, _orienta(v, t)


class Mod:
    """Un modello: parti geometriche (materiale, tinta, emissione, etichetta) e istanze di altri modelli."""

    def __init__(self, nome):
        assert nome not in REG, nome
        self.nome, self.parti, self.figli = nome, {}, []
        REG[nome] = self

    def add(self, mat, tinta, v, t, e=0, tag=''):
        p = self.parti.setdefault((MI[mat], tinta, e, tag), [[], []])
        off = len(p[0])
        p[0].extend(v)
        p[1].extend((a + off, b + off, c + off) for a, b, c in t)

    def ins(self, altro, tx=0.0, ty=0.0, tz=0.0, rot=0.0, s=1.0):
        self.figli.append((altro.nome, tx, ty, tz, rot, s))


def tutte_le_parti(nome, tr=(0.0, 1.0, 0.0, 0.0, 0.0)):
    """Appiattisce un modello con le sue istanze. tr = (rotazione rad, scala, tx, ty, tz). Restituisce (chiave, vertici, tri)."""
    r, k, tx, ty, tz = tr
    c, s = math.cos(r), math.sin(r)
    m = REG[nome]
    for key, (v, t) in m.parti.items():
        yield key, [((x * c + z * s) * k + tx, y * k + ty, (-x * s + z * c) * k + tz) for x, y, z in v], t
    for (fn, ax, ay, az, rot, sc) in m.figli:
        ntx, nty, ntz = (ax * c + az * s) * k + tx, ay * k + ty, (-ax * s + az * c) * k + tz
        yield from tutte_le_parti(fn, (r + rot * R, k * sc, ntx, nty, ntz))


def conta_tri(nome):
    return sum(len(t) for _, _, t in tutte_le_parti(nome))


def scrivi_obj(nome, cartella):
    """.obj con UV proiettate sull'asse dominante e .mtl che punta alle texture PNG."""
    righe, mtl, base, vtn = ['# {} - v2, 1 unita = 1 metro, Y su'.format(nome), 'mtllib {}.mtl'.format(nome)], [], 0, 0
    usati = {}
    for k, (key, v, t) in enumerate(tutte_le_parti(nome)):
        mat, tinta, e, tag = key
        mn, gen, sc, _, _ = MATERIALI[mat]
        nm = '{}_{}{}'.format(mn, tinta[1:], '_e' if e else '')
        if nm not in usati:
            usati[nm] = 1
            kd = ' '.join('{:.3f}'.format(int(tinta[i:i + 2], 16) / 255) for i in (1, 3, 5))
            mtl += ['newmtl ' + nm, 'Kd ' + kd]
            if gen != 'liscio':
                mtl.append('map_Kd texture/{}.png'.format(gen))
            if e:
                mtl.append('Ke ' + kd)
        righe += ['o {}_{:03d}'.format(tag or mn, k), 'usemtl ' + nm]
        righe += ['v {:.4f} {:.4f} {:.4f}'.format(*p) for p in v]
        for a, b, c in t:
            A, B, C = v[a], v[b], v[c]
            n = cross(sub(B, A), sub(C, A))
            ax = max(range(3), key=lambda i: abs(n[i]))
            uv = [((p[2], p[1]) if ax == 0 else (p[0], p[2]) if ax == 1 else (p[0], p[1])) for p in (A, B, C)]
            for x, y in uv:
                righe.append('vt {:.4f} {:.4f}'.format(x / sc, y / sc))
            righe.append('f {0}/{3} {1}/{4} {2}/{5}'.format(a + 1 + base, b + 1 + base, c + 1 + base, vtn + 1, vtn + 2, vtn + 3))
            vtn += 3
        base += len(v)
    (cartella / (nome + '.obj')).write_text('\n'.join(righe) + '\n')
    (cartella / (nome + '.mtl')).write_text('\n'.join(mtl) + '\n')
