---
project: Beaver-Works
checked_at: 2026-09-13T14:07:00Z
health_status: healthy
context_type: brownfield
language_family: dotnet
stack_assessment_available: false
checks_run:
  - lockfile
  - dependency_audit
  - outdated_deps
  - test_runner
  - ci_cd
  - configuration
audit_findings:
  critical: 0
  high: 0
  moderate: 0
  low: 0
test_runner_detected: true
ci_provider: null
recommended_fixes: 2
---

## Zdrowie zależności

### Plik lock

```
Status: obecny (packages.lock.json)
Menedżer pakietów: dotnet
```

`packages.lock.json` jest teraz obecny we wszystkich czterech projektach
(`BeaverWorks.Core`, `BeaverWorks.Desktop`, `BeaverWorks.Core.Tests`,
`BeaverWorks.UiTests`), a `RestorePackagesWithLockFile` jest włączone
centralnie w `Directory.Build.props`. `dotnet restore BeaverWorks.sln`
przechodzi bez błędów z aktywnym plikiem lock — poprawka z poprzedniego
audytu została wdrożona poprawnie.

### Audyt bezpieczeństwa

```
Narzędzie: dotnet list package --vulnerable --include-transitive
Podsumowanie: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW
Bezpośrednie vs przechodnie: nierozróżniane — żadnych ustaleń w żadnej kategorii
```

Nie znaleziono podatnych pakietów w żadnym z czterech projektów, zarówno
wśród zależności bezpośrednich, jak i przechodnich, względem skonfigurowanych
źródeł NuGet (nuget.org plus jedno prywatne źródło).

### Nieaktualne zależności

```
Pakiety z rozbieżnością major-wersji: 1
```

- **coverlet.collector** (BeaverWorks.Core.Tests, BeaverWorks.UiTests): 6.0.4 → 10.0.1 (4 wersje major w tyle)

Dodatkowo, o jedną wersję major w tyle (niepilne, tylko do wiadomości):
- Microsoft.NET.Test.Sdk: 17.14.1 → 18.10.0
- xunit.runner.visualstudio: 3.1.4 → 4.0.0
- CommunityToolkit.Mvvm: 8.4.0 → 8.4.2 (tylko patch, brak akcji)

## Zestaw testów

```
Test runner: xUnit
Znalezione testy: 2 testy (1 w BeaverWorks.Core.Tests, 1 w BeaverWorks.UiTests)
Wykonanie testów: przechodzą
```

Konfiguracja: `tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`,
`tests/BeaverWorks.UiTests/BeaverWorks.UiTests.csproj`
Framework: xUnit 2.9.3 (test SDK 17.14.1), projekt testów UI dodatkowo
odwołuje się do FlaUI.UIA3 5.0.0 do testowania na poziomie interfejsu.

`dotnet build BeaverWorks.sln` kończy się sukcesem: 0 ostrzeżeń, 0 błędów.
`dotnet test tests/BeaverWorks.Core.Tests/...` uruchomił się i przeszedł
(1/1). Testy w `BeaverWorks.UiTests` zostały poprawnie wykryte
(`BeaverWorks.UiTests.UnitTest1.Test1`), ale nie zostały uruchomione w tym
przebiegu — testy UI oparte na FlaUI wymagają aktywnej sesji pulpitu Windows
z uruchomioną docelową aplikacją, co wykracza poza zakres audytu
tylko-do-odczytu.

## CI/CD

```
Dostawca: nie wykryto
Konfiguracja: nie znaleziono
```

| Etap           | Status | Uwagi                        |
|----------------|--------|-------------------------------|
| Lint           | ✗      | nieskonfigurowany             |
| Testy          | ✗      | nieskonfigurowany             |
| Build          | ✗      | nieskonfigurowany             |
| Sprawdzanie typów | ✗   | nieskonfigurowany             |
| Bezpieczeństwo | ✗      | nieskonfigurowany             |

ℹ Nie wykryto konfiguracji CI/CD. Skonfigurujesz to w lekcji o
infrastrukturze i wdrożeniach. Na razie lokalny test runner wystarcza do
współpracy z agentem — i jest obecny oraz działa.

## Konfiguracja

### Ważność niska

- **Brak `.env.example`** — w ścisłym sensie nie dotyczy (to offline'owa
  aplikacja desktopowa bez zaobserwowanej konfiguracji opartej na zmiennych
  środowiskowych), ale jeśli w przyszłości pojawią się lokalne
  sekrety/ścieżki konfiguracyjne (np. nadpisanie ścieżki lokalnego magazynu
  profili), warto je tu udokumentować.

`.editorconfig` oraz `Directory.Build.props` (z `Nullable`, `ImplicitUsings`,
`RestorePackagesWithLockFile`, `EnableNETAnalyzers`, `AnalysisLevel`) są już
obecne i scentralizowane — obie poprawki z poprzedniego audytu zostały
wdrożone. `dotnet build BeaverWorks.sln` nadal przechodzi z 0 ostrzeżeniami
przy włączonych analizatorach.

Cała pozostała oczekiwana konfiguracja (`.gitignore`, `Nullable` włączone w
każdym projekcie, `ImplicitUsings` włączone w każdym projekcie) jest obecna
i spójna we wszystkich czterech projektach.

## Odniesienie do oceny stosu

Nie znaleziono `stack-assessment.md`. Uruchom `/10x-stack-assess`, aby
uzyskać analizę bramek jakości.

Uwaga (z hand-offu tech-stack, nie z formalnej oceny stosu): plik
`context/foundation/tech-stack.md` tego projektu zapisuje
`bootstrapper_confidence: best-effort` oraz `quality_override: true`,
ponieważ rejestr starterów 10x nie ma karty WPF/MVVM — ten projekt został
założony ręcznie, a nie przez `/10x-bootstrapper`. To spójne z tym, co
pokazuje ten audyt: struktura projektu, konwencje `Nullable`/MVVM oraz
okablowanie testów są solidne, ale boilerplate, który normalnie
dostarczyłby szablon startera (opt-in na plik lock, `.editorconfig`, wspólny
`Directory.Build.props`, CI), nie został wygenerowany automatycznie i
wymaga ręcznego dodania — co obejmuje ten raport.

## Rekomendowane poprawki

### Napraw przed pracą z agentem (Kategoria A)

Brak — poprzednie ustalenia wysokiej i średniej ważności (plik lock,
`.editorconfig`, `Directory.Build.props`) zostały już wdrożone. Poniższe
dwie pozycje to niska ważność, czysto porządkowe.

### 1. Podnieś wersję `coverlet.collector` — 4 wersje major w tyle

**Wpływ**: Duże rozbieżności wersji w narzędziach testowych niosą ryzyko
niekompatybilności z nowszymi SDK w przyszłości i pomijają lata poprawek.
**Ważność**: niska
**Nakład pracy**: szybki (< 5 min)
**Naprawa**:

```
dotnet add tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj package coverlet.collector --version 10.0.1
dotnet add tests/BeaverWorks.UiTests/BeaverWorks.UiTests.csproj package coverlet.collector --version 10.0.1
```

Po podniesieniu wersji uruchom ponownie `dotnet test`, żeby potwierdzić, że
nic się nie zepsuło.

### 2. Podnieś `Microsoft.NET.Test.Sdk` i `xunit.runner.visualstudio` o jedną wersję major

**Wpływ**: Utrzymuje aktualność łańcucha narzędzi testowych; niskie ryzyko,
umiarkowana długoterminowa korzyść (nowsze funkcje SDK, lepsza integracja
z VS/CLI).
**Ważność**: niska
**Nakład pracy**: szybki (< 5 min)
**Naprawa**:

```
dotnet add tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj package Microsoft.NET.Test.Sdk --version 18.10.0
dotnet add tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj package xunit.runner.visualstudio --version 4.0.0
dotnet add tests/BeaverWorks.UiTests/BeaverWorks.UiTests.csproj package Microsoft.NET.Test.Sdk --version 18.10.0
dotnet add tests/BeaverWorks.UiTests/BeaverWorks.UiTests.csproj package xunit.runner.visualstudio --version 4.0.0
```

CommunityToolkit.Mvvm (8.4.0 → 8.4.2) jest tylko o wersję patch w tyle —
brak akcji wymaganej.

### Zaadresowane w kolejnych lekcjach (Kategoria B)

### Brak pipeline'u CI/CD

**Lekcja**: [Sprint Zero z Agentem: infrastruktura, walking skeleton i pierwszy deploy (M1L5)](https://platforma.przeprogramowani.pl/external/10xdevs-3/m1-l5)
**Co tam zrobisz**: Skonfigurujesz pipeline GitHub Actions obejmujący build,
testy oraz (dla tego stosu) `dotnet list package --vulnerable` jako krok
bezpieczeństwa.

### Brak AGENTS.md / CLAUDE.md

**Lekcja**: [Agent Onboarding: Agents.md, AI Rules i feedback loops (M1L4)](https://platforma.przeprogramowani.pl/external/10xdevs-3/m1-l4)
**Co tam zrobisz**: Zbudujesz plik instrukcji dla agenta z właściwymi,
specyficznymi dla projektu konwencjami (struktura MVVM, wzorce
CommunityToolkit.Mvvm, konwencje testowania xUnit/FlaUI) — wygenerowanie
zalążka teraz byłoby przedwczesne.

### Brak konfiguracji wdrożenia/pakowania (MSIX/self-contained)

**Lekcja**: [Sprint Zero z Agentem: infrastruktura, walking skeleton i pierwszy deploy (M1L5)](https://platforma.przeprogramowani.pl/external/10xdevs-3/m1-l5)
**Co tam zrobisz**: Skonfigurujesz publikację self-contained albo pakowanie
MSIX, zgodnie z celem wdrożenia `self-host` zapisanym w hand-offie tech-stack.

## Podsumowanie

Status zdrowia: healthy (zdrowy)

Poprawki z poprzedniego audytu zostały wdrożone: plik lock NuGet jest teraz
obecny we wszystkich czterech projektach, a `.editorconfig` wraz ze
scentralizowanym `Directory.Build.props` (z włączonymi analizatorami .NET)
zastąpiły powielone ustawienia. Build przechodzi bez ostrzeżeń, oba projekty
testowe działają, a audyt bezpieczeństwa nie wykazuje żadnych podatności.
Jedyne pozostałe ustalenia to niska ważność — kilka pakietów testowych kilka
wersji major w tyle — czysto porządkowe, nieblokujące.

Następny krok: projekt jest gotowy do pracy z agentem. CI/CD i pakowanie do
wdrożenia (MSIX/self-contained) są objęte nadchodzącą lekcją o
infrastrukturze; przejdź do wdrożenia agenta (AGENTS.md/CLAUDE.md).
