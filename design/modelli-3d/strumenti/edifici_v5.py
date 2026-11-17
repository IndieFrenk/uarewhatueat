"""Piazza v5: facciate, casa, bar, bottega e arredo della piazza di Canolo, dimensionati sugli spazi di VillageDressing.cs.

Riusa le funzioni v4 (edificio, finestra, porta, tenda, tetto_a_falde, comignolo) senza copiarle.
Convenzioni (uguali ai v4): 1 unita = 1 m, Y su, FRONTE verso +Z, pivot al centro del fronte a terra
(edifici: z in [-profondita, 0], x in [-w/2, w/2]; arredo: pivot al centro in pianta, base a y=0).
Unity importa gli .obj specchiando X: vista dal fronte (da +Z) il modello resta identico, ma la x del modello diventa -x locale.
"""
import math

import edifici_v4 as ed
from edifici_v4 import comignolo, edificio, porta, tenda, tetto_a_falde
from geo import Mod, REG, caja, esfera, lastra, obliquo, piano, prisma, tetto, tronco, tubo

SPENTO = '#252b33'   # vetro delle finestre spente
_finestra_v4 = ed.finestra


def finestra_vista(nome, w=1.1, h=1.2, **k):
    """La finestra v4 ha il vetro a z=-0.04..-0.005, dietro al muro pieno di edificio(): non si vedrebbe. Aggiungo un vetro 5 mm fuori dal piano del muro."""
    m = _finestra_v4(nome, w, h, **k)
    luce, e = k.get('luce', '#e8d6a0'), k.get('e', 1)
    m.add('vetro', luce, *caja(-w / 2, 0, 0.004, w / 2, h, 0.012), e=e)
    if k.get('tenda'):
        t = k['tenda']
        m.add('tessuto', t, *caja(-w / 2 + 0.02, 0.1, 0.014, -w * 0.12, h - 0.06, 0.026))
        m.add('tessuto', t, *caja(w * 0.12, 0.1, 0.014, w / 2 - 0.02, h - 0.06, 0.026))
    return m


ed.finestra = finestra_vista   # edificio() chiama finestra() dal suo modulo
finestra = finestra_vista

# --------------------------------------------------------------------------- scritte (font 5x7 a geometria)
FONT = {
    'A': '01110 10001 10001 11111 10001 10001 10001', 'B': '11110 10001 10001 11110 10001 10001 11110',
    'C': '01110 10001 10000 10000 10000 10001 01110', 'D': '11110 10001 10001 10001 10001 10001 11110',
    'E': '11111 10000 10000 11110 10000 10000 11111', 'F': '11111 10000 10000 11110 10000 10000 10000',
    'H': '10001 10001 10001 11111 10001 10001 10001', 'I': '11111 00100 00100 00100 00100 00100 11111',
    'L': '10000 10000 10000 10000 10000 10000 11111', 'M': '10001 11011 10101 10101 10001 10001 10001',
    'N': '10001 11001 10101 10011 10001 10001 10001', 'O': '01110 10001 10001 10001 10001 10001 01110',
    'P': '11110 10001 10001 11110 10000 10000 10000', 'R': '11110 10001 10001 11110 10100 10010 10001',
    'S': '01111 10000 10000 01110 00001 00001 11110', 'T': '11111 00100 00100 00100 00100 00100 00100',
    'U': '10001 10001 10001 10001 10001 10001 01110', 'V': '10001 10001 10001 10001 10001 01010 00100',
}


def scritta(m, testo, cx, y0, z, h, tinta, e=0, spessore=0.025):
    """Lettere a rilievo (blocchi) centrate su cx, alte h, leggibili da +Z. Restituisce la larghezza."""
    px = h / 7
    tot = len(testo) * 6 * px - px
    x = cx - tot / 2
    for ch in testo:
        if ch != ' ':
            for r, riga in enumerate(FONT[ch].split()):
                c = 0
                while c < 5:
                    if riga[c] != '1':
                        c += 1
                        continue
                    c1 = c
                    while c1 + 1 < 5 and riga[c1 + 1] == '1':
                        c1 += 1
                    m.add('liscio', tinta, *caja(x + c * px, y0 + (6 - r) * px, z, x + (c1 + 1) * px, y0 + (7 - r) * px, z + spessore), e=e)
                    c = c1 + 1
        x += 6 * px
    return tot


def insegna5(m, testo, cx, y0, y1, z, larg, tavola, cornice, ink, e=0, h=None, ganci=True):
    """Insegna a tavola con scritta in rilievo: cornice di legno, tavola 'insegna' (vernice scrostata), lettere."""
    x0, x1 = cx - larg / 2, cx + larg / 2
    m.add('legno', cornice, *caja(x0 - 0.08, y0 - 0.08, z, x1 + 0.08, y1 + 0.08, z + 0.1))
    m.add('insegna', tavola, *caja(x0, y0, z + 0.1, x1, y1, z + 0.115))
    h = h or (y1 - y0) * 0.6
    scritta(m, testo, cx, (y0 + y1) / 2 - h / 2, z + 0.115, h, ink, e)
    if ganci:
        for x in (x0, x1):
            m.add('legno', cornice, *caja(x - 0.04, y1 + 0.08, z - 0.02, x + 0.04, y1 + 0.3, z + 0.02))


def vaso(m, x, y, z, s, fiori=('#d94a5a', '#f2d21b', '#e8e4f0')):
    m.add('terra', '#a8613f', *prisma(x, z, y, y + s * 0.8, s * 0.5, s * 0.38, 10))
    m.add('erba', '#4f8a3a', *esfera(x, y + s * 1.1, z, s * 0.55, s * 0.45, s * 0.55, 7, 5))
    for i in range(4):
        a = i * 1.7 + x
        m.add('liscio', fiori[i % len(fiori)], *esfera(x + math.cos(a) * s * 0.35, y + s * 1.4, z + math.sin(a) * s * 0.3, s * 0.12, s * 0.12, s * 0.12, 5, 3))


def piano_alto(x, yb=4.3, w=1.1, h=1.3, persiane='#4b6a50', acceso=False, **k):
    """Finestra del piano di sopra (spenta o accesa), nel formato di edificio(finestre=...)."""
    opz = dict(persiane=persiane, luce='#f0c070' if acceso else SPENTO, e=1 if acceso else 0, lamelle=False)
    opz.update(k)
    return (x, yb, w, h, opz)


def vetrina(x, w, telaio, yb=0.85, h=1.7, luce='#f6ecd2'):
    return (x, yb, w, h, dict(persiane=None, luce=luce, telaio=telaio))


# --------------------------------------------------------------------------- negozi della fila nord e sud
def macelleria():
    """12 x 8, colmo 6.5 + 1.6. Bancone di marmo davanti (z 0.55..1.05), salumi appesi sotto il tendone."""
    fin = [vetrina(-3.9, 3.0, '#8a2a2a'), vetrina(-0.3, 3.0, '#8a2a2a'),
           piano_alto(-4.2, persiane='#8a2a2a'), piano_alto(-1.4, persiane='#8a2a2a', acceso=True),
           piano_alto(1.4, persiane='#8a2a2a'), piano_alto(4.2, persiane='#8a2a2a')]
    m = edificio('macelleria', 12, 8, 6.5, 'intonaco', '#d9bfae', '#5b3a34', 1.6, 'x', finestre=fin, porta_x=3.0, porta_w=1.0,
                 porta_tinta='#8a2a2a', comignoli=[(-4, -5.5), (4, -5.5)])
    insegna5(m, 'MACELLERIA', 0, 3.25, 3.95, 0.04, 6.4, '#e6d8c4', '#5a1a1a', '#8a1f1f')
    tenda(m, -5.5, 5.5, 2.5, 1.1, '#a02a2a', '#f0ece0', 12)
    m.add('metallo', '#9aa1a6', *tubo((-5.2, 2.55, 0.15), (1.4, 2.55, 0.15), 0.012, 0.012, 5))
    for i, x in enumerate((-4.6, -3.7, -2.8, -1.9, -1.0, -0.1, 0.8)):
        m.add('metallo', '#9aa1a6', *tubo((x, 2.55, 0.15), (x, 2.05, 0.15), 0.008, 0.008, 4))
        m.add('crosta' if i % 2 else 'liscio', '#7a3b2e' if i % 2 else '#b24a4a', *esfera(x, 1.9, 0.15, 0.1, 0.17, 0.08, 7, 5))
    m.add('piastrelle', '#ebe5d6', *caja(-2.0, 0, 0.55, 2.0, 0.9, 1.05))
    m.add('tessuto', '#a02a2a', *caja(-2.05, 0.9, 0.5, 2.05, 0.95, 1.1))
    for i in range(4):
        m.add('liscio', '#c9c4b8', *caja(-1.7 + i * 0.9, 0.95, 0.65, -0.95 + i * 0.9, 1.0, 0.95))
        m.add('crosta', '#b24a4a', *esfera(-1.32 + i * 0.9, 1.05, 0.8, 0.22, 0.07, 0.12, 7, 4))
    return m


def fioraio():
    """8 x 8. Bancale con vasi davanti (due metà, lasciano libera la porta), due vasi grandi ai lati."""
    fin = [vetrina(-2.5, 1.8, '#2f5b3a', luce='#eaf4dc'), vetrina(2.5, 1.8, '#2f5b3a', luce='#eaf4dc'),
           piano_alto(-2.5, persiane='#2f5b3a'), piano_alto(0, persiane='#2f5b3a', acceso=True), piano_alto(2.5, persiane='#2f5b3a')]
    m = edificio('fioraio', 8, 8, 6.5, 'intonaco', '#b9c7a4', '#4f4a56', 1.6, 'x', finestre=fin, porta_x=0.0, porta_w=1.0,
                 porta_tinta='#2f5b3a', vetro_porta=True, comignoli=[(2.0, -5.5)])
    insegna5(m, 'FIORI', 0, 3.25, 3.95, 0.04, 3.4, '#dfe6cc', '#26502e', '#2d6a3a')
    tenda(m, -3.6, 3.6, 2.5, 1.1, '#3f6a47', '#e6e0d0', 10)
    for s in (-1, 1):
        m.add('legno', '#8a6a44', *caja(min(s * 0.7, s * 3.5), 0, 0.4, max(s * 0.7, s * 3.5), 0.5, 1.2))
        m.add('legno', '#6b4a2f', *caja(min(s * 0.75, s * 3.45), 0.5, 0.45, max(s * 0.75, s * 3.45), 0.55, 1.15))
        for i in range(3):
            vaso(m, s * (1.2 + i * 0.9), 0.55, 0.8, 0.42)
        vaso(m, s * 3.9, 0, 0.8, 0.8)
    return m


def tabacchi():
    """8 x 8. Insegna scura con il cartello a T sporgente."""
    fin = [vetrina(2.1, 2.6, '#26324f'), vetrina(-3.1, 1.2, '#26324f'),
           piano_alto(-2.5, persiane='#26324f'), piano_alto(0, persiane='#26324f'), piano_alto(2.5, persiane='#26324f', acceso=True)]
    m = edificio('tabacchi', 8, 8, 6.5, 'intonaco', '#d9c799', '#5a4a44', 1.6, 'x', finestre=fin, porta_x=-1.5, porta_w=1.0,
                 porta_tinta='#26324f', vetro_porta=True, comignoli=[(-2.0, -5.5)])
    insegna5(m, 'TABACCHI', 0.4, 3.25, 3.95, 0.04, 4.6, '#2c3350', '#141a2c', '#e6e0d0')
    tenda(m, -3.7, 3.7, 2.5, 1.1, '#264a82', '#e6e0d0', 10)
    # cartello a T blu: braccio a muro e tavola a due facce
    m.add('metallo', '#2a2d30', *caja(3.1, 3.55, 0.0, 3.2, 3.62, 0.85))
    m.add('liscio', '#1b3f8a', *caja(3.08, 3.0, 0.5, 3.22, 3.9, 1.1))
    for sx in (3.06, 3.22):
        m.add('liscio', '#f4f1e6', *caja(sx, 3.62, 0.62, sx + 0.02, 3.78, 0.98))
        m.add('liscio', '#f4f1e6', *caja(sx, 3.1, 0.77, sx + 0.02, 3.62, 0.83))
    return m


def osteria():
    """12 x 9. Facciata con travi a vista, cassette di fiori, barile a destra, lanterna."""
    fin = [(-3.9, 1.0, 1.3, 1.4, dict(persiane='#6a3f32', tenda='#c9503a', cassetta=True, luce='#f0c070')),
           (3.9, 1.0, 1.3, 1.4, dict(persiane='#6a3f32', tenda='#c9503a', cassetta=True, luce='#f0c070')),
           piano_alto(-3.9, 4.4, persiane='#6a3f32'), piano_alto(3.9, 4.4, persiane='#6a3f32'), piano_alto(0, 4.4, persiane='#6a3f32', acceso=True)]
    lat = [(1, -4.5, 1.2, 1.1, 1.3, dict(persiane='#6a3f32', lamelle=False)), (1, -4.5, 4.4, 1.1, 1.3, dict(persiane='#6a3f32', luce=SPENTO, e=0, lamelle=False)),
           (-1, -4.5, 4.4, 1.1, 1.3, dict(persiane='#6a3f32', luce=SPENTO, e=0, lamelle=False))]
    m = edificio('osteria', 12, 9, 6.5, 'intonaco', '#caa97e', '#6a3f32', 1.9, 'x', finestre=fin, laterali=lat, porta_x=0.0, porta_w=1.2,
                 porta_tinta='#4a3326', comignoli=[(3.5, -6.5)])
    for x in (-5.8, -1.9, 1.9, 5.8):
        m.add('legno', '#4a3326', *caja(x - 0.12, 0.76, -0.03, x + 0.12, 6.4, 0.08))
    insegna5(m, 'OSTERIA', 0, 3.3, 3.95, 0.1, 3.0, '#d9c08a', '#3a2418', '#6a1f1a')
    m.add('metallo', '#2a2a2a', *tubo((2.9, 3.4, 0.0), (2.9, 3.4, 0.7), 0.02, 0.02, 5))
    m.add('liscio', '#fff0c8', *caja(2.8, 2.85, 0.6, 3.0, 3.3, 0.8), e=1)
    m.add('metallo', '#2a2a2a', *caja(2.76, 3.3, 0.56, 3.04, 3.36, 0.84))
    m.add('legno', '#6a3f32', *prisma(5.2, 0.55, 0, 0.9, 0.42, 0.36, 12))
    for y in (0.15, 0.75):
        m.add('metallo', '#3a3d40', *prisma(5.2, 0.55, y, y + 0.05, 0.435, 0.435, 12))
    return m


def canonica():
    """7 x 8, casa del parroco accanto alla chiesa: targa con la scritta."""
    fin = [(2.0, 1.2, 1.1, 1.3, dict(persiane='#4b6a50')), (-2.3, 1.2, 1.1, 1.3, dict(persiane='#4b6a50')),
           piano_alto(-2.3, 4.0, 1.0, persiane='#4b6a50'), piano_alto(0, 4.0, 1.0, persiane='#4b6a50', acceso=True), piano_alto(2.3, 4.0, 1.0, persiane='#4b6a50')]
    lat = [(1, -4, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', lamelle=False)), (-1, -4, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', lamelle=False)),
           (-1, -4, 4.0, 1.1, 1.3, dict(persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False))]
    m = edificio('canonica', 7, 8, 6.5, 'intonaco', '#c7b294', '#594d4d', 1.8, 'x', finestre=fin, laterali=lat, porta_x=-0.1, porta_w=1.0,
                 porta_tinta='#3b2a20', comignoli=[(1.6, -5.5)])
    m.add('pietra', '#a89d86', *caja(-1.55, 2.5, 0.0, 1.35, 2.95, 0.06))
    scritta(m, 'CANONICA', -0.1, 2.58, 0.06, 0.28, '#2a241c')
    m.add('metallo', '#c9a227', *esfera(0.7, 1.05, 0.12, 0.03, 0.03, 0.03, 5, 3))
    return m


# --------------------------------------------------------------------------- casa del protagonista, bar, bottega
def balcone(m, x, y, larg=2.2, prof=0.9, tinta='#3a3d40'):
    m.add('pietra', '#a8a290', *caja(x - larg / 2, y, 0.0, x + larg / 2, y + 0.12, prof))
    for i in range(int(larg / 0.2) + 1):
        px = x - larg / 2 + 0.05 + i * (larg - 0.1) / int(larg / 0.2)
        m.add('metallo', tinta, *caja(px - 0.012, y + 0.12, prof - 0.04, px + 0.012, y + 1.0, prof - 0.016))
    m.add('metallo', tinta, *caja(x - larg / 2, y + 1.0, prof - 0.05, x + larg / 2, y + 1.05, prof))
    for sx in (-1, 1):
        m.add('metallo', tinta, *caja(x + sx * (larg / 2 - 0.025), y + 0.12, 0.0, x + sx * (larg / 2) + (0 if sx > 0 else 0.025), y + 1.0, prof))


def casa_piazza():
    """8 x 8, colmo a 6 (+2 di tetto). Porta al centro del fronte (la porta di gioco 'Entra in casa'), balcone sopra."""
    fin = [(-2.6, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', cassetta=True)), (2.6, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', luce='#f0c070')),
           piano_alto(-2.6, 3.7, persiane='#4b6a50', cassetta=True), piano_alto(2.6, 3.7, persiane='#4b6a50', acceso=True),
           (0, 3.7, 1.0, 1.9, dict(persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False))]
    lat = [(1, -4, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', lamelle=False)), (1, -4, 3.7, 1.1, 1.3, dict(persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False)),
           (-1, -4, 1.2, 1.1, 1.3, dict(persiane='#4b6a50', lamelle=False)), (-1, -4, 3.7, 1.1, 1.3, dict(persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False))]
    m = edificio('casa_piazza', 8, 8, 6, 'intonaco', '#b39980', '#804033', 2.0, 'x', finestre=fin, laterali=lat, porta_x=0.0, porta_w=1.0,
                 porta_tinta='#5b4030', comignoli=[(2.4, -5.5)])
    balcone(m, 0, 3.45)
    m.add('pietra', '#a8a290', *caja(-0.4, 2.45, 0.0, 0.4, 2.55, 0.12))   # targa sopra la porta
    return m


def bar_piazza():
    """8 x 8, colmo a 6. Porta a vetri al centro del fronte (la porta di gioco 'Entra al bar'), tendone e insegna al neon."""
    fin = [(-2.7, 0.9, 1.9, 1.6, dict(persiane=None, luce='#f0c070', tenda='#8a2a2a')), (2.7, 0.9, 1.9, 1.6, dict(persiane=None, luce='#f0c070', tenda='#8a2a2a')),
           piano_alto(-2.7, 4.2, 1.1, 1.2, persiane='#2f4a3a'), piano_alto(0, 4.2, 1.1, 1.2, persiane='#2f4a3a', acceso=True), piano_alto(2.7, 4.2, 1.1, 1.2, persiane='#2f4a3a')]
    lat = [(1, -4, 1.0, 1.0, 1.4, dict(persiane='#2f4a3a', lamelle=False)), (-1, -4, 1.0, 1.0, 1.4, dict(persiane='#2f4a3a', lamelle=False)),
           (1, -4, 4.2, 1.1, 1.2, dict(persiane='#2f4a3a', luce=SPENTO, e=0, lamelle=False)), (-1, -4, 4.2, 1.1, 1.2, dict(persiane='#2f4a3a', luce=SPENTO, e=0, lamelle=False))]
    m = edificio('bar_piazza', 8, 8, 6, 'mattone', '#b5684a', '#4a3b34', 1.6, 'x', finestre=fin, laterali=lat, porta_x=0.0, porta_w=1.0,
                 porta_tinta='#2f4a3a', vetro_porta=True, comignoli=[(2.4, -5.5)])
    insegna5(m, 'BAR SPORT', 0, 3.2, 3.9, 0.04, 5.2, '#2a1d14', '#1a120c', '#ffb347', e=1, h=0.45)
    tenda(m, -3.4, 3.4, 2.55, 1.3, '#24467a', '#e6e0d0', 10)
    return m


def bottega():
    """Esterno a due piani del negozio (interno x 0..6, z 0..12). 6,9 x 12,9, colmo a 6,3 (+2,2 di tetto a falde con colmo lungo z).
    Il muro e' 25 cm fuori dalle pareti interne. Varco della porta (guscio v4 del negozio: x mondo 2.5..3.57, largo 1,07) a x_modello in [-0.5, 0.57]: con il fronte verso -z (yaw 180) e lo
    specchio X di Unity cade a x mondo 2.5..3.57 se il pivot e' a (3, 0, -0.45). Il varco e' quasi centrato: lo specchio X conta poco."""
    m = Mod('bottega')
    hw, D, Ht, T, rh = 3.45, 12.9, 6.3, 0.25, 2.2
    I, Z = '#e0b87a', '#8d867a'
    g0, g1 = -0.5, 0.57   # varco della porta
    m.add('intonaco', I, *caja(-hw, 0, -D, -hw + T, Ht, 0))
    m.add('intonaco', I, *caja(hw - T, 0, -D, hw, Ht, 0))
    m.add('intonaco', I, *caja(-hw + T, 0, -D, hw - T, Ht, -D + T))
    m.add('intonaco', I, *caja(-hw + T, 0, -T, g0, Ht, 0))
    m.add('intonaco', I, *caja(g1, 0, -T, hw - T, Ht, 0))
    m.add('intonaco', I, *caja(g0, 2.2, -T, g1, Ht, 0))
    # zoccolo in pietra, interrotto dal varco
    for (x0, x1, z0, z1) in ((-hw - 0.03, g0, -0.25, 0.03), (g1, hw + 0.03, -0.25, 0.03), (-hw - 0.03, -hw, -D - 0.03, -0.25), (hw, hw + 0.03, -D - 0.03, -0.25), (-hw, hw, -D - 0.03, -D)):
        m.add('pietra', Z, *caja(x0, 0, z0, x1, 0.9, z1))
    for (x0, x1, z0, z1) in ((-hw - 0.05, g0, -0.3, 0.05), (g1, hw + 0.05, -0.3, 0.05), (-hw - 0.05, -hw, -D - 0.05, -0.3), (hw, hw + 0.05, -D - 0.05, -0.3),
                             (-hw, hw, -D - 0.05, -D)):
        m.add('pietra', Z, *caja(x0, 0.9, z0, x1, 0.96, z1))   # cornice sopra lo zoccolo
    # porta: stipiti, architrave, gradino
    for (x0, x1) in ((g0 - 0.12, g0), (g1, g1 + 0.12)):
        m.add('legno', '#3b2a20', *caja(x0, 0, -0.05, x1, 2.34, 0.1))
    m.add('legno', '#3b2a20', *caja(g0 - 0.12, 2.2, -0.05, g1 + 0.12, 2.34, 0.1))
    m.add('pietra', '#9a9484', *caja(g0 - 0.15, 0, 0.0, g1 + 0.15, 0.1, 0.6))
    # vetrine accese ai lati della porta (x modello = -x di C#: la stretta a sinistra, la larga a destra)
    m.ins(finestra('bottega_v0', 1.0, 1.5, persiane=None, luce='#f4ebc8', telaio='#e8e2d0'), -2.45, 0.65, 0)
    m.ins(finestra('bottega_v1', 1.6, 1.5, persiane=None, luce='#f4ebc8', telaio='#e8e2d0'), 2.15, 0.65, 0)
    for x, w in ((-2.45, 1.0), (2.15, 1.6)):   # merce sul ripiano
        for i, c in enumerate(('#b24a4a', '#4f8a3a', '#d9b64a', '#3f6fb5')):
            if i * 0.3 + 0.2 < w:
                m.add('stampa_b' if i % 2 else 'cartone', c, *caja(x - w / 2 + 0.12 + i * 0.3, 1.0, 0.01, x - w / 2 + 0.3 + i * 0.3, 1.24, 0.14))
    # tendone, insegna, cartello APERTO, targa del campanello, cassetta della posta
    tenda(m, -hw + 0.2, hw - 0.2, 2.65, 1.2, '#33663f', '#e6e0d0', 10)
    insegna5(m, 'ALIMENTARI', 0, 3.3, 3.95, 0.04, 4.6, '#d6e0c4', '#25402f', '#2d6a3a')
    m.add('legno', '#5a1a1a', *caja(-1.73, 1.42, 0.0, -1.15, 1.66, 0.04))
    scritta(m, 'APERTO', -1.44, 1.5, 0.04, 0.09, '#f4f1e6')
    m.add('liscio', '#4ce37a', *caja(-1.7, 1.38, 0.0, -1.18, 1.4, 0.03), e=1)
    m.add('metallo', '#b88c33', *caja(0.78, 1.22, 0.0, 0.9, 1.56, 0.04))
    m.add('legno', '#a02a2a', *caja(0.96, 1.1, 0.0, 1.2, 1.38, 0.14))
    # piano di sopra: tre finestre con cassette di fiori, due per lato, tetto a falde
    for x in (-2.4, 0.03, 2.45):
        m.ins(finestra('bottega_p%d' % int(x * 100), 0.9, 1.2, persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False, cassetta=True), x, 4.4, 0)
    for k, z in enumerate((-2.5, -6.45, -10.4)):
        for j, yb in enumerate((1.2, 4.4)):
            m.ins(finestra('bottega_sd%d%d' % (k, j), 1.0, 1.3, persiane='#4b6a50', luce=SPENTO if yb > 2 else '#f0c070', e=0 if yb > 2 else 1, lamelle=False), hw, yb, z, rot=90)
            m.ins(finestra('bottega_ss%d%d' % (k, j), 1.0, 1.3, persiane='#4b6a50', luce=SPENTO, e=0, lamelle=False), -hw, yb, z, rot=-90)
    tetto_a_falde(m, -hw - 0.35, -D - 0.35, hw + 0.35, 0.35, Ht - 0.08, Ht + rh, 'z', '#804033', '#5a2d26', timpano=('intonaco', I, 0.36))
    zb = -D - 0.36
    m.add('intonaco', I, *piano((-hw - 0.05, Ht - 0.03, zb), (hw + 0.05, Ht - 0.03, zb), (0, Ht + rh - 0.12, zb), (0, Ht + rh - 0.12, zb), (0, Ht, zb - 5)))
    m.add('vetro', '#f0c070', *caja(-0.28, Ht + 0.55, 0.362, 0.28, Ht + 1.15, 0.37), e=1)   # lucernario nel frontone
    m.add('legno', '#e8e2d0', *caja(-0.34, Ht + 0.5, 0.36, 0.34, Ht + 0.55, 0.385))
    m.add('legno', '#e8e2d0', *caja(-0.34, Ht + 1.15, 0.36, 0.34, Ht + 1.2, 0.385))
    comignolo(m, 1.6, Ht + 0.9, -9.0, 1.4)
    # antenna TV anni 90 sul colmo
    m.add('metallo', '#52575c', *tubo((0, Ht + rh - 0.1, -4.0), (0, Ht + rh + 2.2, -4.0), 0.025, 0.02, 5))
    for i in range(3):
        m.add('metallo', '#52575c', *caja(-0.5 + i * 0.12, Ht + rh + 1.9 + i * 0.35 - 0.02, -4.02, 0.5 - i * 0.12, Ht + rh + 1.9 + i * 0.35 + 0.02, -3.98))
    # bici appoggiata a destra della vetrina larga (ruote nel piano xy, contro il muro)
    bx = hw - 1.35
    for wx in (-0.45, 0.45):
        m.add('gomma', '#1b1b1b', *tubo((bx + wx, 0.34, 0.5), (bx + wx, 0.34, 0.54), 0.34, 0.34, 16))
        m.add('metallo', '#c8cdd0', *tubo((bx + wx, 0.34, 0.495), (bx + wx, 0.34, 0.545), 0.28, 0.28, 16))
    for (a, b) in (((-0.45, 0.34), (-0.02, 0.32)), ((-0.02, 0.32), (-0.2, 0.82)), ((-0.2, 0.82), (0.33, 0.78)), ((-0.02, 0.32), (0.33, 0.78)),
                   ((0.33, 0.78), (0.45, 0.34)), ((-0.45, 0.34), (-0.2, 0.82)), ((0.33, 0.78), (0.36, 0.95))):
        m.add('metallo', '#26667f', *tubo((bx + a[0], a[1], 0.52), (bx + b[0], b[1], 0.52), 0.018, 0.018, 5))
    m.add('gomma', '#1b1b1b', *caja(bx - 0.34, 0.84, 0.44, bx - 0.1, 0.89, 0.6))
    m.add('gomma', '#1b1b1b', *caja(bx + 0.26, 0.95, 0.4, bx + 0.46, 0.99, 0.64))
    return m


# --------------------------------------------------------------------------- arredo
def monumento():
    """Monumento ai caduti: tre gradini (3,4 m alla base), basamento, obelisco con targa 'CADUTI', punta a piramide. Alto 5,3 m."""
    m = Mod('monumento')
    for i in range(3):
        s = 3.4 - i * 0.6
        m.add('pietra', '#b3ada0', *caja(-s / 2, i * 0.3, -s / 2, s / 2, (i + 1) * 0.3, s / 2))
    m.add('pietra', '#a8a290', *caja(-0.7, 0.9, -0.7, 0.7, 1.7, 0.7))
    m.add('pietra', '#b8b2a0', *caja(-0.78, 1.7, -0.78, 0.78, 1.82, 0.78))
    m.add('pietra', '#b3ada0', *tronco(0, 0, 1.82, 4.7, 0.95, 0.95, 0.55, 0.55))
    m.add('pietra', '#b8b2a0', *tronco(0, 0, 4.7, 5.3, 0.55, 0.55, 0.04, 0.04))
    m.add('metallo', '#3a3d40', *caja(-0.45, 1.05, 0.7, 0.45, 1.5, 0.73))
    scritta(m, 'CADUTI', 0, 1.2, 0.73, 0.1, '#c9a227')
    return m


def utilitaria():
    """Utilitaria anni 90 (1,55 x 3,6 x 1,45), asse lungo = z, muso a +z."""
    m = Mod('utilitaria')
    B = '#8c9980'
    m.add('liscio', B, *tronco(0, 0, 0.28, 0.86, 1.55, 3.6, 1.5, 3.5))
    cab = (-0.15, 0.86, 1.42, 1.46, 1.9, 1.3, 1.35)   # cz, y0, y1, w0, d0, w1, d1
    cz, y0, y1, w0, d0, w1, d1 = cab
    m.add('liscio', B, *tronco(0, cz, y0, y1, w0, d0, w1, d1))

    def vetro_su(p0, p1, p2, p3, verso, k=0.1):
        """Quadrato di vetro su una faccia della cabina (angoli in senso orario), rientrato di k, 4 mm fuori."""
        def lerp(a, b, t): return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
        q = [lerp(lerp(p0, p1, k), lerp(p3, p2, k), k), lerp(lerp(p0, p1, 1 - k), lerp(p3, p2, 1 - k), k),
             lerp(lerp(p0, p1, 1 - k), lerp(p3, p2, 1 - k), 1 - k), lerp(lerp(p0, p1, k), lerp(p3, p2, k), 1 - k)]
        n = (verso[0] * 0.004, verso[1] * 0.004, verso[2] * 0.004)
        q = [(a[0] + n[0], a[1] + n[1], a[2] + n[2]) for a in q]
        m.add('vetro', '#1d2830', *piano(*q, (q[0][0] + verso[0], q[0][1] + verso[1], q[0][2] + verso[2])))

    f0, f1 = cz + d0 / 2, cz + d1 / 2   # z davanti in basso / in alto
    b0, b1 = cz - d0 / 2, cz - d1 / 2
    vetro_su((-w0 / 2, y0, f0), (w0 / 2, y0, f0), (w1 / 2, y1, f1), (-w1 / 2, y1, f1), (0, 0.4, 1), 0.08)             # parabrezza
    vetro_su((w0 / 2, y0, b0), (-w0 / 2, y0, b0), (-w1 / 2, y1, b1), (w1 / 2, y1, b1), (0, 0.4, -1), 0.1)            # lunotto
    def P(sx, t, k):   # punto sulla faccia laterale: t lungo z (0 davanti), k in altezza (0 basso)
        w = w0 + (w1 - w0) * k
        z = (f0 + (f1 - f0) * k) + ((b0 + (b1 - b0) * k) - (f0 + (f1 - f0) * k)) * t
        return (sx * w / 2, y0 + (y1 - y0) * k, z)
    for sx in (-1, 1):   # due finestrini per lato, separati dal montante centrale
        for (t0, t1) in ((0.06, 0.47), (0.53, 0.94)):
            vetro_su(P(sx, t0, 0), P(sx, t1, 0), P(sx, t1, 1), P(sx, t0, 1), (sx, 0.3, 0), 0.06)
    # fari, fanali, paraurti, targhe, ruote, specchietti
    for s in (-1, 1):
        m.add('liscio', '#f4ead0', *caja(s * 0.5 - 0.15, 0.58, 1.79, s * 0.5 + 0.15, 0.78, 1.82), e=1)
        m.add('liscio', '#b02a2a', *caja(s * 0.5 - 0.15, 0.6, -1.82, s * 0.5 + 0.15, 0.76, -1.79))
        m.add('liscio', '#3a3d40', *caja(s * 0.88 - 0.05, 0.9, 0.75, s * 0.88 + 0.05, 1.0, 0.95))
    m.add('gomma', '#2a2d30', *caja(-0.78, 0.28, 1.78, 0.78, 0.5, 1.9))
    m.add('gomma', '#2a2d30', *caja(-0.78, 0.28, -1.9, 0.78, 0.5, -1.78))
    m.add('metallo', '#1a1c1e', *caja(-0.3, 0.52, 1.8, 0.3, 0.62, 1.82))
    m.add('liscio', '#e6e0d0', *caja(-0.22, 0.3, 1.9, 0.22, 0.4, 1.915))
    m.add('liscio', '#e6e0d0', *caja(-0.22, 0.3, -1.915, 0.22, 0.4, -1.9))
    for sx in (-1, 1):
        for z in (1.15, -1.1):
            m.add('gomma', '#1b1b1b', *tubo((sx * 0.7, 0.3, z), (sx * 0.84, 0.3, z), 0.3, 0.3, 14))
            m.add('metallo', '#b8bcbf', *tubo((sx * 0.83, 0.3, z), (sx * 0.86, 0.3, z), 0.17, 0.17, 10))
        m.add('liscio', '#4f554a', *caja(sx * 0.78 - 0.004, 0.45, -0.62, sx * 0.78 + 0.004, 1.3, -0.6))   # fessura portiera
        m.add('liscio', '#4f554a', *caja(sx * 0.78 - 0.004, 0.45, 0.48, sx * 0.78 + 0.004, 1.3, 0.5))
    return m


def cassonetto():
    """Cassonetto verde con coperchio e due ruote (1,1 x 1,2 x 0,9), fronte a +z."""
    m = Mod('cassonetto')
    m.add('metallo', '#3a7a45', *tronco(0, 0, 0.12, 1.0, 1.1, 0.9, 1.1, 0.9))
    for y in (0.4, 0.7):
        m.add('metallo', '#2f6639', *caja(-0.56, y, -0.46, 0.56, y + 0.03, 0.46))
    m.add('gomma', '#2a2d30', *obliquo(-0.58, 1.0, -0.47, 0.58, 1.18, 0.47, 0, -0.2))
    m.add('gomma', '#2a2d30', *caja(-0.3, 1.0, 0.47, 0.3, 1.06, 0.55))
    for s in (-1, 1):
        m.add('gomma', '#1b1b1b', *tubo((s * 0.4, 0.1, -0.3), (s * 0.4 + 0.001, 0.1, -0.3), 0.1, 0.1, 10))
        m.add('gomma', '#1b1b1b', *tubo((s * 0.4, 0.1, 0.3), (s * 0.4 + 0.001, 0.1, 0.3), 0.1, 0.1, 10))
    m.add('liscio', '#e6e0d0', *caja(-0.25, 0.55, 0.449, 0.25, 0.85, 0.46))   # etichetta
    return m


def bacheca():
    """Bacheca del paese: due pali, pannello di sughero, tettuccio, scritta AVVISI, manifesti. 2,5 x 2,3 x 0,35, fronte +z."""
    m = Mod('bacheca')
    for s in (-1, 1):
        m.add('legno', '#4a3326', *caja(s * 1.0 - 0.05, 0, -0.05, s * 1.0 + 0.05, 2.0, 0.05))
    m.add('legno', '#6b4a2f', *caja(-1.15, 0.85, -0.04, 1.15, 2.05, 0.08))
    m.add('legno', '#b08a58', *caja(-1.05, 0.95, 0.08, 1.05, 1.95, 0.1))
    m.add('legno', '#4a3326', *tetto(-1.25, -0.2, 1.25, 0.35, 2.05, 2.3, 'x'))
    scritta(m, 'AVVISI', 0, 1.78, 0.1, 0.12, '#2a241c')
    pc = ['#efe8d0', '#d6b85a', '#b8473a', '#cfd2d6', '#8aa6c9', '#efe8d0']
    for i in range(6):
        x0, y0 = -0.95 + (i % 3) * 0.65, 1.0 + (i // 3) * 0.42
        skew = (-0.03, 0.02, 0.035, -0.02, 0.025, -0.035)[i]
        m.add('stampa_a' if i % 2 else 'stampa_b', pc[i], *obliquo(x0, y0, 0.1, x0 + 0.5, y0 + 0.36, 0.112, skew, 0))
        m.add('metallo', '#c9a227', *esfera(x0 + 0.25 + skew, y0 + 0.34, 0.116, 0.014, 0.014, 0.01, 4, 3))
    return m


def fioriera():
    """Fioriera di pietra con cespugli fioriti (1,1 x 0,8 x 0,6), fronte +z."""
    m = Mod('fioriera')
    m.add('pietra', '#8d867a', *caja(-0.55, 0, -0.275, 0.55, 0.5, 0.275))
    m.add('pietra', '#a09a8a', *caja(-0.58, 0.46, -0.3, 0.58, 0.52, 0.3))
    m.add('terra', '#6b4a35', *caja(-0.5, 0.5, -0.24, 0.5, 0.54, 0.24))
    for i in range(5):
        x = -0.4 + i * 0.2
        m.add('erba', '#4f8a3a', *esfera(x, 0.62, 0.0, 0.14, 0.13, 0.17, 6, 4))
        m.add('liscio', ('#d94a5a', '#f2d21b', '#e8e4f0')[i % 3], *esfera(x + 0.03, 0.76, 0.06 * (-1) ** i, 0.045, 0.045, 0.045, 5, 3))
    return m


def tavolino_osteria():
    """Tavolino tondo con tovaglia, bottiglia e bicchiere, due sgabelli a +-0,62 m su x. 1,6 x 0,8 x 0,4."""
    m = Mod('tavolino_osteria')
    m.add('metallo', '#3a3d40', *prisma(0, 0, 0, 0.72, 0.035, 0.03, 8))
    m.add('metallo', '#3a3d40', *prisma(0, 0, 0, 0.03, 0.22, 0.22, 10))
    m.add('legno', '#8a6a44', *prisma(0, 0, 0.72, 0.76, 0.42, 0.42, 14))
    m.add('tessuto', '#b33a3a', *prisma(0, 0, 0.76, 0.765, 0.4, 0.4, 14))
    m.add('vetro', '#3f7a3a', *prisma(0.1, 0.05, 0.765, 0.97, 0.035, 0.03, 8))
    m.add('vetro', '#3f7a3a', *prisma(0.1, 0.05, 0.97, 1.02, 0.014, 0.012, 6))
    m.add('vetro', '#d8e4e0', *prisma(-0.12, -0.08, 0.765, 0.84, 0.03, 0.04, 8))
    for sx in (-1, 1):
        x = sx * 0.62
        m.add('legno', '#5a4636', *prisma(x, 0, 0.42, 0.46, 0.2, 0.2, 10))
        for dx, dz in ((0.12, 0.12), (-0.12, 0.12), (0.12, -0.12), (-0.12, -0.12)):
            m.add('legno', '#4a3a2c', *caja(x + dx - 0.02, 0, dz - 0.02, x + dx + 0.02, 0.42, dz + 0.02))
    return m


def pavimento_piazza():
    """Selciato della piazza: origine al centro (x -21..21, z -12..12 = mondo x -16..26, z -24..0), lastre 2,8 m di quattro grigi su fughe,
    piu i due marciapiedi (nord z 12.2..13.8, sud z -13.6..-12.2) con la faccia alta a y=0."""
    m = Mod('pavimento_piazza')
    up = (0, 5, 0)
    tinte = ['#8a867a', '#7f7b70', '#918d82', '#74716a']
    for i in range(14):
        for j in range(8):
            cx, cz = -14.3 + 3 * i - 5, -1.5 - 3 * j + 12
            t = tinte[(i * 7 + j * 3 + (i * j) % 5) % 4]
            m.add('selciato', t, *lastra(cx - 1.4, 0.015, cz - 1.4, cx + 1.4, 0.015, cz + 1.4, up))
    m.add('pietra', '#a8a496', *caja(-21, -0.5, 12.2, 21, 0.0, 13.8))
    m.add('pietra', '#a8a496', *caja(-21, -0.5, -13.6, 21, 0.0, -12.2))
    return m


def tutto():
    macelleria(); fioraio(); tabacchi(); osteria(); canonica(); casa_piazza(); bar_piazza(); bottega()
    for f in (monumento, utilitaria, cassonetto, bacheca, fioriera, tavolino_osteria, pavimento_piazza):
        f()
