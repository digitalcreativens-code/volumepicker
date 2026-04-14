# VolumeGuard

WPF (.NET 8) utility for Windows that **enforces a maximum master volume** on **all active playback audio endpoints**, according to a **time schedule**, with **password protection**, **system tray** operation, **HKCU auto-start**, and a **watchdog** mode in the **same EXE**.

## Jedan fajl za distribuciju

Glavna i watchdog logika su u **istom** `VolumeGuard.exe`. Watchdog se pokreće sa argumentom:

```text
VolumeGuard.exe --watchdog
```

Glavna instanca automatski podiže watchdog; watchdog održava glavnu instancu živom (osim pri **namernom** gašenju posle ispravne šifre — koristi se mali marker fajl da se izbegne trenutni restart).

### Kako da dobiješ gotov `VolumeGuard.exe` (bez Visual Studio)

Na **ovom Mac okruženju** ne mogu da ti generišem bajtove `.exe` fajla (nema instaliranog `dotnet` SDK-a, a WPF se ionako ne builduje na macOS-u). Izbori:

1. **GitHub Actions**  
   - Workflow fajl je u repou kao **`docs/github-workflow-build-windows-exe.yml`** (van `.github/`) zato što GitHub **odbija push** workflow-a preko HTTPS tokena koji nema scope **`workflow`**.  
   - **Opcija A:** na GitHubu otvori repo → **Add file** → **Create new file** → putanja `.github/workflows/build-windows-exe.yml` → nalepi sadržaj iz `docs/github-workflow-build-windows-exe.yml` → Commit. Zatim **Actions** → **Build Windows EXE** → **Run workflow** (ili push u `src/VolumeGuard/`).  
   - **Opcija B:** u GitHubu **Settings → Developer settings → Personal access tokens** dodaj scope **`workflow`** na token koji Cursor koristi, pa u lokalnom repou vrati isti YAML pod `.github/workflows/` i uradi `git push`.  
   - Artefakt: **VolumeGuard-win-x64-singlefile** → `VolumeGuard.exe`.

2. **Bilo koji Windows PC sa .NET 8 SDK**  
   - U PowerShell-u iz korena repoa:
   - `.\scripts\publish-windows.ps1`  
   - Izlaz: `dist\win-x64\VolumeGuard.exe` (self-contained single-file).

### Single-file publish (ručno)

Na Windows mašini, u korenu repoa:

```bash
dotnet publish .\src\VolumeGuard\VolumeGuard.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSingleFile=true
```

Izlaz je u `src\VolumeGuard\bin\Release\net8.0-windows\win-x64\publish\VolumeGuard.exe` (jedan veliki EXE + eventualni sidecar za native — zavisno od verzije SDK-a).

Za framework-dependent (manji, zahteva instaliran .NET 8 runtime):

```bash
dotnet publish .\src\VolumeGuard\VolumeGuard.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

## Razvoj / debug

```bash
dotnet build .\src\VolumeGuard\VolumeGuard.csproj -c Debug
dotnet run --project .\src\VolumeGuard\VolumeGuard.csproj
```

> Napomena: `TargetFramework` je `net8.0-windows` (WPF). **Build u Visual Studio na Windows-u** je primarni scenario; na drugim OS-ovima `dotnet build` može biti nedostupan bez Windows targeting paketa.

## Podešavanja

- JSON konfiguracija: `%LocalAppData%\VolumeGuard\config.json`
- Prvi start traži **master šifru** (čuva se kao **BCrypt** hash).
- Raspored po podrazumevanom:
  - 22:00–06:30 → max **20%**
  - 06:30–07:30 → max **40%**
  - 08:30–22:00 → max **85%**
  - **Rupa 07:30–08:30**: podrazumevano ponašanje drži **prethodni limit (40%)** dok ne krene sledeći slot.

## Šta aplikacija *stvarno* kontroliše

- **Master volume** na svim **aktivnim playback** endpoint-ima (tipično svi izlazni uređaji u Sound settings). To je ono što Windows mikser najčešće prikazuje kao glavni klizač po uređaju.
- **Per-app** klizači u mikseru su relativni; spuštanje master limita i dalje ograničava maksimalni izlaz u praksi za većinu aplikacija.
- Aplikacije koje koriste **exclusive mode** ili zaobilaze standardni mikser mogu biti izuzetak — to je ograničenje OS-a, ne ove aplikacije.

## Realnost oko „zaštite“

- **Običan korisnik** bez administratorskih privilegija teško gasi oba procesa i zaobilazi šifru kroz sam UI.
- **Administrator** i dalje može da ubije procese, obriše `Run` ključ, menja fajlove konfiguracije itd.
- Za jaču kontrolu (roditeljski / kiosk scenario) sledeći korak je **Windows servis** + policy / drugi nivo nadzora.

## Tehnologije

- .NET 8, WPF, MVVM gde ima smisla  
- **NAudio** (Core Audio) za master volume  
- **BCrypt.Net-Next** za hash lozinke  
- **HKCU** `...\Windows\CurrentVersion\Run` za auto-start
