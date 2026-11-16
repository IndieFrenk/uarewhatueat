"""Geografia di Canolo Nuovo (RC): proiezione in metri e griglia del rilievo.
Dati: (c) OpenStreetMap contributors (ODbL); quote EU-DEM 25 m tramite opentopodata.org."""
import math

LAT0, LON0 = 38.33217225177128, 16.159237364767357   # centro indicato da Francesco
R_TERRENO, A_GRIGLIA, N_GRIGLIA = 5000.0, 0.12, 80
KX, KZ = 111320.0 * math.cos(math.radians(LAT0)), 110574.0


def proietta(lat, lon):
    """(x, z) in metri: x verso est, z verso sud (il nord è -z)."""
    return (lon - LON0) * KX, -(lat - LAT0) * KZ


def inversa(x, z):
    return LAT0 - z / KZ, LON0 + x / KX


def coordinate_griglia():
    """Linee della griglia: fitte al centro (~15 m), larghe lontano (~300 m)."""
    out = []
    for i in range(N_GRIGLIA + 1):
        t = -1 + 2 * i / N_GRIGLIA
        out.append(R_TERRENO * (A_GRIGLIA * t + (1 - A_GRIGLIA) * t * abs(t)))
    return out
