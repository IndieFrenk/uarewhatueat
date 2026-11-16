#!/usr/bin/env python3
"""Genera i modelli 3D di esempio (file .obj) e la pagina v1.html con il visore.

Uso: python3 genera.py
Scrive: ../obj/*.obj, ../obj/*.mtl e ../v1.html (dal modello v1.template.html).
Unità: 1 = 1 metro, asse Y verso l'alto, i modelli guardano verso +Z.
"""
import json
import math
import random
from pathlib import Path

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
R = math.pi / 180


# ----------------------------------------------------------------- vettori
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _orienta(v, t):
    """Gira i triangoli in modo che guardino fuori dal solido (solidi convessi)."""
    c = [sum(p[i] for p in v) / len(v) for i in range(3)]
    out = []
    for a, b, d in t:
        A, B, C = v[a], v[b], v[d]
        n = cross(sub(B, A), sub(C, A))
        m = [(A[i] + B[i] + C[i]) / 3 - c[i] for i in range(3)]
        out.append((a, b, d) if dot(n, m) >= 0 else (a, d, b))
    return out


# --------------------------------------------------------------- primitive
def tronco(cx, cz, y0, y1, w0, d0, w1, d1):
    """Parallelepipedo rastremato: larghezza/profondità in basso (w0,d0) e in alto (w1,d1)."""
    v = [(cx - w0 / 2, y0, cz - d0 / 2), (cx + w0 / 2, y0, cz - d0 / 2),
         (cx + w0 / 2, y0, cz + d0 / 2), (cx - w0 / 2, y0, cz + d0 / 2),
         (cx - w1 / 2, y1, cz - d1 / 2), (cx + w1 / 2, y1, cz - d1 / 2),
         (cx + w1 / 2, y1, cz + d1 / 2), (cx - w1 / 2, y1, cz + d1 / 2)]
    t = []
    for q in [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]:
        t += [(q[0], q[1], q[2]), (q[0], q[2], q[3])]
    return v, _orienta(v, t)


def caja(x0, y0, z0, x1, y1, z1):
    return tronco((x0 + x1) / 2, (z0 + z1) / 2, y0, y1, x1 - x0, z1 - z0, x1 - x0, z1 - z0)


def prisma(cx, cz, y0, y1, r0, r1, n=8, fase=0.0, ex=1.0, ez=1.0):
    """Cilindro o cono (r1 = 0 per la punta) con n lati."""
    v = []
    for i in range(n):
        a = fase + 2 * math.pi * i / n
        v.append((cx + r0 * ex * math.cos(a), y0, cz + r0 * ez * math.sin(a)))
    if r1 > 0:
        for i in range(n):
            a = fase + 2 * math.pi * i / n
            v.append((cx + r1 * ex * math.cos(a), y1, cz + r1 * ez * math.sin(a)))
    else:
        v.append((cx, y1, cz))
    bc = len(v)
    v.append((cx, y0, cz))
    t = []
    for i in range(n):
        j = (i + 1) % n
        if r1 > 0:
            t += [(i, j, n + j), (i, n + j, n + i)]
        else:
            t.append((i, j, n))
        t.append((i, j, bc))
    if r1 > 0:
        tc = len(v)
        v.append((cx, y1, cz))
        for i in range(n):
            t.append((n + i, n + (i + 1) % n, tc))
    return v, _orienta(v, t)


def esfera(cx, cy, cz, rx, ry, rz, seg=8, anelli=5):
    v = [(cx, cy + ry, cz)]
    for k in range(1, anelli):
        ph = math.pi * k / anelli
        for i in range(seg):
            a = 2 * math.pi * i / seg
            v.append((cx + rx * math.sin(ph) * math.cos(a), cy + ry * math.cos(ph),
                      cz + rz * math.sin(ph) * math.sin(a)))
    v.append((cx, cy - ry, cz))
    bot = len(v) - 1
    t = [(0, 1 + i, 1 + (i + 1) % seg) for i in range(seg)]
    for k in range(anelli - 2):
        b = 1 + k * seg
        for i in range(seg):
            j = (i + 1) % seg
            t += [(b + i, b + seg + i, b + seg + j), (b + i, b + seg + j, b + j)]
    b = 1 + (anelli - 2) * seg
    t += [(b + i, bot, b + (i + 1) % seg) for i in range(seg)]
    return v, _orienta(v, t)


def tetto(x0, z0, x1, z1, y0, y1, cresta='x'):
    """Tetto a due falde. cresta='x': il colmo corre lungo X; 'z': lungo Z (timpano sul fronte)."""
    if cresta == 'x':
        zm = (z0 + z1) / 2
        v = [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1), (x0, y1, zm), (x1, y1, zm)]
        t = [(0, 1, 2), (0, 2, 3), (0, 1, 5), (0, 5, 4), (3, 2, 5), (3, 5, 4), (0, 3, 4), (1, 2, 5)]
    else:
        xm = (x0 + x1) / 2
        v = [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1), (xm, y1, z0), (xm, y1, z1)]
        t = [(0, 1, 2), (0, 2, 3), (0, 3, 5), (0, 5, 4), (1, 2, 5), (1, 5, 4), (0, 1, 4), (3, 2, 5)]
    return v, _orienta(v, t)


def piano(p0, p1, p2, p3, verso):
    """Quadrato singolo con la faccia rivolta verso il punto 'verso' (per pareti e pavimenti)."""
    n = cross(sub(p1, p0), sub(p2, p0))
    t = [(0, 1, 2), (0, 2, 3)] if dot(n, sub(verso, p0)) >= 0 else [(0, 2, 1), (0, 3, 2)]
    return [p0, p1, p2, p3], t


def lastra(x0, y0, z0, x1, y1, z1, verso):
    """Quadrato su un piano asse-allineato (due coordinate uguali -> parete, pavimento o soffitto)."""
    if abs(z0 - z1) < 1e-9:
        q = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0)]
    elif abs(x0 - x1) < 1e-9:
        q = [(x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0)]
    else:
        q = [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)]
    return piano(*q, verso)


# ------------------------------------------------------------------ modello
class Mod:
    def __init__(self, nome):
        self.nome = nome
        self.parti = {}  # (colore, emissione, etichetta) -> [vertici, triangoli]

    def add(self, colore, v, t, e=0, tag=''):
        p = self.parti.setdefault((colore, e, tag), [[], []])
        off = len(p[0])
        p[0].extend(v)
        p[1].extend((a + off, b + off, c + off) for a, b, c in t)

    def inserisci(self, altro, tx=0.0, ty=0.0, tz=0.0, rot=0.0, s=1.0):
        c, sn = math.cos(rot * R), math.sin(rot * R)
        for (col, e, tag), (v, t) in altro.parti.items():
            nv = [((x * c + z * sn) * s + tx, y * s + ty, (-x * sn + z * c) * s + tz) for x, y, z in v]
            self.add(col, nv, t, e, tag)

    def triangoli(self):
        return sum(len(t) for _, t in self.parti.values())


# ------------------------------------------------------------------ prodotti
NATURALI = ['pane', 'formaggio', 'uova', 'mela', 'latte', 'salame', 'miele', 'verdura']
INDUSTRIALI = ['patatine', 'lattina', 'merendina', 'caramelle', 'birra', 'sigarette', 'surgelati', 'lattina_blu']
PASSO = {'pane': 0.36, 'formaggio': 0.2, 'uova': 0.26, 'mela': 0.22, 'latte': 0.14, 'salame': 0.3, 'miele': 0.14,
         'verdura': 0.2, 'patatine': 0.24, 'lattina': 0.09, 'merendina': 0.2, 'caramelle': 0.24, 'birra': 0.09,
         'sigarette': 0.1, 'surgelati': 0.24, 'lattina_blu': 0.09}


def prodotto(m, tipo, x, y, z):
    """Un prodotto con la base appoggiata a quota y."""
    if tipo == 'pane':
        m.add('#b5793a', *esfera(x, y + 0.055, z, 0.16, 0.06, 0.07, 6, 4))
    elif tipo == 'formaggio':
        m.add('#e0bf4a', *caja(x - 0.07, y, z - 0.07, x + 0.07, y + 0.1, z + 0.07))
    elif tipo == 'uova':
        m.add('#d8cfb6', *caja(x - 0.1, y, z - 0.05, x + 0.1, y + 0.06, z + 0.05))
    elif tipo == 'mela':
        for dx in (-0.05, 0.05):
            m.add('#a8352c', *esfera(x + dx, y + 0.05, z, 0.05, 0.05, 0.05, 6, 4))
    elif tipo == 'latte':
        m.add('#e9ebee', *caja(x - 0.045, y, z - 0.045, x + 0.045, y + 0.2, z + 0.045))
        m.add('#3f6fb5', *caja(x - 0.047, y + 0.12, z - 0.047, x + 0.047, y + 0.16, z + 0.047))
    elif tipo == 'salame':
        m.add('#7a3b2e', *caja(x - 0.14, y, z - 0.03, x + 0.14, y + 0.06, z + 0.03))
    elif tipo == 'miele':
        m.add('#d99a1e', *prisma(x, z, y, y + 0.1, 0.05, 0.05, 6))
    elif tipo == 'verdura':
        m.add('#587f38', *esfera(x, y + 0.07, z, 0.075, 0.07, 0.075, 6, 4))
    elif tipo == 'patatine':
        m.add('#d6279a', *caja(x - 0.1, y, z - 0.03, x + 0.1, y + 0.28, z + 0.03))
        m.add('#f2d21b', *caja(x - 0.1, y + 0.1, z + 0.03, x + 0.1, y + 0.17, z + 0.035))
    elif tipo == 'lattina':
        m.add('#b8322a', *prisma(x, z, y, y + 0.12, 0.033, 0.033, 8))
    elif tipo == 'lattina_blu':
        m.add('#2f6fcf', *prisma(x, z, y, y + 0.12, 0.033, 0.033, 8))
    elif tipo == 'merendina':
        m.add('#2e86de', *caja(x - 0.07, y, z - 0.025, x + 0.07, y + 0.11, z + 0.025))
        m.add('#ffd23f', *caja(x - 0.05, y + 0.04, z + 0.025, x + 0.05, y + 0.07, z + 0.03))
    elif tipo == 'caramelle':
        m.add('#ff7eb6', *caja(x - 0.09, y, z - 0.05, x + 0.09, y + 0.14, z + 0.05))
        m.add('#8be0ff', *caja(x - 0.09, y + 0.14, z - 0.05, x + 0.09, y + 0.17, z + 0.05))
    elif tipo == 'birra':
        m.add('#2f6b3a', *prisma(x, z, y, y + 0.2, 0.035, 0.035, 8))
        m.add('#2f6b3a', *prisma(x, z, y + 0.2, y + 0.26, 0.035, 0.014, 8))
    elif tipo == 'sigarette':
        m.add('#d12e2e', *caja(x - 0.04, y, z - 0.015, x + 0.04, y + 0.09, z + 0.015))
        m.add('#eeeeee', *caja(x - 0.04, y + 0.06, z - 0.015, x + 0.04, y + 0.09, z + 0.017))
    elif tipo == 'surgelati':
        m.add('#3aa0ff', *caja(x - 0.1, y, z - 0.03, x + 0.1, y + 0.2, z + 0.03))
        m.add('#ffffff', *caja(x - 0.06, y + 0.05, z + 0.03, x + 0.06, y + 0.15, z + 0.034))


def riga(m, tipo, x0, x1, y, z):
    x = x0 + PASSO[tipo] / 2
    while x < x1 - 0.04:
        prodotto(m, tipo, x, y, z)
        x += PASSO[tipo]


# -------------------------------------------------------------------- arredi
def scaffale(livelli, w=1.5, d=0.45, h=2.0, nome='scaffale'):
    """Scaffale col dorso a z=0 e il fronte verso +z; 'livelli' = tipi di prodotto per ripiano (dal basso)."""
    m = Mod(nome)
    for x in (0.0, w - 0.04):
        for z in (0.0, d - 0.04):
            m.add('#6f767c', *caja(x, 0, z, x + 0.04, h, z + 0.04))
    m.add('#4d5359', *caja(0, 0, 0, w, h, 0.02))
    m.add('#a3a9ad', *caja(0, h - 0.04, 0, w, h, d))
    ys = [y for y in (0.15, 0.6, 1.05, 1.5) if y < h - 0.2]
    for y, tipo in zip(ys, livelli):
        m.add('#a3a9ad', *caja(0, y - 0.03, 0, w, y, d))
        if tipo:
            riga(m, tipo, 0.02, w - 0.02, y, 0.13)
            riga(m, tipo, 0.02, w - 0.02, y, 0.31)
    return m


def cassa():
    m = Mod('cassa')
    m.add('#6b4a2f', *caja(0, 0, 0, 0.8, 0.95, 1.6))
    m.add('#b0a38a', *caja(-0.05, 0.95, -0.05, 0.85, 0.99, 1.65))
    m.add('#4a4e52', *caja(0.15, 0.99, 0.1, 0.55, 1.15, 0.5))
    m.add('#3bbf6a', *caja(0.2, 1.15, 0.15, 0.5, 1.3, 0.19), e=2)
    m.add('#2b2b2b', *caja(0.1, 0.99, 0.7, 0.7, 1.0, 1.15))
    m.add('#c28b3a', *caja(0.1, 0.99, 1.25, 0.4, 1.1, 1.5))
    for i, c in enumerate(['#ff7eb6', '#ffd23f', '#8be0ff', '#ff7eb6']):
        m.add(c, *esfera(0.16 + 0.07 * (i % 2), 1.13, 1.32 + 0.06 * (i // 2), 0.04, 0.03, 0.04, 6, 4))
    return m


def banco_affettati():
    m = Mod('banco_affettati')
    m.add('#d8d2c0', *caja(0, 0, 0, 0.9, 0.85, 2.5))
    m.add('#9aa1a6', *caja(-0.03, 0.85, -0.03, 0.93, 0.89, 2.53))
    for x in (0.04, 0.84):
        for z in (0.02, 2.46):
            m.add('#9aa1a6', *caja(x - 0.015, 0.89, z - 0.015, x + 0.015, 1.38, z + 0.015))
    m.add('#9aa1a6', *caja(0.02, 1.38, 0.0, 0.88, 1.42, 2.5))
    m.add('#fff0c8', *caja(0.1, 1.36, 0.1, 0.8, 1.38, 2.3), e=2)
    zs = [0.25, 0.6, 0.95, 1.3, 1.65]
    for z, tipo in zip(zs, ['salame', 'formaggio', 'salame', 'formaggio', 'salame']):
        prodotto(m, tipo, 0.45, 0.89, z)
    m.add('#d98a8a', *esfera(0.25, 0.97, 1.95, 0.1, 0.08, 0.1, 6, 4))
    m.add('#d98a8a', *esfera(0.62, 0.97, 1.95, 0.1, 0.08, 0.1, 6, 4))
    m.add('#3a3d40', *caja(0.15, 0.89, 2.18, 0.45, 0.94, 2.46))
    m.add('#c8ced2', *caja(0.28, 0.94, 2.25, 0.31, 1.18, 2.42))
    return m


def cestone():
    m = Mod('cestone')
    m.add('#7a5a3a', *caja(0, 0, 0, 0.8, 0.35, 0.6))
    for i in range(6):
        c = ['#a8352c', '#587f38', '#a8352c', '#d8a21e', '#587f38', '#a8352c'][i]
        m.add(c, *esfera(0.14 + 0.13 * (i % 5), 0.4, 0.17 + 0.25 * (i // 5 + (i % 2)), 0.06, 0.055, 0.06, 6, 4))
    return m


PAV_A, PAV_B = '#8b8b78', '#6c6e60'
MURO, ZOCCOLO, SOFFITTO = '#b6ae8e', '#6e7a63', '#c9c6b4'


def negozio_interno():
    """Bottega 5 x 8 m. Entrando dalla porta (z=0) la cassa sta subito a sinistra (lato x=5)."""
    m = Mod('negozio_interno')
    C = (2.5, 1.5, 4.0)
    H = 3.0
    for i in range(10):
        for j in range(16):
            m.add(PAV_A if (i + j) % 2 == 0 else PAV_B,
                  *lastra(i * 0.5, 0, j * 0.5, i * 0.5 + 0.5, 0, j * 0.5 + 0.5, C))
    for (x0, x1) in ((0, 2.7), (3.8, 5)):
        m.add(MURO, *lastra(x0, 0, 0, x1, H, 0, C))
        m.add(ZOCCOLO, *lastra(x0, 0, 0.01, x1, 1.1, 0.01, C))
    m.add(MURO, *lastra(2.7, 2.2, 0, 3.8, H, 0, C))
    m.add(MURO, *lastra(0, 0, 8, 5, H, 8, C))
    m.add(ZOCCOLO, *lastra(0, 0, 7.99, 5, 1.1, 7.99, C))
    m.add(MURO, *lastra(0, 0, 0, 0, H, 8, C))
    m.add(ZOCCOLO, *lastra(0.01, 0, 0, 0.01, 1.1, 8, C))
    m.add(MURO, *lastra(5, 0, 0, 5, H, 8, C))
    m.add(ZOCCOLO, *lastra(4.99, 0, 0, 4.99, 1.1, 8, C))
    m.add(SOFFITTO, *lastra(0, H, 0, 5, H, 8, C))
    m.add('#3b2a20', *caja(2.65, 0, 0, 2.72, 2.2, 0.12))
    m.add('#3b2a20', *caja(3.78, 0, 0, 3.85, 2.2, 0.12))
    m.add('#3a2f2a', *caja(2.75, 0, 0.1, 3.75, 0.012, 0.9))
    # neon sul soffitto
    for z in (1.5, 4.0, 6.5):
        m.add('#5a5f63', *caja(1.6, 2.96, z - 0.1, 3.4, H, z + 0.1))
        m.add('#e8f4ff', *caja(1.7, 2.9, z - 0.06, 3.3, 2.96, z + 0.06), e=2)
    # manifesti e calendario
    m.add('#c0392b', *lastra(3.3, 1.5, 7.985, 4.1, 2.3, 7.985, C))
    m.add('#f1e4a8', *lastra(3.45, 1.7, 7.98, 3.95, 2.1, 7.98, C))
    m.add('#e8d9a0', *lastra(1.0, 1.7, 7.985, 1.5, 2.4, 7.985, C))
    # scaffali: parete destra (prodotti industriali, molto colorati), fondo, isola centrale
    industriali = [['patatine', 'caramelle', 'merendina', 'surgelati'],
                   ['lattina', 'birra', 'sigarette', 'lattina_blu'],
                   ['merendina', 'patatine', 'caramelle', 'lattina'],
                   ['birra', 'surgelati', 'patatine', 'merendina']]
    for k, z_fine in enumerate((3.1, 4.6, 6.1, 7.6)):
        m.inserisci(scaffale(industriali[k]), 0.03, 0, z_fine, 90)
    naturali = [['pane', 'verdura', 'uova', 'mela'], ['formaggio', 'latte', 'miele', 'salame']]
    m.inserisci(scaffale(naturali[0]), 2.1, 0, 7.97, 180)
    m.inserisci(scaffale(naturali[1]), 3.6, 0, 7.97, 180)
    m.inserisci(scaffale(['caramelle', 'merendina', 'patatine', 'lattina'], h=1.55), 1.85, 0, 2.8, -90)
    m.inserisci(scaffale(['latte', 'uova', 'mela', 'miele'], h=1.55), 1.85, 0, 5.8, 90)
    m.inserisci(scaffale(['merendina', 'caramelle', 'lattina_blu', 'surgelati'], h=1.55), 1.85, 0, 4.3, -90)
    m.inserisci(scaffale(['pane', 'verdura', 'formaggio', 'mela'], h=1.55), 1.85, 0, 4.3, 90)
    # pane in fondo a sinistra, banco affettati, cassa
    m.inserisci(scaffale(['pane', 'pane', 'pane', None], nome='pane'), 4.97, 0, 5.6, -90)
    m.inserisci(banco_affettati(), 4.1, 0, 2.7)
    m.inserisci(cassa(), 4.2, 0, 0.3)
    m.inserisci(cestone(), 0.7, 0, 0.6)
    return m


# -------------------------------------------------------------------- persone
def persona(nome, h=1.75, ancho=1.0, hs=1.0, camicia='#6b7a8f', pantaloni='#3b3f47', pelle='#d2a98a',
            capelli='#4a3b30', grembiule=None, tonaca=None, vestito=None, trecce=False, caramella=False,
            chierica=False, marchio=True):
    """Corpo semplice a pochi poligoni, volto quasi vuoto (solo occhi e naso) e marchio sulla fronte."""
    m = Mod(nome)
    s = h / 1.75
    if not tonaca:
        for sx in (-1, 1):
            gx = sx * 0.095 * ancho * s
            m.add(pantaloni, *prisma(gx, 0, 0.06 * s, 0.9 * s, 0.062 * s, 0.085 * s, 6))
            m.add('#2b2623', *caja(gx - 0.05 * s, 0, -0.04 * s, gx + 0.05 * s, 0.07 * s, 0.16 * s))
    if tonaca:
        m.add(tonaca, *tronco(0, 0, 0.03, 1.46 * s, 0.58 * s, 0.36 * s, 0.42 * s, 0.24 * s))
        m.add('#f2f2f2', *caja(-0.04 * s, 1.44 * s, 0.08 * s, 0.04 * s, 1.52 * s, 0.12 * s))
    else:
        m.add(camicia, *tronco(0, 0, 0.86 * s, 1.46 * s, 0.34 * ancho * s, 0.21 * s, 0.42 * ancho * s, 0.24 * s))
    if vestito:
        m.add(vestito, *tronco(0, 0, 0.45 * s, 1.1 * s, 0.46 * s, 0.3 * s, 0.30 * s, 0.2 * s))
    if grembiule:
        m.add(grembiule, *caja(-0.13 * ancho * s, 0.75 * s, 0.108 * s, 0.13 * ancho * s, 1.35 * s, 0.135 * s))
        if grembiule == '#d9d2c0':
            m.add('#8e1f1f', *caja(-0.06 * s, 0.95 * s, 0.134 * s, 0.05 * s, 1.1 * s, 0.14 * s))
    manica = tonaca or camicia
    for sx in (-1, 1):
        ax = sx * 0.25 * ancho * s
        m.add(manica, *tronco(ax, 0, 0.8 * s, 1.42 * s, 0.085 * s, 0.085 * s, 0.1 * s, 0.1 * s))
        m.add(pelle, *caja(ax - 0.04 * s, 0.7 * s, -0.04 * s, ax + 0.04 * s, 0.82 * s, 0.04 * s))
    if caramella:
        m.add('#ff5fa2', *caja(0.25 * s - 0.03, 0.66 * s, 0.05 * s, 0.25 * s + 0.03, 0.73 * s, 0.14 * s))
    m.add(pelle, *caja(-0.045 * s, 1.44 * s, -0.045 * s, 0.045 * s, 1.55 * s, 0.045 * s))
    rx, ry, rz = 0.105 * s * hs, 0.125 * s * hs, 0.115 * s * hs
    cy = 1.52 * s + ry * 0.9
    m.add(pelle, *esfera(0, cy, 0, rx, ry, rz, 8, 6))
    if chierica:
        m.add(capelli, *esfera(0, cy + ry * 0.25, -0.01 * s, rx * 1.06, ry * 0.45, rz * 1.06, 8, 4))
    else:
        m.add(capelli, *esfera(0, cy + ry * 0.5, -0.008 * s, rx * 1.07, ry * 0.62, rz * 1.08, 8, 4))
    for sx in (-1, 1):
        ex = sx * 0.04 * s * hs
        m.add('#1a1818', *caja(ex - 0.012 * s, cy + ry * 0.1, rz * 0.88, ex + 0.012 * s, cy + ry * 0.1 + 0.02 * s, rz))
        if trecce:
            m.add(capelli, *esfera(sx * (rx + 0.03 * s), cy - ry * 0.4, -0.02 * s, 0.045 * s, 0.07 * s, 0.045 * s, 6, 4))
    m.add('#c29878', *caja(-0.012 * s, cy - ry * 0.25, rz * 0.9, 0.012 * s, cy + ry * 0.05, rz * 1.14))
    # marchio sulla fronte (parte a sé: si può accendere e spegnere)
    if not marchio:
        return m
    yy, k, z0, z1 = cy + ry * 0.5, 1.9 * s * hs, rz * 0.8, rz * 1.06
    glifo = '#ffb347'
    m.add(glifo, *caja(-0.008 * k, yy - 0.03 * k, z0, 0.008 * k, yy + 0.03 * k, z1), e=2, tag='marchio')
    m.add(glifo, *caja(-0.03 * k, yy + 0.012 * k, z0, 0.03 * k, yy + 0.028 * k, z1), e=2, tag='marchio')
    m.add(glifo, *caja(-0.034 * k, yy - 0.03 * k, z0, -0.02 * k, yy - 0.012 * k, z1), e=2, tag='marchio')
    m.add(glifo, *caja(0.02 * k, yy - 0.03 * k, z0, 0.034 * k, yy - 0.012 * k, z1), e=2, tag='marchio')
    return m


# ---------------------------------------------------------------------- edifici
def fin_fronte(m, x, y0, y1, w, z=0.0, col='#e8d6a0'):
    m.add('#3a3029', *caja(x - w / 2 - 0.08, y0 - 0.08, z - 0.03, x + w / 2 + 0.08, y1 + 0.08, z + 0.03))
    m.add(col, *caja(x - w / 2, y0, z - 0.02, x + w / 2, y1, z + 0.06), e=1)


def fin_lato(m, xs, y0, y1, zc, w, col='#e8d6a0'):
    a, b = (xs - 0.02, xs + 0.06) if xs > 0 else (xs - 0.06, xs + 0.02)
    m.add(col, *caja(a, y0, zc - w / 2, b, y1, zc + w / 2), e=1)


def porta(m, x, w=1.0, h=2.1, col='#3b2a20'):
    m.add(col, *caja(x - w / 2, 0, -0.02, x + w / 2, h, 0.08))


def tenda(m, x0, x1, y, prof, c1, c2, n=8):
    passo = (x1 - x0) / n
    for i in range(n):
        m.add(c1 if i % 2 == 0 else c2,
              *tronco((x0 + passo * (i + 0.5)), prof / 2, y, y + 0.35, passo, prof, passo, prof * 0.6))


def chiesa():
    m = Mod('chiesa')
    m.add('#b8aa92', *caja(-4, 0, -14, 4, 7, 0))
    m.add('#6a4a40', *tetto(-4.4, -14.4, 4.4, 0.4, 7, 10.2, 'z'))
    m.add('#b0a28a', *caja(-6.8, 0, -2.8, -4.0, 14, 0))
    m.add('#6a4a40', *prisma(-5.4, -1.4, 14, 18.5, 2.05, 0, 4, math.pi / 4))
    m.add('#4a4038', *caja(-5.45, 18.5, -1.45, -5.35, 19.8, -1.35))
    m.add('#4a4038', *caja(-5.8, 19.1, -1.45, -5.0, 19.3, -1.35))
    m.add('#1b1512', *caja(-6.0, 11.0, -0.05, -4.8, 13.0, 0.05))
    m.add('#3b2a20', *caja(-1.1, 0, -0.02, 1.1, 3.6, 0.15))
    m.add('#9c8f78', *caja(-1.5, 0, 0, -1.1, 4.0, 0.2))
    m.add('#9c8f78', *caja(1.1, 0, 0, 1.5, 4.0, 0.2))
    m.add('#9c8f78', *caja(-1.5, 3.6, 0, 1.5, 4.0, 0.2))
    m.add('#8a7e6a', *caja(-2.2, 0, 0, 2.2, 0.18, 1.2))
    m.add('#8a7e6a', *caja(-1.8, 0.18, 0, 1.8, 0.36, 0.8))
    m.add('#7a6c58', *caja(-0.9, 5.2, -0.02, 0.9, 7.0, 0.06))
    m.add('#c9b27a', *caja(-0.7, 5.4, -0.02, 0.7, 6.8, 0.08), e=1)
    for z in (-3.2, -6.4, -9.6, -12.4):
        fin_lato(m, 4.0, 2.4, 5.4, z, 0.7, '#c9b27a')
        fin_lato(m, -4.0, 2.4, 5.4, z, 0.7, '#c9b27a')
    return m


def bar():
    m = Mod('bar')
    m.add('#a8603f', *caja(-3.5, 0, -5, 3.5, 3.8, 0))
    m.add('#4a3b34', *caja(-3.7, 3.8, -5.2, 3.7, 4.0, 0.3))
    fin_fronte(m, -2.0, 1.0, 2.6, 1.9)
    fin_fronte(m, 2.0, 1.0, 2.6, 1.9)
    porta(m, 0, 0.9, 2.1)
    m.add('#ffb347', *caja(-1.5, 3.0, 0.02, 1.5, 3.65, 0.12), e=1)
    tenda(m, -3.2, 3.2, 2.75, 1.2, '#a02a2a', '#e6e0d0', 8)
    for x in (-2.2, 2.2):
        m.add('#7a5a3a', *prisma(x, 2.0, 0, 0.75, 0.04, 0.04, 6))
        m.add('#7a5a3a', *prisma(x, 2.0, 0.75, 0.78, 0.42, 0.42, 8))
        for dx, dz in ((-0.65, 0), (0.65, 0)):
            m.add('#5a4636', *caja(x + dx - 0.2, 0, 2.0 + dz - 0.2, x + dx + 0.2, 0.45, 2.0 + dz + 0.2))
    return m


def macelleria():
    m = Mod('macelleria')
    m.add('#d9d2c0', *caja(-2.5, 0, -5, 2.5, 3.6, 0))
    m.add('#5b4038', *caja(-2.7, 3.6, -5.2, 2.7, 3.8, 0.3))
    fin_fronte(m, -1.3, 0.9, 2.5, 1.9, col='#f0e6cf')
    porta(m, 1.6, 0.9, 2.1)
    m.add('#b02a2a', *caja(-1.2, 3.05, 0.02, 1.2, 3.5, 0.1), e=1)
    tenda(m, -2.4, 2.4, 2.7, 1.0, '#a02a2a', '#f0ece0', 6)
    return m


def casa():
    m = Mod('casa')
    m.add('#c9b79a', *caja(-3, 0, -7, 3, 5, 0))
    m.add('#5c3d36', *tetto(-3.4, -7.4, 3.4, 0.4, 5, 7.2, 'x'))
    m.add('#8a6a5a', *caja(1.6, 5.0, -5.4, 2.1, 7.6, -4.9))
    porta(m, -1.5, 1.0, 2.1)
    fin_fronte(m, 0.9, 1.2, 2.3, 1.1)
    fin_fronte(m, -1.5, 3.3, 4.4, 1.0)
    fin_fronte(m, 1.5, 3.3, 4.4, 1.0, col='#f0c070')
    fin_lato(m, 3.0, 1.2, 2.3, -3.5, 1.1)
    fin_lato(m, -3.0, 3.3, 4.4, -3.5, 1.0)
    return m


def negozio_esterno():
    m = Mod('negozio_esterno')
    m.add('#8fa58a', *caja(-3, 0, -8, 3, 3.8, 0))
    m.add('#5a5048', *tetto(-3.3, -8.3, 3.3, 0.3, 3.8, 5.0, 'x'))
    fin_fronte(m, -1.1, 0.8, 2.5, 2.8, col='#f1ecd0')
    porta(m, 1.9, 1.0, 2.1, '#4b6a50')
    m.add('#4b6a50', *caja(-2.4, 3.0, 0.02, 2.4, 3.65, 0.1))
    m.add('#e6e0d0', *caja(-2.3, 3.07, 0.1, 2.3, 3.58, 0.13))
    tenda(m, -2.9, 2.9, 2.75, 1.0, '#3f6a47', '#e6e0d0', 8)
    m.inserisci(cestone(), -2.6, 0, 0.45)
    m.inserisci(cestone(), -1.7, 0, 0.45)
    return m


def osteria():
    m = Mod('osteria')
    m.add('#b89c7a', *caja(-3.5, 0, -6, 3.5, 4.0, 0))
    for x in (-3.5, -1.2, 1.2, 3.5):
        m.add('#4a3326', *caja(x - 0.12, 0, -0.03, x + 0.12, 4.0, 0.08))
    m.add('#4a3326', *caja(-3.5, 2.8, -0.03, 3.5, 2.95, 0.08))
    m.add('#6a3f32', *tetto(-3.9, -6.4, 3.9, 0.4, 4.0, 6.0, 'x'))
    fin_fronte(m, -2.3, 1.0, 2.2, 1.2)
    fin_fronte(m, 2.3, 1.0, 2.2, 1.2)
    fin_fronte(m, 0, 3.15, 3.85, 1.2, col='#f0c070')
    porta(m, 0, 1.0, 2.2)
    m.add('#6b4a2f', *prisma(1.8, 0.9, 0, 0.9, 0.4, 0.36, 10))
    return m


def cabina_telefonica():
    m = Mod('cabina_telefonica')
    m.add('#b52a2a', *caja(-0.45, 0, -0.45, 0.45, 2.3, 0.45))
    m.add('#2b2b2b', *caja(-0.5, 2.3, -0.5, 0.5, 2.4, 0.5))
    for lato in ((-0.35, 0.9, 0.44, 0.35, 2.1, 0.46), (-0.35, 0.9, -0.46, 0.35, 2.1, -0.44),
                 (0.44, 0.9, -0.35, 0.46, 2.1, 0.35), (-0.46, 0.9, -0.35, -0.44, 2.1, 0.35)):
        m.add('#a9d4e0', *caja(*lato), e=1)
    return m


def lampione():
    m = Mod('lampione')
    m.add('#2e3236', *prisma(0, 0, 0, 4.2, 0.07, 0.04, 6))
    m.add('#fff0c8', *caja(-0.15, 4.0, -0.15, 0.15, 4.2, 0.15), e=2)
    m.add('#2e3236', *caja(-0.25, 4.2, -0.25, 0.25, 4.3, 0.25))
    return m


def abete():
    m = Mod('abete')
    m.add('#5a3f2a', *prisma(0, 0, 0, 1.2, 0.18, 0.14, 6))
    m.add('#2f5b3a', *prisma(0, 0, 1.0, 3.2, 1.5, 0, 8))
    m.add('#2a5236', *prisma(0, 0, 2.2, 4.6, 1.2, 0, 8))
    m.add('#244a30', *prisma(0, 0, 3.6, 6.2, 0.9, 0, 8))
    return m


def fontana():
    m = Mod('fontana')
    m.add('#8d8d87', *prisma(0, 0, 0, 0.5, 2.0, 2.0, 12))
    m.add('#4f7f8f', *prisma(0, 0, 0.5, 0.54, 1.75, 1.75, 12))
    m.add('#8d8d87', *prisma(0, 0, 0.5, 2.0, 0.25, 0.18, 8))
    m.add('#8d8d87', *prisma(0, 0, 1.8, 2.1, 0.75, 0.5, 10))
    return m


def montagna():
    m = Mod('montagna')
    m.add('#5b6670', *prisma(0, 0, 0, 55, 55, 0, 6, 0.3))
    m.add('#e6eaee', *prisma(0, 0, 40, 55, 15.5, 0, 6, 0.3))
    return m


def prodotti():
    """Tavolo con tutti i prodotti in fila: sopra i naturali, sotto gli industriali."""
    m = Mod('prodotti')
    m.add('#6b4a2f', *caja(-1.4, 0.0, -0.9, 1.4, 0.4, 0.9))
    m.add('#8a6a44', *caja(-1.45, 0.4, -0.95, 1.45, 0.45, 0.95))
    for i, tipo in enumerate(NATURALI):
        prodotto(m, tipo, -1.2 + i * 0.34, 0.45, -0.4)
    for i, tipo in enumerate(INDUSTRIALI):
        prodotto(m, tipo, -1.2 + i * 0.34, 0.45, 0.4)
    return m


def podio():
    m = Mod('podio')
    m.add('#2a2d2f', *prisma(0, 0, -0.06, 0, 5.5, 5.5, 24))
    return m


def terreno_negozio():
    m = Mod('terreno_negozio')
    m.add('#3b3d3f', *lastra(-40, -0.03, -40, 40, -0.03, 40, (0, 5, 0)))
    return m


def terreno_paese():
    m = Mod('terreno_paese')
    m.add('#5b6648', *lastra(-220, 0, -220, 220, 0, 220, (0, 5, 0)))
    m.add('#8a867a', *lastra(-10, 0.03, -10, 10, 0.03, 10, (0, 5, 0)))
    m.add('#7a7468', *lastra(-3.5, 0.04, 10, 3.5, 0.04, 14, (0, 5, 0)))
    return m


# ------------------------------------------------------------------- assemblaggio
def costruisci():
    persone = {
        'padre': persona('padre', 1.76, 1.0, 1.0, camicia='#7d8b6f', pantaloni='#3f4047', capelli='#6a6a66',
                         grembiule='#cfc8b4', marchio=False),
        'figlia': persona('figlia', 1.15, 0.9, 1.3, camicia='#d9c25a', pantaloni='#5a4f63', capelli='#4a3328',
                          vestito='#d9c25a', trecce=True, caramella=True),
        'parroco': persona('parroco', 1.78, 1.0, 1.0, tonaca='#1e1e22', capelli='#9a9a98', chierica=True),
        'macellaio': persona('macellaio', 1.72, 1.3, 1.05, camicia='#d9d2c0', pantaloni='#4a4a4a',
                             capelli='#3a2c24', grembiule='#d9d2c0'),
    }
    modelli = {m.nome: m for m in [
        negozio_interno(), scaffale(['patatine', 'lattina', 'pane', 'mela']), cassa(), banco_affettati(),
        prodotti(), chiesa(), bar(), macelleria(), casa(), negozio_esterno(), osteria(), cabina_telefonica(),
        lampione(), abete(), fontana(), montagna(), podio(), terreno_negozio(), terreno_paese()]}
    modelli.update(persone)
    return modelli


def scene():
    rng = random.Random(7)
    alberi = []
    while len(alberi) < 46:
        x, z = rng.uniform(-75, 75), rng.uniform(-75, 65)
        if abs(x) < 22 and -34 < z < 24:
            continue
        alberi.append({'m': 'abete', 'p': [round(x, 1), 0, round(z, 1)], 'r': rng.randint(0, 359),
                       's': round(rng.uniform(0.9, 1.5), 2)})
    montagne = [{'m': 'montagna', 'p': p, 'r': r, 's': s} for p, r, s in [
        ([-70, 0, -135], 10, 1.0), ([10, 0, -155], 40, 1.4), ([85, 0, -130], 70, 1.1),
        ([-140, 0, -105], 20, 1.2), ([145, 0, -115], 55, 1.0)]]
    neon = [{'p': [2.5, 2.8, z], 'c': [1.0, 0.96, 0.86], 'i': 1.5, 'on': 'sempre', 'f': 1 if z == 4.0 else 0}
            for z in (1.5, 4.0, 6.5)]
    return {
        'negozio': {
            'nome': 'Negozio', 'sole': 0.35,
            'items': [{'m': 'terreno_negozio', 'p': [0, 0, 0]}, {'m': 'negozio_interno', 'p': [0, 0, 0]},
                      {'m': 'figlia', 'p': [3.95, 0, 1.75], 'r': 90}],
            'luci': neon + [{'p': [4.5, 1.6, 1.0], 'c': [0.5, 1.0, 0.7], 'i': 0.7, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Cassa (subito a sinistra)', 'p': [4.6, 2.0, 1.1]},
                          {'t': 'Banco affettati e pane', 'p': [4.55, 1.6, 4.0]},
                          {'t': 'Scaffali industriali', 'p': [0.3, 2.15, 4.6]},
                          {'t': 'Scaffali naturali', 'p': [2.8, 2.15, 7.8]},
                          {'t': 'La figlia e i dolci', 'p': [3.95, 1.5, 1.75]}],
            'orbita': {'t': [2.5, 1.0, 4.0], 'd': 11.5, 'y': 40, 'p': 38},
            'fps': {'e': [3.25, 1.65, 0.5], 'y': 0, 'p': -3}},
        'personaggi': {
            'nome': 'Personaggi', 'sole': 0.7,
            'items': [{'m': 'podio', 'p': [0, 0, 0]},
                      {'m': 'padre', 'p': [-2.4, 0, 0], 'r': 12}, {'m': 'figlia', 'p': [-0.8, 0, 0.1], 'r': -8},
                      {'m': 'parroco', 'p': [0.9, 0, 0], 'r': -10}, {'m': 'macellaio', 'p': [2.5, 0, 0], 'r': -15}],
            'luci': [{'p': [-2.5, 2.6, 2.8], 'c': [1.0, 0.82, 0.6], 'i': 2.2, 'on': 'sempre', 'f': 0},
                     {'p': [3.2, 2.2, -2.0], 'c': [0.5, 0.65, 1.0], 'i': 1.6, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Il padre (senza marchio)', 'p': [-2.4, 1.95, 0]}, {'t': 'La figlia', 'p': [-0.8, 1.35, 0.1]},
                          {'t': 'Il parroco', 'p': [0.9, 2.0, 0]}, {'t': 'Il macellaio', 'p': [2.5, 1.95, 0]}],
            'orbita': {'t': [0, 1.0, 0], 'd': 6.2, 'y': 12, 'p': 7}},
        'paese': {
            'nome': 'Paese', 'sole': 1.0, 'nebbia': 0.4,
            'items': [{'m': 'terreno_paese', 'p': [0, 0, 0]}, {'m': 'fontana', 'p': [0, 0, 0]},
                      {'m': 'chiesa', 'p': [0, 0, -10]}, {'m': 'bar', 'p': [10, 0, 2], 'r': -90},
                      {'m': 'negozio_esterno', 'p': [-10, 0, 2], 'r': 90}, {'m': 'macelleria', 'p': [-10, 0, -7], 'r': 90},
                      {'m': 'osteria', 'p': [4, 0, 10], 'r': 180}, {'m': 'casa', 'p': [-6, 0, 10], 'r': 180},
                      {'m': 'cabina_telefonica', 'p': [9.5, 0, 8]},
                      {'m': 'lampione', 'p': [-6, 0, 5]}, {'m': 'lampione', 'p': [6, 0, -5]},
                      {'m': 'padre', 'p': [-8.3, 0, 2.2], 'r': -90}, {'m': 'figlia', 'p': [-8.2, 0, 3.4], 'r': -60},
                      {'m': 'parroco', 'p': [3, 0, -8.5]}, {'m': 'macellaio', 'p': [-8.4, 0, -6.5], 'r': -90}]
                     + montagne + alberi,
            'luci': [{'p': [-6, 4.1, 5], 'c': [1.0, 0.85, 0.6], 'i': 3.0, 'on': 'sera', 'f': 0},
                     {'p': [6, 4.1, -5], 'c': [1.0, 0.85, 0.6], 'i': 3.0, 'on': 'sera', 'f': 0},
                     {'p': [8.5, 2, 2], 'c': [1.0, 0.7, 0.4], 'i': 3.0, 'on': 'sera', 'f': 0},
                     {'p': [-8.5, 2, 2], 'c': [0.9, 0.95, 0.8], 'i': 3.0, 'on': 'sera', 'f': 0}],
            'etichette': [{'t': 'Chiesa', 'p': [0, 11, -10]}, {'t': 'Bar', 'p': [12, 5, 2]},
                          {'t': 'Il negozio', 'p': [-12, 6, 2]}, {'t': 'Macelleria', 'p': [-12, 5, -7]},
                          {'t': 'Osteria', 'p': [4, 6, 12]}, {'t': 'Casa', 'p': [-6, 9, 12]},
                          {'t': 'Cabina', 'p': [9.5, 3, 8]}, {'t': 'Piazza', 'p': [0, 2.6, 0]}],
            'orbita': {'t': [0, 2, -2], 'd': 46, 'y': 20, 'p': 28}},
        'prodotti': {
            'nome': 'Prodotti', 'sole': 0.8,
            'items': [{'m': 'podio', 'p': [0, 0, 0], 's': 1.3}, {'m': 'prodotti', 'p': [0, 0, 0], 's': 2.4}],
            'luci': [{'p': [-1.5, 3.0, 3.0], 'c': [1.0, 0.95, 0.85], 'i': 2.5, 'on': 'sempre', 'f': 0},
                     {'p': [3.5, 2.5, 2.0], 'c': [0.7, 0.8, 1.0], 'i': 1.4, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Fila dietro: naturali (pane, formaggio, uova, mele, latte, salame, miele, verdura)',
                           'p': [0, 2.3, -0.96]},
                          {'t': 'Fila davanti: industriali (patatine, lattine, merendine, caramelle, birra, sigarette, surgelati)',
                           'p': [0, 0.75, 2.2]}],
            'orbita': {'t': [0, 0.9, 0.2], 'd': 9, 'y': 0, 'p': 30}},
    }


def scrivi_obj(m, cartella):
    righe, mtl, base = ['# {} - esempio v1, 1 unita = 1 metro, Y su'.format(m.nome), 'mtllib {}.mtl'.format(m.nome)], [], 0
    for k, ((col, e, tag), (v, t)) in enumerate(m.parti.items()):
        nome = (tag or 'parte') + '_{:02d}'.format(k)
        mtl += ['newmtl m{}'.format(k),
                'Kd {:.3f} {:.3f} {:.3f}'.format(*(int(col[i:i + 2], 16) / 255 for i in (1, 3, 5)))]
        if e:
            mtl.append('Ke {:.3f} {:.3f} {:.3f}'.format(*(int(col[i:i + 2], 16) / 255 for i in (1, 3, 5))))
        righe += ['o ' + nome, 'usemtl m{}'.format(k)]
        righe += ['v {:.4f} {:.4f} {:.4f}'.format(*p) for p in v]
        righe += ['f {} {} {}'.format(a + 1 + base, b + 1 + base, c + 1 + base) for a, b, c in t]
        base += len(v)
    (cartella / (m.nome + '.obj')).write_text('\n'.join(righe) + '\n')
    (cartella / (m.nome + '.mtl')).write_text('\n'.join(mtl) + '\n')


def main():
    modelli = costruisci()
    cartella = RADICE / 'obj'
    cartella.mkdir(exist_ok=True)
    dati = {'modelli': {}, 'scene': scene(), 'file': []}
    for nome, m in modelli.items():
        scrivi_obj(m, cartella)
        dati['modelli'][nome] = {'parti': [
            {'c': c, 'e': e, 't': tag, 'v': [round(x * 100) for p in v for x in p], 'i': [i for tri in t for i in tri]}
            for (c, e, tag), (v, t) in m.parti.items()]}
        dati['file'].append({'nome': nome, 'tri': m.triangoli()})
    modello = (AQUI / 'v1.template.html').read_text()
    # un modello per riga: il file resta leggibile e gli strumenti non si bloccano su righe enormi
    j = lambda o: json.dumps(o, separators=(',', ':'))
    testo = '{"unita":"cm",\n"modelli":{\n' + ',\n'.join('"{}":{}'.format(n, j(m)) for n, m in dati['modelli'].items())
    testo += '},\n"scene":{\n' + ',\n'.join('"{}":{}'.format(n, j(s)) for n, s in dati['scene'].items())
    testo += '},\n"file":' + j(dati['file']) + '}'
    (RADICE / 'v1.html').write_text(modello.replace('__DATI__', testo))
    print('{} modelli, {} triangoli totali'.format(len(modelli), sum(f['tri'] for f in dati['file'])))


if __name__ == '__main__':
    main()
