using System.Collections.Generic;

/// <summary>
/// Tutti i testi della storia: bar, Vittorio, TG del mattino, appunti del quaderno.
/// Dati puri, nessuna logica di gioco. Tono: anni 90, montagna, malinconia, mai splatter.
/// </summary>
public static class StoryTexts
{
    // ---------------------------------------------------------------- BAR: chiacchiere
    // Per paesano, tre atti. (battuta, appunto sul quaderno). La battuta dice la cosa nuova dell'atto.

    static readonly Dictionary<string, (string chat, string note)[]> chats = new Dictionary<string, (string chat, string note)[]>
    {
        ["parroco"] = new[]
        {
            ("Don Mario fa girare il bicchiere di vino bianco. «Hai visto Rocco? Sulla fronte ha come l'impronta di un dito, scura. Io dico che è una benedizione, figliolo. Il Signore segna chi gli sta a cuore.»",
             "Don Mario crede che il segno in fronte sia una benedizione: Dio segna chi gli sta a cuore."),
            ("«Non è mai capitato che il Signore chiamasse chi porta il segno. Solo gli altri. Strano, no? Io non lo dico dal pulpito, ma ci penso la notte.»",
             "Don Mario ha notato che vengono presi solo quelli senza segno. Per lui resta un mistero della fede."),
            ("«Domenica ho letto il Vangelo a sei persone e a una sedia. Dicono paradiso, e io ci credo ancora... ma non ho più il coraggio di guardare i bambini in fronte.»",
             "Don Mario non ha più il coraggio di guardare i bambini in fronte: forse non crede più al paradiso."),
        },
        ["barista"] = new[]
        {
            ("Tonino asciuga lo stesso bicchiere da dieci minuti. «Fino a poco fa a quest'ora il bar era pieno. Adesso la gente compra le patatine dal camion e se le mangia a casa, da sola.»",
             "Tonino dice che la gente compra sempre più merce confezionata, quella del camion, e mangia da sola a casa."),
            ("«Sai cosa ho notato? Chi beve con me, il segno ce l'ha sempre più chiaro. Io ci guadagno, ma ogni tanto mi chiedo se non faccio un torto a qualcuno.»",
             "Tonino ha notato che il segno si schiarisce a chi beve. Gli sembra quasi di fare un torto ai clienti."),
            ("«Chiudo prima, la sera. Dopo le nove il silenzio non è più quello della montagna. È un silenzio che ascolta, capisci?»",
             "Tonino chiude prima del solito: dice che dopo le nove il silenzio «ascolta»."),
        },
        ["macellaio"] = new[]
        {
            ("Rocco batte il pugno sul bancone. «Io il segno ce l'ho e dormo tranquillo. Tutti a dire stai attento, stai attento. A me, che mi vengano a prendere? Non si tocca chi è segnato!»",
             "Rocco si sente intoccabile perché ha il segno in fronte."),
            ("«Ieri allo specchio mi sembrava meno scuro. Sarà la luce. Sarà la luce, no? Dimmi che è la luce.»",
             "Rocco ha visto il proprio segno schiarirsi e non è più sicuro di essere al riparo."),
            ("«Ho tolto la carne dalla vetrina. Di notte la cella frigorifera fa un rumore come di respiro. Dicono che sono matto. Non è la cella che mi fa paura.»",
             "Rocco ha tolto la carne dalla vetrina: di notte la sua cella frigorifera fa un rumore come di respiro."),
        },
        ["fioraia"] = new[]
        {
            ("Assunta si infila un rametto di timo nel taschino. «Ti ho lasciato un cesto di verdure davanti al negozio. Fa' mangiare la bambina, mi raccomando. Il corpo si ricorda di quello che gli dai.»",
             "Assunta dice che il corpo «si ricorda di quello che gli dai» e regala verdure dell'orto."),
            ("«Chi mangia dall'orto ha il segno più scuro. L'ho visto su mia sorella e su altri due. Non so cosa voglia dire. Tu che vendi le scatole, guarda cosa compra la gente.»",
             "Assunta ha notato che chi mangia dall'orto tiene il segno più scuro, e suggerisce di guardare cosa compra la gente."),
            ("«Ormai regalo tutto, tanto clienti non ne ho più. Prendi pure i pomodori, è l'ultima serra. E non lasciare che la bambina mangi le schifezze nelle carte colorate.»",
             "Assunta regala le ultime verdure e raccomanda di non far mangiare alla bambina i dolci nelle carte colorate."),
        },
        ["oste"] = new[]
        {
            ("Peppe si pulisce le mani sul grembiule. «Polenta, cinghiale, grasso fino ai gomiti: roba vera. Mio nonno mangiava così e arrivò a novant'anni. Se uno deve andarsene, se ne va lo stesso.»",
             "Peppe è convinto che il cibo vero, anche pesante, non faccia male."),
            ("«Strano: i clienti che mangiano da me ogni giorno il segno ce l'hanno tutti. Quelli che vivono di merendine... meno. Ma sarà una coincidenza. Sarà una coincidenza.»",
             "Peppe: chi mangia ogni giorno da lui conserva il segno, chi vive di merendine no. Il cibo grasso ma di casa sembra proteggere."),
            ("«Ho dato da mangiare a tutti, anche a chi non poteva pagare. Se devo andare, vado con la pancia piena. È una bella morte, non trovi?»",
             "Peppe offre da mangiare a tutti, anche gratis: dice che è meglio andarsene con la pancia piena."),
        },
        ["vecchia"] = new[]
        {
            ("Caterina non ordina niente. Tiene le mani sulla borsa. «Io non compro da nessuno, vivo dell'orto. Mia nonna diceva che ogni tanto la montagna chiede il suo conto. Non so cosa intendesse.»",
             "Caterina dice che sua nonna parlava di una montagna che «chiede il suo conto». Lei vive solo dell'orto."),
            ("«Il segno non l'ho mai avuto e non mi hanno presa. Mangio quello che coltivo, sempre le stesse cose. Dimmi tu che hai studiato: a me non torna niente.»",
             "Caterina non ha mai avuto il segno e non l'hanno presa: il suo caso non torna con nessuna teoria."),
            ("«Quando passa la luce sulla valle io chiudo le imposte e dico il rosario. Non per Dio. Per rispetto. Non si guarda chi mangia.»",
             "Caterina chiude le imposte quando passa la luce sulla valle: «non si guarda chi mangia»."),
        },
        ["lucia"] = new[]
        {
            ("Lucia fa un tiro e soffia il fumo di lato, per non farlo arrivare a te. «La tua Giulia è sveglia, sai. Ma disegna cose strane sul quaderno. Segni, righe. L'accompagno io a scuola, tranquillo.»",
             "Lucia dice che Giulia disegna segni strani sul quaderno."),
            ("«A scuola eravamo sette bambini. Adesso cinque. Nessuno dice niente, i genitori fanno finta. Le carte delle caramelle le trovo anche sotto i banchi. I bambini mangiano di nascosto.»",
             "A scuola i bambini sono sempre meno e Lucia trova carte di caramelle anche sotto i banchi."),
            ("«Ho smesso di fumare. Due giorni. Poi ho riacceso. Se devo andare, vado col mio vizio. Ma Giulia no: Giulia deve mangiare roba vera. Promettimelo.»",
             "Lucia mi ha fatto promettere che Giulia mangerà cibo vero, non industriale."),
        },
    };

    static readonly string[] idle =
    {
        "Ha già detto tutto quello che sapeva, per stasera. Guarda il bicchiere e tace.",
        "«Parlare fa bene, ma non cambia le cose.» Poi si volta verso la porta, come se aspettasse qualcuno.",
        "Scrolla le spalle. Nel bar si sente solo il frigo del banco e l'orologio sopra gli scaffali.",
    };

    public static bool HasChat(string id) => chats.ContainsKey(id);
    public static string Chat(string id, int act) => chats[id][act - 1].chat;
    public static string ChatNote(string id, int act) => chats.TryGetValue(id, out var c) ? c[act - 1].note : null;
    public static string Idle(int n) => idle[n % idle.Length];

    // ---------------------------------------------------------------- BAR: cornice

    public static string Opener(int act, string name)
    {
        switch (act)
        {
            case 1: return $"Ti siedi al bancone accanto a {name}. Il bar sa di caffè bruciato e di sigarette spente. Fuori fa buio presto, in montagna.";
            case 2: return $"{name} ti fa spazio senza alzare gli occhi. Nel bar si parla piano, come in chiesa.";
            default: return $"{name} ti guarda come si guarda uno che domani potrebbe non esserci. Il bar è più grande di prima: troppi sgabelli vuoti.";
        }
    }

    public static string MarkNarration(string name, bool visible) => visible
        ? $"Sulla fronte di {name} il segno è nitido: una macchia scura, con simboli minuscoli, come graffi di punta di penna."
        : $"La fronte di {name} è liscia. Pulita come quella di un bambino. E ti mette a disagio.";

    public static string EmptyStool(string who) => $"Lo sgabello di {who} è rimasto vuoto. Nessuno ci si è seduto.";

    /// <summary>Cosa dice un paesano di chi è sparito. who = ultimo preso, count = quanti mancano.</summary>
    public static string MissingLine(string id, string who, int count, int act)
    {
        if (id == "parroco")
        {
            if (act == 1) return $"«{who} è stato chiamato al cielo. Preghiamo per lui. E per noi, che restiamo.»";
            if (act == 2) return $"«{count} chiamati, ormai. Il Signore sceglie con un criterio che non capiamo. Ma c'è un criterio, figliolo, c'è.»";
            return $"«{count} sono andati. Mi chiedo ormai se a chiamarli sia proprio Lui.»";
        }
        if (act == 1) return $"«Hai saputo di {who}? Sparito. Il letto rifatto, la tavola apparecchiata, e lui non c'era più.»";
        if (act == 2) return $"«Dopo {who} siamo a {count} vuoti in paese. Dicono che sono andati in paradiso. Io dico che in paradiso non si va senza le scarpe.»";
        return $"«{who}... {count} sedie vuote, ormai. Questo non è più un paese, è una sala d'aspetto.»";
    }

    // ---------------------------------------------------------------- BAR: azioni

    public static string Derision(string id, string name)
    {
        switch (id)
        {
            case "parroco": return "Don Mario ti posa una mano sulla spalla. «Figliolo, il dolore ti mostra ombre. Il Signore non mangia nessuno. Va' a letto, e domenica vieni a Messa.»";
            case "barista": return "Tonino ride e ti riempie il bicchiere. «Mangiati! Ma sentitelo. Offre la casa, così la smetti di dire stupidaggini.»";
            case "macellaio": return "Rocco ride così forte che il bancone trema. «Mangiati! A me? Ragazzi, il bottegaio dice che ci mangiano!» Ride ancora, ma un po' meno di prima. Gli altri ridono con lui, e a te viene da guardare il pavimento.";
            case "fioraia": return "Assunta ti guarda a lungo. «Sei stanco, ti vedo.» Non ti dà torto, ma nemmeno ragione. Poi cambia discorso, e tutti fanno finta che tu non abbia parlato.";
            case "oste": return "Peppe sbuffa. «Mangiati! Qui l'unico che mangia sono io, e non ho mai fatto male a nessuno. Siediti, che ti metto qualcosa nel piatto.» Ridono tutti. Tu non hai fame.";
            case "vecchia": return "Caterina scuote la testa. «Hai bevuto troppo, bottegaio. Va' a casa dalla bambina.» Ti fa male, perché in parte ha ragione.";
            case "lucia": return "Lucia spegne la sigaretta. «Ti capisco, ti hanno tolto tua moglie. Ma non dire queste cose davanti ai bambini. Mi fai paura.»";
            default: return $"{name} ride. «Ti sei bevuto il cervello, bottegaio?» Qualcuno al tavolo alza il bicchiere alla tua salute.";
        }
    }

    public static string Heeded(string id) => id == "fioraia"
        ? "Assunta resta in silenzio. «Mia madre diceva che il corpo ascolta quello che gli dai.» Ti stringe il braccio. «Domani passa dalla serra, ti do le cose buone. E ne tengo da parte anche per me.»"
        : "Lucia guarda la brace della sigaretta. «Mi stai dicendo di dare ai bambini le mele invece delle caramelle? Mi stai dicendo...» Non finisce la frase. «Domani porto il pane di Assunta in classe. Solo per sicurezza.»";

    public static string AlreadyWarned(string name) => $"{name} ti ha già sentito. «Basta, adesso. Non rovinare la serata a tutti.»";

    public static string Drink(int n, int act, string name)
    {
        if (n >= 2) return $"{name} copre il bicchiere con la mano. «Basta così. Domani apri il negozio, e la bambina ti aspetta.»";
        string mood = act == 1 ? "Un bicchiere in due non fa male a nessuno."
                    : act == 2 ? "Il secondo bicchiere è sempre più facile del primo."
                    : "Bevete per non ascoltare il silenzio.";
        return $"Fai portare da bere a {name} e paghi tu. Lui alza il bicchiere verso di te. {mood}\n\nPer un attimo gli pare di stare meglio. Poi il bicchiere è vuoto, e il vuoto è sempre quello.";
    }

    public const string NoMoney = "Frughi nelle tasche: non hai abbastanza lire per offrire un giro.";
    public const string Coffee = "Ordini un caffè. È amaro e bollente, e ti tiene sveglio. Va bene così.";
    public const string OwnDrinkEnough = "Il bicchiere resta sul banco. Per stasera hai bevuto abbastanza.";
    public static string OwnDrink(int act) =>
        act == 1 ? "Ordini un bicchiere di rosso. Un bicchiere non ha mai fatto male a nessuno."
        : act == 2 ? "Ordini un altro bicchiere. La mano non trema, ma hai smesso di contarli."
        : "Ordini da bere. Il bicchiere è l'unica cosa, qui dentro, che non ti guarda.";

    public static string LookMark(string name) =>
        $"Aspetti che {name} si volti verso la lampada. Sotto i capelli, sulla fronte, ci sono simboli sottili come graffi. Ne copi uno sul tovagliolo senza sapere perché. Trovi un altro pezzo della scritta.";

    // ---------------------------------------------------------------- VITTORIO

    public const string ConspIntro =
        "In fondo al bar, sotto la mensola con la radio spenta, un uomo in giaccone militare scrive su un quaderno a quadretti. Tutti lo evitano con lo sguardo: è Vittorio, quello che parlava con il mondo intero dalla soffitta, prima che il mondo smettesse di rispondere.\n\nTi fa cenno di sederti. «Finalmente. Uno che guarda le fronti.»";

    public const string ConspBody =
        "Vittorio ha steso sul tavolo il quaderno, due ritagli di giornale e un pezzo di carta velina. Il caffè davanti a lui è freddo da un'ora. Parla piano, senza guardare nessuno.";

    public static string ConspClueId(int day) => day <= 3 ? "consp_lights" : day == 4 ? "consp_stamp" : day == 5 ? "consp_truck" : day == 6 ? "consp_letters" : "consp_label";

    public static string Theory(int day)
    {
        switch (ConspClueId(day))
        {
            case "consp_lights":
                return "«Vent'anni a girare la manopola delle onde corte, e ti dico cos'è quella luce sulla valle: non è un fenomeno atmosferico. Passa a controllare. Di sera, solo di sera. E porta via chi ha la fronte pulita. Solo il martedì e il sabato, secondo i miei conti.»";
            case "consp_stamp":
                return "«Il segno è un timbro. Come quello sanitario sulle carni, hai presente? Quello tondo, viola, che dice che la bestia è passata dal controllo. Lo avrà messo il Ministero, ci scommetto, con un'autorizzazione che nessuno ha mai letto.»";
            case "consp_truck":
                return "«Ho seguito il camion col binocolo dalla strada di sopra. Non porta solo le consegne: ogni settimana più roba confezionata, a metà prezzo. Nessuno la compra così se non per ingrassare un vitello. E chi la mangia perde il segno. L'autista è uno di loro, sicuro.» Abbassa la voce. «E chi la mette sugli scaffali, bottegaio? Chi la vende ai nostri figli?»";
            case "consp_letters":
                return "«Ho messo insieme i calchi. Non sono simboli, sono lettere. Un alfabeto storto, ma lettere. La prima parola è NON. La seconda comincia per C, e credo sia 'condannato'. O 'confermato'. Ci sono altre lettere che non riconosco.»";
            default:
                return "«Ho sbagliato quasi tutto, ma una cosa l'ho capita. Il segno non serve a proteggerci. È un'etichetta, bottegaio. È scritta per chi legge dall'altra parte. E noi non siamo i lettori.»";
        }
    }

    public const string ConspFragmentGiven =
        "Vittorio ti passa un foglio piegato in quattro. «Un calco, fatto con la matita. Lo tengo da dieci giorni senza sapere cosa farne. Tienilo tu.» Trovi un altro pezzo della scritta.";

    public const string ConspFragmentAgain = "«Quello te l'ho già dato. Ti ricordi cosa c'era scritto?» Scuote la testa.";

    public const string ConspFragmentNone =
        "«Oggi non ho altro. Torna con quello che vedi, e confrontiamo. Le fronti, bottegaio: guardale con calma, quando nessuno ti vede.»";

    public static bool ConspGivesFragment(int day) => day == 3 || day == 5 || day == 6;

    // ---------------------------------------------------------------- QUADERNO: appunti

    public static string ConspNote(string id)
    {
        switch (id)
        {
            case "consp_met": return "Ho conosciuto Vittorio, l'ex radioamatore. Tutti lo credono pazzo, ma forse ha ragione su molto. I dettagli vanno verificati.";
            case "consp_lights": return "Vittorio: la luce sulla valle passa a controllare e porta via chi ha la fronte pulita. I giorni precisi che dà forse sono sbagliati.";
            case "consp_stamp": return "Vittorio: il segno è un timbro, come quelli sulle carni. Sul Ministero probabilmente sbaglia.";
            case "consp_truck": return "Vittorio ha seguito il camion: porta sempre più merce confezionata a prezzi bassissimi, e chi la mangia perde il segno. Il mio negozio è l'ultimo anello.";
            case "consp_letters": return "Vittorio: i simboli sono lettere. La prima parola è «non», la seconda comincia per C (sul resto non è sicuro).";
            case "consp_label": return "Vittorio: il segno non serve a noi. È un'etichetta scritta per chi guarda dall'altra parte.";
            default: return null;
        }
    }

    // ---------------------------------------------------------------- TG DEL MATTINO
    // Sette notiziari, due pagine ciascuno. Lire, sigle, segnale che va e viene.

    static readonly string[][] tg =
    {
        new[] {
            "(Sigla. Il conduttore ha la giacca marrone, la scrivania è di legno finto.)\n\nBuongiorno. Il Governo conferma la manovra: ritoccate le accise, il pieno costerà in media quattromila lire in più. I sindacati minacciano lo sciopero. E c'è già polemica per il prezzo del pane, aumentato di cinquanta lire in alcune regioni.\n\nIn campionato, la Nazionale prepara l'amichevole di mercoledì.",
            "Dalle province. Grande distribuzione: da questa settimana arrivano nei piccoli centri delle zone interne le nuove confezioni famiglia, pasti pronti e merendine a prezzi promozionali. «Un'occasione per le botteghe isolate», dice il portavoce.\n\n(L'immagine ondeggia, il suono si strappa.)\n\nPrevisioni: nuvolosità in aumento sui rilievi. Buona giornata.",
        },
        new[] {
            "(Sigla. Il volume salta, poi ritorna.)\n\nApertura: la lira resta sotto pressione. Il ministro rassicura: «nessuna svalutazione». Intanto il Totocalcio premia un solo tredici: montepremi di quasi due miliardi.\n\nCronaca: ritrovati sani e salvi due escursionisti dispersi nei boschi dell'entroterra.",
            "Ambiente. Alcuni osservatori segnalano strane luci nelle vallate interne, nelle ore notturne. La Protezione civile parla di riflessi, forse di un fenomeno atmosferico. (Fruscio.) «...nessun motivo di preoccupazione...»\n\nDalle province, oggi si passa direttamente allo sport. Nessun servizio dalle vostre zone.",
        },
        new[] {
            "(Sigla. Il conduttore tossisce e riprende.)\n\nSciopero dei treni di otto ore, disagi per i pendolari. Bollette in aumento. Nello spettacolo: la serata del sabato ha battuto ogni record, dodici milioni di spettatori.\n\nEconomia: boom dei prodotti confezionati, calo del fresco specie nei mercati rionali. «Ormai si compra quello che dura», dice un esperto in studio.",
            "Il gruppo industriale annuncia consegne straordinarie ai negozi delle località isolate: sconti fino al quaranta per cento su dolciumi e bevande, «per sostenere i piccoli esercenti».\n\n(Il segnale va via. Resta il fruscio. Poi, per un attimo, una voce di donna che non è quella del TG: «...non guardare...». Poi il notiziario ricomincia, come se niente fosse.)",
        },
        new[] {
            "(Sigla. L'immagine è più scura del solito.)\n\nIn primo piano le tasse sulla casa. Poi i consumi: dolci in confezione in crescita del dodici per cento. «Mangiamo sempre più in fretta», commenta un esperto, «e sempre meno cose che marciscono».\n\nIn Europa niente di nuovo. A Roma, polemiche sul Festival.",
            "Dalle zone montane: alcuni piccoli comuni risultano muti da giorni. Si fanno i nomi di tre, quattro paesi, uno dopo l'altro. La prefettura parla di «accertamenti di routine». (Il segnale cade e torna.) Si passa alle previsioni.\n\nNessuno ha nominato il vostro paese. Nemmeno per sbaglio.",
        },
        new[] {
            "(Sigla. Il conduttore sembra non aver dormito.)\n\nMaltempo: neve sopra i mille metri, strade interrotte nelle zone interne. I soccorsi invitano a restare in casa. I rifornimenti saranno garantiti «solo lungo le direttrici principali».\n\nE intanto il caro-benzina fa litigare maggioranza e opposizione.",
            "Industria. Il gruppo conferma che anche la prossima settimana le forniture ai piccoli negozi saranno aumentate: «quantità doppie, a prezzi di favore». L'inviato chiede: «Ma perché tanta generosità?» Risposta del portavoce, a mezza voce: «Perché ci conviene che tutti...»\n\n(Il segnale cade proprio qui. Il resto della frase non si sente.)",
        },
        new[] {
            "(Sigla. Edizione ridotta per un guasto al ripetitore.)\n\nGoverno: fiducia incassata. Lira stabile. Sport: la Nazionale vince due a uno, e nei bar di città si applaude. Da voi, immagino, no.\n\n(Il conduttore si corregge, sorride a metà. «Da voi» non era nel copione.)",
            "Un appello: «chi avesse notizie di familiari residenti nei comuni interni è pregato di rivolgersi alla prefettura, telefonando al numero...» (Il suono si spegne a metà cifra. L'immagine resta, senza voce, per un lungo minuto. Poi tornano le previsioni, come se nessuno avesse parlato.)",
        },
        new[] {
            "(Ultimo notiziario. La sigla parte storta, senza musica.)\n\nA reti unificate: la situazione nei comuni interni è «sotto controllo». Nessun cittadino segnalato come disperso. Le autorità ringraziano la popolazione per la collaborazione.\n\n(Il conduttore guarda un punto fuori inquadratura. Poi guarda te. Ti sembra che guardi proprio te.)",
            "«Buon appetito.»\n\nIl segnale cade. Rimane solo la neve sullo schermo e un fruscio lungo, come un respiro. Spegni la TV prima che passi.",
        },
    };

    static readonly string[] tgNotes =
    {
        "TG: i grandi gruppi portano nei paesini di montagna pasti pronti e merendine a prezzi da saldo.",
        "TG: strane luci nelle vallate interne, di notte. Per le autorità sono riflessi. Nessun servizio dal nostro paese.",
        "TG: sconti fino al 40% su dolci e bevande per i negozi isolati. Nel fruscio, per un attimo, una voce che non era del TG.",
        "TG: alcuni comuni di montagna non danno più notizie. Prefettura: «accertamenti di routine». Il nostro paese non è mai nominato.",
        "TG: forniture doppie e a prezzi di favore ai piccoli negozi. Il portavoce dice «ci conviene che tutti...» e il segnale cade.",
        "TG: un appello per chi cerca notizie dei familiari nei comuni interni, interrotto a metà. Del mio paese, nulla.",
        "TG: «sotto controllo», e il conduttore sembra guardare me. Dice: «buon appetito».",
    };

    public static string[] Tg(int day) => tg[System.Math.Max(1, System.Math.Min(day, tg.Length)) - 1];
    public static string TgNote(int day) => tgNotes[System.Math.Max(1, System.Math.Min(day, tgNotes.Length)) - 1];
}
