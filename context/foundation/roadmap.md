---
project: "Beaver-Works"
version: 1
status: draft
created: 2026-09-13
updated: 2026-09-13
prd_version: 1
main_goal: speed
top_blocker: time
milestone_id: renovation-mvp
milestone_seq: 1
milestone_status: open
---

# Roadmap: Beaver-Works

> Wyprowadzone z `context/foundation/prd.md` (v1) + auto-zbadanego stanu bazowego kodu.
> Edytuj w miejscu; archiwizuj przy zastąpieniu.
> Poniższe fragmenty są ułożone w kolejności zależności. Tabela "W skrócie" to indeks.

## Milestone (Kamień milowy)

**M-1: MVP — przypinanie zadań remontowych + rekomendacje wg budżetu** — Status: open

- **Cel:** Dostarczyć kompletne, jednotygodniowe MVP opisane w PRD: użytkownik może zalogować się lokalnie, przypiąć zadania remontowe do planu mieszkania i nigdy ich nie stracić przy zapisie/ponownym otwarciu, zarządzać tymi zadaniami, zadeklarować budżet czasu/pieniędzy i otrzymać rekomendację, co zrobić dalej w ramach tego budżetu.
- **Materiały źródłowe:** `context/foundation/prd.md` (v1)
- **Gotowe, gdy:** każdy element F-NN i S-NN poniżej ma status `done`.

## Streszczenie wizji

Zwykła lista zadań remontowych traci kontekst przestrzenny — nie widać, gdzie
w mieszkaniu dotyczy dane zadanie. Ta aplikacja przypina zadania remontowe
bezpośrednio do obrazu planu mieszkania (np. "to gniazdko trzeba wymienić")
zamiast prowadzić arkusz, dzięki czemu postęp i priorytet można śledzić
pokój po pokoju. Wyróżnik produktu — cecha, bez której produkt niczym nie
różniłby się od zwykłego to-do — to połączenie zadań przypiętych
geometrycznie do planu Z deterministyczną regułą "co zrobić dalej", która
pokazuje pracę faktycznie mieszczącą się w pozostałym budżecie czasu i
pieniędzy użytkownika.

## Gwiazda przewodnia

**S-01: Użytkownik może przypiąć zadanie remontowe do planu mieszkania i
przetrwa ono zapis/ponowne otwarcie** — to dokładnie ten fragment, który
Kryteria sukcesu (Primary) w PRD nazywają wprost: najmniejszy przepływ
end-to-end, który dowodzi, że główna hipoteza produktu (zadania przypięte do
planu + trwała geometria) faktycznie działa — dlatego jest sekwencjonowany
jako pierwszy, mimo że zależy od fundamentu logowania.

> "Gwiazda przewodnia" oznacza tutaj: najmniejszy fragment end-to-end, którego
> udane dostarczenie dowodzi, że główny pomysł działa — wszystko, co
> następuje później, ma znaczenie tylko wtedy, gdy ten przepływ się utrzyma.

## W skrócie

| ID | Change ID | Efekt (użytkownik może …) | Wymagania wstępne | Odwołania do PRD | Status |
| ---- | ------------------------- | ------------------------------------------------------------------------ | -------------- | ------------------------------------------- | -------- |
| F-01 | local-auth-and-profiles | (fundament) lokalne logowanie + przechowywanie haseł jako soleny skrót gotowe | — | FR-001, Access Control | in-progress |
| S-01 | pin-and-persist-task | przypiąć zadanie do planu mieszkania; przetrwa ono zapis, zamknięcie i ponowne otwarcie | F-01 | US-01, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-012 | proposed |
| S-02 | manage-tasks | przeglądać, edytować (w tym status) i usuwać zadania z listy | F-01, S-01 | US-01, FR-009, FR-010, FR-011 | proposed |
| S-03 | budget-based-recommendations | zadeklarować budżet czasu/pieniędzy i zobaczyć uporządkowaną wg priorytetu rekomendację mieszczącą się w budżecie | F-01, S-01, S-02 | US-02, FR-013, FR-014, FR-015, FR-016 | proposed |

## Stan bazowy (Baseline)

Co już istnieje w kodzie na dzień `2026-09-13` (zbadane automatycznie i
potwierdzone przez użytkownika). Fundamenty poniżej zakładają, że to jest
obecne i NIE odtwarzają tego od nowa.

- **Frontend:** brak — `MainWindow.xaml` to pusta powłoka; foldery
  `Views/`/`ViewModels/` w `BeaverWorks.Desktop` są puste.
- **Backend / API:** nie dotyczy — aplikacja desktopowa, brak warstwy serwera.
- **Dane:** brak — foldery `Models/`/`Persistence/` w `BeaverWorks.Core` są
  puste; istnieje tylko placeholder `Class1.cs`.
- **Auth:** brak — brak ekranu logowania, brak przechowywania poświadczeń;
  `tech-stack.md` deklaruje zamiar (`has_auth: true`), ale nic nie jest
  zaimplementowane.
- **Deploy / infra:** brak, świadomie poza zakresem na razie (brak CI/CD wg
  konwencji repo).
- **Observability:** brak — brak logowania/śledzenia błędów.

## Fundamenty

### F-01: Fundament lokalnego logowania i profilu

- **Efekt:** (fundament) istnieje lokalny ekran logowania, hasła są
  przechowywane jako solony skrót poza plikiem projektu, a zalogowany
  użytkownik trafia na (ewentualnie pustą) listę ostatnich projektów.
- **Change ID:** local-auth-and-profiles
- **Odwołania do PRD:** FR-001, sekcja Access Control
- **Odblokowuje:** S-01, S-02, S-03 (każdy kolejny fragment zakłada
  zalogowanego użytkownika z własnym zbiorem projektów); spełnia guardrail
  "hasła nigdy nie są przechowywane jawnym tekstem".
- **Wymagania wstępne:** —
- **Równolegle z:** —
- **Blokery:** —
- **Niewiadome:** —
- **Ryzyko:** Wyłącznie lokalne logowanie dodaje niewielkie tarcie dla
  jednoosobowej aplikacji desktopowej, ale FR-001 i sekcja Access Control
  wymagają tego wprost; celowo utrzymane minimalnie (model płaski, brak
  ról, standardowy platformowy solony skrót), aby chronić tygodniowy
  deadline.
- **Status:** in-progress

## Fragmenty (Slices)

### S-01: Przypnij i zachowaj zadanie remontowe

- **Efekt:** użytkownik może utworzyć projekt, zobaczyć plan mieszkania
  (wbudowany przykład lub własny plik), kliknąć miejsce na planie, aby
  utworzyć zadanie z tytułem i domyślnie kolorowanym markerem, a po
  zapisaniu i ponownym otwarciu projektu zadanie oraz dokładna pozycja
  markera pozostają niezmienione.
- **Change ID:** pin-and-persist-task
- **Odwołania do PRD:** US-01, FR-002, FR-003, FR-004, FR-005, FR-006,
  FR-007, FR-008, FR-012
- **Wymagania wstępne:** F-01
- **Równolegle z:** —
- **Blokery:** —
- **Niewiadome:** —
- **Ryzyko:** To jest kluczowy fragment gwiazdy przewodniej — wszystko
  inne traci sens, jeśli przepływ kliknięcie → marker → zapis → odczyt nie
  zachowa geometrii dokładnie; sekwencjonowany zaraz po logowaniu, aby
  udowodnić główną hipotezę, zanim powstanie dalszy zakres.
- **Status:** proposed

### S-02: Zarządzaj zadaniami

- **Efekt:** użytkownik może zobaczyć listę zadań i szczegóły wybranego
  zadania, edytować zadanie (w tym zmieniać status, co natychmiast
  aktualizuje kolor markera) oraz usunąć zadanie.
- **Change ID:** manage-tasks
- **Odwołania do PRD:** US-01, FR-009, FR-010, FR-011
- **Wymagania wstępne:** F-01, S-01
- **Równolegle z:** —
- **Blokery:** —
- **Niewiadome:** —
- **Ryzyko:** Wydzielenie zarządzania (lista/edycja/usuwanie/status) z S-01
  utrzymuje fragment gwiazdy przewodniej w rozmiarze możliwym do
  zaplanowania w jednym przebiegu `/10x-plan`, jednocześnie pokrywając
  FR-009/010/011 jako spójny, widoczny dla użytkownika efekt.
- **Status:** proposed

### S-03: Rekomendacje zadań wg budżetu

- **Efekt:** użytkownik może zadeklarować w profilu tygodniowy budżet
  czasu i miesięczny budżet pieniędzy, otworzyć widok rekomendacji
  pokazujący uporządkowaną wg priorytetu listę zadań mieszczących się w
  pozostałym budżecie (z wykluczeniem zadań ukończonych/aktywnych/
  zablokowanych/z niespełnionymi zależnościami) wraz z krótkim
  uzasadnieniem, oraz zobaczyć, że budżety zmniejszają się po ukończeniu
  zadania i resetują się automatycznie na początku kolejnego okresu
  kalendarzowego.
- **Change ID:** budget-based-recommendations
- **Odwołania do PRD:** US-02, FR-013, FR-014, FR-015, FR-016
- **Wymagania wstępne:** F-01, S-01, S-02
- **Równolegle z:** —
- **Blokery:** —
- **Niewiadome:** —
- **Ryzyko:** Reguła rekomendacji potrzebuje ręcznie ustawianego statusu
  Blocked oraz zależności między zadaniami, które istnieją dopiero po
  wdrożeniu edycji z S-02; sekwencjonowanie na końcu unika budowania
  matematyki budżetu na danych wejściowych, których jeszcze nie ma.
- **Status:** proposed

## Przekazanie do backlogu

| ID Roadmapy | Change ID | Proponowany tytuł zgłoszenia | Gotowe do `/10x-plan` | Uwagi |
| ----------- | ------------------------- | -------------------------------------------------------------- | ---------------------- | --------------------------- |
| F-01 | local-auth-and-profiles | Lokalne logowanie + przechowywanie haseł jako solony skrót | tak | Uruchom `/10x-plan local-auth-and-profiles` |
| S-01 | pin-and-persist-task | Przypnij zadanie do planu i zachowaj je po zapisie/odczycie | nie | Zablokowane przez F-01 |
| S-02 | manage-tasks | Lista zadań, edycja, zmiana statusu, usuwanie | nie | Zablokowane przez F-01, S-01 |
| S-03 | budget-based-recommendations | Rekomendacje zadań mieszczące się w budżecie z uzasadnieniem | nie | Zablokowane przez F-01, S-01, S-02 |

## Otwarte pytania roadmapy

_Brak — sekcja `## Open Questions` w PRD nie zgłasza żadnych blokujących
otwartych pytań (cross-check zamykający shape-notes zaakceptowany bez luk)._

## Odłożone (Parked)

- **Pomieszczenia jako poligony** — Dlaczego odłożone: PRD §Non-Goals; MVP
  nie modeluje pomieszczeń jako geometrii poligonowej.
- **Automatyczne przypisanie zadania do pomieszczenia (point-in-polygon)** —
  Dlaczego odłożone: PRD §Non-Goals; wymaga poligonów pomieszczeń, których
  nie ma w MVP.
- **Kolorowanie pomieszczeń wg postępu remontu** — Dlaczego odłożone: PRD
  §Non-Goals; zależne od poligonów pomieszczeń.
- **Linie instalacji (elektryczna/hydrauliczna/internetowa)** — Dlaczego
  odłożone: PRD §Non-Goals; geometria LineString odłożona, niepotrzebna do
  udowodnienia wartości MVP.
- **Import/eksport w ustandaryzowanym formacie geoprzestrzennym** —
  Dlaczego odłożone: PRD §Non-Goals; MVP przechowuje dane wewnętrznie we
  własnym formacie projektu.
- **Zoom/pan/dopasowanie widoku planu** — Dlaczego odłożone: PRD
  §Non-Goals; MVP zakłada stały widok planu.
- **Alternatywny renderer mapowy** — Dlaczego odłożone: PRD §Non-Goals; w
  MVP działa wyłącznie natywny renderer wybranego stosu UI.
- **Zewnętrzne repozytorium danych lub synchronizacja chmurowa** —
  Dlaczego odłożone: PRD §Non-Goals; MVP zapisuje projekt wyłącznie jako
  pojedynczy plik lokalny.
- **Wielu użytkowników pracujących nad jednym projektem** — Dlaczego
  odłożone: PRD §Non-Goals; jeden użytkownik, jedno urządzenie na sesję.
- **Priorytety przypisane do pomieszczeń** — Dlaczego odłożone: PRD
  §Non-Goals; niepotrzebne, aby reguła rekomendacji działała w MVP.
- **Rysowanie planu wewnątrz aplikacji** — Dlaczego odłożone: PRD
  §Non-Goals; użytkownik przygotowuje i importuje plan poza aplikacją.
- **"Idealny/nieidealny" termin jako tie-breaker w rekomendacjach** —
  Dlaczego odłożone: PRD §Non-Goals; MVP sortuje wyłącznie wg priorytetu w
  ramach budżetu.
- **Pełna odporność zapisu na awarie (zapis atomowy + kopia zapasowa)** —
  Dlaczego odłożone: PRD §Non-Goals; MVP używa prostego nadpisu pliku;
  pełna odporność to kolejny etap.

## Historia kamieni milowych

_(puste — to pierwszy kamień milowy)_

## Zrobione

_(puste — nic jeszcze nie zostało zarchiwizowane)_
