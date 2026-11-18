# ROADMAP

Stato al 17/11/2026. Legenda: [x] fatto, [~] fatto in parte, [ ] da fare.

## Motore

**Unity 6 (6000.2.6f2) con URP**, confermato. Alternative valutate: Godot 4 (leggero, ma si perderebbero codice, scena, asset Kenney/FSP e il flusso Rider) e Unreal 5 (troppo pesante per un gestionale con un tocco di horror, C++/Blueprint senza vantaggi qui). Unity ha già controller in prima persona, fisica degli oggetti, NavMesh e TextMesh, e l'editor è collegato via MCP. Nota: la pipeline è URP, non HDRP.

Verifiche senza aprire l'editor:
- `dotnet build tools/CompileCheck`: compila `Assets/src` contro le DLL di Unity.
- `dotnet run --project tools/LogicTests`: test della logica pura in `Assets/src/Core` (marchio, soldi, notti, finali).

## Architettura

- `Assets/src/Core`: logica pura senza UnityEngine (`GameState`, `MarkRules`, `Economy`, `Villager`, `Product`). Testabile.
- `Assets/src/Game`: sistemi Unity (mondo, clienti, flusso della giornata, dialoghi, atmosfera). `WorldAnchors` collega il mondo ai sistemi.
- `Assets/src/UI`: UI costruita da codice (`UIKit`: notifiche, menu a scelte).
- Il mondo si costruisce da codice (`GameBootstrap` + `WorldBuilder`) partendo da `negozio_layout.json`, così la scena non va montata a mano. Il menu «uarewhatueat/Cuoci il mondo nella scena» lo salva dentro `Game.unity` (mesh e materiali generati in `Game_Mondo.asset`) per vederlo e ritoccarlo nell'editor; in Play `GameBootstrap` non lo ricostruisce. Dopo modifiche a `WorldBuilder` va ricotto (perde i ritocchi a mano).

## Gioco: funzionalità essenziali
Fase 0, contratti (Opus):
- [x] Motore scelto, strumenti di verifica, contratti condivisi (`Core`, `WorldAnchors`, `Interactable`, `UIKit`).

Fase 1, sistemi base:
- [x] Regole del marchio, soldi senza bancarotta, presa notturna casuale e graduale, con test.
- [x] Catalogo prodotti anni 90 in lire (naturale, industriale, dolci, alcol, fumo) e paesani con abitudini.
- [x] Mondo costruito da codice: negozio da `negozio_layout.json`, casa, bar, passaggi tra i luoghi, scena di gioco.
- [x] Flusso della giornata: orologio fermo nei momenti di respiro, fasi (mattina, negozio, pranzo, negozio, sera, bar, notte), 7 giorni.
- [x] HUD: ora, giorno, soldi, prompt di interazione, notifiche.

Fase 2, gameplay:
- [x] Clienti: entrano, prendono prodotti dagli scaffali, fanno la fila, pagano alla cassa se il prezzo va bene. Quello che comprano finisce nella loro dieta.
- [x] Ordini al camion: menu nel magazzino, consegna la mattina dopo, industriale in offerta.
- [x] Dialoghi: bar la sera, cospirazionista, TG del mattino, avvertire i paesani (deriso), pezzi della scritta e decifrazione.
- [x] Figlia: cena scelta dal padre, dolci di nascosto, segnali (carte di caramelle, marchio sbiadito). Specchio per vedere la propria fronte.
- [x] Atmosfera: luci che tremolano, nebbia, la luce sopra la valle nelle notti di presa, vuoti lasciati dai presi.

Fase 3, chiusura:
- [x] Finali (perdita, salvo da soli, salvezza condivisa, amaro) e schermata finale.
- [x] Primo playtest nell'editor: menu «uarewhatueat/Crea scena di gioco», apri `Assets/Scenes/Game.unity`, Play. Giocati 7 giorni fino al finale, corretti Vittorio e Lucia mai attivi (id vs ruolo), segnali di Giulia coperti dal toast, finale se viene preso il padre, prompt E doppio.
- [~] Bilanciamento in `GlobalConfig`, giro completo di 7 giorni: Vittorio prendibile dalla notte 4, bere al bar facoltativo, caffè, offrire da bere 3000 lire, spese fisse 25000 lire/giorno, merce del camion col prezzo base, orologio che corre al bar, notte più buia. Da ritarare giocando con mouse e tastiera.
- [x] Includere lo shader «Universal Render Pipeline/Lit» e «Universal Render Pipeline/Terrain Lit» tra quelli sempre inclusi prima di fare una build (il mondo usa `Shader.Find`).
- [x] Tasto N ignorato con menu aperto/input bloccato.

Comandi in gioco: WASD e mouse, E usa e prende, clic sinistro posa, clic destro lancia, P prezzo dell'oggetto in mano, N passa alla fase dopo nei momenti di respiro, J quaderno degli indizi.

## Modelli 3D e mondo (cartella `design/modelli-3d`)
- [x] v1: schema di negozio, personaggi, paese, prodotti.
- [x] v2: texture procedurali, rilievo simulato, molto più dettaglio.
- [x] v3: punti ricevuti da Francesco il 16/11:
  - [x] Personaggi: controllo delle sovrapposizioni di mesh (`strumenti/controlla_mesh.py`) e correzione delle facce complanari (cinture, suole, grembiuli, fusciacca, spalle).
  - [ ] Personaggi: Francesco cerca i modelli. Quelli attuali restano segnaposto e **non vengono salvati come file** nella repo. Rimettere il marchio come decal sui modelli nuovi.
  - [x] Banco affettati dettagliato: vetrina con vassoi, mortadella e formaggi, prosciutto su morsa con coltello, bilancia con display, affettatrice (lama, protezione, carrello, affilatoio, manopola), carta oleata, coltelliera, prosciutti e salami appesi.
  - [x] Paese da codice (VillageDressing): chiesa con campanile, macelleria, fioraio, tabacchi, osteria, fontana, panchine, monumento, cabina SIP, auto, insegne; negozio esterno a due piani.
  - [x] Prodotti riconoscibili (ProductLook): bottiglie, scatole, sacchetti, salumi, etichette per industriale.
  - [~] Canolo Nuovo (RC) ricostruito sulla planimetria reale: rilievo EU-DEM, 148 edifici con la sagoma vera girati verso la strada, strade, recinti, bosco, serra, laghetto, chiesa «Madonna della Neve». In corso: Terrain da `dati/canolo_quote.json` e sagome edifici fuori dalla piazza.
    - [ ] Confermare quali edifici sono negozio, macelleria, osteria e negozio di piante (oggi scelti per vicinanza al centro).
    - [ ] Rifinire: tetti diversi per forma, balconi, muri a secco, vegetazione varia (castagni, ulivi), illuminazione notturna del paese.
    - [ ] Rilievo più fitto vicino al centro (oggi circa 15 m) e strade che seguono il terreno con scarpate.
- [x] v4: negozio con spazio per muoversi dietro il banco e la cassa e una parte di magazzino.
  - [x] Negozio largo 6 m (era 5): corridoio di 1 m dietro banco affettati e cassa, passaggio di 80 cm tra i due, porta del magazzino in fondo al corridoio.
  - [x] Magazzino 6 x 3,5 m: scaffali metallici con scatole, bancali con carico, sacchi, congelatore, carrello a mano, scrivania con registro, serranda per le consegne.
  - [x] Visore: camminata in prima persona (W A S D, frecce a schermo) con collisioni. `obj_v4/negozio_layout.json` elenca posizioni e ingombri per ricostruire il negozio in Unity.
  - [ ] Magazzino completo (cella frigorifera, zona carico e scarico) e scale verso un eventuale piano superiore.
  - [ ] Porte e passaggi interattivi: flap del banco, porta del magazzino apribile.
- [ ] Texture dipinte a mano al posto di quelle procedurali.
- [ ] Rielaborare i modelli per renderli meno «cozy» (usura, luci fredde, asimmetrie).
- [ ] Altri luoghi: cabina telefonica, ufficio postale, scuola, cimitero, funivia dismessa.

## Unity
- [~] Importare `.obj` e texture (`obj_v3/`): importati in `Assets/Resources/Models/v3` (caricati con Resources.Load) con texture (ObjMaterialFix), da sostituire alle primitive.
- [x] Rilievo come Terrain da `dati/canolo_quote.json`: il "Fuori" della piazza è Canolo vero (`Assets/src/Game/World/Canolo.cs`: Terrain 5 km, edifici estrusi, strade, recinti, bosco, serra, laghetto; trasformazione e crediti nel file e in `CREDITS.md`). Da verificare nell'editor: tetti, finestre, prestazioni, raccordo del terreno con la piazza.
- [ ] Pipeline URP: shader di usura, post-processing (vignetta, grana, nebbia), stile PS1 opzionale.
- [ ] Sostituire i segnaposto dei personaggi con i modelli scelti.

## Da fare ancora
- [x] Prova di magazzino (playtest-2): la porta era chiusa dalla scrivania (20 cm dietro il varco), scrivania spostata, varco 1,1 m, cardine a ovest, zona di scarico tra i bancali con registro ordini sulla scrivania. Presa e posa provate con i metodi veri di PlayerInteraction. Ancora da provare con mouse e tastiera veri.
- [x] Ritarate le spese e i margini: camion con sconto industriale 40% e ricarico naturale 12% (`GlobalConfig`). Sette giorni giocati da un bot: solo naturale = a pareggio (+4,6k lire/giorno dopo i fissi), molto industriale = +100k al giorno ma la figlia viene presa. Nota: il prezzo +20% dal menu P è sempre accettato, aggiungerebbe 20% al naturale senza rischio.
- [ ] Verificare leggibilità delle etichette dei prodotti.

## Dati e licenze
- Mappa: © OpenStreetMap contributors (ODbL). Quote: EU-DEM 25 m. Vedi `design/modelli-3d/dati/LEGGIMI.md`.
- I kit Kenney in `Assets/Prefab` sono CC0. Da verificare le licenze di `suburban` e `ResortTown` prima della distribuzione.
