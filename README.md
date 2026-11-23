# CaffeineCitrix64 (awake-citrix-rdp-sessions)

[![GitHub Release](https://img.shields.io/github/v/release/aalies/awake-citrix-rdp-sessions)](https://github.com/aalies/awake-citrix-rdp-sessions/releases/latest)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/aalies/awake-citrix-rdp-sessions/blob/master/LICENSE.md)

64-Bit-Keep-Awake-Utility (`.NET 8` / Windows Forms) für die **Citrix Workspace App** (`wfica32` / `CDViewer`) sowie **Windows App / RDP** mit System-Tray-Steuerung, Aktivitäts-/Vordergrundprüfung und Live-Debug-Log.

## Download & Binaries

Fertige Release-Binaries (`win-x64`) stehen im [GitHub Releases Bereich](https://github.com/aalies/awake-citrix-rdp-sessions/releases/latest) zum Download bereit (`awake-citrix-rdp-sessions-win-x64.zip`).

## Warum dieses Tool?

- **64-Bit statt veraltetes 32-Bit-COM:** Klassische `WFICALib`-Ansätze scheitern in aktuellen 64-Bit-Umgebungen häufig (`REGDB_E_CLASSNOTREG` / `0x80040154`) oder erfordern Registry-/COM-Eingriffe. Unterstützt neben Citrix Workspace auch **Microsoft Windows App / Remote Desktop** (`msrdc`, `remotedesktop`, `desktopviewer`).
- **Kein Stören bei aktiver Arbeit in Citrix (Per-Session-Prüfung):** Wenn eine bestimmte Citrix- oder Remote-Session im Vordergrund aktiv ist und darin Benutzereingaben (`GetLastInputInfo`) erkannt wurden, wird der Refresh **nur für diese aktive Session** übersprungen – parallele Hintergrund-Sessions erhalten weiterhin ihre Keep-Awake-Impulse, und Chat-Eingaben (z. B. in MS Teams) verlieren nicht den Fokus.
- **Kein `WM_ACTIVATE`-Fokusraub:** Impulse werden gezielt über `PostMessage` an Haupt- und sichtbare Kindfenster gesendet, ohne künstliches Re-Aktivieren des Fensters.

## Funktionen

- **System-Tray-Icon** mit Statusanzeige (Aktiv / Pausiert) und Linksklick zum Umschalten.
- **Zwei wählbare Impuls-Modi** über das Tray-Menü:
  - **Maus-Impuls (`WM_MOUSEMOVE` an `(1,1)`):** Standardmodus, schonend für Texteingaben und Remote-Apps.
  - **F15-Taste (`VK_F15` ohne `WM_ACTIVATE`):** Alternative, falls `WM_MOUSEMOVE` von der Sitzung ignoriert wird.
- **Konfigurierbare Ziel-Keywörter (`TargetKeywords`)** in [`caffeine.properties`](bin/Release/net8.0-windows/win-x64/caffeine.properties:1) (Standard: `wfica,cdviewer,citrix,msrdc,remotedesktop,desktopviewer`) mit automatischem Ausschluss des eigenen Prozesses (`Environment.ProcessId`), um Selbstansprachen bei geöffnetem Debug-Log-Fenster zu verhindern.
- **Live-Debug-Logfenster** (`Debug-Log anzeigen`) sowie persistente Logdatei `caffeine_debug.log` im Ausgabeverzeichnis.
- **Soforttest** (`Jetzt sofort testen`) zum Prüfen der erkannten Fenster und Sendevorgänge.

## Voraussetzungen & Build (VS Code)

1. **.NET 8 SDK** installieren (`winget install Microsoft.DotNet.SDK.8`) und VS Code neu starten.
2. Im Projektordner ([`Main.cs`](Main.cs:1) und [`CaffeineCitrix64.csproj`](CaffeineCitrix64.csproj:1)) im VS-Code-Terminal bauen:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Lizenz & Open Source

Dieses Projekt ist als Open Source Software unter der **GNU General Public License v3.0 (GNU GPLv3)** veröffentlicht. 
Siehe [`LICENSE.md`](LICENSE.md:1) for Details.

**Haftungsausschluss (As-Is):**
Die Software wird "wie besehen" (AS IS) ohne ausdrückliche oder stillschweigende Gewährleistung bereitgestellt.
