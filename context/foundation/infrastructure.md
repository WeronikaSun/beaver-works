---
project: beaver-works
researched_at: 2026-09-13
recommended_platform: GitHub Releases + samowystarczalny plik EXE (single-file)
runner_up: Tradycyjny instalator (Inno Setup) dołączany do GitHub Releases
context_type: mvp
tech_stack:
  language: C# (.NET 10.0.300, rollForward disable)
  framework: WPF / MVVM (CommunityToolkit.Mvvm)
  runtime: net10.0-windows, self-host
---

> **Uwaga o adaptacji**: domyślna pula kandydatów tego skilla (Cloudflare,
> Vercel, Netlify, Fly.io, Railway, Render) dotyczy hostingu web/cloud.
> Beaver-Works to autorska aplikacja desktopowa na Windows z
> `deployment_target: self-host` (`context/foundation/tech-stack.md`) oraz
> jawnym non-goalem w PRD: "zewnętrzne repozytorium danych lub synchronizacja
> chmurowa" — nie ma tu żadnego komponentu serwerowego do hostowania. Na
> życzenie użytkownika badanie zostało przekierowane na **mechanizmy
> dystrybucji/aktualizacji aplikacji desktopowej**, ocenione względem tych
> samych pięciu kryteriów przyjaznych agentowi (CLI-first, minimalna
> infrastruktura do utrzymania, dokumentacja czytelna dla agenta, stabilny i
> skryptowalny proces wydania, integracja z agentem/CLI) — kandydatami są
> tu sposoby pakowania i kanały wydania, a nie platformy hostingowe.

## Rekomendacja

**Dystrybucja przez GitHub Releases jako samowystarczalny (self-contained),
jednoplikowy EXE `win-x64` budowany przez `dotnet publish`.**

To rozwiązanie pasuje do wszystkich odpowiedzi z wywiadu: ręczne aktualizacje
są akceptowalne (nie potrzeba infrastruktury auto-update), priorytetem jest
minimalny nakład pracy przy pakowaniu (bez pisania instalatora), autor zna
już GitHub, a dystrybucja jest ściśle prywatna (brak wpisu w Microsoft Store,
więc brak narzutu certyfikacji/polityk sklepu). Nie wymaga też żadnego
bieżącego "hostingu" — artefakt to statyczny plik dołączony do tagu GitHub
Release.

## Porównanie opcji

| Opcja | CLI-first | Narzut operacyjny | Dokumentacja czytelna dla agenta | Stabilny, skryptowalny release | Integracja z agentem/CLI | Suma |
|---|---|---|---|---|---|---|
| GitHub Releases + self-contained EXE | Pass | Pass (brak infra) | Pass | Pass | Pass (`gh release create`) | 5 Pass |
| Tradycyjny instalator (Inno Setup/WiX) + GitHub Releases | Partial | Pass | Partial | Partial | Partial | 2 Pass / 3 Partial |
| ClickOnce | Partial | Pass | Partial | Fail (zorientowany na kreator VS, trudny do skryptowania) | Fail | 1 Pass / 2 Partial / 2 Fail |
| MSIX (sideload lub Store) | Partial | Partial (zarządzanie certyfikatem/podpisem) | Pass | Partial | Partial | 1 Pass / 3 Partial / 1 Fail |

### Krótka lista opcji

#### 1. GitHub Releases + samowystarczalny, jednoplikowy EXE (Rekomendowane)

`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true`
produkuje jeden plik `.exe` bez zewnętrznej zależności od zainstalowanego
środowiska .NET. To zwykła komenda CLI, którą łatwo wpiąć w krok
`gh release create`, jeśli kiedyś powstanie pipeline, i nie wymaga żadnej
infrastruktury podpisywania/sklepu dla prywatnej, jednoosobowej dystrybucji.
Bezpośrednio odpowiada na "minimalny wysiłek" + "ręczna aktualizacja" +
"znajomość GitHub" + "prywatna dystrybucja" z wywiadu.

#### 2. Tradycyjny instalator (Inno Setup / WiX) opakowujący ten sam EXE

Dodaje wpis w Menu Start, obsługę odinstalowania i przyjemniejsze pierwsze
uruchomienie, wciąż publikowany jako załącznik do GitHub Release. Sensowna
ścieżka rozwoju, gdyby priorytet "minimalny wysiłek" się zmienił — ale to
dodatkowa praca (skrypt `.iss`/`.wxs` do utrzymania), nieuzasadniona przy
1-tygodniowym, jednoosobowym MVP.

#### 3. MSIX

Najlepsza długoterminowa opcja, jeśli kiedykolwiek pojawi się potrzeba
dystrybucji przez Store albo sandboxingu klasy enterprise, a dokumentacja
Windows App SDK/WinUI już dziś kieruje nowe aplikacje w tę stronę. Dla tego
MVP dodaje narzut konfiguracji certyfikatu/podpisu i sideloadingu
nieproporcjonalny do ściśle prywatnej, jednourządzeniowej aplikacji, którą
PRD jawnie ogranicza do braku chmury/synchronizacji. Pozostaje jako trzecia
opcja na wypadek zmiany odpowiedzi o prywatnej dystrybucji.

(ClickOnce został rozważony, ale nie trafił na listę: pozostaje wspierany na
.NET 8–10, ale własne wytyczne Microsoftu traktują go jako legacy/tryb
utrzymaniowy obok MSIX, jego przepływ publikacji skoncentrowany na kreatorze
VS nie skryptuje się czysto jako krok wydania uruchamiany przez agenta, i nie
daje żadnej przewagi nad zwykłym self-contained EXE dla prywatnej,
jednoosobowej aplikacji.)

## Kontrola stronniczości (anti-bias): GitHub Releases + self-contained EXE

### Adwokat diabła — słabości

1. **Brak podpisu kryptograficznego** — niepodpisany EXE pobrany z GitHub
   wywoła ostrzeżenie SmartScreen "Nieznany wydawca" przy pierwszym
   uruchomieniu; dla solowej/prywatnej aplikacji to jednorazowe kliknięcie,
   ale realne tarcie, gdyby aplikacja miała być kiedykolwiek udostępniona
   komuś innemu.
2. **Ręczna dyscyplina aktualizacji to pojedynczy punkt awarii** — skoro Q1
   potwierdziło, że ręczne aktualizacje są ok, nic nie wymusza, że autor
   faktycznie pobierze nową wersję; nieaktualny EXE ze starym błędem (np.
   poprawką uszkodzenia pliku zapisu) może po cichu przetrwać.
3. **Publikacja self-contained single-file powiększa plik wykonywalny** do
   ok. 70–150 MB (dołącza pełne środowisko .NET) — bez znaczenia na dysku
   solowego użytkownika, ale zaskoczenie, gdyby autor chciał kiedyś
   swobodnie wysłać build mailem/udostępnić.
4. **Osobliwości ekstrakcji `PublishSingleFile`** — niektóre zależności
   natywne (interop COM, pewne przypadki brzegowe zasobów WPF) mogą
   zachowywać się inaczej po samo-ekstrakcji z jednego pliku niż przy
   zwykłej publikacji folderowej; warto zrobić test dymny na docelowej
   maszynie, nie tylko na maszynie deweloperskiej.
5. **GitHub Releases nie ma wbudowanego UX wersjonowania/rollbacku dla
   użytkownika końcowego** — powrót do poprzedniego builda wymaga ręcznego
   znalezienia i pobrania starszego otagowanego assetu; nie ma "jednej
   komendy rollbacku", jaką dałaby platforma hostingowa.

### Pre-mortem — jak to może się nie udać

Sześć miesięcy później autor wydał trzy self-contained buildy EXE do GitHub
Releases, każdy zastępujący poprzedni bez dyscypliny changeloga. Zmiana
formatu pliku zapisu w buildzie 3 po cichu psuje kompatybilność z projektami
utworzonymi w buildzie 1, a ponieważ nie ma auto-update, stary EXE leżący w
Downloads jest wciąż używany, ukrywając niekompatybilność aż do momentu, gdy
projekt zapisany już w buildzie 3 zostaje otwarty buildem 1 i pozycje
markerów wydają się przesunięte. Debugowanie jest trudne, bo nigdzie nie jest
zapisane, "który build EXE utworzył ten plik". Osobno, SmartScreen zaczął
agresywniej oznaczać najnowszy build po aktualizacji Windows, a bez
podpisywania kodu autor uznał to za fałszywy alarm — dopóki skan nie wykrył
prawdziwego fałszywie pozytywnego wskazania heurystyki antywirusowej, czego
dałoby się uniknąć podpisanym buildem i spójnymi notatkami wydania powiązanymi
z wersją schematu pliku projektu.

### Nieznane niewiadome

- **Wersjonowanie formatu pliku projektu nie było częścią guardrails w PRD
  poza "brak utraty danych przy zapisie/odczycie"** — nic obecnie nie
  zapisuje wersji schematu/aplikacji w zapisanym pliku projektu, więc
  przyszła zmiana formatu (np. dodanie poligonów pomieszczeń z "kolejnego
  etapu" wymienionego w non-goals PRD) nie ma dziś żadnej ścieżki
  wstecznej kompatybilności, gdy realne projekty już istnieją na dysku.
- **Reputacja SmartScreen jest kumulatywna i przypisana do konkretnego
  hasha pliku** — każdy nowy niepodpisany build zaczyna od zera reputacji;
  częste wydawanie niepodpisanego EXE będzie na nowo wywoływać ostrzeżenia
  przy *każdej* nowej wersji, nie tylko pierwszej.
- **`--self-contained true` przypina dokładne środowisko uruchomieniowe,
  z którym zbudowano EXE** — poprawka bezpieczeństwa do współdzielonego
  runtime .NET NIE dociera automatycznie do tej aplikacji tak, jak
  dotarłaby przy wdrożeniu zależnym od frameworka; autor musi przebudować i
  ponownie rozdystrybuować, by uwzględnić CVE w runtime, co łatwo przeoczyć
  przy rzadko dotykanym narzędziu osobistym.
- **`PublishReadyToRun` optymalizuje pod system operacyjny/architekturę
  maszyny budującej** — jeśli autor kiedykolwiek zbuduje na Windows ARM64
  lub innej wersji Windows niż maszyna docelowa, zoptymalizowany obraz R2R
  może po cichu wrócić do wolniejszych ścieżek JIT zamiast jawnie zawieść.

## Historia operacyjna

- **Wdrożenia podglądowe (preview)**: nie dotyczy — nie ma serwera; każdy
  build to pełny artefakt. "Podgląd" to po prostu uruchomienie świeżo
  opublikowanego EXE lokalnie przed otagowaniem GitHub Release.
- **Sekrety**: żadne nie są potrzebne dla MVP (brak usług chmurowych, brak
  kluczy API zgodnie z wymaganiem offline-first w PRD). Jeśli w przyszłości
  pipeline wydania będzie potrzebował certyfikatu podpisującego kod,
  przechowywać go jako zaszyfrowany sekret GitHub Actions, nigdy w
  repozytorium.
- **Rollback**: pobrać poprzedni otagowany asset z GitHub Releases i
  uruchomić go zamiast bieżącego EXE; nie ma żadnego stanu środowiska do
  cofnięcia, ponieważ aplikacja to pojedynczy lokalny plik plus własne pliki
  projektów użytkownika na dysku (a wybór "brak auto-update" oznacza, że
  rollback nigdy ich nie dotyka).
- **Zatwierdzenie (approval)**: opublikowanie nowego GitHub Release
  (otagowanie + wgranie builda) to jedyna akcja "produkcyjna", a przy tej
  skali dla solowego projektu to w całości decyzja autora — nie jest
  uzasadniona osobna bramka zatwierdzenia.
- **Logi**: nie ma żadnego logu runtime/serwera do śledzenia. Wynik
  build/publish odczytywany jest bezpośrednio z konsoli `dotnet publish`;
  jeśli w przyszłości powstanie workflow release w GitHub Actions,
  `gh run view --log` odczytuje go nieinteraktywnie.

## Rejestr ryzyk

| Ryzyko | Źródło | Prawdopodobieństwo | Wpływ | Mitigacja |
|---|---|---|---|---|
| Niepodpisany EXE wywołuje ostrzeżenia SmartScreen przy każdej nowej wersji | Adwokat diabła | W | N | Zaakceptować dla użytku solowego/prywatnego; dodać podpisywanie kodu tylko jeśli aplikacja zostanie kiedyś udostępniona poza autora |
| Brak wersji schematu/aplikacji zapisanej w plikach projektu | Nieznane niewiadome | Ś | Ś | Dodać proste pole `schemaVersion` do formatu pliku projektu już teraz, zanim powstanie więcej buildów, zgodnie z wymaganiem trwałości z FR-012 |
| Ręczna dyscyplina aktualizacji zależy od pamięci autora o ponownym pobraniu | Pre-mortem | Ś | Ś | Prowadzić krótki CHANGELOG.md przy każdym wydaniu; sprawdzać "czy jestem na najnowszym tagu" przed otwarciem istniejącego projektu po dłuższej przerwie |
| `--self-contained` przypina runtime; poprawki bezpieczeństwa wymagają ręcznej przebudowy | Nieznane niewiadome | N | Ś | Okresowo przebudowywać/republikować nawet bez zmian funkcjonalnych, gdy wychodzi wydanie bezpieczeństwa runtime .NET |
| Brak wbudowanego narzędzia rollback; cofnięcie to ręczne pobranie z GitHub | Adwokat diabła | N | N | Zachowywać każdy otagowany asset Release (domyślne zachowanie GitHub); opisać raz krok "jak zrobić rollback" w README repozytorium |

## Pierwsze kroki

1. Potwierdzić, że projekt Desktop już celuje w `net10.0-windows` z
   `<UseWPF>true</UseWPF>` (tak jest już w `Directory.Build.props`/csproj).
2. Opublikować lokalnie samowystarczalny, jednoplikowy build:
   `dotnet publish src\BeaverWorks.Desktop\BeaverWorks.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true`
3. Przetestować dymnie powstały EXE (w
   `bin\Release\net10.0-windows\win-x64\publish\`) na faktycznej maszynie
   docelowej, nie tylko na maszynie deweloperskiej, aby wychwycić
   osobliwości ekstrakcji single-file.
4. Utworzyć tag GitHub Release (np. `v0.1.0`) i dołączyć EXE jako załącznik
   wydania — przez interfejs webowy GitHub albo
   `gh release create v0.1.0 <ścieżka-do-exe>`, jeśli GitHub CLI jest
   zainstalowane.
5. Prowadzić krótki wpis `CHANGELOG.md` przy każdym tagu odnotowujący
   wszelkie zmiany formatu pliku projektu, aby przyszły autor mógł
   ustalić, który build zapisał który plik.

## Poza zakresem

Poniższe nie zostało zbadane w tym opracowaniu:
- Konfiguracja obrazu Docker (nie dotyczy — brak wdrożenia
  kontenerowego)
- Konfiguracja pipeline'u CI/CD (jawnie odłożona zgodnie z
  `context/foundation/tech-stack.md` — żaden `.github/workflows/` nie
  powinien powstać, dopóki nie zostanie o to poproszone)
- Infrastruktura auto-update, certyfikaty podpisywania kodu oraz wpis w
  Microsoft Store (wszystko jawnie zdepriorytetyzowane odpowiedziami z
  wywiadu; wrócić do tematu, jeśli zmieni się zakres dystrybucji)
- Architektura produkcyjna skalowana (multi-region, HA, DR — nie dotyczy
  jednoosobowej lokalnej aplikacji desktopowej)
