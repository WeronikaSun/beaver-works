---
project: "Beaver-Works"
version: 1
status: draft
created: 2026-09-04
context_type: greenfield
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
— osoba planująca/realizująca remont mieszkania, która chce śledzić zadania
przypięte do konkretnych miejsc na planie swojego mieszkania.

### Secondary persona

Hobbyści remontujący mieszkania szerzej, którzy mierzą się z tym samym
rozdźwiękiem między "co trzeba zrobić" a "gdzie w mieszkaniu to jest". MVP
jest skopiowane pod personę główną; szersza atrakcyjność dla hobbystów to
zamiar projektowy, nie wymaganie MVP.

## Success Criteria

### Primary
- Pełny przepływ end-to-end działa: logowanie → utworzenie/otwarcie
  projektu → wyświetlenie planu → kliknięcie miejsca na planie → utworzenie
  zadania z markerem → edycja/usunięcie zadania → zapis i ponowne otwarcie
  projektu bez utraty danych → wyświetlenie rekomendowanego kolejnego
  zadania wraz z uzasadnieniem.
- Pierwszy pionowy fragment (utworzenie projektu → przykładowy plan →
  kliknięcie → tytuł zadania → kolorowy marker → zapis projektu na dysk →
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
  łącznie w pozostałym budżecie bieżącego okresu użytkownika, z zadaniami
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
  niezależnie od siebie.

## Functional Requirements

### Dostęp i projekt
- FR-001: Użytkownik może zalogować się przez lokalny ekran logowania. Priority: must-have
  > Socrates: Kontrargument rozważony: "ekran logowania dodaje tarcie bez
  > realnej korzyści bezpieczeństwa dla jedynego użytkownika na własnym
  > komputerze." Rozstrzygnięcie: zostaje jak jest — wymagane jako
  > mechanizm kontroli dostępu.
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
  > Socrates: Brak kontrargumentu; zostaje jak jest — sensowny domenowo CRUD.
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
  > przenosi się do kolejnego etapu.

### Profil i rekomendacja
- FR-013: Użytkownik może zadeklarować w swoim profilu dostępny budżet
  czasu (godziny/tydzień) i budżet pieniędzy (kwota/miesiąc) — każdy z
  tych budżetów ma własny, niezależny okres. Priority: must-have
  > Socrates: Wynikło z dyskusji nad FR-014 — budżety muszą być gdzieś
  > przechowywane zanim posłużą do rekomendacji. Brak kontrargumentu wobec
  > samego przechowywania; zostaje jak jest.
- FR-014: Aplikacja rekomenduje uporządkowaną według priorytetu listę
  zadań, które mieszczą się łącznie w pozostałym budżecie czasu i
  pieniędzy użytkownika na bieżący okres — budżety te są twardymi,
  nieprzekraczalnymi limitami. Zadania Done/Active/ręcznie Blocked oraz
  zadania z niespełnionymi zależnościami są wykluczone z wyboru. Lista
  pokazuje krótkie uzasadnienie odwołujące się do priorytetu i budżetu.
  Priority: must-have
  > Socrates: Kontrargument rozważony: "wagi scoringu są zgadywane bez
  > realnych danych użytkowania" (dot. pierwotnego pomysłu ważonego
  > scoringu priorytet/czas/koszt). Rozstrzygnięcie: zastąpione prostszym,
  > łatwiejszym do wyjaśnienia modelem — dobór zadań wg priorytetu
  > mieszczących się w twardym budżecie czasu/pieniędzy. Priorytety
  > pomieszczeń oraz "idealny/nieidealny" termin wykonania jako
  > tie-breaker są świadomie odłożone do kolejnego etapu — mieszczenie się
  > w 1-tygodniowym budżecie MVP wymagało tego uproszczenia.
- FR-015: Oznaczenie zadania jako ukończone (Done) odejmuje jego szacowany
  czas od pozostałego budżetu czasu bieżącego tygodnia oraz jego szacowany
  koszt od pozostałego budżetu pieniędzy bieżącego miesiąca — na podstawie
  daty ukończenia zadania (nie daty utworzenia zadania). Priority: must-have
  > Socrates: Wynikło z rozmowy o mechanice budżetu — bez odejmowania
  > zużycia budżety w profilu byłyby statyczne i nieużyteczne w praktyce.
  > Rozstrzygnięcie: zadanie ukończone poza swoim "domyślnym" okresem
  > (np. przeciągnięte z poprzedniego tygodnia) obciąża budżet okresu, w
  > którym faktycznie zostało ukończone — bez proporcjonalnego rozliczania
  > wstecz.
- FR-016: Budżet czasu i budżet pieniędzy użytkownika resetują się do
  pełnej zadeklarowanej wartości na początku kolejnego kalendarzowego
  okresu — nowy tydzień dla budżetu czasu, nowy miesiąc dla budżetu
  pieniędzy — niezależnie od siebie i bez wpływu na wcześniej odjęte
  zużycie z poprzednich okresów. Priority: must-have
  > Socrates: Reset liczony jest od granic kalendarzowych, nie od daty
  > utworzenia profilu — prostsze mentalnie dla użytkownika i łatwiejsze
  > do zaimplementowania bez dodatkowego stanu.

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

## Business Logic

Aplikacja dobiera zestaw zadań remontowych mieszczący się w pozostałym
budżecie czasu (tygodniowym) i budżecie pieniędzy (miesięcznym) użytkownika,
traktując oba budżety jako twarde, nieprzekraczalne limity, i porządkuje
wybrane zadania według priorytetu.

Reguła konsumuje jako wejście: budżet czasu (godziny/tydzień) i budżet
pieniędzy (kwota/miesiąc) zadeklarowany przez użytkownika w profilu — każdy
z własnym, niezależnym okresem; dla każdego zadania — priorytet (1–5),
szacowany czas, szacowany koszt, status oraz zależności blokujące od innych
zadań. Wyjściem jest uporządkowana lista zadań, które łącznie mieszczą się
w dostępnym budżecie, wraz z krótkim, czytelnym uzasadnieniem odwołującym
się do priorytetu i zużycia budżetu. Zadania ukończone, aktywne, ręcznie
zablokowane lub z niespełnionymi zależnościami nigdy nie trafiają na
listę. Użytkownik spotyka tę regułę jako panel rekomendacji, który
przelicza się na bieżąco, gdy zmienia się priorytet zadań, ich estymaty
albo budżet użytkownika.

Oba budżety zużywają się w miarę kończenia zadań: gdy zadanie jest
oznaczane jako Done, jego szacowany czas obniża pozostały budżet czasu
bieżącego tygodnia, a jego szacowany koszt obniża pozostały budżet
pieniędzy bieżącego miesiąca — o tym, do którego okresu przypisać zużycie,
decyduje data ukończenia zadania. Oba budżety wracają do pełnej
zadeklarowanej wartości na początku kolejnego kalendarzowego okresu
(nowy tydzień dla czasu, nowy miesiąc dla pieniędzy), niezależnie od
siebie i bez wpływu na wcześniej odjęte zużycie z poprzednich okresów.

Brak oszacowanego kosztu lub czasu dla zadania nie może być automatycznie
traktowany jako zero — faworyzowałoby to nieoszacowane zadania. Reguła
przyjmuje neutralne wartości domyślne lub obniża pewność rekomendacji dla
takich zadań.

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
- **Import/eksport danych w ustandaryzowanym formacie geoprzestrzennym** —
  MVP przechowuje dane wewnętrznie w formacie projektu; wymiana z
  zewnętrznymi narzędziami geoprzestrzennymi to etap kolejny.
- **Zoom/pan/dopasowanie planu** — MVP zakłada stały widok planu; nawigacja
  po dużych/złożonych planach to etap kolejny.
- **Alternatywny renderer mapowy** — pierwszy i jedyny renderer w MVP to
  renderer natywny wybranego stosu UI; renderery mapowe/webowe odłożone.
- **Zewnętrzne repozytorium danych lub synchronizacja chmurowa** — MVP
  zapisuje projekt wyłącznie jako pojedynczy plik lokalny; brak wymogu
  bazy danych czy chmury.
- **Wielu użytkowników pracujących nad jednym projektem** — brak
  współdzielenia/współpracy w czasie rzeczywistym nad tym samym plikiem
  projektu; jeden użytkownik, jedno urządzenie na sesję.
- **Priorytety przypisane do pomieszczeń** — sam mechanizm istnieje w
  koncepcji, ale nie jest wymagany do działania rekomendacji w MVP;
  odłożony do kolejnego etapu.
- **Rysowanie planu wewnątrz aplikacji** — użytkownik przygotowuje plan
  poza aplikacją i go importuje; wbudowany edytor/rysownik geometrii planu
  to nie-cel MVP.
- **"Idealny/nieidealny" termin wykonania zadania jako tie-breaker** —
  rozważony podczas projektowania algorytmu rekomendacji, ale odłożony —
  MVP sortuje wyłącznie wg priorytetu w ramach budżetu.
- **Pełna odporność zapisu na awarie (zapis atomowy + kopia zapasowa)** —
  MVP używa prostego nadpisu pliku projektu; pełna odporność to kolejny
  etap.

## Open Questions

Brak otwartych pytań blokujących — `shape-notes.md` przeszło zamykający
quality cross-check ze statusem `accepted` (brak zidentyfikowanych luk).
