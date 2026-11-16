#!/usr/bin/env python3
"""Genera la v2: modelli .obj con UV, la pagina v2.html (texture procedurali) e, con --texture, i PNG delle texture.

Uso: python3 genera_v2.py            # .obj e v2.html
     python3 genera_v2.py --texture  # in più, esporta le texture dal visore (serve Playwright, vedi esporta_texture.js)
"""
import json
import random
from pathlib import Path

import edifici_v2 as ed
import modelli_v2 as mo
from geo import GEN, MATERIALI, REG, conta_tri, scrivi_obj

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
ESPORTA = ['negozio_guscio', 'scaffale_tipo', 'cassa', 'banco_affettati', 'cestone', 'padre', 'figlia', 'parroco', 'macellaio', 'chiesa', 'bar',
           'macelleria', 'negozio_esterno', 'osteria', 'casa', 'negozio_piante', 'cabina_telefonica', 'lampione', 'fontana', 'abete', 'sasso',
           'staccionata', 'panchina', 'barile', 'legna', 'tavolo_prodotti', 'porta_vetro', 'finestra_gotica'] + ['p_' + t for t in mo.NATURALI + mo.INDUSTRIALI]


def costruisci():
    mo.personaggi()
    ed.tutto()
    mo.negozio_interno()
    mo.cestone('cestone')
    mo.scaffale('scaffale_tipo', ['patatine', 'lattina', 'pane', 'mela'], cartello='#ff6a3d')
    # guscio del negozio senza istanze (arredi e prodotti sono modelli a parte)
    g = mo.Mod('negozio_guscio')
    g.parti = REG['negozio_interno'].parti


def scene():
    rng = random.Random(7)
    alberi = []
    while len(alberi) < 46:
        x, z = rng.uniform(-75, 75), rng.uniform(-75, 65)
        if abs(x) < 22 and -34 < z < 24:
            continue
        alberi.append({'m': 'abete', 'p': [round(x, 1), 0, round(z, 1)], 'r': rng.randint(0, 359), 's': round(rng.uniform(0.9, 1.5), 2)})
    sassi = []
    while len(sassi) < 22:
        x, z = rng.uniform(-60, 60), rng.uniform(-60, 55)
        if abs(x) < 20 and -30 < z < 20:
            continue
        sassi.append({'m': 'sasso', 'p': [round(x, 1), 0, round(z, 1)], 'r': rng.randint(0, 359), 's': round(rng.uniform(0.7, 2.2), 2)})
    montagne = [{'m': 'montagna', 'p': p, 'r': r, 's': s} for p, r, s in [
        ([-70, 0, -135], 10, 1.0), ([10, 0, -155], 40, 1.4), ([85, 0, -130], 70, 1.1), ([-140, 0, -105], 20, 1.2), ([145, 0, -115], 55, 1.0)]]
    recinti = [{'m': 'staccionata', 'p': [x, 0, z], 'r': r} for x, z, r in [(-26, 13, 0), (-23, 13, 0), (-20, 13, 0), (15, 20, 0), (18, 20, 0), (21, 20, 0), (-24, -18, 90), (-24, -15, 90)]]
    neon = [{'p': [2.5, 2.7, z], 'c': [1.0, 0.96, 0.86], 'i': 1.5, 'on': 'sempre', 'f': 1 if z == 4.0 else 0} for z in (1.5, 4.0, 6.5)]
    return {
        'negozio': {
            'nome': 'Negozio', 'sole': 0.35,
            'items': [{'m': 'terreno_negozio', 'p': [0, 0, 0]}, {'m': 'negozio_interno', 'p': [0, 0, 0]}, {'m': 'figlia', 'p': [3.95, 0, 1.75], 'r': 90}],
            'luci': neon + [{'p': [4.5, 1.6, 1.0], 'c': [0.5, 1.0, 0.7], 'i': 0.7, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Cassa (subito a sinistra)', 'p': [4.6, 2.0, 1.1]}, {'t': 'Banco affettati e pane', 'p': [4.55, 1.6, 4.0]},
                          {'t': 'Scaffali industriali', 'p': [0.3, 2.45, 4.6]}, {'t': 'Scaffali naturali', 'p': [2.8, 2.45, 7.8]},
                          {'t': 'La figlia e i dolci', 'p': [3.95, 1.5, 1.75]}],
            'orbita': {'t': [2.5, 1.0, 4.0], 'd': 11.5, 'y': 40, 'p': 38}, 'fps': {'e': [3.25, 1.65, 0.5], 'y': 0, 'p': -3}},
        'personaggi': {
            'nome': 'Personaggi', 'sole': 0.7,
            'items': [{'m': 'podio', 'p': [0, 0, 0]}, {'m': 'padre', 'p': [-2.4, 0, 0], 'r': 12}, {'m': 'figlia', 'p': [-0.8, 0, 0.1], 'r': -8},
                      {'m': 'parroco', 'p': [0.9, 0, 0], 'r': -10}, {'m': 'macellaio', 'p': [2.5, 0, 0], 'r': -15}],
            'luci': [{'p': [-2.5, 2.6, 2.8], 'c': [1.0, 0.82, 0.6], 'i': 2.2, 'on': 'sempre', 'f': 0}, {'p': [3.2, 2.2, -2.0], 'c': [0.5, 0.65, 1.0], 'i': 1.6, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Il padre (senza marchio)', 'p': [-2.4, 1.95, 0]}, {'t': 'La figlia', 'p': [-0.8, 1.35, 0.1]},
                          {'t': 'Il parroco', 'p': [0.9, 2.0, 0]}, {'t': 'Il macellaio', 'p': [2.5, 1.95, 0]}],
            'orbita': {'t': [0, 1.0, 0], 'd': 4.6, 'y': 12, 'p': 7}},
        'paese': {
            'nome': 'Paese', 'sole': 1.0, 'nebbia': 0.4,
            'items': [{'m': 'terreno_paese', 'p': [0, 0, 0]}, {'m': 'fontana', 'p': [0, 0, 0]}, {'m': 'chiesa', 'p': [0, 0, -10]},
                      {'m': 'bar', 'p': [10, 0, 2], 'r': -90}, {'m': 'negozio_esterno', 'p': [-10, 0, 2], 'r': 90}, {'m': 'macelleria', 'p': [-10, 0, -7], 'r': 90},
                      {'m': 'negozio_piante', 'p': [10, 0, -6], 'r': -90}, {'m': 'osteria', 'p': [4, 0, 10], 'r': 180}, {'m': 'casa', 'p': [-6, 0, 10], 'r': 180},
                      {'m': 'cabina_telefonica', 'p': [9.5, 0, 8], 'r': -90}, {'m': 'lampione', 'p': [-6, 0, 5]}, {'m': 'lampione', 'p': [6, 0, -5]},
                      {'m': 'panchina', 'p': [-4, 0, -8.6]}, {'m': 'panchina', 'p': [5, 0, 6.4], 'r': 180}, {'m': 'barile', 'p': [0.0, 0, 9.5]}, {'m': 'barile', 'p': [0.9, 0, 9.5]},
                      {'m': 'legna', 'p': [-9.6, 0, 9.4]}, {'m': 'padre', 'p': [-8.3, 0, 2.2], 'r': -90}, {'m': 'figlia', 'p': [-8.2, 0, 3.4], 'r': -60},
                      {'m': 'parroco', 'p': [3, 0, -8.5]}, {'m': 'macellaio', 'p': [-8.4, 0, -6.5], 'r': -90}] + montagne + alberi + sassi + recinti,
            'luci': [{'p': [-6, 4.1, 5], 'c': [1.0, 0.85, 0.6], 'i': 3.0, 'on': 'sera', 'f': 0}, {'p': [6, 4.1, -5], 'c': [1.0, 0.85, 0.6], 'i': 3.0, 'on': 'sera', 'f': 0},
                     {'p': [8.5, 2, 2], 'c': [1.0, 0.7, 0.4], 'i': 3.0, 'on': 'sera', 'f': 0}, {'p': [-8.5, 2, 2], 'c': [0.9, 0.95, 0.8], 'i': 3.0, 'on': 'sera', 'f': 0}],
            'etichette': [{'t': 'Chiesa', 'p': [0, 11, -10]}, {'t': 'Bar', 'p': [12, 5, 2]}, {'t': 'Il negozio', 'p': [-12, 6, 2]}, {'t': 'Macelleria', 'p': [-12, 5, -7]},
                          {'t': 'Negozio di piante', 'p': [12, 4.5, -6]}, {'t': 'Osteria', 'p': [4, 6.5, 12]}, {'t': 'Casa', 'p': [-6, 9, 12]},
                          {'t': 'Cabina', 'p': [9.5, 3, 8]}, {'t': 'Piazza', 'p': [0, 3.2, 0]}],
            'orbita': {'t': [0, 2, -2], 'd': 46, 'y': 20, 'p': 28}},
        'prodotti': {
            'nome': 'Prodotti', 'sole': 0.8,
            'items': [{'m': 'podio', 'p': [0, 0, 0], 's': 1.6}, {'m': 'tavolo_prodotti', 'p': [0, 0, 0], 's': 3.0}],
            'luci': [{'p': [-1.5, 3.0, 3.0], 'c': [1.0, 0.95, 0.85], 'i': 2.5, 'on': 'sempre', 'f': 0}, {'p': [3.5, 2.5, 2.0], 'c': [0.7, 0.8, 1.0], 'i': 1.4, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Fila dietro: naturali (pane, formaggio, uova, mele, latte, salame, miele, verdura)', 'p': [0, 2.9, -1.2]},
                          {'t': 'Fila davanti: industriali (patatine, lattine, merendine, caramelle, birra, sigarette, surgelati)', 'p': [0, 0.95, 2.8]}],
            'orbita': {'t': [0, 1.1, 0.2], 'd': 8, 'y': 0, 'p': 28}},
    }


def main():
    costruisci()
    cartella = RADICE / 'obj_v2'
    cartella.mkdir(exist_ok=True)
    for n in ESPORTA:
        scrivi_obj(n, cartella)
    # un modello per riga: il file resta leggibile e gli strumenti non si bloccano su righe enormi
    j = lambda o: json.dumps(o, separators=(',', ':'))
    modelli = {}
    for n, m in REG.items():
        modelli[n] = {'parti': [{'m': mat, 'c': tinta, 'e': e, 't': tag, 'v': [round(x * 100) for p in v for x in p], 'i': [i for tri in t for i in tri]}
                                for (mat, tinta, e, tag), (v, t) in m.parti.items()],
                      'figli': [[f[0], round(f[1], 3), round(f[2], 3), round(f[3], 3), f[4], f[5]] for f in m.figli]}
    testo = '{"unita":"cm","materiali":' + j([[m[0], GEN.index(m[1]), m[2], m[3], m[4]] for m in MATERIALI]) + ',"generatori":' + j(GEN)
    testo += ',\n"modelli":{\n' + ',\n'.join('"{}":{}'.format(n, j(m)) for n, m in modelli.items())
    testo += '},\n"scene":{\n' + ',\n'.join('"{}":{}'.format(n, j(s)) for n, s in scene().items())
    testo += '},\n"file":' + j([{'nome': n, 'tri': conta_tri(n)} for n in ESPORTA]) + '}'
    (RADICE / 'v2.html').write_text((AQUI / 'v2.template.html').read_text().replace('__DATI__', testo))
    print('{} modelli, v2.html {} KB'.format(len(modelli), len((RADICE / 'v2.html').read_text()) // 1024))


if __name__ == '__main__':
    main()
