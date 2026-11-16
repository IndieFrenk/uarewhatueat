#!/usr/bin/env python3
"""Scarica le quote del terreno attorno a Canolo Nuovo (EU-DEM 25 m) in dati/canolo_quote.json."""
import json
import sys
import time
import urllib.request
from pathlib import Path

from canolo_geo import coordinate_griglia, inversa

USCITA = Path(__file__).resolve().parent.parent / 'dati' / 'canolo_quote.json'
xs = coordinate_griglia()
punti = [(i, j) for j in range(len(xs)) for i in range(len(xs))]
quote = {}
if USCITA.exists():
    quote = {tuple(map(int, k.split(','))): v for k, v in json.loads(USCITA.read_text())['quote'].items()}
da_fare = [p for p in punti if p not in quote]
print(len(da_fare), 'punti da scaricare', flush=True)
for k in range(0, len(da_fare), 100):
    blocco = da_fare[k:k + 100]
    loc = '|'.join('{:.6f},{:.6f}'.format(*inversa(xs[i], xs[j])) for i, j in blocco)
    for tent in range(6):
        try:
            rq = urllib.request.Request('https://api.opentopodata.org/v1/eudem25m?locations=' + loc, headers={'User-Agent': 'uarewhatueat-design/1.0'})
            r = json.load(urllib.request.urlopen(rq, timeout=60))
            for p, e in zip(blocco, r['results']):
                quote[p] = e['elevation'] if e['elevation'] is not None else 0.0
            break
        except Exception as e:  # noqa: BLE001
            print('riprovo', k, repr(e)[:80], flush=True)
            time.sleep(3 * (tent + 1))
    else:
        sys.exit('troppi errori')
    USCITA.write_text(json.dumps({'n': len(xs), 'quote': {'{},{}'.format(*p): v for p, v in quote.items()}}, separators=(',', ':')))
    print(k + len(blocco), '/', len(da_fare), flush=True)
    time.sleep(1.2)
print('fatto')
