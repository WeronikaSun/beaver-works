# Beaver-Works

Desktopowa aplikacja C#/WPF do planowania remontu na planie mieszkania.

## Wymagania

- Windows
- SDK .NET 10.0.300 (wersja przypięta w global.json)
- Internet przy pierwszym pobraniu pakietów NuGet

## Struktura

- src/BeaverWorks.Desktop - interfejs WPF i MVVM
- src/BeaverWorks.Core - modele, logika i zapis danych
- tests/BeaverWorks.Core.Tests - testy logiki i zapisu
- tests/BeaverWorks.UiTests - testy interfejsu z FlaUI
- DOC - dokumentacja i ustalenia z 10x

## Uruchomienie

W PowerShell, z głównego katalogu projektu:

```powershell
dotnet restore BeaverWorks.sln
dotnet build BeaverWorks.sln --no-restore
dotnet test BeaverWorks.sln --no-build
dotnet run --project src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj --no-build
```

W Visual Studio otwórz BeaverWorks.sln i ustaw BeaverWorks.Desktop jako projekt startowy.

## Aktualny stan

Gotowy szkielet: cztery projekty, referencje, CommunityToolkit.Mvvm 8.4.0 i FlaUI.UIA3 5.0.0.
Aplikacja wyświetla puste okno. Dwa testy szablonowe sprawdzają wyłącznie konfigurację uruchamiania testów.
Funkcje produktu i rzeczywiste testy nie są jeszcze zaimplementowane.

Ustalenia produktowe: [podsumowanie 10x](DOC/beaver-works-podsumowanie.md).
Następny etap: projekt, przykładowy plan, dodanie zadania kliknięciem i zapis/odczyt pozycji markera.
