// Bu 7maid Booster - one-click Windows gaming optimizer (new interface, same tweak engine)
// C# 5 / .NET Framework 4 (csc.exe from Windows)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Bu7maidBooster
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // background jobs of the engine (OC steps, OC resume after a restart, the after-restart check) use the same exe with arguments
            if (args.Length > 0) { Bu7maidZero.Program.ZeroMain(args); return; }
            bool created;
            using (Mutex m = new Mutex(true, "Bu7maidBooster_single_instance", out created))
            {
                if (!created) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }

    static class T
    {
        public static readonly Color Bg0 = Color.FromArgb(8, 10, 18);
        public static readonly Color Bg1 = Color.FromArgb(17, 19, 44);
        public static readonly Color Card = Color.FromArgb(22, 25, 50);
        public static readonly Color Line = Color.FromArgb(48, 52, 94);
        public static readonly Color Txt = Color.FromArgb(236, 240, 255);
        public static readonly Color Muted = Color.FromArgb(134, 142, 178);
        public static readonly Color A1 = Color.FromArgb(112, 92, 255);
        public static readonly Color A2 = Color.FromArgb(24, 212, 255);
        public static readonly Color Good = Color.FromArgb(46, 214, 130);
        public static readonly Color Warn = Color.FromArgb(255, 196, 60);
        public static readonly Color Bad = Color.FromArgb(255, 84, 96);
    }

    class Hit
    {
        public string Id;
        public RectangleF R;
    }

    class MainForm : Form
    {
        const int W = 980, H = 640;
        const string Version = "v2.15";

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct MEMSTAT
        {
            public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys;
            public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual; public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MEMSTAT m);

        float sc = 1f;
        Stopwatch sw = Stopwatch.StartNew();
        System.Windows.Forms.Timer tick;
        double lastT = 0;
        Random rnd = new Random();

        // state
        int page = 0;                 // 0 boost, 1 monitor, 2 restore
        int mode = 0;                 // 0 full, 1 safe
        bool delStartup = false, measure = true;
        float tglAnim = 0f, tglAnim2 = 1f;
        Bu7maidZero.MainForm z;
        void DrawLive(Graphics g, double t)
        {
            CardBox(g, 104, 410, 410, 200);
            Txt(g, "STEPS   (don't close this window)", Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, 124, 418, 380, 18, StringAlignment.Near, StringAlignment.Center);
            int n = steps.Count, first = Math.Max(0, n - 7);
            for (int i = first; i < n; i++)
            {
                StepRec s = steps[i]; float y = 442 + (i - first) * 23;
                bool cur = i == n - 1;
                if (!cur)
                {
                    using (SolidBrush b = new SolidBrush(Alpha(T.Good, 50))) g.FillEllipse(b, 126, y + 4, 14, 14);
                    using (Pen p = new Pen(T.Good, 1.8f)) g.DrawLines(p, new PointF[] { new PointF(129, y + 11), new PointF(132, y + 14), new PointF(137, y + 8) });
                }
                else
                {
                    float r = 5f + 2f * (float)Math.Abs(Math.Sin(t * 4));
                    using (SolidBrush b = new SolidBrush(Alpha(T.A2, 70))) g.FillEllipse(b, 133 - r, y + 11 - r, r * 2, r * 2);
                    using (SolidBrush b = new SolidBrush(T.A2)) g.FillEllipse(b, 129, y + 7, 8, 8);
                }
                Txt(g, s.T, Fnt("Segoe UI", 9f, cur ? FontStyle.Bold : FontStyle.Regular), cur ? T.Txt : Alpha(T.Muted, 235), 150, y, 356, 22, StringAlignment.Near, StringAlignment.Center);
            }
            CardBox(g, 534, 410, 410, 200);
            Txt(g, "WHAT IT IS DOING NOW", Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, 554, 418, 380, 18, StringAlignment.Near, StringAlignment.Center);
            if (n == 0) return;
            StepRec cs = steps[n - 1];
            int show = Math.Min(cs.Do.Count, 1 + (int)((DateTime.Now - cs.Start).TotalSeconds / 0.5));
            for (int i = 0; i < show && i < 6; i++)
            {
                float y = 444 + i * 26;
                using (SolidBrush b = new SolidBrush(i % 2 == 0 ? T.A2 : T.A1)) g.FillEllipse(b, 556, y + 9, 7, 7);
                Txt(g, cs.Do[i], Fnt("Segoe UI", 9f, FontStyle.Regular), T.Txt, 572, y, 364, 24, StringAlignment.Near, StringAlignment.Center);
            }
        }

        DateTime restartAt = DateTime.MinValue;
        string ocSummary = "";
        Bu7maidZero.BenchRes resB, resA;
        void LoadRes() { try { resB = Bu7maidZero.BenchRes.Load("before"); resA = Bu7maidZero.BenchRes.Load("after"); } catch { } }
        class StepRec { public string T; public List<string> Do = new List<string>(); public DateTime Start; public double Secs; public bool Done; }
        readonly List<StepRec> steps = new List<StepRec>();
        List<StepRec> lastSteps = null; List<string> lastGames = null, lastErrs = null; double lastSecs = 0; int lastErrCount = 0; string lastName = "";
        DateTime runStart = DateTime.Now;
        void NewStep(string title)
        {
            DateTime n = DateTime.Now;
            if (steps.Count > 0) { StepRec p = steps[steps.Count - 1]; p.Done = true; p.Secs = (n - p.Start).TotalSeconds; }
            StepRec s = new StepRec(); s.T = title; s.Start = n; s.Do.AddRange(Detail(title)); steps.Add(s);
        }
        static string[] Detail(string t)
        {
            if (t.StartsWith("Backup")) return new string[] { "Saved your original services + settings (for RESTORE)", "Created a Windows restore point (first run only)" };
            if (t.StartsWith("Health")) return new string[] { "Checks recent crashes, damaged system files, broken services and drivers", "Repairs what it can automatically", "If the PC is unstable: skips risky tweaks (MSI, timer, security-off, extra service cuts)" };
            if (t.StartsWith("Ultimate power")) return new string[] { "Ultimate Performance power plan ON", "CPU cores never park, full speed all the time", "Hibernation + power throttling OFF", "Laptop on battery keeps a saving mode" };
            if (t.StartsWith("Merging svchost")) return new string[] { "Merged background service hosts", "Far fewer running processes, more free RAM" };
            if (t.StartsWith("Disabling useless")) return new string[] { "Stopped telemetry, Maps, Fax, Xbox, updaters...", "Services for hardware you do not have: OFF", "Game launchers no longer start with Windows", "Windows Hello PIN + sign-in kept working" };
            if (t.StartsWith("Telemetry")) return new string[] { "Telemetry, ad ID, Copilot, Recall, Widgets: OFF", "Cortana + web results in Start search: OFF", "Edge background mode + startup boost: OFF", "Background apps + error reporting: OFF" };
            if (t.StartsWith("Removing bloat")) return new string[] { "Removed preinstalled junk apps (news, weather, tips...)", "Copilot removed", "Xbox, Store, Photos, Calculator kept" };
            if (t.StartsWith("Game Mode")) return new string[] { "Game Mode ON, Game Bar + Game DVR OFF", "Hardware GPU scheduling ON", "Games get High GPU + CPU + disk priority", "Fullscreen optimization fix for common games" };
            if (t.StartsWith("Lower input")) return new string[] { "Scheduler favors the game you are playing", "Mouse acceleration OFF, keyboard delay minimum", "Smaller mouse + keyboard buffers", "Faster app close, no startup delay" };
            if (t.StartsWith("Network")) return new string[] { "Lower ping: faster TCP replies on every adapter", "Fast DNS: 1.1.1.1 + 8.8.8.8", "Download speed tuning (auto-tuning, RSS)", "Adapter power saving OFF, DNS cache flushed" };
            if (t.StartsWith("Light visuals")) return new string[] { "Animations + transparency OFF", "Memory kept in RAM, prefetch OFF", "NVMe + USB power saving OFF", "NTFS last-access OFF, TRIM ON, MSI interrupts" };
            if (t.StartsWith("VBS")) return new string[] { "VBS + Memory Integrity + mitigations OFF", "Windows Defender real-time + SmartScreen OFF" };
            if (t.StartsWith("Security kept")) return new string[] { "Windows security left ON (Safe Boost)" };
            if (t.StartsWith("Tuning for your CPU")) return new string[] { "Settings matched to your CPU brand" };
            if (t.StartsWith("Storage")) return new string[] { "SSD/NVMe: SysMain + Prefetch OFF, TRIM ON", "HDD: kept ON because it helps hard drives" };
            if (t.StartsWith("Mouse")) return new string[] { "Input devices checked, lowest delay set" };
            if (t.StartsWith("Setting monitor")) return new string[] { "Monitor set to its highest refresh rate" };
            if (t.StartsWith("Detecting your GPU")) return new string[] { "Graphics card tweaks for your GPU brand", "NVIDIA: low latency + max performance profile" };
            if (t.StartsWith("Extra cuts")) return new string[] { "More background processes OFF (safe list only)" };
            if (t.StartsWith("Scanning your games")) return new string[] { "Found your games and tuned each one", "Game settings files only - folders untouched" };
            if (t.StartsWith("Browsers")) return new string[] { "Browser downloads go straight to Downloads" };
            if (t.StartsWith("Cleaning temp")) return new string[] { "Temp, Windows temp, Prefetch, update cache deleted" };
            if (t.StartsWith("Freeing disk")) return new string[] { "OneDrive removed only if present (Full Boost)", "Quick disk cleanup", "RAM cleaned" };
            return new string[0];
        }
        bool running = false, done = false;
        int doneKind = 0;             // 0 boost, 1 restore
        string runTitle = "BOOSTING";
        float pct = 0f, dispPct = 0f;
        string stepText = "";
        int errs = -1;
        List<string> errList = new List<string>();
        List<string> logs = new List<string>();
        string batPath = null;

        // monitor
        PerformanceCounter cpuCounter;
        bool cpuTried = false;
        float cpuNow = 0f, ramNow = 0f; int procCount = 0; double ramTotalGb = 0, ramUsedGb = 0;
        float[] cpuHist = new float[60];
        double lastSample = -10;

        // ui
        List<Hit> hits = new List<Hit>();
        string hoverId = null, downId = null;
        Dictionary<string, float> hv = new Dictionary<string, float>();
        Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        float[] px = new float[40], py = new float[40], ps = new float[40], pz = new float[40];

        public MainForm()
        {
            using (Graphics g0 = CreateGraphics()) { sc = g0.DpiX / 96f; }
            if (sc < 1f) sc = 1f;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size((int)(W * sc), (int)(H * sc));
            Text = "Bu 7maid Booster";
            BackColor = T.Bg0;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            try
            {
                using (GraphicsPath gp = Round(new RectangleF(0, 0, W * sc, H * sc), 18f * sc)) { Region = new Region(gp); }
            }
            catch { }
            Bu7maidZero.MemTrim.Start();
            Shown += delegate
            {
                Bu7maidZero.Updater.Start("Booster", Version, this, delegate { return running; });
                ReadSpecsAsync();
                // the helper engine form is built after the window is already on screen, so opening never freezes
                System.Windows.Forms.Timer st = new System.Windows.Forms.Timer(); st.Interval = 350;
                st.Tick += delegate
                {
                    st.Stop(); st.Dispose();
                    try { Bu7maidZero.Tg.Name = "Booster " + Version; z = new Bu7maidZero.MainForm(); z.BInit(); z.BQuiet(); Bu7maidZero.Tg.Hello(); ocSummary = z.BOcSummary(); } catch { z = null; }
                };
                st.Start();
            };
            StartMonitor();
            for (int i = 0; i < px.Length; i++) { px[i] = (float)(rnd.NextDouble() * W); py[i] = (float)(rnd.NextDouble() * H); ps[i] = 6f + (float)rnd.NextDouble() * 18f; pz[i] = 1.2f + (float)rnd.NextDouble() * 2.2f; }
            stepText = "";
            tick = new System.Windows.Forms.Timer();
            tick.Interval = 15;
            tick.Tick += OnTick;
            tick.Start();
        }

        // ---------------- helpers ----------------
        static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = rad * 2f;
            if (d > r.Width) d = r.Width; if (d > r.Height) d = r.Height;
            if (d <= 0.5f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        Font Fnt(string family, float size, FontStyle st)
        {
            string key = family + "|" + size.ToString("0.0") + "|" + (int)st;
            Font f;
            if (!fonts.TryGetValue(key, out f)) { f = new Font(family, size, st, GraphicsUnit.Point); fonts[key] = f; }
            return f;
        }
        void Txt(Graphics g, string s, Font f, Color c, float x, float y, float w, float h, StringAlignment ha, StringAlignment va)
        {
            using (StringFormat sf = new StringFormat())
            using (SolidBrush b = new SolidBrush(c))
            {
                sf.Alignment = ha; sf.LineAlignment = va; sf.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(s, f, b, new RectangleF(x, y, w, h), sf);
            }
        }
        float Hov(string id) { float v; return hv.TryGetValue(id, out v) ? v : 0f; }
        void AddHit(string id, float x, float y, float w, float h)
        {
            Hit ht = new Hit(); ht.Id = id; ht.R = new RectangleF(x, y, w, h); hits.Add(ht);
            if (!hv.ContainsKey(id)) hv[id] = 0f;
        }
        static Color Alpha(Color c, int a) { if (a < 0) a = 0; if (a > 255) a = 255; return Color.FromArgb(a, c.R, c.G, c.B); }
        static Color Mix(Color a, Color b, float t)
        {
            if (t < 0) t = 0; if (t > 1) t = 1;
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        // ---------------- timing / data ----------------
        void AutoRestartCheck() { if (restartAt != DateTime.MinValue && DateTime.Now >= restartAt) { restartAt = DateTime.MinValue; RestartPc(); } }
        void OnTick(object s, EventArgs e)
        { AutoRestartCheck();
            double t = sw.Elapsed.TotalSeconds;
            double dt = t - lastT; if (dt > 0.1) dt = 0.1; lastT = t;
            dispPct += (pct - dispPct) * 0.12f;
            tglAnim += ((delStartup ? 1f : 0f) - tglAnim) * 0.25f; tglAnim2 += ((measure ? 1f : 0f) - tglAnim2) * 0.25f;
            List<string> keys = new List<string>(hv.Keys);
            foreach (string k in keys) { float target = (k == hoverId) ? 1f : 0f; hv[k] += (target - hv[k]) * 0.25f; }
            for (int i = 0; i < px.Length; i++)
            {
                py[i] -= ps[i] * (float)dt;
                if (py[i] < -10f) { py[i] = H + 10f; px[i] = (float)(rnd.NextDouble() * W); }
            }
            if (t - lastSample >= 1.0) { lastSample = t; Sample(); }
            if (WindowState != FormWindowState.Minimized) Invalidate();
        }
        // the slow parts (performance counter, process list) run on a background thread - the window never waits for them
        void StartMonitor()
        {
            Thread th = new Thread(delegate ()
            {
                while (true)
                {
                    try
                    {
                        if (!cpuTried)
                        {
                            cpuTried = true;
                            try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); cpuCounter.NextValue(); } catch { cpuCounter = null; }
                        }
                        if (cpuCounter != null) { try { cpuNow = cpuCounter.NextValue(); } catch { } }
                        if (cpuNow < 0) cpuNow = 0; if (cpuNow > 100) cpuNow = 100;
                        MEMSTAT ms = new MEMSTAT(); ms.dwLength = (uint)Marshal.SizeOf(typeof(MEMSTAT));
                        if (GlobalMemoryStatusEx(ref ms))
                        {
                            ramNow = ms.dwMemoryLoad; ramTotalGb = ms.ullTotalPhys / 1073741824.0; ramUsedGb = (ms.ullTotalPhys - ms.ullAvailPhys) / 1073741824.0;
                        }
                        Process[] pl = Process.GetProcesses(); procCount = pl.Length; foreach (Process pp in pl) pp.Dispose();
                    }
                    catch { }
                    Thread.Sleep(1500);
                }
            });
            th.IsBackground = true; th.Priority = ThreadPriority.BelowNormal; th.Start();
        }
        void Sample()
        {
            try
            {
                for (int i = 0; i < cpuHist.Length - 1; i++) cpuHist[i] = cpuHist[i + 1];
                cpuHist[cpuHist.Length - 1] = cpuNow;
                if (z != null && page == 2) ocSummary = z.BOcSummary();
            }
            catch { }
        }

        // ---------------- painting ----------------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.ResetTransform();
            g.ScaleTransform(sc, sc);
            double t = sw.Elapsed.TotalSeconds;
            hits.Clear();
            DrawBackground(g, t);
            DrawTitle(g);
            DrawNav(g);
            if (page == 0) DrawBoostPage(g, t);
            else if (page == 1) DrawGamesPage(g, t);
            else if (page == 2) DrawOcPage(g, t);
            else if (page == 3) DrawToolsPage(g, t);
            else if (page == 6) DrawResultPage(g, t);
            else if (page == 7) DrawStatusPage(g, t);
            else if (page == 4) DrawMonitorPage(g, t);
            else DrawRestorePage(g, t);
            DrawFooter(g);
        }

        void DrawBackground(Graphics g, double t)
        {
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(0, 0, W, H), T.Bg0, T.Bg1, 55f)) g.FillRectangle(b, 0, 0, W, H);
            Glow(g, (float)(220 + 130 * Math.Sin(t * 0.30)), (float)(170 + 70 * Math.Cos(t * 0.25)), 380f, T.A1, 46);
            Glow(g, (float)(780 + 110 * Math.Cos(t * 0.22)), (float)(470 + 80 * Math.Sin(t * 0.31)), 340f, T.A2, 30);
            for (int i = 0; i < px.Length; i++)
            {
                int a = (int)(25 + 45 * (0.5 + 0.5 * Math.Sin(t * 0.8 + i)));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 190, 200, 255))) g.FillEllipse(b, px[i], py[i], pz[i], pz[i]);
            }
        }
        void Glow(Graphics g, float cx, float cy, float r, Color c, int alpha)
        {
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                using (PathGradientBrush pg = new PathGradientBrush(gp))
                {
                    pg.CenterColor = Alpha(c, alpha);
                    pg.SurroundColors = new Color[] { Alpha(c, 0) };
                    g.FillPath(pg, gp);
                }
            }
        }

        void DrawTitle(Graphics g)
        {
            // logo
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(22, 12, 30, 30), T.A1, T.A2, 45f))
            using (GraphicsPath gp = Round(new RectangleF(22, 12, 30, 30), 9f)) g.FillPath(b, gp);
            Glyph(g, 0, 37, 27, 11f, Color.White);
            Txt(g, "BU 7MAID BOOSTER", Fnt("Segoe UI Semibold", 11.5f, FontStyle.Bold), T.Txt, 62, 8, 300, 22, StringAlignment.Near, StringAlignment.Center);
            Txt(g, "one-click gaming optimizer", Fnt("Segoe UI", 8.5f, FontStyle.Regular), T.Muted, 62, 27, 300, 18, StringAlignment.Near, StringAlignment.Center);
            // window buttons
            AddHit("min", W - 96, 10, 40, 32); AddHit("close", W - 52, 10, 40, 32);
            float hm = Hov("min"), hc = Hov("close");
            if (hm > 0.01f) using (SolidBrush b = new SolidBrush(Alpha(Color.White, (int)(26 * hm)))) { using (GraphicsPath gp = Round(new RectangleF(W - 96, 10, 40, 32), 8f)) g.FillPath(b, gp); }
            if (hc > 0.01f) using (SolidBrush b = new SolidBrush(Alpha(T.Bad, (int)(200 * hc)))) { using (GraphicsPath gp = Round(new RectangleF(W - 52, 10, 40, 32), 8f)) g.FillPath(b, gp); }
            using (Pen p = new Pen(Mix(T.Muted, Color.White, Math.Max(hm, 0f)), 1.6f)) g.DrawLine(p, W - 83, 26, W - 69, 26);
            using (Pen p = new Pen(Mix(T.Muted, Color.White, hc), 1.6f)) { g.DrawLine(p, W - 39, 20, W - 25, 32); g.DrawLine(p, W - 25, 20, W - 39, 32); }
        }

        void DrawNav(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Alpha(Color.Black, 70))) g.FillRectangle(b, 0, 52, 84, H - 52);
            using (Pen p = new Pen(Alpha(T.Line, 120), 1f)) g.DrawLine(p, 84, 52, 84, H);
            string[] ids = { "nav0", "nav1", "nav2", "nav3", "nav4", "nav5", "nav6", "nav7" };
            string[] names = { "BOOST", "GAMES", "OVERCLOCK", "TOOLS", "MONITOR", "RESTORE", "RESULT", "STATUS" };
            int[] gk = { 0, 4, 5, 6, 1, 2, 7, 3 };
            int[] order = { 0, 1, 2, 3, 7, 6, 4, 5 };
            for (int oi = 0; oi < 8; oi++)
            {
                int i = order[oi];
                float y = 54 + oi * 72;
                AddHit(ids[i], 8, y, 68, 66);
                float h = Hov(ids[i]); bool act = (page == i);
                if (act || h > 0.02f)
                {
                    using (SolidBrush b = new SolidBrush(Alpha(T.A1, act ? 50 : (int)(34 * h)))) { using (GraphicsPath gp = Round(new RectangleF(8, y, 68, 66), 14f)) g.FillPath(b, gp); }
                }
                if (act)
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(0, y + 12, 4, 42), T.A1, T.A2, 90f)) g.FillRectangle(b, 0, y + 12, 4, 42);
                }
                Color ic = act ? T.Txt : Mix(T.Muted, T.Txt, h);
                Glyph(g, gk[i], 42, y + 23, 11f, ic);
                Txt(g, names[i], Fnt("Segoe UI Semibold", 7f, FontStyle.Bold), ic, 4, y + 42, 76, 20, StringAlignment.Center, StringAlignment.Center);
            }
        }

        void DrawFooter(Graphics g)
        {
            Txt(g, "Bu 7maid Booster " + Version + "   |   Windows 10 / 11   |   needs Administrator", Fnt("Segoe UI", 8f, FontStyle.Regular), Alpha(T.Muted, 200), 100, H - 28, 520, 20, StringAlignment.Near, StringAlignment.Center);
            Txt(g, "backups: C:\\Bu7maid_Backup", Fnt("Segoe UI", 8f, FontStyle.Regular), Alpha(T.Muted, 200), W - 360, H - 28, 340, 20, StringAlignment.Far, StringAlignment.Center);
        }

        // vector icons: 0 bolt, 1 pulse, 2 undo, 3 check
        void Glyph(Graphics g, int kind, float cx, float cy, float s, Color c)
        {
            using (Pen p = new Pen(c, 2.2f))
            using (SolidBrush b = new SolidBrush(c))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                if (kind == 0)
                {
                    PointF[] pts = {
                        new PointF(cx + 0.15f * s, cy - 1.0f * s), new PointF(cx - 0.55f * s, cy + 0.12f * s), new PointF(cx - 0.05f * s, cy + 0.12f * s),
                        new PointF(cx - 0.18f * s, cy + 1.0f * s), new PointF(cx + 0.55f * s, cy - 0.18f * s), new PointF(cx + 0.05f * s, cy - 0.18f * s) };
                    g.FillPolygon(b, pts);
                }
                else if (kind == 1)
                {
                    PointF[] pts = {
                        new PointF(cx - 1.0f * s, cy), new PointF(cx - 0.4f * s, cy), new PointF(cx - 0.2f * s, cy - 0.8f * s),
                        new PointF(cx + 0.15f * s, cy + 0.8f * s), new PointF(cx + 0.38f * s, cy), new PointF(cx + 1.0f * s, cy) };
                    g.DrawLines(p, pts);
                }
                else if (kind == 2)
                {
                    g.DrawArc(p, cx - 0.8f * s, cy - 0.8f * s, 1.6f * s, 1.6f * s, 200, 250);
                    PointF[] tri = { new PointF(cx - 0.95f * s, cy - 0.85f * s), new PointF(cx - 0.95f * s, cy - 0.1f * s), new PointF(cx - 0.2f * s, cy - 0.45f * s) };
                    g.FillPolygon(b, tri);
                }
                else if (kind == 4)
                {
                    using (GraphicsPath gp = Round(new RectangleF(cx - 1.1f * s, cy - 0.6f * s, 2.2f * s, 1.2f * s), 0.5f * s)) g.DrawPath(p, gp);
                    g.DrawLine(p, cx - 0.65f * s, cy - 0.25f * s, cx - 0.65f * s, cy + 0.25f * s);
                    g.DrawLine(p, cx - 0.9f * s, cy, cx - 0.4f * s, cy);
                    g.FillEllipse(b, cx + 0.35f * s, cy - 0.3f * s, 0.26f * s, 0.26f * s);
                    g.FillEllipse(b, cx + 0.62f * s, cy - 0.02f * s, 0.26f * s, 0.26f * s);
                }
                else if (kind == 7)
                {
                    g.FillRectangle(b, cx - 1.0f * s, cy + 0.1f * s, 0.55f * s, 0.8f * s);
                    g.FillRectangle(b, cx - 0.27f * s, cy - 0.45f * s, 0.55f * s, 1.35f * s);
                    g.FillRectangle(b, cx + 0.46f * s, cy - 0.9f * s, 0.55f * s, 1.8f * s);
                }
                else if (kind == 5)
                {
                    g.DrawArc(p, cx - 1.0f * s, cy - 0.8f * s, 2.0f * s, 2.0f * s, 180, 180);
                    g.DrawLine(p, cx, cy + 0.2f * s, cx + 0.5f * s, cy - 0.45f * s);
                    g.FillEllipse(b, cx - 0.12f * s, cy + 0.08f * s, 0.24f * s, 0.24f * s);
                }
                else if (kind == 6)
                {
                    g.DrawLine(p, cx - 1.0f * s, cy - 0.6f * s, cx + 1.0f * s, cy - 0.6f * s);
                    g.DrawLine(p, cx - 1.0f * s, cy, cx + 1.0f * s, cy);
                    g.DrawLine(p, cx - 1.0f * s, cy + 0.6f * s, cx + 1.0f * s, cy + 0.6f * s);
                    g.FillEllipse(b, cx - 0.5f * s, cy - 0.8f * s, 0.4f * s, 0.4f * s);
                    g.FillEllipse(b, cx + 0.3f * s, cy - 0.2f * s, 0.4f * s, 0.4f * s);
                    g.FillEllipse(b, cx - 0.2f * s, cy + 0.4f * s, 0.4f * s, 0.4f * s);
                }
                else
                {
                    PointF[] pts = { new PointF(cx - 0.55f * s, cy), new PointF(cx - 0.12f * s, cy + 0.45f * s), new PointF(cx + 0.6f * s, cy - 0.45f * s) };
                    p.Width = 3.4f;
                    g.DrawLines(p, pts);
                }
            }
        }

        // ---------------- reusable widgets ----------------
        void CardBox(Graphics g, float x, float y, float w, float h)
        {
            using (GraphicsPath gp = Round(new RectangleF(x, y, w, h), 16f))
            {
                using (SolidBrush b = new SolidBrush(Alpha(T.Card, 190))) g.FillPath(b, gp);
                using (Pen p = new Pen(Alpha(T.Line, 150), 1f)) g.DrawPath(p, gp);
            }
        }
        // kind 0 gradient, 1 ghost, 2 danger
        void Btn(Graphics g, string id, string text, float x, float y, float w, float h, int kind, bool selected)
        {
            AddHit(id, x, y, w, h);
            float hv0 = Hov(id);
            using (GraphicsPath gp = Round(new RectangleF(x, y, w, h), h / 2f))
            {
                if (kind == 0 || selected)
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x, y, w, h), Mix(T.A1, Color.White, hv0 * 0.15f), Mix(T.A2, Color.White, hv0 * 0.15f), 0f)) g.FillPath(b, gp);
                }
                else if (kind == 2)
                {
                    using (SolidBrush b = new SolidBrush(Alpha(T.Bad, (int)(40 + 90 * hv0)))) g.FillPath(b, gp);
                    using (Pen p = new Pen(Alpha(T.Bad, 200), 1.2f)) g.DrawPath(p, gp);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Alpha(Color.White, (int)(10 + 26 * hv0)))) g.FillPath(b, gp);
                    using (Pen p = new Pen(Alpha(T.Line, 220), 1.2f)) g.DrawPath(p, gp);
                }
            }
            Color tc = (kind == 0 || selected) ? Color.White : (kind == 2 ? Mix(T.Bad, Color.White, 0.5f) : T.Txt);
            Txt(g, text, Fnt("Segoe UI Semibold", 9.5f, FontStyle.Bold), tc, x, y, w, h, StringAlignment.Center, StringAlignment.Center);
        }
        void Ring(Graphics g, float cx, float cy, float r, float frac, Color c1, Color c2, string big, string small)
        {
            if (frac < 0) frac = 0; if (frac > 1) frac = 1;
            using (Pen tp = new Pen(Alpha(Color.White, 22), 9f)) g.DrawEllipse(tp, cx - r, cy - r, r * 2, r * 2);
            if (frac > 0.004f)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(cx - r, cy - r, r * 2, r * 2), c1, c2, 45f))
                using (Pen p = new Pen(b, 9f))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                    g.DrawArc(p, cx - r, cy - r, r * 2, r * 2, -90, frac * 359.5f);
                }
            }
            Txt(g, big, Fnt("Segoe UI Semibold", r * 0.34f, FontStyle.Bold), T.Txt, cx - r, cy - r * 0.42f, r * 2, r * 0.7f, StringAlignment.Center, StringAlignment.Center);
            Txt(g, small, Fnt("Segoe UI", 8.5f, FontStyle.Regular), T.Muted, cx - r, cy + r * 0.18f, r * 2, 22, StringAlignment.Center, StringAlignment.Center);
        }
        void StatCard(Graphics g, float x, float y, float w, float h, string label, string value, float frac, Color c1, Color c2)
        {
            CardBox(g, x, y, w, h);
            Txt(g, label, Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, x + 18, y + 10, w - 36, 18, StringAlignment.Near, StringAlignment.Center);
            Txt(g, value, Fnt("Segoe UI Semibold", 17f, FontStyle.Bold), T.Txt, x + 18, y + 28, w - 36, 34, StringAlignment.Near, StringAlignment.Center);
            using (GraphicsPath gp = Round(new RectangleF(x + 18, y + h - 20, w - 36, 6), 3f)) using (SolidBrush b = new SolidBrush(Alpha(Color.White, 22))) g.FillPath(b, gp);
            if (frac > 0.01f)
            {
                float fw = (w - 36) * Math.Min(1f, frac);
                using (GraphicsPath gp = Round(new RectangleF(x + 18, y + h - 20, Math.Max(6f, fw), 6), 3f))
                using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x + 18, y + h - 20, Math.Max(6f, fw), 6), c1, c2, 0f)) g.FillPath(b, gp);
            }
        }

        // ---------------- pages ----------------
        void DrawBoostPage(Graphics g, double t)
        {
            // left info card
            CardBox(g, 104, 96, 232, 276);
            Txt(g, "WHAT BOOST TUNES", Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, 124, 108, 200, 18, StringAlignment.Near, StringAlignment.Center);
            string[] items = { "Network + input latency", "Games: GPU, priority, FPS", "Services + startup apps", "Timer, power plan, CPU", "Cleans temp, cache, RAM", "Browsers: downloads folder" };
            Color[] cols = { T.A2, T.A1, T.Good, T.Warn, T.A2, T.A1 };
            for (int i = 0; i < items.Length; i++)
            {
                float y = 138 + i * 38;
                using (SolidBrush b = new SolidBrush(cols[i])) g.FillEllipse(b, 126, y + 8, 8, 8);
                Txt(g, items[i], Fnt("Segoe UI", 9.5f, FontStyle.Regular), T.Txt, 144, y, 188, 24, StringAlignment.Near, StringAlignment.Center);
            }
            // right stat cards
            StatCard(g, 704, 96, 244, 84, "CPU", ((int)cpuNow) + " %", cpuNow / 100f, T.A2, T.A1);
            StatCard(g, 704, 192, 244, 84, "MEMORY", ((int)ramNow) + " %   " + ramUsedGb.ToString("0.0") + " / " + ramTotalGb.ToString("0") + " GB", ramNow / 100f, T.A1, T.A2);
            StatCard(g, 704, 288, 244, 84, "RUNNING PROCESSES", procCount.ToString(), Math.Min(1f, procCount / 300f), T.Good, T.A2);

            DrawDial(g, t, 520f, 238f, 112f);

            // text under dial
            string line;
            Color lc = T.Muted;
            if (running) line = stepText.Length > 0 ? stepText : "Working...";
            else if (done)
            {
                string nm = doneKind == 0 ? "Boost" : (doneKind == 1 ? "Restore" : (doneKind == 2 ? "Game tuning" : "Task"));
                if (errs > 0) { line = nm + " done - " + errs + " small problem(s), the rest is applied"; lc = T.Warn; }
                else { line = doneKind == 1 ? "Everything is back to normal" : nm + " complete - all done"; lc = T.Good; }
            }
            else line = "Tweaks network, input, games, services and startup in one click";
            Txt(g, line, Fnt("Segoe UI", 11f, FontStyle.Regular), lc, 330, 380, 380, 28, StringAlignment.Center, StringAlignment.Center);

            if (running)
            {
                DrawLive(g, t);
            }
            else if (done)
            {
                if (doneKind <= 1)
                {
                    Btn(g, "restartNow", "RESTART NOW", 392, 436, 180, 44, 0, false);
                    Btn(g, "later", restartAt != DateTime.MinValue ? "CANCEL" : "LATER", 584, 436, 130, 44, 1, false);
                    if (doneKind == 0 && lastSteps != null) Btn(g, "report", "REPORT", 726, 436, 130, 44, 1, false);
                    Txt(g, restartAt != DateTime.MinValue ? "Restarting by itself in " + Math.Max(0, (int)Math.Ceiling((restartAt - DateTime.Now).TotalSeconds)) + " s - then the after-check runs alone" : doneKind == 0 && z != null && z.BAfterPending ? "After the restart a small window shows your FPS + DPC before / after" : "A restart finishes some tweaks (timer, network, drivers)", Fnt("Segoe UI", 8.5f, FontStyle.Regular), Alpha(T.Muted, 220), 300, 492, 440, 20, StringAlignment.Center, StringAlignment.Center);
                }
                else Btn(g, "later", "OK", 450, 436, 140, 44, 0, false);
                if (errList.Count > 0)
                {
                    Txt(g, errList[0], Fnt("Segoe UI", 8.5f, FontStyle.Regular), Alpha(T.Warn, 230), 270, 514, 500, 20, StringAlignment.Center, StringAlignment.Center);
                }
            }
            else
            {
                // segmented mode
                float sx = 536 - 160, sy = 424;
                using (GraphicsPath gp = Round(new RectangleF(sx - 4, sy - 4, 328, 52), 26f))
                {
                    using (SolidBrush b = new SolidBrush(Alpha(Color.White, 12))) g.FillPath(b, gp);
                    using (Pen p = new Pen(Alpha(T.Line, 200), 1f)) g.DrawPath(p, gp);
                }
                Btn(g, "full", "FULL BOOST", sx, sy, 160, 44, 1, mode == 0);
                Btn(g, "safe", "SAFE BOOST", sx + 164, sy, 160, 44, 1, mode == 1);
                string cap = mode == 0 ? "Full: max FPS and lowest delay. Windows antivirus is turned OFF." : "Safe: same tweaks, Windows security stays ON (best for shared PCs).";
                Txt(g, cap, Fnt("Segoe UI", 8.5f, FontStyle.Regular), T.Muted, 290, 478, 460, 20, StringAlignment.Center, StringAlignment.Center);
                Toggle(g, "toggleDel", 330, 508, tglAnim, "Also delete startup apps (instead of only turning them off)");
                Toggle(g, "toggleMeasure", 330, 542, tglAnim2, "Measure FPS + DPC before / after the restart (recommended)");
            }
        }

        void Toggle(Graphics g, string id, float tx, float ty, float anim, string label)
        {
            AddHit(id, tx, ty - 4, 400, 32);
            using (GraphicsPath gp = Round(new RectangleF(tx, ty, 44, 24), 12f))
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(tx, ty, 44, 24), Mix(Color.FromArgb(60, 64, 100), T.A1, anim), Mix(Color.FromArgb(60, 64, 100), T.A2, anim), 0f)) g.FillPath(b, gp);
            using (SolidBrush b2 = new SolidBrush(Color.White)) g.FillEllipse(b2, tx + 3 + 20 * anim, ty + 3, 18, 18);
            Txt(g, label, Fnt("Segoe UI", 9f, FontStyle.Regular), T.Txt, tx + 56, ty - 2, 380, 28, StringAlignment.Near, StringAlignment.Center);
        }

        void DrawDial(Graphics g, double t, float cx, float cy, float R)
        {
            AddHit("dial", cx - R, cy - R, R * 2, R * 2);
            float h = Hov("dial");
            float pulse = (float)(0.5 + 0.5 * Math.Sin(t * (running ? 4.0 : 1.6)));
            Glow(g, cx, cy, R + 46f + 6f * pulse, done ? T.Good : T.A1, (int)((running ? 90 : 56) + 30 * pulse + 30 * h));
            // track
            using (Pen tp = new Pen(Alpha(Color.White, 24), 10f)) g.DrawEllipse(tp, cx - R, cy - R, R * 2, R * 2);
            RectangleF rr = new RectangleF(cx - R, cy - R, R * 2, R * 2);
            if (running)
            {
                float sweep = dispPct * 3.595f;
                if (sweep > 1f)
                    using (LinearGradientBrush b = new LinearGradientBrush(rr, T.A1, T.A2, 45f))
                    using (Pen p = new Pen(b, 10f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, rr, -90, sweep); }
                // moving highlight
                float a0 = (float)((t * 220.0) % 360.0);
                using (Pen p = new Pen(Alpha(Color.White, 90), 3f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, rr, a0, 30); }
            }
            else if (done)
            {
                using (Pen p = new Pen(T.Good, 10f)) g.DrawEllipse(p, rr);
            }
            else
            {
                float a0 = (float)((t * 70.0) % 360.0);
                using (LinearGradientBrush b = new LinearGradientBrush(rr, T.A1, T.A2, 45f))
                using (Pen p = new Pen(b, 10f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, rr, a0, 80 + 20 * pulse); }
                float r2 = R - 16;
                using (Pen p = new Pen(Alpha(T.A2, 90), 2f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, cx - r2, cy - r2, r2 * 2, r2 * 2, -(float)((t * 110.0) % 360.0), 50); }
            }
            // inner disc
            float ri = R - 24 + 3f * h;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - ri, cy - ri, ri * 2, ri * 2);
                using (PathGradientBrush pg = new PathGradientBrush(gp))
                {
                    pg.CenterColor = Mix(Color.FromArgb(36, 40, 86), Color.FromArgb(58, 54, 130), h);
                    pg.SurroundColors = new Color[] { Color.FromArgb(14, 16, 38) };
                    g.FillPath(pg, gp);
                }
                using (Pen p = new Pen(Alpha(T.A1, 120), 1.2f)) g.DrawPath(p, gp);
            }
            if (running)
            {
                Txt(g, ((int)Math.Round(dispPct)) + "%", Fnt("Segoe UI Semibold", 38f, FontStyle.Bold), T.Txt, cx - ri, cy - 40, ri * 2, 70, StringAlignment.Center, StringAlignment.Center);
                Txt(g, runTitle, Fnt("Segoe UI Semibold", 9f, FontStyle.Bold), T.A2, cx - ri, cy + 28, ri * 2, 22, StringAlignment.Center, StringAlignment.Center);
            }
            else if (done)
            {
                Glyph(g, 3, cx, cy - 12, 26f, T.Good);
                Txt(g, "DONE", Fnt("Segoe UI Semibold", 16f, FontStyle.Bold), T.Txt, cx - ri, cy + 24, ri * 2, 30, StringAlignment.Center, StringAlignment.Center);
            }
            else
            {
                Glyph(g, 0, cx, cy - 30, 15f, Mix(T.A2, Color.White, 0.3f + 0.4f * h));
                Txt(g, "BOOST", Fnt("Segoe UI Black", 26f, FontStyle.Bold), T.Txt, cx - ri, cy - 6, ri * 2, 44, StringAlignment.Center, StringAlignment.Center);
                Txt(g, "TAP TO OPTIMIZE", Fnt("Segoe UI", 7.5f, FontStyle.Bold), T.Muted, cx - ri, cy + 36, ri * 2, 20, StringAlignment.Center, StringAlignment.Center);
            }
        }

        void PageHead(Graphics g, string title, string sub)
        {
            Txt(g, title, Fnt("Segoe UI Semibold", 20f, FontStyle.Bold), T.Txt, 104, 66, 600, 36, StringAlignment.Near, StringAlignment.Center);
            Txt(g, sub, Fnt("Segoe UI", 10f, FontStyle.Regular), T.Muted, 104, 102, 800, 22, StringAlignment.Near, StringAlignment.Center);
        }
        static string N0(double v) { return v.ToString("0", System.Globalization.CultureInfo.InvariantCulture); }
        static string N1(double v) { return v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); }
        void ResCard(Graphics g, float x, float y, float w, string tag, Color col, Bu7maidZero.BenchRes b)
        {
            CardBox(g, x, y, w, 214);
            Txt(g, tag, Fnt("Segoe UI Semibold", 8.5f, FontStyle.Bold), col, x + 20, y + 14, w - 40, 20, StringAlignment.Near, StringAlignment.Center);
            if (b == null) { Txt(g, "no result saved yet", Fnt("Segoe UI", 10.5f, FontStyle.Regular), T.Muted, x, y + 70, w, 60, StringAlignment.Center, StringAlignment.Center); return; }
            Txt(g, N0(b.Avg), Fnt("Segoe UI Black", 40f, FontStyle.Bold), T.Txt, x, y + 38, w, 70, StringAlignment.Center, StringAlignment.Center);
            Txt(g, "FPS average", Fnt("Segoe UI", 9f, FontStyle.Regular), T.Muted, x, y + 106, w, 20, StringAlignment.Center, StringAlignment.Center);
            Txt(g, "1% low " + N0(b.Low1) + "      score " + b.Score.ToString("N0"), Fnt("Segoe UI", 9.5f, FontStyle.Regular), T.Txt, x, y + 132, w, 22, StringAlignment.Center, StringAlignment.Center);
            if (b.DpcMax > 0) Txt(g, "DPC worst " + N0(b.DpcMax) + " us      typical " + N0(b.DpcP99) + " us", Fnt("Segoe UI", 9.5f, FontStyle.Regular), b.DpcMax <= 150 ? T.Good : (b.DpcMax <= 500 ? T.Warn : T.Bad), x, y + 158, w, 22, StringAlignment.Center, StringAlignment.Center);
            if (b.TempMax > 0) Txt(g, "GPU max " + b.TempMax + " C    core " + b.ClockAvg + " MHz", Fnt("Segoe UI", 8.5f, FontStyle.Regular), T.Muted, x, y + 182, w, 20, StringAlignment.Center, StringAlignment.Center);
        }
        void DrawResultPage(Graphics g, double t)
        {
            PageHead(g, "Result", "Your FPS and DPC delay before and after the boost - same 3D test, real numbers.");
            ResCard(g, 104, 140, 410, "BEFORE", T.Muted, resB);
            ResCard(g, 534, 140, 410, "AFTER", T.Good, resA);
            CardBox(g, 104, 372, 840, 130);
            if (resB != null && resA != null && resB.Avg > 0)
            {
                double d = resA.Avg - resB.Avg, pc = d / resB.Avg * 100.0;
                Color vc = pc > 1.5 ? T.Good : (pc < -1.5 ? T.Bad : T.Warn);
                string s1 = pc > 1.5 ? "+" + N1(d) + " FPS   (+" + N1(pc) + " %)" : (pc < -1.5 ? N1(d) + " FPS   (" + N1(pc) + " %)" : "about the same   (" + (pc >= 0 ? "+" : "") + N1(pc) + " %)");
                Txt(g, s1, Fnt("Segoe UI Semibold", 20f, FontStyle.Bold), vc, 104, 384, 840, 44, StringAlignment.Center, StringAlignment.Center);
                string s2 = "";
                if (resB.DpcP99 > 0 && resA.DpcP99 > 0) { double dq = (resA.DpcP99 - resB.DpcP99) / resB.DpcP99 * 100.0; s2 = "DPC delay: " + (dq <= -3 ? N0(-dq) + " % lower - better" : (dq >= 3 ? N0(dq) + " % higher - worse" : "about the same")); }
                Txt(g, s2, Fnt("Segoe UI", 11f, FontStyle.Regular), T.Txt, 104, 432, 840, 26, StringAlignment.Center, StringAlignment.Center);
                Txt(g, "Score " + resB.Score.ToString("N0") + "  to  " + resA.Score.ToString("N0"), Fnt("Segoe UI", 9f, FontStyle.Regular), T.Muted, 104, 462, 840, 22, StringAlignment.Center, StringAlignment.Center);
            }
            else Txt(g, "Turn on \"Measure FPS + DPC before / after\" on the BOOST page and press BOOST.\nThe PC restarts, measures again by itself, and the difference shows here.", Fnt("Segoe UI", 10.5f, FontStyle.Regular), T.Muted, 130, 384, 790, 100, StringAlignment.Center, StringAlignment.Center);
        }
        void DrawStatusPage(Graphics g, double t)
        {
            PageHead(g, "Status", "What is ON / OFF on this PC right now.");
            Btn(g, "stRefresh", "REFRESH", 790, 66, 154, 38, 1, false);
            List<Bu7maidZero.SItem> L = z != null ? z.BStatusItems : null;
            string sum = z != null ? z.BStatusSummary : "";
            CardBox(g, 104, 126, 840, 480);
            if (L == null || L.Count == 0) { Txt(g, sum.Length > 0 ? sum : "checking...", Fnt("Segoe UI", 11f, FontStyle.Regular), T.Muted, 104, 126, 840, 480, StringAlignment.Center, StringAlignment.Center); return; }
            Txt(g, sum, Fnt("Segoe UI Semibold", 11f, FontStyle.Bold), T.Txt, 126, 134, 800, 26, StringAlignment.Near, StringAlignment.Center);
            for (int i = 0; i < L.Count && i < 30; i++)
            {
                Bu7maidZero.SItem it = L[i]; int col = i % 2, row = i / 2;
                float x = 124 + col * 410, y = 168 + row * 29;
                Color dc = it.S == 1 ? T.Good : (it.S == 0 ? T.Bad : T.Muted);
                using (SolidBrush b = new SolidBrush(dc)) g.FillEllipse(b, x, y + 10, 8, 8);
                Txt(g, it.N, Fnt("Segoe UI", 9f, FontStyle.Regular), T.Txt, x + 16, y, 270, 28, StringAlignment.Near, StringAlignment.Center);
                Txt(g, it.V, Fnt("Segoe UI Semibold", 8.5f, FontStyle.Bold), dc, x + 286, y, 100, 28, StringAlignment.Far, StringAlignment.Center);
            }
        }
        void DrawGamesPage(Graphics g, double t)
        {
            PageHead(g, "Games", "Finds your games and tunes each one for maximum FPS and lowest delay.");
            CardBox(g, 104, 140, 840, 330);
            string[] items = { "Finds games from Steam, Epic, Riot, Rockstar, EA, Ubisoft, FiveM, Fortnite, COD...", "Graphics card: high performance for every game", "CPU + disk priority: High while you play", "Game settings files: VSync off, light shadows, no heavy effects", "Fortnite: DirectX 11 instead of DirectX 12 (more FPS)", "Never touches the game folders - only settings and Windows preferences" };
            for (int i = 0; i < items.Length; i++)
            {
                float y = 162 + i * 44;
                using (SolidBrush b = new SolidBrush(i % 2 == 0 ? T.A2 : T.A1)) g.FillEllipse(b, 130, y + 10, 8, 8);
                Txt(g, items[i], Fnt("Segoe UI", 10.5f, FontStyle.Regular), T.Txt, 150, y, 770, 28, StringAlignment.Near, StringAlignment.Center);
            }
            Btn(g, "games", running ? "WORKING..." : "TUNE MY GAMES", 380, 400, 280, 52, 0, false);
            Txt(g, "Close the games first. RESTORE ALL puts every game setting back.", Fnt("Segoe UI", 8.5f, FontStyle.Regular), Alpha(T.Muted, 220), 104, 486, 840, 20, StringAlignment.Center, StringAlignment.Center);
        }
        void DrawOcPage(Graphics g, double t)
        {
            PageHead(g, "Overclock", "Graphics card: AUTO OC finds the highest stable speed and saves it. It lowers itself if a game crashes.");
            CardBox(g, 104, 140, 840, 150);
            Txt(g, "GPU OVERCLOCK", Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, 128, 152, 300, 18, StringAlignment.Near, StringAlignment.Center);
            Txt(g, ocSummary.Length > 0 ? ocSummary : "reading...", Fnt("Segoe UI", 11f, FontStyle.Regular), T.Txt, 128, 176, 790, 50, StringAlignment.Near, StringAlignment.Near);
            Txt(g, "NVIDIA cards only. A red \"don't touch anything\" screen shows while it tests - if the PC restarts by itself it continues alone.", Fnt("Segoe UI", 8.5f, FontStyle.Regular), Alpha(T.Muted, 220), 128, 244, 790, 36, StringAlignment.Near, StringAlignment.Near);
            Btn(g, "ocAuto", "AUTO OC", 104, 312, 270, 56, 0, false);
            Btn(g, "ocTest", "GPU FPS TEST", 392, 312, 270, 56, 1, false);
            Btn(g, "ocDpc", "DPC LATENCY", 680, 312, 264, 56, 1, false);
            Btn(g, "ocReset", "RESET GPU TO STOCK", 104, 384, 270, 48, 2, false);
            CardBox(g, 104, 450, 840, 120);
            Txt(g, "HOW IT WORKS", Fnt("Segoe UI Semibold", 8f, FontStyle.Bold), T.Muted, 128, 460, 300, 18, StringAlignment.Near, StringAlignment.Center);
            Txt(g, "Each step is stress-tested like a heavy game, then a final full test runs. The saved speed is below the maximum on purpose, for stability. RAM and CPU overclock can only be done in the BIOS - Windows cannot do it.", Fnt("Segoe UI", 9.5f, FontStyle.Regular), T.Txt, 128, 482, 790, 80, StringAlignment.Near, StringAlignment.Near);
        }
        void DrawToolsPage(Graphics g, double t)
        {
            PageHead(g, "Tools", "Quick cleaners. Each one shows its progress on the BOOST page.");
            string[] ti = { "CLEAN DISK", "CLEAN RAM", "VIRUS SCAN" };
            float ysh = 148 + 3 * 108;
            CardBox(g, 104, ysh, 840, 92);
            Txt(g, "SHARE WITH FRIENDS", Fnt("Segoe UI Semibold", 13f, FontStyle.Bold), T.Txt, 130, ysh + 14, 500, 30, StringAlignment.Near, StringAlignment.Center);
            Txt(g, "Copies the download link. Your friends get the program and it updates itself.", Fnt("Segoe UI", 9.5f, FontStyle.Regular), T.Muted, 130, ysh + 48, 600, 26, StringAlignment.Near, StringAlignment.Center);
            Btn(g, "toolShare", "COPY LINK", 760, ysh + 22, 160, 48, 1, false);
            Btn(g, "toolTg", "TEST TELEGRAM", 570, ysh + 22, 180, 48, 1, false);
            string[] ts = { "Deep clean: temp, caches, old Windows updates, dumps. Takes a few minutes (BOOST already does a fast one).", "Frees unused memory instantly. Nothing else is touched.", "Quick scan with the built-in Microsoft engine. Removes what it finds." };
            string[] id = { "toolClean", "toolRam", "toolScan" };
            for (int i = 0; i < 3; i++)
            {
                float y = 148 + i * 108;
                CardBox(g, 104, y, 840, 92);
                Txt(g, ti[i], Fnt("Segoe UI Semibold", 13f, FontStyle.Bold), T.Txt, 130, y + 14, 500, 30, StringAlignment.Near, StringAlignment.Center);
                Txt(g, ts[i], Fnt("Segoe UI", 9.5f, FontStyle.Regular), T.Muted, 130, y + 48, 560, 26, StringAlignment.Near, StringAlignment.Center);
                Btn(g, id[i], running ? "WORKING..." : "RUN", 760, y + 22, 160, 48, 1, false);
            }
        }

        // ---------------- your hardware (motherboard, BIOS, CPU, RAM, GPU, drives, network) ----------------
        class SpecRow { public string L, V; public int S; }
        List<SpecRow> specs = null; bool specsBusy = false; int monSub = 0;
        static List<System.Management.ManagementBaseObject> WmiGet(string scope, string q)
        {
            List<System.Management.ManagementBaseObject> l = new List<System.Management.ManagementBaseObject>();
            try
            {
                using (System.Management.ManagementObjectSearcher s = scope == null ? new System.Management.ManagementObjectSearcher(q) : new System.Management.ManagementObjectSearcher(scope, q))
                    foreach (System.Management.ManagementBaseObject o in s.Get()) l.Add(o);
            }
            catch { }
            return l;
        }
        static string Wv(System.Management.ManagementBaseObject o, string p)
        {
            try { object v = o[p]; return v == null ? "" : Convert.ToString(v).Trim(); } catch { return ""; }
        }
        static long Wl(System.Management.ManagementBaseObject o, string p)
        {
            long r; return long.TryParse(Wv(o, p), out r) ? r : 0;
        }
        void ReadSpecsAsync()
        {
            if (specsBusy) return; specsBusy = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<SpecRow> r = new List<SpecRow>();
                try { ReadSpecs(r); } catch { }
                specs = r; specsBusy = false;
            });
        }
        static void AddSpec(List<SpecRow> r, string l, string v, int s) { SpecRow x = new SpecRow(); x.L = l; x.V = v; x.S = s; r.Add(x); }
        static void ReadSpecs(List<SpecRow> r)
        {
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Manufacturer, Product FROM Win32_BaseBoard"))
            { AddSpec(r, "Motherboard", (Wv(o, "Manufacturer") + " " + Wv(o, "Product")).Trim(), 0); break; }
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
            {
                string d = Wv(o, "ReleaseDate"), txt = Wv(o, "SMBIOSBIOSVersion"); int flag = 0;
                if (d.Length >= 8)
                {
                    int y, m, dd;
                    if (int.TryParse(d.Substring(0, 4), out y) && int.TryParse(d.Substring(4, 2), out m) && int.TryParse(d.Substring(6, 2), out dd))
                    {
                        txt += "   (" + y + "-" + m.ToString("00") + "-" + dd.ToString("00") + ")";
                        if (DateTime.Now.Year - y >= 2) { txt += "  -  old BIOS, check the board maker's site for an update"; flag = 1; }
                    }
                }
                AddSpec(r, "BIOS", txt, flag); break;
            }
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor"))
            { AddSpec(r, "CPU", Wv(o, "Name") + "   " + Wv(o, "NumberOfCores") + " cores / " + Wv(o, "NumberOfLogicalProcessors") + " threads   " + Wl(o, "MaxClockSpeed") + " MHz", 0); break; }
            // RAM
            long total = 0; int mods = 0; long spd = 0, conf = 0; string rtype = "";
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
            {
                total += Wl(o, "Capacity"); mods++;
                long s1 = Wl(o, "Speed"), c1 = Wl(o, "ConfiguredClockSpeed");
                if (s1 > spd) spd = s1; if (c1 > conf) conf = c1;
                long mt = Wl(o, "SMBIOSMemoryType"); rtype = mt == 34 ? "DDR5" : (mt == 26 ? "DDR4" : (mt == 24 ? "DDR3" : rtype));
            }
            int slots = 0;
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT MemoryDevices FROM Win32_PhysicalMemoryArray")) { slots += (int)Wl(o, "MemoryDevices"); }
            if (mods > 0)
            {
                long run = conf > 0 ? conf : spd; int rf = 0;
                string rt = (total / 1073741824L) + " GB " + rtype + "   " + mods + " stick" + (mods > 1 ? "s" : "") + "   running at " + run + " MHz";
                if (spd > run + 200) { rt += "  -  kit is rated " + spd + ": turn ON XMP / EXPO in the BIOS"; rf = 1; }
                else if (rtype == "DDR5" && run > 0 && run <= 4800) { rt += "  -  default speed: if your kit is rated higher, turn ON EXPO / XMP in the BIOS"; rf = 1; }
                else if (rtype == "DDR4" && run > 0 && run <= 2666) { rt += "  -  default speed: if your kit is rated higher, turn ON XMP in the BIOS"; rf = 1; }
                AddSpec(r, "RAM", rt, rf);
                if (mods == 1 && slots >= 2) AddSpec(r, "RAM channels", "only 1 stick = single channel (slower). Two matching sticks give more FPS.", 1);
                else if (mods >= 2) AddSpec(r, "RAM channels", "dual channel (good)", 0);
            }
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Name, DriverVersion, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController"))
            {
                string nm = Wv(o, "Name"); if (nm.Length == 0 || nm.IndexOf("Basic", StringComparison.OrdinalIgnoreCase) >= 0 && nm.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                string res = Wl(o, "CurrentHorizontalResolution") > 0 ? "   " + Wl(o, "CurrentHorizontalResolution") + "x" + Wl(o, "CurrentVerticalResolution") + " @ " + Wl(o, "CurrentRefreshRate") + " Hz" : "";
                AddSpec(r, "Graphics", nm + "   driver " + Wv(o, "DriverVersion") + res, 0);
            }
            bool gotDisk = false;
            foreach (System.Management.ManagementBaseObject o in WmiGet(@"root\microsoft\windows\storage", "SELECT FriendlyName, MediaType, BusType, Size FROM MSFT_PhysicalDisk"))
            {
                long mt = Wl(o, "MediaType"), bt = Wl(o, "BusType"); string kind = bt == 17 ? "NVMe SSD" : (mt == 4 ? "SSD" : (mt == 3 ? "HDD" : "disk"));
                AddSpec(r, "Drive", Wv(o, "FriendlyName") + "   " + (Wl(o, "Size") / 1000000000L) + " GB   " + kind, kind == "HDD" ? 1 : 0);
                if (kind == "HDD") r[r.Count - 1].V += "  -  games load much faster from an SSD";
                gotDisk = true;
            }
            if (!gotDisk) foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Model, Size FROM Win32_DiskDrive"))
                AddSpec(r, "Drive", Wv(o, "Model") + "   " + (Wl(o, "Size") / 1000000000L) + " GB", 0);
            string sd = Environment.GetEnvironmentVariable("SystemDrive"); if (string.IsNullOrEmpty(sd)) sd = "C:";
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DeviceID='" + sd + "'"))
            {
                long sz = Wl(o, "Size"), fr = Wl(o, "FreeSpace"); if (sz <= 0) break;
                int pc = (int)(fr * 100 / sz); string t = sd + " has " + (fr / 1073741824L) + " GB free (" + pc + "%)";
                if (pc < 15) t += "  -  low space slows Windows and game installs";
                AddSpec(r, "System drive", t, pc < 15 ? 1 : 0); break;
            }
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Name, Speed FROM Win32_NetworkAdapter WHERE PhysicalAdapter=TRUE AND NetEnabled=TRUE"))
            {
                string nm = Wv(o, "Name"); long sp = Wl(o, "Speed") / 1000000L; bool wifi = nm.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 || nm.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) >= 0 || nm.IndexOf("WLAN", StringComparison.OrdinalIgnoreCase) >= 0;
                string t = nm + "   " + sp + " Mbps"; int fl = 0;
                if (wifi) { t += "  -  Wi-Fi: a cable gives lower ping"; fl = 1; }
                else if (sp > 0 && sp <= 100) { t += "  -  only 100 Mbps: check the cable / port"; fl = 1; }
                AddSpec(r, "Network", t, fl);
            }
            foreach (System.Management.ManagementBaseObject o in WmiGet(null, "SELECT Caption, BuildNumber FROM Win32_OperatingSystem"))
            { AddSpec(r, "Windows", Wv(o, "Caption") + "   build " + Wv(o, "BuildNumber"), 0); break; }
        }
        void DrawSpecs(Graphics g, double t)
        {
            Txt(g, "YOUR PC", Fnt("Segoe UI Semibold", 8.5f, FontStyle.Bold), T.Muted, 104, 74, 300, 20, StringAlignment.Near, StringAlignment.Center);
            CardBox(g, 104, 106, 840, 500);
            List<SpecRow> sp = specs;
            if (sp == null || sp.Count == 0) { Txt(g, specsBusy || sp == null ? "reading your hardware..." : "could not read the hardware info", Fnt("Segoe UI", 11f, FontStyle.Regular), T.Muted, 104, 106, 840, 500, StringAlignment.Center, StringAlignment.Center); return; }
            int tips = 0; foreach (SpecRow s in sp) if (s.S == 1) tips++;
            Txt(g, tips == 0 ? "Everything looks good." : tips + " thing(s) worth checking (orange).", Fnt("Segoe UI Semibold", 10f, FontStyle.Bold), tips == 0 ? T.Good : T.Warn, 126, 114, 600, 26, StringAlignment.Near, StringAlignment.Center);
            for (int i = 0; i < sp.Count && i < 15; i++)
            {
                float y = 146 + i * 30; SpecRow s = sp[i]; Color dc = s.S == 1 ? T.Warn : T.Good;
                using (SolidBrush b = new SolidBrush(dc)) g.FillEllipse(b, 126, y + 11, 8, 8);
                Txt(g, s.L, Fnt("Segoe UI Semibold", 9f, FontStyle.Bold), T.Muted, 146, y, 130, 30, StringAlignment.Near, StringAlignment.Center);
                Txt(g, s.V, Fnt("Segoe UI", 9f, FontStyle.Regular), s.S == 1 ? T.Warn : T.Txt, 280, y, 650, 30, StringAlignment.Near, StringAlignment.Center);
            }
        }
        string SpecsText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(); List<SpecRow> sp = specs;
            if (sp != null) foreach (SpecRow s in sp) sb.AppendLine(s.L + ": " + s.V);
            return sb.ToString();
        }
        void DrawMonitorPage(Graphics g, double t)
        {
            Btn(g, "monLive", "LIVE", 700, 62, 110, 32, monSub == 0 ? 0 : 1, false);
            Btn(g, "monSpecs", "YOUR PC", 824, 62, 120, 32, monSub == 1 ? 0 : 1, false);
            if (monSub == 1) { DrawSpecs(g, t); return; }
            Txt(g, "LIVE SYSTEM", Fnt("Segoe UI Semibold", 8.5f, FontStyle.Bold), T.Muted, 104, 74, 300, 20, StringAlignment.Near, StringAlignment.Center);
            TimeSpan up = TimeSpan.FromMilliseconds((double)(uint)Environment.TickCount);
            Txt(g, "uptime " + (int)up.TotalHours + " h " + up.Minutes + " min", Fnt("Segoe UI", 9f, FontStyle.Regular), T.Muted, 400, 74, 280, 20, StringAlignment.Far, StringAlignment.Center);
            float cw = 270f, gap = 20f, x0 = 104f;
            for (int i = 0; i < 3; i++) CardBox(g, x0 + i * (cw + gap), 100, cw, 220);
            Ring(g, x0 + cw / 2, 205, 66, cpuNow / 100f, T.A2, T.A1, ((int)cpuNow) + "%", "CPU");
            Ring(g, x0 + cw + gap + cw / 2, 205, 66, ramNow / 100f, T.A1, T.A2, ((int)ramNow) + "%", ramUsedGb.ToString("0.0") + " / " + ramTotalGb.ToString("0") + " GB");
            Ring(g, x0 + 2 * (cw + gap) + cw / 2, 205, 66, Math.Min(1f, procCount / 300f), T.Good, T.A2, procCount.ToString(), "processes");
            // cpu history
            CardBox(g, 104, 336, 862 - 22, 220);
            Txt(g, "CPU - last minute", Fnt("Segoe UI Semibold", 8.5f, FontStyle.Bold), T.Muted, 124, 346, 300, 20, StringAlignment.Near, StringAlignment.Center);
            float gx = 124, gy = 374, gw = 820 - 22, gh = 160;
            using (Pen p = new Pen(Alpha(Color.White, 16), 1f)) { for (int i = 0; i <= 4; i++) g.DrawLine(p, gx, gy + gh * i / 4f, gx + gw, gy + gh * i / 4f); }
            PointF[] pts = new PointF[cpuHist.Length];
            for (int i = 0; i < cpuHist.Length; i++) pts[i] = new PointF(gx + gw * i / (cpuHist.Length - 1f), gy + gh - gh * Math.Min(100f, cpuHist[i]) / 100f);
            List<PointF> poly = new List<PointF>(pts);
            poly.Add(new PointF(gx + gw, gy + gh)); poly.Add(new PointF(gx, gy + gh));
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(gx, gy, gw, gh), Alpha(T.A2, 90), Alpha(T.A1, 6), 90f)) g.FillPolygon(b, poly.ToArray());
            using (Pen p = new Pen(T.A2, 2.2f)) { p.LineJoin = LineJoin.Round; g.DrawLines(p, pts); }
        }

        void DrawRestorePage(Graphics g, double t)
        {
            CardBox(g, 200, 120, 640, 360);
            Glyph(g, 2, 520, 190, 22f, T.A2);
            Txt(g, "Restore everything to normal", Fnt("Segoe UI Semibold", 20f, FontStyle.Bold), T.Txt, 220, 228, 600, 40, StringAlignment.Center, StringAlignment.Center);
            Txt(g, "Puts back the original Windows settings, services, startup apps, game\nsettings, browser download folders and colors from the backups\nsaved in C:\\Bu7maid_Backup before BOOST changed them.", Fnt("Segoe UI", 10.5f, FontStyle.Regular), T.Muted, 220, 274, 600, 80, StringAlignment.Center, StringAlignment.Near);
            Btn(g, "restore", "RESTORE TO NORMAL", 390, 380, 260, 48, 2, false);
            Txt(g, "A restart is recommended afterwards.", Fnt("Segoe UI", 8.5f, FontStyle.Regular), Alpha(T.Muted, 220), 220, 440, 600, 20, StringAlignment.Center, StringAlignment.Center);
        }

        // ---------------- input ----------------
        Hit HitAt(float x, float y)
        {
            for (int i = hits.Count - 1; i >= 0; i--) { Hit h = hits[i]; if (h.R.Contains(x, y)) return h; }
            return null;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Hit h = HitAt(e.X / sc, e.Y / sc);
            string id = h == null ? null : h.Id;
            if (running && h != null && h.Id == "dial") id = null;
            hoverId = id;
            Cursor = id != null ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hoverId = null; }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Hit h = HitAt(e.X / sc, e.Y / sc);
            downId = h == null ? null : h.Id;
            if (h == null && e.Y < 52 * sc) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            Hit h = HitAt(e.X / sc, e.Y / sc);
            if (h != null && downId == h.Id) Act(h.Id);
            downId = null;
        }

        void Act(string id)
        {
            switch (id)
            {
                case "min": WindowState = FormWindowState.Minimized; break;
                case "close": Close(); break;
                case "nav0": page = 0; break;
                case "nav1": page = 1; break;
                case "nav2": page = 2; if (z != null) ocSummary = z.BOcSummary(); break;
                case "nav3": page = 3; break;
                case "nav4": page = 4; break;
                case "nav5": page = 5; break;
                case "nav6": page = 6; LoadRes(); break;
                case "nav7": page = 7; if (z != null) z.BStatusRefresh(); break;
                case "stRefresh": if (z != null) z.BStatusRefresh(); break;
                case "toggleMeasure": if (!running) measure = !measure; break;
                case "games": if (!running) { if (z != null) { try { z.BAskCod(); } catch { } } page = 0; Run("GAMES", "TUNING GAMES", 2); } break;
                case "toolClean": if (!running) { page = 0; Run("CLEAN", "CLEANING DISK", 3); } break;
                case "toolRam": if (!running) { page = 0; Run("RAM", "CLEANING RAM", 3); } break;
                case "toolScan":
                    if (!running && MessageBox.Show("Runs a quick virus scan with the built-in Microsoft engine and removes what it finds.\n\nStart?", "Bu 7maid Booster", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { page = 0; Run("SCAN", "SCANNING", 3); }
                    break;
                case "ocAuto": if (!running && z != null) { z.BAutoOc(); ocSummary = z.BOcSummary(); } break;
                case "ocTest": if (!running && z != null) { WindowState = FormWindowState.Minimized; z.BGpuTest(); WindowState = FormWindowState.Normal; Activate(); } break;
                case "ocDpc": if (z != null) z.BDpc(); break;
                case "ocReset": if (!running && z != null) { z.BResetGpu(); ocSummary = z.BOcSummary(); } break;
                case "full": if (!running) mode = 0; break;
                case "safe": if (!running) mode = 1; break;
                case "toggleDel": if (!running) delStartup = !delStartup; break;
                case "dial":
                    if (running) break;
                    if (done) { done = false; break; }
                    StartBoost(); break;
                case "later": done = false; restartAt = DateTime.MinValue; break;
                case "report": ShowReport(); break;
                case "toolTg": Bu7maidZero.Tg.Test(); break;
                case "monLive": monSub = 0; break;
                case "monSpecs": monSub = 1; if (specs == null || specs.Count == 0) ReadSpecsAsync(); break;
                case "toolShare": Bu7maidZero.Updater.Share("Booster"); break;
                case "restartNow": restartAt = DateTime.MinValue; RestartPc(); break;
                case "restore":
                    if (running) break;
                    if (MessageBox.Show("Restore everything to the original Windows settings?", "Bu 7maid Booster", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    { page = 0; Run("RESTORE", "RESTORING", 1); }
                    break;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running)
            {
                MessageBox.Show("A boost is still running. Wait until it finishes, then close.", "Bu 7maid Booster", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true; return;
            }
            base.OnFormClosing(e);
        }

        // ---------------- engine ----------------
        void RestartPc()
        {
            try { Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/r /t 3 /c \"Bu 7maid Booster - restarting\"") { CreateNoWindow = true, UseShellExecute = false }); } catch { }
        }
        string ExtractEngine()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidBooster");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "Bu7maidZero.bat");
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bu7maidZero.bat");
            if (s == null) throw new Exception("engine resource missing - rebuild with BUILD_BOOSTER.bat");
            using (s) using (FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write)) s.CopyTo(f);
            return path;
        }
        void StartBoost()
        {
            if (mode == 0)
            {
                if (MessageBox.Show("FULL BOOST applies every tweak for maximum FPS and turns Windows antivirus OFF.\n\nStart?", "Bu 7maid Booster", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            if (z != null) { try { z.BAskCod(); } catch { } }
            if (measure && z != null)
            {
                WindowState = FormWindowState.Minimized;
                bool okm = true;
                try { okm = z.BMeasureBefore(); } catch { okm = true; }
                WindowState = FormWindowState.Normal; Activate();
                if (!okm) return;
            }
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero");
                Directory.CreateDirectory(dir);
                string flag = Path.Combine(dir, "start_del.flag");
                if (delStartup) File.WriteAllText(flag, "1"); else if (File.Exists(flag)) File.Delete(flag);
            }
            catch { }
            Run(mode == 0 ? "BOOST" : "SAFE", mode == 0 ? "BOOSTING" : "BOOSTING SAFE", 0);
        }
        void Run(string cmd, string title, int kind)
        {
            if (running) return;
            try { if (batPath == null) batPath = ExtractEngine(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Bu 7maid Booster"); return; }
            running = true; done = false; doneKind = kind; runTitle = title; pct = 0f; dispPct = 0f; errs = -1; stepText = "Starting..."; logs.Clear(); errList.Clear(); lock (games) games.Clear(); steps.Clear(); runStart = DateTime.Now;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                psi.Arguments = "/c \"\"" + batPath + "\" " + cmd + " HIDDEN\"";
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = Path.GetDirectoryName(batPath);
                Process pr = Process.Start(psi);
                pr.OutputDataReceived += delegate (object s2, DataReceivedEventArgs ev)
                {
                    if (ev.Data == null) return;
                    string d = ev.Data.Trim();
                    if (d.Length == 0) return;
                    try
                    {
                        if (d.StartsWith("@@PCT=")) { int v; if (int.TryParse(d.Substring(6), out v)) BeginInvoke(new Action(delegate { pct = v; })); }
                        else if (d.StartsWith("@@GAME=")) { lock (games) games.Add(d.Substring(7)); }
                        else if (d.StartsWith("@@ERRS=")) { int v2; if (int.TryParse(d.Substring(7), out v2)) errs = v2; }
                        else if (d.StartsWith("@@ERR=")) { string em = d.Substring(6); BeginInvoke(new Action(delegate { if (errList.Count < 6) errList.Add(em); })); }
                        else if (d.StartsWith("@@MSG=")) { string m = d.Substring(6); BeginInvoke(new Action(delegate { stepText = m; AddLog(m); NewStep(m); })); }
                        else if (d.StartsWith("@@DO=")) { string dm = d.Substring(5); BeginInvoke(new Action(delegate { if (steps.Count > 0 && steps[steps.Count - 1].Do.Count < 8) steps[steps.Count - 1].Do.Add(dm); })); }
                        else if (!d.StartsWith("@@") && d.Length < 200) { string m2 = d; BeginInvoke(new Action(delegate { AddLog(m2); })); }
                    }
                    catch { }
                };
                pr.EnableRaisingEvents = true;
                pr.BeginOutputReadLine();
                pr.Exited += delegate
                {
                    try
                    {
                        BeginInvoke(new Action(delegate
                        {
                            pct = 100f;
                            System.Windows.Forms.Timer t2 = new System.Windows.Forms.Timer();
                            t2.Interval = 700;
                            t2.Tick += delegate { t2.Stop(); t2.Dispose(); running = false; done = true; stepText = "";
                                if (steps.Count > 0) { StepRec ls = steps[steps.Count - 1]; ls.Done = true; ls.Secs = (DateTime.Now - ls.Start).TotalSeconds; }
                                lastSteps = new List<StepRec>(steps); lastSecs = (DateTime.Now - runStart).TotalSeconds; lastErrCount = errs > 0 ? errs : 0; lastErrs = new List<string>(errList); lastName = runTitle; try { Bu7maidZero.Tg.Send("[" + runTitle + "] finished in " + ((int)lastSecs / 60) + ":" + ((int)lastSecs % 60).ToString("00") + (errs > 0 ? " with " + errs + " small problem(s)" : " - no problems") + "\nSteps: " + steps.Count); } catch { }
                                lock (games) lastGames = new List<string>(games);
                                if (doneKind != 1) Bu7maidZero.GpuOc.ReapplyAfterTask(); LoadRes(); if (doneKind == 0) ShowReport(); else ShowGames(); if (doneKind == 0 && z != null && z.BAfterPending) { try { z.BScheduleAfter(); } catch { } }
                                if (doneKind == 0 && measure) restartAt = DateTime.Now.AddSeconds(15); };
                            t2.Start();
                        }));
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                running = false; MessageBox.Show("Could not start: " + ex.Message, "Bu 7maid Booster");
            }
        }
        readonly List<string> games = new List<string>();
        public static bool CopyText(string s)
        {
            try { Clipboard.SetDataObject(s, true, 10, 150); return true; } catch { return false; }
        }
        string MakeReportText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (lastSteps != null && lastSteps.Count > 0)
            {
                sb.AppendLine("Bu 7maid Booster " + Version + " - " + lastName + "  (" + ((int)lastSecs / 60) + ":" + ((int)lastSecs % 60).ToString("00") + ")");
                foreach (StepRec s in lastSteps)
                {
                    sb.AppendLine("- " + s.T + " (" + s.Secs.ToString("0.0") + " s)");
                    foreach (string d in s.Do) sb.AppendLine("    * " + d);
                }
                if (lastGames != null && lastGames.Count > 0) { sb.AppendLine("GAMES"); foreach (string gm in lastGames) sb.AppendLine("  " + gm.Replace("|", "  |  ")); }
                if (lastErrs != null && lastErrs.Count > 0) { sb.AppendLine("PROBLEMS"); foreach (string em in lastErrs) sb.AppendLine("  " + em); }
            }
            else foreach (string l in logs) sb.AppendLine(l);
            return sb.ToString();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C)) { string t = (page == 4 && monSub == 1) ? SpecsText() : MakeReportText(); if (t.Length > 0) { CopyText(t); return true; } }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        void ShowReport()
        {
            if (lastSteps == null || lastSteps.Count == 0) { ShowGames(); return; }
            lock (games) games.Clear();
            try
            {
                List<string[]> rows = new List<string[]>();
                foreach (StepRec s in lastSteps) { List<string> d = new List<string>(s.Do); rows.Add(new string[] { s.T, string.Join("\n", d.ToArray()), s.Secs.ToString("0.0") }); }
                using (ReportForm rf = new ReportForm(lastName, rows, lastGames ?? new List<string>(), lastErrs ?? new List<string>(), lastErrCount, lastSecs, MakeReportText())) rf.ShowDialog(this);
            }
            catch { }
        }
        void ShowGames()
        {
            List<string> gl; lock (games) { gl = new List<string>(games); games.Clear(); }
            if (gl.Count == 0 || (doneKind != 0 && doneKind != 2)) return;
            try { using (Bu7maidZero.GamesListForm gf = new Bu7maidZero.GamesListForm(gl)) gf.ShowDialog(this); } catch { }
        }
        void AddLog(string s)
        {
            logs.Add(s);
            if (logs.Count > 40) logs.RemoveAt(0);
        }
    }

    class ReportForm : Form
    {
        readonly string title; readonly List<string[]> rows; readonly List<string> games, errs; readonly int errCount; readonly double secs;
        int scroll = 0, contentH = 0; string copyText = ""; Rectangle closeR, copyR; Point dragFrom; bool drag;
        int changes = 0, tuned = 0; DateTime copied = DateTime.MinValue;
        public ReportForm(string ttl, List<string[]> r, List<string> g, List<string> e, int ec, double s, string txt)
        {
            copyText = txt;
            title = ttl; rows = r; games = g; errs = e; errCount = ec; secs = s;
            foreach (string[] x in rows) if (x[1].Length > 0) changes += x[1].Split('\n').Length;
            foreach (string gm in games) { string[] q = gm.Split('|'); if (q.Length > 1 && q[1] == "tuned") tuned++; }
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            ClientSize = new Size(760, 640); BackColor = T.Bg0; DoubleBuffered = true; Text = "Bu 7maid Booster - Report";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            try { using (GraphicsPath gp = RR(new RectangleF(0, 0, 760, 640), 16f)) Region = new Region(gp); } catch { }
            MouseWheel += delegate (object o, MouseEventArgs ev) { scroll -= ev.Delta / 2; Clamp(); Invalidate(); };
            MouseDown += delegate (object o, MouseEventArgs ev) { if (closeR.Contains(ev.Location)) { Close(); return; } if (copyR.Contains(ev.Location)) { MainForm.CopyText(copyText); copied = DateTime.Now; Invalidate(); return; } if (ev.Y < 110) { drag = true; dragFrom = ev.Location; } };
            MouseMove += delegate (object o, MouseEventArgs ev) { if (drag) Location = new Point(Location.X + ev.X - dragFrom.X, Location.Y + ev.Y - dragFrom.Y); Cursor = (closeR.Contains(ev.Location) || copyR.Contains(ev.Location)) ? Cursors.Hand : Cursors.Default; };
            MouseUp += delegate { drag = false; };
            KeyDown += delegate (object o, KeyEventArgs ev) { if (ev.Control && ev.KeyCode == Keys.C) { MainForm.CopyText(copyText); copied = DateTime.Now; Invalidate(); } else if (ev.KeyCode == Keys.Escape || ev.KeyCode == Keys.Enter) Close(); else if (ev.KeyCode == Keys.Down) { scroll += 40; Clamp(); Invalidate(); } else if (ev.KeyCode == Keys.Up) { scroll -= 40; Clamp(); Invalidate(); } };
            KeyPreview = true;
        }
        static GraphicsPath RR(RectangleF r, float d)
        {
            GraphicsPath p = new GraphicsPath(); float k = d * 2;
            p.AddArc(r.X, r.Y, k, k, 180, 90); p.AddArc(r.Right - k, r.Y, k, k, 270, 90); p.AddArc(r.Right - k, r.Bottom - k, k, k, 0, 90); p.AddArc(r.X, r.Bottom - k, k, k, 90, 90); p.CloseFigure(); return p;
        }
        void Clamp() { int max = Math.Max(0, contentH - (640 - 110 - 70)); if (scroll > max) scroll = max; if (scroll < 0) scroll = 0; }
        void T2(Graphics g, string s, Font f, Color c, float x, float y, float w, float h, StringAlignment ha)
        {
            using (StringFormat sf = new StringFormat()) using (SolidBrush b = new SolidBrush(c))
            { sf.Alignment = ha; sf.LineAlignment = StringAlignment.Center; sf.Trimming = StringTrimming.EllipsisCharacter; g.DrawString(s, f, b, new RectangleF(x, y, w, h), sf); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(T.Bg0);
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, 760, 110), Color.FromArgb(40, 30, 110), T.Bg0, 90f)) g.FillRectangle(b, 0, 0, 760, 110);
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, 760, 4), T.A1, T.A2, 0f)) g.FillRectangle(b, 0, 0, 760, 4);
            using (Font f1 = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold)) T2(g, "BU 7MAID BOOSTER", f1, T.A2, 30, 16, 300, 18, StringAlignment.Near);
            using (Font f2 = new Font("Segoe UI", 20f, FontStyle.Bold)) T2(g, "Here is what I did on your PC", f2, T.Txt, 30, 36, 600, 40, StringAlignment.Near);
            string sub = errCount > 0 ? errCount + " small problem(s) - everything else was applied" : "Everything was applied with no problems";
            using (Font f3 = new Font("Segoe UI", 9.5f)) T2(g, sub, f3, errCount > 0 ? T.Warn : T.Good, 30, 78, 600, 22, StringAlignment.Near);
            // chips
            string[] cl = { "STEPS", "CHANGES", "GAMES", "TIME" };
            string[] cv = { rows.Count.ToString(), changes.ToString(), tuned.ToString(), ((int)secs / 60) + ":" + ((int)secs % 60).ToString("00") };
            for (int i = 0; i < 4; i++)
            {
                float x = 456 + i * 74f;
                using (GraphicsPath gp = RR(new RectangleF(x, 20, 68, 62), 10f)) { using (SolidBrush b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillPath(b, gp); using (Pen p = new Pen(T.Line, 1f)) g.DrawPath(p, gp); }
                using (Font fa = new Font("Segoe UI", 15f, FontStyle.Bold)) T2(g, cv[i], fa, T.Txt, x, 26, 68, 30, StringAlignment.Center);
                using (Font fb = new Font("Segoe UI Semibold", 7f, FontStyle.Bold)) T2(g, cl[i], fb, T.Muted, x, 56, 68, 16, StringAlignment.Center);
            }
            // list
            g.SetClip(new Rectangle(0, 110, 760, 640 - 110 - 70));
            int y = 122 - scroll;
            using (Font ft = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold)) using (Font fd = new Font("Segoe UI", 9f)) using (Font fs = new Font("Segoe UI", 8f))
            {
                foreach (string[] r in rows)
                {
                    string[] d = r[1].Length > 0 ? r[1].Split('\n') : new string[0];
                    int h = 40 + d.Length * 22 + (d.Length > 0 ? 6 : 0);
                    using (GraphicsPath gp = RR(new RectangleF(24, y, 712, h - 6), 10f)) { using (SolidBrush b = new SolidBrush(T.Card)) g.FillPath(b, gp); using (Pen p = new Pen(T.Line, 1f)) g.DrawPath(p, gp); }
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(50, T.Good))) g.FillEllipse(b, 40, y + 11, 18, 18);
                    using (Pen p = new Pen(T.Good, 2f)) g.DrawLines(p, new PointF[] { new PointF(44, y + 20), new PointF(48, y + 24), new PointF(54, y + 16) });
                    T2(g, r[0], ft, T.Txt, 68, y + 8, 590, 24, StringAlignment.Near);
                    T2(g, r[2] + " s", fs, T.Muted, 660, y + 8, 64, 24, StringAlignment.Far);
                    for (int i = 0; i < d.Length; i++)
                    {
                        using (SolidBrush b = new SolidBrush(i % 2 == 0 ? T.A2 : T.A1)) g.FillEllipse(b, 72, y + 44 + i * 22 + 7, 6, 6);
                        T2(g, d[i], fd, Color.FromArgb(205, 210, 235), 86, y + 40 + i * 22, 630, 22, StringAlignment.Near);
                    }
                    y += h;
                }
                if (games.Count > 0)
                {
                    T2(g, "GAMES", ft, T.A2, 30, y + 4, 300, 24, StringAlignment.Near); y += 32;
                    foreach (string gm in games)
                    {
                        string[] q = gm.Split('|'); string nm = q.Length > 0 ? q[0] : gm, st = q.Length > 1 ? q[1] : "", dt = q.Length > 2 ? q[2] : "";
                        Color sc2 = st == "tuned" ? T.Good : (st == "skipped" ? T.Warn : T.Muted);
                        using (GraphicsPath gp = RR(new RectangleF(24, y, 712, 50), 10f)) { using (SolidBrush b = new SolidBrush(T.Card)) g.FillPath(b, gp); using (Pen p = new Pen(T.Line, 1f)) g.DrawPath(p, gp); }
                        using (SolidBrush b = new SolidBrush(sc2)) g.FillEllipse(b, 42, y + 21, 8, 8);
                        T2(g, nm, ft, T.Txt, 62, y + 4, 480, 24, StringAlignment.Near);
                        T2(g, st == "tuned" ? "TUNED" : (st == "skipped" ? "SKIPPED" : "NO FILE"), fs, sc2, 620, y + 4, 104, 24, StringAlignment.Far);
                        T2(g, dt, fd, T.Muted, 62, y + 26, 660, 20, StringAlignment.Near);
                        y += 56;
                    }
                }
                if (errs.Count > 0)
                {
                    T2(g, "PROBLEMS", ft, T.Warn, 30, y + 4, 300, 24, StringAlignment.Near); y += 32;
                    foreach (string em in errs) { T2(g, "- " + em, fd, T.Warn, 36, y, 690, 22, StringAlignment.Near); y += 22; }
                }
                y += 10;
            }
            contentH = y + scroll - 122; g.ResetClip();
            if (contentH > 640 - 110 - 70 && true)
            {
                float frac = (640f - 180f) / contentH; float th = Math.Max(30f, 460f * frac); float ty = 116f + (460f - th) * (scroll / (float)Math.Max(1, contentH - 460));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 255, 255, 255))) g.FillRectangle(b, 750, ty, 4, th);
            }
            // footer
            using (SolidBrush b = new SolidBrush(Color.FromArgb(230, 10, 12, 26))) g.FillRectangle(b, 0, 570, 760, 70);
            copyR = new Rectangle(230, 584, 160, 42); closeR = new Rectangle(400, 584, 160, 42);
            using (GraphicsPath gp = RR(copyR, 21f)) { using (SolidBrush b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillPath(b, gp); using (Pen p = new Pen(T.Line, 1.2f)) g.DrawPath(p, gp); }
            using (Font fc = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)) T2(g, (DateTime.Now - copied).TotalSeconds < 2 ? "COPIED" : "COPY", fc, T.Txt, copyR.X, copyR.Y, copyR.Width, copyR.Height, StringAlignment.Center);
            using (GraphicsPath gp = RR(closeR, 21f)) using (LinearGradientBrush b = new LinearGradientBrush(closeR, T.A1, T.A2, 0f)) g.FillPath(b, gp);
            using (Font fc = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)) T2(g, "DONE", fc, Color.White, closeR.X, closeR.Y, closeR.Width, closeR.Height, StringAlignment.Center);
        }
    }
}
