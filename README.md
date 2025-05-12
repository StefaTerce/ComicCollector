# ComicCollector

## Descrizione del progetto
**ComicCollector** è un’applicazione web basata su ASP.NET Core 6.0 (Razor Pages) pensata per aiutare gli appassionati di fumetti e manga a gestire la propria collezione personale. Grazie all’integrazione con API esterne (ComicVine e MangaDex) e all’uso dell’AI (Google Gemini), l’utente può cercare titoli, aggiungerli al proprio catalogo con dati precompilati e ottenere raccomandazioni e riepiloghi di pregi e difetti di ogni volume.

---

## Problema che risolve
Gestire una collezione di fumetti e manga in formato cartaceo o digitale può diventare complesso:
- Inserire manualmente tutti i dati richiede tempo e può causare errori.
- Tenere traccia dei titoli già letti e di quelli da recuperare è disorganizzato.
- Scoprire nuovi fumetti o manga in linea con i propri gusti non è immediato.

**ComicCollector** risolve questi punti offrendo:
- Una piattaforma centralizzata per catalogare la collezione.
- Recupero automatico di informazioni da ComicVine e MangaDex per ridurre l’inserimento manuale.
- Consigli personalizzati basati sull’intelligenza artificiale.
- Analisi dei punti di forza e di debolezza di ciascun titolo.

---

## Competitor
- **MyAnimeList** (principale per i manga/anime, ma con funzioni di catalogazione non focalizzate su fumetti occidentali).
- **Goodreads** (ottimo per libri, ma meno specializzato su fumetti e senza AI integrata).
- **ComiXology** (piattaforma di acquisto digitale, non un catalogatore personale).
- **ComicBookDB** (catalogazione, ma con interfaccia obsoleta e nessuna integrazione AI).

---

## Target di utenti
- Collezionisti di fumetti e manga (sia casual che esperti).
- Lettori che vogliono scoprire nuovi titoli in base ai loro gusti.
- Sviluppatori interessati a esempi di integrazione API e AI in applicazioni .NET.
- Chiunque desideri un catalogo digitale ben strutturato e arricchito da dati completi e raccomandazioni intelligenti.

---

## Caratteristiche principali
- **Autenticazione**  
  Registrazione e login sicuri tramite ASP.NET Core Identity.

- **Gestione collezione**  
  - Aggiunta manuale di fumetti/manga oppure tramite ricerca API.  
  - Visualizzazione con paginazione e statistiche (numero di serie, autori, editori).  
  - Modifica e cancellazione degli elementi.

- **Pagina “Scopri”**  
  - Ricerca in ComicVine e MangaDex per nuovi titoli.  
  - Contenuti in evidenza o di tendenza.  
  - Aggiunta rapida dei risultati al catalogo personale.

- **Funzionalità AI (Google Gemini)**  
  - **Raccomandazioni personalizzate** basate sulla collezione esistente.  
  - **Arricchimento dati** (autore, casa editrice, trama, data di pubblicazione).  
  - **Riepiloghi recensione**: pro e contro generati automaticamente.

- **Interfaccia responsive**  
  Basata su Bootstrap 5, compatibile con dispositivi desktop e mobile.

- **Logging & Sicurezza**  
  - Log delle operazioni con `ILogger`.  
  - Gestione sicura delle chiavi API tramite User Secrets.

---

## Tecnologie utilizzate
- **Backend**  
  - ASP.NET Core 6.0 (Razor Pages)  
  - C#  
  - Entity Framework Core + SQL Server  
  - ASP.NET Core Identity  

- **Frontend**  
  - HTML5, CSS3  
  - Bootstrap 5  
  - JavaScript (jQuery, AJAX)  

- **API & Servizi**  
  - ComicVine API  
  - MangaDex API  
  - Google Gemini API  

- **Altro**  
  - HttpClient per chiamate HTTP  
  - Session Management  
  - Microsoft.Extensions.Logging  

---

## Installazione e avvio
1. **Clona il repository**  
   ```bash
   git clone https://github.com/StefaTerce/ComicCollector.git
   cd ComicCollector
   ```

2. **Configura le chiavi API**  
   Imposta le tue chiavi in [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (`ComicVine`, `MangaDex`, `GoogleGemini`).

3. **Aggiorna il database**  
   ```bash
   dotnet ef database update
   ```

4. **Esegui l’app**  
   ```bash
   dotnet run
   ```

5. Apri il browser e vai su `https://localhost:7225`.

---

## Contribuire
1. Forka il progetto  
2. Crea un branch per la tua feature  
   ```bash
   git checkout -b feature/nome-feature
   ```
3. Effettua le modifiche e committa  
4. Apri una Pull Request  

---

