"""Edifici e paesaggio v2: infissi con persiane, porte a pannelli, tetti a tegole, grondaie, tende, insegne, alberi."""
import math
import random

from geo import Mod, REG, caja, esfera, lastra, obliquo, piano, prisma, tetto, tronco, tubo
from modelli_v2 import prodotto


def finestra(nome, w=1.1, h=1.2, persiane='#4b6a50', tenda=None, telaio='#e8e2d0', luce='#e8d6a0', cassetta=False, e=1):
    """Infisso con il piano del muro in z=0 (davanzale e architrave in pietra, persiane aperte ai lati)."""
    m = Mod(nome)
    m.add('vetro', luce, *caja(-w / 2, 0, -0.04, w / 2, h, -0.005), e=e)
    for (x0, y0, x1, y1) in ((-w / 2 - 0.06, -0.04, -w / 2, h + 0.04), (w / 2, -0.04, w / 2 + 0.06, h + 0.04),
                             (-w / 2, h, w / 2, h + 0.05), (-w / 2, -0.04, w / 2, 0.0), (-0.018, 0, 0.018, h), (-w / 2, h * 0.58, w / 2, h * 0.58 + 0.035)):
        m.add('legno', telaio, *caja(x0, y0, -0.02, x1, y1, 0.06))
    m.add('pietra', '#b8b2a0', *caja(-w / 2 - 0.1, -0.085, -0.02, w / 2 + 0.1, -0.03, 0.19))
    m.add('pietra', '#b8b2a0', *caja(-w / 2 - 0.1, h + 0.05, -0.02, w / 2 + 0.1, h + 0.14, 0.08))
    if tenda:
        m.add('tessuto', tenda, *caja(-w / 2 + 0.02, 0.1, -0.025, -w * 0.12, h - 0.06, -0.012))
        m.add('tessuto', tenda, *caja(w * 0.12, 0.1, -0.025, w / 2 - 0.02, h - 0.06, -0.012))
    if persiane:
        for sx in (-1, 1):
            cx = sx * (w / 2 + 0.06 + w / 4)
            m.add('legno', persiane, *caja(cx - w / 4, -0.03, 0.0, cx + w / 4, h + 0.03, 0.035))
            for k in range(int((h + 0.06) / 0.075)):
                y = -0.01 + k * 0.075
                m.add('legno', persiane, *obliquo(cx - w / 4 + 0.035, y, 0.035, cx + w / 4 - 0.035, y + 0.045, 0.05, 0, 0.022))
    if cassetta:
        m.add('legno', '#6b4a2f', *caja(-w / 2 - 0.05, -0.2, 0.19, w / 2 + 0.05, -0.04, 0.4))
        for i in range(7):
            m.add('erba', '#4f8a3a', *esfera(-w / 2 + i * w / 6, -0.0, 0.29, 0.07, 0.1, 0.07, 6, 4))
            m.add('liscio', ['#d94a5a', '#f2d21b', '#e8e4f0'][i % 3], *esfera(-w / 2 + i * w / 6 + 0.02, 0.1, 0.31, 0.03, 0.03, 0.03, 5, 3))
    return m


def porta(nome, w=1.0, h=2.1, tinta='#3b2a20', vetro=False, soglia=True):
    m = Mod(nome)
    m.add('legno', tinta, *caja(-w / 2, 0, -0.03, w / 2, h, 0.04))
    for (x0, y0, x1, y1) in ((-w / 2 - 0.1, 0, -w / 2, h + 0.1), (w / 2, 0, w / 2 + 0.1, h + 0.1), (-w / 2 - 0.1, h, w / 2 + 0.1, h + 0.1)):
        m.add('pietra', '#a8a290', *caja(x0, y0, -0.03, x1, y1, 0.12))
    if vetro:
        m.add('vetro', '#cfe0d8', *caja(-w / 2 + 0.12, h * 0.55, 0.04, w / 2 - 0.12, h - 0.15, 0.05), e=1)
    else:
        for (x0, x1) in ((-w / 2 + 0.1, -0.03), (0.03, w / 2 - 0.1)):
            for (y0, y1) in ((0.15, 0.95), (1.05, h - 0.15)):
                m.add('legno', tinta, *caja(x0, y0, 0.04, x1, y1, 0.065))
                m.add('legno', '#5b4030' if tinta != '#5b4030' else '#3b2a20', *caja(x0 + 0.05, y0 + 0.05, 0.065, x1 - 0.05, y1 - 0.05, 0.075))
    m.add('metallo', '#c9a227', *caja(w / 2 - 0.16, 1.0, 0.04, w / 2 - 0.12, 1.12, 0.1))
    m.add('metallo', '#c9a227', *esfera(w / 2 - 0.14, 1.06, 0.1, 0.028, 0.028, 0.028, 6, 4))
    if soglia:
        m.add('pietra', '#9a9484', *caja(-w / 2 - 0.12, 0, 0.0, w / 2 + 0.12, 0.12, 0.45))
        m.add('pietra', '#aaa492', *caja(-w / 2 - 0.05, 0.12, 0.0, w / 2 + 0.05, 0.2, 0.25))
    return m


def tenda(m, x0, x1, y, prof, c1, c2, n=8):
    """Tenda da sole a strisce con mantovana, inclinata verso l'esterno."""
    dw = (x1 - x0) / n
    for i in range(n):
        c = c1 if i % 2 == 0 else c2
        x = x0 + i * dw
        m.add('tessuto', c, *obliquo(x, y, prof - 0.03, x + dw, y + 0.5, prof, 0, -(prof - 0.06)))
        m.add('tessuto', c, *caja(x, y - 0.2, prof - 0.03, x + dw, y + 0.01, prof))
        m.add('tessuto', c, *tubo((x + dw / 2, y - 0.2, prof - 0.015), (x + dw / 2 + 0.001, y - 0.2, prof - 0.015), dw / 2, dw / 2, 8))
    for x in (x0, x1):
        m.add('metallo', '#3a3d40', *tubo((x, y - 0.2, prof - 0.02), (x, y + 0.5, 0.05), 0.012, 0.012, 5))


def tetto_a_falde(m, x0, z0, x1, z1, y0, y1, cresta, tinta, tinta_colmo, timpano=None, rb=0.0):
    """Tetto a tegole come prisma pieno, colmo in rilievo, grondaie e (facoltativo) timpano intonacato."""
    m.add('tegole', tinta, *tetto(x0, z0, x1, z1, y0, y1, cresta))
    if cresta == 'x':
        zm = (z0 + z1) / 2
        m.add('tegole', tinta_colmo, *caja(x0 - 0.02, y1 - 0.02, zm - 0.1, x1 + 0.02, y1 + 0.09, zm + 0.1))
        for z in (z0, z1):
            m.add('metallo', '#6a6e72', *tubo((x0 + 0.05, y0 - 0.03, z), (x1 - 0.05, y0 - 0.03, z), 0.04, 0.04, 6))
        if timpano:
            for x, sg in ((x0 + 0.005, -1), (x1 - 0.005, 1)):
                pass
    else:
        xm = (x0 + x1) / 2
        m.add('tegole', tinta_colmo, *caja(xm - 0.1, y1 - 0.02, z0 - 0.02, xm + 0.1, y1 + 0.09, z1 + 0.02))
        for x in (x0, x1):
            m.add('metallo', '#6a6e72', *tubo((x, y0 - 0.03, z0 + 0.05), (x, y0 - 0.03, z1 - 0.05), 0.04, 0.04, 6))
        if timpano:
            mat, tint, zf = timpano
            m.add(mat, tint, *piano((x0 + 0.3, y0 + 0.05, zf), (x1 - 0.3, y0 + 0.05, zf), (xm, y1 - 0.12, zf), (xm, y1 - 0.12, zf), (xm, y0 + 0.5, zf + 5)))


def comignolo(m, x, y, z, h=1.4):
    m.add('mattone', '#8a5a48', *caja(x - 0.25, y, z - 0.25, x + 0.25, y + h, z + 0.25))
    m.add('cemento', '#8d8a82', *caja(x - 0.32, y + h, z - 0.32, x + 0.32, y + h + 0.08, z + 0.32))
    m.add('metallo', '#3a3d40', *caja(x - 0.18, y + h + 0.08, z - 0.18, x + 0.18, y + h + 0.14, z + 0.18))


def edificio(nome, w, d, h, mat, tinta, tegola, rh, cresta='x', finestre=(), laterali=(), porta_x=0.0, porta_w=1.0,
             porta_tinta='#3b2a20', zoccolo='#8d867a', colmo=None, comignoli=(), vetro_porta=False):
    """Edificio con fronte a z=0 che guarda +Z, pianta x in [-w/2, w/2], z in [-d, 0].
    finestre: (x, y_bordo_inferiore, larghezza, altezza, opzioni) sul fronte; laterali: (lato +1/-1, z, y, larghezza, altezza, opzioni)."""
    m = Mod(nome)
    m.add(mat, tinta, *caja(-w / 2, 0, -d, w / 2, h, 0))
    m.add('pietra', zoccolo, *caja(-w / 2 - 0.04, 0, -d - 0.04, w / 2 + 0.04, 0.7, 0.04))
    m.add('pietra', zoccolo, *caja(-w / 2 - 0.05, 0.7, -d - 0.05, w / 2 + 0.05, 0.76, 0.05))
    tetto_a_falde(m, -w / 2 - 0.35, -d - 0.35, w / 2 + 0.35, 0.35, h - 0.08, h + rh, cresta, tegola, colmo or tegola)
    m.ins(porta(nome + '_porta', porta_w, 2.1, porta_tinta, vetro_porta), porta_x, 0, 0)
    for i, (x, yb, fw, fh, opz) in enumerate(finestre):
        m.ins(finestra('%s_f%d' % (nome, i), fw, fh, **opz), x, yb, 0)
    for i, (lato, z, yb, fw, fh, opz) in enumerate(laterali):
        m.ins(finestra('%s_l%d' % (nome, i), fw, fh, **opz), lato * w / 2, yb, z, rot=90 if lato > 0 else -90)
    for (x, z) in comignoli:
        comignolo(m, x, h + rh * 0.55, z, 1.4)
    return m


def insegna(m, x0, x1, y0, y1, z, tinta_cornice='#3a3029', tinta='#e6e0d0'):
    m.add('legno', tinta_cornice, *caja(x0 - 0.07, y0 - 0.07, z, x1 + 0.07, y1 + 0.07, z + 0.1))
    m.add('stampa_a', tinta, *caja(x0, y0, z + 0.1, x1, y1, z + 0.115))
    m.add('legno', tinta_cornice, *caja(x0 - 0.04, y1 + 0.07, z - 0.02, x0 + 0.04, y1 + 0.3, z + 0.02))
    m.add('legno', tinta_cornice, *caja(x1 - 0.04, y1 + 0.07, z - 0.02, x1 + 0.04, y1 + 0.3, z + 0.02))


# ------------------------------------------------------------------ edifici del paese
def finestra_gotica():
    if 'finestra_gotica' in REG:
        return REG['finestra_gotica']
    m = Mod('finestra_gotica')
    w, h = 0.75, 2.2
    m.add('vetro', '#c9b27a', *caja(-w / 2, 0, -0.04, w / 2, h, -0.01), e=1)
    m.add('vetro', '#c9b27a', *tetto(-w / 2, -0.04, w / 2, -0.01, h, h + 0.42, 'z'), e=1)
    for (x0, y0, x1, y1) in ((-w / 2 - 0.08, -0.03, -w / 2, h + 0.03), (w / 2, -0.03, w / 2 + 0.08, h + 0.03), (-0.02, 0, 0.02, h + 0.3),
                             (-w / 2, h * 0.5, w / 2, h * 0.5 + 0.04), (-w / 2, -0.03, w / 2, 0.02)):
        m.add('pietra', '#a89d86', *caja(x0, y0, -0.03, x1, y1, 0.1))
    m.add('pietra', '#a89d86', *tetto(-w / 2 - 0.08, -0.03, w / 2 + 0.08, 0.1, h, h + 0.55, 'z'))
    return m


def chiesa():
    m = Mod('chiesa')
    m.add('intonaco', '#c9bfa6', *caja(-4, 0, -14, 4, 7, 0))
    m.add('pietra', '#9c917b', *caja(-4.05, 0, -14.05, 4.05, 1.3, 0.05))
    m.add('pietra', '#9c917b', *caja(-4.08, 1.3, -14.08, 4.08, 1.38, 0.08))
    tetto_a_falde(m, -4.5, -14.5, 4.5, 0.5, 7, 10.4, 'z', '#6a4a40', '#4a3028', timpano=('intonaco', '#c9bfa6', 0.51))
    for z in (-3.2, -6.4, -9.6, -12.4):
        for sx in (-1, 1):
            x0 = 4.0 if sx > 0 else -4.55
            m.add('pietra', '#a89d86', *caja(x0, 0, z - 0.25, x0 + 0.55, 4.6, z + 0.25))
            m.add('pietra', '#a89d86', *obliquo(x0, 4.6, z - 0.25, x0 + 0.55, 5.4, z + 0.25, -0.55 * sx, 0))
            if z != -12.4:
                m.ins(finestra_gotica(), sx * 4.0, 2.2, z - 1.6, rot=90 if sx > 0 else -90)
    # portale e rosone
    for (x0, x1) in ((-1.5, -1.15), (1.15, 1.5)):
        m.add('pietra', '#a89d86', *caja(x0, 0, 0, x1, 4.0, 0.2))
    m.add('pietra', '#a89d86', *caja(-1.5, 3.7, 0, 1.5, 4.0, 0.2))
    m.add('pietra', '#a89d86', *tetto(-1.5, 0, 1.5, 0.2, 4.0, 5.0, 'z'))
    m.add('legno', '#3b2a20', *caja(-1.15, 0, -0.02, 1.15, 3.7, 0.12))
    for sx in (-1, 1):
        for y in (0.3, 1.5, 2.7):
            m.add('legno', '#4b362a', *caja(sx * 0.55 - 0.4, y, 0.12, sx * 0.55 + 0.4, y + 1.0, 0.16))
        m.add('metallo', '#c9a227', *caja(sx * 0.1 - 0.015, 1.0, 0.16, sx * 0.1 + 0.015, 1.2, 0.2))
    m.add('pietra', '#8a7e6a', *caja(-2.4, 0, 0, 2.4, 0.18, 1.3))
    m.add('pietra', '#9a8e7a', *caja(-2.0, 0.18, 0, 2.0, 0.36, 0.9))
    m.add('pietra', '#a89d86', *tubo((0, 6.25, 0.0), (0, 6.25, 0.14), 0.95, 0.95, 20))
    m.add('vetro', '#c9b27a', *tubo((0, 6.25, 0.05), (0, 6.25, 0.1), 0.78, 0.78, 20), e=1)
    for i in range(8):
        g = i * math.pi / 8
        m.add('pietra', '#a89d86', *tubo((math.cos(g) * -0.8, 6.25 + math.sin(g) * -0.8, 0.1), (math.cos(g) * 0.8, 6.25 + math.sin(g) * 0.8, 0.1), 0.03, 0.03, 4))
    # campanile
    m.add('pietra', '#a89d86', *caja(-6.9, 0, -3.0, -3.9, 4.2, 0.1))
    m.add('intonaco', '#c9bfa6', *caja(-6.8, 4.2, -2.9, -4.0, 12.6, 0.0))
    m.add('pietra', '#a89d86', *caja(-6.9, 4.2, -3.0, -3.9, 4.35, 0.1))
    m.add('pietra', '#a89d86', *caja(-6.9, 12.6, -3.0, -3.9, 12.8, 0.1))
    m.add('intonaco', '#c9bfa6', *caja(-6.8, 12.8, -2.9, -4.0, 15.2, 0.0))
    m.add('pietra', '#a89d86', *caja(-6.95, 15.2, -3.05, -3.85, 15.4, 0.15))
    for (cx, cz) in ((-5.4, 0.0),):
        m.add('liscio', '#1b1512', *caja(cx - 0.5, 13.3, cz - 0.02, cx + 0.5, 14.9, cz + 0.04))
        m.add('liscio', '#1b1512', *tetto(cx - 0.5, cz - 0.02, cx + 0.5, cz + 0.04, 14.9, 15.2, 'z'))
        for k in range(8):
            m.add('legno', '#4a3a2c', *obliquo(cx - 0.48, 13.35 + k * 0.2, cz + 0.0, cx + 0.48, 13.4 + k * 0.2, cz + 0.05, 0, 0.03))
    m.add('liscio', '#1b1512', *caja(-3.98, 13.3, -1.95, -3.94, 14.9, -0.95))
    m.add('liscio', '#1b1512', *caja(-6.86, 13.3, -1.95, -6.82, 14.9, -0.95))
    m.add('liscio', '#f4f1e6', *tubo((-5.4, 10.6, 0.0), (-5.4, 10.6, 0.1), 0.6, 0.6, 24))
    m.add('metallo', '#2a2a2a', *tubo((-5.4, 10.6, 0.1), (-5.4, 10.6, 0.12), 0.64, 0.64, 24))
    m.add('liscio', '#1b1b1b', *caja(-5.42, 10.6, 0.1, -5.38, 11.1, 0.13))
    m.add('liscio', '#1b1b1b', *caja(-5.4, 10.58, 0.1, -5.0, 10.62, 0.13))
    m.add('tegole', '#4a4f58', *prisma(-5.4, -1.45, 15.4, 20.0, 2.2, 0, 4, math.pi / 4))
    m.add('metallo', '#c9a227', *esfera(-5.4, 20.1, -1.45, 0.14, 0.14, 0.14, 6, 4))
    m.add('metallo', '#3a3d40', *caja(-5.45, 20.2, -1.5, -5.35, 21.5, -1.4))
    m.add('metallo', '#3a3d40', *caja(-5.85, 20.8, -1.5, -4.95, 21.0, -1.4))
    return m


def bar():
    m = edificio('bar', 7, 5, 3.8, 'mattone', '#b5684a', '#4a3b34', 0.55, 'x', porta_x=0.0, porta_w=1.0, porta_tinta='#2f4a3a', vetro_porta=True,
                 finestre=[(-2.1, 0.9, 1.9, 1.6, dict(persiane=None, luce='#f0c070', tenda='#8a2a2a')), (2.1, 0.9, 1.9, 1.6, dict(persiane=None, luce='#f0c070', tenda='#8a2a2a'))],
                 laterali=[(1, -2.5, 1.0, 1.0, 1.4, dict(persiane='#2f4a3a')), (-1, -2.5, 1.0, 1.0, 1.4, dict(persiane='#2f4a3a'))],
                 comignoli=[(2.4, -3.8)])
    insegna(m, -1.6, 1.6, 3.0, 3.6, 0.04, '#2a1d14', '#f0c070')
    m.add('liscio', '#ffb347', *caja(-1.6, 3.0, 0.15, 1.6, 3.6, 0.17), e=1)
    tenda(m, -3.3, 3.3, 2.6, 1.3, '#a02a2a', '#e6e0d0', 10)
    for x in (-2.3, 2.3):
        m.add('metallo', '#3a3d40', *prisma(x, 2.2, 0, 0.74, 0.04, 0.04, 6))
        m.add('metallo', '#3a3d40', *prisma(x, 2.2, 0, 0.03, 0.22, 0.22, 10))
        m.add('legno', '#8a6a44', *prisma(x, 2.2, 0.74, 0.78, 0.42, 0.42, 14))
        for dx in (-0.62, 0.62):
            m.add('legno', '#5a4636', *caja(x + dx - 0.2, 0.42, 2.0 - 0.2, x + dx + 0.2, 0.46, 2.0 + 0.2))
            m.add('legno', '#5a4636', *caja(x + dx - 0.2, 0.46, 2.2 + (0.18 if dx < 0 else -0.2) - 0.0, x + dx + 0.2, 0.9, 2.2 + (0.2 if dx < 0 else -0.18)))
            for ex, ez in ((-0.18, -0.18), (0.18, -0.18), (-0.18, 0.18), (0.18, 0.18)):
                m.add('metallo', '#3a3d40', *caja(x + dx + ex - 0.015, 0, 2.2 + ez - 0.015 - 0.2, x + dx + ex + 0.015, 0.42, 2.2 + ez + 0.015 - 0.2))
    return m


def macelleria():
    m = edificio('macelleria', 5, 5, 3.6, 'piastrelle_muro', '#d9d2c0', '#5b4038', 0.5, 'x', porta_x=1.6, porta_w=0.95, porta_tinta='#8a2a2a',
                 finestre=[(-0.9, 0.85, 2.6, 1.7, dict(persiane=None, luce='#f6ecd2', telaio='#8a2a2a'))],
                 laterali=[(1, -2.5, 1.0, 0.9, 1.2, dict(persiane='#8a2a2a'))], comignoli=[(-1.5, -3.8)])
    insegna(m, -1.7, 1.3, 3.0, 3.5, 0.04, '#5a1a1a', '#e6d8c4')
    tenda(m, -2.4, 2.4, 2.65, 1.1, '#a02a2a', '#f0ece0', 8)
    for i in range(3):
        m.add('metallo', '#9aa1a6', *tubo((-1.7 + i * 0.5, 2.3, 0.15), (-1.7 + i * 0.5, 1.75, 0.15), 0.008, 0.008, 4))
        m.add('liscio', '#b24a4a', *esfera(-1.7 + i * 0.5, 1.6, 0.15, 0.1, 0.16, 0.08, 7, 5))
    return m


def negozio_esterno():
    m = edificio('negozio_esterno', 6, 8, 3.8, 'intonaco', '#9eb59a', '#5a5048', 1.2, 'x', porta_x=1.9, porta_w=1.0, porta_tinta='#4b6a50', vetro_porta=True,
                 finestre=[(-0.9, 0.8, 2.9, 1.7, dict(persiane=None, luce='#f1ecd0', telaio='#4b6a50'))],
                 laterali=[(1, -2.5, 1.2, 1.0, 1.3, dict(persiane='#4b6a50')), (1, -5.5, 1.2, 1.0, 1.3, dict(persiane='#4b6a50')),
                           (-1, -2.5, 1.2, 1.0, 1.3, dict(persiane='#4b6a50'))], comignoli=[(-1.5, -5.5)])
    insegna(m, -2.5, 2.5, 3.0, 3.6, 0.05, '#3a5040', '#e6e0d0')
    tenda(m, -2.9, 2.9, 2.65, 1.1, '#3f6a47', '#e6e0d0', 10)
    from modelli_v2 import cestone
    m.ins(cestone('cestone_fuori_1'), -2.7, 0, 0.5)
    m.ins(cestone('cestone_fuori_2', ('verdura', 'mela', 'verdura')), -1.7, 0, 0.55, rot=-5)
    return m


def osteria():
    m = edificio('osteria', 7, 6, 4.0, 'intonaco', '#c4a881', '#6a3f32', 1.9, 'x', porta_x=0.0, porta_w=1.1, porta_tinta='#4a3326',
                 finestre=[(-2.3, 1.0, 1.2, 1.3, dict(persiane='#6a3f32', tenda='#c9503a', cassetta=True)), (2.3, 1.0, 1.2, 1.3, dict(persiane='#6a3f32', tenda='#c9503a', cassetta=True)),
                           (-2.3, 2.7, 1.1, 1.0, dict(persiane='#6a3f32')), (2.3, 2.7, 1.1, 1.0, dict(persiane='#6a3f32')), (0, 2.8, 1.0, 1.0, dict(persiane='#6a3f32', luce='#f0c070'))],
                 laterali=[(1, -3, 1.2, 1.1, 1.2, dict(persiane='#6a3f32'))], comignoli=[(1.8, -4.0)])
    for x in (-3.5, -1.2, 1.2, 3.5):
        m.add('legno', '#4a3326', *caja(x - 0.12, 0.76, -0.03, x + 0.12, 4.0, 0.08))
    m.add('legno', '#4a3326', *caja(-3.5, 2.45, -0.03, 3.5, 2.6, 0.08))
    m.add('metallo', '#2a2a2a', *tubo((1.8, 3.0, 0.3), (1.8, 3.4, 0.3), 0.02, 0.02, 5))
    m.add('legno', '#6a3f32', *prisma(2.9, 1.0, 0, 0.9, 0.42, 0.36, 12))
    for y in (0.15, 0.75):
        m.add('metallo', '#3a3d40', *prisma(2.9, 1.0, y, y + 0.05, 0.435, 0.435, 12))
    return m


def casa():
    m = edificio('casa', 6, 7, 5.0, 'intonaco', '#c9b79a', '#5c3d36', 2.2, 'x', porta_x=-1.5, porta_w=1.0, porta_tinta='#5b4030',
                 finestre=[(0.9, 1.2, 1.1, 1.2, dict(persiane='#4b6a50')), (-1.5, 3.2, 1.0, 1.2, dict(persiane='#4b6a50', cassetta=True)),
                           (1.5, 3.2, 1.0, 1.2, dict(persiane='#4b6a50', luce='#f0c070', tenda='#d9c9a0', cassetta=True))],
                 laterali=[(1, -3.5, 1.2, 1.1, 1.2, dict(persiane='#4b6a50')), (-1, -3.5, 3.2, 1.0, 1.2, dict(persiane='#4b6a50'))],
                 comignoli=[(1.6, -5.0)])
    m.add('legno', '#4a3326', *caja(-2.3, 2.45, 0.0, -0.7, 2.5, 0.9))
    for x in (-2.3, -1.5, -0.7):
        m.add('legno', '#4a3326', *caja(x - 0.03, 2.5, 0.85, x + 0.03, 3.1, 0.9))
    return m


def negozio_piante():
    m = Mod('negozio_piante')
    m.add('legno', '#7b8a6a', *caja(-2.5, 0, -5, 2.5, 3.0, 0))
    m.add('pietra', '#8d867a', *caja(-2.55, 0, -5.05, 2.55, 0.5, 0.05))
    m.add('tegole', '#6a4a40', *tetto(-2.8, -5.3, 2.8, 0.5, 3.0, 4.0, 'x'))
    m.add('vetro', '#bfd8c8', *caja(-2.3, 0.6, 0.0, 0.9, 2.6, 0.02), e=1)
    for x in (-2.3, -1.23, -0.17, 0.9):
        m.add('metallo', '#e8e4d4', *caja(x - 0.03, 0.6, 0.0, x + 0.03, 2.6, 0.06))
    m.add('metallo', '#e8e4d4', *caja(-2.33, 2.57, 0.0, 0.93, 2.63, 0.06))
    m.ins(porta('np_porta', 0.95, 2.1, '#3f6a47', True), 1.8, 0, 0)
    m.add('stampa_a', '#e6e0d0', *caja(-1.6, 2.7, 0.05, 1.8, 3.1, 0.1))
    rng = random.Random(5)
    for i in range(11):
        x, z = -2.1 + i * 0.4, 0.5 + (i % 3) * 0.25
        m.add('terra', '#a8613f', *prisma(x, z, 0, 0.3, 0.14, 0.1, 8))
        m.add('erba', '#4f8a3a', *esfera(x, 0.5, z, 0.2, 0.22, 0.2, 7, 5))
        if i % 3 == 0:
            m.add('liscio', ['#d94a5a', '#f2d21b', '#e8e4f0'][i % 3 + (0 if i < 6 else 0)], *esfera(x + 0.05, 0.62, z + 0.12, 0.05, 0.05, 0.05, 5, 3))
    for i in range(3):
        m.add('legno', '#8a6a44', *caja(-2.4 + i * 0.9, 0, -0.5, -1.8 + i * 0.9, 0.6, -0.0))
    return m


# ---------------------------------------------------------------- paesaggio
def abete():
    m = Mod('abete')
    m.add('corteccia', '#6a4a35', *prisma(0, 0, 0, 1.4, 0.2, 0.13, 7))
    for i, (y0, y1, r, t) in enumerate([(0.9, 2.7, 1.7, '#2f5b3a'), (1.9, 3.8, 1.45, '#2a5236'), (2.9, 4.8, 1.2, '#2f5b3a'), (3.9, 5.7, 0.95, '#244a30'), (4.8, 6.6, 0.7, '#2a5236')]):
        m.add('aghi', t, *prisma(0, 0, y0, y1, r, 0, 9, i * 0.4))
    return m


def sasso():
    m = Mod('sasso')
    m.add('roccia', '#8d8d86', *esfera(0, 0.25, 0, 0.5, 0.3, 0.4, 7, 4))
    m.add('roccia', '#7a7a74', *esfera(0.4, 0.15, 0.2, 0.3, 0.2, 0.28, 6, 4))
    return m


def staccionata():
    m = Mod('staccionata')
    for i in range(4):
        m.add('legno', '#7a5a38', *caja(i * 1.0 - 0.05, 0, -0.05, i * 1.0 + 0.05, 1.05, 0.05))
        m.add('legno', '#8a6a44', *prisma(i * 1.0, 0, 1.05, 1.1, 0.07, 0.04, 4, 0.78))
    for y in (0.4, 0.8):
        m.add('legno', '#8a6a44', *caja(0, y, -0.03, 3.0, y + 0.1, 0.03))
    return m


def panchina():
    m = Mod('panchina')
    for z in (0.0, 0.08, 0.16, 0.24, 0.32):
        m.add('legno', '#7a5a38', *caja(-0.8, 0.45, z, 0.8, 0.49, z + 0.07))
    for y in (0.6, 0.75):
        m.add('legno', '#7a5a38', *caja(-0.8, y, 0.33, 0.8, y + 0.1, 0.37))
    for x in (-0.7, 0.7):
        m.add('metallo', '#2e3236', *caja(x - 0.03, 0, 0.0, x + 0.03, 0.46, 0.38))
        m.add('metallo', '#2e3236', *caja(x - 0.03, 0.46, 0.33, x + 0.03, 0.9, 0.37))
    return m


def barile():
    m = Mod('barile')
    m.add('legno', '#7a5a38', *prisma(0, 0, 0, 0.9, 0.35, 0.35, 12))
    m.add('legno', '#7a5a38', *esfera(0, 0.45, 0, 0.42, 0.45, 0.42, 12, 6))
    for y in (0.12, 0.45, 0.78):
        m.add('metallo', '#3a3d40', *prisma(0, 0, y - 0.025, y + 0.025, 0.43 if y == 0.45 else 0.37, 0.43 if y == 0.45 else 0.37, 12))
    return m


def legna():
    m = Mod('legna')
    for r in range(4):
        for c in range(5 - r % 2):
            m.add('corteccia', '#8a6a44', *tubo((c * 0.24 + (r % 2) * 0.12, 0.1 + r * 0.2, 0), (c * 0.24 + (r % 2) * 0.12, 0.1 + r * 0.2, 0.9), 0.1, 0.1, 8))
    return m


def lampione():
    m = Mod('lampione')
    m.add('metallo', '#2e3236', *prisma(0, 0, 0, 0.25, 0.16, 0.1, 8))
    m.add('metallo', '#2e3236', *prisma(0, 0, 0.25, 4.0, 0.06, 0.04, 8))
    m.add('metallo', '#2e3236', *prisma(0, 0, 3.5, 3.55, 0.09, 0.09, 8))
    m.add('metallo', '#2e3236', *caja(-0.24, 4.0, -0.24, 0.24, 4.06, 0.24))
    m.add('liscio', '#fff0c8', *caja(-0.18, 4.06, -0.18, 0.18, 4.4, 0.18), e=2)
    for sx, sz in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        m.add('metallo', '#2e3236', *caja(sx * 0.18 - 0.015, 4.06, sz * 0.18 - 0.015, sx * 0.18 + 0.015, 4.4, sz * 0.18 + 0.015))
    m.add('metallo', '#2e3236', *tetto(-0.28, -0.28, 0.28, 0.28, 4.4, 4.62, 'x'))
    return m


def fontana():
    m = Mod('fontana')
    m.add('pietra', '#8d8d87', *prisma(0, 0, 0, 0.55, 2.1, 2.1, 16))
    m.add('pietra', '#a3a39c', *prisma(0, 0, 0.55, 0.62, 2.15, 2.15, 16))
    m.add('liscio', '#4f7f8f', *prisma(0, 0, 0.5, 0.56, 1.85, 1.85, 16), e=0)
    m.add('pietra', '#8d8d87', *prisma(0, 0, 0.5, 1.9, 0.28, 0.2, 8))
    m.add('pietra', '#a3a39c', *prisma(0, 0, 1.8, 2.15, 0.85, 0.5, 12))
    m.add('liscio', '#4f7f8f', *prisma(0, 0, 2.1, 2.16, 0.7, 0.7, 12))
    m.add('pietra', '#8d8d87', *prisma(0, 0, 2.1, 2.7, 0.12, 0.08, 8))
    m.add('pietra', '#a3a39c', *esfera(0, 2.8, 0, 0.18, 0.2, 0.18, 8, 5))
    return m


def cabina_telefonica():
    m = Mod('cabina_telefonica')
    r = '#b52a2a'
    for (x, z) in ((-0.45, -0.45), (0.45, -0.45), (-0.45, 0.45), (0.45, 0.45)):
        m.add('metallo', r, *caja(x - 0.03, 0, z - 0.03, x + 0.03, 2.3, z + 0.03))
    m.add('metallo', r, *caja(-0.5, 2.3, -0.5, 0.5, 2.45, 0.5))
    m.add('metallo', r, *tetto(-0.5, -0.5, 0.5, 0.5, 2.45, 2.6, 'z'))
    m.add('metallo', r, *caja(-0.45, 0, -0.45, 0.45, 0.18, 0.45))
    for lato in ('f', 'b', 'l', 'r'):
        for k in range(3):
            for j in range(2):
                a0, a1 = -0.4 + j * 0.42 + 0.02, -0.4 + j * 0.42 + 0.4
                y0, y1 = 0.25 + k * 0.7, 0.25 + k * 0.7 + 0.62
                geo = {'f': caja(a0, y0, 0.44, a1, y1, 0.46), 'b': caja(a0, y0, -0.46, a1, y1, -0.44),
                       'l': caja(-0.46, y0, a0, -0.44, y1, a1), 'r': caja(0.44, y0, a0, 0.46, y1, a1)}[lato]
                m.add('vetro', '#a9d4e0', *geo, e=1)
    m.add('liscio', '#1b1b1b', *caja(-0.1, 1.0, -0.4, 0.1, 1.4, -0.3))
    m.add('stampa_a', '#f4f1e6', *caja(-0.4, 2.0, 0.44, 0.4, 2.28, 0.47))
    return m


def montagna():
    m = Mod('montagna')
    m.add('roccia', '#66717c', *prisma(0, 0, 0, 55, 55, 0, 6, 0.3))
    m.add('roccia', '#5a656f', *prisma(-30, 8, 0, 34, 32, 0, 6, 0.9))
    m.add('roccia', '#707b86', *prisma(32, -6, 0, 38, 30, 0, 6, 0.1))
    m.add('roccia', '#eef1f4', *prisma(0, 0, 40, 55, 15.5, 0, 6, 0.3))
    m.add('roccia', '#eef1f4', *prisma(32, -6, 29, 38, 8.5, 0, 6, 0.1))
    return m


def terreno_paese():
    m = Mod('terreno_paese')
    up = (0, 5, 0)
    for i in range(20):  # tessere da 22 m: i triangoli enormi vengono tagliati male da alcuni rasterizzatori
        for j in range(20):
            m.add('erba', '#4a7a2c', *lastra(-220 + i * 22, 0, -220 + j * 22, -198 + i * 22, 0, -198 + j * 22, up))
    m.add('selciato', '#b8b4a8', *lastra(-10, 0.03, -10, 10, 0.03, 10, up))
    m.add('terra', '#9a8668', *lastra(-3.5, 0.025, 10, 3.5, 0.025, 14, up))
    m.add('terra', '#9a8668', *lastra(-3.5, 0.025, -16, 3.5, 0.025, -10, up))
    for (x0, z0, x1, z1) in ((-10.3, -10.3, 10.3, -10.0), (-10.3, 10.0, 10.3, 10.3), (-10.3, -10.3, -10.0, 10.3), (10.0, -10.3, 10.3, 10.3)):
        m.add('pietra', '#a8a496', *caja(x0, 0, z0, x1, 0.12, z1))
    return m


def terreno_negozio():
    m = Mod('terreno_negozio')
    for i in range(8):
        for j in range(8):
            m.add('cemento', '#6d7073', *lastra(-40 + i * 10, -0.03, -40 + j * 10, -30 + i * 10, -0.03, -30 + j * 10, (0, 5, 0)))
    return m


def podio():
    m = Mod('podio')
    m.add('cemento', '#3a3d40', *prisma(0, 0, -0.06, 0, 4.2, 4.2, 28))
    return m


def tavolo_prodotti():
    from modelli_v2 import INDUSTRIALI, NATURALI
    m = Mod('tavolo_prodotti')
    m.add('legno', '#6b4a2f', *caja(-1.4, 0, -0.9, 1.4, 0.4, 0.9))
    m.add('legno', '#8a6a44', *caja(-1.45, 0.4, -0.95, 1.45, 0.45, 0.95))
    for i, t in enumerate(NATURALI):
        m.ins(prodotto(t), -1.2 + i * 0.34, 0.45, -0.4)
    for i, t in enumerate(INDUSTRIALI):
        m.ins(prodotto(t), -1.2 + i * 0.34, 0.45, 0.4)
    return m


def tutto():
    chiesa(); bar(); macelleria(); negozio_esterno(); osteria(); casa(); negozio_piante()
    for f in (abete, sasso, staccionata, panchina, barile, legna, lampione, fontana, cabina_telefonica, montagna, terreno_paese, terreno_negozio, podio, tavolo_prodotti):
        f()
