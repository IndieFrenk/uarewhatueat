#!/usr/bin/env python3
"""Genera la v4: v4.html, i modelli nuovi o cambiati in obj_v4/ (personaggi esclusi) e il layout del negozio.

Uso: python3 genera_v4.py
Prima servono dati/canolo_osm.json (estrai_osm.py) e dati/canolo_quote.json (scarica_quote.py).
"""
import json
from pathlib import Path

import canolo_v4 as cn
import edifici_v4 as ed
import modelli_v4 as mo
from geo import GEN, MATERIALI, REG, conta_tri, scrivi_obj

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
# i personaggi non si salvano come file: i modelli li sceglie Francesco
ESPORTA = ['negozio_guscio', 'magazzino', 'cassa', 'banco_affettati', 'scaffale_tipo', 'cestone', 'porta_vetro', 'porta_magazzino', 'serranda', 'scrivania_mag', 'sgabello',
           'congelatore', 'carrello_mano', 'sacchi', 'bancale', 'bancale_c0', 'bancale_c2', 'scatola_0', 'scatola_1', 'scatola_2', 'rack_mag_dx0', 'rack_mag_fo',
           'sc_dx_0', 'sc_dx_1', 'sc_dx_2', 'sc_dx_3', 'sc_fo_0', 'sc_fo_1', 'sc_pane', 'sc_is_0', 'sc_is_1', 'sc_is_2', 'sc_is_3']


def costruisci():
    mo.personaggi()
    ed.tutto()
    mo.negozio_interno()
    mo.cestone('cestone')
    mo.scaffale('scaffale_tipo', ['patatine', 'lattina', 'pane', 'mela'], cartello='#ff6a3d')
    mo.bancale('bancale')
    g = mo.Mod('negozio_guscio')
    g.parti = REG['negozio_interno'].parti


def scene(items_canolo, etichette_canolo):
    neon = [{'p': [3.0, 2.7, z], 'c': [1.0, 0.96, 0.86], 'i': 1.5, 'on': 'sempre', 'f': 1 if z == 4.0 else 0} for z in (1.5, 4.0, 6.5)]
    return {
        'negozio': {
            'nome': 'Negozio e magazzino', 'sole': 0.35,
            'items': [{'m': 'terreno_negozio', 'p': [0, 0, 0]}, {'m': 'negozio_interno', 'p': [0, 0, 0]}, {'m': 'figlia', 'p': [3.85, 0, 1.75], 'r': 90}],
            'luci': neon + [{'p': [3.0, 2.3, 9.0], 'c': [1.0, 0.85, 0.6], 'i': 1.6, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Cassa (subito a sinistra)', 'p': [4.5, 2.0, 1.1]}, {'t': 'Banco affettati e pane', 'p': [4.55, 1.7, 4.0]},
                          {'t': 'Scaffali industriali', 'p': [0.3, 2.45, 4.6]}, {'t': 'Scaffali naturali', 'p': [2.8, 2.45, 7.7]},
                          {'t': 'La figlia e i dolci', 'p': [3.85, 1.5, 1.75]}, {'t': 'Corridoio dietro banco e cassa', 'p': [5.5, 2.2, 3.4]},
                          {'t': 'Passaggio del personale', 'p': [4.5, 1.4, 2.3]}, {'t': 'Porta del magazzino', 'p': [5.5, 2.5, 7.9]},
                          {'t': 'Magazzino', 'p': [3.0, 2.6, 10.3]}],
            'orbita': {'t': [3.0, 1.0, 5.6], 'd': 15, 'y': 38, 'p': 40},
            'fps': {'e': [3.0, 1.65, 0.45], 'y': 0, 'p': -3, 'limiti': [0.25, 0.25, 5.75, 11.25], 'ostacoli': mo.OSTACOLI}},
        'banco': {
            'nome': 'Banco affettati', 'sole': 0.8,
            'items': [{'m': 'podio', 'p': [0, 0, 0], 's': 0.9}, {'m': 'banco_affettati', 'p': [-0.45, 0, -1.25]}],
            'luci': [{'p': [-1.6, 2.4, 0.6], 'c': [1.0, 0.95, 0.85], 'i': 2.2, 'on': 'sempre', 'f': 0}, {'p': [1.4, 2.2, -0.8], 'c': [0.8, 0.85, 1.0], 'i': 1.6, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Vetrina con salumi e formaggi', 'p': [0.0, 1.45, -0.7]}, {'t': 'Prosciutto su morsa', 'p': [0.1, 1.35, 0.2]},
                          {'t': 'Bilancia', 'p': [0.0, 1.3, 0.65]}, {'t': 'Affettatrice', 'p': [0.15, 1.45, 1.0]}, {'t': 'Prosciutti e salami appesi', 'p': [0.31, 2.3, 0.0]}],
            'orbita': {'t': [0, 1.1, 0.0], 'd': 3.4, 'y': -68, 'p': 20}},
        'personaggi': {
            'nome': 'Personaggi (segnaposto)', 'sole': 0.7,
            'items': [{'m': 'podio', 'p': [0, 0, 0]}, {'m': 'padre', 'p': [-2.4, 0, 0], 'r': 12}, {'m': 'figlia', 'p': [-0.8, 0, 0.1], 'r': -8},
                      {'m': 'parroco', 'p': [0.9, 0, 0], 'r': -10}, {'m': 'macellaio', 'p': [2.5, 0, 0], 'r': -15}],
            'luci': [{'p': [-2.5, 2.6, 2.8], 'c': [1.0, 0.82, 0.6], 'i': 2.2, 'on': 'sempre', 'f': 0}, {'p': [3.2, 2.2, -2.0], 'c': [0.5, 0.65, 1.0], 'i': 1.6, 'on': 'sempre', 'f': 0}],
            'etichette': [{'t': 'Il padre (senza marchio)', 'p': [-2.4, 1.95, 0]}, {'t': 'La figlia', 'p': [-0.8, 1.35, 0.1]},
                          {'t': 'Il parroco', 'p': [0.9, 2.0, 0]}, {'t': 'Il macellaio', 'p': [2.5, 1.95, 0]}],
            'orbita': {'t': [0, 1.0, 0], 'd': 4.6, 'y': 12, 'p': 7}},
        'canolo': {
            'nome': 'Canolo Nuovo', 'sole': 1.0, 'nebbia': 0.04, 'far': 15000,
            'items': [{'m': '@terreno', 'p': [0, 0, 0]}] + [i for i in items_canolo if i['m'] != 'terreno_canolo'], 'luci': [], 'etichette': etichette_canolo,
            'orbita': {'t': [0, 0, 0], 'd': 430, 'y': 25, 'p': 30}},
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
    items, etichette, T, n_ed = cn.costruisci_canolo()
    cartella = RADICE / 'obj_v4'
    cartella.mkdir(exist_ok=True)
    for n in ESPORTA:
        scrivi_obj(n, cartella)
    j = lambda o: json.dumps(o, separators=(',', ':'))
    def compatta(v, t):
        """Unisce i vertici uguali (coordinate in cm): i file restano piccoli."""
        indice, nuovi, tri = {}, [], []
        for a, b2, c in t:
            for k in (a, b2, c):
                q = tuple(round(x * 100) for x in v[k])
                if q not in indice:
                    indice[q] = len(nuovi) // 3
                    nuovi.extend(q)
                tri.append(indice[q])
        return nuovi, tri

    def righe(arr, n=300):
        return '[' + ',\n'.join(','.join(str(x) for x in arr[k:k + n]) for k in range(0, len(arr), n)) + ']'

    def modello_json(m):
        parti = []
        for (mat, tinta, e, tag), (v, t) in m.parti.items():
            vv, ii = compatta(v, t)
            parti.append('{"m":%d,"c":"%s","e":%d,"t":"%s",\n"v":%s,\n"i":%s}' % (mat, tinta, e, tag, righe(vv), righe(ii)))
        figli = j([[f[0], round(f[1], 3), round(f[2], 3), round(f[3], 3), f[4], f[5]] for f in m.figli])
        return '{"parti":[' + ',\n'.join(parti) + '],"figli":' + figli + '}'
    testo = '{"unita":"cm","materiali":' + j([[m[0], GEN.index(m[1]), m[2], m[3], m[4]] for m in MATERIALI]) + ',"generatori":' + j(GEN)
    terreno = {'xs': [round(x, 2) for x in T.xs], 'h': [round((T.h[jj][ii] - T.h0) * 100) for jj in range(T.n) for ii in range(T.n)]}
    testo += ',\n"terreno":{"xs":' + j(terreno['xs']) + ',\n"h":' + righe(terreno['h'], 40) + '}'
    testo += ',\n"modelli":{\n' + ',\n'.join('"{}":{}'.format(n, modello_json(m)) for n, m in REG.items() if n != 'terreno_canolo')
    def scena_json(s):
        resto = {k: v for k, v in s.items() if k != 'items'}
        return j(resto)[:-1] + ',\n"items":[' + ',\n'.join(j(i) for i in s['items']) + ']}'
    testo += '},\n"scene":{\n' + ',\n'.join('"{}":{}'.format(n, scena_json(s)) for n, s in scene(items, etichette).items())
    testo += '},\n"file":' + j([{'nome': n, 'tri': conta_tri(n)} for n in ESPORTA]) + '}'
    layout = []
    for nome in ('negozio_interno', 'magazzino'):
        for (fn, x, y, z, rot, sc) in REG[nome].figli:
            layout.append({'in': nome, 'modello': fn, 'x': x, 'y': y, 'z': z, 'rotY': rot, 'scala': sc})
    (cartella / 'negozio_layout.json').write_text(json.dumps({'unita': 'metri', 'ostacoli_xz': [list(o) for o in mo.OSTACOLI], 'istanze': layout}, indent=1))
    (RADICE / 'v4.html').write_text((AQUI / 'v4.template.html').read_text().replace('__DATI__', testo))
    print('{} modelli, {} edifici di Canolo, v4.html {} KB'.format(len(REG), n_ed, len((RADICE / 'v4.html').read_text()) // 1024))


if __name__ == '__main__':
    main()
