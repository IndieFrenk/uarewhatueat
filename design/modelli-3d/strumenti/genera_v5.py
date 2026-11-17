#!/usr/bin/env python3
"""Genera la v5 (piazza di Canolo) in obj_v5/: .obj + .mtl e le sole texture usate (copiate da obj_v4/texture).

Uso: python genera_v5.py
Orientamento: fronte verso +Z, pivot al centro del fronte (edifici) o al centro in pianta (arredo), 1 unita = 1 m, Y su.
"""
import re
import shutil
from pathlib import Path

import edifici_v5 as ed
from geo import REG, conta_tri, scrivi_obj

AQUI = Path(__file__).resolve().parent
RADICE = AQUI.parent
ESPORTA = ['macelleria', 'fioraio', 'tabacchi', 'osteria', 'canonica', 'casa_piazza', 'bar_piazza', 'bottega',
           'monumento', 'utilitaria', 'cassonetto', 'bacheca', 'fioriera', 'tavolino_osteria', 'pavimento_piazza']


def main():
    ed.tutto()
    cartella = RADICE / 'obj_v5'
    (cartella / 'texture').mkdir(parents=True, exist_ok=True)
    usate = set()
    for n in ESPORTA:
        scrivi_obj(n, cartella)
        usate |= set(re.findall(r'map_Kd texture/(\w+)\.png', (cartella / (n + '.mtl')).read_text()))
    for t in sorted(usate):
        shutil.copy(RADICE / 'obj_v4' / 'texture' / (t + '.png'), cartella / 'texture' / (t + '.png'))
    for n in ESPORTA:
        print('{:18s} {:6d} tri'.format(n, conta_tri(n)))
    print('texture:', ', '.join(sorted(usate)))


if __name__ == '__main__':
    main()
