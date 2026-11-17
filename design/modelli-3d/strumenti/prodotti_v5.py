#!/usr/bin/env python3
"""v5: i 25 prodotti del catalogo (Assets/src/Core/Catalog.cs) in stile v3/v4, un .obj per id.

Uso: python3 prodotti_v5.py  ->  design/modelli-3d/obj_v5/prodotti/<id>.obj/.mtl/texture/  (+ copia in Assets/Resources/Models/v5/prodotti, senza .meta)

I prodotti che la v3/v4 aveva gia' (pane, uova, formaggio, latte, patatine, birra, merendina, caramelle, sigarette) sono riusati cosi' come sono
(modelli_v4.prodotto) e rinominati con l'id del catalogo; gli altri sono nuovi. Nome file = id del catalogo: ProductLook.Build li cerca con quel nome.

Convenzioni: metri, Y su, base a y=0, centro su x=z=0, fronte +z. Scrittura "piana" (niente rotazione come in canolo_v5): l'importatore OBJ di Unity
specchia x ma anche le UV viaggiano con i vertici, quindi lo stampato sul fronte +z si legge dritto visto da +z. Le stampe sono su fronte e retro
(il prodotto non ha un davanti fisso sul ripiano). Ingombri: <= 0.36 x 0.32 x 0.3 m, compatibili con i ripiani v4 (passo ~0.45, profondita' ~0.3).
Vertici a 1 mm (la v5 degli edifici usa 1 cm: troppo per i dettagli di un pacchetto).
"""
import shutil
from pathlib import Path

import modelli_v4 as mo
from geo import MATERIALI, Mod, caja, cross, esfera, prisma, sub, tubo, tutte_le_parti

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
USCITA = RADICE / 'obj_v5' / 'prodotti'
UNITY = RADICE.parents[1] / 'Assets' / 'Resources' / 'Models' / 'v5' / 'prodotti'

RIUSATI = {'pane_forno': 'pane', 'uova_paese': 'uova', 'formaggio_malga': 'formaggio', 'latte': 'latte', 'patatine': 'patatine',
           'birra': 'birra', 'merendine': 'merendina', 'caramelle': 'caramelle', 'sigarette': 'sigarette'}


def bottiglia(m, vetro, r, hc, hs, rn, ht, etichetta=None, capsula='#c9a227', cap_h=0.03, fasce=()):
    """Bottiglia: corpo cilindrico (0..hc), spalla conica (hs), collo (fino a ht), capsula, etichetta a fascia (y0, y1, tinta, mat)."""
    m.add('vetro', vetro, *tubo((0, 0, 0), (0, hc, 0), r, r, 14))
    m.add('vetro', vetro, *tubo((0, hc, 0), (0, hc + hs, 0), r, rn, 14))
    m.add('vetro', vetro, *tubo((0, hc + hs, 0), (0, ht, 0), rn, rn, 10))
    m.add('metallo', capsula, *tubo((0, ht - cap_h, 0), (0, ht, 0), rn * 1.12, rn * 1.12, 10))
    for y0, y1, tinta, mat in ([etichetta] if etichetta else []) + list(fasce):
        m.add(mat, tinta, *tubo((0, y0, 0), (0, y1, 0), r * 1.03, r * 1.03, 14))


def scatola(m, x, y, z, tinta, stampa, band=None, mat='cartone'):
    """Scatola di cartone x*y*z con fascia stampata su fronte e retro (+z e -z) e alette in cima."""
    hx, hz = x / 2, z / 2
    m.add(mat, tinta, *caja(-hx, 0, -hz, hx, y, hz))
    for s in (-1, 1):
        a, b = (hz, hz + 0.003) if s > 0 else (-hz - 0.003, -hz)
        m.add('stampa_a', stampa, *caja(-hx * 0.82, y * 0.3, a, hx * 0.82, y * 0.82, b))
        if band:
            m.add('stampa_b', band, *caja(-hx * 0.82, y * 0.12, a, hx * 0.82, y * 0.26, b))


def nuovo(nome):
    m = Mod(nome)
    a = m.add
    if nome == 'mortadella':   # insaccato sdraiato lungo x, rete e legatura
        a('crosta', '#d98a82', *tubo((-0.15, 0.07, 0), (0.15, 0.07, 0), 0.07, 0.07, 16))
        for sx in (-1, 1):
            a('liscio', '#f2d4cc', *tubo((sx * 0.1495, 0.07, 0), (sx * 0.15, 0.07, 0), 0.066, 0.066, 16))
        for x in (-0.1, -0.05, 0.0, 0.05, 0.1):
            a('cartone', '#e8dcc0', *tubo((x - 0.004, 0.07, 0), (x + 0.004, 0.07, 0), 0.0715, 0.0715, 16))
        for z in (-0.03, 0.0, 0.03):
            a('liscio', '#f4e8dc', *esfera(0.152, 0.07 + z * 0.8, z, 0.004, 0.008, 0.008, 5, 3))
        a('cartone', '#8a6a3a', *tubo((0.12, 0.135, 0), (0.12, 0.15, 0), 0.006, 0.006, 6))
    elif nome == 'prosciutto':  # coscia con grasso sopra, osso e zampino verso +x
        a('crosta', '#a8453a', *esfera(-0.02, 0.075, 0, 0.13, 0.07, 0.065, 12, 7))
        a('liscio', '#efdfbb', *esfera(-0.02, 0.1, 0, 0.12, 0.04, 0.05, 10, 5))
        a('liscio', '#e8d6b0', *tubo((0.1, 0.075, 0), (0.15, 0.075, 0), 0.018, 0.012, 8))
        a('corteccia', '#4a3320', *tubo((0.13, 0.075, 0), (0.15, 0.06, 0), 0.012, 0.016, 8))
        a('liscio', '#efe0c0', *esfera(-0.13, 0.075, 0, 0.012, 0.05, 0.05, 6, 4))
    elif nome == 'castagne':   # cassetta di legno col mucchio di castagne
        a('legno', '#9a7444', *caja(-0.13, 0, -0.07, 0.13, 0.012, 0.07))
        for s in (-1, 1):
            a('legno', '#a98250', *caja(-0.13, 0, s * 0.07 - (0.012 if s > 0 else 0), 0.13, 0.075, s * 0.07 + (0 if s > 0 else 0.012)))
            a('legno', '#a98250', *caja(s * 0.13 - (0.012 if s > 0 else 0), 0, -0.07, s * 0.13 + (0 if s > 0 else 0.012), 0.075, 0.07))
        for i in range(6):
            for j in range(3):
                for k in range(2):
                    x, z = -0.1 + i * 0.04 + k * 0.02, -0.045 + j * 0.045 + k * 0.02
                    a('corteccia', '#6b3a1c', *esfera(x, 0.06 + k * 0.025, z, 0.022, 0.017, 0.02, 6, 4))
    elif nome == 'olio_oliva':
        bottiglia(m, '#6c7a1c', 0.04, 0.15, 0.07, 0.014, 0.30, (0.04, 0.125, '#efe3a8', 'stampa_a'), '#c9a227', 0.035,
                  fasce=((0.075, 0.09, '#2f6b2a', 'stampa_b'),))
    elif nome == 'passata':
        bottiglia(m, '#b3261e', 0.045, 0.115, 0.03, 0.026, 0.17, (0.035, 0.095, '#f4ecd6', 'stampa_a'), '#2e7d32', 0.025,
                  fasce=((0.06, 0.07, '#d62828', 'stampa_b'),))
    elif nome == 'vino_rosso':
        bottiglia(m, '#17321c', 0.04, 0.17, 0.055, 0.0155, 0.30, (0.06, 0.14, '#f1e8cf', 'stampa_a'), '#7a1a1a', 0.07,
                  fasce=((0.1, 0.108, '#8a1c1c', 'stampa_b'),))
    elif nome == 'vino_sfuso':  # fiasco: vetro rosso scuro, fondo rivestito di paglia
        a('vetro', '#6a1a1a', *esfera(0, 0.1, 0, 0.068, 0.075, 0.068, 12, 7))
        a('cartone', '#c9a45c', *esfera(0, 0.085, 0, 0.072, 0.062, 0.072, 12, 6))
        a('cartone', '#b89048', *prisma(0, 0, 0, 0.03, 0.058, 0.07, 14))
        a('vetro', '#6a1a1a', *tubo((0, 0.165, 0), (0, 0.235, 0), 0.026, 0.017, 10))
        a('legno', '#a88a5a', *prisma(0, 0, 0.235, 0.262, 0.019, 0.021, 8))
    elif nome == 'brodo_dadi':
        scatola(m, 0.10, 0.12, 0.06, '#f4c61c', '#fff5d0', '#d62828')
        a('liscio', '#d62828', *caja(-0.05, 0.1, -0.03, 0.05, 0.12, 0.03))
    elif nome == 'cracker':
        scatola(m, 0.14, 0.20, 0.06, '#2f6fcf', '#ffd23f', '#d62828')
        for s in (-1, 1):
            z = 0.03 if s > 0 else -0.033
            a('cartone', '#d9a85a', *caja(-0.035, 0.09, z, 0.035, 0.145, z + 0.003))
        a('liscio', '#1d4fa0', *caja(-0.07, 0.19, -0.03, 0.07, 0.20, 0.03))
    elif nome == 'zucchero':   # busta di carta bianca con fascia blu e fascia rossa
        a('cartone', '#f4f6fb', *caja(-0.05, 0, -0.035, 0.05, 0.17, 0.035))
        a('cartone', '#e6e9f2', *caja(-0.05, 0.17, -0.02, 0.05, 0.18, 0.02))
        for s in (-1, 1):
            z0, z1 = (0.035, 0.038) if s > 0 else (-0.038, -0.035)
            a('stampa_a', '#1f4fb5', *caja(-0.04, 0.06, z0, 0.04, 0.13, z1))
            a('liscio', '#d62828', *caja(-0.04, 0.03, z0, 0.04, 0.045, z1))
    elif nome == 'lievito':    # panetto incartato rosso
        a('cartone', '#d9261c', *caja(-0.04, 0, -0.015, 0.04, 0.05, 0.015))
        for s in (-1, 1):
            z0, z1 = (0.015, 0.018) if s > 0 else (-0.018, -0.015)
            a('stampa_a', '#ffffff', *caja(-0.03, 0.015, z0, 0.03, 0.035, z1))
    elif nome == 'farina':    # sacchetto di carta 1 kg
        a('cartone', '#efe6cf', *caja(-0.06, 0, -0.04, 0.06, 0.19, 0.04))
        a('cartone', '#dcd0b0', *caja(-0.06, 0.19, -0.025, 0.06, 0.20, 0.025))
        for s in (-1, 1):
            z0, z1 = (0.04, 0.043) if s > 0 else (-0.043, -0.04)
            a('stampa_a', '#2f6fb5', *caja(-0.05, 0.07, z0, 0.05, 0.16, z1))
            a('liscio', '#f2c21b', *caja(-0.05, 0.04, z0, 0.05, 0.055, z1))
    elif nome == 'amaro':
        bottiglia(m, '#2a1208', 0.045, 0.14, 0.09, 0.02, 0.30, (0.05, 0.15, '#d9b13a', 'stampa_b'), '#c9a227', 0.02,
                  fasce=((0.085, 0.115, '#a31515', 'stampa_a'),))
    elif nome == 'biscotti':
        scatola(m, 0.16, 0.22, 0.06, '#1e90e8', '#ffd23f', '#e8412c')
        for s in (-1, 1):
            z = 0.03 if s > 0 else -0.033
            for x in (-0.03, 0.03):
                a('crosta', '#c98a45', *caja(x - 0.025, 0.1, z, x + 0.025, 0.15, z + 0.003))
        a('liscio', '#1670b8', *caja(-0.08, 0.21, -0.03, 0.08, 0.22, 0.03))
    elif nome == 'cacao':      # latta tonda
        a('metallo', '#7a3a18', *prisma(0, 0, 0, 0.13, 0.05, 0.05, 16))
        a('metallo', '#e89a1c', *prisma(0, 0, 0.125, 0.14, 0.052, 0.052, 16))
        a('stampa_b', '#e8861a', *tubo((0, 0.03, 0), (0, 0.11, 0), 0.0508, 0.0508, 16))
        a('liscio', '#fff2d0', *esfera(0, 0.075, 0.0505, 0.022, 0.022, 0.003, 8, 4))
        a('liscio', '#fff2d0', *esfera(0, 0.075, -0.0505, 0.022, 0.022, 0.003, 8, 4))
    elif nome == 'sigari':     # scatola di legno con etichetta e fascetta
        a('legno', '#7a4f2a', *caja(-0.08, 0, -0.05, 0.08, 0.045, 0.05))
        a('legno', '#8c5d33', *caja(-0.081, 0.035, -0.051, 0.081, 0.05, 0.051))
        a('stampa_b', '#d9b13a', *caja(-0.05, 0.05, -0.03, 0.05, 0.053, 0.03))
        a('liscio', '#a31515', *caja(-0.015, 0.05, -0.0515, 0.015, 0.0505, 0.0515))
    else:
        raise ValueError(nome)
    return m


def scrivi(nome, cartella):
    """.obj con un solo oggetto e un submesh per materiale, vertici a 1 mm, UV come geo.scrivi_obj (asse dominante)."""
    mtl, usati, facce, vi, vv, vts, vtv = [], set(), {}, {}, [], {}, []
    for key, v, t in tutte_le_parti(nome):
        mat, tinta, e, tag = key
        mn, gen, sc, _, _ = MATERIALI[mat]
        nm = '{}_{}{}'.format(mn, tinta[1:], '_e' if e else '')
        if nm not in usati:
            usati.add(nm)
            mtl += ['newmtl ' + nm, 'Kd ' + ' '.join('{:.3f}'.format(int(tinta[i:i + 2], 16) / 255) for i in (1, 3, 5))]
            if gen != 'liscio':
                mtl.append('map_Kd texture/{}.png'.format(gen))
        righe = facce.setdefault(nm, [])
        for a, b, c in t:
            A, B, C = v[a], v[b], v[c]
            n = cross(sub(B, A), sub(C, A))
            ax = max(range(3), key=lambda i: abs(n[i]))
            f = []
            for p in (A, B, C):
                q = tuple(round(x, 4) for x in p)
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
    testa = ['# {} - v5 prodotto, 1 unita = 1 metro, base a y=0'.format(nome), 'mtllib {}.mtl'.format(nome), 'o ' + nome]
    testa += ['v {:g} {:g} {:g}'.format(*p) for p in vv] + ['vt {:g} {:g}'.format(*u) for u in vtv]
    for nm, fs in facce.items():
        testa += ['usemtl ' + nm] + fs
    (cartella / (nome + '.obj')).write_text('\n'.join(testa) + '\n')
    (cartella / (nome + '.mtl')).write_text('\n'.join(mtl) + '\n')
    return [p for _, v, _ in tutte_le_parti(nome) for p in v]


def main():
    ids = ['pane_forno', 'uova_paese', 'mortadella', 'prosciutto', 'formaggio_malga', 'castagne', 'olio_oliva', 'passata', 'farina', 'latte',
           'vino_rosso', 'vino_sfuso', 'patatine', 'brodo_dadi', 'cracker', 'zucchero', 'lievito', 'birra', 'amaro', 'merendine', 'biscotti',
           'caramelle', 'cacao', 'sigarette', 'sigari']
    for f in list(USCITA.glob('*')) if USCITA.exists() else []:
        if f.is_file():
            f.unlink()
    USCITA.mkdir(parents=True, exist_ok=True)
    for i in ids:
        if i in RIUSATI:
            Mod(i).ins(mo.prodotto(RIUSATI[i]))
        else:
            nuovo(i)
        pts = scrivi(i, USCITA)
        d = [max(p[k] for p in pts) - min(p[k] for p in pts) for k in range(3)]
        assert d[0] <= 0.36 and d[1] <= 0.32 and d[2] <= 0.3, (i, d)   # ingombro compatibile con i ripiani v4
        assert min(p[1] for p in pts) > -0.01, i                       # base a y=0
        print('{:16s} {:.3f} x {:.3f} x {:.3f} m'.format(i, *d))
    tex = USCITA / 'texture'
    tex.mkdir(exist_ok=True)
    usate = set()
    for f in USCITA.glob('*.mtl'):
        usate |= {r.split('/')[-1] for r in f.read_text().split() if r.startswith('texture/')}
    for t in usate:
        shutil.copy(RADICE / 'obj_v3' / 'texture' / t, tex / t)
    if UNITY.parent.exists():
        shutil.rmtree(UNITY, ignore_errors=True)
        shutil.copytree(USCITA, UNITY)
    print('{} prodotti in {} (copia in {})'.format(len(ids), USCITA, UNITY))


if __name__ == '__main__':
    main()
