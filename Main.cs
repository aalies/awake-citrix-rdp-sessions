using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CaffeineCitrix64
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayAppContext());
        }
    }

    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Timer _timer;
        private readonly ToolStripMenuItem _toggleItem;
        private readonly ToolStripMenuItem _modeItem;
        private readonly DebugLogForm _logForm;
        private readonly string _logFilePath;

        private bool _isActive = true;
        private bool _useMouseMoveOnly = false; // F15 default
        private List<string> _targetKeywords = new List<string> { "wfica", "cdviewer", "citrix", "msrdc", "remotedesktop", "desktopviewer" };

        private readonly string _configFilePath;

        private const uint ActiveInCitrixThresholdMs = 45_000;

        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int VK_F15 = 0x7E;

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public TrayAppContext()
        {
            _logFilePath = Path.Combine(AppContext.BaseDirectory, "caffeine_debug.log");
            _configFilePath = Path.Combine(AppContext.BaseDirectory, "caffeine.properties");
            _logForm = new DebugLogForm();

            LoadSettings();

            _toggleItem = new ToolStripMenuItem("Pausieren", null, OnToggleActive);
            _modeItem = new ToolStripMenuItem("", null, OnToggleMode);
            UpdateModeMenuText();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Debug-Log anzeigen", null, OnShowLog);
            menu.Items.Add("Jetzt sofort testen", null, OnTestNow);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_toggleItem);
            menu.Items.Add(_modeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Beenden", null, OnExitClick);

            _trayIcon = new NotifyIcon
            {
                Icon = CreateCircleIcon(Color.LimeGreen),
                Text = "Citrix Caffeine 64-Bit: AKTIV",
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.MouseClick += OnTrayMouseClick;

            _timer = new Timer
            {
                Interval = 30_000
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Log("Gestartet: Überspringt Refresh, wenn Citrix aktiv im Vordergrund ist und gearbeitet wird.");
        }

        private void OnTrayMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleActive();
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            OnTick(forceSend: false);
        }

        private void OnShowLog(object? sender, EventArgs e)
        {
            ShowLogWindow();
        }

        private void OnTestNow(object? sender, EventArgs e)
        {
            OnTick(forceSend: true);
        }

        private void OnToggleActive(object? sender, EventArgs e)
        {
            ToggleActive();
        }

        private void OnToggleMode(object? sender, EventArgs e)
        {
            _useMouseMoveOnly = !_useMouseMoveOnly;
            UpdateModeMenuText();
            SaveSettings();
            Log($"Impuls-Modus gewechselt auf: {(_useMouseMoveOnly ? "WM_MOUSEMOVE" : "VK_F15")}");
        }

        private void UpdateModeMenuText()
        {
            _modeItem.Text = _useMouseMoveOnly
                ? "Modus: Nur Maus-Impuls (minimale Mausbewegung)"
                : "Modus: F15-Taste (ohne WM_ACTIVATE)";
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    foreach (var line in File.ReadAllLines(_configFilePath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        var parts = trimmed.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToLowerInvariant();
                            string val = parts[1].Trim();
                            if (key == "usemousemoveonly" || key == "mousemove")
                            {
                                if (bool.TryParse(val, out bool result))
                                {
                                    _useMouseMoveOnly = result;
                                }
                            }
                            else if (key == "mode")
                            {
                                if (val.Equals("mousemove", StringComparison.OrdinalIgnoreCase) || val.Equals("mouse", StringComparison.OrdinalIgnoreCase))
                                {
                                    _useMouseMoveOnly = true;
                                }
                                else if (val.Equals("f15", StringComparison.OrdinalIgnoreCase))
                                {
                                    _useMouseMoveOnly = false;
                                }
                            }
                            else if (key == "targetkeywords" || key == "keywords")
                            {
                                var keywords = val.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                                if (keywords.Length > 0)
                                {
                                    _targetKeywords.Clear();
                                    foreach (var kw in keywords)
                                    {
                                        _targetKeywords.Add(kw.Trim().ToLowerInvariant());
                                    }
                                }
                            }
                        }
                    }
                    Log($"Konfiguration geladen aus '{_configFilePath}': Mode={(_useMouseMoveOnly ? "MouseMove" : "F15")}, TargetKeywords=[{string.Join(", ", _targetKeywords)}]");
                }
                else
                {
                    Log($"Keine Konfigurationsdatei gefunden unter '{_configFilePath}'. Verwende Standardwerte (TargetKeywords=[{string.Join(", ", _targetKeywords)}], Mode=F15).");
                }
            }
            catch (Exception ex)
            {
                Log($"Fehler beim Laden der Konfiguration: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# CaffeineCitrix64 Properties File");
                sb.AppendLine($"UseMouseMoveOnly={_useMouseMoveOnly}");
                sb.AppendLine($"Mode={(_useMouseMoveOnly ? "MouseMove" : "F15")}");
                sb.AppendLine($"TargetKeywords={string.Join(",", _targetKeywords)}");
                File.WriteAllText(_configFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log($"Fehler beim Speichern der Konfiguration: {ex.Message}");
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            ExitApp();
        }

        private static uint GetUserIdleTimeMs()
        {
            var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref lii)) return 0;
            return unchecked((uint)Environment.TickCount - lii.dwTime);
        }

        private void OnTick(bool forceSend)
        {
            if (!_isActive && !forceSend) return;

            var citrixProcs = FindCitrixProcesses();
            if (citrixProcs.Count == 0)
            {
                Log("Kein laufender Citrix-Prozess (wfica / cdviewer / citrix) mit Fenster gefunden.");
                return;
            }

            uint idleMs = GetUserIdleTimeMs();
            bool isUserActive = idleMs < ActiveInCitrixThresholdMs;
            IntPtr fg = isUserActive ? GetForegroundWindow() : IntPtr.Zero;
            uint fgPid = 0;
            if (fg != IntPtr.Zero)
            {
                GetWindowThreadProcessId(fg, out fgPid);
            }

            foreach (var proc in citrixProcs)
            {
                try
                {
                    if (!forceSend && isUserActive && fgPid != 0 && fgPid == (uint)proc.Id)
                    {
                        Log($"Ziel: '{proc.ProcessName}' (PID {proc.Id}) ist im Vordergrund & Benutzereingabe erkannt -> Refresh übersprungen.");
                        continue;
                    }

                    IntPtr mainHwnd = proc.MainWindowHandle;
                    string title = GetWindowTitle(mainHwnd);
                    string className = GetWindowClass(mainHwnd);

                    Log($"Ziel: '{proc.ProcessName}' (PID {proc.Id}) | HWND 0x{mainHwnd.ToInt64():X} | Klasse '{className}' | Titel '{title}'");

                    bool okMain = SendNonIntrusivePulse(mainHwnd);
                    Log($" -> Hauptfenster: {(okMain ? "GESENDET" : $"FEHLER ({Marshal.GetLastWin32Error()})")}");

                    var children = GetChildWindows(mainHwnd);
                    foreach (IntPtr child in children)
                    {
                        string cClass = GetWindowClass(child);
                        string cTitle = GetWindowTitle(child);
                        bool okChild = SendNonIntrusivePulse(child);
                        Log($" -> Kindfenster 0x{child.ToInt64():X} ['{cClass}' / '{cTitle}']: {(okChild ? "GESENDET" : $"FEHLER ({Marshal.GetLastWin32Error()})")}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Fehler bei PID {proc.Id}: {ex.Message}");
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }

        private bool SendNonIntrusivePulse(IntPtr hWnd)
        {
            if (_useMouseMoveOnly)
            {
                IntPtr lParam = (IntPtr)((1 << 16) | 1);
                return PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
            }
            else
            {
                bool down = PostMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_F15, (IntPtr)0x00010001);
                System.Threading.Thread.Sleep(20);
                bool up = PostMessage(hWnd, WM_KEYUP, (IntPtr)VK_F15, unchecked((IntPtr)0xC0010001));
                return down && up;
            }
        }

        private List<Process> FindCitrixProcesses()
        {
            var result = new List<Process>();
            int currentPid = Environment.ProcessId;
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.Id == currentPid)
                    {
                        proc.Dispose();
                        continue;
                    }

                    string name = proc.ProcessName.ToLowerInvariant();
                    bool matches = false;
                    foreach (var kw in _targetKeywords)
                    {
                        if (name.Contains(kw))
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (matches && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        result.Add(proc);
                    }
                    else
                    {
                        proc.Dispose();
                    }
                }
                catch
                {
                    proc.Dispose();
                }
            }
            return result;
        }

        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            var list = new List<IntPtr>();
            EnumChildWindows(parent, (hWnd, _) =>
            {
                if (IsWindowVisible(hWnd)) list.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return list;
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowClass(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private void ShowLogWindow()
        {
            if (!_logForm.Visible) _logForm.Show();
            _logForm.BringToFront();
        }

        private void ToggleActive()
        {
            _isActive = !_isActive;
            _toggleItem.Text = _isActive ? "Pausieren" : "Aktivieren";
            _trayIcon.Icon = CreateCircleIcon(_isActive ? Color.LimeGreen : Color.Gray);
            _trayIcon.Text = _isActive ? "Citrix Caffeine 64-Bit: AKTIV" : "Citrix Caffeine 64-Bit: PAUSIERT";
            Log(_isActive ? "Status: AKTIVIERT" : "Status: PAUSIERT");
        }

        private void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logForm.AppendLine(line);
            try { File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        private static Icon CreateCircleIcon(Color color)
        {
            using var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 2, 2, 12, 12);
            IntPtr hIcon = bmp.GetHicon();
            Icon cloned = (Icon)Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            return cloned;
        }

        private void ExitApp()
        {
            _timer.Stop();
            _timer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _logForm.Close();
            Application.Exit();
        }
    }

    internal sealed class DebugLogForm : Form
    {
        private readonly TextBox _textBox;

        public DebugLogForm()
        {
            Text = "Caffeine – Debug-Log";
            Size = new Size(780, 420);
            StartPosition = FormStartPosition.CenterScreen;

            _textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.LightGreen
            };

            var clearBtn = new Button
            {
                Text = "Log leeren",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            clearBtn.Click += OnClearClick;

            Controls.Add(_textBox);
            Controls.Add(clearBtn);

            FormClosing += OnFormClosing;
        }

        private void OnClearClick(object? sender, EventArgs e)
        {
            _textBox.Clear();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        public void AppendLine(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLine), line);
                return;
            }
            _textBox.AppendText(line + Environment.NewLine);
        }
    }
}