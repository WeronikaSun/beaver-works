---
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
---

## Why this stack

Programista solo z twardym tygodniowym deadlinem po godzinach i realnym
doświadczeniem w C#/.NET/WPF zdecydował się zostać przy znanym, produktywnym
stosie zamiast uczyć się nowego (Rust+Tauri albo Dart+Flutter) pod presją
czasu. `dotnet` to jedyna karta .NET w rejestrze starterów, ale jej domyślna
komenda scaffoldująca tworzy ASP.NET Core Web API (`dotnet new webapi`), a
NIE aplikację desktopową WPF — w rejestrze nie ma w ogóle dedykowanej karty
WPF/MVVM, więc jest to wybór ścieżki niestandardowej ze świadomą, jawnie
zapisaną luką: `from_official_starter` to false, pewność bootstrappera to
best-effort, a domyślna komenda scaffoldująca dla tego starter_id jest BŁĘDNA
dla tego projektu i musi zostać nadpisana albo pominięta przy uruchamianiu
bootstrappera. Docelowy stos to aplikacja WPF/MVVM w C# na aktualnej,
stabilnej wersji .NET SDK wspieranej przez Visual Studio, z
CommunityToolkit.Mvvm do ViewModeli/komend/powiadomień, System.Text.Json do
serializacji, xUnit do testów jednostkowych i integracyjnych oraz FlaUI (lub
odpowiednikiem) do co najmniej jednego testu z perspektywy interfejsu
użytkownika. Dystrybucja to self-contained/instalator/MSIX, jeżeli będzie to
proporcjonalne do zakresu projektu — zapisane tutaj jako `self-host`
(najbliższa dostępna wartość enum; żaden z domyślnych celów wdrożenia karty,
skierowanych na hosting chmurowy, nie pasuje do aplikacji desktopowej). C# jest
silnie typowany, WPF/MVVM to dobrze udokumentowany, konwencjonalny wzorzec, a
autor potrafi ocenić działanie agenta względem tego stosu — dlatego
self-check przeszedł pozytywnie na wszystkich punktach poza pochodzeniem z
oficjalnego startera. CI/CD jest świadomie odłożone: `github-actions` /
`manual-promotion` są zapisane wyłącznie jako placeholder schematu — żaden
pipeline nie powinien zostać wygenerowany, dopóki nie zostanie o to
poproszone.
