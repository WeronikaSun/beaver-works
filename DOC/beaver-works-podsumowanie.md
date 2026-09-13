# Beaver-Works — podsumowanie ustaleń

> Zebrane z `context/foundation/prd.md` i `context/foundation/tech-stack.md` oraz
> z rozmowy prowadzonej podczas prób `/10x-tech-stack-selector` i `/10x-bootstrapper`.

## 1. Co to za projekt

**Beaver-Works** — desktopowa aplikacja C#/WPF do zarządzania zadaniami
remontowymi przypiętymi do konkretnych miejsc na planie mieszkania.

- **Kontekst**: greenfield, projekt niestandardowy (custom), budowany
  po godzinach przez jedną osobę.
- **Deadline**: twardy, 1 tydzień pracy po godzinach (hard deadline: 2026-09-14).
- **Skala**: mała (jeden użytkownik/kilku hobbystów, brak wymogu skalowania).

## 2. Problem i wizja

Zwykła lista zadań remontowych traci kontekst przestrzenny — nie widać, gdzie
w mieszkaniu dane zadanie dotyczy. Aplikacja łączy plan mieszkania z
zarządzaniem zadaniami: zadania są przypinane do konkretnych miejsc na planie,
a dodatkowo deterministyczna reguła rekomenduje kolejne zadanie do zrobienia
na podstawie priorytetu i dostępnego budżetu czasu/pieniędzy.

## 3. Persony

- **Główna**: autor projektu — osoba planująca/realizująca własny remont,
  która chce śledzić zadania przypięte do miejsc na planie mieszkania.
- **Drugorzędna**: hobbyści remontujący mieszkania szerzej — MVP nie jest pod
  nich projektowane wprost, ale to zamierzony kierunek rozwoju.

## 4. Kryteria sukcesu

**Podstawowe**
- Pełny przepływ end-to-end: logowanie → utworzenie/otwarcie projektu →
  wyświetlenie planu → kliknięcie miejsca → utworzenie zadania z markerem →
  edycja/usunięcie zadania → zapis i ponowne otwarcie bez utraty danych →
  wyświetlenie rekomendowanego kolejnego zadania z uzasadnieniem.
- Pierwszy pionowy fragment (utworzenie projektu → przykładowy plan →
  kliknięcie → tytuł zadania → kolorowy marker → zapis na dysk →
  zamknięcie/otwarcie → marker w tym samym miejscu) osiągalny w ≤ 1 tydzień.

**Drugorzędne**
- Uzasadnienie rekomendacji następnego zadania jest krótkie i zrozumiałe.

**Guardrails**
- Zapis/odczyt projektu nigdy nie traci ani nie przesuwa pozycji markerów.
- Hasła nigdy nie są przechowywane jawnym tekstem — wyłącznie jako bezpieczne
  skróty z solą.

## 5. Kluczowe historyjki użytkownika

**US-01 — dodanie zadania klikając plan i zapis projektu**
- Kliknięcie miejsca na planie + wpisanie tytułu tworzy zadanie z kolorowym
  markerem (domyślny status: Planned).
- Kolor markera aktualizuje się przy zmianie statusu.
- Po zapisaniu i ponownym otwarciu projektu zadanie i pozycja markera są
  zachowane identycznie (współrzędne 0–1, bez utraty precyzji poza ustalonym
  marginesem zaokrąglenia).
- Kliknięcie poza faktycznie wyświetlanym obszarem planu (poza marginesami
  przy letterboxingu) nie tworzy zadania w błędnym miejscu.

**US-02 — budżet i rekomendacja zadań**
- Użytkownik ustawia budżet czasu (tygodniowy) i budżet pieniędzy
  (miesięczny) w profilu.
- Widok rekomendacji pokazuje listę zadań posortowaną wg priorytetu,
  mieszczącą się łącznie w pozostałym budżecie — z krótkim uzasadnieniem.
- Zadania z niespełnionymi zależnościami nigdy nie trafiają na listę.
- Lista przelicza się na bieżąco przy zmianie priorytetu, estymat lub budżetu.
- Ukończenie zadania (Done) odejmuje jego czas/koszt od budżetu bieżącego
  okresu (wg daty ukończenia, nie utworzenia).
- Budżety resetują się do pełnej wartości na początku kolejnego okresu
  kalendarzowego (nowy tydzień dla czasu, nowy miesiąc dla pieniędzy) —
  niezależnie od siebie.

## 6. Wymagania funkcjonalne (skrót)

**Dostęp i projekt**
- FR-001 Lokalny ekran logowania (must-have; świadomie zostaje mimo tarcia
  dla jedynego użytkownika — mechanizm kontroli dostępu).
- FR-002 Utworzenie nowego projektu remontowego (must-have).
- FR-003 Otwarcie istniejącego projektu z dysku (must-have).
- FR-004 Wybór planu mieszkania — wbudowany przykładowy plan albo własny
  plik przygotowany poza aplikacją (must-have; rysowanie planu w aplikacji
  odrzucone jako poza zakresem MVP).

**Plan i zadania**
- FR-005 Plan mieszkania jako tło (must-have).
- FR-006 Kliknięcie miejsca na planie tworzy zadanie przypisane do miejsca
  (must-have).
- FR-007 Zadanie przechowuje: tytuł, opcjonalny opis, status, priorytet
  (1–5), szacowany koszt, szacowany czas, opcjonalny identyfikator
  pomieszczenia, geometrię punktową, zależności, daty utworzenia/aktualizacji
  (must-have).
- FR-008 Markery kolorowane wg statusu zadania (must-have).
- FR-009 Lista zadań + szczegóły wybranego zadania (must-have).
- FR-010 Edycja zadania, w tym zmiana statusu (must-have).
- FR-011 Usunięcie zadania (must-have).

**Trwałość danych**
- FR-012 Zapis projektu na dysku — prosty nadpis pliku w MVP, bez pełnej
  atomowości/kopii zapasowej (must-have; pełna odporność odłożona na
  później).

**Profil i rekomendacja**
- FR-013 Deklaracja budżetu czasu (godziny/tydzień) i budżetu pieniędzy
  (kwota/miesiąc) w profilu — niezależne okresy (must-have).
- FR-014 Rekomendacja listy zadań mieszczących się w budżecie czasu i
  pieniędzy jako twardych, nieprzekraczalnych limitach; zadania
  Done/Active/ręcznie Blocked oraz z niespełnionymi zależnościami wykluczone;
  krótkie uzasadnienie odwołujące się do priorytetu i budżetu (must-have;
  ważona punktacja priorytet/czas/koszt odrzucona jako zbyt niepewna bez
  danych — zastąpiona prostszym doborem wg priorytetu w ramach budżetu).
- FR-015 Ukończenie zadania odejmuje czas/koszt od budżetu bieżącego okresu
  wg daty ukończenia (must-have).
- FR-016 Reset budżetów na początku kolejnego okresu kalendarzowego,
  niezależnie od siebie (must-have).

## 7. Wymagania niefunkcjonalne

- Zmiana statusu zadania i koloru markera widoczna natychmiast, bez
  zauważalnego opóźnienia.
- Zapis projektu kończy się potwierdzeniem albo czytelnym komunikatem błędu,
  odczuwalnie natychmiastowy dla typowego rozmiaru projektu.
- Hasło nigdy nie opuszcza urządzenia jawnym tekstem i nigdy nie jest
  przechowywane w postaci możliwej do odczytania.
- Pełna użyteczność offline — podstawowy przepływ MVP nie wymaga sieci.
- Utrata zasilania/awaria podczas zapisu nie może uszkodzić pliku projektu
  bardziej niż utrata bieżącej, niezapisanej zmiany.

## 8. Logika biznesowa (silnik rekomendacji)

Dobiera zestaw zadań mieszczący się łącznie w pozostałym budżecie czasu
(tygodniowym) i budżecie pieniędzy (miesięcznym), traktując oba jako twarde
limity, i porządkuje wybrane zadania wg priorytetu.

- Wejście: budżet czasu i budżet pieniędzy użytkownika (niezależne okresy);
  dla każdego zadania — priorytet (1–5), szacowany czas, szacowany koszt,
  status, zależności blokujące.
- Wyjście: uporządkowana lista zadań mieszczących się w budżecie + krótkie
  uzasadnienie odwołujące się do priorytetu i zużycia budżetu.
- Zadania ukończone/aktywne/ręcznie zablokowane/z niespełnionymi
  zależnościami nigdy nie trafiają na listę.
- Panel rekomendacji przelicza się na bieżąco przy zmianie priorytetów,
  estymat lub budżetu.
- Ukończenie zadania (Done) zmniejsza oba budżety wg daty ukończenia.
- Oba budżety wracają do pełnej wartości na początku kolejnego okresu
  kalendarzowego, niezależnie od siebie, bez wpływu na wcześniejsze zużycie.
- Brak oszacowanego kosztu/czasu NIE jest traktowany jako zero (to
  faworyzowałoby nieoszacowane zadania) — reguła przyjmuje neutralne
  wartości domyślne lub obniża pewność rekomendacji dla takich zadań.

## 9. Kontrola dostępu

- Prosty lokalny ekran logowania z lokalnymi profilami użytkowników.
- Hasła przechowywane wyłącznie jako bezpieczne skróty z solą (standardowy
  mechanizm platformy, bez własnej kryptografii).
- Dane uwierzytelniania przechowywane poza plikiem projektu.
- Po zalogowaniu: lista ostatnich projektów użytkownika.
- Model płaski — jeden zalogowany użytkownik ma pełny dostęp do swoich
  projektów, brak ról (admin/gość) w MVP.
- Rozważone i odrzucone dla MVP (mogą wrócić później): logowanie kontem
  systemu operacyjnego, PIN przypisany do projektu, tryb
  właściciel/tylko-do-odczytu.

## 10. Poza zakresem MVP (Non-Goals)

- Pomieszczenia jako poligony (geometria).
- Automatyczne przypisanie zadania do pomieszczenia (point-in-polygon).
- Kolorowanie pomieszczeń wg postępu remontu.
- Linie instalacji (elektryczna/hydrauliczna/internetowa).
- Import/eksport w ustandaryzowanym formacie geoprzestrzennym.
- Zoom/pan/dopasowanie planu (stały widok w MVP).
- Alternatywny renderer mapowy (tylko natywny renderer wybranego stosu UI).
- Zewnętrzne repozytorium danych / synchronizacja chmurowa (tylko lokalny
  plik).
- Wielu użytkowników pracujących nad jednym projektem jednocześnie.
- Priorytety przypisane do pomieszczeń.
- Rysowanie planu wewnątrz aplikacji (plan przygotowywany poza aplikacją).
- "Idealny/nieidealny" termin wykonania jako tie-breaker rekomendacji.
- Pełna odporność zapisu na awarie (zapis atomowy + kopia zapasowa).

## 11. Wybrany stos technologiczny

**Platforma**: C# / .NET (aktualna stabilna wersja SDK wspierana przez
Visual Studio) / **WPF** z wzorcem **MVVM**.

**Kluczowe biblioteki i narzędzia**:
- `CommunityToolkit.Mvvm` — ViewModele, komendy, powiadomienia.
- `System.Text.Json` — serializacja projektu do pliku.
- `xUnit` — testy jednostkowe i integracyjne.
- `FlaUI` (lub równoważne narzędzie) — co najmniej jeden test UI z
  perspektywy użytkownika.

**Dystrybucja**: self-contained package / instalator / MSIX, jeśli
proporcjonalne do zakresu projektu (zapisane w hand-offie jako `self-host` —
najbliższa dostępna wartość, żaden z hostowanych w chmurze celów wdrożenia
karty rejestru nie pasuje do aplikacji desktopowej).

**CI/CD**: świadomie odłożone. `github-actions` / `manual-promotion`
zapisane wyłącznie jako placeholder schematu — żaden pipeline nie powinien
być generowany, dopóki nie zostanie o to poproszone wprost.

**Dlaczego ten stos**: doświadczenie autora w C#/.NET/WPF pod twardym,
tygodniowym deadlinem po godzinach; alternatywy z rejestru (Rust+Tauri,
Dart+Flutter) byłyby technicznie silniejsze dla desktopu, ale nieznane
autorowi i zbyt ryzykowne do nauki pod presją czasu.

## 12. Znany brak w narzędziach 10x (ważne!)

Rejestr starterów używany przez `/10x-tech-stack-selector` i
`/10x-bootstrapper` **nie ma dedykowanej karty WPF/MVVM**. Jedyna karta .NET
w rejestrze scaffolduje **ASP.NET Core Web API** (`dotnet new webapi`), co
jest złym typem projektu dla tej aplikacji.

**Wniosek**: `/10x-bootstrapper` nie powinien być używany do scaffoldingu
tego projektu — wygenerowałby niewłaściwy szkielet (Web API zamiast WPF).

**Ustalony plan działania**:
1. Ręczne założenie projektu WPF (`dotnet new wpf`), projektu testów xUnit,
   projektu testów FlaUI, dodanie pakietów `CommunityToolkit.Mvvm` i
   `System.Text.Json`, ułożenie struktury MVVM (Views/ViewModels/Models).
2. Zachowanie `context/` (w tym ten plik i `prd.md`, `tech-stack.md`) bez
   zmian w repozytorium.
3. Uruchomienie `/10x-health-check` na już założonym projekcie — audyt
   zależności, wykrycie test runnera, sprawdzenie CI/CD i lista braków z
   priorytetami — zamiast dalszych prób `/10x-bootstrapper`.

## 13. Hand-off (`context/foundation/tech-stack.md`) — stan na dziś

```yaml
starter_id: dotnet
package_manager: dotnet
project_name: beaver-works
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: self-host
  ci_provider: github-actions
  ci_default_flow: manual-promotion
  bootstrapper_confidence: best-effort
  path_taken: custom
  quality_override: true
  self_check_answers:
    typed: true
    from_official_starter: false
    conventions: true
    docs_current: true
    can_judge_agent: true
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: false
  has_background_jobs: false
```

`starter_id: dotnet` jest zapisany, bo to jedyna dostępna wartość dla tego
języka w rejestrze — ale, jak w sekcji 12, jego domyślna komenda nie pasuje
do WPF i nie powinna być użyta wprost.

## 14. Otwarte pytania

Brak — `shape-notes.md` przeszło zamykający quality cross-check ze statusem
`accepted` (brak zidentyfikowanych luk blokujących).
