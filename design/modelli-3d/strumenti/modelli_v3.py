"""Modelli v2 - prodotti, arredi del negozio, personaggi. Ogni modello è in metri, Y su, fronte verso +Z."""
import math
import random

from geo import (Mod, REG, caja, esfera, lastra, obliquo, prisma, tetto, tronco, tubo)

NATURALI = ['pane', 'formaggio', 'uova', 'mela', 'latte', 'salame', 'miele', 'verdura']
INDUSTRIALI = ['patatine', 'lattina', 'merendina', 'caramelle', 'birra', 'sigarette', 'surgelati', 'lattina_blu']
PASSO = {'pane': 0.38, 'formaggio': 0.3, 'uova': 0.26, 'mela': 0.3, 'latte': 0.13, 'salame': 0.36, 'miele': 0.14,
         'verdura': 0.2, 'patatine': 0.26, 'lattina': 0.09, 'merendina': 0.2, 'caramelle': 0.24, 'birra': 0.09,
         'sigarette': 0.11, 'surgelati': 0.25, 'lattina_blu': 0.09}


def prodotto(tipo):
    """Un prodotto, base a y=0 e centro su x=z=0."""
    k = 'p_' + tipo
    if k in REG:
        return REG[k]
    m = Mod(k)
    a = m.add
    if tipo == 'pane':
        a('crosta', '#c98a45', *esfera(0, 0.058, 0, 0.17, 0.062, 0.075, 10, 6))
        for dx in (-0.09, -0.03, 0.03, 0.09):
            a('crosta', '#efd9a8', *obliquo(dx - 0.012, 0.085, -0.04, dx + 0.012, 0.118, 0.04, 0.02, 0))
    elif tipo == 'formaggio':
        a('crosta', '#d9b64a', *prisma(0, 0, 0, 0.11, 0.13, 0.13, 14))
        a('crosta', '#a8812c', *prisma(0, 0, 0.09, 0.112, 0.137, 0.137, 14))
        a('liscio', '#f0d878', *prisma(0, 0, 0.11, 0.113, 0.12, 0.12, 14))
    elif tipo == 'uova':
        a('cartone', '#b8a07a', *caja(-0.11, 0, -0.06, 0.11, 0.045, 0.06))
        a('cartone', '#c4ac84', *obliquo(-0.11, 0.045, 0.055, 0.11, 0.17, 0.06, 0, -0.09))
        for i in range(3):
            for j in range(2):
                a('liscio', '#f1e6cf', *esfera(-0.07 + i * 0.07, 0.065, -0.03 + j * 0.06, 0.026, 0.032, 0.026, 6, 4))
    elif tipo == 'mela':
        a('cartone', '#b8a07a', *caja(-0.13, 0, -0.08, 0.13, 0.03, 0.08))
        for i, (x, z) in enumerate([(-0.07, -0.03), (0.0, 0.04), (0.07, -0.03), (-0.02, -0.04)]):
            a('pelle', '#a52b24' if i != 1 else '#c5a02e', *esfera(x, 0.075, z, 0.052, 0.05, 0.052, 7, 5))
            a('corteccia', '#4a3320', *tubo((x, 0.12, z), (x + 0.005, 0.145, z), 0.005, 0.004, 5))
    elif tipo == 'latte':
        a('stampa_a', '#e8eef5', *caja(-0.045, 0, -0.045, 0.045, 0.17, 0.045))
        a('stampa_a', '#e8eef5', *tetto(-0.045, -0.045, 0.045, 0.045, 0.17, 0.215, 'z'))
        a('liscio', '#3f6fb5', *caja(-0.0465, 0.075, -0.0465, 0.0465, 0.125, 0.0465))
    elif tipo == 'salame':
        a('crosta', '#7a3b2e', *tubo((-0.15, 0.035, 0), (0.15, 0.035, 0), 0.035, 0.035, 10))
        a('crosta', '#7a3b2e', *esfera(-0.15, 0.035, 0, 0.012, 0.035, 0.035, 6, 4))
        for x in (-0.1, 0.0, 0.1):
            a('liscio', '#d8c9a0', *tubo((x - 0.004, 0.035, 0), (x + 0.004, 0.035, 0), 0.039, 0.039, 10))
    elif tipo == 'miele':
        a('vetro', '#d99a1e', *prisma(0, 0, 0, 0.1, 0.052, 0.052, 12))
        a('stampa_a', '#f1e4b8', *prisma(0, 0, 0.025, 0.075, 0.0535, 0.0535, 12))
        a('metallo', '#c9a227', *prisma(0, 0, 0.1, 0.12, 0.056, 0.056, 12))
    elif tipo == 'verdura':
        a('erba', '#5a8a35', *esfera(0, 0.07, 0, 0.082, 0.065, 0.082, 8, 5))
        a('erba', '#7fae4a', *esfera(0, 0.085, 0, 0.06, 0.06, 0.06, 7, 4))
    elif tipo == 'patatine':
        a('stampa_b', '#d6279a', *tronco(0, 0, 0, 0.26, 0.19, 0.05, 0.2, 0.07))
        a('stampa_b', '#d6279a', *caja(-0.1, 0.26, -0.02, 0.1, 0.29, 0.02))
        for x in (-0.08, -0.04, 0.0, 0.04, 0.08):
            a('metallo', '#e8e8e8', *caja(x - 0.006, 0.26, -0.0215, x + 0.006, 0.29, 0.0215))
        a('stampa_a', '#f2d21b', *caja(-0.07, 0.07, 0.033, 0.07, 0.17, 0.04))
    elif tipo in ('lattina', 'lattina_blu'):
        t = '#b8322a' if tipo == 'lattina' else '#2f6fcf'
        a('stampa_a', t, *tubo((0, 0, 0), (0, 0.118, 0), 0.033, 0.033, 12))
        a('metallo', '#c8ccd0', *prisma(0, 0, 0.118, 0.126, 0.03, 0.028, 12))
        a('metallo', '#c8ccd0', *prisma(0, 0, 0, 0.004, 0.031, 0.031, 12))
    elif tipo == 'merendina':
        a('stampa_b', '#2e86de', *caja(-0.07, 0, -0.025, 0.07, 0.11, 0.025))
        for sx in (-1, 1):
            a('stampa_b', '#2e86de', *caja(sx * 0.07 - (0.015 if sx < 0 else 0), 0.012, -0.016, sx * 0.07 + (0.015 if sx > 0 else 0), 0.098, 0.016))
        a('stampa_a', '#ffd23f', *caja(-0.05, 0.04, 0.025, 0.05, 0.075, 0.029))
    elif tipo == 'caramelle':
        a('cartone', '#c28b3a', *caja(-0.1, 0, -0.07, 0.1, 0.02, 0.07))
        for sx, sz, w, d in [(-0.1, -0.07, 0.01, 0.14), (0.09, -0.07, 0.01, 0.14), (-0.1, -0.07, 0.2, 0.01), (-0.1, 0.06, 0.2, 0.01)]:
            a('cartone', '#c28b3a', *caja(sx, 0, sz, sx + w, 0.07, sz + d))
        cols = ['#ff7eb6', '#ffd23f', '#8be0ff', '#ff6a3d', '#b6f06a']
        rng = random.Random(3)
        for i in range(16):
            a('liscio', cols[i % 5], *esfera(rng.uniform(-0.08, 0.08), 0.045 + rng.uniform(0, 0.025), rng.uniform(-0.05, 0.05), 0.02, 0.016, 0.02, 5, 4))
    elif tipo == 'birra':
        a('vetro', '#2f6b3a', *tubo((0, 0, 0), (0, 0.13, 0), 0.033, 0.033, 12))
        a('vetro', '#2f6b3a', *tubo((0, 0.13, 0), (0, 0.19, 0), 0.033, 0.014, 12))
        a('vetro', '#2f6b3a', *tubo((0, 0.19, 0), (0, 0.25, 0), 0.014, 0.014, 8))
        a('stampa_a', '#efe6c8', *tubo((0, 0.04, 0), (0, 0.1, 0), 0.0345, 0.0345, 12))
        a('metallo', '#c9a227', *prisma(0, 0, 0.248, 0.262, 0.017, 0.017, 8))
    elif tipo == 'sigarette':
        a('stampa_a', '#d12e2e', *caja(-0.04, 0, -0.015, 0.04, 0.09, 0.015))
        a('stampa_a', '#eeeeee', *caja(-0.0405, 0.06, -0.0155, 0.0405, 0.09, 0.0155))
    elif tipo == 'surgelati':
        a('stampa_b', '#3aa0ff', *caja(-0.1, 0, -0.03, 0.1, 0.2, 0.03))
        a('stampa_a', '#ffffff', *caja(-0.06, 0.05, 0.03, 0.06, 0.15, 0.034))
    else:
        raise ValueError(tipo)
    return m


def scaffale(nome, livelli, w=1.5, d=0.45, h=2.0, cartello=None, seme=1):
    """Dorso a z=0, fronte verso +z. 'livelli': tipo di prodotto per ripiano, dal basso."""
    m = Mod(nome)
    rng = random.Random(seme)
    for x in (0.0, w - 0.04):
        for z in (0.0, d - 0.04):
            m.add('metallo', '#7a8288', *caja(x, 0, z, x + 0.04, h, z + 0.04))
    m.add('metallo', '#4f565c', *caja(0, 0, 0, w, h, 0.02))
    for y in (0.3, 0.75, 1.2, 1.65):
        m.add('metallo', '#454b51', *caja(0.04, y, 0.02, w - 0.04, y + 0.02, 0.03))
    m.add('metallo', '#a8afb3', *caja(0, h - 0.04, 0, w, h, d))
    m.add('metallo', '#2f3438', *caja(0, 0, 0.02, w, 0.1, d - 0.03))
    ys = [y for y in (0.15, 0.6, 1.05, 1.5) if y < h - 0.2]
    for y, tipo in zip(ys, livelli):
        m.add('metallo', '#a8afb3', *caja(0, y - 0.03, 0, w, y, d))
        m.add('liscio', '#e8e4d4', *caja(0, y - 0.05, d - 0.004, w, y, d + 0.012))
        for i in range(int((w - 0.1) / 0.3)):
            m.add('stampa_a', rng.choice(['#ffe14a', '#f1f1f1', '#ff6a3d']), *caja(0.1 + i * 0.3, y - 0.045, d + 0.012, 0.18 + i * 0.3, y - 0.012, d + 0.017))
        if not tipo:
            continue
        p = prodotto(tipo)
        x = 0.03 + PASSO[tipo] / 2
        while x < w - 0.05 - PASSO[tipo] / 2 + 1e-6:
            for z in (0.13, 0.31):
                m.ins(p, x, y, z, rot=rng.uniform(-9, 9))
            x += PASSO[tipo]
    if cartello:
        m.add('stampa_a', cartello, *caja(0.1, h + 0.02, 0.08, w - 0.1, h + 0.32, 0.12))
        m.add('metallo', '#7a8288', *caja(0.1, h, 0.1, 0.14, h + 0.02, 0.12))
    return m


def cassa():
    m = Mod('cassa')
    m.add('legno', '#5f4128', *caja(0, 0, 0, 0.8, 0.92, 1.6))
    for z0, z1 in ((0.1, 0.75), (0.85, 1.5)):
        m.add('legno', '#7a5a38', *caja(-0.012, 0.12, z0, 0.0, 0.8, z1))
    m.add('metallo', '#c8cdd0', *caja(-0.05, 0.92, -0.05, 0.85, 0.95, 1.65))
    m.add('gomma', '#222426', *caja(0.05, 0.95, 0.75, 0.7, 0.955, 1.1))
    m.add('metallo', '#5a5f64', *caja(0.35, 0.95, 0.12, 0.76, 1.08, 0.52))
    m.add('metallo', '#3d4146', *obliquo(0.35, 1.08, 0.12, 0.56, 1.16, 0.52, 0.12, 0))
    for i in range(4):
        for j in range(3):
            m.add('liscio', ['#e8e4d4', '#c2c2c2', '#d9534f'][j], *caja(0.37 + j * 0.05, 1.09 + i * 0.012 + 0.0, 0.16 + i * 0.09, 0.405 + j * 0.05, 1.105 + i * 0.012, 0.2 + i * 0.09))
    m.add('metallo', '#3d4146', *caja(0.58, 1.08, 0.2, 0.72, 1.3, 0.44))
    m.add('liscio', '#3bbf6a', *caja(0.575, 1.14, 0.24, 0.58, 1.26, 0.4), e=2)
    m.add('stampa_a', '#f4f1e6', *tubo((0.66, 1.34, 0.32), (0.66, 1.34, 0.4), 0.04, 0.04, 12))
    for i, c in enumerate(['#ff7eb6', '#ffd23f', '#8be0ff', '#ff6a3d', '#b6f06a', '#ff7eb6']):
        z = 1.2 + 0.07 * (i % 3)
        x = 0.08 + 0.09 * (i // 3)
        m.add('legno', '#c9b38a', *tubo((x, 0.95, z), (x, 1.13, z), 0.004, 0.004, 4))
        m.add('liscio', c, *esfera(x, 1.15, z, 0.032, 0.032, 0.008, 8, 5))
    m.add('legno', '#7a5a38', *prisma(0.14, 1.3, 0.95, 0.99, 0.2, 0.2, 10))
    return m


def banco_affettati():
    """Corre lungo z (2.5 m). Lato clienti x=0, dietro il banco x=0.9. Da z=0: vetrina, prosciutto su morsa, bilancia, affettatrice."""
    m = Mod('banco_affettati')
    a = m.add
    zc = 1.12   # fine della vetrina
    a('piastrelle_muro', '#d8d2c0', *caja(0, 0.1, 0, 0.9, 0.85, 2.5))
    a('metallo', '#8d9499', *caja(-0.02, 0, -0.02, 0.92, 0.1, 2.52))
    a('metallo', '#c8cdd0', *caja(-0.03, 0.85, -0.03, 0.93, 0.89, 2.53))
    for z in (0.5, 1.0, 1.5, 2.0):
        a('metallo', '#a9afb3', *caja(-0.005, 0.2, z - 0.01, 0.0, 0.8, z + 0.01))
    # vetrina: piano, montanti, telaio, luce, vassoi con carta oleata
    a('metallo', '#e2e5e7', *caja(0.05, 0.89, 0.03, 0.85, 0.9, zc))
    for x in (0.04, 0.84):
        for z in (0.03, zc):
            a('metallo', '#9aa1a6', *caja(x - 0.012, 0.9, z - 0.012, x + 0.012, 1.42, z + 0.012))
    a('metallo', '#9aa1a6', *caja(0.02, 1.42, 0.0, 0.88, 1.46, zc + 0.03))
    a('metallo', '#9aa1a6', *caja(0.03, 1.1, 0.03, 0.045, 1.115, zc))
    a('metallo', '#9aa1a6', *caja(0.03, 0.9, 0.0, 0.05, 0.96, zc + 0.02))
    a('liscio', '#fff0c8', *caja(0.12, 1.4, 0.1, 0.78, 1.42, zc - 0.05), e=2)
    for z in (0.12, 0.45, 0.78):
        a('metallo', '#cfd3d6', *caja(0.1, 0.9, z, 0.8, 0.915, z + 0.28))
        a('stampa_a', '#f4efd8', *caja(0.12, 0.915, z + 0.01, 0.78, 0.918, z + 0.27))
    for i, t in enumerate(['salame', 'formaggio', 'salame']):
        m.ins(prodotto(t), 0.25, 0.918, 0.26 + i * 0.33, rot=90)
    a('crosta', '#d98a8a', *tubo((0.5, 1.0, 0.14), (0.5, 1.0, 0.5), 0.09, 0.09, 14))
    a('liscio', '#e6a0a0', *tubo((0.5, 1.0, 0.5), (0.5, 1.0, 0.505), 0.088, 0.088, 14))
    for k in range(7):
        a('liscio', '#f2f0e4', *esfera(0.5 + 0.05 * math.cos(k), 1.0 + 0.05 * math.sin(k), 0.507, 0.012, 0.012, 0.004, 5, 3))
    m.ins(prodotto('formaggio'), 0.55, 0.918, 0.72, rot=0)
    m.ins(prodotto('formaggio'), 0.55, 1.03, 0.72, rot=20)
    a('liscio', '#d98a8a', *esfera(0.6, 0.97, 1.0, 0.1, 0.06, 0.1, 8, 5))
    for z in (0.28, 0.6, 0.92):
        a('stampa_a', '#ffe14a', *obliquo(0.06, 0.92, z, 0.14, 0.98, z + 0.002, 0, 0))
    # prosciutto su morsa, con coltello
    a('legno', '#6b4a2f', *caja(0.28, 0.89, 1.22, 0.82, 0.93, 1.7))
    for z in (1.3, 1.62):
        a('metallo', '#5a5f64', *caja(0.52, 0.93, z - 0.02, 0.58, 1.02, z + 0.02))
    a('metallo', '#5a5f64', *caja(0.45, 1.0, 1.32, 0.65, 1.03, 1.6))
    a('crosta', '#a4503f', *esfera(0.55, 1.14, 1.47, 0.12, 0.1, 0.27, 12, 7))
    a('liscio', '#c96a6a', *esfera(0.55, 1.14, 1.21, 0.1, 0.085, 0.018, 10, 4))
    a('liscio', '#ece4d0', *tubo((0.55, 1.14, 1.72), (0.55, 1.13, 1.8), 0.03, 0.022, 8))
    a('liscio', '#ece4d0', *esfera(0.55, 1.13, 1.81, 0.04, 0.035, 0.035, 6, 4))
    a('metallo', '#d6dade', *obliquo(0.74, 0.93, 1.28, 0.752, 0.945, 1.66, 0, 0))
    a('legno', '#3a2a1c', *caja(0.74, 0.93, 1.66, 0.76, 0.96, 1.76))
    # bilancia elettronica con piatto e display
    a('metallo', '#e8eaec', *caja(0.3, 0.89, 1.78, 0.82, 0.945, 2.02))
    a('metallo', '#e8eaec', *obliquo(0.3, 0.945, 1.78, 0.4, 1.26, 2.02, 0, 0))
    a('liscio', '#1b1d1f', *caja(0.304, 1.1, 1.8, 0.327, 1.24, 2.0))
    a('liscio', '#ff3b2f', *caja(0.326, 1.17, 1.84, 0.33, 1.22, 1.96), e=2)
    a('liscio', '#3bbf6a', *caja(0.326, 1.12, 1.84, 0.33, 1.15, 1.88), e=2)
    for k in range(4):
        a('liscio', '#c2c2c2', *caja(0.326, 0.97, 1.84 + k * 0.035, 0.332, 0.99, 1.866 + k * 0.035))
    a('metallo', '#d6dade', *tubo((0.58, 0.945, 1.9), (0.58, 0.96, 1.9), 0.15, 0.15, 24))
    a('metallo', '#9aa1a6', *tubo((0.58, 0.96, 1.9), (0.58, 0.965, 1.9), 0.155, 0.155, 24))
    # affettatrice: base, motore, lama con protezione, piatto carrello, spingitore, affilatoio, manopola spessore
    a('metallo', '#cfd3d6', *caja(0.26, 0.89, 2.06, 0.86, 0.95, 2.49))
    for x, z in ((0.3, 2.1), (0.82, 2.1), (0.3, 2.45), (0.82, 2.45)):
        a('gomma', '#1b1b1b', *prisma(x, z, 0.875, 0.888, 0.03, 0.03, 8))
    a('metallo', '#d6dadd', *caja(0.55, 0.95, 2.12, 0.86, 1.2, 2.47))
    a('metallo', '#d6dadd', *esfera(0.705, 1.2, 2.295, 0.155, 0.08, 0.175, 12, 4))
    a('metallo', '#e6e9ec', *tubo((0.58, 1.09, 2.2), (0.58, 1.09, 2.222), 0.16, 0.16, 32))
    a('metallo', '#5a5f64', *tubo((0.58, 1.09, 2.222), (0.58, 1.09, 2.234), 0.166, 0.166, 32))
    a('metallo', '#3a3d40', *tubo((0.58, 1.09, 2.234), (0.58, 1.09, 2.26), 0.045, 0.045, 12))
    for g in range(6):
        t = g * math.pi / 3
        a('metallo', '#9aa1a6', *tubo((0.58 + 0.03 * math.cos(t), 1.09 + 0.03 * math.sin(t), 2.222), (0.58 + 0.15 * math.cos(t), 1.09 + 0.15 * math.sin(t), 2.222), 0.004, 0.004, 4))
    a('metallo', '#c8ccd0', *caja(0.36, 0.98, 2.19, 0.46, 1.27, 2.214))
    a('metallo', '#c8ccd0', *caja(0.34, 0.97, 2.07, 0.5, 0.995, 2.2))
    a('metallo', '#c8ccd0', *caja(0.34, 0.995, 2.07, 0.5, 1.12, 2.09))
    a('gomma', '#1b1b1b', *tubo((0.42, 1.12, 2.08), (0.42, 1.27, 2.08), 0.011, 0.011, 6))
    a('gomma', '#1b1b1b', *esfera(0.42, 1.285, 2.08, 0.026, 0.026, 0.026, 8, 5))
    a('metallo', '#9aa1a6', *caja(0.7, 1.2, 2.18, 0.8, 1.25, 2.34))
    a('gomma', '#1b1b1b', *tubo((0.75, 1.25, 2.26), (0.75, 1.3, 2.26), 0.018, 0.018, 8))
    a('gomma', '#1b1b1b', *tubo((0.24, 1.0, 2.34), (0.26, 1.0, 2.34), 0.045, 0.045, 14))
    a('liscio', '#d9534f', *caja(0.86, 1.0, 2.4, 0.875, 1.05, 2.44))
    a('liscio', '#3bbf6a', *caja(0.86, 1.0, 2.35, 0.875, 1.05, 2.38))
    a('metallo', '#e8eaec', *tubo((0.58, 0.95, 2.0), (0.58, 0.955, 2.0), 0.11, 0.11, 18))
    for k in range(5):
        a('liscio', '#d98a8a', *caja(0.52 + 0.012 * (k % 2), 0.955 + k * 0.004, 1.95 - 0.003 * k, 0.64 + 0.012 * (k % 2), 0.959 + k * 0.004, 1.99 - 0.003 * k))
    # carta oleata e coltelliera
    a('metallo', '#9aa1a6', *tubo((0.84, 1.0, 1.18), (0.84, 1.0, 1.5), 0.04, 0.04, 10))
    a('liscio', '#f4f1e6', *tubo((0.84, 1.0, 1.2), (0.84, 1.0, 1.48), 0.058, 0.058, 14))
    a('legno', '#6b4a2f', *caja(0.72, 0.895, 0.04, 0.84, 1.08, 0.2))
    for k in range(3):
        a('legno', '#2a1d14', *caja(0.74 + 0.03 * k, 1.08, 0.08, 0.76 + 0.03 * k, 1.13, 0.14))
    # prosciutti e salami appesi dietro il banco
    a('metallo', '#5a5f64', *caja(0.74, 2.24, 0.15, 0.78, 2.28, 2.4))
    for z in (0.3, 0.75, 1.3):
        a('metallo', '#5a5f64', *tubo((0.76, 2.24, z), (0.76, 2.08, z), 0.006, 0.006, 4))
        a('crosta', '#a4503f', *esfera(0.76, 1.95, z, 0.07, 0.15, 0.07, 8, 5))
    for z in (0.5, 0.52, 1.05, 1.07, 1.7, 1.72):
        a('metallo', '#5a5f64', *tubo((0.76, 2.24, z), (0.76, 2.12, z), 0.004, 0.004, 4))
        a('crosta', '#7a3b2e', *tubo((0.76, 2.12, z), (0.76, 1.8, z), 0.03, 0.03, 8))
    return m


def cestone(nome='cestone', frutta=('mela', 'verdura', 'mela')):
    m = Mod(nome)
    for y in (0.04, 0.16, 0.28):
        for z, o in ((0.0, 0.0), (0.58, 0.0)):
            m.add('legno', '#8a6a44', *caja(0, y, z, 0.8, y + 0.09, z + 0.02))
        for x in (0.0, 0.78):
            m.add('legno', '#8a6a44', *caja(x, y, 0.02, x + 0.02, y + 0.09, 0.58))
    for x in (0.0, 0.78):
        for z in (0.0, 0.58):
            m.add('legno', '#6b4a2f', *caja(x, 0, z, x + 0.02, 0.37, z + 0.02))
    m.add('legno', '#6b4a2f', *caja(0.02, 0.02, 0.02, 0.78, 0.05, 0.58))
    for i, t in enumerate(frutta):
        m.ins(prodotto(t), 0.2 + i * 0.2, 0.05, 0.3, rot=i * 40)
        m.ins(prodotto(t), 0.3 + i * 0.2, 0.05, 0.18, rot=i * 70)
    return m


def porta_vetro():
    """Anta vetrata con cerniera in x=0, apertura verso +x, altezza 2.1."""
    m = Mod('porta_vetro')
    w, h = 1.05, 2.1
    m.add('legno', '#4b6a50', *caja(0, 0, 0, 0.1, h, 0.05))
    m.add('legno', '#4b6a50', *caja(w - 0.1, 0, 0, w, h, 0.05))
    m.add('legno', '#4b6a50', *caja(0, 0, 0, w, 0.2, 0.05))
    m.add('legno', '#4b6a50', *caja(0, h - 0.12, 0, w, h, 0.05))
    m.add('legno', '#4b6a50', *caja(0, 0.9, 0.0, w, 0.96, 0.05))
    m.add('vetro', '#bcd6d8', *caja(0.1, 0.2, 0.015, w - 0.1, h - 0.12, 0.03), e=3)
    m.add('metallo', '#c8cdd0', *tubo((w - 0.14, 0.85, 0.07), (w - 0.14, 1.3, 0.07), 0.015, 0.015, 8))
    m.add('stampa_a', '#d93a2f', *caja(0.25, 1.5, 0.055, 0.75, 1.72, 0.06))
    return m


def negozio_interno():
    """Bottega 5 x 8 m, entrando dalla porta (z=0) la cassa è subito a sinistra (lato x=5)."""
    m = Mod('negozio_interno')
    C = (2.5, 1.5, 4.0)
    H = 3.0
    rng = random.Random(11)

    def par(mat, tint, x0, y0, z0, x1, y1, z1, e=0):
        m.add(mat, tint, *lastra(x0, y0, z0, x1, y1, z1, C), e=e)

    def fascia(x0, x1, y0, y1, z=None, x=None, z0=0.0, z1=0.0):
        """Parete con zoccolo di piastrelle fino a 1.5 m e intonaco sopra."""
        for (ya, yb, mat, tint) in ((y0, min(y1, 1.5), 'piastrelle_muro', '#d9d6c8'), (max(y0, 1.5), y1, 'intonaco', '#c9c0a0')):
            if yb <= ya:
                continue
            if z is not None:
                par(mat, tint, x0, ya, z, x1, yb, z)
            else:
                par(mat, tint, x, ya, z0, x, yb, z1)

    # pavimento a piastrelle 30 cm, tinte leggermente diverse
    for i in range(17):
        for j in range(27):
            x0, z0 = i * 0.3, j * 0.3
            base = (0x8b, 0x8b, 0x78) if (i + j) % 2 == 0 else (0x6a, 0x6c, 0x5e)
            k = rng.uniform(0.85, 1.08)
            tint = '#{:02x}{:02x}{:02x}'.format(*(min(255, int(c * k)) for c in base))
            m.add('piastrelle', tint, *lastra(x0, 0, z0, min(x0 + 0.3, 5), 0, min(z0 + 0.3, 8), C))
    # pareti: fronte con porta e vetrina
    fascia(0, 2.7, 0, 0.9, z=0)
    fascia(0, 0.4, 0.9, 2.3, z=0)
    fascia(2.2, 2.7, 0.9, 2.3, z=0)
    fascia(0, 2.7, 2.3, 3.0, z=0)
    fascia(3.8, 5, 0, 3.0, z=0)
    fascia(2.7, 3.8, 2.2, 3.0, z=0)
    fascia(0, 5, 0, 3.0, z=8)
    fascia(0, 0, 0, 3.0, x=0, z0=0, z1=8)
    fascia(5, 5, 0, 3.0, x=5, z0=0, z1=8)
    m.add('legno', '#3d6b4a', *caja(0.0, 1.5, 7.97, 5, 1.53, 8.0))
    for (x0, x1, z0, z1) in ((0.02, 0.05, 0, 8), (4.95, 4.98, 0, 8)):
        m.add('legno', '#3d6b4a', *caja(x0, 1.5, z0, x1, 1.53, z1))
    m.add('legno', '#3d6b4a', *caja(0, 1.5, 0.0, 2.7, 1.53, 0.03))
    m.add('legno', '#3d6b4a', *caja(3.8, 1.5, 0.0, 5, 1.53, 0.03))
    # soffitto con travi
    par('intonaco', '#d4cfba', 0, H, 0, 5, H, 8)
    for z in (0.5, 2.0, 3.5, 5.0, 6.5, 7.8):
        m.add('legno', '#4a3523', *caja(0, 2.84, z - 0.07, 5, H, z + 0.07))
    for z in (1.5, 4.0, 6.5):
        m.add('metallo', '#5a5f63', *caja(1.6, 2.8, z - 0.1, 3.4, 2.84, z + 0.1))
        m.add('liscio', '#e8f4ff', *caja(1.7, 2.76, z - 0.05, 3.3, 2.8, z + 0.05), e=2)
        m.add('liscio', '#e8f4ff', *caja(1.7, 2.76, z - 0.05 + 0.0, 3.3, 2.8, z + 0.05), e=2)
    # porta d'ingresso, battente aperto, e vetrina
    for x in (2.65, 3.8):
        m.add('legno', '#4b6a50', *caja(x, 0, 0, x + 0.08, 2.2, 0.14))
    m.add('legno', '#4b6a50', *caja(2.65, 2.15, 0, 3.88, 2.25, 0.14))
    m.add('pietra', '#9a9484', *caja(2.65, 0, 0, 3.88, 0.03, 0.3))
    m.ins(porta_vetro(), 3.8, 0, 0.02, rot=95)
    m.add('metallo', '#c9a227', *prisma(3.25, 0.12, 2.05, 2.12, 0.035, 0.035, 8))
    m.add('legno', '#3d2c1f', *caja(2.75, 0, 0.1, 3.75, 0.012, 0.9))
    m.add('vetro', '#cfe6ee', *caja(0.4, 0.9, -0.02, 2.2, 2.3, 0.0), e=3)
    for (x0, y0, x1, y1) in ((0.36, 0.9, 0.4, 2.3), (2.2, 0.9, 2.24, 2.3), (0.36, 2.3, 2.24, 2.34), (0.36, 0.86, 2.24, 0.9), (1.28, 0.9, 1.32, 2.3)):
        m.add('legno', '#4b6a50', *caja(x0, y0, 0.0, x1, y1, 0.1))
    m.add('pietra', '#b8b2a0', *caja(0.3, 0.86, 0.0, 2.3, 0.9, 0.22))
    m.ins(cestone('cestone_vetrina'), 0.55, 0.9, 0.0, rot=0, s=0.9)
    m.add('stampa_a', '#f4efd8', *caja(1.4, 1.25, 0.1, 2.1, 1.6, 0.12))
    # scaffali: parete destra industriali, fondo naturali, isola centrale doppia, pane a sinistra
    ind = [['patatine', 'caramelle', 'merendina', 'surgelati'], ['lattina', 'birra', 'sigarette', 'lattina_blu'],
           ['merendina', 'patatine', 'caramelle', 'lattina'], ['birra', 'surgelati', 'patatine', 'merendina']]
    car = ['#ff6a3d', '#ffd23f', '#2e86de', '#d6279a']
    for k, zf in enumerate((3.1, 4.6, 6.1, 7.6)):
        m.ins(scaffale('sc_dx_%d' % k, ind[k], cartello=car[k], seme=k + 1), 0.03, 0, zf, rot=90)
    m.ins(scaffale('sc_fo_0', ['pane', 'verdura', 'uova', 'mela'], seme=7), 2.1, 0, 7.97, rot=180)
    m.ins(scaffale('sc_fo_1', ['formaggio', 'latte', 'miele', 'salame'], seme=8), 3.6, 0, 7.97, rot=180)
    m.ins(scaffale('sc_is_0', ['caramelle', 'merendina', 'patatine', 'lattina'], h=1.55, seme=9), 1.85, 0, 2.8, rot=-90)
    m.ins(scaffale('sc_is_1', ['merendina', 'caramelle', 'lattina_blu', 'surgelati'], h=1.55, seme=10), 1.85, 0, 4.3, rot=-90)
    m.ins(scaffale('sc_is_2', ['latte', 'uova', 'mela', 'miele'], h=1.55, seme=12), 1.85, 0, 5.8, rot=90)
    m.ins(scaffale('sc_is_3', ['pane', 'verdura', 'formaggio', 'mela'], h=1.55, seme=13), 1.85, 0, 4.3, rot=90)
    m.ins(scaffale('sc_pane', ['pane', 'pane', 'pane', None], seme=14), 4.97, 0, 5.6, rot=-90)
    m.ins(banco_affettati(), 4.1, 0, 2.7)
    m.ins(cassa(), 4.2, 0, 0.3)
    m.ins(cestone('cestone_ingresso'), 0.7, 0, 0.9, rot=10)
    # dettagli di parete e angoli
    m.add('liscio', '#f4f1e6', *tubo((2.5, 2.45, 7.97), (2.5, 2.45, 7.93), 0.2, 0.2, 20))
    m.add('metallo', '#2a2a2a', *tubo((2.5, 2.45, 7.93), (2.5, 2.45, 7.9), 0.22, 0.22, 20))
    m.add('liscio', '#1b1b1b', *caja(2.49, 2.45, 7.9, 2.51, 2.62, 7.92))
    m.add('liscio', '#1b1b1b', *caja(2.5, 2.44, 7.9, 2.62, 2.46, 7.92))
    m.add('stampa_a', '#c0392b', *lastra(3.3, 1.6, 7.985, 4.1, 2.4, 7.985, C))
    m.add('stampa_b', '#f1e4a8', *lastra(3.45, 1.78, 7.98, 3.95, 2.2, 7.98, C))
    m.add('stampa_a', '#e8d9a0', *lastra(0.7, 1.7, 7.985, 1.3, 2.4, 7.985, C))
    m.add('metallo', '#8d9499', *esfera(0.45, 2.55, 7.55, 0.22, 0.22, 0.09, 12, 6))
    m.add('metallo', '#3d6fb5', *prisma(4.5, 7.4, 0, 0.3, 0.17, 0.2, 12))
    m.add('legno', '#7a5a38', *tubo((4.5, 0.3, 7.4), (4.6, 1.4, 7.5), 0.015, 0.015, 6))
    m.add('tessuto', '#d8d0b8', *caja(4.45, 1.3, 7.45, 4.75, 1.42, 7.55))
    m.add('liscio', '#e8e4d4', *caja(1.0, 1.2, 0.0, 1.06, 1.4, 0.03))
    return m


# ------------------------------------------------------------------ personaggi
def persona(nome, h=1.75, a=1.0, hs=1.0, pelle='#d6a98a', camicia='#6b7a8f', pantaloni='#3b3f47', capelli='#4a3b30',
            scarpe='#2b2623', grembiule=None, tonaca=None, vestito=None, trecce=False, caramella=False, chierica=False,
            marchio=True, baffi=False, occhiali=False, barba=False, pancia=0.0, maniche_corte=False, cappello=None):
    """Corpo a pochi poligoni ma articolato: arti piegati, mani, scarpe, volto con naso, orecchie, sopracciglia e bocca."""
    m = Mod(nome)
    s = h / 1.75
    A = m.add

    def p(x, y, z): return (x * s, y * s, z * s)

    def E(cx, cy, cz, rx, ry, rz, sg=8, an=5): return esfera(cx * s, cy * s, cz * s, rx * s, ry * s, rz * s, sg, an)

    def T(p0, p1, r0, r1, n=8): return tubo(p(*p0), p(*p1), r0 * s, r1 * s, n)

    def C(x0, y0, z0, x1, y1, z1): return caja(x0 * s, y0 * s, z0 * s, x1 * s, y1 * s, z1 * s)
    # gambe e scarpe
    if not tonaca:
        for sx in (-1, 1):
            x = sx * 0.1 * a
            A('tessuto', pantaloni, *T((x, 0.95, 0), (x, 0.52, 0.015), 0.083, 0.064))
            A('tessuto', pantaloni, *E(x, 0.52, 0.015, 0.066, 0.066, 0.066, 6, 4))
            A('tessuto', pantaloni, *T((x, 0.52, 0.015), (x, 0.09, 0.0), 0.062, 0.045))
            A('pelle', scarpe, *C(x - 0.05, 0.01, -0.045, x + 0.05, 0.075, 0.14))
            A('pelle', scarpe, *E(x, 0.045, 0.14, 0.05, 0.036, 0.045, 7, 4))
            A('gomma', '#151515', *C(x - 0.053, 0.0, -0.048, x + 0.053, 0.016, 0.17))
    # busto: bacino, petto, spalle
    cam = tonaca or camicia
    if tonaca:
        A('tessuto', tonaca, *tronco(0, 0, 0.03 * s, 1.0 * s, 0.62 * s, 0.4 * s, 0.46 * s, 0.3 * s))
        A('tessuto', tonaca, *tronco(0, 0, 1.0 * s, 1.3 * s, 0.46 * s, 0.3 * s, 0.46 * s, 0.26 * s))
        A('tessuto', tonaca, *tronco(0, 0, 1.3 * s, 1.47 * s, 0.46 * s, 0.26 * s, 0.3 * s, 0.18 * s))
        A('tessuto', '#222226', *tronco(0, 0, 0.0, 0.06 * s, 0.64 * s, 0.42 * s, 0.62 * s, 0.4 * s))
        A('tessuto', '#5a2a2a', *C(-0.245, 0.98, -0.158, 0.245, 1.04, 0.158))
        for i in range(7):
            A('liscio', '#3a3a3e', *E(0, 0.18 + i * 0.16, 0.162 - i * 0.004, 0.015, 0.015, 0.008, 5, 3))
    else:
        A('tessuto', pantaloni, *tronco(0, 0, 0.86 * s, 1.0 * s, 0.35 * a * s, 0.22 * s, 0.35 * a * s, 0.22 * s))
        A('tessuto', cam, *tronco(0, 0.01 * s, 1.0 * s, 1.28 * s, 0.37 * a * s, (0.22 + pancia) * s, 0.40 * a * s, 0.25 * s))
        A('tessuto', cam, *tronco(0, 0, 1.28 * s, 1.47 * s, 0.42 * a * s, 0.25 * s, 0.30 * a * s, 0.18 * s))
        bz = 0.117 + pancia * 0.5
        A('gomma', '#3a2a1c', *C(-0.182 * a, 0.955, -0.117, 0.182 * a, 1.004, bz))
        A('metallo', '#c9a227', *C(-0.025, 0.96, bz - 0.002, 0.025, 0.998, bz + 0.012))
        for i in range(4):
            A('liscio', '#e8e4d4', *E(0, 1.06 + i * 0.1, 0.124 + pancia * 0.5, 0.012, 0.012, 0.006, 5, 3))
    A('pelle', pelle, *T((0, 1.46, 0), (0, 1.57, 0.005), 0.05, 0.046))
    if not tonaca:
        A('tessuto', cam, *tubo(p(0, 1.47, 0), p(0, 1.5, 0), 0.062 * s, 0.056 * s, 10))
    else:
        A('liscio', '#f2f2f2', *tubo(p(0, 1.47, 0), p(0, 1.505, 0), 0.062 * s, 0.062 * s, 10))
    # grembiule
    if grembiule:
        gz0, gz1 = 0.108 + pancia * 0.5, 0.136 + pancia * 0.5
        A('tessuto', grembiule, *C(-0.135 * a, 0.72, gz0, 0.135 * a, 1.0, gz1))
        A('tessuto', grembiule, *C(-0.13 * a, 1.0, gz0 + 0.012, 0.13 * a, 1.36, gz1))
        A('tessuto', grembiule, *C(-0.07 * a, 1.2, gz0 + 0.012, 0.07 * a, 1.43, gz1 - 0.002))
        A('tessuto', grembiule, *C(-0.21 * a, 1.012, -0.108, 0.21 * a, 1.046, gz1 + 0.003))
        if grembiule == '#d9d2c0':
            A('liscio', '#8e1f1f', *C(-0.06, 0.9, gz1 - 0.002, 0.05, 1.08, gz1 + 0.004))
            A('liscio', '#8e1f1f', *C(0.04, 1.14, gz1 + 0.008, 0.1, 1.22, gz1 + 0.014))
    if vestito:
        A('tessuto', vestito, *tronco(0, 0, 0.5 * s, 1.1 * s, 0.50 * s, 0.34 * s, 0.30 * s, 0.2 * s))
        A('tessuto', '#f1e3c4', *tronco(0, 0, 0.49 * s, 0.54 * s, 0.52 * s, 0.36 * s, 0.5 * s, 0.34 * s))
    # braccia: spalla, gomito, polso, mano
    for sx in (-1, 1):
        sh, el, wr = (sx * 0.235 * a, 1.4, 0.0), (sx * 0.27 * a, 1.13, 0.03), (sx * 0.275 * a, 0.9, 0.13)
        corte = maniche_corte and not tonaca
        A('tessuto', cam, *E(*sh, 0.06, 0.06, 0.06, 7, 4))
        A('tessuto', cam, *T(sh, el, 0.055, 0.047))
        A('pelle' if corte else 'tessuto', pelle if corte else cam, *T(el, wr, 0.046, 0.037))
        if not corte:
            A('tessuto', '#e8e4d4' if not tonaca else '#f2f2f2', *tubo(p(*wr), p(wr[0], wr[1] - 0.025, wr[2] - 0.004), 0.04 * s, 0.04 * s, 8))
        hx, hy, hz = wr
        A('pelle', pelle, *C(hx - 0.036, hy - 0.1, hz - 0.02, hx + 0.036, hy - 0.02, hz + 0.02))
        A('pelle', pelle, *C(hx - 0.034, hy - 0.17, hz - 0.01, hx + 0.034, hy - 0.1, hz + 0.025))
        A('pelle', pelle, *T((hx - sx * 0.03, hy - 0.06, hz + 0.01), (hx - sx * 0.02, hy - 0.13, hz + 0.045), 0.011, 0.009, 5))
    if caramella:
        hx = 0.275 * a
        A('legno', '#e8e4d4', *tubo(p(hx, 0.74, 0.17), p(hx, 0.6, 0.17), 0.004 * s, 0.004 * s, 4))
        A('liscio', '#ff5fa2', *E(hx, 0.76, 0.17, 0.045, 0.045, 0.012, 8, 5))
    # testa
    ry, rx, rz = 0.125 * hs, 0.105 * hs, 0.118 * hs
    cy = 1.55 + ry * 0.88
    A('pelle', pelle, *E(0, cy, 0, rx, ry, rz, 10, 7))
    A('pelle', pelle, *E(0, cy - 0.072 * hs, 0.02 * hs, 0.086 * hs, 0.062 * hs, 0.092 * hs, 8, 5))
    A('pelle', '#c9967a', *C(-0.014 * hs, cy - 0.055 * hs, rz * 0.9, 0.014 * hs, cy + 0.015 * hs, rz * 1.17))
    A('pelle', '#c9967a', *E(0, cy - 0.052 * hs, rz * 1.15, 0.018 * hs, 0.012 * hs, 0.014 * hs, 6, 4))
    for sx in (-1, 1):
        A('pelle', pelle, *E(sx * rx * 1.0, cy - 0.005 * hs, -0.006, 0.014 * hs, 0.03 * hs, 0.022 * hs, 6, 4))
        ex = sx * 0.04 * hs
        A('liscio', '#efe9df', *E(ex, cy + 0.015 * hs, rz * 0.9, 0.0165 * hs, 0.011 * hs, 0.01 * hs, 7, 4))
        A('liscio', '#1c1a1a', *E(ex, cy + 0.015 * hs, rz * 0.9 + 0.006 * hs, 0.0075 * hs, 0.0075 * hs, 0.005 * hs, 6, 4))
        A('capelli', '#3a2e26' if not chierica else '#8a8a88', *C(ex - 0.026 * hs, cy + 0.042 * hs, rz * 0.91, ex + 0.026 * hs, cy + 0.053 * hs, rz * 1.012))
    A('liscio', '#6a2f2f', *C(-0.024 * hs, cy - 0.078 * hs, rz * 0.88, 0.024 * hs, cy - 0.07 * hs, rz * 1.02))
    A('liscio', '#b07070', *C(-0.02 * hs, cy - 0.0695 * hs, rz * 0.9, 0.02 * hs, cy - 0.0655 * hs, rz * 1.027))
    if baffi:
        A('capelli', '#4a4a48', *C(-0.036 * hs, cy - 0.063 * hs, rz * 0.9, 0.036 * hs, cy - 0.047 * hs, rz * 1.06))
    if barba:
        A('capelli', '#5a5048', *E(0, cy - 0.085 * hs, 0.02 * hs, 0.084 * hs, 0.05 * hs, 0.09 * hs, 8, 4))
    if occhiali:
        for sx in (-1, 1):
            ex = sx * 0.04 * hs
            A('metallo', '#2a2a2a', *C(ex - 0.025 * hs, cy + 0.0 * hs, rz * 0.96, ex + 0.025 * hs, cy + 0.003 * hs, rz * 1.03))
            A('metallo', '#2a2a2a', *C(ex - 0.025 * hs, cy + 0.03 * hs, rz * 0.96, ex + 0.025 * hs, cy + 0.033 * hs, rz * 1.03))
    # capelli
    if cappello:
        A('tessuto', cappello, *E(0, cy + ry * 0.45, -0.005, rx * 1.1, ry * 0.65, rz * 1.12, 10, 5))
        A('tessuto', cappello, *C(-rx * 1.0, cy + ry * 0.18, rz * 0.3, rx * 1.0, cy + ry * 0.25, rz * 1.25))
    elif chierica:
        A('capelli', capelli, *E(0, cy + ry * 0.2, -0.012, rx * 1.07, ry * 0.5, rz * 1.07, 10, 5))
    else:
        A('capelli', capelli, *E(0, cy + ry * 0.74, -0.03, rx * 1.06, ry * 0.42, rz * 1.12, 10, 5))
        A('capelli', capelli, *E(0, cy + ry * 0.1, -0.05 * hs, rx * 1.0, ry * 0.7, rz * 0.85, 8, 5))
    if trecce:
        for sx in (-1, 1):
            A('capelli', capelli, *E(sx * (rx + 0.03), cy - ry * 0.35, -0.03, 0.04, 0.07, 0.04, 6, 4))
            A('tessuto', '#d9534f', *E(sx * (rx + 0.028), cy - ry * 0.02, -0.03, 0.017, 0.017, 0.017, 5, 3))
    if marchio:
        yy, k, z0, z1 = (cy + ry * 0.34) * s, 1.0 * s * hs, rz * 0.74 * s, rz * 1.04 * s
        g = '#ffb347'
        A('liscio', '#5a2a20', *caja(-0.034 * k, yy - 0.034 * k, z0 - 0.005, 0.034 * k, yy + 0.034 * k, z1 - 0.012 * s), tag='marchio')
        for (x0, y0, x1, y1) in [(-0.008, -0.03, 0.008, 0.03), (-0.03, 0.012, 0.03, 0.028), (-0.034, -0.03, -0.02, -0.012), (0.02, -0.03, 0.034, -0.012), (-0.012, -0.01, 0.012, 0.0)]:
            A('liscio', g, *caja(x0 * k, yy + y0 * k, z0, x1 * k, yy + y1 * k, z1), e=2, tag='marchio')
    return m


def personaggi():
    return {
        'padre': persona('padre', 1.76, 1.0, 1.0, camicia='#7d8b6f', pantaloni='#3f4047', capelli='#6a6a66', grembiule='#cfc8b4',
                         marchio=False, baffi=True, occhiali=True, pancia=0.03, maniche_corte=False),
        'figlia': persona('figlia', 1.15, 0.9, 1.3, camicia='#d9c25a', pantaloni='#5a4f63', capelli='#4a3328', vestito='#d9c25a',
                          trecce=True, caramella=True, maniche_corte=True),
        'parroco': persona('parroco', 1.78, 1.0, 1.0, tonaca='#1e1e22', capelli='#9a9a98', chierica=True),
        'macellaio': persona('macellaio', 1.72, 1.3, 1.05, camicia='#d9d2c0', pantaloni='#4a4a4a', capelli='#3a2c24', grembiule='#d9d2c0',
                             pancia=0.05, maniche_corte=True, barba=True, cappello='#e8e4d4'),
    }
