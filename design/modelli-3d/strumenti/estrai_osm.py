#!/usr/bin/env python3
"""Converte dati/canolo.osm (OpenStreetMap) in metri locali: dati/canolo_osm.json."""
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path

from canolo_geo import proietta

D = Path(__file__).resolve().parent.parent / 'dati'
r = ET.parse(D / 'canolo.osm').getroot()
nodi = {n.get('id'): proietta(float(n.get('lat')), float(n.get('lon'))) for n in r.findall('node')}
out = {'edifici': [], 'strade': [], 'recinti': [], 'boschi': [], 'acque': [], 'serre': [], 'prati': [], 'punti': []}
for w in r.findall('way'):
    t = {x.get('k'): x.get('v') for x in w.findall('tag')}
    pts = [nodi[n.get('ref')] for n in w.findall('nd') if n.get('ref') in nodi]
    if len(pts) < 2:
        continue
    pts = [[round(x, 2), round(z, 2)] for x, z in pts]
    base = {'id': int(w.get('id')), 'pts': pts, 'tag': {k: v for k, v in t.items() if k in ('building', 'building:levels', 'name', 'amenity', 'highway', 'surface', 'landuse', 'natural', 'barrier', 'roof:shape', 'height')}}
    if 'building' in t:
        out['edifici'].append(base)
    elif 'highway' in t:
        out['strade'].append(base)
    elif t.get('barrier') == 'fence':
        out['recinti'].append(base)
    elif t.get('landuse') == 'forest' or t.get('natural') in ('wood', 'scrub'):
        out['boschi'].append(base)
    elif t.get('natural') == 'water':
        out['acque'].append(base)
    elif t.get('landuse') == 'greenhouse_horticulture':
        out['serre'].append(base)
    elif t.get('landuse') in ('grass', 'meadow'):
        out['prati'].append(base)
    if t.get('amenity'):
        out['punti'].append(base)
(D / 'canolo_osm.json').write_text(json.dumps(out, separators=(',', ':')))
xs = [p[0] for e in out['edifici'] for p in e['pts']]
zs = [p[1] for e in out['edifici'] for p in e['pts']]
print({k: len(v) for k, v in out.items()})
print('edifici: x', round(min(xs)), round(max(xs)), 'z', round(min(zs)), round(max(zs)))
for p in out['punti']:
    print('punto', p['tag'], p['pts'][0], len(p['pts']))
for e in out['edifici']:
    if 'name' in e['tag'] or 'building:levels' in e['tag'] or e['tag'].get('building') not in ('yes', 'house'):
        print('edificio', e['tag'])
