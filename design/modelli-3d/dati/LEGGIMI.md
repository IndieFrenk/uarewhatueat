# Dati di Canolo Nuovo

- `canolo.osm`: estratto di OpenStreetMap attorno a 38.33217, 16.15924 (scaricato con la API 0.6, bbox 16.1535,38.3290,16.1650,38.3355).
- `canolo_osm.json`: gli stessi dati in metri locali (x verso est, z verso sud), generati da `strumenti/estrai_osm.py`.
- `canolo_quote.json`: quote del terreno (EU-DEM 25 m tramite opentopodata.org) su una griglia fitta al centro (circa 15 m) e larga lontano (circa 300 m), fino a 5 km. Generato da `strumenti/scarica_quote.py`.

Dati della mappa: © OpenStreetMap contributors, licenza ODbL (https://www.openstreetmap.org/copyright). Chi riusa o ridistribuisce questi dati deve mantenere l'attribuzione.
