// Bu 7maid Zero - gaming optimizer UI. STATUS + BIOS read natively (no console).
// BOOST / BOOST SAFE / RESTORE run the embedded engine in a visible window (they restart the PC).
// CLEAN / RAM / SCAN run the engine hidden with an in-app progress overlay.
// Builds with the .NET Framework 4.x compiler shipped in Windows (C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Bu7maidZero
{
    static class Program
    {
        public static bool ResumeOc;
        // one stress test in its own process: if the driver crashes it, only this process dies and the main window survives
        static int OcChild(string[] a)
        {
            string errFile = BenchRes.PathFor("oc_child_err"), startFile = BenchRes.PathFor("oc_child_start");
            try
            {
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                using (GlBenchForm f = new GlBenchForm())
                {
                    f.Stress = a[1] == "1"; f.Warm = double.Parse(a[2], ci); f.Dur = double.Parse(a[3], ci); f.Title = a[4];
                    f.OnStarted = delegate { try { File.WriteAllText(startFile, "1"); } catch { } };
                    BenchRes r = f.Execute();
                    if (r != null) { r.Save("oc_child"); return 0; }
                    File.WriteAllText(errFile, f.Error.Length > 0 ? f.Error : "Test cancelled.");
                }
            }
            catch (Exception ex) { try { File.WriteAllText(errFile, "GPU test error: " + ex.Message); } catch { } }
            return 2;
        }
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 5 && string.Equals(args[0], "/octest", StringComparison.OrdinalIgnoreCase)) { Environment.ExitCode = OcChild(args); return; }
            foreach (string a0 in args) if (string.Equals(a0, "/ocapply", StringComparison.OrdinalIgnoreCase)) { GpuOc.ApplyAtLogon(); return; }
            foreach (string a1 in args) if (string.Equals(a1, "/ocresume", StringComparison.OrdinalIgnoreCase)) ResumeOc = true;
            bool after = false; foreach (string a in args) if (string.Equals(a, "/after", StringComparison.OrdinalIgnoreCase)) after = true;
            if (after)
            {
                string pend = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero", "autotest.pending");
                if (!File.Exists(pend)) return;
                try { File.Delete(pend); } catch { }
                try { ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/delete /tn \"Bu7maidZero After Test\" /f"); q.CreateNoWindow = true; q.UseShellExecute = false; Process.Start(q); } catch { }
                try { Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true).DeleteValue("Bu7maidAfter", false); } catch { }
                GpuOc.ApplyQuiet();
                Application.Run(ResultForm.ForAfterFlow());
                return;
            }
            Application.Run(new MainForm());
        }
    }

    internal sealed class GamesListForm : Form
    {
        public static readonly List<string> Pending = new List<string>();
        readonly List<string[]> rows = new List<string[]>();
        Panel host;
        public GamesListForm(List<string> items)
        {
            foreach (string s in items) { string[] p = s.Split(new char[] { '|' }, 3); if (p.Length == 3) rows.Add(p); }
            Text = "Your games"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen; BackColor = UI.Bg; ForeColor = UI.Txt; Font = new Font("Segoe UI", 9f);
            int h = 150 + rows.Count * 62; if (h > 640) h = 640; if (h < 260) h = 260;
            ClientSize = new Size(560, h);
            int tuned = 0; foreach (string[] r in rows) if (r[1] != "skipped") tuned++;
            Label t = new Label(); t.Text = rows.Count == 0 ? "No games found" : "Games found on your PC: " + rows.Count; t.Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold); t.ForeColor = UI.Txt; t.SetBounds(22, 14, 520, 32); Controls.Add(t);
            Label s2 = new Label(); s2.Text = rows.Count == 0 ? "Install a supported game and run again." : "Tuned: " + tuned + "   -   settings files edited, folders untouched. RESTORE puts everything back."; s2.ForeColor = UI.Muted; s2.SetBounds(24, 48, 520, 22); Controls.Add(s2);
            host = new Panel(); host.SetBounds(12, 80, 536, h - 80 - 64); host.AutoScroll = true; host.BackColor = UI.Bg; Controls.Add(host);
            int y = 0;
            foreach (string[] r in rows)
            {
                Panel card = new Panel(); card.SetBounds(0, y, 516, 56); card.BackColor = UI.Card; host.Controls.Add(card);
                Color cc = r[1] == "tuned" ? UI.Good : (r[1] == "nofile" ? UI.Warn : UI.Muted);
                string chip = r[1] == "tuned" ? "TUNED" : (r[1] == "nofile" ? "TUNED (GPU/CPU)" : "SKIPPED");
                Label n = new Label(); n.Text = r[0]; n.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold); n.ForeColor = UI.Txt; n.BackColor = UI.Card; n.SetBounds(14, 6, 330, 20); card.Controls.Add(n);
                Label c2 = new Label(); c2.Text = chip; c2.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold); c2.ForeColor = cc; c2.BackColor = UI.Card; c2.TextAlign = ContentAlignment.MiddleRight; c2.SetBounds(350, 6, 156, 20); card.Controls.Add(c2);
                Label d = new Label(); d.Text = r[2]; d.ForeColor = UI.Muted; d.BackColor = UI.Card; d.AutoEllipsis = true; d.SetBounds(14, 29, 490, 20); card.Controls.Add(d);
                y += 62;
            }
            Button ok = new Button(); ok.Text = "OK"; ok.FlatStyle = FlatStyle.Flat; ok.FlatAppearance.BorderSize = 0; ok.BackColor = UI.Accent; ok.ForeColor = Color.White; ok.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            ok.SetBounds(200, h - 52, 160, 38); ok.DialogResult = DialogResult.OK; Controls.Add(ok); AcceptButton = ok;
        }
    }

    // ---- old-version cleanup + self-update (shared by Zero and Booster) ----
    internal static class Updater
    {
        // Put the direct link of your version.txt here (empty = auto-update off, old-version cleanup still works)
        public const string ZeroUrl = "https://raw.githubusercontent.com/vatabdulla2-ctrl/bu7maid/main/zero.txt";
        public const string BoosterUrl = "https://raw.githubusercontent.com/vatabdulla2-ctrl/bu7maid/main/booster.txt";

        static int[] Parse(string v)
        {
            List<int> r = new List<int>(); string cur = "";
            foreach (char ch in (v ?? "") + ".") { if (ch >= '0' && ch <= '9') cur += ch; else if (cur.Length > 0) { int n; if (int.TryParse(cur, out n)) r.Add(n); cur = ""; } }
            return r.ToArray();
        }
        static int Cmp(int[] a, int[] b)
        {
            int n = Math.Max(a.Length, b.Length);
            for (int i = 0; i < n; i++) { int x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0; if (x != y) return x < y ? -1 : 1; }
            return 0;
        }
        public static void Start(string product, string curVer, Control host, Func<bool> busy)
        {
            Thread t = new Thread(delegate () { try { CleanOld(product, curVer); CheckUpdate(product, curVer, host, busy); } catch { } });
            t.IsBackground = true; t.Start();
        }
        // copies the download link of the newest version, to send to friends
        public static void Share(string product)
        {
            string url = product == "Zero" ? ZeroUrl : BoosterUrl;
            if (string.IsNullOrEmpty(url)) return;
            string link = url.Replace("raw.githubusercontent.com", "github.com").Replace("/main/zero.txt", "/raw/main/Zero.exe").Replace("/main/booster.txt", "/raw/main/Booster.exe");
            bool ok = false; try { Clipboard.SetDataObject(link, true, 10, 150); ok = true; } catch { }
            MessageBox.Show((ok ? "Download link copied:\n\n" : "Could not reach the clipboard. Press Ctrl+C now to copy this message:\n\n") + link + "\n\nSend it to your friends. They download the program once, then it updates itself.", "Bu 7maid " + product, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // deletes older Bu 7maid <product> vX.exe and "- BUILD.zip" files (this folder, Desktop, Downloads). Never the running file, never a newer one.
        static void CleanOld(string product, string curVer)
        {
            string self = ""; try { self = Application.ExecutablePath; } catch { }
            List<string> dirs = new List<string>();
            string selfDir = ""; try { selfDir = Path.GetDirectoryName(self); dirs.Add(selfDir); dirs.Add(Path.GetDirectoryName(selfDir)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("^Bu 7maid " + product + " v([0-9][0-9.]*)( \\(\\d+\\))?( - BUILD)?\\.(exe|zip)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex rxd = new System.Text.RegularExpressions.Regex("^Bu 7maid " + product + " v([0-9][0-9.]*)( \\(\\d+\\))?( - BUILD)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int[] cur = Parse(curVer);
            foreach (string d in dirs.ToArray())
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                try { string bf = Path.Combine(d, "Bu 7maid " + product + ".bat"); if (File.Exists(bf)) { File.SetAttributes(bf, FileAttributes.Normal); File.Delete(bf); } } catch { }   // old build helper - not needed any more
                string[] subs; try { subs = Directory.GetDirectories(d, "Bu 7maid " + product + " v*"); } catch { subs = new string[0]; }
                foreach (string sd in subs)
                {
                    try
                    {
                        System.Text.RegularExpressions.Match dm = rxd.Match(Path.GetFileName(sd));
                        if (!dm.Success) continue;
                        if (Cmp(Parse(dm.Groups[1].Value), cur) >= 0) continue;
                        string full = Path.GetFullPath(sd).TrimEnd('\\') + "\\";
                        if (self.StartsWith(full, StringComparison.OrdinalIgnoreCase)) continue;   // never the folder we run from
                        Directory.Delete(sd, true);
                    }
                    catch { }
                }
                string[] files; try { files = Directory.GetFiles(d, "Bu 7maid " + product + " v*"); } catch { continue; }
                foreach (string f in files)
                {
                    try
                    {
                        if (string.Equals(f, self, StringComparison.OrdinalIgnoreCase)) continue;
                        System.Text.RegularExpressions.Match m = rx.Match(Path.GetFileName(f));
                        if (!m.Success) continue;
                        if (Cmp(Parse(m.Groups[1].Value), cur) >= 0) continue;
                        File.SetAttributes(f, FileAttributes.Normal); File.Delete(f);
                    }
                    catch { }
                }
            }
        }
        // version.txt = 3 lines: version (v16.2) / direct link to the exe / SHA-256 of the exe
        static void CheckUpdate(string product, string curVer, Control host, Func<bool> busy)
        {
            string url = product == "Zero" ? ZeroUrl : BoosterUrl;
            if (string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
            try { System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072; } catch { }
            string txt;
            using (System.Net.WebClient w = new System.Net.WebClient()) { w.Headers[System.Net.HttpRequestHeader.CacheControl] = "no-cache"; txt = w.DownloadString(url); }
            string[] ln = txt.Replace("\r", "").Split('\n');
            if (ln.Length < 2) return;
            string ver = ln[0].Trim(), link = ln[1].Trim(), hash = ln.Length > 2 ? ln[2].Trim().Replace(" ", "").ToLowerInvariant() : "";
            if (Cmp(Parse(ver), Parse(curVer)) <= 0) return;
            if (!link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
            bool checkHash = hash.Length == 64;
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            string self = Application.ExecutablePath; string target = self + ".new";
            string tmp = Path.Combine(Path.GetTempPath(), "bu7maid_update_" + Guid.NewGuid().ToString("N") + ".exe");
            using (System.Net.WebClient w = new System.Net.WebClient()) w.DownloadFile(link, tmp);
            string got;
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            using (FileStream fs = File.OpenRead(tmp)) got = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            if (checkHash && got != hash) { try { File.Delete(tmp); } catch { } Tg.Send("[UPDATE] " + ver + " download rejected (wrong SHA-256)"); return; }   // wrong file - never run it
            if (new FileInfo(tmp).Length < 200000) { try { File.Delete(tmp); } catch { } return; }   // not a real program
            File.Copy(tmp, target, true); try { File.Delete(tmp); } catch { }   // new version waits next to the running file
            // wait until the program is idle, then restart into the new version
            for (int i = 0; i < 720; i++)
            {
                bool b = true;
                try { host.Invoke(new Action(delegate { b = busy(); })); } catch { return; }
                if (!b) break;
                Thread.Sleep(5000);
            }
            try
            {
                host.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        // same file is replaced by the new version, then started again
                        string bat = Path.Combine(Path.GetTempPath(), "bu7maid_swap_" + Guid.NewGuid().ToString("N") + ".bat");
                        string newSelf = Path.Combine(dir, "Bu 7maid " + product + " " + ver + ".exe");
                        Tg.Send("[UPDATE] " + curVer + " -> " + ver + " installed, restarting");
                        Thread.Sleep(1500);
                        File.WriteAllText(bat, "@echo off\r\nfor /l %%i in (1,1,20) do (\r\n  move /y \"" + target + "\" \"" + newSelf + "\" >nul 2>&1 && goto run\r\n  ping -n 2 127.0.0.1 >nul\r\n)\r\n:run\r\nif /i not \"" + newSelf + "\"==\"" + self + "\" del /f /q \"" + self + "\" >nul 2>&1\r\nstart \"\" \"" + newSelf + "\"\r\ndel \"%~f0\"\r\n", System.Text.Encoding.Default);
                        ProcessStartInfo p = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/c \"" + bat + "\"");
                        p.CreateNoWindow = true; p.UseShellExecute = false; Process.Start(p);
                        Application.Exit();
                    }
                    catch { }
                }));
            }
            catch { }
        }
    }

    internal static class MemTrim
    {
        [DllImport("kernel32.dll")] static extern bool SetProcessWorkingSetSize(IntPtr h, IntPtr min, IntPtr max);
        static System.Windows.Forms.Timer t;
        public static void Start()
        {
            if (t != null) return;
            t = new System.Windows.Forms.Timer(); t.Interval = 20000;
            t.Tick += delegate { try { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1)); } catch { } };
            t.Start();
        }
    }

    static class UI
    {
        public static readonly Color Bg = Color.FromArgb(8, 9, 20);
        public static readonly Color Side = Color.FromArgb(12, 13, 28);
        public static readonly Color Card = Color.FromArgb(19, 21, 42);
        public static readonly Color Card2 = Color.FromArgb(30, 33, 62);
        public static readonly Color Line = Color.FromArgb(48, 52, 92);
        public static readonly Color Accent = Color.FromArgb(124, 92, 255);
        public static readonly Color Accent2 = Color.FromArgb(0, 200, 255);
        public static readonly Color Txt = Color.FromArgb(236, 237, 240);
        public static readonly Color Muted = Color.FromArgb(138, 144, 156);
        public static readonly Color Good = Color.FromArgb(46, 214, 120);
        public static readonly Color Warn = Color.FromArgb(255, 186, 52);
        public static readonly Color Bad = Color.FromArgb(255, 92, 92);
        public static float Phase; public static bool Strong;

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Max(1f, Math.Min(rad * 2f, Math.Min(r.Width, r.Height)));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color Mix(Color a, Color b, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Glow()
        {
            float t = (float)(0.5 + 0.5 * Math.Sin(Phase * 2 * Math.PI));
            return Mix(Accent, Accent2, t * 0.6f);
        }

        public static float Dpi(Graphics g) { return g.DpiX / 96f; }

        public static void Drift(Graphics g, Rectangle area)
        {
            SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < 26; i++)
            {
                float t = (Phase * (1.5f + (i % 4) * 0.5f) + i * 0.137f) % 1f;
                float x = area.X + area.Width * (((i * 0.6180339f) % 1f) + 0.04f * (float)Math.Sin((t + i) * 6.28));
                float y = area.Bottom - area.Height * t;
                float r = 1.2f + (i % 3);
                int a = (int)(70 * Math.Sin(t * Math.PI));
                Color c = i % 3 == 0 ? Accent2 : Accent;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(Math.Max(0, a), c))) g.FillEllipse(b, x, y, r * 2, r * 2);
            }
            g.SmoothingMode = old;
        }

        public static void Blobs(Graphics g, Rectangle area, int seed)
        {
            SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < 3; i++)
            {
                double ph = Phase * 2 * Math.PI + i * 2.1 + seed;
                float cx = area.X + area.Width * (0.5f + 0.42f * (float)Math.Sin(ph * 0.7 + i));
                float cy = area.Y + area.Height * (0.5f + 0.45f * (float)Math.Cos(ph * 0.5 + i * 1.7));
                float rad = Math.Min(area.Width, area.Height) * (0.42f + 0.1f * (float)Math.Sin(ph));
                Color c = i == 1 ? Accent2 : Accent;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(cx - rad, cy - rad, rad * 2, rad * 2);
                    using (PathGradientBrush pg = new PathGradientBrush(gp))
                    {
                        pg.CenterColor = Color.FromArgb(Strong ? 44 : 26, c);
                        pg.SurroundColors = new Color[] { Color.FromArgb(0, c) };
                        g.FillEllipse(pg, cx - rad, cy - rad, rad * 2, rad * 2);
                    }
                }
            }
            g.SmoothingMode = old;
        }
    }

    class DB : Panel
    {
        public DB() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); }
    }

    class ZBtn : Control
    {
        public string Title = "", Sub = "";
        public int Kind; // 0 card, 1 primary, 2 nav, 3 danger
        public bool Selected;
        bool hv, dn;
        public ZBtn()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent; Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hv = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hv = dn = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { dn = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = UI.Dpi(g);
            RectangleF r = new RectangleF(1, 1, Width - 3, Height - 3);
            bool on = Enabled;
            using (GraphicsPath p = UI.Round(r, (Kind == 2 ? 8 : 12) * s))
            {
                if (Kind == 2)
                {
                    if (Selected || (hv && on)) using (SolidBrush b = new SolidBrush(Selected ? UI.Card2 : UI.Card)) g.FillPath(b, p);
                    if (Selected) using (SolidBrush ab = new SolidBrush(UI.Glow())) g.FillRectangle(ab, 1, Height * 0.25f, 3 * s, Height * 0.5f);
                }
                else if (Kind == 1)
                {
                    using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), on ? UI.Accent : UI.Card2, on ? UI.Glow() : UI.Card2, LinearGradientMode.ForwardDiagonal)) g.FillPath(br, p);
                    if (on && hv) using (SolidBrush hb = new SolidBrush(Color.FromArgb(dn ? 55 : 26, 255, 255, 255))) g.FillPath(hb, p);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(on && hv ? UI.Card2 : UI.Card)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Kind == 3 ? UI.Bad : (on && hv ? UI.Glow() : UI.Line), 1.3f * s)) g.DrawPath(pen, p);
                }
            }
            Color tc = !on ? UI.Muted : Kind == 1 ? Color.White : Kind == 3 ? UI.Bad : Kind == 2 ? (Selected ? UI.Txt : UI.Muted) : UI.Txt;
            int pad = (int)((Kind == 2 ? 20 : 18) * s);
            TextFormatFlags fl = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            using (Font f1 = new Font("Segoe UI Semibold", Kind == 1 ? 17f : Kind == 2 ? 10.5f : 11.5f))
            using (Font f2 = new Font("Segoe UI", Kind == 1 ? 10f : 8.75f))
            {
                int h1 = f1.Height, h2 = Sub.Length > 0 ? f2.Height : 0, top = (Height - h1 - h2) / 2;
                TextRenderer.DrawText(g, Title, f1, new Rectangle(pad, top, Width - pad - 8, h1), tc, fl);
                if (h2 > 0) TextRenderer.DrawText(g, Sub, f2, new Rectangle(pad, top + h1, Width - pad - 8, h2), Kind == 1 ? Color.FromArgb(255, 222, 228) : UI.Muted, fl);
            }
        }
    }

    class ZBar : Control
    {
        public int Value = -1; // -1 = indeterminate
        public ZBar() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = UI.Round(new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f)) using (SolidBrush b = new SolidBrush(UI.Card2)) g.FillPath(b, p);
            if (Value < 0)
            {
                float w = Width * 0.3f, x = (UI.Phase * 2f % 1f) * (Width + w) - w;
                float xx = Math.Max(0, x), ww = Math.Min(w, Width - xx); if (ww < 2) return;
                using (GraphicsPath p2 = UI.Round(new RectangleF(xx, 0, ww, Height - 1), Height / 2f)) using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), UI.Accent, UI.Glow(), 0f)) g.FillPath(br, p2);
                return;
            }
            float fw = (Width - 1) * Math.Min(100, Value) / 100f; if (fw < Height) fw = Value <= 0 ? 0 : Height;
            if (fw < 2) return;
            using (GraphicsPath p2 = UI.Round(new RectangleF(0, 0, fw, Height - 1), Height / 2f))
            {
                using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(0, 0, Math.Max(1, fw), Height), UI.Accent, UI.Glow(), 0f)) g.FillPath(br, p2);
                Region old = g.Clip; g.SetClip(p2);
                float sx = (UI.Phase * 3f % 1f) * (fw + 60) - 30;
                using (LinearGradientBrush sh = new LinearGradientBrush(new RectangleF(sx, 0, 40, Height), Color.FromArgb(0, 255, 255, 255), Color.FromArgb(70, 255, 255, 255), 0f))
                { sh.WrapMode = WrapMode.TileFlipX; g.FillRectangle(sh, sx, 0, 40, Height); }
                g.Clip = old;
            }
        }
    }

    class SItem { public string N, V; public int S; public SItem(string n, int s, string v) { N = n; S = s; V = v; } }
    class TLine { public string T; public Color C; public bool H; public TLine(string t, Color c, bool h) { T = t; C = c; H = h; } }

    class BiosView : Panel
    {
        List<TLine> data = new List<TLine>();
        public BiosView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            AutoScroll = true; BackColor = UI.Bg;
        }
        public void SetData(List<TLine> d) { data = d ?? new List<TLine>(); Relayout(); }
        void Relayout()
        {
            using (Graphics g = CreateGraphics())
            {
                float sc = UI.Dpi(g); int y = (int)(6 * sc); int w = ClientSize.Width - (int)(14 * sc);
                using (Font hf = new Font("Segoe UI Semibold", 11f)) using (Font bf = new Font("Segoe UI", 10f))
                    foreach (TLine l in data)
                    {
                        if (l.H) { y += (int)(14 * sc) + hf.Height; }
                        else { Size sz = TextRenderer.MeasureText(g, l.T, bf, new Size(w - (int)(40 * sc), 0), TextFormatFlags.WordBreak); y += sz.Height + (int)(10 * sc); }
                    }
                AutoScrollMinSize = new Size(0, y + (int)(10 * sc));
            }
            Invalidate();
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Relayout(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
            int ox = AutoScrollPosition.X, oy = AutoScrollPosition.Y;
            int y = (int)(6 * sc) + oy, left = (int)(4 * sc) + ox, w = ClientSize.Width - (int)(14 * sc);
            using (Font hf = new Font("Segoe UI Semibold", 11f)) using (Font bf = new Font("Segoe UI", 10f))
                foreach (TLine l in data)
                {
                    if (l.H)
                    {
                        y += (int)(14 * sc);
                        using (SolidBrush b = new SolidBrush(UI.Glow())) g.FillRectangle(b, left, y + (int)(3 * sc), (int)(4 * sc), hf.Height - (int)(6 * sc));
                        TextRenderer.DrawText(g, l.T, hf, new Point(left + (int)(14 * sc), y), l.C, TextFormatFlags.NoPrefix);
                        using (Pen lp = new Pen(UI.Line, 1f)) g.DrawLine(lp, left + (int)(14 * sc) + TextRenderer.MeasureText(l.T, hf).Width + (int)(10 * sc), y + hf.Height / 2, left + w, y + hf.Height / 2);
                        y += hf.Height;
                    }
                    else
                    {
                        Rectangle r = new Rectangle(left + (int)(24 * sc), y, w - (int)(40 * sc), 10000);
                        Size sz = TextRenderer.MeasureText(g, l.T, bf, new Size(r.Width, 0), TextFormatFlags.WordBreak);
                        using (GraphicsPath p = UI.Round(new RectangleF(left + (int)(10 * sc), y - (int)(3 * sc), w - (int)(12 * sc), sz.Height + (int)(6 * sc)), 8 * sc))
                        using (SolidBrush b = new SolidBrush(UI.Card)) g.FillPath(b, p);
                        using (SolidBrush dot = new SolidBrush(l.C == UI.Warn ? UI.Warn : l.C == UI.Good ? UI.Good : UI.Muted))
                            g.FillEllipse(dot, left + (int)(16 * sc), y + (int)(5 * sc), 6 * sc, 6 * sc);
                        TextRenderer.DrawText(g, l.T, bf, new Rectangle(r.X, y, r.Width, sz.Height), l.C == UI.Txt ? UI.Txt : l.C, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                        y += sz.Height + (int)(10 * sc);
                    }
                }
        }
    }

    class MainForm : Form
    {
        internal const string Version = "v17.7";
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);

        string batPath;
        bool busy;
        DB side, content, topBar, logo, specsCard, statusList, overlay, headerBand;
        Label pageTitle, pageSub, footLbl, taskTitle, taskStep;
        DB taskPct, taskLogView; int taskPercent = -1; ZBtn taskCancel; int logPageSel = -1; System.Windows.Forms.Timer restartTimer; int restartLeft;
        readonly List<string> taskLog = new List<string>();
        BiosView biosBox;
        ZBtn boostBtn, safeBtn, biosAuto, biosManual, restoreBtn;
        ComboBox cbBoard, cbCpu;
        ZBar taskBar;
        readonly Dictionary<string, DB> pages = new Dictionary<string, DB>();
        readonly Dictionary<string, ZBtn> navs = new Dictionary<string, ZBtn>();
        readonly List<Control> lockable = new List<Control>();
        System.Windows.Forms.Timer anim, procTimer; int tick; Bitmap bgSmall;

        string sCpu = "reading...", sGpu = "reading...", sRam = "-", sDisk = "-", sHz = "-", sProc = "-";
        List<SItem> statusItems = new List<SItem>();
        string statusSummary = "";
        bool biosManualMode, biosLoaded;

        public MainForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Bu 7maid Zero " + Version;
            ClientSize = new Size(1060, 700);
            BackColor = UI.Bg; ForeColor = UI.Txt;
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.75F);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildShell(); BuildHome(); BuildStatus(); BuildBios(); BuildTests(); BuildOc(); BuildTools(); BuildOverlay();
            Show("home");
            ResumeLayout(false);

            try { batPath = ExtractEngine(); } catch (Exception ex) { MessageBox.Show("Engine error: " + ex.Message, "Bu 7maid Zero"); }

            anim = new System.Windows.Forms.Timer(); anim.Interval = 33;
            anim.Tick += delegate {
                if (WindowState == FormWindowState.Minimized || ActiveForm != this) return;
                if (slideT > 0 && curPage != null) { slideT--; curPage.Top = 110 + slideT * 3; }
                UI.Phase += 0.0045f; if (UI.Phase > 1f) UI.Phase -= 1f;
                bool even = (tick++ & 1) == 0;
                if (overlay.Visible) { taskBar.Invalidate(); taskPct.Invalidate(); return; }
                foreach (DB pg in pages.Values) if (pg.Visible) pg.Invalidate();
                if (even) { topBar.Invalidate(); if (headerBand != null) headerBand.Invalidate(); }
            };
            anim.Start(); MemTrim.Start();
            procTimer = new System.Windows.Forms.Timer(); procTimer.Interval = 6000;
            procTimer.Tick += delegate { UpdateProc(); }; procTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = 1; try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { } try { DwmSetWindowAttribute(Handle, 19, ref on, 4); } catch { }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e); UpdateProc(); Tg.Hello();
            ThreadPool.QueueUserWorkItem(delegate { LoadSpecs(); });
            if (Program.ResumeOc) { Program.ResumeOc = false; Show("oc"); BeginInvoke(new Action(delegate { OcGpuAutoRun(true); })); }
            else Updater.Start("Zero", Version, this, delegate { return busy; });
        }

        // ---------- shell ----------
        void BuildShell()
        {
            side = new DB(); side.SetBounds(0, 0, 236, 700); side.BackColor = UI.Side;
            side.Paint += delegate (object s, PaintEventArgs e) { e.Graphics.Clip = new Region(new Rectangle(0, 0, side.Width, side.Height)); UI.Blobs(e.Graphics, new Rectangle(-30, 120, 300, 520), 0); };
            Controls.Add(side);
            logo = new DB(); logo.SetBounds(18, 22, 200, 64); logo.BackColor = UI.Side; logo.Paint += PaintLogo; side.Controls.Add(logo);

            string[] ids = { "home", "status", "bios", "tests", "oc", "tools" };
            string[] nm = { "HOME", "STATUS", "BIOS", "TESTS", "OVERCLOCK", "TOOLS" };
            for (int i = 0; i < ids.Length; i++)
            {
                ZBtn n = new ZBtn(); n.Kind = 2; n.Title = nm[i]; n.SetBounds(14, 118 + i * 52, 208, 44);
                string id = ids[i]; n.Click += delegate { if (!busy) Show(id); };
                side.Controls.Add(n); navs[id] = n;
            }
            restoreBtn = new ZBtn(); restoreBtn.Kind = 3; restoreBtn.Title = "RESTORE"; restoreBtn.Sub = "undo everything";
            restoreBtn.SetBounds(14, 568, 208, 58);
            restoreBtn.Click += delegate { if (Ask("RESTORE puts Windows back to how it was before Bu 7maid Zero.\n\nStart?")) RunHiddenTask("RESTORE", "RESTORE", true, null, true); };
            side.Controls.Add(restoreBtn); lockable.Add(restoreBtn);
            footLbl = Lbl(Version + "  -  ready", 22, 648, 200, 22, 8.5f, FontStyle.Regular, UI.Muted); side.Controls.Add(footLbl);

            content = new DB(); content.SetBounds(236, 0, 824, 700); content.BackColor = UI.Bg; Controls.Add(content);
            DB header = new DB(); header.SetBounds(0, 4, 824, 104); header.BackColor = UI.Bg;
            header.Paint += delegate (object s, PaintEventArgs e) { UI.Blobs(e.Graphics, new Rectangle(380, -40, 480, 200), 3); };
            content.Controls.Add(header); headerBand = header;
            topBar = new DB(); topBar.SetBounds(0, 0, 824, 4); topBar.Paint += PaintTopBar; content.Controls.Add(topBar);
            pageTitle = Lbl("", 36, 28, 740, 44, 21f, FontStyle.Bold, UI.Txt); content.Controls.Add(pageTitle); pageTitle.BringToFront();
            pageSub = Lbl("", 38, 72, 740, 22, 10f, FontStyle.Regular, UI.Muted); content.Controls.Add(pageSub); pageSub.BringToFront();
        }

        DB NewPage(string id)
        {
            DB p = new DB(); p.SetBounds(0, 110, 824, 590); p.BackColor = UI.Bg; p.Visible = false; content.Controls.Add(p); pages[id] = p;
            p.ControlAdded += delegate (object so, ControlEventArgs ev) { ZBtn zb = ev.Control as ZBtn; if (zb != null) zb.BackColor = UI.Bg; };
            p.Paint += delegate (object s, PaintEventArgs e) {
                if (bgSmall == null) bgSmall = new Bitmap(206, 148);
                using (Graphics bg = Graphics.FromImage(bgSmall))
                {
                    bg.Clear(UI.Bg); bg.ScaleTransform(0.25f, 0.25f);
                    UI.Strong = true;
                    UI.Blobs(bg, new Rectangle(-80, -40, 560, 420), 5);
                    UI.Blobs(bg, new Rectangle(380, 220, 520, 420), 9);
                    UI.Strong = false;
                }
                Graphics g = e.Graphics;
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.DrawImage(bgSmall, new Rectangle(0, 0, p.Width, p.Height));
                UI.Drift(g, new Rectangle(0, 0, 824, 590));
            };
            return p;
        }

        DB curPage; int slideT;
        void Show(string id)
        {
            if (pages.ContainsKey(id)) { curPage = pages[id]; slideT = 9; curPage.Top = 110 + 27; }
            foreach (KeyValuePair<string, DB> kv in pages) kv.Value.Visible = kv.Key == id;
            foreach (KeyValuePair<string, ZBtn> kv in navs) { kv.Value.Selected = kv.Key == id; kv.Value.Invalidate(); }
            string[,] h = { { "home", "Boost your PC", "one click: more FPS, less delay, fewer processes" },
                            { "status", "Status", "what Bu 7maid Zero has changed on this PC right now" },
                            { "bios", "BIOS advisor", "what to change in your BIOS, and exactly where it is" },
                            { "tests", "Tests", "measure before and after - real numbers" },
                            { "oc", "Overclock lab", "RAM, CPU and GPU - each one is a button you press yourself" },
                            { "tools", "Tools", "cleaning, RAM, virus scan and GPU colors" } };
            for (int i = 0; i < h.GetLength(0); i++) if (h[i, 0] == id) { pageTitle.Text = h[i, 1]; pageSub.Text = h[i, 2]; }
            if (id == "status" && statusItems.Count == 0) RefreshStatus();
            if (id == "bios" && !biosLoaded) RefreshBios();
            if (id == "oc" && !ocLoaded) ThreadPool.QueueUserWorkItem(delegate { LoadOcInfo(); });
        }

        void PaintTopBar(object s, PaintEventArgs e)
        {
            Control c = (Control)s;
            using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, c.Width / 2), c.Height), UI.Accent, UI.Accent2, 0f))
            { br.WrapMode = WrapMode.TileFlipX; br.TranslateTransform(UI.Phase * c.Width, 0); e.Graphics.FillRectangle(br, 0, 0, c.Width, c.Height); }
        }
        void PaintLogo(object s, PaintEventArgs e)
        {
            Control c = (Control)s; Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
            float box = 46 * sc; RectangleF r = new RectangleF(1, (c.Height - box) / 2f, box, box);
            using (GraphicsPath p = UI.Round(r, 12 * sc)) using (LinearGradientBrush br = new LinearGradientBrush(r, UI.Accent, UI.Glow(), LinearGradientMode.ForwardDiagonal)) g.FillPath(br, p);
            float bw = box * 0.15f, gap = box * 0.09f, baseY = r.Bottom - box * 0.24f;
            float x0 = r.X + box * 0.26f;
            float[] hs = { 0.26f, 0.42f, 0.60f };
            for (int i = 0; i < 3; i++)
            {
                float hh = box * hs[i];
                using (GraphicsPath bp = UI.Round(new RectangleF(x0 + i * (bw + gap), baseY - hh, bw, hh), bw * 0.4f))
                using (SolidBrush wb = new SolidBrush(Color.White)) g.FillPath(wb, bp);
            }
            float ax = x0 + 2 * (bw + gap) + bw * 0.5f, aTop = baseY - box * 0.72f;
            using (Pen ar = new Pen(Color.White, 2.3f * sc) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(ar, ax, aTop, ax, aTop + box * 0.2f);
                g.DrawLine(ar, ax, aTop, ax - box * 0.1f, aTop + box * 0.1f);
                g.DrawLine(ar, ax, aTop, ax + box * 0.1f, aTop + box * 0.1f);
            }
            int tx = (int)(box + 14 * sc);
            using (Font f1 = new Font("Segoe UI Black", 12.5f)) using (Font f2 = new Font("Segoe UI Semibold", 9.5f))
            { int top = (c.Height - f1.Height - f2.Height) / 2; TextRenderer.DrawText(g, "BU 7MAID", f1, new Point(tx, top), UI.Txt, TextFormatFlags.NoPadding); TextRenderer.DrawText(g, "ZERO  " + Version, f2, new Point(tx, top + f1.Height), UI.Accent, TextFormatFlags.NoPadding); }
        }

        // ---------- home ----------
        void BuildHome()
        {
            DB p = NewPage("home");
            specsCard = new DB(); specsCard.SetBounds(36, 4, 752, 150); specsCard.BackColor = UI.Bg; specsCard.Paint += PaintSpecs; p.Controls.Add(specsCard);

            boostBtn = new ZBtn(); boostBtn.Kind = 1; boostBtn.Title = "BOOST"; boostBtn.Sub = "everything  -  max FPS, lowest delay";
            boostBtn.SetBounds(36, 172, 470, 130);
            boostBtn.Click += delegate { if (Ask("BOOST applies every tweak, tunes all your games for max FPS and turns antivirus OFF.\nProgress shows here. You are asked before the PC restarts.\n\nStart?")) { AskCod(); AskStartup(); if (MeasureBefore()) RunHiddenTask("BOOST", "BOOST", true, null, true); } };
            p.Controls.Add(boostBtn); lockable.Add(boostBtn);

            safeBtn = new ZBtn(); safeBtn.Title = "BOOST SAFE"; safeBtn.Sub = "same tweaks, antivirus stays ON";
            safeBtn.SetBounds(522, 172, 266, 130);
            safeBtn.Click += delegate { if (Ask("BOOST SAFE applies the tweaks but keeps Windows security ON (best for friends).\nYou will be asked before the PC restarts.\n\nStart?")) { AskCod(); AskStartup(); if (MeasureBefore()) RunHiddenTask("SAFE", "BOOST SAFE", true, null, true); } };
            p.Controls.Add(safeBtn); lockable.Add(safeBtn);

            p.Controls.Add(Lbl("QUICK", 38, 326, 200, 20, 8.5f, FontStyle.Bold, UI.Muted));
            string[] ids = { "status", "bios", "tests", "games", "tools" };
            string[] t = { "STATUS", "BIOS", "TESTS", "GAMES", "TOOLS" };
            string[] sub = { "what is ON / OFF", "settings + advice", "GPU / DPC / ping", "tune every game", "clean / scan / fix" };
            for (int i = 0; i < 5; i++)
            {
                ZBtn b = new ZBtn(); b.Title = t[i]; b.Sub = sub[i]; b.SetBounds(36 + i * 152, 350, 144, 74);
                string id = ids[i];
                if (id == "games") b.Click += delegate { if (!busy && Ask("Scans your PC for games (Steam, Epic, Riot, FiveM, Fortnite, GTA, Forza...) and tunes each one for max FPS: GPU = high performance, priority High, game settings file tuned, background apps freeze while you play.\n\nStart?")) { AskCod(); RunHiddenTask("GAMES", "Tuning your games", true, "Games tuned. DLSS and graphics presets are chosen inside each game."); } };
                else b.Click += delegate { if (!busy) Show(id); };
                p.Controls.Add(b);
            }
            p.Controls.Add(Lbl("Undo anytime with RESTORE (bottom-left). Share only the .exe with friends and tell them to press BOOST SAFE.", 38, 446, 752, 44, 9.5f, FontStyle.Regular, UI.Muted));
        }

        void PaintSpecs(object s, PaintEventArgs e)
        {
            Control c = (Control)s; Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
            using (GraphicsPath p = UI.Round(new RectangleF(0, 0, c.Width - 1, c.Height - 1), 14 * sc)) using (SolidBrush b = new SolidBrush(UI.Card)) g.FillPath(b, p);
            using (Font tag = new Font("Segoe UI Semibold", 8.25f)) using (Font val = new Font("Segoe UI", 10.5f)) using (Font big = new Font("Segoe UI Black", 22f))
            {
                int lx = (int)(22 * sc), vx = (int)(92 * sc), row = (int)(34 * sc), y0 = (int)(16 * sc), colW = (int)(360 * sc);
                string[] tags = { "CPU", "GPU", "RAM" };
                string[] vals = { sCpu, sGpu, sRam + "        DISK   " + sDisk };
                for (int i = 0; i < 3; i++)
                {
                    int y = y0 + i * row;
                    TextRenderer.DrawText(g, tags[i], tag, new Rectangle(lx, y, vx - lx, row), UI.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, vals[i], val, new Rectangle(vx, y, colW, row), UI.Txt, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
                int rx = (int)(470 * sc), rv = (int)(570 * sc);
                TextRenderer.DrawText(g, "SCREEN", tag, new Rectangle(rx, y0, (int)(100 * sc), row), UI.Muted, TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, sHz, val, new Rectangle(rv, y0, (int)(170 * sc), row), UI.Txt, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, "PROCESSES", tag, new Rectangle(rx, y0 + row, (int)(120 * sc), row), UI.Muted, TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, sProc, big, new Rectangle(rx, y0 + row + (int)(16 * sc), (int)(150 * sc), (int)(48 * sc)), UI.Accent, TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "live", tag, new Rectangle(rx + (int)(128 * sc), y0 + row + (int)(28 * sc), (int)(80 * sc), row), UI.Muted, TextFormatFlags.VerticalCenter);
            }
        }

        // ---------- status ----------
        void BuildStatus()
        {
            DB p = NewPage("status");
            statusList = new DB(); statusList.SetBounds(36, 4, 752, 492); statusList.BackColor = UI.Bg; statusList.Paint += PaintStatus; p.Controls.Add(statusList);
            ZBtn r = new ZBtn(); r.Title = "REFRESH"; r.SetBounds(36, 506, 170, 46); r.Click += delegate { RefreshStatus(); }; p.Controls.Add(r);
            ZBtn shr = new ZBtn(); shr.Title = "SHARE LINK"; shr.SetBounds(216, 506, 170, 46); shr.Click += delegate { Updater.Share("Zero"); }; p.Controls.Add(shr);
            ZBtn tgb = new ZBtn(); tgb.Title = "TEST TELEGRAM"; tgb.SetBounds(396, 506, 190, 46); tgb.Click += delegate { Tg.Test(); }; p.Controls.Add(tgb);
        }
        void PaintStatus(object s, PaintEventArgs e)
        {
            Control c = (Control)s; Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
            using (GraphicsPath p = UI.Round(new RectangleF(0, 0, c.Width - 1, c.Height - 1), 14 * sc)) using (SolidBrush b = new SolidBrush(UI.Card)) g.FillPath(b, p);
            using (Font hf = new Font("Segoe UI Semibold", 11.5f)) using (Font f = new Font("Segoe UI", 9.75f)) using (Font pf = new Font("Segoe UI Semibold", 8.25f))
            {
                TextRenderer.DrawText(g, statusSummary.Length > 0 ? statusSummary : "press REFRESH", hf, new Point((int)(22 * sc), (int)(14 * sc)), UI.Txt);
                int y = (int)(52 * sc), rowH = (int)(25 * sc), colW = (c.Width - (int)(44 * sc)) / 2;
                for (int i = 0; i < statusItems.Count; i++)
                {
                    SItem it = statusItems[i]; int col = i % 2, rIdx = i / 2;
                    int x = (int)(22 * sc) + col * colW, yy = y + rIdx * rowH;
                    Color dc = it.S == 1 ? UI.Good : it.S == 3 ? UI.Warn : it.S == 2 ? UI.Muted : UI.Bad;
                    using (SolidBrush db = new SolidBrush(dc)) g.FillEllipse(db, x, yy + rowH / 2f - 4 * sc, 8 * sc, 8 * sc);
                    TextRenderer.DrawText(g, it.N, f, new Rectangle(x + (int)(16 * sc), yy, colW - (int)(140 * sc), rowH), UI.Txt, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, it.V, pf, new Rectangle(x + colW - (int)(132 * sc), yy, (int)(120 * sc), rowH), dc, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }
        void RefreshStatus()
        {
            statusSummary = "checking..."; statusList.Invalidate();
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<SItem> L = new List<SItem>();
                const string LM = "HKEY_LOCAL_MACHINE\\", CU = "HKEY_CURRENT_USER\\";
                L.Add(Chk("Bu 7maid power plan", Run("powercfg.exe", "/getactivescheme", 8000).Contains("Bu 7maid")));
                long? sp = RD(LM + @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB"); L.Add(Chk("svchost merged (fewer processes)", sp.HasValue && sp.Value > 3670016));
                bool? vbs = null; try { foreach (ManagementObject o in Wmi(@"root\Microsoft\Windows\DeviceGuard", "SELECT VirtualizationBasedSecurityStatus FROM Win32_DeviceGuard")) { vbs = ToL(o["VirtualizationBasedSecurityStatus"]) != 2; try { foreach (ManagementObject o2 in Wmi(null, "SELECT HypervisorPresent FROM Win32_ComputerSystem")) { if (!Convert.ToBoolean(o2["HypervisorPresent"])) vbs = true; break; } } catch { } break; } } catch { }
                long? vreg = RD(LM + @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity");
                L.Add(vbs.HasValue ? ((!vbs.Value && vreg.HasValue && vreg.Value == 0) ? new SItem("VBS / Memory Integrity OFF", 2, "RESTART PC") : Chk("VBS / Memory Integrity OFF", vbs.Value)) : new SItem("VBS / Memory Integrity OFF", 2, "n/a"));
                bool rt = true; try { foreach (ManagementObject o in Wmi(@"root\Microsoft\Windows\Defender", "SELECT RealTimeProtectionEnabled FROM MSFT_MpComputerStatus")) { rt = !Convert.ToBoolean(o["RealTimeProtectionEnabled"]); break; } } catch { }
                L.Add(new SItem("Defender realtime OFF", rt ? 1 : 0, rt ? "OFF" : "ON (safe)"));
                L.Add(Chk("Game DVR OFF", Is(CU + @"System\GameConfigStore", "GameDVR_Enabled", 0)));
                L.Add(Chk("Game Bar presence writer OFF", Is(LM + @"SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter", "ActivationType", 0)));
                L.Add(new SItem("GPU hardware scheduling ON", Is(LM + @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2) ? 1 : 0, Is(LM + @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2) ? "ON" : "OFF"));
                L.Add(Chk("MPO OFF (no flicker)", Is(LM + @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5)));
                string bcd = ""; try { bcd = (Run(Environment.SystemDirectory + "\\bcdedit.exe", "/enum {current}", 8000) ?? "").ToLower(); } catch { }
                L.Add(Chk("Dynamic tick OFF (steady timer)", System.Text.RegularExpressions.Regex.IsMatch(bcd, @"disabledynamictick\s+yes")));
                bool hvPresent = false; try { foreach (ManagementObject o in Wmi(null, "SELECT HypervisorPresent FROM Win32_ComputerSystem")) { hvPresent = Convert.ToBoolean(o["HypervisorPresent"]); break; } } catch { }
                bool hvCfgOff = System.Text.RegularExpressions.Regex.IsMatch(bcd, @"hypervisorlaunchtype\s+off");
                L.Add(new SItem("Windows hypervisor OFF (VBS can't run)", hvCfgOff && !hvPresent ? 1 : 0, hvCfgOff && !hvPresent ? "DONE" : hvCfgOff ? "RESTART PC" : "NOT SET"));
                L.Add(new SItem("Foreground priority", Is(LM + @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38) ? 1 : 0, "0x26"));
                L.Add(Chk("Network throttling OFF", Is(LM + @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 4294967295L)));
                L.Add(Chk("Telemetry OFF", Is(LM + @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0)));
                { long? smv = RD(LM + @"SYSTEM\CurrentControlSet\Services\SysMain", "Start"); bool smo = smv.HasValue && smv.Value == 4; L.Add(new SItem("SysMain OFF", smo ? 1 : 0, smo ? "DONE" : "Start=" + (smv.HasValue ? smv.Value.ToString() : "?"))); }
                L.Add(Chk("Delivery Optimization OFF", Is(LM + @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 0)));
                L.Add(Chk("Power throttling OFF", Is(LM + @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1)));
                L.Add(Chk("Search indexing OFF", Is(LM + @"SYSTEM\CurrentControlSet\Services\WSearch", "Start", 4)));
                L.Add(Chk("Network usage driver (NDU) OFF", Is(LM + @"SYSTEM\CurrentControlSet\Services\Ndu", "Start", 4)));
                L.Add(Chk("Background apps OFF", Is(CU + @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1)));
                L.Add(new SItem("Copilot removed", Is(LM + @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1) ? 1 : 0, "GONE"));
                string mc = Run("powershell.exe", "-NoProfile -Command \"(Get-MMAgent).MemoryCompression\"", 15000).Trim();
                if (mc.Length > 0) L.Add(Chk("Memory compression OFF", mc.StartsWith("False")));
                L.Add(new SItem("Better colors (vibrance)", Run("schtasks.exe", "/Query /TN \"Bu7maid Colors\"", 8000).Contains("Bu7maid Colors") ? 1 : 2, "set"));
                try { foreach (ManagementObject o in Wmi(null, "SELECT ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory")) { long cf = ToL(o["ConfiguredClockSpeed"]), rd = ToL(o["Speed"]); L.Add(new SItem("RAM speed " + cf + "/" + rd, cf >= rd ? 1 : 3, cf >= rd ? "FULL" : "XMP OFF")); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT Name,CurrentRefreshRate,MaxRefreshRate FROM Win32_VideoController")) { string n = Str(o["Name"]); if (!(n.Contains("NVIDIA") || n.Contains("Radeon") || n.Contains("Arc"))) continue; long cu = ToL(o["CurrentRefreshRate"]), mx = ToL(o["MaxRefreshRate"]); if (cu > 0) L.Add(new SItem("Screen " + cu + " Hz", mx <= cu ? 1 : 3, mx <= cu ? "MAX" : "max " + mx)); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem")) { double tt = ToL(o["TotalVisibleMemorySize"]) / 1048576.0, fr = ToL(o["FreePhysicalMemory"]) / 1048576.0; L.Add(new SItem("RAM in use", 2, (tt - fr).ToString("0.0") + " / " + tt.ToString("0") + " GB")); break; } } catch { }
                L.Add(new SItem("Processes running", 2, Process.GetProcesses().Length.ToString()));
                int good = 0, tot = 0; foreach (SItem it in L) if (it.S == 0 || it.S == 1) { tot++; if (it.S == 1) good++; }
                string sum = good + " of " + tot + " tweaks active" + (good == tot && tot > 0 ? "  -  all good" : "");
                try { BeginInvoke(new Action(delegate { statusItems = L; statusSummary = sum; statusList.Invalidate(); })); } catch { }
            });
        }
        static SItem Chk(string n, bool ok) { return new SItem(n, ok ? 1 : 0, ok ? "DONE" : "NOT SET"); }

        // ---------- bios ----------
        void BuildBios()
        {
            DB p = NewPage("bios");
            biosAuto = new ZBtn(); biosAuto.Title = "AUTO - read this PC"; biosAuto.SetBounds(36, 2, 220, 44);
            biosAuto.Click += delegate { biosManualMode = false; cbBoard.Visible = cbCpu.Visible = false; RefreshBios(); };
            p.Controls.Add(biosAuto);
            biosManual = new ZBtn(); biosManual.Title = "MANUAL - friend's PC"; biosManual.SetBounds(266, 2, 220, 44);
            biosManual.Click += delegate { biosManualMode = true; cbBoard.Visible = cbCpu.Visible = true; RefreshBios(); };
            p.Controls.Add(biosManual);
            cbBoard = Combo(new string[] { "ASUS", "MSI", "GIGABYTE", "ASROCK", "OTHER" }, 510, 10, 128);
            cbCpu = Combo(new string[] { "AMD Ryzen", "AMD X3D", "Intel" }, 650, 10, 138);
            cbBoard.SelectedIndexChanged += delegate { if (biosManualMode) RefreshBios(); };
            cbCpu.SelectedIndexChanged += delegate { if (biosManualMode) RefreshBios(); };
            cbBoard.Visible = cbCpu.Visible = false; p.Controls.Add(cbBoard); p.Controls.Add(cbCpu);
            biosBox = new BiosView(); biosBox.SetBounds(36, 58, 752, 436); p.Controls.Add(biosBox);
            ZBtn go = new ZBtn(); go.Kind = 3; go.Title = "RESTART INTO BIOS"; go.SetBounds(36, 506, 250, 46);
            go.Click += delegate { if (Ask("The PC restarts straight into the BIOS in 5 seconds. Save your work first.\n\nRestart now?")) Run("shutdown.exe", "/r /fw /t 5", 4000); };
            p.Controls.Add(go); lockable.Add(go);
        }
        void RefreshBios()
        {
            biosAuto.Selected = !biosManualMode; biosManual.Selected = biosManualMode; biosAuto.Invalidate(); biosManual.Invalidate();
            biosLoaded = true;
            biosBox.SetData(new List<TLine>(new TLine[] { new TLine("reading...", UI.Muted, false) }));
            bool man = biosManualMode; string mb = Convert.ToString(cbBoard.SelectedItem), mc = Convert.ToString(cbCpu.SelectedItem);
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<TLine> L; try { L = Bios(man, mb, mc); } catch (Exception ex) { L = new List<TLine>(); L.Add(new TLine("Could not read: " + ex.Message, UI.Bad, false)); }
                try { BeginInvoke(new Action(delegate { biosBox.SetData(L); })); } catch { }
            });
        }
        List<TLine> Bios(bool man, string mBoard, string mCpu)
        {
            List<TLine> L = new List<TLine>();
            string mf = "", model = "(your board)", biosVer = "?", biosDate = "?", cpu = "";
            long ramCfg = 0, ramRated = 0; bool laptop = false;
            string v; bool amd, x3d, intel;
            if (man)
            {
                v = mBoard; intel = mCpu == "Intel"; x3d = mCpu == "AMD X3D"; amd = !intel;
                cpu = intel ? "Intel Core" : x3d ? "AMD Ryzen X3D" : "AMD Ryzen"; mf = v;
            }
            else
            {
                try { foreach (ManagementObject o in Wmi(null, "SELECT Manufacturer,Product FROM Win32_BaseBoard")) { mf = Str(o["Manufacturer"]); model = Str(o["Product"]); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT SMBIOSBIOSVersion,ReleaseDate FROM Win32_BIOS")) { biosVer = Str(o["SMBIOSBIOSVersion"]); string d = Str(o["ReleaseDate"]); if (d.Length >= 8) biosDate = d.Substring(0, 4) + "-" + d.Substring(4, 2) + "-" + d.Substring(6, 2); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT Name FROM Win32_Processor")) { cpu = Str(o["Name"]).Trim(); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT ConfiguredClockSpeed,Speed FROM Win32_PhysicalMemory")) { ramCfg = ToL(o["ConfiguredClockSpeed"]); ramRated = ToL(o["Speed"]); break; } } catch { }
                try { foreach (ManagementObject o in Wmi(null, "SELECT Name FROM Win32_Battery")) { laptop = true; break; } } catch { }
                v = mf.ToUpper().Contains("ASUS") ? "ASUS" : (mf.ToUpper().Contains("MSI") || mf.ToUpper().Contains("MICRO-STAR")) ? "MSI" : mf.ToUpper().Contains("GIGABYTE") ? "GIGABYTE" : mf.ToUpper().Contains("ASROCK") ? "ASROCK" : "OTHER";
                amd = cpu.ToUpper().Contains("AMD"); x3d = cpu.ToUpper().Contains("X3D"); intel = cpu.ToUpper().Contains("INTEL");
            }
            bool okBios = false, okBar = false, okSb = false, okTpm = false, okUefi = false; string barTxt = "";
            if (!man)
            {
                try { DateTime bd; if (DateTime.TryParse(biosDate, out bd)) okBios = (DateTime.Now - bd).TotalDays < 400; } catch { }
                try
                {
                    string o = Run(Environment.SystemDirectory + "\\nvidia-smi.exe", "-q -d MEMORY", 6000);
                    if (string.IsNullOrEmpty(o)) o = Run(@"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe", "-q -d MEMORY", 6000);
                    int b1 = o == null ? -1 : o.IndexOf("BAR1");
                    if (b1 >= 0)
                    {
                        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(o.Substring(b1), @"Total\s*:\s*(\d+)\s*MiB");
                        if (m.Success) { long mb = long.Parse(m.Groups[1].Value); okBar = mb >= 4096; barTxt = mb + " MiB"; }
                    }
                } catch { }
                okSb = Is(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled", 1);
                try { foreach (ManagementObject o in Wmi(@"root\cimv2\security\microsofttpm", "SELECT IsEnabled_InitialValue FROM Win32_Tpm")) { okTpm = Convert.ToBoolean(o["IsEnabled_InitialValue"]); break; } } catch { }
                okUefi = Is(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "PEFirmwareType", 2);
            }
            L.Add(new TLine(man ? "YOUR CHOICE" : "YOUR PC", UI.Accent, true));
            L.Add(new TLine("Board : " + (man ? v : mf + " " + model), UI.Txt, false));
            if (!man) { L.Add(new TLine("BIOS  : " + biosVer + "   date " + biosDate, UI.Txt, false)); L.Add(new TLine("RAM   : running " + ramCfg + " MT/s   (rated " + ramRated + ")", UI.Txt, false)); }
            L.Add(new TLine("CPU   : " + cpu, UI.Txt, false));
            if (laptop) { L.Add(new TLine("LAPTOP", UI.Accent, true)); L.Add(new TLine("Laptop BIOS is locked by the maker - most options below won't exist.", UI.Warn, false)); L.Add(new TLine("Only do: update BIOS from the maker's site, and set the fan/power mode to Performance in its app.", UI.Warn, false)); return L; }
            string enter = v == "ASUS" || v == "MSI" ? "DEL, then F7 for Advanced Mode" : v == "GIGABYTE" ? "DEL, then F2 for Advanced Mode" : "DEL or F2";
            L.Add(new TLine("HOW TO ENTER", UI.Accent, true));
            L.Add(new TLine("Restart and press " + enter + "  (or use the RESTART INTO BIOS button).", UI.Txt, false));
            int n = 1; bool anyNeeded = false;
            bool asus = v == "ASUS", msi = v == "MSI", gig = v == "GIGABYTE", asr = v == "ASROCK";
            Func<string, string, string, string, string> P = delegate (string a1, string m1, string g1, string r1) { return asus ? a1 : msi ? m1 : gig ? g1 : asr ? r1 : a1; };
            bool i1314 = intel && (man || System.Text.RegularExpressions.Regex.IsMatch(cpu, @"i[3579]-1[34]\d{3}"));
            long micro = -1;
            if (!man && i1314) { try { byte[] ub = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "Update Revision", null) as byte[]; if (ub != null && ub.Length >= 8) micro = BitConverter.ToUInt32(ub, 4); } catch { } }
            L.Add(new TLine("Only what your PC really needs is listed (checked live from Windows). Everything else is already fine - leave it alone.", UI.Muted, false));
            // 1. BIOS update
            bool needBios = i1314 && !man ? (micro >= 0 && micro < 0x12B) : (!man && !okBios);
            if (man) needBios = true;
            if (needBios)
            {
                anyNeeded = true;
                L.Add(new TLine(n++ + ". UPDATE BIOS  -  " + (man ? "do this first" : i1314 ? "IMPORTANT (your CPU microcode is 0x" + micro.ToString("X") + ", the stability fix is 0x12B or newer)" : "your BIOS is from " + biosDate), UI.Accent, true));
                L.Add(new TLine("Download the newest BIOS for " + (man ? v : mf + " " + model) + " from the maker's support site onto a USB stick (FAT32).", UI.Txt, false));
                L.Add(new TLine("Flash tool: " + P("Tool > ASUS EZ Flash 3", "M-FLASH (press it on the first BIOS screen)", "Q-Flash (F8)", "Tool > Instant Flash") + ".  Settings reset after flashing, so do this FIRST.", UI.Txt, false));
            }
            else if (i1314) L.Add(new TLine("CPU microcode 0x" + micro.ToString("X") + " - Intel's stability fix is already in your BIOS. No update needed.", UI.Good, false));
            // 2. RAM profile
            bool okRam = !man && ramCfg >= ramRated && ramRated > 3200;
            bool noXmp = !man && ramRated > 0 && ramRated <= 3200;
            if (man || (!okRam && !noXmp))
            {
                anyNeeded = true;
                L.Add(new TLine(n++ + ". RAM PROFILE  -  ENABLE  (your RAM is rated " + (man ? "faster" : ramRated.ToString()) + " but runs " + (man ? "slower" : ramCfg.ToString()) + " MT/s)", UI.Accent, true));
                L.Add(new TLine("Go to: " + P("Ai Tweaker", "OC (top)", "Tweaker", "OC Tweaker > DRAM Configuration"), UI.Txt, false));
                L.Add(new TLine("   >  " + P("Ai Overclock Tuner", "Extreme Memory Profile (X.M.P.)", "Extreme Memory Profile (X.M.P.)", "Load XMP Setting") + "   =   " + P(amd ? "EXPO I" : "XMP I", "Profile 1", "Profile 1", "XMP 1"), UI.Good, false));
            }
            else if (okRam) L.Add(new TLine("RAM already runs at " + ramCfg + " MT/s - profile is ON.", UI.Good, false));
            // 3. Resizable BAR
            if (man || !okBar)
            {
                anyNeeded = true;
                L.Add(new TLine(n++ + ". RESIZABLE BAR  -  ENABLE  (more FPS)", UI.Accent, true));
                if (!okUefi) { L.Add(new TLine("First: " + P("Boot > CSM", "Settings > Boot", "Boot", "Boot > CSM") + "   >   " + P("Launch CSM", "CSM / UEFI Mode", "CSM Support", "CSM") + "   =   " + (msi ? "UEFI" : "Disabled"), UI.Warn, false)); }
                L.Add(new TLine("Go to: " + P("Advanced > PCI Subsystem Settings", "Settings > Advanced > PCI Subsystem Settings", "Settings > IO Ports", "Advanced > North Bridge Configuration"), UI.Txt, false));
                L.Add(new TLine("   >  Above 4G Decoding   =   Enabled", UI.Good, false));
                L.Add(new TLine("   >  Re-Size BAR Support   =   Enabled", UI.Good, false));
            }
            else L.Add(new TLine("Resizable BAR already ON (GPU window " + barTxt + ").", UI.Good, false));
            if (!man && !okTpm) { anyNeeded = true; L.Add(new TLine(n++ + ". TPM looks OFF  -  turn it ON  (" + P("Advanced > PCH-FW Configuration > TPM Device Selection = Firmware TPM", "Settings > Security > Trusted Computing", "Settings > Miscellaneous > Intel Platform Trust Technology = Enabled", "Security > Intel Platform Trust Technology = Enabled") + ")", UI.Warn, true)); }
            if (!man && !okSb) { anyNeeded = true; L.Add(new TLine(n++ + ". SECURE BOOT looks OFF  -  turn it ON  (needed for Valorant / new CoD)", UI.Warn, true)); }
            if (!anyNeeded) L.Add(new TLine("Nothing to change in the BIOS - everything important is already set. You are done.", UI.Good, true));
            if (anyNeeded) L.Add(new TLine("AFTER: press F10 = Save & Exit. Then open STATUS here - it shows if it worked.", UI.Muted, false));
            return L;
        }

        // ---------- tests ----------
        BenchRes benchBefore, benchAfter; DB benchPanel; Font fbTitle, fbSub, fbBig, fbTag, fbRow, fbSmall;
        void BuildTests()
        {
            DB p = NewPage("tests");
            benchBefore = BenchRes.Load("before"); benchAfter = BenchRes.Load("after");
            fbTitle = new Font("Segoe UI", 13f, FontStyle.Bold); fbSub = new Font("Segoe UI", 9f); fbBig = new Font("Segoe UI Black", 28f);
            fbTag = new Font("Segoe UI", 10f, FontStyle.Bold); fbRow = new Font("Segoe UI", 10f); fbSmall = new Font("Segoe UI", 8.5f);
            benchPanel = new DB(); benchPanel.SetBounds(36, 4, 752, 396); benchPanel.BackColor = UI.Bg;
            benchPanel.Paint += delegate (object s, PaintEventArgs e) { PaintBench(e.Graphics, benchPanel.Width, benchPanel.Height); };
            p.Controls.Add(benchPanel);
            ZBtn rb = Big("START GPU TEST", "30 s. First run = BEFORE, next run = AFTER", 36, 408, 752, 54, 1);
            rb.Click += delegate { RunGpuBench(benchBefore == null ? "before" : "after"); };
            p.Controls.Add(rb); lockable.Add(rb);
            ZBtn dpc = Big("DPC TOOL", "driver delay graph", 36, 470, 246, 54, 0);
            dpc.Click += delegate { new DpcForm().Show(); };
            p.Controls.Add(dpc); lockable.Add(dpc);
            ZBtn del = Big("PING TEST", "ping + jitter + loss", 289, 470, 246, 54, 0);
            del.Click += delegate { RunPingTest(); };
            p.Controls.Add(del); lockable.Add(del);
            ZBtn rs = Big("RESET", "start a new comparison", 542, 470, 246, 54, 3);
            rs.Click += delegate { if (Ask("Delete the saved BEFORE and AFTER results?")) { try { File.Delete(BenchRes.PathFor("before")); File.Delete(BenchRes.PathFor("after")); } catch { } benchBefore = benchAfter = null; benchPanel.Invalidate(); } };
            p.Controls.Add(rs); lockable.Add(rs);
            p.Controls.Add(Lbl("Press START before BOOST, then again after BOOST + restart (or use the optional automatic measuring when you press BOOST). Same scene, same size, VSync off, nothing else open. Every number is counted from real frames your GPU finished.", 38, 530, 752, 50, 9.5f, FontStyle.Regular, UI.Muted));
        }
        void DrawTxt(Graphics g, string t, Font f, Color c, float x, float y, float w, float h, bool right)
        {
            TextFormatFlags fl = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | (right ? TextFormatFlags.Right : TextFormatFlags.Left);
            TextRenderer.DrawText(g, t, f, new Rectangle((int)x, (int)y, (int)w, (int)h), c, fl);
        }
        static string Num(double v, string f) { return v.ToString(f, System.Globalization.CultureInfo.InvariantCulture); }
        void DrawBenchCard(Graphics g, float s, RectangleF r, string tag, Color tagCol, BenchRes res)
        {
            using (GraphicsPath gp = UI.Round(r, 12 * s)) using (SolidBrush b = new SolidBrush(UI.Card2)) g.FillPath(b, gp);
            using (GraphicsPath gp = UI.Round(r, 12 * s)) using (Pen pn = new Pen(res == null ? UI.Line : tagCol, 1.4f * s)) g.DrawPath(pn, gp);
            DrawTxt(g, tag, fbTag, tagCol, r.X + 16 * s, r.Y + 10 * s, r.Width - 32 * s, 22 * s, false);
            if (res == null)
            {
                TextRenderer.DrawText(g, "not tested yet", fbRow, new Rectangle((int)r.X, (int)(r.Y + r.Height / 2 - 12 * s), (int)r.Width, (int)(24 * s)), UI.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }
            DrawTxt(g, res.Score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), fbBig, UI.Txt, r.X + 16 * s, r.Y + 30 * s, r.Width - 32 * s, 50 * s, false);
            DrawTxt(g, "SCORE", fbSmall, UI.Muted, r.X + 16 * s, r.Y + 78 * s, 120 * s, 16 * s, false);
            string[] lab = { "Average FPS", "1% low FPS", "Stability (+-)", "Max GPU temp", "GPU clock (avg)" };
            string[] val = { Num(res.Avg, "0.0"), Num(res.Low1, "0.0"), Num(res.Spread, "0.0") + " %", res.TempMax > 0 ? res.TempMax + " C" : "n/a", res.ClockAvg > 0 ? res.ClockAvg + " MHz" : "n/a" };
            for (int i = 0; i < lab.Length; i++)
            {
                float y = r.Y + 88 * s + i * 20 * s;
                DrawTxt(g, lab[i], fbRow, UI.Muted, r.X + 16 * s, y, r.Width * 0.5f, 22 * s, false);
                DrawTxt(g, val[i], fbRow, UI.Txt, r.X + r.Width * 0.45f, y, r.Width * 0.55f - 16 * s, 22 * s, true);
            }
            DrawTxt(g, res.When + "   " + res.Size + "   " + Num(res.Seconds, "0.0") + " s   " + res.Frames.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " frames", fbSmall, UI.Muted, r.X + 14 * s, r.Bottom - 20 * s, r.Width - 32 * s, 18 * s, false);
        }
        void PaintBench(Graphics g, int W, int H)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            float s = UI.Dpi(g);
            RectangleF full = new RectangleF(0, 0, W - 1, H - 1);
            using (GraphicsPath gp = UI.Round(full, 14 * s)) using (SolidBrush b = new SolidBrush(UI.Card)) g.FillPath(b, gp);
            using (GraphicsPath gp = UI.Round(full, 14 * s)) using (Pen pn = new Pen(UI.Line, 1f)) g.DrawPath(pn, gp);
            DrawTxt(g, "GPU BENCHMARK", fbTitle, UI.Txt, 18 * s, 10 * s, 300 * s, 26 * s, false);
            string gname = benchAfter != null ? benchAfter.Gpu : benchBefore != null ? benchBefore.Gpu : "built in  -  same 3D scene every run  -  60 s";
            DrawTxt(g, gname, fbSub, UI.Muted, 18 * s, 34 * s, W - 36 * s, 18 * s, false);
            float pad = 14 * s, top = 58 * s, cw = (W - 3 * pad) / 2f, ch = 214 * s;
            DrawBenchCard(g, s, new RectangleF(pad, top, cw, ch), "BEFORE TWEAK", UI.Muted, benchBefore);
            DrawBenchCard(g, s, new RectangleF(2 * pad + cw, top, cw, ch), "AFTER TWEAK", UI.Good, benchAfter);
            RectangleF vr = new RectangleF(pad, top + ch + 10 * s, W - 2 * pad, H - (top + ch + 10 * s) - pad);
            using (GraphicsPath gp = UI.Round(vr, 12 * s)) using (SolidBrush b = new SolidBrush(UI.Card2)) g.FillPath(b, gp);
            if (benchBefore == null || benchAfter == null)
            {
                TextRenderer.DrawText(g, "Run BEFORE and AFTER - the difference shows here", fbRow, new Rectangle((int)vr.X, (int)vr.Y, (int)vr.Width, (int)vr.Height), UI.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }
            double pct = (benchAfter.Avg - benchBefore.Avg) / benchBefore.Avg * 100.0;
            double dFps = benchAfter.Avg - benchBefore.Avg, dLow = benchAfter.Low1 - benchBefore.Low1; int dScore = benchAfter.Score - benchBefore.Score;
            Color vc = pct > 1.5 ? UI.Good : pct < -1.5 ? UI.Bad : UI.Warn;
            string verdict = pct > 1.5 ? "FASTER  " + (pct >= 0 ? "+" : "") + Num(pct, "0.0") + "%" : pct < -1.5 ? "SLOWER  " + Num(pct, "0.0") + "%" : "SAME  (" + (pct >= 0 ? "+" : "") + Num(pct, "0.0") + "%  - inside normal +-1.5% noise)";
            DrawTxt(g, verdict, fbTag, vc, vr.X + 16 * s, vr.Y + 6 * s, vr.Width - 32 * s, 24 * s, false);
            string d1 = "Score " + (dScore >= 0 ? "+" : "") + dScore.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + "     Avg FPS " + (dFps >= 0 ? "+" : "") + Num(dFps, "0.0") + "     1% low " + (dLow >= 0 ? "+" : "") + Num(dLow, "0.0");
            if (benchBefore.TempMax > 0 && benchAfter.TempMax > 0) d1 += "     Temp " + (benchAfter.TempMax - benchBefore.TempMax >= 0 ? "+" : "") + (benchAfter.TempMax - benchBefore.TempMax) + " C";
            DrawTxt(g, d1, fbRow, UI.Txt, vr.X + 16 * s, vr.Y + 30 * s, vr.Width - 32 * s, 22 * s, false);
            double mx = Math.Max(benchBefore.Avg, benchAfter.Avg); float bw = vr.Width - 32 * s, by = vr.Y + 58 * s;
            using (SolidBrush b1 = new SolidBrush(UI.Muted)) g.FillRectangle(b1, vr.X + 16 * s, by, (float)(bw * benchBefore.Avg / mx), 7 * s);
            using (SolidBrush b2 = new SolidBrush(vc)) g.FillRectangle(b2, vr.X + 16 * s, by + 11 * s, (float)(bw * benchAfter.Avg / mx), 7 * s);
            string warn = benchAfter.Note.Length > 0 ? benchAfter.Note : benchBefore.Note;
            if (warn.Length > 0) DrawTxt(g, warn, fbSmall, UI.Warn, vr.X + 16 * s, vr.Bottom - 22 * s, vr.Width - 32 * s, 18 * s, false);
        }
        void RunGpuBench(string slot)
        {
            if (busy) return;
            if (!Ask("GPU TEST draws a fixed 3D scene in its own window: 3 s warm-up, then exactly 30 s counted.\n\nClose games and other apps and don't touch the mouse. Esc cancels.\nThe result is saved as " + slot.ToUpper() + ".\n\nStart?")) return;
            SetBusy(true, "GPU TEST");
            BenchRes r = null; string err = "";
            try { WindowState = FormWindowState.Minimized; Thread.Sleep(600); using (GlBenchForm f = new GlBenchForm()) { r = f.Execute(); err = f.Error; } }
            catch (Exception ex) { err = ex.Message; }
            WindowState = FormWindowState.Normal; Activate(); SetBusy(false, "");
            if (r == null) { MessageBox.Show(err.Length > 0 ? err : "Test cancelled - nothing was saved.", "Bu 7maid Zero", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { r.Save(slot); } catch (Exception ex) { MessageBox.Show("Could not save the result: " + ex.Message, "Bu 7maid Zero"); }
            benchBefore = BenchRes.Load("before"); benchAfter = BenchRes.Load("after"); benchPanel.Invalidate();
            string m = slot.ToUpper() + " saved  (" + Num(r.Seconds, "0.0") + " s measured, " + r.Frames.ToString("N0") + " frames):  score " + r.Score.ToString("N0") + ",  average " + Num(r.Avg, "0.0") + " FPS,  1% low " + Num(r.Low1, "0.0") + " FPS.";
            if (r.Note.Length > 0) m += "\n\n" + r.Note;
            MessageBox.Show(m, "GPU TEST", MessageBoxButtons.OK, r.Note.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // ---------- optional FPS before/after around BOOST ----------
        bool autoAfterPending;
        // returns false when the user cancelled the whole BOOST
        bool MeasureBefore()
        {
            autoAfterPending = false;
            if (!Ask("OPTIONAL: measure your PC now (DPC delay 10 s + GPU FPS 15 s, small window) and again right after the restart, then show the difference?\n\nYes = measure.  No = skip it and just BOOST.")) return true;
            WindowState = FormWindowState.Minimized;
            ResultForm rf = ResultForm.ForBeforeFlow(); rf.ShowDialog();
            bool ok = rf.Ok; string err = rf.Err; rf.Dispose();
            WindowState = FormWindowState.Normal; Activate();
            if (!ok) return Ask((err.Length > 0 ? err : "The measurement was cancelled.") + "\n\nContinue BOOST without the before/after comparison?");
            autoAfterPending = true; benchBefore = BenchRes.Load("before"); benchAfter = null; if (benchPanel != null) benchPanel.Invalidate();
            return true;
        }
        void ScheduleAfterTest()
        {
            try
            {
                Directory.CreateDirectory(OcStore.Dir); File.WriteAllText(Path.Combine(OcStore.Dir, "autotest.pending"), "1");
                string exe = Application.ExecutablePath; bool ok = false;
                try
                {
                    ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/create /tn \"Bu7maidZero After Test\" /tr \"\\\"" + exe + "\\\" /after\" /sc onlogon /rl highest /delay 0000:02 /f");
                    q.CreateNoWindow = true; q.UseShellExecute = false; Process pr = Process.Start(q); pr.WaitForExit(10000); ok = pr.ExitCode == 0;
                }
                catch { }
                if (!ok) { try { Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce").SetValue("Bu7maidAfter", "\"" + exe + "\" /after"); } catch { } }
            }
            catch { }
        }

        // ---------- overclock lab (everything is a button - nothing happens by itself) ----------
        Label[] ocInfo = new Label[3]; bool ocLoaded; string ocBoard = "", ocCpuLine = "", ocRamLine = "", ocGpuLine = "";
        void BuildOc()
        {
            DB p = NewPage("oc");
            string[] nm = { "RAM", "CPU", "GPU" };
            for (int i = 0; i < 3; i++)
            {
                int y = 4 + i * 138;
                p.Controls.Add(Lbl(nm[i], 38, y, 80, 26, 13f, FontStyle.Bold, UI.Accent));
                ocInfo[i] = Lbl("reading hardware...", 116, y + 2, 672, 46, 9.5f, FontStyle.Regular, UI.Txt); p.Controls.Add(ocInfo[i]);
                int yb = y + 52;
                ZBtn t = Big(i == 2 ? "AUTO OC" : "TEST " + nm[i], i == 2 ? "safe steps, each one tested, saved" : (i == 0 ? "speed + error check, then compare" : "speed + error check, then compare"), 36, yb, 246, 56, i == 2 ? 1 : 0);
                ZBtn m = Big(i == 2 ? "TEST GPU" : "BIOS STEPS", i == 2 ? "30 s FPS, compare with stock" : "exact path for your board", 289, yb, 246, 56, 0);
                ZBtn c = Big(i == 2 ? "RESET GPU" : "CLEAR RESULTS", i == 2 ? "back to stock clocks now" : "start a new comparison", 542, yb, 246, 56, 0);
                int k = i;
                t.Click += delegate { if (k == 0) OcRamTest(); else if (k == 1) OcCpuTest(); else OcGpuAuto(); };
                m.Click += delegate { if (k == 2) OcGpuTest(); else OcBiosSteps(k); };
                c.Click += delegate { if (k == 2) OcGpuReset(true); else { OcStore.Delete(k == 0 ? "oc_ram.txt" : "oc_cpu.txt"); RefreshOc(); } };
                p.Controls.Add(t); p.Controls.Add(m); p.Controls.Add(c); lockable.Add(t); lockable.Add(m); lockable.Add(c);
            }
            ZBtn all = Big("RESTORE ALL TO NORMAL", "GPU back to stock, saved profile + startup task removed, results cleared", 36, 424, 752, 60, 3);
            all.Click += delegate { OcRestoreAll(); };
            p.Controls.Add(all); lockable.Add(all);
            p.Controls.Add(Lbl("Honest limits: Windows can change the GPU clocks, but RAM and CPU speed can only be changed in the BIOS - here you get the exact BIOS steps and a test that proves the gain and checks for errors. Any error = go back to the default.", 38, 494, 752, 56, 9.5f, FontStyle.Regular, UI.Muted));
        }
        void LoadOcInfo()
        {
            string board = "", cpu = "", ram = "", gpu = "";
            try { foreach (ManagementObject o in Wmi(null, "SELECT Manufacturer, Product FROM Win32_BaseBoard")) { board = ("" + o["Manufacturer"]).Trim() + " " + ("" + o["Product"]).Trim(); break; } } catch { }
            try { foreach (ManagementObject o in Wmi(null, "SELECT Name, MaxClockSpeed FROM Win32_Processor")) { cpu = ("" + o["Name"]).Trim() + "  -  max " + ToL(o["MaxClockSpeed"]) + " MHz"; break; } } catch { }
            try
            {
                long cap = 0, cf = 0, rd = 0; int n = 0;
                foreach (ManagementObject o in Wmi(null, "SELECT Capacity, ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory")) { cap += ToL(o["Capacity"]); cf = ToL(o["ConfiguredClockSpeed"]); rd = ToL(o["Speed"]); n++; }
                if (n > 0) ram = (cap / 1073741824L) + " GB in " + n + " stick(s), running " + cf + " MT/s, rated " + rd + " MT/s" + (cf < rd ? "  -  BELOW rated, turn on XMP/EXPO" : "  -  at rated speed");
            }
            catch { }
            try
            {
                string er; IntPtr h = GpuOc.Open(out er);
                if (h == IntPtr.Zero) gpu = er;
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(96); Nvml.nvmlDeviceGetName(h, sb, 96);
                    uint mx = 0; Nvml.nvmlDeviceGetMaxClockInfo(h, 0, out mx); int c0, m0; bool ok = GpuOc.GetOffsets(h, out c0, out m0);
                    gpu = sb.ToString() + "  -  max " + mx + " MHz  -  " + (ok ? "offset now: core " + c0.ToString("+0;-0;0") + " / memory " + m0.ToString("+0;-0;0") : "clock offsets not readable on this driver");
                }
            }
            catch (Exception ex) { gpu = "GPU info: " + ex.Message; }
            ocBoard = board; ocCpuLine = cpu; ocRamLine = ram; ocGpuLine = gpu; ocLoaded = true;
            try { BeginInvoke(new Action(RefreshOc)); } catch { }
        }
        static string Diff(double b, double n, string unit)
        {
            double pc = b > 0 ? (n - b) / b * 100.0 : 0;
            string v = Math.Abs(pc) < 2.0 ? "same (inside normal test noise)" : (pc > 0 ? "+" + Num(pc, "0.0") + " % faster" : Num(pc, "0.0") + " % slower");
            return Num(b, "0.0") + " -> " + Num(n, "0.0") + " " + unit + "  =  " + v;
        }
        string OcResultLine(string file, string unit, string fmtNote)
        {
            Dictionary<string, string> d = OcStore.Read(file);
            if (!d.ContainsKey("base")) return "no result yet - press TEST on the untouched setup first, change the BIOS, press TEST again.";
            string s = "BEFORE " + Num(OcStore.GetDbl(d, "base", 0), "0.0") + " " + unit + " (errors " + OcStore.GetInt(d, "baseErr", 0) + ")";
            if (d.ContainsKey("now")) s = Diff(OcStore.GetDbl(d, "base", 0), OcStore.GetDbl(d, "now", 0), unit) + "   errors " + OcStore.GetInt(d, "nowErr", 0) + (OcStore.GetInt(d, "nowErr", 0) > 0 ? "  -  UNSTABLE, go back!" : "");
            else s += "  -  now change the BIOS and press TEST again";
            return s;
        }
        void RefreshOc()
        {
            if (!ocLoaded) { return; }
            string gl = ocGpuLine; BenchRes b = BenchRes.Load("ocbase"), n = BenchRes.Load("ocnow");
            if (b != null) gl += "\n" + (n != null ? Diff(b.Avg, n.Avg, "FPS") : "stock test saved: " + Num(b.Avg, "0.0") + " FPS - apply AUTO OC then press TEST GPU");
            ocInfo[0].Text = (ocRamLine.Length > 0 ? ocRamLine : "RAM info not readable") + "\n" + OcResultLine("oc_ram.txt", "GB/s", "");
            ocInfo[1].Text = (ocCpuLine.Length > 0 ? ocCpuLine : "CPU info not readable") + "\n" + OcResultLine("oc_cpu.txt", "pts", "");
            Dictionary<string, string> sp = OcStore.Read(GpuOc.ProfileFile);
            if (OcStore.GetInt(sp, "enabled", 0) == 1) gl += "\nSAVED OC (applies every Windows start): core " + OcStore.GetInt(sp, "core", 0).ToString("+0;-0;0") + ", memory " + OcStore.GetInt(sp, "mem", 0).ToString("+0;-0;0") + (OcStore.GetInt(sp, "pl", 0) > 0 ? ", power " + (OcStore.GetInt(sp, "pl", 0) / 1000) + " W" : "") + "   (" + (sp.ContainsKey("when") ? sp["when"] : "") + ")";
            ocInfo[2].Text = gl;
        }
        void OcRunCpuRam(bool ram)
        {
            string file = ram ? "oc_ram.txt" : "oc_cpu.txt";
            if (!Ask((ram ? "RAM TEST: measures memory speed and checks about 1 GB of memory for errors (about 30 s)." : "CPU TEST: measures CPU speed on all cores and checks every result for errors (about 20 s).") + "\n\nClose games and other apps. The PC may get slow while testing.\nThe first run is saved as BEFORE, later runs are compared with it.\n\nStart?")) return;
            SetBusy(true, ram ? "RAM test" : "CPU test"); ocInfo[ram ? 0 : 1].Text = "testing... leave the PC alone";
            ThreadPool.QueueUserWorkItem(delegate
            {
                double val = 0, single = 0; long err = 0; string fail = "";
                try { if (ram) val = OcTests.Ram(10.0, out err); else { val = OcTests.Cpu(16.0, out single, out err); val = val / 1000.0; } }
                catch (Exception ex) { fail = ex.Message; }
                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        SetBusy(false, "");
                        if (fail.Length > 0) { MessageBox.Show("Test failed: " + fail, "Bu 7maid Zero"); RefreshOc(); return; }
                        Dictionary<string, string> d = OcStore.Read(file); System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                        string slot = d.ContainsKey("base") ? "now" : "base";
                        d[slot] = val.ToString("0.000", ci); d[slot + "Err"] = err.ToString(); OcStore.Write(file, d); RefreshOc();
                        string m = (slot == "base" ? "Saved as BEFORE: " : "Result: ") + Num(val, "0.0") + (ram ? " GB/s" : " pts") + ", errors found: " + err + ".";
                        if (slot == "now") m = Diff(OcStore.GetDbl(d, "base", 0), val, ram ? "GB/s" : "pts") + "\nErrors found: " + err;
                        if (err > 0) m += "\n\nERRORS FOUND - this setting is not stable. Go back to the default in the BIOS now.";
                        MessageBox.Show(m, ram ? "RAM TEST" : "CPU TEST", MessageBoxButtons.OK, err > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    }));
                }
                catch { }
            });
        }
        void OcRamTest() { OcRunCpuRam(true); }
        void OcCpuTest() { OcRunCpuRam(false); }
        void OcBiosSteps(int which)
        {
            string b = ocBoard.ToUpper(); string xmp, cpu;
            if (b.Contains("ASUS")) { xmp = "Press Del at boot > F7 (Advanced) > Ai Tweaker > Ai Overclock Tuner = XMP I / EXPO."; cpu = "Ai Tweaker > Intel Performance Profile / Asus MultiCore Enhancement = Intel Default Settings (stable)."; }
            else if (b.Contains("MSI")) { xmp = "Press Del at boot > OC tab > A-XMP / EXPO = Enabled, pick Profile 1."; cpu = "OC tab > keep CPU Lite Load / Intel Default Settings (do not add manual voltage)."; }
            else if (b.Contains("GIGABYTE") || b.Contains("AORUS")) { xmp = "Press Del at boot > Tweaker > Extreme Memory Profile (XMP) = Profile 1."; cpu = "Tweaker > Intel Default Settings / Performance Profile (not Extreme)."; }
            else if (b.Contains("ASROCK")) { xmp = "Press F2 at boot > OC Tweaker > Load XMP Setting = XMP 1."; cpu = "OC Tweaker > CPU Configuration > Intel Default Settings."; }
            else { xmp = "In the BIOS find the memory profile (called XMP, EXPO or DOCP) and choose Profile 1."; cpu = "Choose the 'Intel Default Settings' or 'Performance' profile only."; }
            string m;
            if (which == 0) m = "RAM - your board: " + (ocBoard.Length > 0 ? ocBoard : "unknown") + "\r\n\r\n1. Press TEST RAM first (this is your BEFORE).\r\n2. Restart into the BIOS.\r\n3. " + xmp + "\r\n4. Leave voltage on Auto. Save and exit (F10).\r\n5. Back in Windows press TEST RAM again: you get the % difference and the error count.\r\n\r\nIf the PC will not start or the test shows ANY error: BIOS > Load Optimized Defaults (F5) or clear CMOS.";
            else m = "CPU - your board: " + (ocBoard.Length > 0 ? ocBoard : "unknown") + "\r\n\r\n1. Update the BIOS to the newest version first (it carries the Intel microcode fix for 13th/14th gen stability).\r\n2. " + cpu + "\r\n3. Do NOT raise CPU voltage or force an all-core multiplier on a 14th-gen chip - this is the main cause of crashes and wear.\r\n4. Press TEST CPU before and after the change to see the gain and the error count.\r\n\r\nAny error = BIOS > Load Optimized Defaults.";
            MessageBox.Show(m, "BIOS STEPS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        BenchRes OcBench(string title, double warm, double dur, bool stress)
        {
            string res = BenchRes.PathFor("oc_child"), errF = BenchRes.PathFor("oc_child_err"), stF = BenchRes.PathFor("oc_child_start");
            try { File.Delete(res); File.Delete(errF); File.Delete(stF); } catch { }
            BenchRes r = null;
            try
            {
                WindowState = FormWindowState.Minimized; Thread.Sleep(400);
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath, "/octest " + (stress ? "1" : "0") + " " + warm.ToString("0.0", ci) + " " + dur.ToString("0.0", ci) + " \"" + title.Replace("\"", "") + "\"");
                psi.UseShellExecute = false;
                Process pr = Process.Start(psi); Stopwatch sw = Stopwatch.StartNew(); bool hung = false;
                while (!pr.WaitForExit(120)) { Application.DoEvents(); if (sw.Elapsed.TotalSeconds > warm + dur + 120) { hung = true; try { pr.Kill(); } catch { } break; } }
                r = BenchRes.Load("oc_child");
                if (r == null)
                {
                    string err = File.Exists(errF) ? File.ReadAllText(errF).Trim() : "";
                    if (err.Length > 0) ocLastErr = err;
                    else if (hung || File.Exists(stF)) ocLastErr = "The test program was stopped by the graphics driver (driver reset).";
                    else ocLastErr = "The test program could not start.";
                }
            }
            catch (Exception ex) { ocLastErr = ex.Message; }
            WindowState = FormWindowState.Normal; Activate();
            return r;
        }
        string ocLastErr = "";
        // writes a marker first: if the PC freezes during this step, the next AUTO OC run knows exactly where and stays below it
        BenchRes OcTrial(string kind, int core, int mem, string title, double warm, double dur)
        {
            ocLastErr = ""; Dictionary<string, string> d = new Dictionary<string, string>(); d["kind"] = kind; d["core"] = core.ToString(); d["mem"] = mem.ToString(); OcStore.Write("oc_trial.txt", d);
            BenchRes r = OcBench(title, warm, dur, true); OcStore.Delete("oc_trial.txt"); return r;
        }
        void OcGpuTest()
        {
            if (!Ask("GPU TEST: 30 s fixed scene. The first run is saved as STOCK, later runs (after AUTO OC) are compared with it.\n\nClose games and other apps.\n\nStart?")) return;
            SetBusy(true, "GPU test"); ocLastErr = "";
            BenchRes r = OcBench("GPU TEST", 3.0, 30.0, false); SetBusy(false, "");
            if (r == null) { MessageBox.Show(ocLastErr.Length > 0 ? ocLastErr : "Cancelled - nothing saved.", "Bu 7maid Zero"); return; }
            string slot = BenchRes.Load("ocbase") == null ? "ocbase" : "ocnow";
            try { r.Save(slot); } catch { }
            RefreshOc();
            BenchRes b = BenchRes.Load("ocbase"); string m = slot == "ocbase" ? "Saved as STOCK: " + Num(r.Avg, "0.0") + " FPS, 1% low " + Num(r.Low1, "0.0") + "." : Diff(b.Avg, r.Avg, "FPS") + "\n1% low: " + Num(b.Low1, "0.0") + " -> " + Num(r.Low1, "0.0") + "\nReadings: max " + r.TempMax + " C, core avg " + r.ClockAvg + " MHz, memory avg " + r.MemClkAvg + " MHz, peak power " + r.PowerMax + " W.";
            if (r.Note.Length > 0) m += "\n\n" + r.Note;
            MessageBox.Show(m, "GPU TEST", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        void OcGpuReset(bool ask)
        {
            if (ask && !Ask("Put the GPU back to stock clocks and remove the saved OC profile?")) return;
            string er; IntPtr h = GpuOc.Open(out er); int rc = 0;
            if (h != IntPtr.Zero) { rc = GpuOc.SetOffsets(h, 0, 0); GpuOc.DefaultPower(h); }
            OcStore.Delete(GpuOc.ProfileFile); OcStore.Delete(GpuOc.GuardFile); GpuOc.RemoveLogonTask();
            if (ask) MessageBox.Show(h == IntPtr.Zero ? "Saved profile and startup task removed. (GPU not reachable now: " + er + " - clocks are already stock after any restart.)" : (rc == 0 ? "GPU is back to stock clocks. Saved profile removed." : "Profile removed, but the driver refused the reset: " + GpuOc.Why(rc) + " A restart also returns the GPU to stock."), "Bu 7maid Zero");
            ThreadPool.QueueUserWorkItem(delegate { LoadOcInfo(); });
        }
        OcRedForm ocRed;
        void RedShow(string m) { try { if (ocRed == null || ocRed.IsDisposed) { ocRed = new OcRedForm(); ocRed.Show(); } ocRed.Msg(m); } catch { } }
        void RedHide() { try { if (ocRed != null) { ocRed.Close(); ocRed.Dispose(); ocRed = null; } } catch { } }
        void OcStatus(int lvl, string title, string msg)
        {
            RedHide(); try { SetBusy(false, ""); } catch { }
            Tg.Send("[AUTO OC] " + (lvl == 0 ? "ALL DONE" : lvl == 1 ? "PARTLY / WARNING" : "FAILED") + " - " + title + "\nGPU: " + Tg.Gpu() + "\n\n" + msg);
            try { using (OcStatusForm f = new OcStatusForm(lvl, title, msg)) f.ShowDialog(); } catch { MessageBox.Show(msg, title); }
            ThreadPool.QueueUserWorkItem(delegate { LoadOcInfo(); });
        }
        string GpuNames()
        {
            try { List<string> n = new List<string>(); foreach (ManagementObject o in Wmi(null, "SELECT Name FROM Win32_VideoController")) { string s = Convert.ToString(o["Name"]); if (s.Length > 0) n.Add(s); } return string.Join(" + ", n.ToArray()); } catch { return "unknown"; }
        }
        string OcNullReason()
        {
            string e = ocLastErr.ToLower();
            if (e.IndexOf("cancel") >= 0) return "cancel";
            if (e.IndexOf("(driver reset)") >= 0) return "driver";
            return "other";
        }
        void OcAdjustSoak()
        {
            Dictionary<string, string> pg = OcStore.Read("oc_progress.txt"); int sc = OcStore.GetInt(pg, "soakC", 0), sm = OcStore.GetInt(pg, "soakM", 0);
            if (sm > 0) sm = sm >= 200 ? sm - 200 : 0; else if (sc > 0) sc = sc >= 25 ? sc - 25 : 0;
            pg["soakC"] = sc.ToString(); pg["soakM"] = sm.ToString(); OcStore.Write("oc_progress.txt", pg);
        }
        void OcAddCeiling(string kind, int core, int mem)
        {
            Dictionary<string, string> cl = OcStore.Read("oc_ceiling.txt");
            if (kind == "core" || kind == "soak") cl["core"] = Math.Min(OcStore.GetInt(cl, "core", 100000), core).ToString();
            if (kind == "mem" || kind == "soak") cl["mem"] = Math.Min(OcStore.GetInt(cl, "mem", 100000), mem).ToString();
            OcStore.Write("oc_ceiling.txt", cl);
        }
        void OcFinishAuto() { OcStore.Delete("oc_progress.txt"); OcStore.Delete("oc_trial.txt"); GpuOc.RemoveResumeTask(); }
        // the driver reset: remember where, restart Windows, and AUTO OC continues by itself after the restart
        void OcDriverReset(IntPtr h, string kind, int core, int mem)
        {
            OcAddCeiling(kind, core, mem); if (kind == "soak") OcAdjustSoak();
            try { GpuOc.SetOffsets(h, 0, 0); GpuOc.DefaultPower(h); } catch { }
            SetBusy(false, "");
            try { ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/r /t 20 /c \"Bu 7maid Zero: the graphics driver reset - restarting, AUTO OC continues by itself\""); q.CreateNoWindow = true; q.UseShellExecute = false; Process.Start(q); } catch { }
            RedShow("The graphics driver reset at " + (kind == "mem" ? "memory +" + mem : "core +" + core) + " (this card's limit area).\n\nThe PC restarts in 20 seconds. AUTO OC then continues by itself\nfrom where it stopped and stays below this point.\nWait - do not press anything.");
        }
        void OcGpuAuto() { OcGpuAutoRun(false); }
        void OcGpuAutoRun(bool byTask)
        {
            Dictionary<string, string> pg = OcStore.Read("oc_progress.txt"); bool resuming = OcStore.GetInt(pg, "active", 0) == 1;
            if (byTask && !resuming) { GpuOc.RemoveResumeTask(); return; }
            if (byTask) RedShow("AUTO OC continues by itself after the restart.\nIt stays RED until it is finished.\nDon't touch the mouse, keyboard or any window.");
            int rsCount = OcStore.GetInt(pg, "resumes", 0) + 1;
            if (resuming)
            {
                int rs = rsCount;
                if (!byTask && !Ask("A previous AUTO OC run was interrupted. It continues from where it stopped (core +" + OcStore.GetInt(pg, "core", 0) + ", memory +" + OcStore.GetInt(pg, "mem", 0) + ") and does not start over.\n\nContinue?\n(No = leave it, nothing is changed)")) return;
                if (rs > 6) { OcFinishAuto(); try { string e0; IntPtr h0 = GpuOc.Open(out e0); if (h0 != IntPtr.Zero) { GpuOc.SetOffsets(h0, 0, 0); GpuOc.DefaultPower(h0); } } catch { } OcStatus(2, "AUTO OC gave up", "AUTO OC was interrupted too many times, so it stopped and the card stays at stock.\nTell me what happened and I will lower the limits."); return; }
                pg["resumes"] = rs.ToString();
            }
            else if (!Ask("Card: " + GpuNames() + "\n\nAUTO OC finds the highest SAFE setting for this card, judged by stability - not by the FPS of one test (many games gain where a simple test does not).\n\n- power limit up to the card's own factory maximum\n- core clock climbs as high as the card holds (fine steps near the top), then memory; each step is stressed for 10 s with a heavy game-like scene at your full screen resolution plus a picture-correctness check\n- then a FULL 45 s stability test\n\nIt starts from your saved overclock (if you have one) and climbs from there. If the driver resets or the PC restarts, a RED screen \"don't touch anything\" shows and AUTO OC CONTINUES from where it stopped - it never starts over.\nSafety: everything is lost by itself on any restart; if a game or the driver crashes while the OC is on, the saved OC LOWERS ITSELF at the next start (no need to run AUTO OC again); if the PC crashes soon after applying it, it is switched off automatically.\nAbout 5-8 minutes. Close games and other apps. Esc cancels.\n\nStart?")) return;
            string er; IntPtr h = GpuOc.Open(out er);
            if (h == IntPtr.Zero) { OcFinishAuto(); OcStatus(1, "AUTO OC not available", "Your card: " + GpuNames() + "\n\n" + er + "\n\nAUTO OC works through the NVIDIA driver, so it needs an NVIDIA card. The FPS tweaks from BOOST still apply to every card."); return; }
            bool offOk = GpuOc.SetOffsets(h, 0, 0) == 0; int offWhy = offOk ? 0 : GpuOc.SetOffsets(h, 0, 0);
            int capC, capM; GpuOc.Range(h, out capC, out capM);
            Dictionary<string, string> tr = OcStore.Read("oc_trial.txt");
            if (tr.Count > 0)
            {
                string kd = tr.ContainsKey("kind") ? tr["kind"] : ""; int tcc = OcStore.GetInt(tr, "core", 0), tmm = OcStore.GetInt(tr, "mem", 0);
                OcAddCeiling(kd, tcc, tmm); if (kd == "soak") { pg = OcStore.Read("oc_progress.txt"); OcAdjustSoak(); pg = OcStore.Read("oc_progress.txt"); if (resuming) pg["resumes"] = rsCount.ToString(); }
                OcStore.Delete("oc_trial.txt");
            }
            Dictionary<string, string> ceil = OcStore.Read("oc_ceiling.txt"); int ceilC = OcStore.GetInt(ceil, "core", 100000), ceilM = OcStore.GetInt(ceil, "mem", 100000);
            SetBusy(true, "GPU auto OC"); ocLastErr = "";
            int goodCore = 0, goodMem = 0, soakC = 0, soakM = 0; string phase = "core", stop = ""; bool useArt = true; double floor = 0; uint plDef = 0, plNow = 0;
            BenchRes fpsBase = null;
            if (resuming)
            {
                goodCore = OcStore.GetInt(pg, "core", 0); goodMem = OcStore.GetInt(pg, "mem", 0); phase = pg.ContainsKey("phase") ? pg["phase"] : "core";
                soakC = OcStore.GetInt(pg, "soakC", 0); soakM = OcStore.GetInt(pg, "soakM", 0); useArt = OcStore.GetInt(pg, "useArt", 1) == 1; floor = OcStore.GetDbl(pg, "floor", 0); plNow = (uint)OcStore.GetInt(pg, "pl", 0);
                uint dd; try { Nvml.nvmlDeviceGetPowerManagementDefaultLimit(h, out dd); plDef = dd; } catch { }
                if (plNow > 0) { try { Nvml.nvmlDeviceSetPowerManagementLimit(h, plNow); } catch { } }
                fpsBase = BenchRes.Load("ocbase");
                if (goodCore > 0 && offOk) GpuOc.SetOffsets(h, goodCore, goodMem);
            }
            else
            {
                GpuOc.AddResumeTask(Application.ExecutablePath);
                Dictionary<string, string> prof = OcStore.Read(GpuOc.ProfileFile); BenchRes pb = BenchRes.Load("ocbase");
                bool fromProf = OcStore.GetInt(prof, "enabled", 0) == 1 && OcStore.GetInt(prof, "floorOk", 0) == 1 && pb != null && (OcStore.GetInt(prof, "core", 0) > 0 || OcStore.GetInt(prof, "mem", 0) > 0);
                if (fromProf)
                {
                    fpsBase = pb; goodCore = OcStore.GetInt(prof, "core", 0); goodMem = OcStore.GetInt(prof, "mem", 0); useArt = OcStore.GetInt(prof, "art", 1) == 1; floor = OcStore.GetDbl(prof, "floor", 0);
                    plNow = GpuOc.RaisePower(h, out plDef); if (offOk) GpuOc.SetOffsets(h, goodCore, goodMem);
                }
                else
                {
                    fpsBase = OcBench("FPS reference - stock", 2.0, 16.0, false);
                    if (fpsBase == null) { OcStatus(OcNullReason() == "cancel" ? 1 : 2, "AUTO OC stopped", ocLastErr.Length > 0 ? ocLastErr : "Cancelled - nothing changed."); OcFinishAuto(); return; }
                    try { fpsBase.Save("ocbase"); File.Delete(BenchRes.PathFor("ocnow")); } catch { }
                    BenchRes bas = OcTrial("ref", 0, 0, "STRESS reference - stock", 1.5, 10.0);
                    if (bas == null) { OcStatus(OcNullReason() == "cancel" ? 1 : 2, "AUTO OC stopped", ocLastErr.Length > 0 ? ocLastErr : "Cancelled - nothing changed."); OcFinishAuto(); return; }
                    useArt = bas.Artifacts == 0; floor = bas.Avg * 0.975; plNow = GpuOc.RaisePower(h, out plDef);
                }
            }
            Action save = delegate
            {
                pg["active"] = "1"; pg["core"] = goodCore.ToString(); pg["mem"] = goodMem.ToString(); pg["phase"] = phase; pg["soakC"] = soakC.ToString(); pg["soakM"] = soakM.ToString();
                pg["useArt"] = useArt ? "1" : "0"; pg["floor"] = floor.ToString("R", System.Globalization.CultureInfo.InvariantCulture); pg["pl"] = plNow.ToString(); OcStore.Write("oc_progress.txt", pg);
            };
            save();
            if (!resuming && plNow > 0)
            {
                BenchRes r0 = OcTrial("pl", 0, 0, "STRESS  power limit " + (plNow / 1000) + " W", 1.0, 10.0);
                if (r0 == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "pl", 0, 0); return; } }
                if (r0 == null || (useArt && r0.Artifacts > 0) || r0.Avg < floor) { GpuOc.DefaultPower(h); plNow = 0; save(); }
            }
            List<int> csl = new List<int>(), msl = new List<int>(); if (goodCore > 0) csl.Add(goodCore + 15); for (int v = 50; v <= 600; v += 50) if (v > goodCore + 15) csl.Add(v); if (goodMem > 0) msl.Add(goodMem + 100); for (int v = 200; v <= 2000; v += 200) if (v > goodMem + 100) msl.Add(v);
            int[] coreSteps = csl.ToArray(); int[] memSteps = msl.ToArray();
            if (phase == "core")
            {
                int failC = 100000;
                if (offOk)
                {
                    for (int i = 0; i < coreSteps.Length && stop.Length == 0; i++)
                    {
                        if (coreSteps[i] <= goodCore) continue;
                        if (coreSteps[i] >= ceilC) { failC = ceilC; stop = "stayed below the point where it crashed (core +" + ceilC + ")"; break; }
                        if (capC > 0 && coreSteps[i] > capC) { stop = "reached the driver's core limit (+" + capC + ")"; break; }
                        if (GpuOc.SetOffsets(h, coreSteps[i], goodMem) != 0) { stop = "driver refused core +" + coreSteps[i]; break; }
                        BenchRes r = OcTrial("core", coreSteps[i], goodMem, "STRESS  core +" + coreSteps[i], 1.0, 10.0);
                        uint t = 0; Nvml.nvmlDeviceGetTemperature(h, 0, out t);
                        if (r == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "core", coreSteps[i], goodMem); return; } failC = coreSteps[i]; stop = "aborted at core +" + coreSteps[i]; break; }
                        if (useArt && r.Artifacts > 0) { failC = coreSteps[i]; stop = "picture errors at core +" + coreSteps[i]; break; }
                        if (r.Avg < floor || r.TempMax > 86 || t > 86) { failC = coreSteps[i]; stop = "core +" + coreSteps[i] + " too hot or throttling (" + r.TempMax + " C)"; break; }
                        goodCore = coreSteps[i]; save();
                    }
                    // fine steps: halve the distance between the last good value and the first bad one
                    for (int it = 0; it < 5 && failC < 100000 && failC - goodCore > 5; it++)
                    {
                        int mid = ((goodCore + failC) / 2 / 5) * 5; if (mid <= goodCore || mid >= failC) break;
                        if (GpuOc.SetOffsets(h, mid, goodMem) != 0) break;
                        BenchRes rr = OcTrial("core", mid, goodMem, "STRESS  core +" + mid + "  (fine step)", 1.0, 10.0);
                        if (rr == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "core", mid, goodMem); return; } failC = mid; continue; }
                        if (!(useArt && rr.Artifacts > 0) && rr.Avg >= floor && rr.TempMax <= 86) { goodCore = mid; save(); } else failC = mid;
                    }
                }
                phase = "mem"; save();
            }
            string stopMem = "";
            if (phase == "mem")
            {
                int failM = 100000;
                if (offOk)
                    for (int i = 0; i < memSteps.Length && stopMem.Length == 0; i++)
                    {
                        if (memSteps[i] <= goodMem) continue;
                        if (memSteps[i] >= ceilM) { failM = ceilM; stopMem = "stayed below the point where it crashed (memory +" + ceilM + ")"; break; }
                        if (capM > 0 && memSteps[i] > capM) { stopMem = "reached the driver's memory limit (+" + capM + ")"; break; }
                        if (GpuOc.SetOffsets(h, goodCore, memSteps[i]) != 0) { stopMem = "driver refused memory +" + memSteps[i]; break; }
                        BenchRes r = OcTrial("mem", goodCore, memSteps[i], "STRESS  memory +" + memSteps[i], 1.0, 10.0);
                        if (r == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "mem", goodCore, memSteps[i]); return; } failM = memSteps[i]; stopMem = "aborted at memory +" + memSteps[i]; break; }
                        if (useArt && r.Artifacts > 0) { failM = memSteps[i]; stopMem = "picture errors at memory +" + memSteps[i]; break; }
                        if (r.Avg < floor || r.TempMax > 86) { failM = memSteps[i]; stopMem = "memory +" + memSteps[i] + " too hot or throttling (" + r.TempMax + " C)"; break; }
                        goodMem = memSteps[i]; save();
                    }
                // fine steps for memory too
                for (int it = 0; it < 4 && offOk && failM < 100000 && failM - goodMem > 20; it++)
                {
                    int mid = ((goodMem + failM) / 2 / 10) * 10; if (mid <= goodMem || mid >= failM) break;
                    if (GpuOc.SetOffsets(h, goodCore, mid) != 0) break;
                    BenchRes rr = OcTrial("mem", goodCore, mid, "STRESS  memory +" + mid + "  (fine step)", 1.0, 10.0);
                    if (rr == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "mem", goodCore, mid); return; } failM = mid; continue; }
                    if (!(useArt && rr.Artifacts > 0) && rr.Avg >= floor && rr.TempMax <= 86) { goodMem = mid; save(); } else failM = mid;
                }
                soakC = goodCore >= 50 ? goodCore * 70 / 100 / 5 * 5 : goodCore; soakM = goodMem >= 200 ? goodMem * 50 / 100 / 20 * 20 : goodMem; phase = "soak"; save();
            }
            if (stopMem.Length > 0) stop = stop + (stop.Length > 0 ? "; " : "") + stopMem;
            int finC = soakC, finM = soakM; bool any = false; BenchRes fin = null; string soakMsg = "";
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (offOk) GpuOc.SetOffsets(h, finC, finM);
                any = finC > 0 || finM > 0 || plNow > 0; if (!any) break;
                soakC = finC; soakM = finM; save();
                fin = OcTrial("soak", finC, finM, "FULL STABILITY TEST 45 s  " + (finC > 0 ? "core " + finC.ToString("+0") : "") + (finM > 0 ? "  mem " + finM.ToString("+0") : ""), 2.0, 45.0);
                if (fin == null) { string nr = OcNullReason(); if (nr == "cancel") { OcAbortAuto(h); return; } if (nr == "driver") { OcDriverReset(h, "soak", finC, finM); return; } }
                bool bad = fin == null || (useArt && fin.Artifacts > 0) || fin.TempMax > 88 || fin.Avg < floor;
                if (!bad) break;
                soakMsg += (soakMsg.Length > 0 ? "; " : "") + (fin == null ? "aborted" : (useArt && fin.Artifacts > 0 ? "picture errors " + fin.Artifacts : fin.TempMax > 88 ? "too hot " + fin.TempMax + " C" : "throttling")) + " at core " + finC.ToString("+0;-0;0") + " / mem " + finM.ToString("+0;-0;0");
                fin = null;
                if (finM > 0) finM = finM >= 200 ? finM - 200 : 0; else if (finC > 0) finC = finC >= 25 ? finC - 25 : 0; else if (plNow > 0) { GpuOc.DefaultPower(h); plNow = 0; } else break;
            }
            BenchRes fpsNow = (fin != null && any) ? OcBench("FPS reference - with OC", 2.0, 16.0, false) : null;
            SetBusy(false, "");
            OcFinishAuto();
            if (!any || fin == null)
            {
                GpuOc.SetOffsets(h, 0, 0); GpuOc.DefaultPower(h); OcStore.Delete(GpuOc.ProfileFile); GpuOc.RemoveLogonTask();
                OcStatus(2, "AUTO OC: nothing saved", "Your card: " + GpuNames() + "\n\n" + (offOk ? "" : "Clock offsets: " + GpuOc.Why(offWhy) + "\n") + "No overclock passed the full stability test with FPS held up - everything stays at stock (nothing is lowered, nothing is broken).\n" + (stop.Length > 0 ? "First problem: " + stop + ".\n" : "") + (soakMsg.Length > 0 ? "Full test: " + soakMsg + ".\n" : ""));
                return;
            }
            try { if (fpsNow != null) fpsNow.Save("ocnow"); } catch { }
            Dictionary<string, string> p = new Dictionary<string, string>(); p["enabled"] = "1"; p["core"] = finC.ToString(); p["mem"] = finM.ToString(); p["pl"] = plNow.ToString(); p["floor"] = floor.ToString("R", System.Globalization.CultureInfo.InvariantCulture); p["art"] = useArt ? "1" : "0"; p["floorOk"] = "1"; p["chk"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.000'Z'"); p["when"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            OcStore.Write(GpuOc.ProfileFile, p); bool tk = GpuOc.AddLogonTask(Application.ExecutablePath);
            int c1 = 0, m1 = 0; bool rb = offOk && GpuOc.GetOffsets(h, out c1, out m1);
            string doneMsg = "AUTO OC done - it passed the full stability test.\n\n" + (plNow > 0 ? "Power limit: " + (plDef / 1000) + " W -> " + (plNow / 1000) + " W (card's own maximum).\n" : "") + (offOk ? "Core " + finC.ToString("+0;-0;0") + " MHz, memory " + finM.ToString("+0;-0;0") + " (driver reads back " + (rb ? c1 + " / " + m1 : "n/a") + ").\n" : "Clock offsets: " + GpuOc.Why(offWhy) + "\n") + "\nStability: 45 s full test at your native resolution, " + fin.Checks + " picture checks, " + fin.Artifacts + " errors.\nReadings: max " + fin.TempMax + " C, core avg " + fin.ClockAvg + " MHz, memory avg " + fin.MemClkAvg + " MHz, peak power " + fin.PowerMax + " W, load " + fin.UtilAvg + " %.\n" + (fpsNow != null && fpsBase != null ? "Simple FPS test: " + Diff(fpsBase.Avg, fpsNow.Avg, "FPS") + "  (this test is light - real games usually gain more or less than this)" : "") + (stop.Length > 0 ? "\nSteps stopped at: " + stop + "." : "") + (soakMsg.Length > 0 ? "\nStepped down after: " + soakMsg + "." : "") + "\n\n" + (tk ? "It is re-applied at every Windows start and switches itself off if the PC crashes after applying it." : "Could not create the startup task - it will be lost at restart.") + "\nRESET GPU or RESTORE ALL TO NORMAL undoes it.";
            if (fpsBase != null && fpsNow != null)
            {
                ResultForm rf = new ResultForm(fpsBase, fpsNow, 0); rf.Heading = "AUTO OC  -  stock vs overclocked"; rf.TagB = "STOCK"; rf.TagA = "WITH OC";
                rf.Info = "Reached: core " + finC.ToString("+0;-0;0") + " MHz, memory " + finM.ToString("+0;-0;0") + (plNow > 0 ? ", power " + (plNow / 1000) + " W" : "") + "  -  stable, " + fin.Artifacts + " errors, max " + fin.TempMax + " C";
                RedHide(); rf.ShowDialog(); rf.Dispose();
            }
            OcStatus(offOk ? 0 : 1, offOk ? "ALL DONE  -  SAVED" : "DONE (partly)  -  SAVED", doneMsg);
        }
        void OcAbortAuto(IntPtr h)
        {
            try { GpuOc.SetOffsets(h, 0, 0); GpuOc.DefaultPower(h); } catch { }
            SetBusy(false, ""); OcFinishAuto();
            OcStatus(1, "AUTO OC cancelled", "The card is back at stock and nothing was saved.");
        }
        void OcRestoreAll()
        {
            if (!Ask("Put everything back to normal?\n\n- GPU clocks back to stock, saved profile and startup task removed\n- all OC test results cleared\n\nRAM and CPU settings live in the BIOS: if you changed them there, use BIOS > Load Optimized Defaults.\n\nContinue?")) return;
            OcGpuReset(false);
            OcStore.Delete("oc_ram.txt"); OcStore.Delete("oc_cpu.txt"); OcStore.Delete("oc_ceiling.txt"); OcStore.Delete("oc_trial.txt"); OcStore.Delete("oc_progress.txt"); GpuOc.RemoveResumeTask();
            try { File.Delete(BenchRes.PathFor("ocbase")); File.Delete(BenchRes.PathFor("ocnow")); } catch { }
            MessageBox.Show("Done. GPU is at stock, the saved overclock and its startup task are removed, test results are cleared.\n\nFor RAM / CPU: BIOS > Load Optimized Defaults (F5) if you changed anything there.", "Bu 7maid Zero");
        }

        // ---------- tools ----------
        void BuildTools()
        {
            DB p = NewPage("tools");
            ZBtn clean = Big("CLEAN", "temp + RAM + free disk space (old updates, dumps, logs)", 36, 10, 370, 88, 0);
            clean.Click += delegate { RunHiddenTask("CLEAN", "Cleaning disk space"); };
            p.Controls.Add(clean); lockable.Add(clean);
            ZBtn ram = Big("RAM CLEAN", "free RAM right now - safe, use anytime", 420, 10, 368, 88, 0);
            ram.Click += delegate { RunHiddenTask("RAM", "Cleaning RAM"); };
            p.Controls.Add(ram); lockable.Add(ram);
            ZBtn scan = Big("VIRUS SCAN", "quick scan + remove, built-in engine", 36, 110, 370, 88, 0);
            scan.Click += delegate { if (Ask("Runs a quick virus scan with the built-in Microsoft engine and removes what it finds.\n\nStart?")) RunHiddenTask("SCAN", "Scanning for viruses", false, "Scan finished. Anything found was removed."); };
            p.Controls.Add(scan); lockable.Add(scan);
            ZBtn nav = Big("FIX EXPLORER", "brings back the File Explorer left panel (Home, Gallery, Desktop...)", 36, 210, 752, 76, 0);
            nav.Click += delegate { RunHiddenTask("NAVFIX", "Fixing File Explorer", false, "Done. Open a new File Explorer window."); };
            p.Controls.Add(nav); lockable.Add(nav);
            ZBtn panel = Big("GPU COLORS", "NVIDIA / AMD settings for stronger colors", 420, 110, 368, 88, 0);
            panel.Click += delegate { ShowColorGuide(); };
            p.Controls.Add(panel);
            ZBtn vf = Big("FORCE VBS OFF", "use if STATUS still says RESTART PC after a restart (Windows lock)", 36, 296, 752, 76, 0);
            vf.Click += delegate { if (Ask("Windows locked VBS. This sets one extra boot step: on the NEXT restart a Windows screen appears before the logo asking you to confirm turning it off - press the key it shows.\n\nThis is Microsoft's own method. Continue?")) RunHiddenTask("VBSFIX", "Forcing VBS off", true, "Done. Now RESTART the PC (Restart, not Shut down). Read the screen before the logo and press the key it asks."); };
            p.Controls.Add(vf); lockable.Add(vf);
            p.Controls.Add(Lbl("BOOST already sets NVIDIA vibrance and Low Latency for you. These are the few panel settings Windows can't change for you.", 38, 384, 752, 44, 9.5f, FontStyle.Regular, UI.Muted));
        }
        void ShowColorGuide()
        {
            MessageBox.Show(
                "STRONGER COLORS (30 seconds, zero FPS cost)\r\n\r\n" +
                "NVIDIA Control Panel > Display > Change resolution:\r\n" +
                "   - Output dynamic range = FULL  (Limited makes blacks grey)\r\n" +
                "   - Output color depth = highest (10 bpc if shown)\r\n" +
                "NVIDIA > Display > Adjust desktop color settings:\r\n" +
                "   - Digital Vibrance = 60-70%\r\n\r\n" +
                "AMD Adrenalin > Display:\r\n" +
                "   - Pixel Format = RGB 4:4:4 Full RGB\r\n" +
                "   - Color Saturation = 110-115%\r\n\r\n" +
                "Monitor menu: set the refresh rate to its maximum.",
                "GPU colors", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- overlay (hidden task progress) ----------
        void BuildOverlay()
        {
            overlay = new DB(); overlay.SetBounds(0, 0, 1060, 700); overlay.BackColor = Color.FromArgb(230, 9, 10, 13); overlay.Visible = false;
            overlay.Paint += delegate (object s, PaintEventArgs e) {
                Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
                RectangleF r = new RectangleF(40 * sc, 60 * sc, 980 * sc, 580 * sc);
                using (GraphicsPath p = UI.Round(r, 16 * sc)) using (SolidBrush b = new SolidBrush(UI.Card)) g.FillPath(b, p);
                using (GraphicsPath p = UI.Round(r, 16 * sc)) using (Pen pen = new Pen(UI.Glow(), 1.6f * sc)) g.DrawPath(pen, p);
            };
            Controls.Add(overlay); overlay.BringToFront();
            taskTitle = Lbl("Working...", 300, 86, 690, 30, 15f, FontStyle.Bold, UI.Txt); taskTitle.BackColor = Color.Transparent; overlay.Controls.Add(taskTitle);
            taskPct = new DB(); taskPct.SetBounds(60, 110, 210, 280); taskPct.BackColor = Color.Transparent;
            taskPct.Paint += delegate (object s, PaintEventArgs e) {
                Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; float sc = UI.Dpi(g);
                float d = Math.Min(taskPct.Width - 12 * sc, 190 * sc); RectangleF rr = new RectangleF((taskPct.Width - d) / 2f, 8 * sc, d, d);
                using (Pen tr = new Pen(UI.Card2, 12 * sc)) g.DrawEllipse(tr, rr);
                using (Pen ap = new Pen(UI.Glow(), 12 * sc) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    if (taskPercent < 0) g.DrawArc(ap, rr, UI.Phase * 720f, 100f);
                    else if (taskPercent > 0) g.DrawArc(ap, rr, -90f, Math.Min(359.5f, taskPercent * 3.6f));
                }
                using (Font f = new Font("Segoe UI Black", 30f)) TextRenderer.DrawText(g, taskPercent >= 0 ? taskPercent + "%" : "...", f, new Rectangle((int)rr.X, (int)rr.Y, (int)rr.Width, (int)rr.Height), UI.Txt, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using (Font f2 = new Font("Segoe UI Semibold", 9.5f))
                {
                    int ok = 0; for (int i = 0; i < taskLog.Count; i++) if (!taskLog[i].StartsWith("!")) ok++;
                    TextRenderer.DrawText(g, ok + " things done", f2, new Rectangle(0, (int)(rr.Bottom + 12 * sc), taskPct.Width, (int)(22 * sc)), UI.Good, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, "full list on the right", f2, new Rectangle(0, (int)(rr.Bottom + 34 * sc), taskPct.Width, (int)(22 * sc)), UI.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                }
            };
            overlay.Controls.Add(taskPct);
            taskStep = Lbl("please wait", 300, 126, 690, 22, 10f, FontStyle.Regular, UI.Muted); taskStep.BackColor = Color.Transparent; overlay.Controls.Add(taskStep);
            taskBar = new ZBar(); taskBar.SetBounds(300, 158, 688, 10); overlay.Controls.Add(taskBar);
            taskLogView = new DB(); taskLogView.SetBounds(300, 184, 688, 392); taskLogView.BackColor = Color.Transparent;
            taskLogView.MouseEnter += delegate { taskLogView.Focus(); };
            taskLogView.MouseWheel += delegate (object s3, MouseEventArgs e3) {
                const int cap0 = 14; int pages0 = Math.Max(1, (taskLog.Count + cap0 - 1) / cap0);
                int cur = logPageSel >= 0 ? logPageSel : pages0 - 1;
                cur += e3.Delta > 0 ? -1 : 1; if (cur < 0) cur = 0; if (cur > pages0 - 1) cur = pages0 - 1;
                logPageSel = (cur == pages0 - 1) ? -1 : cur; taskLogView.Invalidate();
            };
            taskLogView.Paint += delegate (object s2, PaintEventArgs e2) {
                Graphics g2 = e2.Graphics; g2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; float sc2 = UI.Dpi(g2);
                using (Font f = new Font("Segoe UI", 9.5f)) using (Font fb = new Font("Segoe UI", 12f, FontStyle.Bold)) using (Font fs = new Font("Segoe UI Symbol", 9.5f, FontStyle.Bold))
                {
                    const int rows = 14, cap = 14; int n = taskLog.Count, lh = (int)(27 * sc2);
                    int pages = Math.Max(1, (n + cap - 1) / cap), pg = logPageSel >= 0 && logPageSel < pages ? logPageSel : pages - 1;
                    int start = pg * cap, cnt = Math.Min(cap, n - start);
                    int colW = taskLogView.Width - (int)(4 * sc2);
                    for (int i = 0; i < cnt; i++)
                    {
                        string ln = taskLog[start + i];
                        int col = i / rows, row = i % rows, x0 = col * (colW + (int)(14 * sc2)), y = row * lh;
                        bool last = start + i == n - 1, bad = ln.StartsWith("!"), tun = ln.StartsWith("TUNING:"), fnd = ln.StartsWith("SCAN:"), dn2 = ln.StartsWith("DONE:"), dls = ln.StartsWith("DLSS:");
                        if (last) { g2.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath hp = UI.Round(new RectangleF(x0, y + 1, colW, lh - 2), 7 * sc2)) using (SolidBrush hb = new SolidBrush(Color.FromArgb(34, UI.Accent))) g2.FillPath(hb, hp); }
                        if (bad) ln = ln.Substring(1);
                        string mark = bad ? "\u2717" : dls ? "\u25C6" : tun ? "\u26A1" : fnd ? "\u25C6" : last ? "\u25B6" : "\u2713";
                        Color mc = bad ? Color.FromArgb(255, 90, 90) : dls ? Color.FromArgb(70, 170, 255) : tun ? Color.FromArgb(255, 215, 60) : fnd ? Color.FromArgb(90, 200, 255) : last ? UI.Accent : Color.FromArgb(80, 225, 130);
                        Color tc = bad ? Color.FromArgb(255, 120, 120) : dls ? Color.FromArgb(70, 170, 255) : tun ? Color.FromArgb(255, 215, 60) : fnd ? Color.FromArgb(90, 200, 255) : dn2 ? Color.FromArgb(80, 225, 130) : last ? UI.Txt : UI.Muted;
                        TextRenderer.DrawText(g2, mark, fs, new Rectangle(x0 + (int)(2 * sc2), y, (int)(22 * sc2), lh), mc, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                        TextRenderer.DrawText(g2, ln, dls ? fb : f, new Rectangle(x0 + (int)(26 * sc2), y, colW - (int)(28 * sc2), lh), tc, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    }
                    if (pages > 1) TextRenderer.DrawText(g2, "page " + (pg + 1) + " / " + pages + "   -   mouse wheel = older steps", f, new Rectangle(0, rows * lh + (int)(4 * sc2), taskLogView.Width, lh), UI.Muted, TextFormatFlags.Right | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }
            };
            overlay.Controls.Add(taskLogView);
            taskCancel = new ZBtn(); taskCancel.Kind = 3; taskCancel.Title = "CANCEL RESTART"; taskCancel.SetBounds(788, 584, 200, 40); taskCancel.Visible = false;
            taskCancel.Click += delegate { if (restartTimer != null) restartTimer.Stop(); taskCancel.Visible = false; overlay.Visible = false; };
            overlay.Controls.Add(taskCancel);
        }
        void StartRestartCountdown()
        {
            restartLeft = 10; taskPercent = 100; taskBar.Value = 100; taskBar.Invalidate(); taskPct.Invalidate();
            taskCancel.Visible = true; taskCancel.BringToFront();
            taskStep.Text = "Restarting in 10 s  -  press CANCEL RESTART to stay";
            if (restartTimer != null) restartTimer.Stop();
            restartTimer = new System.Windows.Forms.Timer(); restartTimer.Interval = 1000;
            restartTimer.Tick += delegate {
                restartLeft--;
                if (restartLeft <= 0)
                {
                    restartTimer.Stop(); taskStep.Text = "Restarting now...";
                    try { Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/r /t 3 /c \"Bu 7maid Zero - restarting\"") { CreateNoWindow = true, UseShellExecute = false }); } catch { }
                }
                else taskStep.Text = "Restarting in " + restartLeft + " s  -  press CANCEL RESTART to stay";
            };
            restartTimer.Start();
        }
        void RunPingTest()
        {
            if (busy) return;
            SetBusy(true, "PING TEST");
            taskPercent = 0; taskTitle.Text = "Ping test"; taskStep.Text = "testing 1.1.1.1 ..."; overlay.Visible = true; overlay.BringToFront();
            taskLog.Clear(); taskLogView.Invalidate(); taskBar.Value = 0; taskBar.Invalidate(); taskPct.Invalidate();
            ThreadPool.QueueUserWorkItem(delegate {
                List<long> r = new List<long>(); int lost = 0; const int N = 30;
                using (Ping pg = new Ping())
                    for (int i = 1; i <= N; i++)
                    {
                        string line;
                        try { PingReply pr = pg.Send("1.1.1.1", 1000); if (pr.Status == IPStatus.Success) { r.Add(pr.RoundtripTime); line = "reply " + pr.RoundtripTime + " ms"; } else { lost++; line = "lost"; } }
                        catch { lost++; line = "lost"; }
                        int pc = i * 100 / N; string ln = i + "/" + N + "  " + line;
                        try { BeginInvoke(new Action(delegate { taskPercent = pc; taskBar.Value = pc; taskBar.Invalidate(); taskPct.Invalidate(); PushLog(ln); })); } catch { }
                        Thread.Sleep(250);
                    }
                string res;
                if (r.Count == 0) res = "No reply - check your internet connection.";
                else
                {
                    long mn = long.MaxValue, mx = 0, sum = 0, jit = 0;
                    for (int i = 0; i < r.Count; i++) { sum += r[i]; if (r[i] < mn) mn = r[i]; if (r[i] > mx) mx = r[i]; if (i > 0) jit += Math.Abs(r[i] - r[i - 1]); }
                    double avg = (double)sum / r.Count, jt = r.Count > 1 ? (double)jit / (r.Count - 1) : 0;
                    res = "Average: " + avg.ToString("0.0") + " ms\nBest: " + mn + " ms   Worst: " + mx + " ms\nJitter: " + jt.ToString("0.0") + " ms (lower = steadier)\nLost: " + lost + " of " + N + "\n\n" + (jt <= 3 && lost == 0 ? "Very steady - good for online games." : jt <= 8 && lost <= 1 ? "Fine for gaming." : "Unsteady - use a LAN cable, close downloads, restart the router.");
                }
                try { BeginInvoke(new Action(delegate { overlay.Visible = false; SetBusy(false, ""); MessageBox.Show(res, "Ping test", MessageBoxButtons.OK, MessageBoxIcon.Information); })); } catch { }
            });
        }
        // asks once per BOOST: only turn startup apps off, or also delete them from the startup list
        void AskStartup()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero"); Directory.CreateDirectory(dir);
                string flag = Path.Combine(dir, "start_del.flag");
                bool del = MessageBox.Show("Startup apps: BOOST turns OFF everything that starts with Windows (only Discord stays).\n\nYES  =  also DELETE them from the startup list\nNO   =  only turn them off (nothing is deleted)\n\nRESTORE brings them back either way.", "Bu 7maid Zero", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                if (del) File.WriteAllText(flag, "1"); else if (File.Exists(flag)) File.Delete(flag);
            }
            catch { }
        }
        void AskCod()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero");
                Directory.CreateDirectory(dir); string flag = Path.Combine(dir, "cod_ok.flag");
                bool has = false;
                string[] ps = { @"C:\Program Files (x86)\Call of Duty", @"C:\Program Files\Call of Duty", @"D:\Call of Duty", @"D:\Games\Call of Duty", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Call of Duty"), @"C:\Program Files (x86)\Steam\steamapps\common\Call of Duty", @"C:\Program Files (x86)\Battle.net" };
                foreach (string q in ps) { try { if (Directory.Exists(q)) { has = true; break; } } catch { } }
                if (!has) { if (File.Exists(flag)) File.Delete(flag); return; }
                if (MessageBox.Show("Call of Duty found.\n\nYes  =  scan it and tune it too (GPU priority + CPU priority High)\nNo   =  skip it, leave it exactly as it is", "Bu 7maid Zero - Call of Duty", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) File.WriteAllText(flag, "1");
                else if (File.Exists(flag)) File.Delete(flag);
            }
            catch { }
        }
        void RunHiddenTask(string cmd, string title) { RunHiddenTask(cmd, title, true, null); }
        int taskErrs = -1;
        List<string> taskErrList = new List<string>();
        void RunHiddenTask(string cmd, string title, bool determinate, string doneMsg) { RunHiddenTask(cmd, title, determinate, doneMsg, false); }
        void RunHiddenTask(string cmd, string title, bool determinate, string doneMsg, bool restartPrompt)
        {
            taskErrs = -1; taskErrList.Clear(); lock (GamesListForm.Pending) GamesListForm.Pending.Clear(); if (restartTimer != null) restartTimer.Stop(); taskCancel.Visible = false;
            if (busy || batPath == null) return;
            SetBusy(true, title);
            taskPercent = determinate ? 0 : -1; taskTitle.Text = title; taskStep.Text = determinate ? "starting..." : "working, this can take a while..."; overlay.Visible = true; overlay.BringToFront();
            taskLog.Clear(); taskLogView.Invalidate(); taskPct.Invalidate(); taskBar.Value = determinate ? 0 : -1; taskBar.Invalidate();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                psi.Arguments = "/c \"\"" + batPath + "\" " + cmd + " HIDDEN\"";
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = Path.GetDirectoryName(batPath);
                Process pr = Process.Start(psi);
                pr.OutputDataReceived += delegate (object s2, DataReceivedEventArgs ev) {
                    if (ev.Data == null) return; string d = ev.Data.Trim();
                    if (d.StartsWith("@@PCT="))
                    {
                        int v; if (int.TryParse(d.Substring(6), out v))
                            try { BeginInvoke(new Action(delegate { taskPercent = v; taskBar.Value = v; taskBar.Invalidate(); taskPct.Invalidate(); })); } catch { }
                    }
                    else if (d.StartsWith("@@GAME=")) { lock (GamesListForm.Pending) GamesListForm.Pending.Add(d.Substring(7)); }
                    else if (d.StartsWith("@@ERR=")) { string em = d.Substring(6); lock (taskErrList) { if (taskErrList.Count < 8) taskErrList.Add(em); } try { BeginInvoke(new Action(delegate { PushLog("!" + em); })); } catch { } }
                    else if (d.StartsWith("@@ERRS=")) { int ev2; if (int.TryParse(d.Substring(7), out ev2)) taskErrs = ev2; }
                    else if (d.StartsWith("@@MSG="))
                    {
                        string m = d.Substring(6);
                        try { BeginInvoke(new Action(delegate { taskStep.Text = m; PushLog(m); })); } catch { }
                    }
                    else if (d.Trim().Length > 0 && !d.StartsWith("@@") && d.Length < 260)
                    {
                        string m = d.Trim(); try { BeginInvoke(new Action(delegate { if (m.StartsWith("TUNING:")) taskStep.Text = "Tuning " + m.Substring(7).Replace("...", "").Trim(); PushLog(m); })); } catch { }
                    }
                };
                pr.EnableRaisingEvents = true;
                pr.BeginOutputReadLine();
                pr.Exited += delegate { try { BeginInvoke(new Action(delegate {
                    if (determinate) { taskPercent = 100; taskBar.Value = 100; taskBar.Invalidate(); taskPct.Invalidate(); }
                    System.Windows.Forms.Timer t = new System.Windows.Forms.Timer(); t.Interval = determinate ? 450 : 50;
                    t.Tick += delegate {
                        t.Stop(); SetBusy(false, ""); UpdateProc(); if (pages["status"].Visible) RefreshStatus();
                        if (cmd != "RESTORE") GpuOc.ReapplyAfterTask();
                        if (GamesListForm.Pending.Count > 0) { List<string> gl; lock (GamesListForm.Pending) { gl = new List<string>(GamesListForm.Pending); GamesListForm.Pending.Clear(); } try { using (GamesListForm gf = new GamesListForm(gl)) gf.ShowDialog(this); } catch { } }
                        if (restartPrompt)
                        {
                            taskTitle.Text = taskErrs > 0 ? title + " - done, " + taskErrs + " problem(s) (see list)" : title + " - done, no errors";
                            if (autoAfterPending) { autoAfterPending = false; ScheduleAfterTest(); }
                            StartRestartCountdown();
                            return;
                        }
                        overlay.Visible = false;
                        if (false)
                        {
                        }
                        else if (doneMsg != null) MessageBox.Show(doneMsg, "Bu 7maid Zero", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }; t.Start(); })); } catch { } };
            }
            catch (Exception ex) { overlay.Visible = false; SetBusy(false, ""); MessageBox.Show(ex.Message, "Bu 7maid Zero"); }
        }

        // ---------- engine (visible console) ----------
        string ExtractEngine()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "Bu7maidZero.bat");
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bu7maidZero.bat");
            if (s == null) throw new Exception("engine resource missing - rebuild with BUILD_EXE.bat");
            using (s) using (FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write)) s.CopyTo(f);
            return path;
        }
        void RunConsole(string cmd, string name)
        {
            if (busy) { MessageBox.Show("Wait for the current task to finish.", "Bu 7maid Zero"); return; }
            if (batPath == null) { try { batPath = ExtractEngine(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Bu 7maid Zero"); return; } }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                psi.Arguments = "/c \"\"" + batPath + "\" " + cmd + "\"";
                psi.UseShellExecute = true; psi.WorkingDirectory = Path.GetDirectoryName(batPath);
                Process pr = Process.Start(psi);
                pr.EnableRaisingEvents = true;
                pr.Exited += delegate { try { BeginInvoke(new Action(delegate { SetBusy(false, ""); UpdateProc(); })); } catch { } };
                SetBusy(true, name);
            }
            catch (Exception ex) { MessageBox.Show("Could not start: " + ex.Message, "Bu 7maid Zero"); }
        }
        void SetBusy(bool b, string name)
        {
            busy = b;
            foreach (Control c in lockable) c.Enabled = !b;
            foreach (ZBtn n in navs.Values) n.Enabled = !b;
            footLbl.Text = b ? "running: " + name : Version + "  -  ready";
        }

        // ---------- helpers ----------
        bool Ask(string m) { return MessageBox.Show(m, "Bu 7maid Zero", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
        static Label Lbl(string t, int x, int y, int w, int h, float sz, FontStyle st, Color c)
        { Label l = new Label(); l.Text = t; l.SetBounds(x, y, w, h); l.ForeColor = c; l.BackColor = Color.Transparent; l.AutoEllipsis = true; l.UseMnemonic = false; l.Font = new Font("Segoe UI", sz, st); return l; }
        ZBtn Big(string t, string s, int x, int y, int w, int h, int kind) { ZBtn b = new ZBtn(); b.Kind = kind; b.Title = t; b.Sub = s; b.SetBounds(x, y, w, h); return b; }
        ComboBox Combo(string[] items, int x, int y, int w)
        { ComboBox c = new ComboBox(); c.DropDownStyle = ComboBoxStyle.DropDownList; c.FlatStyle = FlatStyle.Flat; c.BackColor = UI.Card2; c.ForeColor = UI.Txt; c.Font = new Font("Segoe UI", 10f); c.SetBounds(x, y, w, 26); c.Items.AddRange(items); c.SelectedIndex = 0; return c; }
        RichTextBox Rtb(int x, int y, int w, int h)
        { RichTextBox r = new RichTextBox(); r.SetBounds(x, y, w, h); r.BackColor = UI.Card; r.ForeColor = UI.Txt; r.BorderStyle = BorderStyle.None; r.ReadOnly = true; r.Font = new Font("Segoe UI", 10f); r.Padding = new Padding(14); return r; }
        static void Append(RichTextBox b, string t, Color c, bool head)
        {
            b.SelectionStart = b.TextLength; b.SelectionLength = 0;
            b.SelectionColor = c; b.SelectionFont = new Font("Segoe UI" + (head ? " Semibold" : ""), head ? 11f : 10f, head ? FontStyle.Bold : FontStyle.Regular);
            b.AppendText((head ? "\n" : "   ") + t + "\n");
        }
        void LoadSpecs()
        {
            string cpu = "unknown", gpu = "unknown", ram = "?", disk = "SSD", hz = "?";
            try { foreach (ManagementObject o in Wmi(null, "SELECT Name FROM Win32_Processor")) { cpu = Str(o["Name"]).Trim(); break; } } catch { }
            try
            {
                string best = null;
                foreach (ManagementObject o in Wmi(null, "SELECT Name,CurrentRefreshRate,MaxRefreshRate FROM Win32_VideoController"))
                {
                    string n = Str(o["Name"]);
                    if (n.Length == 0 || n.Contains("Basic") || n.Contains("Virtual") || n.Contains("Parsec")) continue;
                    bool disc = n.Contains("NVIDIA") || n.Contains("Radeon");
                    if (best == null || disc) { best = n; long cu = ToL(o["CurrentRefreshRate"]), mx = ToL(o["MaxRefreshRate"]); if (cu > 0) hz = cu + " Hz" + (mx > cu ? "  (max " + mx + ")" : ""); }
                }
                if (best != null) gpu = best;
            }
            catch { }
            try { foreach (ManagementObject o in Wmi(null, "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem")) { ram = Math.Round(ToL(o["TotalPhysicalMemory"]) / 1073741824.0) + " GB"; break; } } catch { }
            try
            {
                foreach (ManagementObject o in Wmi(@"root\Microsoft\Windows\Storage", "SELECT MediaType,BusType FROM MSFT_PhysicalDisk"))
                {
                    long mt = ToL(o["MediaType"]), bus = ToL(o["BusType"]);
                    if (mt == 3) disk = "HDD";
                    else if (bus == 17) { disk = "NVMe SSD"; break; }
                }
            }
            catch { }
            try { BeginInvoke(new Action(delegate { sCpu = cpu; sGpu = gpu; sRam = ram; sDisk = disk; sHz = hz; if (specsCard != null) specsCard.Invalidate(); })); } catch { }
        }

        void PushLog(string m) { if (m == null) return; m = m.Trim(); if (m.Length == 0) return; taskLog.Add(m); if (taskLog.Count > 300) taskLog.RemoveAt(0); logPageSel = -1; if (taskLogView != null) taskLogView.Invalidate(); }
        void UpdateProc() { try { sProc = Process.GetProcesses().Length.ToString(); if (specsCard != null) specsCard.Invalidate(); } catch { } }

        static ManagementObjectCollection Wmi(string ns, string q) { ManagementScope sc = ns == null ? new ManagementScope() : new ManagementScope(ns); sc.Connect(); return new ManagementObjectSearcher(sc, new ObjectQuery(q)).Get(); }
        static string Str(object o) { return o == null ? "" : o.ToString(); }
        static long ToL(object o) { try { return o == null ? 0 : Convert.ToInt64(o); } catch { return 0; } }
        static long? RD(string full, string name)
        {
            try
            {
                int i = full.IndexOf('\\'); string hive = full.Substring(0, i), sub = full.Substring(i + 1);
                RegistryKey root = hive == "HKEY_LOCAL_MACHINE" ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey k = root.OpenSubKey(sub)) { if (k == null) return null; object v = k.GetValue(name); if (v == null) return null; if (v is int) return (long)(uint)(int)v; return Convert.ToInt64(v); }
            }
            catch { return null; }
        }
        static bool Is(string full, string name, long val) { long? v = RD(full, name); return v.HasValue && v.Value == val; }
        static string Run(string exe, string args, int ms)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
                psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
                Process p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(ms)) { try { p.Kill(); } catch { } }
                return o;
            }
            catch { return ""; }
        }
    }

    // Built-in DPC latency checker window - same layout as the classic Thesycon tool. Works on Win 10/11, nothing to download.
    class DpcForm : Form
    {
        [DllImport("kernel32.dll")] static extern UIntPtr SetThreadAffinityMask(IntPtr h, UIntPtr m);
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentThread();
        const int N = 60;
        double[] bars = new double[N];
        double cur = 0, maxv = 0;
        volatile bool run = false;
        Thread th;
        Panel graph; Label lCur, lMax, lMsg, lStat; Button bStart;
        System.Windows.Forms.Timer tm;
        PerformanceCounter pDpc, pInt, pRate; string cnt = "";
        static readonly double[] AX = { 0, 100, 500, 1000, 2000, 4000, 8000, 16000 };
        static readonly double[] PY = { 0, 0.05, 0.137, 0.232, 0.373, 0.548, 0.763, 1.0 };
        public DpcForm()
        {
            Text = "DPC Latency Checker"; ClientSize = new Size(700, 470); StartPosition = FormStartPosition.CenterScreen;
            BackColor = SystemColors.Control; Font = new Font("Segoe UI", 9f); MinimumSize = new Size(716, 509);
            MenuStrip ms = new MenuStrip(); ms.BackColor = Color.White;
            ToolStripMenuItem f = new ToolStripMenuItem("File"); ToolStripMenuItem ex = new ToolStripMenuItem("Exit"); ex.Click += delegate { Close(); }; f.DropDownItems.Add(ex);
            ToolStripMenuItem inf = new ToolStripMenuItem("Info"); ToolStripMenuItem ab = new ToolStripMenuItem("About");
            ab.Click += delegate { MessageBox.Show("Bu 7maid Zero - built-in DPC latency checker.\r\nShows the real longest delay (in microseconds) measured on one CPU core. Values are measured, not estimated.", "Info"); };
            inf.DropDownItems.Add(ab); ms.Items.Add(f); ms.Items.Add(inf); MainMenuStrip = ms; Controls.Add(ms);
            graph = new Panel(); graph.Location = new Point(10, 30); graph.Size = new Size(680, 300); graph.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            graph.BackColor = Color.Black; graph.BorderStyle = BorderStyle.Fixed3D;
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(graph, true, null);
            graph.Paint += PaintGraph; graph.Resize += delegate { graph.Invalidate(); }; Controls.Add(graph);
            Label t1 = new Label(); t1.Text = "Test mode:"; t1.SetBounds(14, 345, 130, 20); Controls.Add(t1);
            Label t2 = new Label(); t2.Text = "Current Latency:"; t2.SetBounds(14, 380, 130, 20); Controls.Add(t2);
            Label t3 = new Label(); t3.Text = "Absolute Maximum:"; t3.SetBounds(14, 415, 130, 20); Controls.Add(t3);
            Label v1 = new Label(); v1.Text = "continuous"; v1.SetBounds(150, 345, 80, 20); v1.TextAlign = ContentAlignment.TopRight; Controls.Add(v1);
            lCur = new Label(); lCur.Text = "0 \u00B5s"; lCur.SetBounds(150, 380, 80, 20); lCur.TextAlign = ContentAlignment.TopRight; Controls.Add(lCur);
            lMax = new Label(); lMax.Text = "0 \u00B5s"; lMax.SetBounds(150, 415, 80, 20); lMax.TextAlign = ContentAlignment.TopRight; Controls.Add(lMax);
            lMsg = new Label(); lMsg.SetBounds(245, 338, 340, 100); lMsg.BorderStyle = BorderStyle.FixedSingle; lMsg.BackColor = SystemColors.Control; lMsg.Padding = new Padding(3);
            lMsg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(lMsg);
            bStart = Btn("Start", 338); Button bReset = Btn("Reset", 378), bExit = Btn("Exit", 418);
            bStart.Click += delegate { if (run) { run = false; bStart.Text = "Start"; lStat.Text = "Stopped"; } else { Go(); bStart.Text = "Stop"; lStat.Text = "Analysing DPC latency ..."; } };
            bReset.Click += delegate { lock (bars) { bars = new double[N]; } cur = 0; maxv = 0; };
            bExit.Click += delegate { Close(); };
            lStat = new Label(); lStat.Text = "Press Start"; lStat.SetBounds(6, 448, 400, 18); lStat.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; Controls.Add(lStat);
            Label by = new Label(); by.Text = "Bu 7maid Zero"; by.ForeColor = Color.Blue; by.Font = new Font("Segoe UI", 9f, FontStyle.Underline); by.SetBounds(560, 448, 130, 18); by.TextAlign = ContentAlignment.TopRight; by.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; Controls.Add(by);
            try { pDpc = new PerformanceCounter("Processor Information", "% DPC Time", "_Total"); pInt = new PerformanceCounter("Processor Information", "% Interrupt Time", "_Total"); pRate = new PerformanceCounter("Processor Information", "DPCs Queued/sec", "_Total"); pDpc.NextValue(); pInt.NextValue(); pRate.NextValue(); } catch { pDpc = null; }
            tm = new System.Windows.Forms.Timer(); tm.Interval = 100; tm.Tick += delegate { UpdateUi(); }; tm.Start();
            FormClosing += delegate { run = false; };
            UpdateUi();
        }
        Button Btn(string t, int y) { Button b = new Button(); b.Text = t; b.SetBounds(600, y, 90, 30); b.Anchor = AnchorStyles.Top | AnchorStyles.Right; b.FlatStyle = FlatStyle.System; Controls.Add(b); return b; }
        void Go() { run = true; th = new Thread(Loop); th.IsBackground = true; th.Priority = ThreadPriority.Highest; try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { } th.Start(); }
        void Loop()
        {
            // spin on one core and record the longest gap between two clock reads = time the core was taken by interrupts / DPCs / other work
            try { int cores = Environment.ProcessorCount; if (cores >= 4) SetThreadAffinityMask(GetCurrentThread(), new UIntPtr(1UL << (cores - 2))); } catch { }
            double tick = 1000000.0 / Stopwatch.Frequency, winMax = 0, msMax = 0;
            long last = Stopwatch.GetTimestamp(), msStart = last, winStart = last;
            while (run)
            {
                long now = Stopwatch.GetTimestamp();
                double gap = (now - last) * tick; last = now;
                if (gap > msMax) msMax = gap;
                if ((now - msStart) * tick >= 500.0)
                {
                    double shown = msMax; cur = shown; if (shown > maxv) maxv = shown; if (shown > winMax) winMax = shown;
                    msMax = 0; msStart = now;
                }
                if ((now - winStart) * tick >= 500000.0)
                {
                    lock (bars) { Array.Copy(bars, 1, bars, 0, N - 1); bars[N - 1] = Math.Max(winMax, 1); }
                    winMax = 0; winStart = now;
                }
            }
        }
        void UpdateUi()
        {
            lCur.Text = ((int)cur) + " \u00B5s"; lMax.Text = ((int)maxv) + " \u00B5s";
            lMsg.Text = maxv < 1 ? "Press Start. Close games and other programs first for a clean reading." :
                maxv < 500 ? "Good. Real delay measured on this PC stays low - fine for games, audio and video." :
                maxv < 1000 ? "Some delays seen. Close background programs and test again." :
                "Large delays seen. A driver or a background program is holding the CPU - close programs and test again.";
            if (pDpc != null) { try { cnt = "\r\n\r\nReal Windows counters:\r\nDPC time " + pDpc.NextValue().ToString("0.00") + " %   Interrupt time " + pInt.NextValue().ToString("0.00") + " %   DPCs/sec " + ((int)pRate.NextValue()); } catch { cnt = ""; } }
            lMsg.Text = lMsg.Text + cnt;
            graph.Invalidate();
        }
        static float Y(double v, int bottom, int top)
        {
            if (v < 0) v = 0; if (v > 16000) v = 16000;
            for (int i = 1; i < AX.Length; i++) if (v <= AX[i]) { double t = (v - AX[i - 1]) / (AX[i] - AX[i - 1]); double f = PY[i - 1] + t * (PY[i] - PY[i - 1]); return bottom - (float)f * (bottom - top); }
            return top;
        }
        void PaintGraph(object s, PaintEventArgs e)
        {
            Graphics g = e.Graphics; int W = graph.ClientSize.Width, H = graph.ClientSize.Height, left = 78, bottom = H - 46, top = 6;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int[] lv = { 16000, 8000, 4000, 2000, 1000, 500 };
            Color[] lc = { Color.Red, Color.Red, Color.Red, Color.Red, Color.Yellow, Color.Lime };
            double[] copy; lock (bars) { copy = (double[])bars.Clone(); }
            float bw = (W - left - 4) / (float)N;
            for (int i = 0; i < N; i++)
            {
                if (copy[i] <= 0) continue;
                Color c = copy[i] < 500 ? Color.FromArgb(70, 255, 40) : copy[i] < 1000 ? Color.FromArgb(255, 255, 60) : Color.FromArgb(240, 20, 20);
                float y = Y(copy[i], bottom, top);
                using (Brush b = new SolidBrush(c)) g.FillRectangle(b, left + 2 + i * bw, y, bw - 1.5f, bottom - y);
            }
            using (Font f = new Font("Segoe UI", 11f, FontStyle.Bold))
                for (int i = 0; i < lv.Length; i++)
                {
                    float y = Y(lv[i], bottom, top);
                    using (Pen p = new Pen(lc[i])) g.DrawLine(p, left, y, W, y);
                    using (Brush b = new SolidBrush(lc[i])) g.DrawString(lv[i] + "\u00B5s", f, b, 8 + (lv[i] < 10000 ? 8 : 0), y - 11);
                }
            using (Pen p = new Pen(Color.White, 2)) { g.DrawLine(p, left, 0, left, bottom + 2); g.DrawLine(p, 0, bottom + 2, W, bottom + 2); }
            using (Pen p = new Pen(Color.White)) { g.DrawLine(p, 0, H - 2, left, bottom + 2); }
            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (Brush b = new SolidBrush(Color.White))
            {
                g.DrawString("\u00B5s", f, b, 14, bottom + 4); g.DrawString("s", f, b, 52, bottom + 22);
                float span = W - left - 4;
                for (int k = 0; k <= 30; k++)
                {
                    float x = left + 2 + span * (30 - k) / 30f; if (k == 0) continue;
                    int len = (k % 5 == 0) ? 9 : 5;
                    g.DrawLine(Pens.White, x, bottom + 2, x, bottom + 2 + len);
                    if (k % 5 == 0) g.DrawString("-" + k, f, b, x - 14, bottom + 12);
                }
            }
        }
    }

    // ================= built-in GPU benchmark (OpenGL, fixed scene, no download) =================
    [StructLayout(LayoutKind.Sequential)]
    struct PFD
    {
        public ushort nSize, nVersion; public uint dwFlags;
        public byte iPixelType, cColorBits, cRedBits, cRedShift, cGreenBits, cGreenShift, cBlueBits, cBlueShift, cAlphaBits, cAlphaShift;
        public byte cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits, cDepthBits, cStencilBits, cAuxBuffers, iLayerType, bReserved;
        public uint dwLayerMask, dwVisibleMask, dwDamageMask;
    }
    static class GLB
    {
        const string D = "opengl32.dll";
        [DllImport(D)] public static extern void glClear(uint m);
        [DllImport(D)] public static extern void glClearColor(float r, float g, float b, float a);
        [DllImport(D)] public static extern void glViewport(int x, int y, int w, int h);
        [DllImport(D)] public static extern void glMatrixMode(uint m);
        [DllImport(D)] public static extern void glLoadIdentity();
        [DllImport(D)] public static extern void glFrustum(double l, double r, double b, double t, double n, double f);
        [DllImport(D)] public static extern void glTranslatef(float x, float y, float z);
        [DllImport(D)] public static extern void glRotatef(float a, float x, float y, float z);
        [DllImport(D)] public static extern void glPushMatrix();
        [DllImport(D)] public static extern void glPopMatrix();
        [DllImport(D)] public static extern void glEnable(uint c);
        [DllImport(D)] public static extern void glDisable(uint c);
        [DllImport(D)] public static extern void glBlendFunc(uint s, uint d);
        [DllImport(D)] public static extern void glShadeModel(uint m);
        [DllImport(D)] public static extern void glBegin(uint m);
        [DllImport(D)] public static extern void glEnd();
        [DllImport(D)] public static extern void glVertex3f(float x, float y, float z);
        [DllImport(D)] public static extern void glNormal3f(float x, float y, float z);
        [DllImport(D)] public static extern void glColor4f(float r, float g, float b, float a);
        [DllImport(D)] public static extern uint glGenLists(int n);
        [DllImport(D)] public static extern void glNewList(uint l, uint mode);
        [DllImport(D)] public static extern void glEndList();
        [DllImport(D)] public static extern void glCallList(uint l);
        [DllImport(D)] public static extern void glFinish();
        [DllImport(D)] public static extern void glGenTextures(int n, uint[] t);
        [DllImport(D)] public static extern void glBindTexture(uint target, uint tex);
        [DllImport(D)] public static extern void glTexImage2D(uint target, int level, int internalformat, int w, int h, int border, uint format, uint type, byte[] data);
        [DllImport(D)] public static extern void glTexParameteri(uint target, uint pname, int param);
        [DllImport(D)] public static extern void glTexCoord2f(float s, float t);
        [DllImport(D)] public static extern void glReadPixels(int x, int y, int w, int h, uint format, uint type, byte[] data);
        [DllImport(D)] public static extern void glPixelStorei(uint pname, int param);
        [DllImport(D)] public static extern void glLightfv(uint light, uint pname, float[] p);
        [DllImport(D)] public static extern void glColorMaterial(uint face, uint mode);
        [DllImport(D)] public static extern IntPtr glGetString(uint n);
        [DllImport(D)] public static extern IntPtr wglCreateContext(IntPtr hdc);
        [DllImport(D)] public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr rc);
        [DllImport(D)] public static extern bool wglDeleteContext(IntPtr rc);
        [DllImport(D)] public static extern IntPtr wglGetProcAddress(string n);
        [DllImport(D)] public static extern void glRasterPos2f(float x, float y);
        [DllImport(D)] public static extern void glListBase(uint b);
        [DllImport(D)] public static extern void glCallLists(int n, uint type, byte[] lists);
        [DllImport(D)] public static extern bool wglUseFontBitmapsA(IntPtr hdc, uint first, uint count, uint listBase);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr h);
        [DllImport("gdi32.dll")] public static extern int ChoosePixelFormat(IntPtr hdc, ref PFD p);
        [DllImport("gdi32.dll")] public static extern bool SetPixelFormat(IntPtr hdc, int f, ref PFD p);
        [DllImport("gdi32.dll")] public static extern bool SwapBuffers(IntPtr hdc);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
    }
    [StructLayout(LayoutKind.Sequential)]
    struct NvUtil { public uint gpu, memory; }
    [StructLayout(LayoutKind.Sequential)]
    public struct NvClkOff { public uint Version; public int Type; public int Pstate; public int Offset; public int Min; public int Max; }
    static class Nvml
    {
        [DllImport("nvml.dll")] public static extern int nvmlInit_v2();
        [DllImport("nvml.dll")] public static extern int nvmlShutdown();
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetHandleByIndex_v2(uint i, out IntPtr h);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetTemperature(IntPtr h, int sensor, out uint t);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetClockInfo(IntPtr h, int type, out uint mhz);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetPowerUsage(IntPtr h, out uint mw);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetFanSpeed(IntPtr h, out uint pct);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetMaxClockInfo(IntPtr h, int type, out uint mhz);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetGpcClkVfOffset(IntPtr h, out int offset);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceSetGpcClkVfOffset(IntPtr h, int offset);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetMemClkVfOffset(IntPtr h, out int offset);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceSetMemClkVfOffset(IntPtr h, int offset);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetClockOffsets(IntPtr h, ref NvClkOff o);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceSetClockOffsets(IntPtr h, ref NvClkOff o);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetPowerManagementLimit(IntPtr h, out uint mw);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetPowerManagementDefaultLimit(IntPtr h, out uint mw);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetPowerManagementLimitConstraints(IntPtr h, out uint min, out uint max);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceSetPowerManagementLimit(IntPtr h, uint mw);
        [DllImport("nvml.dll")] public static extern int nvmlDeviceGetUtilizationRates(IntPtr h, out NvUtil u);
        [DllImport("nvml.dll", CharSet = CharSet.Ansi)] public static extern int nvmlDeviceGetName(IntPtr h, System.Text.StringBuilder sb, uint len);
    }

    // ================= overclock helpers: GPU offsets through NVML (volatile, saved profile, crash guard) =================
    static class OcStore
    {
        public static string Dir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero"); } }
        public static Dictionary<string, string> Read(string name)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                string p = Path.Combine(Dir, name); if (!File.Exists(p)) return d;
                foreach (string ln in File.ReadAllLines(p)) { int i = ln.IndexOf('='); if (i > 0) d[ln.Substring(0, i)] = ln.Substring(i + 1); }
            }
            catch { }
            return d;
        }
        public static void Write(string name, Dictionary<string, string> d)
        {
            try
            {
                Directory.CreateDirectory(Dir); List<string> L = new List<string>();
                foreach (KeyValuePair<string, string> kv in d) L.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(Path.Combine(Dir, name), L.ToArray());
            }
            catch { }
        }
        public static void Delete(string name) { try { File.Delete(Path.Combine(Dir, name)); } catch { } }
        public static bool Has(string name) { try { return File.Exists(Path.Combine(Dir, name)); } catch { return false; } }
        public static int GetInt(Dictionary<string, string> d, string k, int def) { int v; return d.ContainsKey(k) && int.TryParse(d[k], out v) ? v : def; }
        public static double GetDbl(Dictionary<string, string> d, string k, double def) { double v; return d.ContainsKey(k) && double.TryParse(d[k], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : def; }
    }
    static class GpuOc
    {
        public const string ProfileFile = "oc_gpu.txt", GuardFile = "oc_guard.flag";
        public static IntPtr Open(out string err)
        {
            err = "";
            try
            {
                int r = Nvml.nvmlInit_v2(); if (r != 0) { err = "NVIDIA driver service not ready (NVML code " + r + ")."; return IntPtr.Zero; }
                IntPtr h; r = Nvml.nvmlDeviceGetHandleByIndex_v2(0, out h); if (r != 0) { err = "No NVIDIA GPU found (NVML code " + r + ")."; return IntPtr.Zero; }
                return h;
            }
            catch (DllNotFoundException) { err = "nvml.dll was not found - this feature needs the NVIDIA driver."; }
            catch (Exception ex) { err = "NVML error: " + ex.Message; }
            return IntPtr.Zero;
        }
        static int NewGet(IntPtr h, int type, out int off, out int mn, out int mx)
        {
            off = 0; mn = 0; mx = 0;
            try { NvClkOff o = new NvClkOff(); o.Version = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(typeof(NvClkOff)) | (1 << 24)); o.Type = type; o.Pstate = 0; int r = Nvml.nvmlDeviceGetClockOffsets(h, ref o); off = o.Offset; mn = o.Min; mx = o.Max; return r; }
            catch { return -999; }
        }
        static int NewSet(IntPtr h, int type, int off)
        {
            try { NvClkOff o = new NvClkOff(); o.Version = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(typeof(NvClkOff)) | (1 << 24)); o.Type = type; o.Pstate = 0; o.Offset = off; return Nvml.nvmlDeviceSetClockOffsets(h, ref o); }
            catch { return -999; }
        }
        // allowed range reported by the driver (0 when unknown)
        public static void Range(IntPtr h, out int coreMax, out int memMax)
        {
            int o, mn, mx; coreMax = 0; memMax = 0;
            if (NewGet(h, 0, out o, out mn, out mx) == 0) coreMax = mx;
            if (NewGet(h, 2, out o, out mn, out mx) == 0) memMax = mx;
        }
        public static bool GetOffsets(IntPtr h, out int core, out int mem)
        {
            core = 0; mem = 0;
            try { if (Nvml.nvmlDeviceGetGpcClkVfOffset(h, out core) == 0 && Nvml.nvmlDeviceGetMemClkVfOffset(h, out mem) == 0) return true; } catch { }
            int a, b, c2, d, e2, f;
            if (NewGet(h, 0, out a, out b, out c2) == 0 && NewGet(h, 2, out d, out e2, out f) == 0) { core = a; mem = d; return true; }
            return false;
        }
        // returns 0 on success, otherwise the NVML code (or -999 when the driver has no such function). Tries the old call, then the new one.
        public static int SetOffsets(IntPtr h, int core, int mem)
        {
            int r1 = -999;
            try { r1 = Nvml.nvmlDeviceSetGpcClkVfOffset(h, core); if (r1 == 0) r1 = Nvml.nvmlDeviceSetMemClkVfOffset(h, mem); } catch { r1 = -999; }
            if (r1 == 0) return 0;
            int r2 = NewSet(h, 0, core); if (r2 == 0) r2 = NewSet(h, 2, mem);
            return r2 == 0 ? 0 : (r2 == -999 ? r1 : r2);
        }
        // power limit up to the card's own factory maximum (safe: it is the vendor limit). Returns mW applied or 0.
        public static uint RaisePower(IntPtr h, out uint defMw)
        {
            defMw = 0;
            try
            {
                uint mn, mx, cur; if (Nvml.nvmlDeviceGetPowerManagementDefaultLimit(h, out defMw) != 0) return 0;
                if (Nvml.nvmlDeviceGetPowerManagementLimitConstraints(h, out mn, out mx) != 0) return 0;
                if (Nvml.nvmlDeviceGetPowerManagementLimit(h, out cur) != 0) return 0;
                if (mx <= cur) return 0;
                return Nvml.nvmlDeviceSetPowerManagementLimit(h, mx) == 0 ? mx : 0;
            }
            catch { return 0; }
        }
        public static void DefaultPower(IntPtr h) { try { uint d; if (Nvml.nvmlDeviceGetPowerManagementDefaultLimit(h, out d) == 0) Nvml.nvmlDeviceSetPowerManagementLimit(h, d); } catch { } }
        public static string Why(int code)
        {
            if (code == 3) return "this GPU / driver does not allow clock offsets from Windows (not supported).";
            if (code == 4) return "no permission - run the app as administrator.";
            if (code == -999) return "your NVIDIA driver has no clock-offset function.";
            return "NVML error code " + code + ".";
        }
        // true when Windows says the last shutdown was a crash / power loss (a normal restart, e.g. after BOOST, is not a crash)
        static bool UncleanBoot()
        {
            try
            {
                ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "wevtutil.exe"), "qe System \"/q:*[System[(EventID=6008 or EventID=41) and TimeCreated[timediff(@SystemTime)<=600000]]]\" /c:1 /f:text");
                q.CreateNoWindow = true; q.UseShellExecute = false; q.RedirectStandardOutput = true;
                Process pr = Process.Start(q); string o = pr.StandardOutput.ReadToEnd(); pr.WaitForExit(10000);
                return pr.ExitCode == 0 ? o.Trim().Length > 0 : true;
            }
            catch { return true; }
        }
        static string WevQuery(string log, string q, int cnt)
        {
            try
            {
                ProcessStartInfo i = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "wevtutil.exe"), "qe " + log + " \"/q:" + q + "\" /c:" + cnt + " /f:text");
                i.CreateNoWindow = true; i.UseShellExecute = false; i.RedirectStandardOutput = true;
                Process pr = Process.Start(i); string o = pr.StandardOutput.ReadToEnd(); pr.WaitForExit(10000); return o;
            }
            catch { return ""; }
        }
        // a graphics-driver timeout (4101) or a crash of cod.exe / a DirectX game since the last check
        static bool GameCrashSince(string iso)
        {
            string a = WevQuery("System", "*[System[Provider[@Name='nvlddmkm'] and (EventID=4101) and TimeCreated[@SystemTime>='" + iso + "']]]", 1);
            if (a.Trim().Length > 0) return true;
            string b = WevQuery("Application", "*[System[Provider[@Name='Application Error'] and (EventID=1000) and TimeCreated[@SystemTime>='" + iso + "']]]", 30).ToLower();
            return b.IndexOf("cod.exe") >= 0 || b.IndexOf("nvwgf2um") >= 0 || b.IndexOf("nvlddmkm") >= 0 || b.IndexOf("d3d12") >= 0;
        }
        // Self-correcting: if a game / the driver crashed since the last start while the OC was on, the saved OC is lowered by itself (no need to run AUTO OC again).
        static void AutoBackoff(Dictionary<string, string> p)
        {
            try
            {
                string now = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.000'Z'"); string chk = p.ContainsKey("chk") ? p["chk"] : "";
                if (chk.Length == 0 || !GameCrashSince(chk)) { p["chk"] = now; OcStore.Write(ProfileFile, p); return; }
                int core = OcStore.GetInt(p, "core", 0), mem = OcStore.GetInt(p, "mem", 0), n = OcStore.GetInt(p, "backoffs", 0) + 1;
                int nc = core * 75 / 100 / 5 * 5, nm = mem * 60 / 100 / 20 * 20;
                p["backoffs"] = n.ToString(); p["chk"] = now;
                if ((nc < 25 && nm < 100) || n > 6) { p["enabled"] = "0"; p["note"] = "Switched off automatically: games kept crashing with the OC on."; }
                else { p["core"] = nc.ToString(); p["mem"] = nm.ToString(); p["note"] = "Lowered automatically after a game/driver crash (" + n + ")."; }
                OcStore.Write(ProfileFile, p);
                Tg.Send("[AUTO OC] game/driver crash detected while OC was on\nGPU: " + Tg.Gpu() + "\n" + (p["enabled"] == "0" ? "OC switched OFF automatically." : "Lowered by itself: core " + core + " -> " + nc + ", memory " + mem + " -> " + nm + "  (step " + n + ")"));
                if (p["enabled"] == "0") RemoveLogonTask();
            }
            catch { }
        }
        public static void RemoveLogonTask()
        {
            try { ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/delete /tn \"Bu7maidZero OC Apply\" /f"); q.CreateNoWindow = true; q.UseShellExecute = false; Process pr = Process.Start(q); pr.WaitForExit(8000); } catch { }
        }
        public static void RemoveResumeTask()
        {
            try { ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/delete /tn \"Bu7maidZero OC Resume\" /f"); q.CreateNoWindow = true; q.UseShellExecute = false; Process pr = Process.Start(q); pr.WaitForExit(8000); } catch { }
        }
        public static bool AddResumeTask(string exe)
        {
            try
            {
                ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/create /tn \"Bu7maidZero OC Resume\" /tr \"\\\"" + exe + "\\\" /ocresume\" /sc onlogon /rl highest /delay 0000:05 /f");
                q.CreateNoWindow = true; q.UseShellExecute = false; Process pr = Process.Start(q); pr.WaitForExit(10000); return pr.ExitCode == 0;
            }
            catch { return false; }
        }
        // used by the after-restart check so the saved overclock is already on when it measures
        public static void ApplyQuiet() { ApplyQuiet(false); }
        // after BOOST / games / cleaning: put the saved overclock back right away and once more a few seconds later (some tweaks reset the card clocks)
        public static void ReapplyAfterTask()
        {
            ThreadPool.QueueUserWorkItem(delegate { try { Thread.Sleep(2500); ApplyQuiet(true); Thread.Sleep(9000); ApplyQuiet(true); } catch { } });
        }
        public static void ApplyQuiet(bool force)
        {
            try
            {
                Dictionary<string, string> p = OcStore.Read(ProfileFile);
                if (OcStore.GetInt(p, "enabled", 0) != 1 || (!force && OcStore.Has(GuardFile))) return;
                IntPtr h = IntPtr.Zero; string err = "";
                for (int i = 0; i < 12 && h == IntPtr.Zero; i++) { h = Open(out err); if (h == IntPtr.Zero) Thread.Sleep(1000); }
                if (h == IntPtr.Zero) return;
                int plv = OcStore.GetInt(p, "pl", 0); if (plv > 0) { try { Nvml.nvmlDeviceSetPowerManagementLimit(h, (uint)plv); } catch { } }
                if (OcStore.GetInt(p, "core", 0) != 0 || OcStore.GetInt(p, "mem", 0) != 0) SetOffsets(h, OcStore.GetInt(p, "core", 0), OcStore.GetInt(p, "mem", 0));
            }
            catch { }
        }
        public static bool AddLogonTask(string exe)
        {
            try
            {
                ProcessStartInfo q = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), "/create /tn \"Bu7maidZero OC Apply\" /tr \"\\\"" + exe + "\\\" /ocapply\" /sc onstart /ru SYSTEM /rl highest /delay 0000:15 /f");
                q.CreateNoWindow = true; q.UseShellExecute = false; Process pr = Process.Start(q); pr.WaitForExit(10000); return pr.ExitCode == 0;
            }
            catch { return false; }
        }
        // Runs at every logon (hidden). If the PC restarted/crashed within 3 minutes of the last apply, the saved OC is switched off by itself.
        public static void ApplyAtLogon()
        {
            try
            {
                Dictionary<string, string> p = OcStore.Read(ProfileFile);
                if (OcStore.GetInt(p, "enabled", 0) != 1) return;
                AutoBackoff(p); if (OcStore.GetInt(p, "enabled", 0) != 1) return;
                if (OcStore.Has(GuardFile) && !UncleanBoot()) OcStore.Delete(GuardFile);
                if (OcStore.Has(GuardFile))
                {
                    p["enabled"] = "0"; p["note"] = "Switched off automatically: the PC restarted soon after the OC was applied (unstable)."; OcStore.Write(ProfileFile, p);
                    OcStore.Delete(GuardFile); RemoveLogonTask(); return;
                }
                string err; IntPtr h = Open(out err); if (h == IntPtr.Zero) return;
                OcStore.Write(GuardFile, new Dictionary<string, string>());
                int plv = OcStore.GetInt(p, "pl", 0); if (plv > 0) { try { Nvml.nvmlDeviceSetPowerManagementLimit(h, (uint)plv); } catch { } }
                int rc = OcStore.GetInt(p, "core", 0) == 0 && OcStore.GetInt(p, "mem", 0) == 0 ? 0 : SetOffsets(h, OcStore.GetInt(p, "core", 0), OcStore.GetInt(p, "mem", 0));
                int c2, m2; bool ok = rc == 0 && (plv > 0 || GetOffsets(h, out c2, out m2));
                p["lastApply"] = ok ? "ok " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") : "failed (" + Why(rc) + ")"; OcStore.Write(ProfileFile, p);
                if (!ok) { OcStore.Delete(GuardFile); return; }
                Thread.Sleep(180000);
                OcStore.Delete(GuardFile);
            }
            catch { OcStore.Delete(GuardFile); }
        }
    }

    // Quick CPU / RAM tests with built-in error checking (used before and after a BIOS change)
    static class OcTests
    {
        static ulong Chunk(ulong x) { for (int i = 0; i < 1000000; i++) { x = x * 6364136223846793005UL + 1442695040888963407UL; x ^= x >> 29; } return x; }
        // returns million-chunks per second (multi core); errors = wrong results
        public static double Cpu(double secs, out double single, out long errors)
        {
            errors = 0; ulong refv = Chunk(12345UL);
            Stopwatch sw = Stopwatch.StartNew(); long n1 = 0;
            while (sw.Elapsed.TotalSeconds < 3.0) { if (Chunk(12345UL) != refv) errors++; n1++; }
            single = n1 / sw.Elapsed.TotalSeconds;
            int th = Math.Max(1, Environment.ProcessorCount); long total = 0, err2 = 0; double stopAt = secs;
            Stopwatch s2 = Stopwatch.StartNew(); List<Thread> T = new List<Thread>();
            for (int t = 0; t < th; t++)
            {
                Thread q = new Thread(delegate ()
                {
                    long n = 0, e = 0;
                    while (s2.Elapsed.TotalSeconds < stopAt) { if (Chunk(12345UL) != refv) e++; n++; }
                    Interlocked.Add(ref total, n); Interlocked.Add(ref err2, e);
                });
                q.IsBackground = true; q.Priority = ThreadPriority.AboveNormal; T.Add(q); q.Start();
            }
            foreach (Thread q in T) q.Join();
            errors += err2; return total / s2.Elapsed.TotalSeconds;
        }
        // returns copy bandwidth in GB/s; errors = memory cells that read back wrong
        public static double Ram(double secs, out long errors)
        {
            errors = 0; int th = Math.Min(6, Math.Max(2, Environment.ProcessorCount / 2)); int len = 64 * 1024 * 1024;
            long bytes = 0; Stopwatch sw = Stopwatch.StartNew(); List<Thread> T = new List<Thread>();
            for (int t = 0; t < th; t++)
            {
                Thread q = new Thread(delegate ()
                {
                    byte[] a = new byte[len], b = new byte[len]; for (int i = 0; i < len; i += 4096) a[i] = (byte)i;
                    long n = 0; while (sw.Elapsed.TotalSeconds < secs) { Buffer.BlockCopy(a, 0, b, 0, len); n += len; }
                    Interlocked.Add(ref bytes, n);
                });
                q.IsBackground = true; T.Add(q); q.Start();
            }
            foreach (Thread q in T) q.Join();
            double gbs = bytes / sw.Elapsed.TotalSeconds / 1e9;
            // pattern check over about 1 GB
            ulong[] m = new ulong[128 * 1024 * 1024]; long bad = 0; Stopwatch s3 = Stopwatch.StartNew(); int pass = 0;
            while (s3.Elapsed.TotalSeconds < 8.0 && pass < 12)
            {
                ulong salt = 0x9E3779B97F4A7C15UL * (ulong)(pass + 1);
                for (int i = 0; i < m.Length; i++) m[i] = (ulong)i * salt;
                for (int i = 0; i < m.Length; i++) if (m[i] != (ulong)i * salt) bad++;
                pass++;
            }
            errors = bad; return gbs;
        }
    }

    // Measures real DPC / interrupt delay: spins on one core and records the longest gap between two clock reads in every 1 ms window.
    static class DpcMeter
    {
        [DllImport("kernel32.dll")] static extern UIntPtr SetThreadAffinityMask(IntPtr h, UIntPtr m);
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentThread();
        // returns max us, p99 of the 1 ms window maxima, number of gaps over 100 us
        public static double[] Live = new double[512]; public static volatile int LiveN; public static volatile float LiveCur, LiveMax;
        public static void Run(double secs, double warm, Action<int> secsLeft, out double max, out double p99, out int spikes, out string series)
        {
            LiveN = 0; LiveCur = 0; LiveMax = 0;
            double mx = 0, p = 0; int sp = 0; bool done = false; string ser = "";
            Thread th = new Thread(delegate ()
            {
                try { int cores = Environment.ProcessorCount; if (cores >= 4) SetThreadAffinityMask(GetCurrentThread(), new UIntPtr(1UL << (cores - 2))); } catch { }
                double tick = 1000000.0 / Stopwatch.Frequency; List<double> wins = new List<double>();
                long st = Stopwatch.GetTimestamp(), last = st, winStart = st, t0 = st + (long)(warm * Stopwatch.Frequency), tEnd = t0 + (long)(secs * Stopwatch.Frequency);
                double winMax = 0, w100 = 0; double m = 0; int s = 0; long w100Start = st; List<double> bars = new List<double>();
                while (true)
                {
                    long now = Stopwatch.GetTimestamp(); double gap = (now - last) * tick; last = now;
                    if (now >= t0)
                    {
                        if (gap > winMax) winMax = gap; if (gap > w100) w100 = gap; if (gap > m) m = gap; if (gap > 100.0) s++;
                        if ((now - w100Start) * tick >= 100000.0) { bars.Add(w100); if (LiveN < Live.Length) { Live[LiveN] = w100; LiveN++; } LiveCur = (float)w100; LiveMax = (float)m; w100 = 0; w100Start = now; }
                        if ((now - winStart) * tick >= 1000.0) { wins.Add(winMax); winMax = 0; winStart = now; }
                        if (now >= tEnd) break;
                    }
                    else winStart = now;
                }
                if (wins.Count > 0) { wins.Sort(); p = wins[(int)(0.99 * (wins.Count - 1))]; }
                System.Text.StringBuilder sbs = new System.Text.StringBuilder(); for (int bi = 0; bi < bars.Count; bi++) { if (bi > 0) sbs.Append(','); sbs.Append((int)Math.Round(bars[bi])); }
                ser = sbs.ToString(); mx = m; sp = s; done = true;
            });
            th.IsBackground = true; th.Priority = ThreadPriority.Highest;
            ProcessPriorityClass old = ProcessPriorityClass.Normal; try { old = Process.GetCurrentProcess().PriorityClass; Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { }
            Stopwatch sw = Stopwatch.StartNew(); th.Start();
            while (!th.Join(40)) { Application.DoEvents(); if (secsLeft != null) secsLeft((int)Math.Max(0, Math.Ceiling(secs + warm - sw.Elapsed.TotalSeconds))); }
            try { Process.GetCurrentProcess().PriorityClass = old; } catch { }
            max = mx; p99 = p; spikes = sp; series = ser;
        }
    }

    class BenchRes
    {
        public string Gpu = "", When = "", Note = "", Size = "";
        public double Avg, Low1, Min, Seconds, Spread; public long Frames; public string DpcSeries = ""; public int Score, TempMax, ClockAvg, UtilAvg, DpcSpikes, Artifacts, Checks, PowerMax, MemClkAvg; public double DpcMax, DpcP99;
        public static string PathFor(string slot) { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero", "gpu_" + slot + ".txt"); }
        public void Save(string slot)
        {
            System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFor(slot)));
            List<string> L = new List<string>();
            L.Add("gpu=" + Gpu); L.Add("when=" + When); L.Add("note=" + Note); L.Add("size=" + Size);
            L.Add("avg=" + Avg.ToString("R", ci)); L.Add("low1=" + Low1.ToString("R", ci)); L.Add("min=" + Min.ToString("R", ci));
            L.Add("spread=" + Spread.ToString("R", ci)); L.Add("seconds=" + Seconds.ToString("R", ci)); L.Add("frames=" + Frames.ToString(ci)); L.Add("score=" + Score.ToString(ci));
            L.Add("dpcseries=" + DpcSeries); L.Add("powmax=" + PowerMax.ToString(ci)); L.Add("memclk=" + MemClkAvg.ToString(ci)); L.Add("artifacts=" + Artifacts.ToString(ci)); L.Add("checks=" + Checks.ToString(ci)); L.Add("dpcmax=" + DpcMax.ToString("R", ci)); L.Add("dpcp99=" + DpcP99.ToString("R", ci)); L.Add("dpcsp=" + DpcSpikes.ToString(ci)); L.Add("temp=" + TempMax.ToString(ci)); L.Add("clock=" + ClockAvg.ToString(ci)); L.Add("util=" + UtilAvg.ToString(ci));
            File.WriteAllLines(PathFor(slot), L.ToArray());
        }
        public static BenchRes Load(string slot)
        {
            try
            {
                string p = PathFor(slot); if (!File.Exists(p)) return null;
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                BenchRes r = new BenchRes();
                foreach (string ln in File.ReadAllLines(p))
                {
                    int i = ln.IndexOf('='); if (i < 1) continue; string k = ln.Substring(0, i), v = ln.Substring(i + 1);
                    if (k == "gpu") r.Gpu = v; else if (k == "when") r.When = v; else if (k == "note") r.Note = v; else if (k == "size") r.Size = v;
                    else if (k == "avg") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.Avg);
                    else if (k == "low1") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.Low1);
                    else if (k == "min") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.Min);
                    else if (k == "spread") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.Spread);
                    else if (k == "seconds") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.Seconds);
                    else if (k == "frames") long.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.Frames);
                    else if (k == "score") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.Score);
                    else if (k == "dpcseries") r.DpcSeries = v;
                    else if (k == "powmax") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.PowerMax);
                    else if (k == "memclk") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.MemClkAvg);
                    else if (k == "artifacts") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.Artifacts);
                    else if (k == "checks") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.Checks);
                    else if (k == "dpcmax") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.DpcMax);
                    else if (k == "dpcp99") double.TryParse(v, System.Globalization.NumberStyles.Float, ci, out r.DpcP99);
                    else if (k == "dpcsp") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.DpcSpikes);
                    else if (k == "temp") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.TempMax);
                    else if (k == "clock") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.ClockAvg);
                    else if (k == "util") int.TryParse(v, System.Globalization.NumberStyles.Integer, ci, out r.UtilAvg);
                }
                return r.Avg > 0 ? r : null;
            }
            catch { return null; }
        }
    }

    // The test window: a fixed OpenGL scene (about 6 million lit triangles + 300 blended full-screen layers) rendered with VSync OFF.
    // Warm-up 3 s, then exactly 30 s of counted frames. Every number is measured from real finished frames (glFinish + Stopwatch).
    class GlBenchForm : Form
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int SwapIntervalFn(int i);
        public string Error = "", Title = "GPU TEST"; public double Warm = 3.0, Dur = 30.0; public bool Stress; public Action OnStarted;
        int refHash, artifacts, checks; bool haveRef; double lastChk = -100, checkTime;
        static int HashPx(byte[] b) { unchecked { uint h = 2166136261; for (int i = 0; i < b.Length; i += 3) { h = (h ^ b[i]) * 16777619; h = (h ^ b[i + 1]) * 16777619; h = (h ^ b[i + 2]) * 16777619; } return (int)h; } }
        volatile bool abort;
        IntPtr hdc = IntPtr.Zero, rc = IntPtr.Zero; uint sphereList, quadList;
        int stressRep = 1; const int COLS = 12, ROWS = 8, STACKS = 180, SLICES = 180, LAYERS = 300;
        uint fontBase; byte[] hudBytes = new byte[0]; string hudLast = "";
        volatile bool polling, counting; int tMax, powMax; double clkSum, utilSum, memSum; int nS; string nvName = ""; volatile int lvT, lvC, lvM, lvP, lvU, lvF = -1; string hud2 = ""; byte[] hud2Bytes = new byte[0];
        public GlBenchForm()
        {
            AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen;
            Rectangle wa = Screen.PrimaryScreen.Bounds;
            ClientSize = (wa.Width >= 1700 && wa.Height >= 960) ? new Size(1600, 900) : new Size(1280, 720);
            BackColor = Color.Black; TopMost = true; ShowInTaskbar = false; KeyPreview = true;
            KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) abort = true; };
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }
        static void V(double a, double b)
        {
            float x = (float)(Math.Sin(a) * Math.Cos(b)), y = (float)Math.Cos(a), z = (float)(Math.Sin(a) * Math.Sin(b));
            GLB.glNormal3f(x, y, z); GLB.glVertex3f(x, y, z);
        }
        uint[] texIds = new uint[0]; string hud3 = "";
        void BuildTextures()
        {
            int n = 10, sz = 2048; texIds = new uint[n]; GLB.glGenTextures(n, texIds);
            byte[] px = new byte[sz * sz * 4]; uint s = 2463534242;
            for (int t = 0; t < n; t++)
            {
                for (int i = 0; i < px.Length; i += 4) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; px[i] = (byte)(s >> 8); px[i + 1] = (byte)(s >> 16); px[i + 2] = (byte)(s >> 24); px[i + 3] = 255; }
                GLB.glBindTexture(0xDE1, texIds[t]);
                GLB.glTexParameteri(0xDE1, 0x2801, 0x2601); GLB.glTexParameteri(0xDE1, 0x2800, 0x2601);
                GLB.glTexImage2D(0xDE1, 0, 4, sz, sz, 0, 0x1908, 0x1401, px);
            }
        }
        void BuildScene()
        {
            sphereList = GLB.glGenLists(1);
            GLB.glNewList(sphereList, 0x1300);
            GLB.glBegin(4);
            for (int i = 0; i < STACKS; i++)
            {
                double a0 = Math.PI * i / STACKS, a1 = Math.PI * (i + 1) / STACKS;
                for (int j = 0; j < SLICES; j++)
                {
                    double b0 = 2 * Math.PI * j / SLICES, b1 = 2 * Math.PI * (j + 1) / SLICES;
                    V(a0, b0); V(a1, b0); V(a1, b1);
                    V(a0, b0); V(a1, b1); V(a0, b1);
                }
            }
            GLB.glEnd(); GLB.glEndList();
            quadList = GLB.glGenLists(1);
            GLB.glNewList(quadList, 0x1300);
            GLB.glBegin(7);
            for (int k = 0; k < LAYERS; k++)
            {
                float c1 = 0.3f + 0.7f * ((k * 29) % 100) / 100f, c2 = 0.3f + 0.7f * ((k * 53) % 100) / 100f, c3 = 0.3f + 0.7f * ((k * 71) % 100) / 100f;
                GLB.glColor4f(c1, c2, c3, 0.012f);
                GLB.glVertex3f(-1, -1, 0); GLB.glVertex3f(1, -1, 0); GLB.glVertex3f(1, 1, 0); GLB.glVertex3f(-1, 1, 0);
            }
            GLB.glEnd(); GLB.glEndList();
        }
        void DrawFrame(double t)
        {
            GLB.glClear(0x4100);
            GLB.glMatrixMode(0x1700); GLB.glLoadIdentity();
            GLB.glTranslatef(0f, 0f, -52f);
            GLB.glRotatef((float)(t * 18.0), 0f, 1f, 0f);
            GLB.glEnable(0xB71); GLB.glEnable(0xB50); GLB.glDisable(0xBE2);
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    int idx = r * COLS + c;
                    GLB.glPushMatrix();
                    GLB.glTranslatef((c - (COLS - 1) / 2f) * 2.3f, (r - (ROWS - 1) / 2f) * 2.3f, ((c + r) % 3) * 2f);
                    GLB.glRotatef((float)(t * 30.0 + idx * 13), 0.3f, 1f, 0f);
                    GLB.glColor4f(0.4f + 0.6f * ((idx * 37) % 100) / 100f, 0.4f + 0.6f * ((idx * 59) % 100) / 100f, 0.4f + 0.6f * ((idx * 83) % 100) / 100f, 1f);
                    GLB.glCallList(sphereList);
                    GLB.glPopMatrix();
                }
            GLB.glMatrixMode(0x1701); GLB.glPushMatrix(); GLB.glLoadIdentity();
            GLB.glMatrixMode(0x1700); GLB.glPushMatrix(); GLB.glLoadIdentity();
            GLB.glDisable(0xB71); GLB.glDisable(0xB50); GLB.glEnable(0xBE2); GLB.glBlendFunc(0x302, 0x303);
            GLB.glCallList(quadList);
            int rp = Stress ? (((int)(t * 1.5)) % 3 == 2 ? 0 : stressRep) : 1;
            if (Stress && texIds.Length > 0 && rp > 0)
            {
                GLB.glEnable(0xDE1);
                for (int i = 0; i < texIds.Length; i++)
                {
                    GLB.glBindTexture(0xDE1, texIds[i]);
                    for (int k = 0; k < 3 * rp; k++)
                    {
                        float sc = 1.5f + (k / 3) * 0.13f + k * 2.7f + i * 0.35f, ox = (float)(t * 0.05 * (k + 1)), oy = (float)(t * 0.03 * (i + 1));
                        GLB.glColor4f(0.9f, 0.9f, 1f, 0.035f);
                        GLB.glBegin(7);
                        GLB.glTexCoord2f(ox, oy); GLB.glVertex3f(-1, -1, 0); GLB.glTexCoord2f(ox + sc, oy); GLB.glVertex3f(1, -1, 0);
                        GLB.glTexCoord2f(ox + sc, oy + sc); GLB.glVertex3f(1, 1, 0); GLB.glTexCoord2f(ox, oy + sc); GLB.glVertex3f(-1, 1, 0);
                        GLB.glEnd();
                    }
                }
                GLB.glDisable(0xDE1);
            }
            GLB.glDisable(0xBE2);
            GLB.glPopMatrix(); GLB.glMatrixMode(0x1701); GLB.glPopMatrix(); GLB.glMatrixMode(0x1700);
        }
        void DrawHud(string text, double frac)
        {
            GLB.glMatrixMode(0x1701); GLB.glPushMatrix(); GLB.glLoadIdentity();
            GLB.glMatrixMode(0x1700); GLB.glPushMatrix(); GLB.glLoadIdentity();
            GLB.glDisable(0xB71); GLB.glDisable(0xB50); GLB.glDisable(0xBE2);
            if (Stress) { GLB.glColor4f(0.92f, 0.08f, 0.08f, 1f); text = "DON'T TOUCH ANYTHING  -  " + text; } else GLB.glColor4f(0.1f, 0.45f, 1f, 1f);
            GLB.glBegin(7);
            GLB.glVertex3f(-1f, -1f, 0); GLB.glVertex3f(1f, -1f, 0); GLB.glVertex3f(1f, -0.97f, 0); GLB.glVertex3f(-1f, -0.97f, 0);
            GLB.glVertex3f(-1f, 0.97f, 0); GLB.glVertex3f(1f, 0.97f, 0); GLB.glVertex3f(1f, 1f, 0); GLB.glVertex3f(-1f, 1f, 0);
            GLB.glVertex3f(-1f, -1f, 0); GLB.glVertex3f(-0.985f, -1f, 0); GLB.glVertex3f(-0.985f, 1f, 0); GLB.glVertex3f(-1f, 1f, 0);
            GLB.glVertex3f(0.985f, -1f, 0); GLB.glVertex3f(1f, -1f, 0); GLB.glVertex3f(1f, 1f, 0); GLB.glVertex3f(0.985f, 1f, 0);
            GLB.glEnd();
            if (frac >= 0)
            {
                float x1 = -0.95f + 1.9f * (float)Math.Min(1.0, frac);
                GLB.glBegin(7);
                GLB.glColor4f(0.18f, 0.18f, 0.22f, 1f); GLB.glVertex3f(-0.95f, 0.955f, 0); GLB.glVertex3f(0.95f, 0.955f, 0); GLB.glVertex3f(0.95f, 0.975f, 0); GLB.glVertex3f(-0.95f, 0.975f, 0);
                GLB.glColor4f(0.18f, 0.85f, 0.48f, 1f); GLB.glVertex3f(-0.95f, 0.955f, 0); GLB.glVertex3f(x1, 0.955f, 0); GLB.glVertex3f(x1, 0.975f, 0); GLB.glVertex3f(-0.95f, 0.975f, 0);
                GLB.glEnd();
            }
            if (fontBase != 0 && text.Length > 0)
            {
                if (text != hudLast) { hudLast = text; hudBytes = System.Text.Encoding.ASCII.GetBytes(text); }
                GLB.glColor4f(1f, 1f, 1f, 1f); GLB.glRasterPos2f(-0.95f, 0.90f);
                GLB.glListBase(fontBase - 32); GLB.glCallLists(hudBytes.Length, 0x1401, hudBytes);
                if (hud2.Length > 0)
                {
                    if (hud3.Length > 0) { byte[] b3 = System.Text.Encoding.ASCII.GetBytes(hud3); GLB.glColor4f(1f, 0.85f, 0.4f, 1f); GLB.glRasterPos2f(-0.95f, 0.78f); GLB.glListBase(fontBase - 32); GLB.glCallLists(b3.Length, 0x1401, b3); }
                    hud2Bytes = System.Text.Encoding.ASCII.GetBytes(hud2); GLB.glColor4f(0.55f, 0.85f, 1f, 1f); GLB.glRasterPos2f(-0.95f, 0.84f);
                    GLB.glListBase(fontBase - 32); GLB.glCallLists(hud2Bytes.Length, 0x1401, hud2Bytes);
                }
            }
            GLB.glPopMatrix(); GLB.glMatrixMode(0x1701); GLB.glPopMatrix(); GLB.glMatrixMode(0x1700);
        }
        void Poll()
        {
            try
            {
                if (Nvml.nvmlInit_v2() != 0) return;
                IntPtr h; if (Nvml.nvmlDeviceGetHandleByIndex_v2(0, out h) != 0) return;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(96); Nvml.nvmlDeviceGetName(h, sb, 96); nvName = sb.ToString();
                while (polling)
                {
                    uint tmp = 0, clk = 0, mclk = 0, pw = 0, fan = 0; NvUtil u;
                    Nvml.nvmlDeviceGetTemperature(h, 0, out tmp); Nvml.nvmlDeviceGetClockInfo(h, 0, out clk); Nvml.nvmlDeviceGetClockInfo(h, 2, out mclk); Nvml.nvmlDeviceGetUtilizationRates(h, out u);
                    bool hasP = Nvml.nvmlDeviceGetPowerUsage(h, out pw) == 0; bool hasF = Nvml.nvmlDeviceGetFanSpeed(h, out fan) == 0;
                    lvT = (int)tmp; lvC = (int)clk; lvM = (int)mclk; lvP = hasP ? (int)(pw / 1000) : -1; lvU = (int)u.gpu; lvF = hasF ? (int)fan : -1;
                    if (counting) lock (this) { if ((int)tmp > tMax) tMax = (int)tmp; if (hasP && (int)(pw / 1000) > powMax) powMax = (int)(pw / 1000); clkSum += clk; memSum += mclk; utilSum += u.gpu; nS++; }
                    Thread.Sleep(500);
                }
                Nvml.nvmlShutdown();
            }
            catch { }
        }
        public BenchRes Execute()
        {
            if (Stress) { StartPosition = FormStartPosition.Manual; Bounds = Screen.PrimaryScreen.Bounds; }
            Show(); Activate(); Application.DoEvents();
            try
            {
                hdc = GLB.GetDC(Handle);
                PFD pfd = new PFD(); pfd.nSize = (ushort)Marshal.SizeOf(typeof(PFD)); pfd.nVersion = 1; pfd.dwFlags = 0x25; pfd.iPixelType = 0; pfd.cColorBits = 32; pfd.cDepthBits = 24;
                int fmt = GLB.ChoosePixelFormat(hdc, ref pfd);
                if (fmt == 0 || !GLB.SetPixelFormat(hdc, fmt, ref pfd)) { Error = "Could not set up the 3D window (pixel format)."; return null; }
                rc = GLB.wglCreateContext(hdc);
                if (rc == IntPtr.Zero || !GLB.wglMakeCurrent(hdc, rc)) { Error = "Could not start OpenGL. Install / update the graphics driver and try again."; return null; }
                string renderer = Marshal.PtrToStringAnsi(GLB.glGetString(0x1F01)) ?? "";
                if (renderer.IndexOf("GDI Generic", StringComparison.OrdinalIgnoreCase) >= 0 || renderer.IndexOf("llvmpipe", StringComparison.OrdinalIgnoreCase) >= 0)
                { Error = "Windows is using a software renderer, not your GPU (remote desktop or missing driver). The test needs the real GPU driver."; return null; }
                bool vsyncOff = false;
                IntPtr sp = GLB.wglGetProcAddress("wglSwapIntervalEXT");
                if (sp != IntPtr.Zero) { try { SwapIntervalFn f = (SwapIntervalFn)Marshal.GetDelegateForFunctionPointer(sp, typeof(SwapIntervalFn)); f(0); vsyncOff = true; } catch { } }
                int w = ClientSize.Width, h = ClientSize.Height;
                GLB.glViewport(0, 0, w, h);
                GLB.glMatrixMode(0x1701); GLB.glLoadIdentity(); double asp = (double)w / h; GLB.glFrustum(-0.55 * asp, 0.55 * asp, -0.55, 0.55, 1.0, 300.0);
                GLB.glMatrixMode(0x1700); GLB.glLoadIdentity();
                GLB.glShadeModel(0x1D01); GLB.glClearColor(0.02f, 0.02f, 0.05f, 1f);
                GLB.glEnable(0x4000); GLB.glEnable(0x4001); GLB.glEnable(0xB57); GLB.glColorMaterial(0x408, 0x1602);
                GLB.glLightfv(0x4000, 0x1203, new float[] { 0.3f, 0.8f, 1f, 0f });
                GLB.glLightfv(0x4001, 0x1203, new float[] { -0.8f, -0.2f, 0.6f, 0f });
                GLB.glLightfv(0x4001, 0x1201, new float[] { 0.4f, 0.6f, 1f, 1f });
                BuildScene();
                if (Stress) { BuildTextures(); stressRep = Math.Max(1, Math.Min(8, (int)Math.Round(33177600.0 / Math.Max(1.0, (double)w * h)))); hud3 = "STRESS SCENE  " + w + "x" + h + " native, pixel load like " + (stressRep >= 8 ? "up to 8K" : "8K x" + stressRep) + "   " + (ROWS * COLS * STACKS * SLICES * 2 / 1000000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " M triangles/frame   lighting x2   " + LAYERS + " blend layers   " + (texIds.Length * 16) + " MB textures x" + (texIds.Length * 3) + " passes   picture check every 3 s"; }
                IntPtr hfont = IntPtr.Zero;
                try
                {
                    using (Font hf = new Font("Consolas", 15f, FontStyle.Bold)) { hfont = hf.ToHfont(); }
                    IntPtr oldF = GLB.SelectObject(hdc, hfont); uint fb = GLB.glGenLists(96);
                    if (GLB.wglUseFontBitmapsA(hdc, 32, 96, fb)) fontBase = fb;
                    GLB.SelectObject(hdc, oldF);
                }
                catch { fontBase = 0; }
                try { Process.GetCurrentProcess().PriorityClass = Stress ? ProcessPriorityClass.AboveNormal : ProcessPriorityClass.High; Thread.CurrentThread.Priority = Stress ? ThreadPriority.AboveNormal : ThreadPriority.Highest; } catch { }
                if (OnStarted != null) { try { OnStarted(); } catch { } }
                double warm = Warm, dur = Dur;
                int[] segN = new int[6]; double lastPump = 0, hudT = 0; int hudFrames = 0; double liveFps = 0;
                List<double> ft = new List<double>(40000);
                Stopwatch sw = Stopwatch.StartNew();
                double tStart = -1, lastEnd = 0; Thread poll = null; polling = true; poll = new Thread(Poll); poll.IsBackground = true; poll.Start();
                while (!abort)
                {
                    double now = sw.Elapsed.TotalSeconds;
                    if (Stress && tStart >= 0 && now - lastChk >= 3.0)
                    {
                        // correctness check: the same fixed picture must come out pixel-identical every time; a wrong pixel = the overclock makes the GPU compute wrong
                        double c0 = sw.Elapsed.TotalSeconds; lastChk = c0;
                        GLB.glPixelStorei(0x0D05, 1); DrawFrame(1.234); GLB.glFinish();
                        byte[] px = new byte[w * h * 3]; GLB.glReadPixels(0, 0, w, h, 0x1907, 0x1401, px);
                        int hh = HashPx(px); checks++;
                        if (!haveRef) { refHash = hh; haveRef = true; } else if (hh != refHash) artifacts++;
                        double c1 = sw.Elapsed.TotalSeconds; checkTime += c1 - c0; lastEnd += c1 - c0; now = c1;
                    }
                    DrawFrame(tStart < 0 ? now : now - tStart);
                    string hud; double frac;
                    if (tStart < 0) { hud = Title + "  warm-up  " + (int)Math.Ceiling(warm - now) + " s   (the test starts by itself)"; frac = -1; }
                    else { double left = dur - (now - tStart); if (left < 0) left = 0; hud = Title + "   " + (int)Math.Ceiling(left) + " s left     " + (int)liveFps + " FPS     Esc = cancel"; frac = (now - tStart) / dur; }
                    DrawHud(hud, frac);
                    if (!GLB.SwapBuffers(hdc)) { Error = "The graphics driver stopped responding during the test (driver reset) - this setting is NOT stable."; return null; }
                    GLB.glFinish();
                    double end = sw.Elapsed.TotalSeconds;
                    hudFrames++; if (end - hudT >= 0.5) { liveFps = hudFrames / (end - hudT); hudFrames = 0; hudT = end; hud2 = lvT > 0 ? "GPU " + lvT + " C    core " + lvC + " MHz    memory " + lvM + " MHz" + (lvP >= 0 ? "    power " + lvP + " W" : "") + "    load " + lvU + " %" + (lvF >= 0 ? "    fan " + lvF + " %" : "") : ""; }
                    if (tStart < 0)
                    {
                        if (end >= warm) { tStart = end; lastEnd = end; counting = true; }
                    }
                    else
                    {
                        ft.Add(end - lastEnd); lastEnd = end;
                        int sg = (int)((end - tStart) / (dur / 6.0)); if (sg > 5) sg = 5; segN[sg]++;
                        if (end - tStart >= dur) break;
                    }
                    if (end - lastPump > 0.1) { lastPump = end; Application.DoEvents(); }
                }
                polling = false; if (poll != null) poll.Join(2000);
                if (abort || ft.Count < 30) { Error = abort ? "Test cancelled - nothing was saved." : "Not enough frames were drawn."; return null; }
                double secs = lastEnd - tStart - checkTime; int n = ft.Count;
                double avg = n / secs;
                double[] s = ft.ToArray(); Array.Sort(s);
                int worst = Math.Max(1, n / 100); double sum = 0; for (int i = n - worst; i < n; i++) sum += s[i];
                double low1 = 1.0 / (sum / worst);
                double mean = secs / n, vsum = 0; for (int i = 0; i < n; i++) vsum += (s[i] - mean) * (s[i] - mean); double sd = Math.Sqrt(vsum / n);
                double segMin = 1e18, segMax = 0;
                for (int k = 0; k < 6; k++) { double sd2 = k < 5 ? dur / 6.0 : (secs - dur * 5.0 / 6.0); if (sd2 < 1.0) continue; double sf = segN[k] / sd2; if (sf < segMin) segMin = sf; if (sf > segMax) segMax = sf; }
                double spread = segMax > 0 ? (segMax - segMin) / avg * 50.0 : 0;
                BenchRes r = new BenchRes();
                r.Spread = spread; r.Artifacts = artifacts; r.Checks = checks;
                r.Gpu = nvName.Length > 0 ? nvName : renderer; r.When = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); r.Size = w + "x" + h;
                r.Avg = avg; r.Low1 = low1; r.Min = 1.0 / s[n - 1]; r.Seconds = secs; r.Frames = n; r.Score = (int)Math.Round(avg * 10.0);
                lock (this) { r.TempMax = tMax; r.PowerMax = powMax; r.MemClkAvg = nS > 0 ? (int)(memSum / nS) : 0; r.ClockAvg = nS > 0 ? (int)(clkSum / nS) : 0; r.UtilAvg = nS > 0 ? (int)(utilSum / nS) : 0; }
                double[] caps = { 30, 50, 59.94, 60, 75, 100, 119.88, 120, 143.86, 144, 164.8, 165, 199.8, 200, 239.76, 240 };
                bool capped = false; foreach (double cp in caps) if (Math.Abs(avg - cp) < 0.7 && sd < mean * 0.08) capped = true;
                if (nvName.Length > 0 && renderer.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) < 0) r.Note = "WARNING: the test ran on \"" + renderer + "\", not your NVIDIA card - set Windows Graphics settings to High performance for this app.";
                else if (!vsyncOff || capped) r.Note = "WARNING: FPS looks capped by VSync - set NVIDIA Control Panel > Vertical sync = Off, then test again.";
                else if (spread > 2.5) r.Note = "Unstable run: FPS drifted +-" + spread.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "% during the run (heat or background apps) - test again.";
                else if (r.UtilAvg > 0 && r.UtilAvg < 85) r.Note = "GPU load only " + r.UtilAvg + "% - something else limits this run (close background apps).";
                return r;
            }
            catch (Exception ex) { Error = "GPU test error: " + ex.Message; return null; }
            finally
            {
                polling = false;
                try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal; Thread.CurrentThread.Priority = ThreadPriority.Normal; } catch { }
                try { if (rc != IntPtr.Zero) { GLB.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero); GLB.wglDeleteContext(rc); } } catch { }
                try { if (hdc != IntPtr.Zero) GLB.ReleaseDC(Handle, hdc); } catch { }
            }
        }
    }

    // Small window used by the automatic BEFORE / AFTER flow. Closing it with X ends the program; it never reopens by itself.
    // silent result report to the developer's Telegram (no IP, no personal data: only card name and the measured numbers). Empty token = off.
    static class Tg
    {
        static bool On { get { return Token.Length > 0 && Token[0] != '_'; } }
        public static string Name = "Zero " + MainForm.Version;
        const string Token = "__TG_TOKEN__", Chat = "";
        static string Dir() { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero"); }
        static string Http(string url, string post)
        {
            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
            System.Net.HttpWebRequest rq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url); rq.Timeout = 15000;
            if (post != null) { byte[] d = System.Text.Encoding.UTF8.GetBytes(post); rq.Method = "POST"; rq.ContentType = "application/x-www-form-urlencoded"; rq.ContentLength = d.Length; using (System.IO.Stream s = rq.GetRequestStream()) s.Write(d, 0, d.Length); }
            using (System.Net.WebResponse rs = rq.GetResponse()) using (StreamReader sr = new StreamReader(rs.GetResponseStream())) return sr.ReadToEnd();
        }
        // chat id: fixed value if set, else the one saved before, else the chat that last wrote to the bot
        static string ChatId()
        {
            if (Chat.Length > 0) return Chat;
            string f = Path.Combine(Dir(), "tg_chat.txt");
            try { if (File.Exists(f)) { string s = File.ReadAllText(f).Trim(); if (s.Length > 0) return s; } } catch { }
            try
            {
                string r = Http("https://api.telegram.org/bot" + Token + "/getUpdates", null); string id = "";
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(r, "\"chat\":\\{\"id\":(-?[0-9]+)")) id = m.Groups[1].Value;
                if (id.Length > 0) { Directory.CreateDirectory(Dir()); File.WriteAllText(f, id); return id; }
            }
            catch { }
            return "";
        }
        public static void Send(string text) { Send(text, false); }
        static void Send(string text, bool sync)
        {
            if (!On) return;
            WaitCallback job = delegate
            {
                try
                {
                    string chat = ChatId(); if (chat.Length == 0) return;
                    Http("https://api.telegram.org/bot" + Token + "/sendMessage", "chat_id=" + Uri.EscapeDataString(chat) + "&text=" + Uri.EscapeDataString(("[" + Name + "] " + text).Length > 3800 ? ("[" + Name + "] " + text).Substring(0, 3800) : "[" + Name + "] " + text));
                }
                catch { }
            };
            ThreadPool.QueueUserWorkItem(job);
        }
        // every time the program opens: tells you the program name + version, and that Telegram works
        public static void Hello()
        {
            if (!On) return; try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        string chat = ChatId(); if (chat.Length == 0) return;
                        Http("https://api.telegram.org/bot" + Token + "/sendMessage", "chat_id=" + Uri.EscapeDataString(chat) + "&text=" + Uri.EscapeDataString("Bu 7maid " + Name + " opened\nPC: " + Environment.MachineName + "\nGPU: " + Gpu()));
                    }
                    catch { }
                });
            }
            catch { }
        }
        // button: sends a test message and says exactly what happened
        public static void Test()
        {
            string res;
            if (!On) res = "Telegram is OFF in this build (no bot token inside).";
            else
            {
                try
                {
                    string chat = ChatId();
                    if (chat.Length == 0) res = "The bot has no chat yet.\n\nOpen your bot in Telegram, press START (send /start), then press this button again.";
                    else
                    {
                        Http("https://api.telegram.org/bot" + Token + "/sendMessage", "chat_id=" + Uri.EscapeDataString(chat) + "&text=" + Uri.EscapeDataString("[" + Name + "] Test OK\nPC: " + Environment.MachineName));
                        res = "Sent. Check Telegram now.";
                    }
                }
                catch (System.Net.WebException ex)
                {
                    string body = ""; try { if (ex.Response != null) using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream())) body = sr.ReadToEnd(); } catch { }
                    if (body.IndexOf("chat not found", StringComparison.OrdinalIgnoreCase) >= 0) { try { File.Delete(Path.Combine(Dir(), "tg_chat.txt")); } catch { } body += "\n(saved chat cleared - send /start to the bot and try again)"; }
                    res = "Telegram refused:\n" + ex.Message + "\n" + (body.Length > 240 ? body.Substring(0, 240) : body);
                }
                catch (Exception ex) { res = "Failed: " + ex.Message; }
            }
            MessageBox.Show(res, "Bu 7maid " + Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static string Gpu() { try { foreach (ManagementObject o in new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController").Get()) return Convert.ToString(o["Name"]); } catch { } return "?"; }
    }
    class OcRedForm : Form
    {
        Label l2;
        public OcRedForm()
        {
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual; Bounds = Screen.PrimaryScreen.Bounds; BackColor = Color.FromArgb(185, 0, 0); ShowInTaskbar = false; ControlBox = false;
            Label l1 = new Label(); l1.Dock = DockStyle.Top; l1.Height = Bounds.Height / 2; l1.TextAlign = ContentAlignment.BottomCenter; l1.ForeColor = Color.White; l1.Font = new Font("Segoe UI Black", Math.Max(24f, Bounds.Height / 14f), FontStyle.Bold); l1.Text = "DON'T TOUCH ANYTHING";
            l2 = new Label(); l2.Dock = DockStyle.Fill; l2.TextAlign = ContentAlignment.TopCenter; l2.ForeColor = Color.White; l2.Font = new Font("Segoe UI", Math.Max(12f, Bounds.Height / 40f), FontStyle.Bold);
            Controls.Add(l2); Controls.Add(l1);
        }
        public void Msg(string s) { l2.Text = s; Update(); }
    }
    // level 0 = green (all done), 1 = yellow (partly / warning), 2 = red (failed)
    class OcStatusForm : Form
    {
        public OcStatusForm(int level, string title, string msg)
        {
            Color bg = level == 0 ? Color.FromArgb(22, 140, 62) : level == 1 ? Color.FromArgb(214, 170, 20) : Color.FromArgb(190, 30, 30);
            Color fg = level == 1 ? Color.FromArgb(30, 25, 0) : Color.White;
            Text = "Bu 7maid Zero  -  AUTO OC"; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; TopMost = true; BackColor = bg;
            AutoScaleMode = AutoScaleMode.None; ClientSize = new Size(620, 400);
            Label t = new Label(); t.SetBounds(16, 12, 588, 52); t.Font = new Font("Segoe UI", 22f, FontStyle.Bold); t.ForeColor = fg; t.BackColor = bg; t.Text = title; t.TextAlign = ContentAlignment.MiddleLeft;
            Label m = new Label(); m.SetBounds(16, 70, 588, 270); m.Font = new Font("Segoe UI", 10.5f); m.ForeColor = fg; m.BackColor = bg; m.Text = msg;
            Button ok = new Button(); ok.Text = "OK"; ok.SetBounds(240, 350, 140, 36); ok.DialogResult = DialogResult.OK; ok.FlatStyle = FlatStyle.System;
            Controls.Add(t); Controls.Add(m); Controls.Add(ok); AcceptButton = ok;
        }
    }
    class ResultForm : Form
    {
        BenchRes B, A; string status = ""; int closeLeft; bool autoFlow, beforeMode; int cd; float sc = 1f; public bool Ok; bool dpcLive; public string DlssInfo = "", DpcErr = "", Err = "", Heading = "FPS TEST  -  before and after the tweak", TagB = "BEFORE", TagA = "AFTER", Info = "";
        System.Windows.Forms.Timer tm;
        Font fT, fN, fL, fS, fD;
        public ResultForm(BenchRes before, BenchRes after, int autoCloseSec)
        {
            B = before; A = after; closeLeft = autoCloseSec; Init();
        }
        public static ResultForm ForBeforeFlow() { ResultForm f = new ResultForm(null, null, 0); f.beforeMode = true; return f; }
        public static ResultForm ForAfterFlow() { ResultForm f = new ResultForm(null, null, 0); f.autoFlow = true; return f; }
        void Init()
        {
            using (Graphics gg = CreateGraphics()) sc = gg.DpiX / 96f;
            Text = "Bu 7maid Zero  -  FPS test"; AutoScaleMode = AutoScaleMode.None; ClientSize = new Size((int)(560 * sc), (int)(440 * sc));
            StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            BackColor = UI.Bg; TopMost = true; DoubleBuffered = true;
            fT = new Font("Segoe UI", 10f, FontStyle.Bold); fN = new Font("Segoe UI Black", 34f); fL = new Font("Segoe UI", 10f);
            fS = new Font("Segoe UI", 9f); fD = new Font("Segoe UI", 17f, FontStyle.Bold);
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (autoFlow) { BeginAfterFlow(); return; }
            if (beforeMode) { BeginBeforeFlow(); return; }
            if (closeLeft > 0)
            {
                tm = new System.Windows.Forms.Timer(); tm.Interval = 1000;
                tm.Tick += delegate { closeLeft--; if (closeLeft <= 0) { tm.Stop(); Close(); } };
                tm.Start();
            }
        }
        // DPC 10 s first (gives Windows time to settle), then the 15 s GPU test. Returns null and sets Err on failure.
        BenchRes Measure(string what)
        {
            string head = what + "\nDon't touch the PC.\n";
            double dmax = 0, dp99 = 0; int dsp = 0; string dser = ""; Color oldBg = BackColor; BackColor = Color.FromArgb(14, 64, 160);
            for (int tryNo = 0; tryNo < 2 && dmax <= 0; tryNo++)
            {
                status = head + "DPC latency check: 10 s"; Invalidate(); Update();
                try { dpcLive = true; DpcMeter.Run(10.0, 1.0, delegate (int left) { status = "DPC latency check: " + left + " s"; Invalidate(); Update(); }, out dmax, out dp99, out dsp, out dser); }
                catch (Exception ex) { dmax = 0; DpcErr = ex.Message; try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bu7maidZero", "autotest.log"), DateTime.Now + "  DPC error: " + ex + "\r\n"); } catch { } }
            }
            dpcLive = false; if (dmax <= 0 && DpcErr.Length == 0) DpcErr = "the meter returned no data";
            status = head + "GPU test: 15 s"; Invalidate(); Update();
            BenchRes r = null; string err = "";
            try { WindowState = FormWindowState.Minimized; Thread.Sleep(400); using (GlBenchForm f = new GlBenchForm()) { f.Title = what; f.Warm = 2.0; f.Dur = 16.0; r = f.Execute(); err = f.Error; } }
            catch (Exception ex) { err = ex.Message; }
            WindowState = FormWindowState.Normal; Activate(); BackColor = oldBg;
            if (r == null) { Err = err.Length > 0 ? err : "The GPU test was cancelled."; return null; }
            r.DpcMax = dmax; r.DpcP99 = dp99; r.DpcSpikes = dsp; r.DpcSeries = dser; return r;
        }
        void BeginBeforeFlow()
        {
            BenchRes r = Measure("Measuring BEFORE the tweak");
            if (r == null) { Ok = false; Close(); return; }
            try { r.Save("before"); File.Delete(BenchRes.PathFor("after")); } catch { }
            B = r; Ok = true; status = ""; Invalidate();
            closeLeft = 5; tm = new System.Windows.Forms.Timer(); tm.Interval = 1000;
            tm.Tick += delegate { closeLeft--; if (closeLeft <= 0) { tm.Stop(); Close(); } };
            tm.Start();
        }
        void BeginAfterFlow()
        {
            B = BenchRes.Load("before");
            BenchRes r = Measure("Measuring AFTER the tweak");
            if (r == null) status = "Could not measure after the tweak:\n" + Err + "\n\nThis window closes by itself.";
            else { try { r.Save("after"); } catch { } A = r; status = "Checking your games for DLSS..."; Invalidate(); Update(); DlssInfo = DlssScan(); status = ""; Fit(); SendBoostReport(); }
            Invalidate();
            closeLeft = 20; tm = new System.Windows.Forms.Timer(); tm.Interval = 1000;
            tm.Tick += delegate { closeLeft--; Invalidate(); if (closeLeft <= 0) { tm.Stop(); Close(); } };
            tm.Start();
        }
        static string N0(double v) { return v.ToString("0", System.Globalization.CultureInfo.InvariantCulture); }
        static string N1(double v) { return v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Func<int, int> X = delegate (int v) { return (int)(v * sc); };
            TextFormatFlags cen = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
            using (LinearGradientBrush sb = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, ClientSize.Width), 4), UI.Accent, UI.Accent2, 0f)) g.FillRectangle(sb, 0, 0, ClientSize.Width, 4);
            TextRenderer.DrawText(g, Heading, fT, new Rectangle(X(20), X(12), X(520), X(26)), UI.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (status.Length > 0 && dpcLive) { DrawDpcLive(g); return; }
            if (status.Length > 0)
            {
                TextRenderer.DrawText(g, status, fD, new Rectangle(X(20), X(60), X(520), X(260)), UI.Txt, cen | TextFormatFlags.WordBreak);
                return;
            }
            Rectangle lc = new Rectangle(X(20), X(46), X(250), X(176)), rcd = new Rectangle(X(290), X(46), X(250), X(176));
            DrawCard(g, lc, TagB, UI.Muted, B, "not measured");
            DrawCard(g, rcd, TagA, UI.Good, A, "measured after the restart");
            Rectangle bn = new Rectangle(X(20), X(236), X(520), X(180));
            using (GraphicsPath gp = UI.Round(new RectangleF(bn.X, bn.Y, bn.Width, bn.Height), 12 * sc)) using (SolidBrush br = new SolidBrush(UI.Card)) g.FillPath(br, gp);
            if (B != null && A != null)
            {
                double d = A.Avg - B.Avg, pct = d / B.Avg * 100.0;
                Color vc = pct > 1.5 ? UI.Good : pct < -1.5 ? UI.Bad : UI.Warn;
                string t = pct > 1.5 ? "+" + N1(d) + " FPS   (+" + N1(pct) + " %)" : pct < -1.5 ? N1(d) + " FPS   (" + N1(pct) + " %)" : "about the same   (" + (pct >= 0 ? "+" : "") + N1(pct) + " %)";
                TextRenderer.DrawText(g, t, fD, new Rectangle(bn.X, bn.Y + X(8), bn.Width, X(44)), vc, cen);
                string sub = "Score " + B.Score.ToString("N0") + "  to  " + A.Score.ToString("N0") + "      same 3D scene, " + N0(A.Seconds) + " s each";
                if (A.Note.Length > 0) sub = A.Note;
                TextRenderer.DrawText(g, sub, fS, new Rectangle(bn.X + X(10), bn.Y + X(54), bn.Width - X(20), X(22)), A.Note.Length > 0 ? UI.Warn : UI.Muted, cen);
                if (Info.Length > 0) TextRenderer.DrawText(g, Info, fS, new Rectangle(bn.X + X(10), bn.Y + X(78), bn.Width - X(20), X(22)), UI.Good, cen);
                if (Info.Length == 0 && B.DpcMax > 0 && A.DpcMax > 0) DrawDpcPanel(g, new Rectangle(X(20), X(428), X(520), X(200)));
                if (Info.Length == 0 && !(B.DpcMax > 0 && A.DpcMax > 0)) TextRenderer.DrawText(g, "DPC: " + (B.DpcMax <= 0 ? "no BEFORE reading was saved (the before test ran in an older version)" : A.DpcMax <= 0 ? "AFTER reading missing - " + DpcErr : "n/a"), fS, new Rectangle(bn.X + X(10), bn.Y + X(124), bn.Width - X(20), X(20)), UI.Warn, cen);
                if (DlssInfo.Length > 0) TextRenderer.DrawText(g, DlssInfo, new Font("Segoe UI", 10.5f, FontStyle.Bold), new Rectangle(bn.X + X(10), bn.Y + X(148), bn.Width - X(20), X(28)), Color.FromArgb(200, 30, 75), cen | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, Info.Length > 0 ? "Close this window with X." : closeLeft > 0 ? "Closes by itself in " + closeLeft + " s (or press X). It will not open again by itself." : "Close this window with X. It will not open again by itself.", fS, new Rectangle(bn.X + X(10), bn.Y + X(108), bn.Width - X(20), X(22)), UI.Muted, cen);
            }
            else if (B != null)
                TextRenderer.DrawText(g, "Applying the tweaks now...\nAfter the restart the app measures again and shows the difference.", fL, new Rectangle(bn.X + X(10), bn.Y, bn.Width - X(20), bn.Height), UI.Txt, cen | TextFormatFlags.WordBreak);
            else
                TextRenderer.DrawText(g, "No BEFORE result was saved, so there is nothing to compare.", fL, new Rectangle(bn.X + X(10), bn.Y, bn.Width - X(20), bn.Height), UI.Muted, cen);
        }
        static Color DpcCol(double v) { return v <= 150.0 ? UI.Good : v <= 500.0 ? UI.Warn : UI.Bad; }
        void Fit()
        {
            bool dp = B != null && A != null && Info.Length == 0 && B.DpcMax > 0 && A.DpcMax > 0;
            ClientSize = new Size((int)(560 * sc), (int)((dp ? 640 : 440) * sc));
        }
        void DrawDpcLive(Graphics g)
        {
            Func<int, int> X = delegate (int v) { return (int)(v * sc); };
            Rectangle pl = new Rectangle(X(70), X(50), X(470), X(250));
            g.FillRectangle(Brushes.Black, pl);
            double[] marks = { 20, 50, 100, 150, 500, 1000 }; double lo = Math.Log(10.0), hi = Math.Log(2000.0);
            Func<double, int> Y = delegate (double v) { double t = (Math.Log(Math.Max(10.0, Math.Min(2000.0, v))) - lo) / (hi - lo); return pl.Bottom - (int)(t * pl.Height); };
            foreach (double mk in marks)
            {
                Color lc = mk <= 150 ? Color.FromArgb(60, 220, 90) : mk <= 500 ? Color.FromArgb(240, 220, 40) : Color.FromArgb(230, 50, 40);
                using (Pen pn = new Pen(lc)) g.DrawLine(pn, pl.X, Y(mk), pl.Right, Y(mk));
                TextRenderer.DrawText(g, mk.ToString("0") + " us", fS, new Rectangle(X(6), Y(mk) - X(9), X(62), X(18)), lc, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            }
            int n = DpcMeter.LiveN; float bw = pl.Width / 100f;
            for (int i = 0; i < n && i < 100; i++)
            {
                double v = DpcMeter.Live[i]; int y = Y(v); Color bc = v <= 150 ? Color.FromArgb(60, 220, 90) : v <= 500 ? Color.FromArgb(240, 220, 40) : Color.FromArgb(230, 50, 40);
                using (SolidBrush b = new SolidBrush(bc)) g.FillRectangle(b, pl.X + i * bw, y, Math.Max(1f, bw - 1f), pl.Bottom - y);
            }
            TextRenderer.DrawText(g, "DPC Latency Checker  -  read alone, nothing else running  (" + status + ")", fT, new Rectangle(X(20), X(12), X(520), X(26)), UI.Txt, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "Current latency: " + N0(DpcMeter.LiveCur) + " us          Absolute maximum: " + N0(DpcMeter.LiveMax) + " us", fL, new Rectangle(X(20), X(312), X(520), X(26)), UI.Txt, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "Green = great, yellow = ok, red = bad.  Don't touch the PC.", fS, new Rectangle(X(20), X(344), X(520), X(22)), UI.Muted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
        void SendBoostReport()
        {
            try
            {
                if (B == null || A == null) return; System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                string m = "[BOOST] done\nGPU: " + Tg.Gpu() + "\nFPS: " + B.Avg.ToString("0.0", ci) + " -> " + A.Avg.ToString("0.0", ci) + "  (" + ((A.Avg - B.Avg) / B.Avg * 100.0).ToString("+0.0;-0.0", ci) + " %)\n1% low: " + B.Low1.ToString("0.0", ci) + " -> " + A.Low1.ToString("0.0", ci) + "\nScore: " + B.Score + " -> " + A.Score;
                if (B.DpcMax > 0 && A.DpcMax > 0) m += "\nDPC worst: " + B.DpcMax.ToString("0", ci) + " -> " + A.DpcMax.ToString("0", ci) + " us\nDPC typical: " + B.DpcP99.ToString("0", ci) + " -> " + A.DpcP99.ToString("0", ci) + " us";
                if (A.TempMax > 0) m += "\nGPU max temp: " + A.TempMax + " C";
                Tg.Send(m);
            }
            catch { }
        }
        static string DlssScan()
        {
            try
            {
                string man = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", "Bu7maid_Backup\\games\\manifest.csv");
                if (!File.Exists(man)) return "";
                List<string> dirs = new List<string>(), fg = new List<string>(); Stopwatch sw = Stopwatch.StartNew();
                foreach (string ln in File.ReadAllLines(man))
                    foreach (string piece in ln.Split('"'))
                    {
                        string s = piece.Trim(',', ' ');
                        if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(s)) { string d = Path.GetDirectoryName(s); if (!dirs.Contains(d)) dirs.Add(d); }
                    }
                foreach (string d in dirs)
                {
                    if (sw.Elapsed.TotalSeconds > 10) break;
                    try { if (Directory.GetFiles(d, "nvngx_dlssg.dll", SearchOption.AllDirectories).Length > 0) fg.Add(new DirectoryInfo(d).Name); } catch { }
                }
                if (dirs.Count == 0) return "";
                return fg.Count > 0 ? "DLSS FRAME GENERATION  -  supported in " + fg.Count + " of your games: " + string.Join(", ", fg.ToArray()) : "DLSS Frame Generation: none of your scanned games has it";
            }
            catch { return ""; }
        }
        void DrawDpcRow(Graphics g, Rectangle r, string tag, BenchRes res)
        {
            using (GraphicsPath gp = UI.Round(new RectangleF(r.X, r.Y, r.Width, r.Height), 10 * sc)) using (SolidBrush br = new SolidBrush(UI.Card)) g.FillPath(br, gp);
            TextFormatFlags lf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix, rf = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, tag, fT, new Rectangle(r.X + (int)(12 * sc), r.Y + (int)(6 * sc), (int)(80 * sc), (int)(20 * sc)), UI.Muted, lf);
            int gx = r.X + (int)(12 * sc), gy = r.Y + (int)(30 * sc), gw = r.Width - (int)(190 * sc), gh = r.Height - (int)(38 * sc);
            string[] parts = res.DpcSeries.Length > 0 ? res.DpcSeries.Split(',') : new string[0]; int n = parts.Length;
            if (n > 0)
            {
                float bw = (float)gw / n;
                for (int i = 0; i < n; i++)
                {
                    double v; double.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
                    double fr = Math.Sqrt(Math.Min(1.0, v / 1000.0)); float bh = Math.Max(2f * sc, (float)(fr * gh));
                    using (SolidBrush b = new SolidBrush(DpcCol(v))) g.FillRectangle(b, gx + i * bw, gy + gh - bh, Math.Max(1f, bw - 1f), bh);
                }
            }
            TextRenderer.DrawText(g, "worst " + N0(res.DpcMax) + " us", fT, new Rectangle(r.Right - (int)(170 * sc), r.Y + (int)(10 * sc), (int)(158 * sc), (int)(24 * sc)), DpcCol(res.DpcMax), rf);
            TextRenderer.DrawText(g, "typical peak " + N0(res.DpcP99) + " us", fS, new Rectangle(r.Right - (int)(170 * sc), r.Y + (int)(36 * sc), (int)(158 * sc), (int)(22 * sc)), DpcCol(res.DpcP99), rf);
            TextRenderer.DrawText(g, res.DpcSpikes + " spikes over 100 us", fS, new Rectangle(r.Right - (int)(170 * sc), r.Y + (int)(58 * sc), (int)(158 * sc), (int)(22 * sc)), UI.Muted, rf);
        }
        void DrawDpcPanel(Graphics g, Rectangle r)
        {
            double dq = B.DpcP99 > 0 ? (A.DpcP99 - B.DpcP99) / B.DpcP99 * 100.0 : 0, dd = (A.DpcMax - B.DpcMax) / B.DpcMax * 100.0;
            Color vc = dq <= -3 ? UI.Good : dq >= 3 ? UI.Bad : UI.Muted;
            string t = "DPC DELAY   " + (dq <= -3 ? N0(-dq) + " % lower - better" : dq >= 3 ? N0(dq) + " % higher - worse" : "about the same") + "     (worst spike " + (dd <= -3 ? N0(-dd) + " % lower" : dd >= 3 ? N0(dd) + " % higher" : "same") + ")";
            TextRenderer.DrawText(g, t, fT, new Rectangle(r.X, r.Y, r.Width, (int)(24 * sc)), vc, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            int rh = (int)(78 * sc);
            DrawDpcRow(g, new Rectangle(r.X, r.Y + (int)(28 * sc), r.Width, rh), TagB, B);
            DrawDpcRow(g, new Rectangle(r.X, r.Y + (int)(28 * sc) + rh + (int)(8 * sc), r.Width, rh), TagA, A);
        }
        void DrawCard(Graphics g, Rectangle r, string tag, Color col, BenchRes res, string empty)
        {
            using (GraphicsPath gp = UI.Round(new RectangleF(r.X, r.Y, r.Width, r.Height), 12 * sc)) using (SolidBrush br = new SolidBrush(UI.Card)) g.FillPath(br, gp);
            using (GraphicsPath gp = UI.Round(new RectangleF(r.X, r.Y, r.Width, r.Height), 12 * sc)) using (Pen pn = new Pen(res == null ? UI.Line : col, 1.4f * sc)) g.DrawPath(pn, gp);
            TextFormatFlags cen = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, tag, fT, new Rectangle(r.X, r.Y + (int)(8 * sc), r.Width, (int)(24 * sc)), col, cen);
            if (res == null) { TextRenderer.DrawText(g, empty, fL, new Rectangle(r.X, r.Y + (int)(40 * sc), r.Width, (int)(100 * sc)), UI.Muted, cen); return; }
            TextRenderer.DrawText(g, N0(res.Avg), fN, new Rectangle(r.X, r.Y + (int)(34 * sc), r.Width, (int)(64 * sc)), UI.Txt, cen);
            TextRenderer.DrawText(g, "FPS", fS, new Rectangle(r.X, r.Y + (int)(98 * sc), r.Width, (int)(20 * sc)), UI.Muted, cen);
            TextRenderer.DrawText(g, "score " + res.Score.ToString("N0") + "    1% low " + N0(res.Low1), fS, new Rectangle(r.X, r.Y + (int)(122 * sc), r.Width, (int)(22 * sc)), UI.Muted, cen);
            if (res.DpcMax <= 0 && res.TempMax > 0) TextRenderer.DrawText(g, "max " + res.TempMax + " C    core " + res.ClockAvg + " MHz    mem " + res.MemClkAvg + " MHz", fS, new Rectangle(r.X, r.Y + (int)(146 * sc), r.Width, (int)(22 * sc)), UI.Muted, cen);
            if (res.DpcMax > 0) TextRenderer.DrawText(g, "DPC worst " + N0(res.DpcMax) + " us    peak " + N0(res.DpcP99) + " us", fS, new Rectangle(r.X, r.Y + (int)(146 * sc), r.Width, (int)(22 * sc)), UI.Muted, cen);
        }
    }
}
