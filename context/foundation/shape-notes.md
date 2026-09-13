---
project: "Beaver-Works"
context_type: greenfield
created: 2026-09-04
updated: 2026-09-04
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  gray_areas_resolved:
    - topic: "zakres głównej persony"
      decision: "Autor (projekt certyfikacyjny) + docelowo hobbyści remontujący mieszkania jako szersza grupa"
    - topic: "rodzaj bólu"
      decision: "Brakująca funkcja — dziś nie istnieje proste narzędzie do przestrzennego planowania remontu"
    - topic: "model kontroli dostępu"
      decision: "Lokalny ekran logowania + profile lokalne, hasła jako bezpieczne skróty z solą (standardowy mechanizm platformy)"
    - topic: "podział na role"
      decision: "Model płaski — jeden zalogowany użytkownik ma pełen dostęp do swoich projektów, bez ról typu admin/gość w MVP"
    - topic: "budżet czasowy MVP"
      decision: "1 tydzień pracy po godzinach na pierwszą kompletną wersję MVP, zaakceptowany bez dalszego skracania zakresu"
    - topic: "rysowanie planu w aplikacji"
      decision: "Odrzucone — użytkownik przygotowuje plik planu poza aplikacją (np. w innym narzędziu graficznym) i importuje go; brak wbudowanego rysownika w MVP"
    - topic: "odporność zapisu projektu"
      decision: "MVP: prosty nadpis pliku, bez pełnej atomowości/kopii zapasowej — pełna odporność przeniesiona do kolejnego etapu"
    - topic: "algorytm rekomendacji"
      decision: "Zastąpiony modelem: profil użytkownika deklaruje twardy budżet czasu i pieniędzy na okres; rekomendacja to lista zadań mieszczących się w budżecie, posortowana wg priorytetu. Priorytety pomieszczeń i idealny/nieidealny termin odłożone na później."
    - topic: "typ produktu i skala"
      decision: "Aplikacja desktopowa; skala small (autor + garstka osób); twardy deadline 14.09.2026, praca po godzinach"
    - topic: "zużywanie i reset budżetu w czasie"
      decision: "Budżet czasu jest tygodniowy, budżet pieniędzy jest miesięczny (niezależne okresy). Ukończenie zadania (Done) odejmuje jego wartość szacowaną od pozostałego budżetu bieżącego okresu, wg daty ukończenia. Oba budżety resetują się do pełnej wartości na początku kolejnego kalendarzowego okresu, bez wpływu wstecz."
  frs_drafted: 16
  quality_check_status: accepted
product_type: desktop
target_scale:
  users: small
  qps: n/a
  data_volume: small
timeline_budget:
  mvp_weeks: 1
  hard_deadline: 2026-09-14
  after_hours_only: true
---

# Beaver-Works — Notatki z shapowania

## Vision & Problem Statement

Zadania remontowe zarządzane jako zwykła lista tracą kontekst przestrzenny —
nie widać, gdzie w mieszkaniu dane zadanie dotyczy. Osoba planująca remont
mieszkania na podstawie planu lokalu potrzebuje przypiąć problem lub zadanie
bezpośrednio do miejsca, którego dotyczy (np. "to gniazdko trzeba
wymienić"), zamiast prowadzić arkusz lub listę to-do bez kontekstu
lokalizacji, co utrudnia śledzenie postępu pokój po pokoju i priorytetyzację
przestrzenną. To brakująca funkcja — dziś nie ma prostego narzędzia łączącego
plan mieszkania z zarządzaniem zadaniami remontowymi.

Insight: interfejs oparty na planie mieszkania — gdzie zadania są przypięte
do konkretnych miejsc na planie, wspierany przez model domenowy niezależny
od technologii prezentacji — plus deterministyczna reguła "rekomendowane
kolejne zadanie" to realna wartość odróżniająca ten produkt od pustego
CRUD-owego to-do w przebraniu planu mieszkania.

## User & Persona

**Persona główna**: Autor, budujący ten projekt jako niestandardowy projekt
certyfikacyjny 10xBuilder — osoba planująca/realizująca remont mieszkania,
która chce śledzić zadania przypięte do konkretnych miejsc na planie swojego
mieszkania.

**Persona drugorzędna**: Hobbyści remontujący mieszkania szerzej, którzy
mierzą się z tym samym rozdźwiękiem między "co trzeba zrobić" a "gdzie w
mieszkaniu to jest". MVP jest skopiowane pod personę główną; szersza
atrakcyjność dla hobbystów to zamiar projektowy, nie wymaganie MVP (samo to
nie implikuje funkcji wielo-kontowych/wielo-najemcy).

## Access Control

Prosty lokalny ekran logowania z lokalnymi profilami użytkowników. Hasła
przechowywane wyłącznie jako bezpieczne skróty z solą, generowane
standardowym mechanizmem platformy (bez własnej kryptografii). Dane
uwierzytelniania przechowywane poza plikiem projektu. Po zalogowaniu
użytkownik widzi listę swoich ostatnich projektów.

Model płaski — jeden zalogowany użytkownik ma pełen dostęp do swoich
projektów; brak podziału na role (np. admin/gość) w MVP.

Alternatywy rozważone i odrzucone dla MVP (mogą wrócić, jeśli lokalne hasło
okaże się sztuczne dla aplikacji jednoosobowej): identyfikacja kontem
systemu operacyjnego, PIN przypisany do projektu, tryb
właściciel/tylko-do-odczytu.

## Success Criteria

### Primary
- Pełny przepływ end-to-end działa: logowanie → utworzenie/otwarcie
  projektu → wyświetlenie planu → kliknięcie miejsca na planie → utworzenie
  zadania z markerem → edycja/usunięcie zadania → zapis i ponowne otwarcie
  projektu bez utraty danych → wyświetlenie rekomendowanego kolejnego
  zadania wraz z uzasadnieniem.
- Pierwszy pionowy fragment (utworzenie projektu → przykładowy plan →
  kliknięcie → tytuł zadania → kolorowy marker → zapis do JSON →
  zamknięcie/otwarcie → marker w tym samym miejscu) działa samodzielnie i
  jest osiągalny w ≤ 1 tydzień pracy po godzinach.

### Secondary
- Uzasadnienie rekomendacji następnego zadania jest zrozumiałe i krótkie
  dla użytkownika końcowego.

### Guardrails
- Zapis/odczyt projektu nigdy nie traci ani nie przesuwa danych zadań
  (pozycja markera na planie musi pozostać dokładnie taka sama po
  zapisie i ponownym otwarciu).
- Hasła nigdy nie są przechowywane jawnym tekstem — wyłącznie jako
  bezpieczne skróty z solą.

## Timeline acknowledgment

Acknowledged on 2026-09-04: 1-week MVP timeline confirmed by user as
realistic without further scope reduction. Rysowanie planu w aplikacji
zostało rozważone i odrzucone (patrz decyzja "rysowanie planu w
aplikacji" w checkpoint) — dzięki temu budżet 1 tygodnia pozostaje
aktualny mimo rozszerzenia logiki rekomendacji o budżety czasu/pieniędzy.

## Functional Requirements

### Dostęp i projekt
- FR-001: Użytkownik może zalogować się przez lokalny ekran logowania. Priority: must-have
  > Socrates: Kontrargument rozważony: "ekran logowania dodaje tarcie bez
  > realnej korzyści bezpieczeństwa dla jedynego użytkownika na własnym
  > komputerze." Rozstrzygnięcie: zostaje jak jest — wymagane przez
  > kryteria certyfikacji 10xBuilder jako mechanizm kontroli dostępu.
- FR-002: Użytkownik może utworzyć nowy projekt remontowy. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-003: Użytkownik może otworzyć istniejący projekt remontowy z dysku. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-004: Użytkownik może wybrać plan mieszkania dla projektu — importując
  wbudowany przykładowy plan albo otwierając własny plik planu
  przygotowany poza aplikacją (np. w zewnętrznym narzędziu graficznym).
  Priority: must-have
  > Socrates: Kontrargument rozważony: "wymaganie własnego pliku planu bez
  > przykładu może blokować pierwsze użycie." Rozstrzygnięcie: dołączyć
  > wbudowany przykładowy plan. Rysowanie planu wewnątrz aplikacji było
  > rozważane, ale odrzucone — użytkownik przygotowuje plan poza aplikacją.

### Plan i zadania
- FR-005: Użytkownik widzi plan mieszkania wyświetlony jako tło. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-006: Użytkownik może kliknąć miejsce na planie, aby utworzyć zadanie
  przypisane do tego miejsca. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-007: Zadanie przechowuje tytuł, opcjonalny opis, status, priorytet
  (1–5), szacowany koszt, szacowany czas, opcjonalny identyfikator
  pomieszczenia, geometrię punktową, zależności od innych zadań oraz daty
  utworzenia/aktualizacji. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest — to wymóg certyfikacji
  > (sensowny domenowo CRUD).
- FR-008: Użytkownik widzi na planie markery kolorowane zależnie od statusu
  zadania. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-009: Użytkownik widzi listę zadań oraz szczegóły wybranego zadania. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-010: Użytkownik może edytować zadanie, w tym zmienić jego status. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.
- FR-011: Użytkownik może usunąć zadanie. Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest.

### Trwałość danych
- FR-012: Aplikacja zapisuje projekt na dysku (prosty nadpis pliku w MVP,
  bez pełnej atomowości/kopii zapasowej) i pozwala go ponownie otworzyć
  bez utraty danych. Priority: must-have
  > Socrates: Kontrargument rozważony: "pełna odporność na uszkodzenia
  > (zapis atomowy + backup) może być zbyt złożona na 1 tydzień."
  > Rozstrzygnięcie: MVP używa prostego nadpisu pliku; pełna odporność
  > (zapis do pliku tymczasowego, atomowa podmiana, kopia zapasowa)
  > przenosi się do kolejnego etapu — patrz `## Forward: technical-roadmap`.

### Profil i rekomendacja
- FR-013: Użytkownik może zadeklarować w swoim profilu dostępny budżet
  czasu (godziny/tydzień) i budżet pieniędzy (kwota/miesiąc) — każdy z
  własnym, niezależnym okresem rozliczeniowym. Priority: must-have
  > Socrates: Wynikło z dyskusji nad FR-014 — budżety muszą być gdzieś
  > przechowywane zanim posłużą do rekomendacji. Brak kontrargumentu wobec
  > samego przechowywania; zostaje jak jest.
- FR-014: Aplikacja rekomenduje uporządkowaną według priorytetu listę
  zadań, które mieszczą się łącznie w zadeklarowanym budżecie czasu i
  pieniędzy użytkownika na dany okres — budżety te są twardymi,
  nieprzekraczalnymi limitami. Zadania Done/Active/ręcznie Blocked oraz
  zadania z niespełnionymi zależnościami są wykluczone z wyboru. Lista
  pokazuje krótkie uzasadnienie odwołujące się do priorytetu i budżetu.
  Priority: must-have
  > Socrates: Kontrargument rozważony: "wagi scoringu są zgadywane bez
  > realnych danych użytkowania" (dot. pierwotnego pomysłu score = priority
  > *10 - czas*waga - koszt*waga). Rozstrzygnięcie: zastąpione prostszym,
  > łatwiejszym do wyjaśnienia modelem — dobór zadań wg priorytetu
  > mieszczących się w twardym budżecie czasu/pieniędzy, zamiast
  > ważonego scoringu. Priorytety pomieszczeń oraz "idealny/nieidealny"
  > termin wykonania jako tie-breaker są świadomie odłożone do kolejnego
  > etapu (patrz `## Forward: technical-roadmap`) — mieszczenie się w
  > 1-tygodniowym budżecie MVP wymagało tego uproszczenia.
- FR-015: Gdy zadanie zostanie oznaczone jako Done, aplikacja odejmuje
  jego szacowany czas od pozostałego budżetu czasu bieżącego tygodnia
  oraz jego szacowany koszt od pozostałego budżetu pieniędzy bieżącego
  miesiąca — na podstawie daty ukończenia zadania (nie daty utworzenia
  ani rozpoczęcia). Priority: must-have
  > Socrates: Wynikło z rozmowy o mechanice budżetu — bez odejmowania
  > zużycia budżety w profilu byłyby statyczne i nieużyteczne w praktyce.
  > Rozstrzygnięcie: odejmowana jest wartość szacowana zadania (nie ma
  > osobnego pola "rzeczywisty koszt/czas" w MVP); zadanie ukończone poza
  > bieżącym okresem (np. przeciągnięte z poprzedniego tygodnia) obciąża
  > budżet okresu, w którym faktycznie zostało ukończone — bez
  > proporcjonalnego rozkładania wstecz.
- FR-016: Budżet czasu i budżet pieniędzy użytkownika resetują się do
  pełnej zadeklarowanej wartości na początku każdego kalendarzowego
  okresu — nowy tydzień dla budżetu czasu, nowy miesiąc dla budżetu
  pieniędzy — niezależnie od poziomu zużycia w poprzednim okresie.
  Priority: must-have
  > Socrates: Brak kontrargumentu; zostaje jak jest — reset kalendarzowy
  > (nie rolling od daty utworzenia profilu) wybrany dla przewidywalności
  > dla użytkownika.

## User Stories

### US-01: Użytkownik dodaje zadanie remontowe klikając plan i zapisuje projekt

- **Given** zalogowany użytkownik z otwartym projektem i wyświetlonym
  planem mieszkania (wbudowany przykładowy plan lub własny zaimportowany
  plik)
- **When** klika miejsce na planie i wpisuje tytuł zadania
- **Then** na planie pojawia się kolorowy marker odpowiadający statusowi
  nowego zadania (domyślnie Planned), zadanie jest widoczne na liście
  zadań, a po zapisaniu i ponownym otwarciu projektu zadanie oraz pozycja
  markera są zachowane bez zmian

#### Acceptance Criteria
- Kliknięcie poza faktycznie wyświetlanym obszarem obrazu planu (poza
  marginesami przy letterboxingu) nie tworzy zadania w błędnym miejscu.
- Domyślny status nowego zadania to Planned.
- Po zmianie statusu zadania kolor markera aktualizuje się zgodnie z
  nowym statusem.
- Zapisany i ponownie otwarty projekt odtwarza zadanie z identyczną
  pozycją geometrii (współrzędne 0–1 bez utraty precyzji poza ustalonym
  marginesem zaokrąglenia).

### US-02: Użytkownik ustala budżet i otrzymuje rekomendację zadań

- **Given** zalogowany użytkownik z co najmniej kilkoma zadaniami o różnych
  priorytetach, szacowanym czasie i koszcie, część z nich zablokowana
  przez niespełnione zależności
- **When** ustawia w swoim profilu dostępny budżet czasu (tygodniowy) i
  budżet pieniędzy (miesięczny) i otwiera widok rekomendacji
- **Then** widzi uporządkowaną wg priorytetu listę zadań mieszczących się
  łącznie w pozostałym budżecie bieżącego okresu, z zadaniami
  zablokowanymi/ukończonymi/aktywnymi wykluczonymi, oraz krótkie
  uzasadnienie wyboru

#### Acceptance Criteria
- Zadania, których zależności nie są ukończone, nigdy nie pojawiają się
  na liście rekomendacji.
- Suma szacowanego czasu i kosztu rekomendowanych zadań nie przekracza
  pozostałego budżetu bieżącego okresu użytkownika.
- Lista rekomendacji przelicza się ponownie, gdy zmieni się priorytet
  zadania, jego estymaty, albo budżet użytkownika.
- Oznaczenie zadania jako Done obniża pozostały budżet czasu bieżącego
  tygodnia (o szacowany czas zadania) i pozostały budżet pieniędzy
  bieżącego miesiąca (o szacowany koszt zadania).
- Na początku nowego tygodnia budżet czasu wraca do pełnej wartości; na
  początku nowego miesiąca budżet pieniędzy wraca do pełnej wartości —
  niezależnie od zużycia w poprzednim okresie.

## Business Logic

Aplikacja dobiera zestaw zadań remontowych mieszczący się w
zadeklarowanym przez użytkownika budżecie czasu (tygodniowym) i budżecie
pieniędzy (miesięcznym), traktując oba budżety jako twarde,
nieprzekraczalne limity, i porządkuje wybrane zadania według priorytetu.

Reguła konsumuje jako wejście: budżet czasu (godziny/tydzień) i budżet
pieniędzy (kwota/miesiąc) zadeklarowany przez użytkownika w profilu — każdy
z własnym, niezależnym okresem rozliczeniowym; dla każdego zadania —
priorytet (1–5), szacowany czas, szacowany koszt, status oraz zależności
blokujące od innych zadań. Wyjściem jest uporządkowana lista zadań, które
łącznie mieszczą się w pozostałym budżecie bieżącego okresu, wraz z
krótkim, czytelnym uzasadnieniem odwołującym się do priorytetu i zużycia
budżetu. Zadania ukończone, aktywne, ręcznie zablokowane lub z
niespełnionymi zależnościami nigdy nie trafiają na listę. Użytkownik
spotyka tę regułę jako panel rekomendacji, który przelicza się na bieżąco,
gdy zmienia się priorytet zadań, ich estymaty albo budżet użytkownika.

Oba budżety zużywają się w miarę kończenia zadań: gdy zadanie jest
oznaczane jako Done, jego szacowany czas obniża pozostały budżet czasu
bieżącego tygodnia, a jego szacowany koszt obniża pozostały budżet
pieniędzy bieżącego miesiąca — o tym, do którego okresu przypisać zużycie,
decyduje data ukończenia zadania. Oba budżety wracają do pełnej
zadeklarowanej wartości na początku kolejnego kalendarzowego okresu
(nowy tydzień dla czasu, nowy miesiąc dla pieniędzy), niezależnie od
zużycia w okresie poprzednim — reset nigdy nie działa wstecz i nie zmienia
już ukończonych zadań z poprzednich okresów.

Brak oszacowanego kosztu lub czasu dla zadania nie może być automatycznie
traktowany jako zero — faworyzowałoby to nieoszacowane zadania. Reguła
przyjmuje neutralne wartości domyślne lub obniża pewność rekomendacji dla
takich zadań.

## Non-Functional Requirements

- Zmiana statusu zadania i odpowiadająca jej zmiana koloru markera na
  planie są widoczne użytkownikowi natychmiast — bez zauważalnego
  opóźnienia po zapisaniu zmiany.
- Zapis projektu na dysku kończy się potwierdzeniem albo czytelnym,
  zrozumiałym komunikatem błędu w czasie odczuwalnym jako natychmiastowy
  dla typowego rozmiaru projektu.
- Hasło użytkownika nigdy nie opuszcza urządzenia w postaci jawnego
  tekstu i nigdy nie jest przechowywane w postaci możliwej do odczytania.
- Aplikacja pozostaje w pełni użyteczna offline — podstawowy przepływ MVP
  nie wymaga połączenia sieciowego.
- Utrata zasilania lub awaria aplikacji podczas zapisu projektu nie może
  pozostawić pliku projektu w stanie nieczytelnym / nie do odzyskania w
  stopniu większym niż utrata bieżącej, niezapisanej jeszcze zmiany.

## Non-Goals

- **Pomieszczenia jako poligony** — MVP nie modeluje pomieszczeń jako
  geometrii poligonowej; to kolejny etap rozwoju.
- **Automatyczne przypisanie zadania do pomieszczenia (point-in-polygon)** —
  wymaga poligonów pomieszczeń, których nie ma w MVP.
- **Kolorowanie pomieszczeń wg postępu remontu** — zależne od poligonów
  pomieszczeń; poza zakresem MVP.
- **Linie instalacji (elektryczna/hydrauliczna/internetowa)** — geometria
  typu LineString odłożona na później; nie jest potrzebna do udowodnienia
  wartości MVP.
- **Import/eksport w formacie podobnym do GeoJSON** — MVP przechowuje dane
  wewnętrznie w formacie projektu; wymiana z zewnętrznymi narzędziami
  geoprzestrzennymi to etap kolejny.
- **Zoom/pan/dopasowanie planu** — MVP zakłada stały widok planu; nawigacja
  po dużych/złożonych planach to etap kolejny.
- **Alternatywny renderer ArcGIS** — pierwszy i jedyny renderer w MVP to
  renderer natywny wybranego stosu UI; ArcGIS/WebView odłożone.
- **Repozytorium SQLite lub synchronizacja chmurowa** — MVP zapisuje projekt
  wyłącznie jako pojedynczy plik lokalny; brak wymogu bazy danych czy
  chmury.
- **Wielu użytkowników pracujących nad jednym projektem** — brak
  współdzielenia/współpracy w czasie rzeczywistym nad tym samym plikiem
  projektu; jeden użytkownik, jedno urządzenie na sesję.
- **Priorytety przypisane do pomieszczeń** — sam mechanizm istnieje w
  koncepcji, ale nie jest wymagany do działania rekomendacji w MVP;
  odłożony do kolejnego etapu.
- **Rysowanie planu wewnątrz aplikacji** — użytkownik przygotowuje plan
  poza aplikacją (np. w zewnętrznym narzędziu graficznym) i go importuje;
  wbudowany edytor/rysownik geometrii planu to nie-cel MVP.
- **"Idealny/nieidealny" termin wykonania zadania jako tie-breaker** —
  rozważony podczas projektowania algorytmu rekomendacji, ale odłożony —
  MVP sortuje wyłącznie wg priorytetu w ramach budżetu.
- **Pełna odporność zapisu na awarie (zapis atomowy + kopia zapasowa)** —
  MVP używa prostego nadpisu pliku projektu; pełna odporność to kolejny
  etap (nie-funkcjonalny non-goal na start).

## Quality cross-check

Wszystkie elementy soft-gate są obecne:

- Access Control: present.
- Business Logic (zdanie-reguła): present.
- Project artifacts (shape-notes.md z poprawnym checkpointem): present.
- Timeline-cost acknowledged: present (1 tydzień, zaakceptowany po
  odrzuceniu rozszerzenia o rysownik planu).
- Non-Goals: present (14 pozycji).

Status: accepted — brak zidentyfikowanych luk wymagających ostrzeżenia.

## Forward: tech-stack

> Zapisane dosłownie z myślą o kolejnym kroku łańcucha (dobór stosu
> technologicznego) — nierozstrzygane na tym etapie, zgodnie z zasadą
> otwartości stosu w /10x-shape.

- Użytkownik poprosił o rozważenie alternatyw dla WPF w stosie
  technologicznym, np. WinForms; wspomniał też "coś takiego jak XAML"
  (prawdopodobnie chodzi o WinUI 3 / .NET MAUI / UWP — te też używają
  XAML-a). Ta decyzja zostanie przekazana do kroku doboru stosu
  technologicznego po `/10x-prd`.
- Preferowany stos z pierwotnego briefu (informacyjnie, nie wiążąco — do
  ponownej oceny przez tech-stack-selector): C#, aktualna stabilna wersja
  .NET obsługiwana przez Visual Studio 2026, WPF, MVVM,
  CommunityToolkit.Mvvm, System.Text.Json, xUnit, FlaUI (lub odpowiednik do
  testów UI), GitHub Actions (lub odpowiednik CI), dystrybucja jako
  self-contained/instalator/MSIX jeśli proporcjonalne do zakresu.
- Nie planowane na start: klasyczny backend HTTP, obowiązkowa usługa
  chmurowa.
- Renderer planu musi być wymienialny przez adapter (np. interfejs
  `IPlanRenderer`) bez przepisywania modelu domenowego, warstwy zapisu ani
  algorytmu rekomendacji — to ograniczenie architektoniczne/domenowe, nie
  wybór stosu, i powinno zostać zachowane niezależnie od finalnie wybranej
  technologii UI.

## Forward: technical-roadmap

> Zapisane dosłownie z myślą o dalszym planowaniu implementacji — nie jest
> to część schematu PRD.

- Preferowana struktura rozwiązania: `BeaverWorks.Domain`,
  `BeaverWorks.Application`, `BeaverWorks.Infrastructure`,
  `BeaverWorks.Wpf` (lub odpowiedni projekt prezentacji w zależności od
  wybranego stosu), z kierunkiem zależności: prezentacja → aplikacja →
  domena; infrastruktura → aplikacja/domena. Domena nie może zależeć od
  frameworka UI, JSON-a, I/O plikowego, SVG ani ArcGIS.
- Model geometrii (podobny do GeoJSON): `Geometry` (abstrakcyjny),
  `Coordinate(X, Y)`, `PointGeometry`, `LineStringGeometry`,
  `PolygonGeometry`. MVP używa wyłącznie `PointGeometry` dla zadań;
  `PolygonGeometry` dla pomieszczeń i `LineStringGeometry` dla instalacji to
  kolejne etapy.
- Współrzędne planu są względne (0–1 na obu osiach, punkt (0,0) w lewym
  górnym rogu), a nie pikselowe — dane pozostają niezależne od
  rozdzielczości/rozmiaru okna/renderera.
- Rekomendowane podejście do renderowania MVP: zachować oryginalny plan
  jako SVG/plik źródłowy; wyświetlać jako podgląd rastrowy (PNG lub
  bitmapa wygenerowana z SVG) w warstwie obrazu; nałożyć przezroczystą
  warstwę interaktywną dla markerów/kliknięć. Unikać renderowania przez
  WebView2/ArcGIS w pierwszym MVP bez uzasadnienia. Znana pułapka:
  "listwowanie" (letterboxing) obrazu przy skalowaniu Uniform wymaga, by
  transformacja kliknięcia na współrzędne względne uwzględniała faktycznie
  wyświetlany prostokąt obrazu, nie cały canvas — wymaga dedykowanych
  testów.
- Przechowywanie projektu: format pojedynczego pliku (własne rozszerzenie,
  w brifie opisane jako `.beaver`), na start zwykły JSON, docelowo archiwum
  ZIP-podobne zawierające `project.json` + `assets/floor-plan.svg`.
  Dokument niesie `schemaVersion` dla przyszłych migracji. Abstrakcja
  repozytorium (np. `IProjectRepository`) z pierwszą implementacją opartą
  o JSON; przyszłe implementacje (np. SQLite, chmura) wymienialne za tym
  samym interfejsem. Zapis musi być odporny na awarie: zapis do pliku
  tymczasowego, walidacja, atomowe podmienienie gdy platforma pozwala,
  opcjonalna kopia zapasowa, jasny komunikat błędu, nigdy utrata ostatniej
  poprawnej wersji.
- Kontrola dostępu (preferowana, wymaga krótkiego porównania przed
  implementacją zgodnie z instrukcją z briefu): prosty lokalny ekran
  logowania z lokalnymi profilami; hasła przechowywane wyłącznie jako
  bezpieczne skróty z solą przez standardowy mechanizm platformy (bez
  własnej kryptografii); dane uwierzytelniania poza plikiem projektu; po
  zalogowaniu lista ostatnich projektów użytkownika. Alternatywy z briefu,
  jeśli lokalne hasło okaże się sztuczne dla aplikacji jednoosobowej:
  identyfikacja kontem systemu operacyjnego, PIN per-projekt, tryb
  właściciel/tylko-do-odczytu.
- Algorytm rekomendacji (deterministyczny, wyjaśnialny): odrzuć zadania
  Done/Active/ręcznie Blocked oraz zadania z niespełnionymi wymaganymi
  zależnościami; oblicz wynik dla pozostałych (punkt startowy:
  `score = priority * 10 - estimatedHours * timeWeight - estimatedCost * costWeight`,
  z neutralnymi wartościami domyślnymi lub obniżoną pewnością gdy koszt/czas
  nieoszacowane — nigdy cichym zerem); wybierz zadanie z najwyższym
  wynikiem; pokaż krótkie, czytelne uzasadnienie.
- Oczekiwania testowe: testy jednostkowe walidacji współrzędnych,
  transformacji ekran→względne i względne→renderer (w tym letterboxing),
  algorytmu rekomendacji, filtrowania po zależnościach, serializacji/
  deserializacji projektu; test integracyjny zapisu i odczytu projektu;
  co najmniej jeden test z perspektywy użytkownika obejmujący: logowanie →
  utworzenie projektu → wybór planu → kliknięcie planu → dodanie zadania →
  zmiana statusu → zapis/ponowne otwarcie → weryfikacja zachowanego stanu
  (dopuszczalny krótszy stabilny scenariusz jeśli pełna automatyzacja UI
  okaże się niestabilna/nieproporcjonalna, ale sam test z perspektywy
  użytkownika jest wymagany przez certyfikację).
- Dokumenty kontekstowe oczekiwane dalej w procesie: `docs/prd.md`,
  `docs/infrastructure.md`, `docs/roadmap.md`, opcjonalnie
  `decisions/`/`docs/adr/` i `ai-worklog.md` dokumentujący rolę AI w
  planowaniu/implementacji/testowaniu.
- Roadmapa po MVP (explicit — nie budować przed ukończeniem przepływu
  MVP): pomieszczenia jako poligony; auto-przypisanie zadania do
  pomieszczenia przez point-in-polygon; kolorowanie pomieszczeń wg
  postępu; linie instalacji (elektryczna/hydrauliczna/internetowa);
  import/eksport w formacie podobnym do GeoJSON; zoom/pan/dopasowanie
  planu; alternatywny renderer ArcGIS; opcjonalne repozytorium SQLite lub
  synchronizacja chmurowa.
- Priorytet pierwszego pionowego fragmentu (explicit z briefu): utworzenie
  projektu → załadowanie przykładowego planu → kliknięcie planu →
  wpisanie tytułu zadania → wyświetlenie kolorowego markera → zapis do
  JSON → zamknięcie/ponowne otwarcie → marker odtworzony w tym samym
  miejscu. Ma pierwszeństwo przed rozbudowaną stylizacją, dodatkowymi
  encjami i ArcGIS.
- Kontekst certyfikacji 10xBuilder (kontekstowo, nie jako wymaganie
  produktowe): brak wymogu publicznego URL dla aplikacji desktopowej;
  kluczowe jest pokazanie prawidłowego procesu wytwarzania z AI,
  dokumentacji, testów i automatyzacji; pierwszy pełny przepływ użytkownika
  powinien być osiągalny w mniej niż tydzień pracy po godzinach; projekt
  nie może być pustym CRUD-em.
