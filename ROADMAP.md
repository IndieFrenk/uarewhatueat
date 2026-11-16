# ROADMAP

Stato al 16/11/2026. Legenda: [x] fatto, [~] fatto in parte, [ ] da fare.

## Gioco
- [x] Concept: gestionale di un supermercato con un tocco di horror (`CLAUDE.md`).
- [x] Trama scritta e approvata (`TRAMA.md`): paese di montagna anni 90, marchio «non commestibile», finali legati al cibo.
- [ ] Pausa dell'orologio di gioco nei momenti di respiro (prima di aprire, al bar, esplorando il paese). Oggi `TimeManager` scorre sempre.
- [ ] Orari reali: apertura, chiusura pranzo, riapertura, bar la sera. Il codice c'è già in `GlobalConfig`.
- [ ] Clienti, cassa e coda (`NPCManager` è vuoto).
- [ ] Soldi: risparmi iniziali X, guadagni che calano quando si smette di vendere industriale, saldo mai sotto zero.
- [ ] Sistema del marchio: cibo industriale, alcol e fumo lo tolgono, il naturale lo mantiene. Presa casuale e graduale.
- [ ] Il cospirazionista, il bar, il TG, la scritta da decifrare.
- [ ] Finali (perdita, salvo da soli, salvezza condivisa, amaro). Durata: circa una settimana di gioco, 1-2 ore.

## Modelli 3D e mondo (cartella `design/modelli-3d`)
- [x] v1: schema di negozio, personaggi, paese, prodotti.
- [x] v2: texture procedurali, rilievo simulato, molto più dettaglio.
- [x] v3: punti ricevuti da Francesco il 16/11:
  - [x] Personaggi: controllo delle sovrapposizioni di mesh (`strumenti/controlla_mesh.py`) e correzione delle facce complanari (cinture, suole, grembiuli, fusciacca, spalle).
  - [ ] Personaggi: Francesco cerca i modelli. Quelli attuali restano segnaposto e **non vengono salvati come file** nella repo. Rimettere il marchio come decal sui modelli nuovi.
  - [x] Banco affettati dettagliato: vetrina con vassoi, mortadella e formaggi, prosciutto su morsa con coltello, bilancia con display, affettatrice (lama, protezione, carrello, affilatoio, manopola), carta oleata, coltelliera, prosciutti e salami appesi.
  - [~] Canolo Nuovo (RC) ricostruito sulla planimetria reale: rilievo EU-DEM, 148 edifici con la sagoma vera girati verso la strada, strade, recinti, bosco, serra, laghetto, chiesa «Madonna della Neve».
    - [ ] Confermare quali edifici sono negozio, macelleria, osteria e negozio di piante (oggi scelti per vicinanza al centro).
    - [ ] Rifinire: tetti diversi per forma, balconi, muri a secco, vegetazione varia (castagni, ulivi), illuminazione notturna del paese.
    - [ ] Rilievo più fitto vicino al centro (oggi circa 15 m) e strade che seguono il terreno con scarpate.
- [ ] Texture dipinte a mano al posto di quelle procedurali.
- [ ] Rielaborare i modelli per renderli meno «cozy» (usura, luci fredde, asimmetrie).
- [ ] Altri luoghi: cabina telefonica, ufficio postale, scuola, cimitero, funivia dismessa.

## Unity
- [ ] Importare `.obj` e texture (`obj_v3/`), impostando le texture in modalità Repeat.
- [ ] Rilievo come Terrain da `dati/canolo_quote.json`.
- [ ] Pipeline URP: shader di usura, post-processing (vignetta, grana, nebbia), stile PS1 opzionale.
- [ ] Sostituire i segnaposto dei personaggi con i modelli scelti.

## Dati e licenze
- Mappa: © OpenStreetMap contributors (ODbL). Quote: EU-DEM 25 m. Vedi `design/modelli-3d/dati/LEGGIMI.md`.
- I kit Kenney in `Assets/Prefab` sono CC0. Da verificare le licenze di `suburban` e `ResortTown` prima della distribuzione.
