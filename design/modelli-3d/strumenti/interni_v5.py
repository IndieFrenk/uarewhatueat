#!/usr/bin/env python3
"""v5: gli interni di casa (x=100) e bar (x=200) in stile v4, un .obj per stanza, pronti per Unity.

Uso: python3 interni_v5.py  ->  design/modelli-3d/obj_v5/interni/{casa_interno,bar_interno}.{obj,mtl} + texture/
                                e copia in Assets/Resources/Models/v5/interni/ (senza .meta).

Convenzioni (importatore OBJ di Unity: x_unity = -x_obj, e rovescia il verso dei triangoli):
  il modello e' scritto in coordinate STANZA (lx, y, lz) = (x_mondo - x0, y, z_mondo - z0), cioe' gia' nel sistema di Unity;
  per compensare l'importatore, scrivi() mette x_obj = -lx e inverte i triangoli. Quindi in Unity basta posare il modello a (x0, 0, z0)
  senza rotazione ne' specchio (diverso dai v4 del negozio, che sono scritti in coordinate Python e stanno a ShopW - x).
  I pezzi (sedie, letto...) hanno il fronte verso +z e il piede a y=0; Mod.ins(rot) ruota come Quaternion.Euler(0, rot, 0) di Unity.
Misure e posizioni: quelle di WorldBuilder.BuildHome / BuildBar (collider e WorldAnchors restano validi).
"""
import math
import random
import shutil
from pathlib import Path

import modelli_v4 as mo
from geo import MATERIALI, REG, Mod, caja, cross, esfera, lastra, obliquo, piano, prisma, sub, tronco, tubo, tutte_le_parti

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
USCITA = RADICE / 'obj_v5' / 'interni'
UNITY = RADICE.parent.parent / 'Assets' / 'Resources' / 'Models' / 'v5' / 'interni'
SU = (0, 1, 0)


# ----------------------------------------------------------------- scorciatoie
def B(m, mat, t, x0, y0, z0, x1, y1, z1, e=0):
    m.add(mat, t, *caja(min(x0, x1), min(y0, y1), min(z0, z1), max(x0, x1), max(y0, y1), max(z0, z1)), e=e)


def Cil(m, mat, t, cx, cz, y0, y1, r, n=12, r1=None, e=0):
    m.add(mat, t, *prisma(cx, cz, y0, y1, r, r if r1 is None else r1, n), e=e)


def Tu(m, mat, t, p0, p1, r, n=6, r1=None, e=0):
    m.add(mat, t, *tubo(p0, p1, r, r if r1 is None else r1, n), e=e)


def Sf(m, mat, t, cx, cy, cz, rx, ry, rz, seg=8, an=5, e=0):
    m.add(mat, t, *esfera(cx, cy, cz, rx, ry, rz, seg, an), e=e)


def Qd(m, mat, t, p0, p1, p2, p3, verso, e=0):
    m.add(mat, t, *piano(p0, p1, p2, p3, verso), e=e)


# ----------------------------------------------------------------- pareti con vani
def P(asse, c, a, y, n):
    """Punto stanza dalle coordinate di parete: a lungo la parete, y, n = distanza dal piano della parete."""
    return (a, y, c + n) if asse == 'z' else (c + n, y, a)


def bx(m, mat, t, asse, c, a0, y0, n0, a1, y1, n1, e=0):
    p, q = P(asse, c, a0, y0, n0), P(asse, c, a1, y1, n1)
    B(m, mat, t, p[0], p[1], p[2], q[0], q[1], q[2], e)


def parete(m, mat, t, asse, c, a0, a1, y0, y1, buchi, verso, e=0):
    """Parete piana (una faccia verso 'verso') ritagliata dai buchi (a0, a1, y0, y1)."""
    bp = sorted({a0, a1} | {min(max(v, a0), a1) for h in buchi for v in h[:2]})
    for u0, u1 in zip(bp, bp[1:]):
        if u1 - u0 < 1e-6:
            continue
        mid = (u0 + u1) / 2
        cop = sorted((max(h[2], y0), min(h[3], y1)) for h in buchi if h[0] < mid < h[1] and h[3] > y0 and h[2] < y1)
        y = y0
        for c0, c1 in cop + [(y1, y1)]:
            if c0 > y + 1e-6:
                q = lastra(u0, y, c, u1, c0, c, verso) if asse == 'z' else lastra(c, y, u0, c, c0, u1, verso)
                m.add(mat, t, *q, e=e)
            y = max(y, c1)


def vano(m, asse, c, s, a0, a1, y0, y1, t, prof=0.2):
    """Spallette del vano: davanzale, architrave e due spalle, spessore 'prof' verso l'esterno (s = +1/-1)."""
    n = s * prof
    vs = P(asse, c, (a0 + a1) / 2, (y0 + y1) / 2, n / 2)
    for y in (y0, y1):
        q = lastra(a0, y, c, a1, y, c + n, vs) if asse == 'z' else lastra(c, y, a0, c + n, y, a1, vs)
        m.add('intonaco', t, *q)
    for a in (a0, a1):
        q = lastra(a, y0, c, a, y1, c + n, vs) if asse == 'z' else lastra(c, y0, a, c + n, y1, a, vs)
        m.add('intonaco', t, *q)


def finestra_int(m, asse, c, s, a0, a1, y0, y1, telaio, tenda, cielo='#a9c4d6', tenda_h=None):
    """Finestra a due ante con vetro opaco color cielo, cornice interna, davanzale e tende con bastone (verso l'interno = -s)."""
    vano(m, asse, c, s, a0, a1, y0, y1, '#d9c9a3')
    am, w, h = (a0 + a1) / 2, a1 - a0, y1 - y0
    bx(m, 'vetro', cielo, asse, c, a0, y0, s * .10, a1, y1, s * .105)
    for (b0, b1, k0, k1) in ((a0, a0 + .05, y0, y1), (a1 - .05, a1, y0, y1), (a0, a1, y0, y0 + .05), (a0, a1, y1 - .05, y1), (am - .02, am + .02, y0, y1),
                             (a0, a1, y0 + h * .62, y0 + h * .62 + .035)):
        bx(m, 'legno', telaio, asse, c, b0, k0, s * .07, b1, k1, s * .13)
    # cornice interna e davanzale
    for (b0, b1, k0, k1) in ((a0 - .07, a0, y0 - .03, y1 + .07), (a1, a1 + .07, y0 - .03, y1 + .07), (a0 - .07, a1 + .07, y1, y1 + .07)):
        bx(m, 'legno', telaio, asse, c, b0, k0, -s * .018, b1, k1, 0)
    bx(m, 'legno', telaio, asse, c, a0 - .1, y0 - .045, -s * .09, a1 + .1, y0, s * .02)
    if tenda:
        th = tenda_h or h + .25
        top = y1 + .14
        Tu(m, 'legno', '#3a2a1c', P(asse, c, a0 - .2, top, -s * .12), P(asse, c, a1 + .2, top, -s * .12), .012, 6)
        for sx, ang in ((a0 - .2, 1), (a1 + .2, 1)):
            Sf(m, 'legno', '#3a2a1c', *P(asse, c, sx, top, -s * .12), .025, .025, .025, 6, 4)
        for k, (b0, b1) in enumerate(((a0 - .17, a0 + .17), (a1 - .17, a1 + .17))):
            for i in range(4):
                u0, u1 = b0 + i * (b1 - b0) / 4, b0 + (i + 1) * (b1 - b0) / 4
                d = -s * (.115 if i % 2 else .145)
                bx(m, 'tessuto', tenda, asse, c, u0, top - th, d, u1, top - .02, d - s * .03)


def porta_int(m, asse, c, s, a0, a1, h, t_anta, t_cornice, vetro=False, maniglia='#b08d3c'):
    """Porta chiusa nel varco (anta rientrata di 6 cm, riquadri sul lato interno, maniglia) con architrave in legno."""
    vano(m, asse, c, s, a0, a1, 0, h, '#d9c9a3', .1)
    am = (a0 + a1) / 2
    if vetro:
        for (b0, b1, k0, k1) in ((a0, a1, 0, .2), (a0, a0 + .1, 0, h), (a1 - .1, a1, 0, h), (a0, a1, h - .12, h), (a0, a1, .9, .96)):
            bx(m, 'legno', t_anta, asse, c, b0, k0, -s * .015, b1, k1, s * .08)
        bx(m, 'vetro', '#bcd6d8', asse, c, a0 + .1, .2, s * .03, a1 - .1, h - .12, s * .05)
    else:
        bx(m, 'legno', t_anta, asse, c, a0, 0, s * .06, a1, h, s * .1)
        for (p0, p1, k0, k1) in ((a0 + .08, am - .03, .12, 1.0), (am + .03, a1 - .08, .12, 1.0), (a0 + .08, am - .03, 1.1, h - .12), (am + .03, a1 - .08, 1.1, h - .12)):
            bx(m, 'legno', t_cornice, asse, c, p0, k0, -s * .02, p1, k1, s * .06)
    for (b0, b1, k0, k1) in ((a0 - .09, a0, 0, h + .09), (a1, a1 + .09, 0, h + .09), (a0 - .09, a1 + .09, h, h + .09)):
        bx(m, 'legno', t_cornice, asse, c, b0, k0, -s * .03, b1, k1, s * .02)
    hx = a1 - .1
    bx(m, 'metallo', maniglia, asse, c, hx - .02, 1.0, -s * .02, hx + .02, 1.1, -s * .08)
    Tu(m, 'metallo', maniglia, P(asse, c, hx, 1.06, -s * .08), P(asse, c, hx - .13, 1.06, -s * .08), .012, 6)


def tagli(a0, a1, vuoti, marg=0.0):
    """Intervalli di [a0, a1] senza i 'vuoti' (a, b), allargati di 'marg'."""
    pezzi = [(a0, a1)]
    for v0, v1 in vuoti:
        pezzi = [q for p in pezzi for q in ((p[0], min(p[1], v0 - marg)), (max(p[0], v1 + marg), p[1])) if q[1] - q[0] > 1e-6]
    return pezzi


def zoccolo(m, t, W, D, vani_s=()):
    """Battiscopa lungo il perimetro (salta i vani dati lungo il muro sud come (a0, a1))."""
    for a0, a1 in tagli(0, W, vani_s, .1):
        B(m, 'legno', t, a0, 0, 0, a1, .1, .02)
    B(m, 'legno', t, 0, 0, D - .02, W, .1, D)
    B(m, 'legno', t, 0, 0, 0, .02, .1, D)
    B(m, 'legno', t, W - .02, 0, 0, W, .1, D)


def lettera(m, ch, x, y, h, w, t, zc, tint, e=3):
    """Lettera a blocchi (B, A, R) su un piano a z=zc, letta da chi guarda verso +z."""
    def b(x0, y0, x1, y1):
        B(m, 'liscio', tint, x0, y0, zc, x1, y1, zc + .04, e)
    m_ = y + h / 2
    if ch in 'BAR':
        b(x, y, x + t, y + h)                         # asta sinistra
        b(x, y + h - t, x + w, y + h)                  # barra alta
        b(x, m_ - t / 2, x + w, m_ + t / 2)            # barra media
    if ch == 'B':
        b(x, y, x + w, y + t)
        b(x + w - t, m_, x + w, y + h)
        b(x + w - t, y, x + w, m_)
    if ch == 'A':
        b(x + w - t, y, x + w, y + h)
    if ch == 'R':
        b(x + w - t, m_, x + w, y + h)
        m.add('liscio', tint, *obliquo(x + w - t, y, zc, x + w, m_, zc + .04, -(w * .45), 0), e=e)


# ----------------------------------------------------------------- pezzi comuni
def sedia(nome, legno, seduta, h_sch=.92, lato=.42):
    """Sedia impagliata, fronte +z, centro del sedile a x=z=0."""
    m = Mod(nome)
    a = lato / 2
    for sx in (-1, 1):
        for sz in (-1, 1):
            B(m, 'legno', legno, sx * (a - .02) - .02, 0, sz * (a - .02) - .02, sx * (a - .02) + .02, .45, sz * (a - .02) + .02)
    B(m, 'legno', legno, -a, .40, -a, a, .45, a)
    B(m, 'tessuto', seduta, -a + .02, .45, -a + .02, a - .02, .49, a - .02)
    for sx in (-1, 1):
        B(m, 'legno', legno, sx * (a - .02) - .02, .45, -a, sx * (a - .02) + .02, h_sch, -a + .04)
    for (y0, y1) in ((.58, .65), (.74, .81), (h_sch - .07, h_sch)):
        B(m, 'legno', legno, -a + .02, y0, -a, a - .02, y1, -a + .035)
    return m


def tv_crt(nome='tv_crt'):
    """Televisore a tubo catodico 28": mobile in legno finto, schermo bombato, retro a tronco, antenna a V. Base a y=0, fronte +z."""
    m = Mod(nome)
    ve = '#8a6a45'
    B(m, 'liscio', '#26262a', -.38, 0, .1, .38, .56, .2)                       # cornice frontale
    B(m, 'legno', ve, -.41, 0, -.03, -.38, .56, .2)
    B(m, 'legno', ve, .38, 0, -.03, .41, .56, .2)
    B(m, 'vetro', '#202a30', -.31, .12, .2, .31, .52, .205)                    # vetro
    B(m, 'vetro', '#2c3a42', -.26, .15, .205, .26, .49, .21)                   # bombatura
    B(m, 'liscio', '#26262a', -.38, 0, .1, .38, .1, .21)                       # fascia comandi
    for i in range(7):
        B(m, 'metallo', '#6a6e72', -.34 + i * .03, .02, .21, -.325 + i * .03, .08, .215)
    Sf(m, 'metallo', '#c9c9c4', .24, .05, .22, .02, .02, .012, 8, 4)
    Sf(m, 'metallo', '#c9c9c4', .3, .05, .22, .02, .02, .012, 8, 4)
    B(m, 'liscio', '#d12e2e', .335, .04, .21, .35, .055, .216, e=2)
    B(m, 'liscio', '#2b2b30', -.34, .12, -.03, .34, .54, .1)                   # corpo
    B(m, 'liscio', '#2b2b30', -.26, .17, -.2, .26, .49, -.03)                  # retro
    for i in range(6):
        B(m, 'metallo', '#1b1b1e', -.2 + i * .07, .53, -.1, -.17 + i * .07, .545, .08)
    Cil(m, 'liscio', '#26262a', 0, -.03, .54, .58, .06, 10)
    Tu(m, 'metallo', '#b9bcc0', (-.01, .58, -.03), (-.3, 1.0, -.08), .006, 5)
    Tu(m, 'metallo', '#b9bcc0', (.01, .58, -.03), (.32, .96, -.08), .006, 5)
    Sf(m, 'metallo', '#b9bcc0', -.3, 1.0, -.08, .014, .014, .014, 5, 3)
    Sf(m, 'metallo', '#b9bcc0', .32, .96, -.08, .014, .014, .014, 5, 3)
    return m


def bottiglia(m, x, y, z, k):
    """Bottiglia sullo scaffale: k = 'vino' / 'amaro' / 'liquore' / 'vermut' / 'grappa'. Alta circa 0,3 m."""
    vetro = {'vino': '#2f5a34', 'amaro': '#4a2a14', 'liquore': '#2f6fcf', 'vermut': '#8a1f1f', 'grappa': '#cfe0d8'}[k]
    et = {'vino': '#e8dcb8', 'amaro': '#c9a227', 'liquore': '#efe6c8', 'vermut': '#e8dcb8', 'grappa': '#d8d2c0'}[k]
    h = {'vino': .29, 'amaro': .26, 'liquore': .28, 'vermut': .27, 'grappa': .25}[k]
    r = .035 if k != 'amaro' else .04
    Cil(m, 'vetro', vetro, x, z, y, y + h * .6, r, 10)
    Cil(m, 'vetro', vetro, x, z, y + h * .6, y + h * .85, r, 10, r1=.014)
    Cil(m, 'vetro', vetro, x, z, y + h * .85, y + h, .014, 8)
    Cil(m, 'stampa_a', et, x, z, y + h * .18, y + h * .46, r + .002, 10)
    Cil(m, 'metallo', '#c9a227', x, z, y + h - .012, y + h + .008, .017, 8)


def sgabello_bar(nome='bar_sgabello'):
    m = Mod(nome)
    Cil(m, 'metallo', '#b9bcc0', 0, 0, 0, .04, .2, 14)
    Cil(m, 'metallo', '#b9bcc0', 0, 0, .04, .54, .02, 8)
    for i in range(3):
        a = i * 2 * math.pi / 3 + .5
        Tu(m, 'metallo', '#b9bcc0', (0, .24, 0), (.17 * math.cos(a), .24, .17 * math.sin(a)), .008, 5)
    for i in range(12):
        a0, a1 = i * math.pi / 6, (i + 1) * math.pi / 6
        Tu(m, 'metallo', '#b9bcc0', (.17 * math.cos(a0), .24, .17 * math.sin(a0)), (.17 * math.cos(a1), .24, .17 * math.sin(a1)), .009, 5)
    Cil(m, 'metallo', '#6a6e72', 0, 0, .52, .55, .185, 14)
    Cil(m, 'pelle', '#7a2323', 0, 0, .55, .62, .18, 14)
    Cil(m, 'pelle', '#8a2a2a', 0, 0, .62, .635, .15, 14)
    return m


def tavolino_bar(nome='bar_tavolino'):
    m = Mod(nome)
    Cil(m, 'metallo', '#2a2d30', 0, 0, 0, .05, .24, 14)
    Cil(m, 'metallo', '#2a2d30', 0, 0, .05, .74, .035, 8)
    Cil(m, 'metallo', '#2a2d30', 0, 0, .72, .76, .12, 12)
    Cil(m, 'pietra', '#e4ded0', 0, 0, .76, .8, .45, 20)
    Cil(m, 'metallo', '#b9bcc0', 0, 0, .755, .765, .455, 20)
    Cil(m, 'liscio', '#d8c14a', .12, .1, .8, .815, .055, 10)            # posacenere
    Cil(m, 'liscio', '#f1ecde', -.15, -.05, .8, .85, .035, 10)          # zuccheriera
    B(m, 'stampa_a', '#c43b2d', -.03, .8, .2, .05, .86, .215)            # segnatavolo
    return m


# ================================================================= CASA
CW, CD, CH = 7.0, 5.0, 2.6


def cucina():
    """Cucina a muro lunga 2,5 m e profonda 0,6: lavello, fornelli a gas con forno, pensili, cappa. Fronte +z, retro a z=-0.3."""
    m = Mod('casa_cucina')
    leg, legc, ton = '#a8743f', '#b98349', '#8a5a32'
    B(m, 'legno', '#3a2a1c', -1.25, 0, -.28, 1.25, .1, .26)
    B(m, 'legno', leg, -1.25, .1, -.3, 1.25, .86, .3)
    for i, (x0, x1) in enumerate(((-1.25, -.65), (-.65, -.25), (.45, .85), (.85, 1.25))):        # sportelli
        B(m, 'legno', legc, x0 + .01, .12, .3, x1 - .01, .84, .315)
        B(m, 'legno', ton, x0 + .05, .17, .315, x1 - .05, .79, .322)
        kx = x1 - .07 if i % 2 == 0 else x0 + .07
        B(m, 'metallo', '#b9bcc0', kx - .01, .45, .322, kx + .01, .7, .35)
    B(m, 'metallo', '#2f3236', -.2, .12, .3, .4, .84, .32)                            # forno e pannello comandi
    B(m, 'vetro', '#15171a', -.14, .2, .32, .34, .6, .325)
    Tu(m, 'metallo', '#c8cdd0', (-.16, .66, .37), (.36, .66, .37), .012, 6)
    Tu(m, 'metallo', '#c8cdd0', (-.16, .66, .37), (-.16, .66, .32), .008, 5)
    Tu(m, 'metallo', '#c8cdd0', (.36, .66, .37), (.36, .66, .32), .008, 5)
    for i in range(4):
        Tu(m, 'gomma', '#1b1b1d', (-.1 + i * .16, .77, .32), (-.1 + i * .16, .77, .36), .02, 8)     # manopole
    B(m, 'liscio', '#c9bfa3', -1.27, .86, -.3, 1.27, .9, .34)                          # piano
    B(m, 'metallo', '#c8cdd0', -1.1, .9, -.2, -.4, .91, .22)                           # lavello
    B(m, 'metallo', '#5a5f64', -1.0, .91, -.12, -.5, .915, .16)
    Tu(m, 'metallo', '#c8cdd0', (-.75, .91, -.22), (-.75, 1.12, -.22), .015, 6)
    Tu(m, 'metallo', '#c8cdd0', (-.75, 1.12, -.22), (-.75, 1.12, -.08), .012, 6)
    B(m, 'metallo', '#2a2d30', -.15, .9, -.2, .45, .91, .22)                           # fornelli
    for (cx, cz) in ((0.0, -.08), (.3, -.08), (0.0, .12), (.3, .12)):
        Cil(m, 'metallo', '#111113', cx, cz, .91, .925, .045, 10)
        Cil(m, 'metallo', '#8a8d90', cx, cz, .925, .93, .06, 10)
        B(m, 'metallo', '#1a1a1c', cx - .09, .93, cz - .006, cx + .09, .95, cz + .006)
        B(m, 'metallo', '#1a1a1c', cx - .006, .93, cz - .09, cx + .006, .95, cz + .09)
    Cil(m, 'metallo', '#b9bcc0', 0, -.08, .95, .985, .07, 10, r1=.06)                  # moka
    Cil(m, 'metallo', '#b9bcc0', 0, -.08, .985, 1.02, .05, 10)
    Cil(m, 'metallo', '#b9bcc0', 0, -.08, 1.02, 1.08, .06, 10, r1=.075)
    B(m, 'gomma', '#1b1b1b', .075, 1.02, -.09, .14, 1.07, -.07)
    Sf(m, 'metallo', '#b9bcc0', 0, 1.085, -.08, .025, .015, .025, 6, 3)
    Cil(m, 'metallo', '#6a6e72', .3, .12, .95, 1.07, .09, 12)                           # pentola con coperchio
    Cil(m, 'metallo', '#8a8d90', .3, .12, 1.07, 1.085, .095, 12)
    Sf(m, 'gomma', '#1b1b1b', .3, 1.1, .12, .018, .018, .018, 5, 3)
    B(m, 'legno', '#9a6f3c', .6, .9, -.15, 1.1, .92, .2)                               # tagliere
    for i, c in enumerate(('#a52b24', '#c5a02e', '#5a8a35')):
        Sf(m, 'liscio', c, .75 + i * .13, .96, .02, .045, .045, .045, 6, 4)
    B(m, 'piastrelle_muro', '#e8dcc0', -1.25, .9, -.35, 1.25, 1.5, -.33)               # alzatina
    for (x0, x1) in ((-1.25, -.25), (.5, 1.25)):                                        # pensili
        B(m, 'legno', leg, x0, 1.5, -.35, x1, 2.2, -.03)
        n = 2 if x1 - x0 > .9 else 2
        for i in range(n):
            u0, u1 = x0 + i * (x1 - x0) / n, x0 + (i + 1) * (x1 - x0) / n
            B(m, 'legno', legc, u0 + .01, 1.52, -.03, u1 - .01, 2.18, -.015)
            B(m, 'legno', ton, u0 + .05, 1.58, -.015, u1 - .05, 2.12, -.008)
            Sf(m, 'metallo', '#b9bcc0', u0 + .08 if i == 0 else u1 - .08, 1.65, -.003, .015, .015, .012, 6, 3)
    B(m, 'metallo', '#c8cdd0', -.25, 1.5, -.35, .5, 1.62, -.03)                          # cappa
    B(m, 'metallo', '#b9bcc0', -.05, 1.62, -.35, .3, 2.2, -.2)
    B(m, 'liscio', '#fff0c8', -.2, 1.49, -.3, .45, 1.5, -.08, e=2)
    return m


def frigo():
    """Frigorifero con congelatore sopra, anni 90, color panna. Fronte +z."""
    m = Mod('casa_frigo')
    c = '#e8e2d2'
    B(m, 'liscio', '#3a3a3c', -.38, 0, -.3, .38, .06, .32)
    B(m, 'liscio', c, -.4, .06, -.35, .4, 1.8, .35)
    B(m, 'gomma', '#2b2b2d', -.4, 1.28, .35, .4, 1.31, .358)
    B(m, 'metallo', '#b8bcc0', .3, 1.0, .35, .335, 1.22, .39)
    B(m, 'metallo', '#b8bcc0', .3, 1.34, .35, .335, 1.55, .39)
    B(m, 'metallo', '#b8bcc0', -.2, 1.74, .351, -.06, 1.78, .357)
    for (x0, y0, x1, y1, t) in ((-.28, .95, -.12, 1.12, '#f2c94c'), (-.08, .75, .06, .9, '#d94a5a'), (-.16, 1.4, -.04, 1.52, '#3aa0ff')):
        B(m, 'stampa_b', t, x0, y0, .35, x1, y1, .355)
    B(m, 'liscio', '#b8a98a', -.4, .06, .35, .4, .14, .352)
    return m


def tavolo_casa():
    m = Mod('casa_tavolo')
    B(m, 'legno', '#8a5a32', -.8, .74, -.5, .8, .8, .5)
    B(m, 'legno', '#6b4426', -.74, .66, -.44, .74, .74, .44)
    for sx in (-1, 1):
        for sz in (-1, 1):
            cx, cz = sx * .7, sz * .4
            Cil(m, 'legno', '#6b4426', cx, cz, 0, .66, .035, 8, r1=.028)
            Cil(m, 'legno', '#6b4426', cx, cz, .3, .34, .045, 8)
    B(m, 'tessuto', '#e8e0cc', -.7, .8, -.2, .7, .805, .2)
    B(m, 'tessuto', '#b3322a', -.7, .805, -.2, .7, .808, -.17)
    B(m, 'tessuto', '#b3322a', -.7, .805, .17, .7, .808, .2)
    for (x, z) in ((-.5, 0), (.5, 0), (0, -.3), (0, .3)):
        Cil(m, 'liscio', '#f1ecde', x, z, .8, .812, .11, 12)
        Cil(m, 'liscio', '#d8d2c0', x, z, .812, .816, .07, 12)
    Cil(m, 'liscio', '#6a8f8a', 0, 0, .805, .93, .05, 10, r1=.065)                      # brocca
    for i in range(5):
        a = i * 1.26
        Tu(m, 'erba', '#4f8a3a', (0, .93, 0), (.05 * math.cos(a), 1.08, .05 * math.sin(a)), .005, 4)
        Sf(m, 'liscio', ['#d94a5a', '#f2d21b', '#e8e4f0'][i % 3], .05 * math.cos(a), 1.09, .05 * math.sin(a), .025, .02, .025, 5, 3)
    return m


def credenza():
    """Credenza con vetrina a reticolo e piatti. Fronte +z, retro a z=-0.225."""
    m = Mod('casa_credenza')
    leg, legc, ton = '#7a5230', '#8a6038', '#6b4426'
    B(m, 'legno', ton, -.8, 0, -.2, .8, .08, .2)
    B(m, 'legno', leg, -.8, .08, -.225, .8, .9, .225)
    for (x0, x1) in ((-.8, 0), (0, .8)):
        B(m, 'legno', legc, x0 + .02, .1, .225, x1 - .02, .88, .24)
        B(m, 'legno', ton, x0 + .08, .17, .24, x1 - .08, .81, .247)
        Sf(m, 'metallo', '#b08d3c', (x0 + x1) / 2 + (.08 if x0 < 0 else -.08), .5, .26, .02, .02, .015, 6, 3)
    B(m, 'legno', leg, -.82, .9, -.225, .82, .94, .26)
    B(m, 'legno', leg, -.78, .94, -.225, .78, 1.9, -.19)                                # fondo vetrina
    for sx in (-1, 1):
        B(m, 'legno', leg, sx * .78, .94, -.225, sx * .75, 1.9, .06)
    B(m, 'legno', leg, -.8, 1.9, -.225, .8, 1.95, .08)
    B(m, 'legno', ton, -.84, 1.95, -.225, .84, 2.0, .1)
    for y in (1.25, 1.56):
        B(m, 'legno', legc, -.78, y, -.19, .78, y + .025, .04)
    for (x0, x1) in ((-.78, 0), (0, .78)):                                               # telai dei vetri (senza vetro: si vede dentro)
        for (a, b, c_, d) in ((x0, x0 + .035, .96, 1.88), (x1 - .035, x1, .96, 1.88), (x0, x1, 1.85, 1.88), (x0, x1, .96, .99)):
            B(m, 'legno', legc, a, c_, .055, b, d, .075)
        B(m, 'legno', legc, (x0 + x1) / 2 - .008, .99, .055, (x0 + x1) / 2 + .008, 1.85, .075)
    for i, x in enumerate((-.6, -.3, 0.0, .3, .6)):                                      # piatti in piedi
        c = ['#f1ecde', '#e8d6a0', '#6a8f8a'][i % 3]
        B(m, 'liscio', c, x - .09, 1.58, -.17, x + .09, 1.80, -.15)
        B(m, 'liscio', '#b3322a' if i % 2 else '#3f6fb5', x - .06, 1.63, -.15, x + .06, 1.75, -.145)
    for i, x in enumerate((-.55, -.2, .15, .5)):
        Cil(m, 'liscio', '#f1ecde', x, -.05, 1.275, 1.34, .035, 8)
        Cil(m, 'liscio', '#d94a5a' if i % 2 else '#3f6fb5', x + .17, -.1, 1.275, 1.37, .03, 8)
    Cil(m, 'liscio', '#c9a227', .55, -.1, 1.585, 1.69, .045, 8, r1=.025)                 # barattolo
    Tu(m, 'metallo', '#b08d3c', (-.5, 1.585, -.1), (-.5, 1.71, -.1), .022, 6, r1=.03)
    B(m, 'tessuto', '#e8e0cc', -.6, .94, -.1, .6, .945, .16)                             # centrino
    return m


def mobile_tv():
    m = Mod('casa_tv')
    leg, ton = '#6e4a2c', '#4a3320'
    B(m, 'legno', ton, -.7, 0, -.25, .7, .08, .22)
    B(m, 'legno', leg, -.7, .08, -.25, .7, .5, .25)
    B(m, 'legno', leg, -.72, .5, -.25, .72, .53, .27)
    B(m, 'gomma', '#1b1b1d', -.66, .13, .0, -.06, .45, .24)                              # vano videoregistratore
    B(m, 'metallo', '#2a2d30', -.6, .22, .1, -.12, .3, .25)                              # VHS
    B(m, 'liscio', '#d12e2e', -.2, .25, .25, -.16, .27, .255, e=2)
    B(m, 'vetro', '#1b2a30', -.58, .24, .25, -.28, .28, .255)
    for (x0, x1) in ((0, .66),):
        B(m, 'legno', '#7a5a38', x0 + .02, .12, .25, x1, .48, .262)
        B(m, 'legno', ton, x0 + .07, .17, .262, x1 - .05, .43, .268)
        Sf(m, 'metallo', '#b08d3c', x0 + .12, .3, .275, .018, .018, .012, 6, 3)
    for (x, z) in ((-.64, -.2), (.64, -.2), (-.64, .2), (.64, .2)):
        B(m, 'gomma', '#1b1b1d', x - .03, 0, z - .03, x + .03, .08, z + .03)
    m.ins(tv_crt('tv_crt_casa'), 0, .53, 0, 0)
    return m


def letto():
    """Matrimoniale 1,4 x 2 m, testata a z=-1 (verso il muro sud). Fronte +z."""
    m = Mod('casa_letto')
    leg, ton = '#6b4a2f', '#4a3320'
    B(m, 'legno', leg, -.7, .12, -1.0, .7, .3, 1.0)
    for (x, z) in ((-.66, -.96), (.66, -.96), (-.66, .96), (.66, .96)):
        B(m, 'legno', ton, x - .035, 0, z - .035, x + .035, .12, z + .035)
    B(m, 'tessuto', '#e8e2d0', -.66, .3, -.97, .66, .5, .97)
    B(m, 'tessuto', '#80504c', -.68, .5, -.32, .68, .545, .98)                           # coperta
    for i in range(3):
        for j in range(5):
            if (i + j) % 2 == 0:
                B(m, 'tessuto', '#c9a07a', -.62 + i * .413, .545, -.26 + j * .24, -.62 + i * .413 + .39, .549, -.26 + j * .24 + .22)
    B(m, 'tessuto', '#d8b9a0', -.68, .5, .94, .68, .545, .98)
    for sx in (-1, 1):
        Sf(m, 'tessuto', '#ece7da', sx * .33, .58, -.68, .28, .085, .2, 8, 4)
    B(m, 'legno', leg, -.72, .12, -1.06, .72, 1.1, -.96)                                 # testata
    B(m, 'legno', ton, -.62, .45, -.96, .62, 1.0, -.93)
    B(m, 'legno', leg, -.74, 1.06, -1.07, .74, 1.12, -.95)
    for sx in (-1, 1):
        Sf(m, 'legno', ton, sx * .7, 1.14, -1.0, .045, .045, .045, 6, 4)
    B(m, 'legno', leg, -.72, .12, .96, .72, .6, 1.04)                                    # pediera
    B(m, 'legno', ton, -.62, .22, 1.04, .62, .5, 1.055)
    return m


def comodino():
    m = Mod('casa_comodino')
    B(m, 'legno', '#6b4a2f', -.2, .06, -.2, .2, .55, .2)
    B(m, 'legno', '#7a5a38', -.16, .12, .2, .16, .28, .212)
    B(m, 'legno', '#7a5a38', -.16, .32, .2, .16, .48, .212)
    Sf(m, 'metallo', '#b08d3c', 0, .2, .22, .015, .015, .012, 6, 3)
    Sf(m, 'metallo', '#b08d3c', 0, .4, .22, .015, .015, .012, 6, 3)
    for sx in (-1, 1):
        for sz in (-1, 1):
            B(m, 'legno', '#4a3320', sx * .17 - .02, 0, sz * .17 - .02, sx * .17 + .02, .06, sz * .17 + .02)
    B(m, 'legno', '#6b4a2f', -.22, .55, -.22, .22, .58, .22)
    Cil(m, 'liscio', '#c9a227', 0, 0, .58, .62, .05, 10)                                 # abat-jour
    Cil(m, 'metallo', '#b08d3c', 0, 0, .62, .74, .012, 6)
    Cil(m, 'liscio', '#fff0c8', 0, 0, .72, .87, .1, 10, r1=.06, e=2)
    return m


def lettino():
    """Lettino di Giulia 0,8 x 1,6 con sponde, coperta rosa e orsetto. Testata a z=-0.8. Fronte +z."""
    m = Mod('casa_lettino')
    v, ton = '#d8c9a8', '#a8946a'
    B(m, 'legno', v, -.4, .1, -.8, .4, .24, .8)
    for (x, z) in ((-.38, -.78), (.38, -.78), (-.38, .78), (.38, .78)):
        B(m, 'legno', ton, x - .03, 0, z - .03, x + .03, .1, z + .03)
    B(m, 'tessuto', '#efe9d8', -.37, .24, -.77, .37, .36, .77)
    B(m, 'tessuto', '#d98ca8', -.38, .36, -.25, .38, .395, .78)
    for i in range(4):
        B(m, 'tessuto', '#f0c9d6', -.34 + i * .18, .395, -.2 + (i % 2) * .3, -.22 + i * .18, .399, -.08 + (i % 2) * .3)
    Sf(m, 'tessuto', '#ece7da', 0, .41, -.58, .2, .06, .14, 8, 4)
    B(m, 'legno', v, -.42, .1, -.86, .42, .78, -.76)                                      # testata con stella
    B(m, 'legno', '#f2d21b', -.06, .5, -.865, .06, .62, -.86)
    B(m, 'legno', '#f2d21b', -.12, .54, -.865, .12, .58, -.86)
    B(m, 'legno', v, -.42, .1, .76, .42, .42, .84)
    for sx in (-1, 1):                                                                    # sponde a sbarre
        B(m, 'legno', v, sx * .4 - .015, .36, -.1, sx * .4 + .015, .56, .1)
        B(m, 'legno', v, sx * .4 - .015, .5, -.1, sx * .4 + .015, .54, .78)
        for k in range(6):
            B(m, 'legno', v, sx * .4 - .01, .36, .0 + k * .13, sx * .4 + .01, .52, .02 + k * .13)
    Sf(m, 'tessuto', '#a0703c', .1, .45, -.2, .07, .08, .06, 8, 5)                        # orsetto
    Sf(m, 'tessuto', '#a0703c', .1, .56, -.2, .055, .05, .05, 8, 5)
    for sx in (-1, 1):
        Sf(m, 'tessuto', '#a0703c', .1 + sx * .04, .6, -.2, .02, .02, .02, 5, 3)
        Sf(m, 'tessuto', '#a0703c', .1 + sx * .075, .46, -.2, .025, .035, .025, 5, 3)
    Sf(m, 'tessuto', '#e8d6b0', .1, .54, -.15, .02, .016, .012, 5, 3)
    return m


def cornice_specchio():
    """Cornice dello specchio, centro (0,0), fronte +z: apertura 0,8 x 1,2. Fondo a z da -0.03 a 0.02; in casa e' ruotata di 180
    e posata con z locale 0 a z=4.97, cosi' il fondo sta contro il muro nord (5.0) e il quad di gioco (4.94) resta davanti al fondo, dentro la cornice."""
    m = Mod('casa_cornice')
    c, oro = '#4a2f1a', '#b08d3c'
    ow, oh, iw, ih = .475, .675, .4, .6
    B(m, 'legno', '#2a1d12', -ow, -oh, -.03, ow, oh, .02)
    for (x0, y0, x1, y1) in ((-ow, -oh, -iw, oh), (iw, -oh, ow, oh), (-iw, ih, iw, oh), (-iw, -oh, iw, -ih)):
        B(m, 'legno', c, x0, y0, -.03, x1, y1, .06)
    for (x0, y0, x1, y1) in ((-iw, -ih, -iw + .02, ih), (iw - .02, -ih, iw, ih), (-iw, ih - .02, iw, ih), (-iw, -ih, iw, -ih + .02)):
        B(m, 'metallo', oro, x0, y0, .05, x1, y1, .065)
    for sx in (-1, 1):
        for sy in (-1, 1):
            Sf(m, 'metallo', oro, sx * (ow - .037), sy * (oh - .037), .06, .022, .022, .012, 6, 4)
    Sf(m, 'metallo', oro, 0, oh + .02, 0, .03, .03, .02, 6, 4)
    return m


def lampadario():
    """Lampadario a sei braccia con paralumi a tulipano (emissivi); origine al soffitto (y=2.6 del locale)."""
    m = Mod('casa_lampadario')
    ot = '#b08d3c'
    Cil(m, 'metallo', ot, 0, 0, 2.55, 2.6, .08, 12)
    Tu(m, 'metallo', ot, (0, 2.2, 0), (0, 2.55, 0), .012, 6)
    Sf(m, 'metallo', ot, 0, 2.17, 0, .07, .08, .07, 8, 5)
    Cil(m, 'metallo', ot, 0, 0, 2.09, 2.13, .03, 8)
    for i in range(6):
        a = i * math.pi / 3
        ca, sa = math.cos(a), math.sin(a)
        Tu(m, 'metallo', ot, (.04 * ca, 2.17, .04 * sa), (.3 * ca, 2.14, .3 * sa), .01, 5)
        Tu(m, 'metallo', ot, (.3 * ca, 2.14, .3 * sa), (.3 * ca, 2.27, .3 * sa), .01, 5)
        Cil(m, 'liscio', '#fff1c8', .3 * ca, .3 * sa, 2.27, 2.4, .055, 8, r1=.035, e=2)
    return m


def casa():
    m = Mod('casa_interno')
    W, D, H = CW, CD, CH
    C = (W / 2, 1.0, D / 2)
    rng = random.Random(7)
    ps = (3.0, 4.0)        # varco della porta (muro sud)
    fin_n, fin_e = (3.9, 4.9), (3.05, 4.15)
    bs = {'S': [(ps[0], ps[1], 0, 2.2)], 'N': [(fin_n[0], fin_n[1], 1.0, 2.0)], 'W': [], 'E': [(fin_e[0], fin_e[1], 1.0, 2.0)]}
    # pavimento: parquet a listelli sfalsati, piastrelle (cotto e panna) nell'angolo cucina
    B(m, 'legno', '#3a2a1c', 0, -.03, 0, W, -.02, D)
    tinte = ['#9a6a3a', '#a8763f', '#8d5f33', '#b0803f', '#966637']
    for j in range(int(round(D / .1))):
        za, zb = j * .1, (j + 1) * .1 - .004
        xa = -(j * .37 % .9)
        while xa < W:
            a, b = max(xa, 0), min(xa + .9 - .004, W)
            if za >= 3.2 - 1e-6:
                a = max(a, 3.6)
            if b > a + 1e-6:
                m.add('legno', rng.choice(tinte), *lastra(a, 0, za, b, 0, zb, ((a + b) / 2, 1, (za + zb) / 2)))
            xa += .9
    for i in range(12):
        for j in range(6):
            m.add('piastrelle', '#d8d2bc' if (i + j) % 2 else '#a9552f', *lastra(i * .3, 0, 3.2 + j * .3, (i + 1) * .3, 0, 3.2 + (j + 1) * .3, (1, 1, 4)))
    B(m, 'pietra', '#a8a290', 3.6, 0, 3.2, 3.64, .006, D)
    B(m, 'tessuto', '#c9a96a', 3.3, .003, 1.9, 5.7, .012, 4.5)                             # tappeto sotto il tavolo
    B(m, 'tessuto', '#7a2f2f', 3.38, .012, 1.98, 5.62, .017, 4.42)
    B(m, 'tessuto', '#c9a96a', 3.6, .017, 2.2, 5.4, .02, 4.2)
    B(m, 'tessuto', '#7a2f2f', 3.66, .02, 2.26, 5.34, .023, 4.14)
    # muri: zoccolo in perlinato fino a 1 m con cimasa, sopra intonaco crema
    for asse, c, a0, a1, k, ctr in (('z', 0, 0, W, 'S', (W / 2, 1, 1)), ('z', D, 0, W, 'N', (W / 2, 1, D - 1)), ('x', 0, 0, D, 'W', (1, 1, D / 2)), ('x', W, 0, D, 'E', (W - 1, 1, D / 2))):
        parete(m, 'legno', '#8b5e34', asse, c, a0, a1, 0, 1.0, bs[k], ctr)
        parete(m, 'intonaco', '#d9c9a3', asse, c, a0, a1, 1.0, H, bs[k], ctr)
    for k in range(1, int(W / .2)):                                                           # listelli del perlinato
        x = k * .2
        if not (ps[0] - .1 < x < ps[1] + .1):
            B(m, 'legno', '#74502c', x - .004, .1, 0, x + .004, 1.0, .012)
        if not (fin_n[0] - .1 < x < fin_n[1] + .1):
            B(m, 'legno', '#74502c', x - .004, .1, D - .012, x + .004, 1.0, D)
    for k in range(1, int(D / .2)):
        z = k * .2
        B(m, 'legno', '#74502c', 0, .1, z - .004, .012, 1.0, z + .004)
        if not (fin_e[0] - .1 < z < fin_e[1] + .1):
            B(m, 'legno', '#74502c', W - .012, .1, z - .004, W, 1.0, z + .004)
    for (x0, x1) in tagli(0, W, [ps], .1):
        B(m, 'legno', '#5b3b22', x0, 1.0, 0, x1, 1.04, .03)
    for (x0, x1) in tagli(0, W, [fin_n], .1):
        B(m, 'legno', '#5b3b22', x0, 1.0, D - .03, x1, 1.04, D)
    B(m, 'legno', '#5b3b22', 0, 1.0, 0, .03, 1.04, D)
    for (z0, z1) in tagli(0, D, [fin_e], .1):
        B(m, 'legno', '#5b3b22', W - .03, 1.0, z0, W, 1.04, z1)
    zoccolo(m, '#3a2a1c', W, D, [ps])
    # soffitto con travi
    m.add('intonaco', '#e6dcc2', *lastra(0, H, 0, W, H, D, (W / 2, 1, D / 2)))
    for z in (.7, 1.9, 3.1, 4.3):
        B(m, 'legno', '#5b3b22', 0, H - .14, z - .07, W, H, z + .07)
    B(m, 'legno', '#5b3b22', 0, H - .06, 0, W, H, .05)
    B(m, 'legno', '#5b3b22', 0, H - .06, D - .05, W, H, D)
    # finestre, porta
    finestra_int(m, 'z', D, 1, fin_n[0], fin_n[1], 1.0, 2.0, '#efe6d0', '#b3472f')
    finestra_int(m, 'x', W, 1, fin_e[0], fin_e[1], 1.0, 2.0, '#efe6d0', '#b3472f')
    porta_int(m, 'z', 0, -1, ps[0], ps[1], 2.2, '#6b4a2f', '#7a5a3f')
    # decorazioni a parete
    B(m, 'legno', '#4a2f1a', 6.1, 1.45, .0, 6.13, 1.95, .02)                                # crocifisso sopra la testata
    B(m, 'legno', '#4a2f1a', 5.95, 1.78, .0, 6.3, 1.82, .02)
    B(m, 'metallo', '#c9a227', 6.1, 1.72, .02, 6.13, 1.78, .03)
    B(m, 'legno', '#4a2f1a', 4.3, 1.5, .0, 5.1, 2.0, .03)                                   # quadro di montagna
    B(m, 'stampa_b', '#5a8ab0', 4.34, 1.54, .03, 5.06, 1.96, .035)
    for (z, y, w_, h_, t) in ((1.0, 1.55, .3, .22, '#d94a5a'), (1.45, 1.6, .2, .28, '#3aa0ff'), (1.85, 1.5, .26, .2, '#f2c94c'), (2.2, 1.62, .18, .24, '#5a8a35')):   # foto e disegni sopra la TV
        B(m, 'legno', '#4a2f1a', .0, y, z - w_ / 2 - .02, .025, y + h_ + .04, z + w_ / 2 + .02)
        B(m, 'stampa_b', t, .025, y + .02, z - w_ / 2, .03, y + h_ + .02, z + w_ / 2)
    # orologio a muro sopra il letto (muro est)
    for (cy, cz) in ((2.05, 1.2),):
        B(m, 'legno', '#4a2f1a', W - .04, cy - .17, cz - .17, W, cy + .17, cz + .17)
        B(m, 'liscio', '#efe6d0', W - .045, cy - .14, cz - .14, W - .04, cy + .14, cz + .14)
        B(m, 'metallo', '#1b1b1b', W - .05, cy - .005, cz - .005, W - .045, cy + .1, cz + .005)
        B(m, 'metallo', '#1b1b1b', W - .05, cy - .005, cz - .005, W - .045, cy + .005, cz + .07)
    # arredi
    ins = lambda p, x, z, rot=0, y=0, s=1: m.ins(p, x, y, z, rot, s)
    ins(cucina(), 2.25, 4.65, 180)
    ins(frigo(), .6, 4.6, 180)
    ins(tavolo_casa(), 4.5, 3.2)
    seg = sedia('casa_sedia', '#7a5230', '#c9a45a')
    for (x, z, r) in ((3.4, 3.2, 90), (5.6, 3.2, 270), (4.5, 2.4, 0), (4.5, 4.0, 180)):
        ins(seg, x, z, r)
    ins(credenza(), 1.1, .225)
    ins(mobile_tv(), .35, 2.0, 90)
    ins(letto(), 6.1, 1.2)
    ins(comodino(), 5.1, .4)
    ins(lettino(), 1.3, 3.0)
    m.ins(cornice_specchio(), 5.8, 1.5, 4.97, 180, 1)         # il quad 'Specchio' di gioco sta a z=4.94, la cornice lo incornicia
    ins(lampadario(), 3.5, 2.5)
    return m


# ================================================================= BAR
BW, BD, BH = 10.0, 7.0, 3.0


def bancone():
    """Bancone 6 x 0,8 x 1,1 m con macchina del caffe', macinino, cassa e alzata di cornetti. Fronte +z = lato clienti (retro a z=-0.4)."""
    m = Mod('bar_bancone')
    leg, ton = '#5a3a22', '#7a5230'
    B(m, 'legno', leg, -3, .1, -.4, 3, 1.04, .4)
    B(m, 'gomma', '#1b1b1d', -3, 0, -.38, 3, .1, .36)
    for i in range(12):
        u0 = -3 + i * .5
        B(m, 'legno', ton, u0 + .03, .16, .4, u0 + .47, .96, .415)
        B(m, 'legno', leg, u0 + .09, .24, .415, u0 + .41, .88, .422)
    B(m, 'pietra', '#c9c4b4', -3.02, 1.04, -.42, 3.02, 1.1, .5)                              # piano di marmo, sporge dal lato clienti
    B(m, 'metallo', '#b08d3c', -3.02, 1.1, .5, 3.02, 1.115, .51)
    for i in range(7):                                                                         # poggiapiedi in ottone
        Tu(m, 'metallo', '#b08d3c', (-3 + i, .12, .4), (-3 + i, .18, .62), .012, 5)
    Tu(m, 'metallo', '#b08d3c', (-3, .18, .62), (3, .18, .62), .028, 8)
    # macchina del caffe' a due gruppi (lato barista = -z)
    x0 = 1.2
    B(m, 'metallo', '#b9bec2', x0 - .34, 1.1, -.25, x0 + .34, 1.42, .2)
    B(m, 'metallo', '#8d9499', x0 - .34, 1.42, -.25, x0 + .34, 1.45, .2)
    B(m, 'gomma', '#2a2a2d', x0 - .3, 1.12, -.3, x0 + .3, 1.16, -.22)
    for sx in (-.15, .15):
        Cil(m, 'metallo', '#8d9499', x0 + sx, -.28, 1.18, 1.26, .045, 8)
        Tu(m, 'gomma', '#1b1b1b', (x0 + sx, 1.2, -.3), (x0 + sx, 1.2, -.5), .014, 6)
        Cil(m, 'liscio', '#f1ecde', x0 + sx * 1.3, -.1, 1.45, 1.5, .035, 8, r1=.045)
        Cil(m, 'liscio', '#f1ecde', x0 + sx * 1.3 + .1, -.1, 1.45, 1.5, .035, 8, r1=.045)
    Tu(m, 'metallo', '#b9bec2', (x0 + .3, 1.32, -.2), (x0 + .38, 1.2, -.34), .008, 5)
    Tu(m, 'metallo', '#b9bec2', (x0 - .3, 1.32, -.2), (x0 - .38, 1.2, -.34), .008, 5)
    Sf(m, 'liscio', '#3bbf6a', x0, 1.4, .205, .018, .018, .01, 6, 3, e=2)
    # macinino
    gx = .55
    B(m, 'gomma', '#2a2a2d', gx - .1, 1.1, -.2, gx + .1, 1.4, .0)
    Cil(m, 'vetro', '#5a3a22', gx, -.1, 1.4, 1.62, .1, 10, r1=.06)
    Cil(m, 'metallo', '#b9bec2', gx, -.1, 1.62, 1.64, .062, 10)
    # cassa
    B(m, 'liscio', '#cfc6a8', -2.5, 1.1, -.2, -2.0, 1.2, .2)
    B(m, 'liscio', '#cfc6a8', -2.5, 1.2, -.2, -2.0, 1.34, -.0)
    B(m, 'vetro', '#1b3a2a', -2.45, 1.24, .0, -2.05, 1.32, .004, e=1)
    for i in range(4):
        for j in range(3):
            B(m, 'liscio', '#6a6a60' if j else '#e8e4d4', -2.46 + j * .06, 1.2, .02 + i * .045, -2.42 + j * .06, 1.215, .05 + i * .045)
    B(m, 'cartone', '#d8c9a0', -2.0 + .0, 1.1, -.2, -1.65, 1.13, .2)
    # alzata con cornetti e vassoio di zollette
    Cil(m, 'metallo', '#c8cdd0', -.6, .2, 1.115, 1.16, .02, 8)
    Cil(m, 'metallo', '#c8cdd0', -.6, .2, 1.16, 1.19, .24, 14)
    for i in range(5):
        a = i * 1.25
        Sf(m, 'crosta', '#c98a45', -.6 + .13 * math.cos(a), 1.23, .2 + .13 * math.sin(a), .07, .035, .04, 8, 4)
    Sf(m, 'crosta', '#c98a45', -.6, 1.25, .2, .06, .03, .04, 8, 4)
    Cil(m, 'cartone', '#d8c9a0', .0, .2, 1.115, 1.15, .1, 10)
    for i in range(8):
        B(m, 'liscio', '#f4f1e6', -.06 + (i % 4) * .035, 1.15, .14 + (i // 4) * .04, -.03 + (i % 4) * .035, 1.17, .17 + (i // 4) * .04)
    for k, x in enumerate((-1.6, -1.45, -1.3)):                                              # pile di tazzine e bicchieri
        Cil(m, 'liscio', '#f1ecde', x, -.1, 1.1, 1.1 + .02 + .05 * (k + 1), .04, 8)
    return m


def mensole():
    """Alzata del retro-banco: armadietti con sportelli, specchio, tre ripiani di bottiglie con luce a LED. Fronte +z, retro a z=-0.15."""
    m = Mod('bar_mensole')
    leg, ton = '#3b2a1c', '#4a3322'
    B(m, 'legno', leg, -3, 0, -.15, 3, 1.0, .15)
    for i in range(6):
        u0 = -3 + i
        B(m, 'legno', ton, u0 + .03, .08, .15, u0 + .97, .94, .165)
        B(m, 'legno', leg, u0 + .12, .16, .165, u0 + .88, .86, .172)
        Sf(m, 'metallo', '#b08d3c', u0 + (.18 if i % 2 == 0 else .82), .5, .18, .016, .016, .012, 6, 3)
    B(m, 'pietra', '#7a766a', -3, 1.0, -.15, 3, 1.04, .17)
    B(m, 'vetro', '#aebcc2', -3, 1.04, -.15, 3, 2.0, -.14)
    B(m, 'legno', leg, -3.0, 1.04, -.15, -2.97, 2.0, .15)
    B(m, 'legno', leg, 2.97, 1.04, -.15, 3.0, 2.0, .15)
    rng = random.Random(5)
    kinds = ['vino', 'amaro', 'liquore', 'vermut', 'grappa']
    for y in (1.22, 1.55, 1.88):
        B(m, 'legno', '#9a7448', -3, y - .025, -.14, 3, y, .14)
        B(m, 'liscio', '#ffb64d', -3, y - .045, .115, 3, y - .025, .14, e=3)
        if y == 1.88:
            B(m, 'legno', leg, -3.03, 2.0, -.15, 3.03, 2.06, .18)
            continue
        x = -2.85
        while x < 2.85:
            bottiglia(m, x, y, .02, rng.choice(kinds))
            x += .17 + rng.random() * .05
    x = -2.7
    for i in range(14):                                                                         # bicchieri sul ripiano alto
        Cil(m, 'vetro', '#dce8ec', -2.7 + i * .38, .02, 1.88, 1.96, .035, 8, r1=.05)
    return m


def frigo_gelati():
    """Espositore verticale dei gelati: cabinet bianco con telaio a vetri (aperto sui vasi), insegna luminosa con cono. Fronte +z."""
    m = Mod('bar_frigo_gelati')
    B(m, 'liscio', '#2b2b2e', -.35, 0, -.3, .35, .08, .3)
    B(m, 'metallo', '#eceff1', -.35, .08, -.35, .35, 1.8, -.25)
    B(m, 'metallo', '#eceff1', -.36, .08, -.35, -.3, 1.8, .35)
    B(m, 'metallo', '#eceff1', .3, .08, -.35, .36, 1.8, .35)
    B(m, 'metallo', '#eceff1', -.36, .08, -.35, .36, .14, .35)
    B(m, 'metallo', '#eceff1', -.36, 1.74, -.35, .36, 1.8, .35)
    B(m, 'metallo', '#c8cdd0', -.31, .14, .35, .31, .17, .37)                                  # telaio
    B(m, 'metallo', '#c8cdd0', -.31, 1.71, .35, .31, 1.74, .37)
    B(m, 'metallo', '#c8cdd0', -.31, .14, .35, -.28, 1.74, .37)
    B(m, 'metallo', '#c8cdd0', .28, .14, .35, .31, 1.74, .37)
    B(m, 'liscio', '#fff0c8', -.3, .17, -.24, .3, 1.71, -.235, e=1)                            # fondo illuminato
    cols = ['#f6e7b0', '#d94a5a', '#5a8a35', '#8a5a32', '#3aa0ff', '#ff7eb6', '#f2c94c', '#e8e4f0']
    for k, y in enumerate((.17, .56, .95, 1.34)):
        B(m, 'metallo', '#c8cdd0', -.3, y, -.24, .3, y + .015, .34)
        for i in range(3):
            for j in range(2):
                Cil(m, 'stampa_b', cols[(k * 3 + i + j * 2) % len(cols)], -.18 + i * .18, -.0 + j * .22 - .02, y + .015, y + .15, .075, 10)
                Cil(m, 'liscio', '#f4f1e6', -.18 + i * .18, -.0 + j * .22 - .02, y + .15, y + .165, .077, 10)
    B(m, 'metallo', '#c8cdd0', -.3, 1.74, .0, .3, 1.8, .38)
    B(m, 'stampa_a', '#d12e2e', -.36, 1.8, -.35, .36, 2.12, .35)                                # insegna luminosa
    B(m, 'liscio', '#fff0c8', -.32, 1.84, .35, .32, 2.08, .36, e=2)
    Tu(m, 'cartone', '#d9a85a', (0, 1.99, .375), (0, 1.86, .375), .045, 8, r1=.004)                 # cono gelato, punta in basso
    Sf(m, 'liscio', '#ff7eb6', 0, 1.99, .375, .06, .045, .02, 8, 4)
    Sf(m, 'liscio', '#f6e7b0', 0, 2.05, .375, .045, .035, .02, 8, 4)
    return m


def flipper():
    """Flipper: lungo 1,1 m, largo 0,55, testata (backbox) a z=-0.55 alta 1,8 m, sul fronte (z=+0.55) i pulsanti. Fronte +z."""
    m = Mod('bar_flipper')
    mt = '#2e86de'
    for (x, z) in ((-.24, -.5), (.24, -.5), (-.24, .5), (.24, .5)):
        Tu(m, 'metallo', '#b9bec2', (x, 0, z), (x, .78, z), .03, 8)
        Cil(m, 'gomma', '#1b1b1d', x, z, 0, .03, .04, 8)
    B(m, 'stampa_b', mt, -.28, .72, -.55, -.26, .92, .55)
    B(m, 'stampa_b', mt, .26, .72, -.55, .28, .92, .55)
    B(m, 'stampa_b', mt, -.28, .72, .53, .28, .84, .55)
    B(m, 'liscio', '#1d1d26', -.28, .72, -.55, .28, .74, .55)
    pf = [(-.26, .90, -.53), (.26, .90, -.53), (.26, .80, .53), (-.26, .80, .53)]            # piano di gioco inclinato
    Qd(m, 'stampa_a', '#f2c94c', *pf, (0, 3, 0))
    B(m, 'liscio', '#d12e2e', -.26, .88, -.4, .26, .905, -.38)
    for (x, z, c) in ((-.1, -.2, '#d12e2e'), (.1, -.25, '#f2d21b'), (0, -.05, '#3aa0ff'), (-.15, .0, '#f2d21b')):
        y = .90 - (z + .53) * (.10 / 1.06)
        Cil(m, 'liscio', c, x, z, y, y + .035, .035, 10)
        Cil(m, 'metallo', '#c8cdd0', x, z, y + .035, y + .04, .036, 10)
    for sx in (-1, 1):
        y = .90 - (.38 + .53) * (.10 / 1.06)
        Tu(m, 'metallo', '#c8cdd0', (sx * .05, y + .02, .39), (sx * .14, y + .03, .34), .014, 6)          # palette
        Tu(m, 'liscio', '#d12e2e', (sx * .28, .84, .4), (sx * .32, .84, .4), .018, 8)                    # pulsanti laterali
    Sf(m, 'metallo', '#d0d4d6', .0, .86, .12, .013, .013, .013, 6, 4)
    B(m, 'liscio', '#1d1d26', -.28, .9, -.62, .28, 1.82, -.35)                                  # backbox
    B(m, 'liscio', '#ffd23f', -.25, 1.2, -.35, .25, 1.78, -.345, e=2)
    B(m, 'stampa_b', '#d6279a', -.25, 1.2, -.345, .25, 1.45, -.34, e=2)
    B(m, 'stampa_b', '#2f6fcf', -.25, 1.45, -.345, .25, 1.78, -.34, e=2)
    B(m, 'metallo', '#c8cdd0', -.29, 1.8, -.63, .29, 1.84, -.34)
    B(m, 'metallo', '#c8cdd0', -.1, .72, .55, .1, .82, .56)                                     # sportello gettoniera
    B(m, 'liscio', '#d12e2e', -.04, .76, .56, .04, .8, .565, e=1)
    return m


def tv_bar():
    """TV a tubo su staffa a muro (muro dietro a z=-0.4 del pezzo), fronte +z, origine al centro del fondo dell'apparecchio."""
    m = Mod('bar_tv')
    B(m, 'metallo', '#2a2d30', -.06, .0, -.4, .06, .18, -.37)
    Tu(m, 'metallo', '#2a2d30', (0, .08, -.38), (0, -.03, -.0), .025, 6)
    B(m, 'metallo', '#2a2d30', -.3, -.07, -.15, .3, -.03, .2)
    m.ins(tv_crt('tv_crt_bar'), 0, -.03, 0, 0, .8)
    return m


def insegna_bar():
    """Insegna al neon 'BAR' su pannello scuro; letta da chi guarda verso +z (muro nord), origine al centro del pannello sul piano del muro (z=0)."""
    m = Mod('bar_insegna')
    B(m, 'metallo', '#1b1b1f', -1.0, -.3, -.04, 1.0, .3, -.01)
    B(m, 'legno', '#3a2a1c', -1.04, -.34, -.045, 1.04, -.3, -.005)
    B(m, 'legno', '#3a2a1c', -1.04, .3, -.045, 1.04, .34, -.005)
    B(m, 'legno', '#3a2a1c', -1.04, -.3, -.045, -1.0, .3, -.005)
    B(m, 'legno', '#3a2a1c', 1.0, -.3, -.045, 1.04, .3, -.005)
    for k, ch in enumerate('BAR'):
        lettera(m, ch, -.78 + k * .55, -.2, .4, .4, .07, -.07, '#ff3b2f', e=3)
    return m


def lampada_bar(nome='bar_lampada'):
    """Lampada a campana smaltata verde appesa al soffitto (y=3.0), emissiva sotto."""
    m = Mod(nome)
    Tu(m, 'metallo', '#1b1b1d', (0, 2.78, 0), (0, 3.0, 0), .008, 5)
    Cil(m, 'metallo', '#2f5d46', 0, 0, 2.58, 2.8, .26, 16, r1=.04)
    Cil(m, 'metallo', '#c9a227', 0, 0, 2.575, 2.585, .262, 16)
    Cil(m, 'liscio', '#ffd9a0', 0, 0, 2.56, 2.575, .2, 14, e=2)
    return m


def bar():
    m = Mod('bar_interno')
    W, D, H = BW, BD, BH
    rng = random.Random(9)
    ps = (4.5, 5.5)
    fa, fb = (1.2, 3.0), (7.0, 8.8)
    bs = {'S': [(ps[0], ps[1], 0, 2.2), (fa[0], fa[1], .9, 2.2), (fb[0], fb[1], .9, 2.2)], 'N': [], 'W': [], 'E': []}
    # pavimento a scacchi
    B(m, 'legno', '#3a2a1c', 0, -.03, 0, W, -.02, D)
    for i in range(int(W / .5)):
        for j in range(int(D / .5) + 1):
            z1 = min((j + 1) * .5, D)
            if z1 - j * .5 < 1e-6:
                continue
            m.add('piastrelle', '#d9d2bd' if (i + j) % 2 else '#5b5a52', *lastra(i * .5 + .002, 0, j * .5 + .002, (i + 1) * .5, 0, z1, (W / 2, 1, D / 2)))
    # muri: zoccolo di legno scuro a 1,2 m, sopra intonaco giallino da fumo
    for asse, c, a0, a1, k, ctr in (('z', 0, 0, W, 'S', (W / 2, 1, 1)), ('z', D, 0, W, 'N', (W / 2, 1, D - 1)), ('x', 0, 0, D, 'W', (1, 1, D / 2)), ('x', W, 0, D, 'E', (W - 1, 1, D / 2))):
        parete(m, 'legno', '#5b3a24', asse, c, a0, a1, 0, 1.2, bs[k], ctr)
        parete(m, 'intonaco', '#c8a76e', asse, c, a0, a1, 1.2, H, bs[k], ctr)
    for k in range(1, int(W / .25)):
        x = k * .25
        if not any(h[0] - .1 < x < h[1] + .1 for h in bs['S'] if h[2] < 1.0):
            B(m, 'legno', '#4a2f1a', x - .004, 0, 0, x + .004, 1.2, .012)
        B(m, 'legno', '#4a2f1a', x - .004, 0, D - .012, x + .004, 1.2, D)
    for k in range(1, int(D / .25)):
        z = k * .25
        B(m, 'legno', '#4a2f1a', 0, 0, z - .004, .012, 1.2, z + .004)
        B(m, 'legno', '#4a2f1a', W - .012, 0, z - .004, W, 1.2, z + .004)
    for (x0, x1) in tagli(0, W, [ps, fa, fb], .1):
        B(m, 'legno', '#3a2a1c', x0, 1.2, 0, x1, 1.25, .04)
    B(m, 'legno', '#3a2a1c', 0, 1.2, D - .04, W, 1.25, D)
    B(m, 'legno', '#3a2a1c', 0, 1.2, 0, .04, 1.25, D)
    B(m, 'legno', '#3a2a1c', W - .04, 1.2, 0, W, 1.25, D)
    # soffitto a doghe con travi
    m.add('legno', '#6a4a2c', *lastra(0, H, 0, W, H, D, (W / 2, 1, D / 2)))
    for z in (1.0, 3.5, 6.0):
        B(m, 'legno', '#3a2a1c', 0, H - .18, z - .09, W, H, z + .09)
    for x in (.0, W / 2 - .09, W - .18):
        B(m, 'legno', '#3a2a1c', x, H - .12, 0, x + .18, H - .0, D)
    # porta a vetri con insegna APERTO
    porta_int(m, 'z', 0, -1, ps[0], ps[1], 2.2, '#3d2a1c', '#4a2f1a', vetro=True)
    B(m, 'stampa_a', '#d12e2e', 4.72, 1.55, .0, 5.28, 1.72, .012)
    B(m, 'liscio', '#fff0c8', 4.76, 1.58, .012, 5.24, 1.69, .016, e=1)
    # vetrine con tendine a meta' altezza
    for (a0, a1) in (fa, fb):
        finestra_int(m, 'z', 0, -1, a0, a1, .9, 2.2, '#3d2a1c', '#c43b2d', cielo='#a9c4d6', tenda_h=.55)
    # lavagna del menu' e poster
    B(m, 'legno', '#4a2f1a', .0, 1.35, 5.25, .05, 2.05, 6.55)
    B(m, 'liscio', '#26332b', .05, 1.4, 5.3, .06, 2.0, 6.5)
    for k, (z0, z1, y) in enumerate(((5.4, 6.2, 1.88), (5.4, 6.0, 1.78), (5.4, 6.3, 1.68), (5.4, 5.9, 1.58), (5.4, 6.1, 1.48))):
        B(m, 'liscio', '#e8e4d0', .06, y, z0, .063, y + .03, z1)
        B(m, 'liscio', '#e8e4d0', .06, y, 6.3, .063, y + .03, 6.4)
    for (z, y, w_, h_, t, mt) in ((1.2, 1.5, .5, .7, '#2e86de', 'stampa_b'), (2.1, 1.55, .55, .75, '#d6279a', 'stampa_a')):   # poster sul muro est
        B(m, 'metallo', '#c8cdd0', W - .03, y - .02, z - w_ / 2 - .02, W, y + h_ + .02, z + w_ / 2 + .02)
        B(m, mt, t, W - .035, y, z - w_ / 2, W - .03, y + h_, z + w_ / 2)
    # orologio sopra la porta
    B(m, 'legno', '#3a2a1c', 4.8, 2.35, .0, 5.2, 2.75, .05)
    B(m, 'liscio', '#efe6d0', 4.82, 2.37, .05, 5.18, 2.73, .055)
    B(m, 'metallo', '#1b1b1b', 4.99, 2.55, .055, 5.01, 2.7, .06)
    B(m, 'metallo', '#1b1b1b', 4.99, 2.55, .055, 5.1, 2.57, .06)
    # arredi
    ins = lambda p, x, z, rot=0, y=0, s=1: m.ins(p, x, y, z, rot, s)
    ins(bancone(), 5.0, 5.6, 180)
    ins(mensole(), 5.0, 6.85, 180)
    m.ins(insegna_bar(), 5.0, 2.5, 6.99, 0, 1)
    sg = sgabello_bar()
    for x in (3.0, 4.3, 5.7, 7.0):
        ins(sg, x, 4.7)
    tv = tavolino_bar()
    seg = sedia('bar_sedia', '#3a2a1c', '#7a2323', h_sch=.9, lato=.4)
    for x in (2.0, 8.0):
        ins(tv, x, 2.2)
        ins(seg, x - .8, 2.2, 90)
        ins(seg, x + .8, 2.2, 270)
    ins(flipper(), .62, 3.7, 90)
    ins(frigo_gelati(), 9.65, 3.75, 270)
    m.ins(tv_bar(), 9.55, 2.1, 4.9, 270, 1)
    lamp = lampada_bar()
    ins(lamp, 5.0, 2.5)
    ins(lamp, 5.0, 5.0)
    return m


# ================================================================= scrittura
def scrivi(nome, cartella):
    """.obj compatto per Unity: x_obj = -x e triangoli invertiti (l'importatore specchia x e rovescia il verso: il risultato e' il modello cosi' com'e')."""
    mtl, usati, facce = [], set(), {}
    vi, vv, vts, vtv = {}, [], {}, []
    for key, v, t in tutte_le_parti(nome):
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
            A, B_, C = v[a], v[b], v[c]
            n = cross(sub(B_, A), sub(C, A))
            ax = max(range(3), key=lambda i: abs(n[i]))
            f = []
            for p in (A, C, B_):                                      # ordine invertito
                q = (round(-p[0], 3), round(p[1], 3), round(p[2], 3))
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
    testa = ['# {} - v5 interni, 1 unita = 1 metro, coordinate stanza (x specchiata per l\'importatore di Unity)'.format(nome), 'mtllib {}.mtl'.format(nome), 'o ' + nome]
    testa += ['v {:g} {:g} {:g}'.format(*p) for p in vv] + ['vt {:g} {:g}'.format(*u) for u in vtv]
    for nm, fs in facce.items():
        testa += ['usemtl ' + nm] + fs
    (cartella / (nome + '.obj')).write_text('\n'.join(testa) + '\n')
    (cartella / (nome + '.mtl')).write_text('\n'.join(mtl) + '\n')
    return len(vv), sum(len(f) for f in facce.values()), len(usati)


def main():
    casa(), bar()
    USCITA.mkdir(parents=True, exist_ok=True)
    UNITY.mkdir(parents=True, exist_ok=True)
    for f in list(USCITA.glob('*.obj')) + list(USCITA.glob('*.mtl')) + list(UNITY.glob('*.obj')) + list(UNITY.glob('*.mtl')):
        f.unlink()
    for nome, (W, D, H) in (('casa_interno', (CW, CD, CH)), ('bar_interno', (BW, BD, BH))):
        xs, ys, zs = zip(*(p for _, v, _ in tutte_le_parti(nome) for p in v))
        # controllo: tutto dentro la stanza (+ spessore dei vani 0,2 m e antenna/cornici); un pezzo fuori posto fa fallire il generatore
        assert -.3 <= min(xs) and max(xs) <= W + .3 and -.3 <= min(zs) and max(zs) <= D + .3 and -.1 <= min(ys) and max(ys) <= H + .1, (nome, min(xs), max(xs), min(zs), max(zs), min(ys), max(ys))
        nv, nt, nm = scrivi(nome, USCITA)
        print('{}: {} vertici, {} triangoli, {} materiali'.format(nome, nv, nt, nm))
    tex, ttex = USCITA / 'texture', UNITY / 'texture'
    tex.mkdir(exist_ok=True)
    ttex.mkdir(exist_ok=True)
    usate = set()
    for f in USCITA.glob('*.mtl'):
        usate |= {r.split('/')[-1] for r in f.read_text().split() if r.startswith('texture/')}
    for t in usate:
        for dst in (tex, ttex):
            shutil.copy(RADICE / 'obj_v3' / 'texture' / t, dst / t)
    for f in list(USCITA.glob('*.obj')) + list(USCITA.glob('*.mtl')):
        shutil.copy(f, UNITY / f.name)
    print('scritto in', USCITA, 'e', UNITY)


if __name__ == '__main__':
    main()
