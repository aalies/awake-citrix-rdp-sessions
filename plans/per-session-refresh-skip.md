# Plan: Per-Session Refresh Skipping for Parallel Citrix / Remote Sessions

## Objective
Modify `CaffeineCitrix64` (`Main.cs`) so that when multiple Citrix or remote desktop sessions are running in parallel, activity suppression applies **only** to the specific active session currently in the foreground. Background sessions will continue to receive keep-awake pulses.

## Architectural Changes in [`Main.cs`](Main.cs:1)
1. **Per-Process Active Foreground Check:**
   - Instead of a global `IsUserActivelyWorkingInCitrix()` check at the start of `OnTick()` that skips *all* refreshes, retrieve user idle time and foreground window PID once per tick.
   - Inside the `foreach (var proc in citrixProcs)` loop, check if `!forceSend`, the user is active (`idleMs < ActiveInCitrixThresholdMs`), and `fgPid == (uint)proc.Id`.
   - If true for a specific process, log that the refresh for this specific session is skipped due to active user input in the foreground, and `continue` to the next process.
   - If false (session is in the background, or user is idle), proceed with sending the non-intrusive pulse to that session.

2. **Logging Enhancements:**
   - Log per session when a refresh is skipped vs when a pulse is sent.

3. **Documentation Updates:**
   - Update [`README.md`](README.md:1) and [`AGENT.md`](AGENT.md:1) to describe per-session refresh skipping behavior.

## Implementation Steps & Todo List
1. Create and refine the implementation plan.
2. Edit [`Main.cs`](Main.cs:1) to implement per-process foreground/activity filtering.
3. Verify compilation and publish with `dotnet publish -c Release -r win-x64 --self-contained false`.
4. Update [`README.md`](README.md:1) and [`AGENT.md`](AGENT.md:1).

## Complexity & Model Recommendation
- **Complexity:** Low to Medium (targeted logic refinement in [`Main.cs`](Main.cs:1)).
- **Recommended Model:** `💻 Code` mode using `gemini-pro` or equivalent high-capability coding model to ensure robust P/Invoke and WinForms loop logic without regressions.
