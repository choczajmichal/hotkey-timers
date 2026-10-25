# Timerki

Bezpłatna aplikacja Windows 10/11 x64: trzy niezależne liczniki z globalnymi
skrótami klawiaturowymi, mini-panelem i powiadomieniami WAV lub głosem Windows.
Kod jest udostępniony na licencji [MIT](LICENSE).

## Informacja o autorstwie AI

Program został wykonany przy użyciu sztucznej inteligencji (AI), na podstawie
poleceń i wymagań opiekuna projektu. Kod aplikacji oraz dokumentacja powstały
z pomocą AI. Opiekunem projektu jest [choczajmichal](https://github.com/choczajmichal).

## Obsługa

- F6, F7, F8 uruchamiają lub restartują odpowiedni timer; skróty można zmienić.
- Klawisze nadal trafiają do aktywnej aplikacji. Program nie automatyzuje gry.
- Czas każdego timera można ustawić w sekundach (1–86400).
- Mini-panel pokazuje aktywne liczniki; można go przeciągać i włączyć przepuszczanie kliknięć.
- Zamknięcie głównego okna ukrywa je do zasobnika. Aby zakończyć program,
  wybierz „Zakończ aplikację” w oknie lub menu ikony zasobnika.
- Opcjonalny autostart dodaje wpis `Timerki` do rejestru bieżącego użytkownika
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Kompilacja

Na Windows zainstaluj .NET 8 SDK lub nowszy SDK obsługujący .NET 8 i uruchom:

```powershell
dotnet publish Timerki/Timerki.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o gotowy-exe
```

Można też użyć `build-exe.bat`. W folderze `gotowy-exe` znajduje się aplikacja
oraz biblioteki natywne środowiska .NET: przenoś cały folder.
Workflow [build.yml](.github/workflows/build.yml) buduje ten sam wariant na GitHub Actions.
Artefakty workflow są obecnie **niepodpisane** i służą do testów oraz przygotowania wydania.

## Dane, bezpieczeństwo i usunięcie

Ustawienia `settings.json` i wybrane dźwięki w `Sounds` są zapisywane przy EXE.
Użyj folderu z prawem zapisu. Przy pierwszym uruchomieniu możliwa jest migracja
ustawień ze starszej wersji z `%APPDATA%\Timerki`.
Nie publikuj swoich ustawień ani prywatnych nagrań razem ze źródłami.

Globalny hook obserwuje naciśnięcia klawiszy w pamięci, aby obsługiwać skróty;
nie zapisuje historii pisania i nie blokuje przekazywania klawiszy innym programom.
Aplikacja nie zawiera telemetrii, aktualizatora ani kodu komunikacji sieciowej.
Głos jest realizowany przez Windows SAPI; biblioteki .NET i system Windows
podlegają własnym licencjom i ustawieniom prywatności.

Aby usunąć aplikację, wyłącz jej autostart, zakończ ją z zasobnika i usuń folder.
Jeśli folder został usunięty wcześniej, usuń wyłącznie wartość `Timerki` z powyższego
klucza autostartu. Dane starszej wersji w `%APPDATA%\Timerki` można usunąć osobno.
Mini-panel może być niewidoczny nad grą w pełnym ekranie exclusive;
gra lub jej system zabezpieczeń mogą ograniczyć działanie skrótów.

## Code signing policy

**Status: projekt przygotowywany do zgłoszenia; brak akceptacji SignPath i brak podpisanych wydań.**
Szczegóły: [polityka podpisywania](CODE_SIGNING.md).
Niepodpisane EXE może być blokowane przez Inteligentną kontrolę aplikacji.
