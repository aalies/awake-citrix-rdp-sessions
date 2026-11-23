# AGENT.md – Working Instructions & Architectural Context

This document outlines the project goal, architecture, design decisions, and guardrails for AI agents and developers modifying **CaffeineCitrix64**.

## 1\. Project Goal

Provide a lightweight, reliable 64-bit keep-awake utility (`win-x64`, `.NET 8`, Windows Forms) for Citrix Workspace sessions that:

- Runs quietly in the background via the Windows system tray,  
- Never disrupts active typing or focus inside Citrix (such as remote MS Teams chat inputs),  
- Requires no administrative registry modifications or VDA-side components, and  
- Transparently logs target window discovery and message dispatch status.

## 2\. Current Architecture ([`Main.cs`](Main.cs:1))

- **Entry Point & UI Thread:** `Program.Main()` uses a global named `Mutex` (`Global\CaffeineCitrix64_SingleInstanceMutex`) to guarantee single-instance execution, then launches a Windows Forms `ApplicationContext` (`TrayAppContext`) managing the `NotifyIcon`, `ContextMenuStrip`, and `System.Windows.Forms.Timer` (default interval: `30_000` ms).  
- **Active Usage Protection (Per-Session Foreground Check):**  
  - Uses `GetLastInputInfo` (`user32.dll`) to calculate local user idle time (`Environment.TickCount - dwTime`).  
  - Uses `GetForegroundWindow` + `GetWindowThreadProcessId` to check whether a target process (`wfica`, `cdviewer`, `citrix`, or configured `TargetKeywords`) currently owns the foreground window.  
  - Excludes the current process ID (`Environment.ProcessId`) to prevent self-targeting when the debug log window is open.  
  - Skips sending pulses **per session**: if a specific target process is in the foreground and `idleMs < ActiveInCitrixThresholdMs` (`45_000` ms), the refresh for that active session is skipped, while parallel background sessions continue receiving keep-awake pulses.  
- **Target Discovery & Background Pulse (`OnTick`, `FindCitrixProcesses`, `GetChildWindows`, `SendNonIntrusivePulse`):**  
  - Enumerates running processes matching configurable `TargetKeywords` (default: `wfica`, `cdviewer`, `citrix`, `msrdc`, `remotedesktop`, `desktopviewer`) with a non-zero `MainWindowHandle`, explicitly excluding `Environment.ProcessId`.  
  - Discovers visible child windows via `EnumChildWindows` + `IsWindowVisible`.  
  - Dispatches non-intrusive messages via `PostMessage` using either `WM_MOUSEMOVE` at `(1, 1)` (default) or `VK_F15` (`WM_KEYDOWN` / `WM_KEYUP` without `WM_ACTIVATE`).  
- **Logging (`DebugLogForm` + `caffeine_debug.log`):**  
  - Appends timestamped entries thread-safely via `InvokeRequired` / `BeginInvoke`.  
  - Intercepts `FormClosing` (`CloseReason.UserClosing`) to hide the log window instead of terminating the tray application.

## 3\. Mandatory Guardrails

1. **Do NOT reintroduce `WM_ACTIVATE`:** Sending `WM_ACTIVATE` causes Citrix/remote applications (notably MS Teams) to reset focus and jump away from active text fields.  
2. **Preserve active-session suppression:** Never send periodic keep-awake pulses while Citrix is the foreground window and the user is actively providing input (except when explicitly triggered via `forceSend` / "Jetzt sofort testen").  
3. **Maintain 64-bit compatibility:** Keep `RuntimeIdentifier` set to `win-x64` and ensure all P/Invoke signatures use 64-bit-safe types (`IntPtr` for window and icon handles).  
4. **Enforce deterministic resource cleanup:** Always dispose `Process` instances (`using` / `Dispose()`) and release Win32 GDI icon handles (`DestroyIcon`).  
5. **Single entry point only:** Ensure only one `Main()` method exists in the project regardless of whether the file is named `Main.cs` or `Program.cs`.  
6. **Verify clean CLI publish:** Any change must compile cleanly with `dotnet publish -c Release -r win-x64 --self-contained false` without compiler errors or `NU1100` linker issues.  
7. **Always update documentation in markdown files ([`AGENT.md`](AGENT.md:1) and [`README.md`](README.md:1)):** Any new feature, configuration property, or architectural change must be documented in the markdown documentation files.

## 4\. Licensing & Open Source

- Licensed under the **GNU General Public License v3.0 (GNU GPLv3)** as an open-source project.
- Provided **"AS IS"** without warranty of any kind (see [`LICENSE.md`](LICENSE.md:1)).

## 5\. Potential Extension Points

- Configurable timer interval and `ActiveInCitrixThresholdMs` via tray menu or a lightweight JSON/INI config file.  
- Additional diagnostic details in the log output (e.g., exact `idleMs` value on skipped ticks).  
- Optional local sleep prevention via `SetThreadExecutionState` (kept strictly decoupled from remote Citrix window messaging).
