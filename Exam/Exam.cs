using Accord.Vision.Detection;
using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;

namespace Exam
{
    internal static class NoCapture
    {
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;  // legacy
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;  // Win 10 1903+

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        // Call this for any Form you create. It also re-applies if the handle is recreated.
        internal static void Apply(Form f)
        {
            async void Set(object s, EventArgs e)
            {
                try
                {
                    if (!f.IsHandleCreated) return;
                    // best-effort; ignore failure on older Windows
                    if (!SetWindowDisplayAffinity(f.Handle, WDA_EXCLUDEFROMCAPTURE))
                    {
                        // Optional fallback for very old builds:
                        // SetWindowDisplayAffinity(f.Handle, WDA_MONITOR);
                    }
                }
                catch { /* ignore */ }
            }

            if (f.IsHandleCreated) Set(null, EventArgs.Empty);
            f.HandleCreated += Set;
        }
    }

    // Add this inside namespace Exam, ABOVE the Exam class
    public class WebPopupForm : Form
    {
        public WebView2 View { get; private set; }
        public bool ShowButtons { get; set; } = true;
        public bool StartMaximized { get; set; } = true;

        private Button btnClose;
        private Button btnMaximize;
        private Panel overlayPanel;
        private bool isMaximized = false;
        private Rectangle previousBounds;
        private readonly int borderThickness = 2;
        private readonly Color borderColor = Color.DimGray;

        public WebPopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            DoubleBuffered = true;

            // Enable shadow on borderless form
            ApplyDropShadow();

            // --- WebView setup ---
            View = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(View);

            // --- Overlay panel for top buttons ---
            overlayPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent,
                Visible = false
            };
            Controls.Add(overlayPanel);

            // --- Close button ---
            btnClose = new Button
            {
                Text = "✕",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new System.Drawing.Size(40, 30)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            // --- Maximize button ---
            btnMaximize = new Button
            {
                Text = "🗗",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new System.Drawing.Size(40, 30)
            };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Click += (s, e) => ToggleMaximize();

            // Add buttons to panel
            overlayPanel.Controls.Add(btnClose);
            overlayPanel.Controls.Add(btnMaximize);

            // Now attach resize handler (after buttons exist)
            overlayPanel.Resize += (s, e) => PositionButtons();

            NoCapture.Apply(this);

            View.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (View.CoreWebView2 != null)
                {
                    var set = View.CoreWebView2.Settings;
                    set.AreDefaultContextMenusEnabled = false;
                    set.AreDevToolsEnabled = false;
                    set.AreBrowserAcceleratorKeysEnabled = false;
                    set.IsZoomControlEnabled = false;

                    View.CoreWebView2.WindowCloseRequested += (s2, e2) => this.Close();
                }
            };

            Load += (s, e) =>
            {
                overlayPanel.Visible = ShowButtons;

                if (StartMaximized)
                    MaximizeToScreen();
                else
                    Invalidate();
            };
        }

        private void MaximizeToScreen()
        {
            previousBounds = new Rectangle(
                Screen.FromControl(this).WorkingArea.Width / 4,
                Screen.FromControl(this).WorkingArea.Height / 4,
                Screen.FromControl(this).WorkingArea.Width / 2,
                Screen.FromControl(this).WorkingArea.Height / 2
            );

            Bounds = Screen.FromControl(this).WorkingArea;
            isMaximized = true;
            btnMaximize.Text = "🗗";
            PositionButtons();
            Invalidate();
        }

        private void PositionButtons()
        {
            if (btnClose == null || btnMaximize == null)
                return; // prevent early call before controls are ready

            int margin = 5;
            btnClose.Location = new System.Drawing.Point(ClientSize.Width - btnClose.Width - margin, margin);
            btnMaximize.Location = new System.Drawing.Point(ClientSize.Width - btnClose.Width - btnMaximize.Width - margin * 2, margin);
        }

        private void ToggleMaximize()
        {
            if (!isMaximized)
            {
                previousBounds = Bounds;
                Bounds = Screen.FromControl(this).WorkingArea;
                isMaximized = true;
                btnMaximize.Text = "🗗";
            }
            else
            {
                Bounds = previousBounds;
                isMaximized = false;
                btnMaximize.Text = "⬜";
            }

            PositionButtons();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionButtons();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Always draw border around the window
            using (Pen p = new Pen(borderColor, borderThickness))
            {
                Rectangle rect = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
                e.Graphics.DrawRectangle(p, rect);
            }
        }

        // ---- Enable native shadow on borderless form ----
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(ref int enabled);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private void ApplyDropShadow()
        {
            try
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                if (enabled == 1)
                {
                    var v = 2;
                    DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
                    var margins = new MARGINS()
                    {
                        cxLeftWidth = 1,
                        cxRightWidth = 1,
                        cyTopHeight = 1,
                        cyBottomHeight = 1
                    };
                    DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                }
            }
            catch
            {
                // fallback on old CS_DROPSHADOW style
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
    }


    public partial class Exam : Form
    {

        private string _userId = null;
        private bool _lms = false;
        private Timer updateTimer;

        // --- LMS polling timer (cookie-based, passive; no navigation) ---
        private Timer _lmsTimer = new Timer();
        //private readonly string _lmsProbeOrigin = "https://ntpclimite-stage.lms.hr.cloud.sap/";
        private readonly string _lmsProbeOrigin = ConfigurationManager.AppSettings["LmsProbeOrigin"];

        private bool _isOnLmsPage = false;

        /* --------------------- Keyboard Locking Logic --------------------- */
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }

        private IntPtr ptrMouseHook;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseClickProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelMouseClickProc callback, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);

        private IntPtr ptrHook;
        private LowLevelKeyboardProc objKeyboardProcess;
        private LowLevelMouseClickProc objMouseProcess;

        private IntPtr captureKey(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                Keys key = objKeyInfo.key;

                bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
                bool alt = (ModifierKeys & Keys.Alt) == Keys.Alt;
                bool shift = (ModifierKeys & Keys.Shift) == Keys.Shift;

                // ===== Block Windows Keys =====
                if (key == Keys.RWin || key == Keys.LWin)
                    return (IntPtr)1;

                // ===== Block Alt+Tab / Ctrl+Alt+Tab / Ctrl+Tab =====
                if (key == Keys.Tab && (alt || (ctrl && alt)))
                    return (IntPtr)1;

                // ===== Block Alt+Esc / Ctrl+Esc =====
                if ((key == Keys.Escape && (ctrl || alt)))
                    return (IntPtr)1;

                // ===== Block Ctrl+C / Ctrl+X / Shift+Insert =====
                if ((ctrl && (key == Keys.C || key == Keys.X)) || (shift && key == Keys.Insert))
                    return (IntPtr)1;

                // ===== Block Function Keys (F1–F12) =====
                if (key >= Keys.F1 && key <= Keys.F12)
                    return (IntPtr)1;

                // ===== Block PrintScreen =====
                if (key == Keys.PrintScreen)
                    return (IntPtr)1;

                // ===== Block Multimedia Keys (Volume, Mute, Play, Stop, Next, etc.) =====
                if (key == Keys.VolumeDown ||
                    key == Keys.VolumeUp ||
                    key == Keys.VolumeMute ||
                    key == Keys.MediaNextTrack ||
                    key == Keys.MediaPreviousTrack ||
                    key == Keys.MediaStop ||
                    key == Keys.MediaPlayPause)
                    return (IntPtr)1;

                // ===== Block Calculator and Browser Keys =====
                if (key == Keys.LaunchApplication1 ||  // often Calculator
                    key == Keys.LaunchApplication2 ||  // could be email, etc.
                    key == Keys.BrowserHome ||
                    key == Keys.BrowserBack ||
                    key == Keys.BrowserForward ||
                    key == Keys.BrowserRefresh ||
                    key == Keys.BrowserSearch)
                    return (IntPtr)1;

                // ===== Optional: Block Alt+F4 (Prevent app exit) =====
                if (alt && key == Keys.F4)
                    return (IntPtr)1;
            }

            return CallNextHookEx(ptrHook, nCode, wParam, lParam);
        }

        private IntPtr captureMouse(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                const int WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205, WM_RBUTTONDBLCLK = 0x206;
                if ((int)wp == WM_RBUTTONDOWN || (int)wp == WM_RBUTTONUP || (int)wp == WM_RBUTTONDBLCLK)
                    return (IntPtr)1;
            }
            return CallNextHookEx(ptrMouseHook, nCode, wp, lp);
        }

        private bool HasAltModifier(int flags) { return (flags & 0x20) == 0x20; }

        /* --------------------- Secondary Monitor Blocking --------------------- */
        private List<Form> secondaryOverlays = new List<Form>();

        private void BlockSecondaryMonitors()
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (!screen.Primary)
                {
                    var overlay = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.Manual,
                        BackColor = Color.Black,
                        WindowState = FormWindowState.Normal,
                        TopMost = true,
                        Bounds = screen.Bounds,
                        ShowInTaskbar = false
                    };
                    overlay.MouseDown += delegate { };
                    overlay.KeyDown += delegate (object s, KeyEventArgs e) { e.SuppressKeyPress = true; };
                    overlay.Show();
                    overlay.BringToFront();
                    secondaryOverlays.Add(overlay);
                }
            }
        }

        private void UnblockSecondaryMonitors()
        {
            foreach (var overlay in secondaryOverlays)
            {
                overlay.Close();
                overlay.Dispose();
            }
            secondaryOverlays.Clear();
        }

        /* --------------------- System Volume Mute --------------------- */
        private MMDeviceEnumerator deviceEnumerator;
        private MMDevice defaultDevice;
        private bool wasInitiallyMuted = false;

        private void MuteSystemVolume()
        {
            try
            {
                if (deviceEnumerator == null) deviceEnumerator = new MMDeviceEnumerator();
                defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                wasInitiallyMuted = defaultDevice.AudioEndpointVolume.Mute;

                if (wasInitiallyMuted)
                {
                    defaultDevice.AudioEndpointVolume.Mute = false;
                    defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 1.0f;
                }

                var sessionManager = defaultDevice.AudioSessionManager;
                var sessions = sessionManager.Sessions;
                int currentProcessId = Process.GetCurrentProcess().Id;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    int sessionProcessId = 0;
                    try
                    {
                        // AudioSessionControl has GetProcessID? Different versions vary.
                        // Try this safe approach:
                        try { sessionProcessId = (int)session.GetProcessID; }
                        catch { /* fallback or skip */ }
                    }
                    catch { }

                    if (sessionProcessId != currentProcessId)
                    {
                        session.SimpleAudioVolume.Mute = true;
                    }
                    else
                    {
                        session.SimpleAudioVolume.Mute = false;
                        session.SimpleAudioVolume.Volume = 1.0f;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("MuteSystemVolume failed: " + ex.Message); }
        }

        //private void MuteMicrophones()
        //{
        //    try
        //    {
        //        var enumerator = new MMDeviceEnumerator();

        //        // Mute playback devices (incoming audio)
        //        var playbackDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        //        foreach (var device in playbackDevices)
        //            device.AudioEndpointVolume.Mute = true;

        //        // Mute capture devices (outgoing audio)
        //        var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        //        foreach (var device in captureDevices)
        //            device.AudioEndpointVolume.Mute = true;
        //    }
        //    catch { }
        //}


        public static void EncryptConfig()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                ConfigurationSection section = config.GetSection("appSettings");

                if (section != null && !section.SectionInformation.IsProtected)
                {
                    section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    section.SectionInformation.ForceSave = true;
                    config.Save(ConfigurationSaveMode.Full);
                    //MessageBox.Show("✅ Config Encrypted Successfully!");
                }
                else
                {
                    MessageBox.Show("ℹ️ Config already encrypted or section not found.");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("❌ Encryption failed: " + ex.Message);
            }
        }
        private void RestoreAudio()
        {
            try
            {
                if (deviceEnumerator == null)
                    deviceEnumerator = new MMDeviceEnumerator();

                if (defaultDevice == null)
                    defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                var sessionManager = defaultDevice.AudioSessionManager;
                var sessions = sessionManager.Sessions;
                int currentProcessId = Process.GetCurrentProcess().Id;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    int sessionProcessId = 0;

                    try
                    {
                        sessionProcessId = (int)session.GetProcessID;
                    }
                    catch { }

                    // Restore mute state for all sessions
                    session.SimpleAudioVolume.Mute = false;
                }

                // Restore microphones
                var captureDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var dev in captureDevices)
                {
                    dev.AudioEndpointVolume.Mute = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("RestoreAudio failed: " + ex.Message);
            }
        }

        private void RestoreSystemVolume()
        {
            try
            {
                if (defaultDevice != null)
                    defaultDevice.AudioEndpointVolume.Mute = wasInitiallyMuted;
            }
            catch (Exception ex)
            {
                MessageBox.Show("RestoreSystemVolume failed: " + ex.Message);
            }
        }

        /* --------------------- Main Exam Form Logic --------------------- */
        public Exam()
        {
            InitializeComponent();
            NoCapture.Apply(this);
        }
        private Timer focusTimer;

        private void InitFaceDetector()
        {
            if (_faceNet != null) return;
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
            string protoPath = Path.Combine(basePath, "deploy.prototxt");
            string modelPath = Path.Combine(basePath, "res10_300x300_ssd_iter_140000_fp16.caffemodel");

            if (!File.Exists(protoPath) || !File.Exists(modelPath))
            {
                Debug.WriteLine("Face model files missing: " + protoPath + " / " + modelPath);
                return;
            }

            _faceNet = CvDnn.ReadNetFromCaffe(protoPath, modelPath);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private bool _videoStarted = false;
        private async void Exam_Load(object sender, EventArgs e)
        {
            try
            {
                if (SystemInformation.TerminalServerSession)
                {
                    MessageBox.Show("Remote Desktop connection detected! Exam cannot start.", "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                    return;
                }

                string[] remoteApps = { "anydesk", "teamviewer", "tv_x64", "ammyy", "parsecd", "splashtop", "chrome-remote-desktop" };
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string pn = p.ProcessName.ToLower();
                        for (int i = 0; i < remoteApps.Length; i++)
                        {
                            if (pn.Contains(remoteApps[i])) { p.Kill(); break; }
                        }
                    }
                    catch { }
                }

                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Terminal Server", true))
                    {
                        if (key != null) key.SetValue("fDenyTSConnections", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }

                    var sc = new ServiceController("TermService");
                    if (sc.Status != ServiceControllerStatus.Stopped)
                        sc.Stop();
                }
                catch { }
            }
            catch { }

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;

            string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SappleSystems", "SecureBrowser", "WebView2Data");

            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // After creating the environment, initialize your WebView2 control
            await webView21.EnsureCoreWebView2Async(webView2Environment);

            webView21.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            // gate

            // Optional: lock down some main WebView behaviors
            var s = webView21.CoreWebView2.Settings;
            s.AreDefaultContextMenusEnabled = false;       // you also block right-click globally
            s.AreDevToolsEnabled = false;
            s.AreBrowserAcceleratorKeysEnabled = false;    // blocks Ctrl+L, Ctrl+N, etc.
            s.IsZoomControlEnabled = false;

            // IMPORTANT: capture window.open/_blank and show in our own owned TopMost form
            webView21.CoreWebView2.NewWindowRequested += async (snd, args) =>
            {
                var deferral = args.GetDeferral();  // allows async work
                try
                {
                    // Determine popup type based on URL
                    bool isSmallPopup = !string.IsNullOrEmpty(args.Uri) && args.Uri.ToLower().Contains("qci.sapple.co.in");

                    // Create popup form
                    var popup = new WebPopupForm
                    {
                        ShowButtons = !isSmallPopup,
                        StartMaximized = !isSmallPopup
                    };

                    this.AddOwnedForm(popup);

                    // --- Size and position ---
                    if (isSmallPopup)
                    {
                        int w = 1000, h = 800;
                        var ownerBounds = this.Bounds;
                        int x = ownerBounds.Left + (ownerBounds.Width - w) / 2;
                        int y = ownerBounds.Top + (ownerBounds.Height - h) / 2;
                        popup.Bounds = new Rectangle(x, y, w, h);
                    }

                    // --- Show popup ---
                    popup.Show();
                    popup.BringToFront();
                    SetForegroundWindow(popup.Handle);

                    // --- Initialize WebView2 ---
                    await popup.View.EnsureCoreWebView2Async(webView2Environment);

                    if (popup.View.CoreWebView2 != null)
                    {
                        var ps = popup.View.CoreWebView2.Settings;
                        ps.AreDefaultContextMenusEnabled = false;
                        ps.AreDevToolsEnabled = false;
                        ps.AreBrowserAcceleratorKeysEnabled = false;
                        ps.IsZoomControlEnabled = false;
                    }

                    args.NewWindow = popup.View.CoreWebView2;

                    // --- Navigate if URL provided ---
                    if (!string.IsNullOrEmpty(args.Uri))
                    {
                        popup.View.CoreWebView2.Navigate(args.Uri);
                    }

                    // --- Keep popup in front ---
                    popup.Activate();
                    popup.BringToFront();
                    SetForegroundWindow(popup.Handle);
                }
                catch
                {
                    // If anything goes wrong, swallow the popup request gracefully
                    args.Handled = true;
                }
                finally
                {
                    deferral?.Complete();
                }
            };

            // end gate f

            string sapUrl = ConfigurationManager.AppSettings["SapWebUrl"];
            webView21.Source = new Uri(sapUrl);

            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            objMouseProcess = new LowLevelMouseClickProc(captureMouse);
            ptrMouseHook = SetWindowsHookEx(14, objMouseProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            BlockSecondaryMonitors();
            MuteSystemVolume();
            //MuteMicrophones();
            StartTopMostEnforcement();

            //updateTimer = new Timer();
            //updateTimer.Interval = 60000; // 1 minute
            //updateTimer.Tick += UpdateLabelHash;
            //updateTimer.Start();
            //UpdateLabelHash(null, null);
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                await Task.Delay(500);

                // ----- Track whether current page is an LMS URL -----
                string currentUrl = null;
                try { currentUrl = webView21.CoreWebView2 != null ? webView21.CoreWebView2.Source : null; } catch { }
                _isOnLmsPage = (currentUrl ?? "").IndexOf(".lms.hr.cloud.sap", StringComparison.OrdinalIgnoreCase) >= 0;

                // ----- Try to discover SF user id from localStorage (as in your code) -----
                string localDecoded = null;
                int tries = 0;
                while (tries < 10)
                {
                    string localRaw = await webView21.ExecuteScriptAsync(@"
(() => {
    let obj = {};
    for (let i=0;i<localStorage.length;i++) {
        let k = localStorage.key(i);
        obj[k] = localStorage.getItem(k);
    }
    return JSON.stringify(obj);
})();
");
                    localDecoded = System.Text.Json.JsonSerializer.Deserialize<string>(localRaw);
                    var outerObj = JsonConvert.DeserializeObject<JObject>(localDecoded);

                    Newtonsoft.Json.Linq.JProperty userProp = null;
                    foreach (var p in outerObj.Properties())
                    {
                        if (p.Name.EndsWith("_lastPageVisited"))
                        {
                            userProp = p;
                            break;
                        }
                    }
                    if (userProp != null)
                    {
                        int cut = userProp.Name.IndexOf("_lastPageVisited", StringComparison.Ordinal);
                        if (cut >= 0) _userId = userProp.Name.Substring(0, cut);
                        break;
                    }

                    tries++;
                    await Task.Delay(500);
                }

                // ----- Start LMS cookie polling after SF login is confirmed -----
                if (!string.IsNullOrEmpty(_userId) && !_lmsTimer.Enabled)
                {
                    _lmsTimer.Interval = 10000; // 10s
                    _lmsTimer.Tick += async delegate
                    {
                        bool ok = await IsLmsLoggedInByCookiesAsync();
                        if (_isOnLmsPage && ok)
                        {
                            // label2.Text = "✅ LMS Active";
                            _lms = true;
                            if (!_videoStarted)
                            {
                                InitFaceDetector();
                                StartCamera();
                                StartFaceUiTimer();
                                ScreenShotTimmer();
                            }
                        }
                        else if (ok)
                        {
                            //  label2.Text = "🟦 LMS session active (not on Learning)";
                            _lms = false;
                            if (_videoStarted)
                            {
                                StopCamera();
                            }
                        }
                        else
                        {
                            // label2.Text = "❌ LMS Logged Out";
                            _lms = false;
                            if (_videoStarted)
                            {
                                StopCamera();
                            }
                        }
                        label2.Left = 10;
                        label2.Top = this.ClientSize.Height - label2.Height - 10;
                        label2.BringToFront();
                    };
                    _lmsTimer.Start();

                    // First immediate check
                    bool first = await IsLmsLoggedInByCookiesAsync();
                    if (_isOnLmsPage && first)
                    {
                        //label2.Text = "✅ LMS Active";
                        _lms = true;
                        if (!_videoStarted)
                        {
                            InitFaceDetector();
                            StartCamera();
                            StartFaceUiTimer();
                            ScreenShotTimmer();
                        }
                    }
                    else if (first)
                    {
                        // label2.Text = "🟦 LMS session active (not on Learning)";
                        _lms = false;
                        if (_videoStarted)
                        {
                            StopCamera();
                        }
                    }
                    else
                    {
                        //  label2.Text = "❌ LMS Logged Out";
                        _lms = false;
                        if (_videoStarted)
                        {
                            StopCamera();
                        }
                    }
                    label2.Left = 10;
                    label2.Top = this.ClientSize.Height - label2.Height - 10;
                    label2.BringToFront();
                }
                else
                {
                    // Even if polling already started, refresh status immediately on any navigation
                    if (_lmsTimer.Enabled)
                    {
                        bool okNow = await IsLmsLoggedInByCookiesAsync();
                        if (_isOnLmsPage && okNow)
                        {
                            //label2.Text = "✅ LMS Active";
                            _lms = true;
                            if (!_videoStarted)
                            {
                                InitFaceDetector();
                                StartCamera();
                                StartFaceUiTimer();
                                ScreenShotTimmer();
                            }
                        }
                        else if (okNow)
                        {
                            //label2.Text = "🟦 LMS session active (not on Learning)";
                            _lms = false;
                            if (_videoStarted)
                            {
                                StopCamera();
                            }
                        }
                        else
                        {
                            //label2.Text = "❌ LMS Logged Out";
                            _lms = false;
                        }
                        label2.Left = 10;
                        label2.Top = this.ClientSize.Height - label2.Height - 10;
                        label2.BringToFront();
                        if (_videoStarted)
                        {
                            StopCamera();
                        }
                    }
                }

                // Keep hash updated after navigation / login state changes
                // UpdateLabelHash(null, null);
            }
            catch (Exception ex)
            {
                label2.Text = "Error: " + ex.Message;
            }
        }

        // ---- Passive LMS login detection: cookie presence on LMS origin (no navigation) ----
        private async Task<bool> IsLmsLoggedInByCookiesAsync()
        {
            try
            {
                if (webView21.CoreWebView2 == null) return false;

                var cookies = await webView21.CoreWebView2.CookieManager
                    .GetCookiesAsync(_lmsProbeOrigin);

                if (cookies == null || cookies.Count == 0)
                    return false;

                // Heuristics: consider logged-in if typical session/auth cookies exist for LMS tenant
                for (int i = 0; i < cookies.Count; i++)
                {
                    var c = cookies[i];
                    string name = c.Name ?? "";
                    string domain = c.Domain ?? "";

                    if (domain.IndexOf(".lms.hr.cloud.sap", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (string.Equals(name, "JSESSIONID", StringComparison.OrdinalIgnoreCase)) return true;
                    if (name.IndexOf("SESSION", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (name.IndexOf("AUTH", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (name.StartsWith("saplb_", StringComparison.OrdinalIgnoreCase)) return true;     // SAP LB cookie
                    if (name.StartsWith("AWSALB", StringComparison.OrdinalIgnoreCase)) return true;     // AWS ALB
                    if (name.StartsWith("AWSALBTG", StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private HaarObjectDetector _faceDetector;
        private volatile bool _faceDetected = false;
        private Net _faceNet;
        private readonly object _videoLock = new object();
        // Camera / face detection
        private DateTime _lastFrameProcessed = DateTime.MinValue;
        private readonly object _frameLock = new object();

        // Timers
        private Timer _faceUiTimer;
        private Timer _ScreenShotTimer;

        private void StartCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
                return;

            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (_videoDevices.Count == 0)
            {
                label1.Text = "No webcam detected";
                label1.Left = (this.ClientSize.Width - label1.Width) / 2;
                return;
            }

            _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
            _videoStarted = true;
        }

        private void StopCamera()
        {
            try
            {
                if (_videoSource != null)
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;

                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }

                    _videoSource = null;
                    _videoStarted = false;
                }
            }
            catch { }
        }

        private void StartFaceUiTimer()
        {
            _faceUiTimer = new Timer();
            _faceUiTimer.Interval = 1000; // 1 second
            _faceUiTimer.Tick += FaceUiTimer_Tick;
            _faceUiTimer.Start();
        }

        private void ScreenShotTimmer()
        {
            _ScreenShotTimer = new Timer();
            _ScreenShotTimer.Interval = 10000; // 10 second
            _ScreenShotTimer.Tick += CaptureScreen_Tick;
            _ScreenShotTimer.Start();
        }

        private void FaceUiTimer_Tick(object sender, EventArgs e)
        {
            SafeSetClipboardTextOnce(" ");
            if (string.IsNullOrEmpty(_userId) || !_lms)
            {
                label1.Text = "Please Login";
                label1.Left = (this.ClientSize.Width - label1.Width) / 2;
                return;
            }

            bool detected = false;
            lock (_frameLock)
            {
                detected = _faceDetected;
            }
            if (detected != true)
            {
                label1.Text = "No Person Detected.";
                label1.Left = (this.ClientSize.Width - label1.Width) / 2;
                SafeSetClipboardTextOnce(" ");
                return;
            }

            string minuteCode = MinuteCode.GetCurrentDeviceMinuteCode();
            label1.Text = "Code: " + minuteCode;
            SafeSetClipboardTextOnce(minuteCode);
            label1.Left = (this.ClientSize.Width - label1.Width) / 2;
        }
        private string _lastClipboardValue = null;

        private void SafeSetClipboardTextOnce(string text)
        {
            if (text == _lastClipboardValue)
                return;

            try
            {
                Clipboard.SetText(text);
                _lastClipboardValue = text;
            }
            catch { }
        }
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // throttle to 1 frame per second
            if ((DateTime.UtcNow - _lastFrameProcessed).TotalSeconds < 1)
                return;

            _lastFrameProcessed = DateTime.UtcNow;

            try
            {
                using (Bitmap frame = (Bitmap)eventArgs.Frame.Clone())
                {
                    bool detected = DetectFace(frame);

                    lock (_frameLock)
                    {
                        _faceDetected = detected;
                    }
                }
            }
            catch
            {
                _faceDetected = false;
            }
        }

        private bool DetectFace(Bitmap bitmap)
        {
            using (Mat img = BitmapConverter.ToMat(bitmap))
            using (Mat blob = CvDnn.BlobFromImage(
                img,
                1.0,
                new OpenCvSharp.Size(300, 300),
                new Scalar(104, 117, 123),
                false,
                false))
            {
                _faceNet.SetInput(blob);

                using (Mat output = _faceNet.Forward())
                {
                    for (int i = 0; i < output.Size(2); i++)
                    {
                        float confidence = output.At<float>(0, 0, i, 2);
                        if (confidence > 0.8f)
                            return true;
                    }
                }
            }
            return false;
        }

        private static string GetMacAddress()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                    return nic.GetPhysicalAddress().ToString();
            }
            return "UnknownMAC";
        }

        private static string GetLocalIPAddress()
        {
            string hostName = Dns.GetHostName();
            var ip = Dns.GetHostAddresses(hostName)
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip != null ? ip.ToString() : "0.0.0.0";
        }

        private static string GetSha256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private void Exam_Resize(object sender, EventArgs e)
        {
            label1.Left = (this.ClientSize.Width - label1.Width) / 2;
            button1.Left = this.ClientSize.Width - button1.Width - 10;

            int maxWidth = this.ClientSize.Width / 2;
            System.Drawing.Size textSize = TextRenderer.MeasureText(
                label2.Text,
                label2.Font,
                new System.Drawing.Size(maxWidth, int.MaxValue),
                TextFormatFlags.WordBreak
            );
            label2.Size = textSize;
            label2.Left = this.ClientSize.Width - label2.Width - 10;
            label2.Top = this.ClientSize.Height - label2.Height - 10;

            webView21.Top = label1.Bottom + 10;
            webView21.Height = this.ClientSize.Height - label1.Height - 20;
            webView21.Width = this.ClientSize.Width;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to exit the program?", "Exit", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    // Close any other open forms or popups first
                    foreach (Form openForm in Application.OpenForms.Cast<Form>().ToList())
                    {
                        if (openForm != this) // don't close the current form yet
                        {
                            openForm.Close();
                        }
                    }

                    // Now close the main form gracefully
                    UnblockSecondaryMonitors();
                    RestoreSystemVolume();
                    RestoreAudio();

                    // Close this form explicitly before exiting
                    this.Close();

                    // Finally, exit the application
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while closing application: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnblockSecondaryMonitors();
            RestoreSystemVolume();

            // Close any owned popups we opened
            foreach (var owned in this.OwnedForms)
            {
                try { owned.Close(); owned.Dispose(); } catch { }
            }
            // stop video
            try
            {
                if (_videoSource != null)
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }
                    _videoSource = null;
                }
            }
            catch { }

            try { if (_faceNet != null) { _faceNet.Dispose(); _faceNet = null; } } catch { }

            try { if (ptrHook != IntPtr.Zero) { UnhookWindowsHookEx(ptrHook); ptrHook = IntPtr.Zero; } } catch { }
            try { if (ptrMouseHook != IntPtr.Zero) { UnhookWindowsHookEx(ptrMouseHook); ptrMouseHook = IntPtr.Zero; } } catch { }

            try { updateTimer?.Stop(); updateTimer?.Dispose(); } catch { }
            try { _lmsTimer?.Stop(); _lmsTimer?.Dispose(); } catch { }

            base.OnFormClosing(e);
        }

        private Timer enforceTopMostTimer = new Timer();

        private void CaptureScreen_Tick(object sender, EventArgs e)
        {
            string mac = GetMacAddress();
            string ip = GetLocalIPAddress();
            CaptureWebViewScreenshotAsync(mac, ip);
        }
        private void StartTopMostEnforcement()
        {
            enforceTopMostTimer.Interval = 500;
            enforceTopMostTimer.Tick += (s, e) =>
            {
                foreach (var overlay in secondaryOverlays)
                {
                    if (!overlay.TopMost)
                    {
                        overlay.TopMost = true;
                        overlay.BringToFront();
                    }
                }
            };
            enforceTopMostTimer.Start();
        }

        private async Task CaptureWebViewScreenshotAsync(string mac, string ip)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Screens");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + mac + "_" + ip + ".png";
                string fullPath = Path.Combine(folderPath, fileName);

                // Determine which WebView2 to capture — main or popup
                CoreWebView2 activeWebView = null;

                // Check if any owned popup is open and visible
                var popup = this.OwnedForms
                    .OfType<WebPopupForm>()
                    .FirstOrDefault(f => f.Visible && f.View?.CoreWebView2 != null);

                if (popup != null)
                {
                    activeWebView = popup.View.CoreWebView2;
                }
                else if (webView21?.CoreWebView2 != null)
                {
                    activeWebView = webView21.CoreWebView2;
                }

                if (activeWebView == null)
                {
                    System.Diagnostics.Debug.WriteLine("No active WebView2 found to capture.");
                    return;
                }

                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await activeWebView.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png,
                        stream
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WebView2 screenshot failed: " + ex.Message);
            }
        }

    }
}
