# CaffeineCitrix64 (awake-citrix-rdp-sessions)

[![GitHub Release](https://img.shields.io/github/v/release/aalies/awake-citrix-rdp-sessions)](https://github.com/aalies/awake-citrix-rdp-sessions/releases/latest)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/aalies/awake-citrix-rdp-sessions/blob/master/LICENSE.md)

64-Bit-Keep-Awake-Utility (`.NET 8` / Windows Forms) für die **Citrix Workspace App** (`wfica32` / `CDViewer`) sowie **Windows App / RDP** mit System-Tray-Steuerung, Aktivitäts-/Vordergrundprüfung und Live-Debug-Log.

## Download & Binaries

Fertige Release-Binaries (`win-x64`) stehen im [GitHub Releases Bereich](https://github.com/aalies/awake-citrix-rdp-sessions/releases/latest) zum Download bereit (`awake-citrix-rdp-sessions-win-x64.zip`).

## Warum dieses Tool?

- **64-Bit statt veraltetes 32-Bit-COM:** Klassische `WFICALib`-Ansätze scheitern in aktuellen 64-Bit-Umgebungen häufig (`REGDB_E_CLASSNOTREG` / `0x80040154`) oder erfordern Registry-/COM-Eingriffe. Unterstützt neben Citrix Workspace auch **Microsoft Windows App / Remote Desktop** (`msrdc`, `remotedesktop`, `desktopviewer`).
- **Kein Stören bei aktiver Arbeit in Citrix (Per-Session-Prüfung):** Wenn eine bestimmte Citrix- oder Remote-Session im Vordergrund aktiv ist und darin Benutzereingaben (`GetLastInputInfo`) erkannt wurden, wird der Refresh **nur für diese aktive Session** übersprungen – parallele Hintergrund-Sessions erhalten weiterhin ihre Keep-Awake-Impulse, und Chat-Eingaben (z. B. in MS Teams) verlieren nicht den Fokus.
- **Kein `WM_ACTIVATE`-Fokusraub:** Impulse werden gezielt über `PostMessage` an Hauptfenster (sowie sichtbare Kindfenster bei Citrix) gesendet, während bei Windows App / RDP (`msrdc`, `remotedesktop`, `desktopviewer`) Kindfenster übersprungen werden, um Audio-Beeps in Eingabe-Capture-Fenstern zu verhindern.
- **Keine Registry Einträge nötig** Es wird kein Registry Eintrag benötigt und das Tool kommt komplett ohne Installation oder Admin Berechtigungen aus.

## Funktionen

- **System-Tray-Icon** mit Statusanzeige (Aktiv / Pausiert) und Linksklick zum Umschalten.
- **Einzelinstanz-Schutz (Single-Instance):** Garantiert über einen globalen `Mutex`, dass das Programm immer nur einmal gleichzeitig läuft.
- **Zwei wählbare Impuls-Modi** über das Tray-Menü:
  - **F15-Taste (`VK_F15` ohne `WM_ACTIVATE`):** **Standardmodus** (`_useMouseMoveOnly = false`), hochzuverlässig bei Citrix VDA und RDP/Windows App, da ein virtueller Funktionstastendruck (`WM_KEYDOWN` / `WM_KEYUP`) vom Remote-System als echte Benutzeraktivität gewertet wird.
  - **Maus-Impuls (`WM_MOUSEMOVE` an `(1,1)`):** **Alternativer Modus** (`_useMouseMoveOnly = true`), sendet eine einzelne `WM_MOUSEMOVE`-Nachricht an Koordinate `(1, 1)` im Fenster (schonend, aber manche Sitzungen ignorieren statische Mausbewegungen).
- **Konfigurierbare Ziel-Keywörter (`TargetKeywords`)** in [`caffeine.properties`](bin/Release/net8.0-windows/win-x64/caffeine.properties:1) (Standard: `wfica,cdviewer,citrix,msrdc,remotedesktop,desktopviewer`) mit automatischem Ausschluss des eigenen Prozesses (`Environment.ProcessId`), um Selbstansprachen bei geöffnetem Debug-Log-Fenster zu verhindern.
- **Live-Debug-Logfenster** (`Debug-Log anzeigen`) sowie persistente Logdatei `caffeine_debug.log` im Ausgabeverzeichnis.
- **Soforttest** (`Jetzt sofort testen`) zum Prüfen der erkannten Fenster und Sendevorgänge.

## Voraussetzungen & Build (VS Code)

1. **.NET 8 SDK** installieren (`winget install Microsoft.DotNet.SDK.8`) und VS Code neu starten.
2. Im Projektordner ([`Main.cs`](Main.cs:1) und [`CaffeineCitrix64.csproj`](CaffeineCitrix64.csproj:1)) im VS-Code-Terminal bauen:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Autostart einrichten

Damit **CaffeineCitrix64** bei der Windows-Anmeldung automatisch im System-Tray startet, können Sie eine Verknüpfung im Windows-Autostart-Ordner ablegen:

1. Kompilieren Sie das Projekt oder laden Sie das aktuelle Release herunter (`awake-citrix-rdp-sessions-win-x64.zip`).
2. Drücken Sie die Tastenkombination `Win + R`, geben Sie `shell:startup` ein und bestätigen Sie mit **Enter** (dies öffnet den Autostart-Ordner des aktuellen Benutzers).
3. Erstellen Sie in diesem Ordner eine Verknüpfung zur kompilierten `CaffeineCitrix64.exe` (z. B. aus dem Verzeichnis `bin/Release/net8.0-windows/win-x64/publish/`).
4. Beim nächsten Windows-Start wird das Tool automatisch im Hintergrund bzw. im System-Tray ausgeführt.

## Lizenz & Open Source

Dieses Projekt ist als Open Source Software unter der **GNU General Public License v3.0 (GNU GPLv3)** veröffentlicht. 
Siehe [`LICENSE.md`](LICENSE.md:1) for Details.

**Haftungsausschluss (As-Is):**
Die Software wird "wie besehen" (AS IS) ohne ausdrückliche oder stillschweigende Gewährleistung bereitgestellt.
