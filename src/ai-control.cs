using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

// Button that draws the ↺ character rotated by Angle degrees
class ReloadButton : Button
{
    float _angle;
    public float Angle { get { return _angle; } set { _angle = value; } }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode     = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        float cx = Width / 2f, cy = Height / 2f;
        e.Graphics.TranslateTransform(cx, cy);
        e.Graphics.RotateTransform(_angle);
        e.Graphics.TranslateTransform(-cx, -cy);
        string text = "\u21BA";
        var sz = e.Graphics.MeasureString(text, Font);
        e.Graphics.DrawString(text, Font, new SolidBrush(ForeColor),
            (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
    }

    protected override void OnMouseEnter(EventArgs e) { BackColor = Color.FromArgb(55, 55, 55); Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { BackColor = Color.FromArgb(25, 25, 25); Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { BackColor = Color.FromArgb(70, 70, 70); Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e)   { BackColor = Color.FromArgb(55, 55, 55); Invalidate(); base.OnMouseUp(e); }
}

public class AIControlForm : Form
{
    Label lblSwap, lblWeb, lblComfy;
    Label statusSwap, statusWeb, statusComfy;
    Button btnSwap, btnWeb, btnComfy;
    Button btnOpenSwap, btnOpenWeb, btnOpenComfy;
    Button btnLogsSwap, btnLogsWeb, btnLogsComfy, btnLogsGui;
    Button btnAll, btnStop, btnReloadAll;
    ReloadButton btnReloadSwap, btnReloadWeb, btnReloadComfy;
    System.Windows.Forms.Timer timer;
    NotifyIcon tray;
    Process swapProc, webProc, comfyProc;
    bool exitForReal = false;
    bool reloadingSwap, reloadingWeb, reloadingComfy;
    System.Windows.Forms.Timer spinTimer;
    List<string> guiLog = new List<string>();

    static Color BG     = Color.FromArgb(25, 25, 25);
    static Color FG     = Color.White;
    static Color GREEN  = Color.FromArgb(50, 205, 50);
    static Color RED    = Color.FromArgb(220, 60, 60);
    static Color ORANGE = Color.FromArgb(255, 165, 0);
    static Color BTNBG  = Color.FromArgb(55, 55, 55);

    // Log file paths (Windows paths for native, WSL paths for WSL services)
    const string SWAP_LOG_WSL  = "/tmp/llama-swap.log";
    const string WEB_LOG_WSL   = "/tmp/owui.log";
    const string COMFY_LOG_WIN = @"C:\Users\Natural\AppData\Local\Temp\comfyui.log";
    const string SWAP_LOG_WIN  = @"C:\Users\Natural\AppData\Local\Temp\llama-swap.log";
    const string WEB_LOG_WIN   = @"C:\Users\Natural\AppData\Local\Temp\owui.log";

    public AIControlForm()
    {
        Text = "Local AI";
        ClientSize = new Size(320, 200);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        var wa = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(wa.Right - 340, wa.Bottom - 240);
        BackColor = BG;
        ForeColor = FG;

        Font font    = new Font("Segoe UI", 9.5f);
        Font fontSt  = new Font("Segoe UI", 8f);
        Font fontBtn = new Font("Segoe UI", 9f);
        Color OPENBG = Color.FromArgb(35, 70, 140);

        // Row 1 — Model Proxy (y=10)
        // layout: ↺ | Open | Start/Stop | ☰
        lblSwap       = MakeLabel("Model Proxy", font, FG, 14, 10);
        statusSwap    = MakeStatusLabel(14, 27, fontSt);
        btnReloadSwap = MakeReloadBtn(110, 10);
        btnOpenSwap   = MakeBtn("Open",  fontBtn, 48, 26, 136, 10, OPENBG);
        btnSwap       = MakeBtn("Start", fontBtn, 68, 26, 188, 10, BTNBG);
        btnLogsSwap   = MakeLogsBtn(282, 10);

        // Row 2 — ComfyUI (y=54)
        lblComfy       = MakeLabel("ComfyUI", font, FG, 14, 54);
        statusComfy    = MakeStatusLabel(14, 71, fontSt);
        btnReloadComfy = MakeReloadBtn(110, 54);
        btnOpenComfy   = MakeBtn("Open",  fontBtn, 48, 26, 136, 54, OPENBG);
        btnComfy       = MakeBtn("Start", fontBtn, 68, 26, 188, 54, BTNBG);
        btnLogsComfy   = MakeLogsBtn(282, 54);

        // Row 3 — Open WebUI (y=98)
        lblWeb       = MakeLabel("Open WebUI", font, FG, 14, 98);
        statusWeb    = MakeStatusLabel(14, 115, fontSt);
        btnReloadWeb = MakeReloadBtn(110, 98);
        btnOpenWeb   = MakeBtn("Open",  fontBtn, 48, 26, 136, 98, OPENBG);
        btnWeb       = MakeBtn("Start", fontBtn, 68, 26, 188, 98, BTNBG);
        btnLogsWeb   = MakeLogsBtn(282, 98);

        var sep = new Label { BorderStyle = BorderStyle.Fixed3D, Size = new Size(296, 2), Location = new Point(12, 148) };

        // Bottom row: Start All | Stop All | Reload All | ☰
        btnAll       = MakeBtn("Start All",  fontBtn, 80, 28, 12,  156, Color.FromArgb(35, 110, 35));
        btnStop      = MakeBtn("Stop All",   fontBtn, 80, 28, 102, 156, Color.FromArgb(140, 35, 35));
        btnReloadAll = MakeBtn("Reload All", fontBtn, 86, 28, 192, 156, Color.FromArgb(130, 90, 0));
        btnLogsGui   = MakeLogsBtn(282, 156, 28);

        Controls.AddRange(new Control[] {
            lblSwap, statusSwap, btnReloadSwap, btnLogsSwap, btnOpenSwap, btnSwap,
            lblComfy, statusComfy, btnReloadComfy, btnLogsComfy, btnOpenComfy, btnComfy,
            lblWeb, statusWeb, btnReloadWeb, btnLogsWeb, btnOpenWeb, btnWeb,
            sep, btnAll, btnStop, btnLogsGui, btnReloadAll
        });

        btnSwap.Click  += (s, e) => { reloadingSwap = false;  if (btnSwap.Text == "Start") DoStart("swap");   else DoStop("swap"); };
        btnWeb.Click   += (s, e) => { reloadingWeb = false;   if (btnWeb.Text == "Start") DoStart("web");     else DoStop("web"); };
        btnComfy.Click += (s, e) => { reloadingComfy = false; if (btnComfy.Text == "Start") DoStart("comfy"); else DoStop("comfy"); };
        btnOpenSwap.Click  += (s, e) => Process.Start("http://localhost:8080");
        btnOpenWeb.Click   += (s, e) => Process.Start("http://localhost:3000");
        btnOpenComfy.Click += (s, e) => Process.Start("http://localhost:8188");
        btnReloadSwap.Click  += (s, e) => { reloadingSwap = true;  DoStop("swap");  DoStart("swap"); };
        btnReloadWeb.Click   += (s, e) => { reloadingWeb = true;   DoStop("web");   DoStart("web"); };
        btnReloadComfy.Click += (s, e) => { reloadingComfy = true; DoStop("comfy"); DoStart("comfy"); };
        btnAll.Click       += (s, e) => { DoStart("swap"); DoStart("web"); DoStart("comfy"); };
        btnStop.Click      += (s, e) => { DoStop("swap"); DoStop("web"); DoStop("comfy"); };
        btnReloadAll.Click += (s, e) => {
            reloadingSwap = true;  DoStop("swap");  DoStart("swap");
            reloadingWeb = true;   DoStop("web");   DoStart("web");
            reloadingComfy = true; DoStop("comfy"); DoStart("comfy");
        };
        btnLogsSwap.Click  += (s, e) => OpenWslLog(SWAP_LOG_WSL, SWAP_LOG_WIN);
        btnLogsWeb.Click   += (s, e) => OpenWslLog(WEB_LOG_WSL,  WEB_LOG_WIN);
        btnLogsComfy.Click += (s, e) => OpenWinLog(COMFY_LOG_WIN);
        btnLogsGui.Click   += (s, e) => ShowGuiLog();

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 3000;
        timer.Tick += (s, e) => UpdateUI();
        timer.Start();

        spinTimer = new System.Windows.Forms.Timer();
        spinTimer.Interval = 50;
        spinTimer.Tick += (s, e) => SpinTick();
        spinTimer.Start();

        SetupTray();
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) { Hide(); ShowInTaskbar = false; } };
        FormClosing += (s, e) => { if (!exitForReal) { e.Cancel = true; Hide(); ShowInTaskbar = false; } };
        FormClosed  += (s, e) => { timer.Stop(); spinTimer.Stop(); tray.Visible = false; tray.Dispose(); };

        GUILog("GUI started");
        UpdateUI();
    }

    void GUILog(string msg)
    {
        guiLog.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + msg);
        if (guiLog.Count > 500) guiLog.RemoveAt(0);
    }

    void OpenWslLog(string wslPath, string winPath)
    {
        var p = RunWSL("bash", "-c", "cat " + wslPath + " > " + winPath.Replace('\\', '/').Replace("C:", "/mnt/c") + " 2>/dev/null || echo 'Log not found.' > " + winPath.Replace('\\', '/').Replace("C:", "/mnt/c"));
        if (p != null) p.WaitForExit(3000);
        OpenWinLog(winPath);
    }

    void OpenWinLog(string path)
    {
        try { Process.Start("notepad.exe", path); }
        catch { MessageBox.Show("Log not found: " + path, "Local AI", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }

    void ShowGuiLog()
    {
        var f = new Form();
        f.Text = "Local AI — GUI Log";
        f.Size = new Size(520, 400);
        f.BackColor = BG;
        f.ForeColor = FG;
        var tb = new TextBox();
        tb.Multiline = true; tb.ReadOnly = true; tb.ScrollBars = ScrollBars.Vertical;
        tb.Dock = DockStyle.Fill; tb.BackColor = Color.FromArgb(35, 35, 35); tb.ForeColor = FG;
        tb.Font = new Font("Consolas", 9f);
        tb.Text = string.Join("\r\n", guiLog.ToArray());
        tb.SelectionStart = tb.Text.Length;
        tb.ScrollToCaret();
        f.Controls.Add(tb);
        f.Show();
    }

    Label MakeStatusLabel(int x, int y, Font f)
    {
        return new Label { Text = "Off", Font = f, ForeColor = RED,
            Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent };
    }

    Label MakeLabel(string text, Font f, Color c, int x, int y)
    {
        return new Label { Text = text, Font = f, ForeColor = c, Location = new Point(x, y), AutoSize = true };
    }

    Button MakeBtn(string text, Font f, int w, int h, int x, int y, Color bg)
    {
        var b = new Button();
        b.Text = text; b.Font = f; b.Size = new Size(w, h); b.Location = new Point(x, y);
        b.FlatStyle = FlatStyle.Flat; b.BackColor = bg; b.ForeColor = FG;
        return b;
    }

    Button MakeLogsBtn(int x, int y, int h = 26)
    {
        var b = new Button();
        b.Text = "\U0001F4CB";
        b.Font = new Font("Segoe UI Emoji", 9f);
        b.Size = new Size(22, h);
        b.Location = new Point(x, y);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 55);
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
        b.BackColor = BG;
        b.ForeColor = Color.FromArgb(150, 150, 150);
        b.Cursor = Cursors.Hand;
        b.Padding = new Padding(0);
        return b;
    }

    ReloadButton MakeReloadBtn(int x, int y)
    {
        var b = new ReloadButton();
        b.Font = new Font("Segoe UI", 12f);
        b.Size = new Size(22, 26);
        b.Location = new Point(x, y);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = BG;
        b.ForeColor = Color.FromArgb(150, 150, 150);
        b.Cursor = Cursors.Hand;
        b.Padding = new Padding(0);
        return b;
    }

    Process RunWSL(params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "wsl.exe";
            psi.Arguments = string.Join(" ", Array.ConvertAll(args, QuoteArg));
            psi.CreateNoWindow = true; psi.UseShellExecute = false;
            return Process.Start(psi);
        }
        catch { return null; }
    }

    string QuoteArg(string arg)
    {
        if (arg == null || arg.Length == 0) return "\"\"";
        if (arg.IndexOfAny(new char[] { ' ', '\t', '\r', '\n', '"', '>' }) < 0) return arg;
        return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    Process RunNative(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo();
            psi.FileName = exe; psi.Arguments = args;
            psi.CreateNoWindow = true; psi.UseShellExecute = false;
            return Process.Start(psi);
        }
        catch { return null; }
    }

    bool IsAlive(Process p) { try { return p != null && !p.HasExited; } catch { return false; } }

    void DoStart(string svc)
    {
        if (svc == "swap")
        {
            GUILog("Model Proxy starting");
            statusSwap.Text = "Starting"; statusSwap.ForeColor = ORANGE;
            btnSwap.Text = "..."; btnSwap.Enabled = false;
            swapProc = RunWSL("bash", "-c",
                "/mnt/c/Users/Natural/llama.cpp/llama-swap" +
                " --config /mnt/c/Users/Natural/llama.cpp/llama-swap-config.yaml" +
                " --listen 0.0.0.0:8080" +
                " >> " + SWAP_LOG_WSL + " 2>&1");
        }
        else if (svc == "web")
        {
            GUILog("Open WebUI starting");
            statusWeb.Text = "Starting"; statusWeb.ForeColor = ORANGE;
            btnWeb.Text = "..."; btnWeb.Enabled = false;
            // Match only the service executable. Using -f can match the launcher command itself.
            var c1 = RunWSL("pkill", "-x", "open-webui");
            if (c1 != null) try { c1.WaitForExit(2000); } catch {}
            var c2 = RunWSL("fuser", "-k", "3000/tcp");
            if (c2 != null) try { c2.WaitForExit(2000); } catch {}
            Thread.Sleep(500);
            webProc = RunWSL("bash", "-c",
                "cd /mnt/c/Users/Natural/llama.cpp &&" +
                " OPENAI_API_BASE_URL=http://localhost:8080/v1 OPENAI_API_KEY=none WEBUI_AUTH=false" +
                " ./owui-venv/bin/open-webui serve --port 3000" +
                " >> " + WEB_LOG_WSL + " 2>&1");
        }
        else if (svc == "comfy")
        {
            GUILog("ComfyUI starting");
            statusComfy.Text = "Starting"; statusComfy.ForeColor = ORANGE;
            btnComfy.Text = "..."; btnComfy.Enabled = false;
            comfyProc = RunNative("cmd.exe",
                "/c \"\"C:\\Users\\Natural\\ComfyUI\\venv\\Scripts\\python.exe\" " +
                "\"C:\\Users\\Natural\\ComfyUI\\main.py\" --listen 0.0.0.0 --port 8188" +
                " >> \"" + COMFY_LOG_WIN + "\" 2>&1\"");
        }
    }

    void DoStop(string svc)
    {
        if (svc == "swap")
        {
            GUILog("Model Proxy stopping");
            if (IsAlive(swapProc)) { try { swapProc.Kill(); } catch {} }
            swapProc = null;
            RunWSL("pkill", "-x", "llama-swap");
            RunWSL("pkill", "-x", "llama-server");
        }
        else if (svc == "web")
        {
            GUILog("Open WebUI stopping");
            if (IsAlive(webProc)) { try { webProc.Kill(); } catch {} }
            webProc = null;
            RunWSL("pkill", "-x", "open-webui");
            RunWSL("bash", "-c", "fuser -k 3000/tcp 2>/dev/null");
        }
        else if (svc == "comfy")
        {
            GUILog("ComfyUI stopping");
            if (IsAlive(comfyProc))
            {
                try
                {
                    var psi = new ProcessStartInfo { FileName = "taskkill",
                        Arguments = "/F /T /PID " + comfyProc.Id,
                        CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(psi);
                }
                catch {}
            }
            comfyProc = null;
            try
            {
                var psi = new ProcessStartInfo { FileName = "wmic",
                    Arguments = "process where \"commandline like '%ComfyUI%main.py%'\" call terminate",
                    CreateNoWindow = true, UseShellExecute = false };
                Process.Start(psi);
            }
            catch {}
        }
    }

    void SpinTick()
    {
        if (reloadingSwap)  { btnReloadSwap.Angle  = (btnReloadSwap.Angle  - 9f + 360f) % 360f; btnReloadSwap.Invalidate(); }
        else if (btnReloadSwap.Angle  != 0f) { btnReloadSwap.Angle  = 0f; btnReloadSwap.Invalidate(); }
        if (reloadingWeb)   { btnReloadWeb.Angle   = (btnReloadWeb.Angle   - 9f + 360f) % 360f; btnReloadWeb.Invalidate(); }
        else if (btnReloadWeb.Angle   != 0f) { btnReloadWeb.Angle   = 0f; btnReloadWeb.Invalidate(); }
        if (reloadingComfy) { btnReloadComfy.Angle = (btnReloadComfy.Angle - 9f + 360f) % 360f; btnReloadComfy.Invalidate(); }
        else if (btnReloadComfy.Angle != 0f) { btnReloadComfy.Angle = 0f; btnReloadComfy.Invalidate(); }
    }

    bool TestPort(int port)
    {
        try {
            using (var c = new TcpClient(AddressFamily.InterNetwork)) {
                var ar = c.BeginConnect("127.0.0.1", port, null, null);
                if (ar.AsyncWaitHandle.WaitOne(400) && c.Connected) return true;
            }
        } catch { }
        try {
            using (var c = new TcpClient(AddressFamily.InterNetworkV6)) {
                var ar = c.BeginConnect("::1", port, null, null);
                if (ar.AsyncWaitHandle.WaitOne(400) && c.Connected) return true;
            }
        } catch { }
        return false;
    }

    void UpdateUI()
    {
        bool s = TestPort(8080), w = TestPort(3000), c = TestPort(8188);
        UpdateSvc(s, swapProc, statusSwap, btnSwap, btnOpenSwap, ref reloadingSwap, "Model Proxy");
        UpdateSvc(w, webProc, statusWeb, btnWeb, btnOpenWeb, ref reloadingWeb, "Open WebUI");
        UpdateSvc(c, comfyProc, statusComfy, btnComfy, btnOpenComfy, ref reloadingComfy, "ComfyUI");
        if (tray != null)
            tray.Text = "Local AI  Proxy:" + (s?"ON":"OFF") + " WebUI:" + (w?"ON":"OFF") + " Comfy:" + (c?"ON":"OFF");
    }

    void UpdateSvc(bool portUp, Process proc, Label statusLbl, Button btn, Button btnOpen, ref bool reloading, string name)
    {
        string prev = statusLbl.Text;
        if (portUp)
        {
            reloading = false;
            statusLbl.Text = "Running"; statusLbl.ForeColor = GREEN;
            btn.Text = "Stop"; btn.Enabled = true; btnOpen.Enabled = true;
        }
        else if (reloading)
        {
            statusLbl.Text = "Restarting"; statusLbl.ForeColor = ORANGE;
            btn.Text = "Stop"; btn.Enabled = true; btnOpen.Enabled = false;
        }
        else if (IsAlive(proc))
        {
            statusLbl.Text = "Starting"; statusLbl.ForeColor = ORANGE;
            btn.Text = "..."; btn.Enabled = false; btnOpen.Enabled = false;
        }
        else
        {
            statusLbl.Text = (proc != null) ? "Crashed" : "Off"; statusLbl.ForeColor = RED;
            btn.Text = "Start"; btn.Enabled = true; btnOpen.Enabled = false;
        }
        if (statusLbl.Text != prev) GUILog(name + ": " + prev + " -> " + statusLbl.Text);
    }

    void ShowFront()
    {
        Show(); ShowInTaskbar = true; WindowState = FormWindowState.Normal;
        TopMost = true; Activate(); BringToFront(); TopMost = false;
    }

    void SetupTray()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.FillEllipse(Brushes.LimeGreen, 1, 1, 14, 14);
        }
        tray = new NotifyIcon();
        tray.Icon = Icon.FromHandle(bmp.GetHicon());
        tray.Visible = true;
        tray.Text = "Local AI";

        var cm = new ContextMenuStrip();
        cm.Items.Add("Show",        null, (s, e) => ShowFront());
        cm.Items.Add("-");
        cm.Items.Add("Start All",   null, (s, e) => { DoStart("swap"); DoStart("web"); DoStart("comfy"); });
        cm.Items.Add("Stop All",    null, (s, e) => { DoStop("swap"); DoStop("web"); DoStop("comfy"); });
        cm.Items.Add("-");
        cm.Items.Add("Open Chat",    null, (s, e) => Process.Start("http://localhost:3000"));
        cm.Items.Add("Open ComfyUI", null, (s, e) => Process.Start("http://localhost:8188"));
        cm.Items.Add("-");
        cm.Items.Add("Proxy Log",   null, (s, e) => OpenWslLog(SWAP_LOG_WSL, SWAP_LOG_WIN));
        cm.Items.Add("WebUI Log",   null, (s, e) => OpenWslLog(WEB_LOG_WSL,  WEB_LOG_WIN));
        cm.Items.Add("ComfyUI Log", null, (s, e) => OpenWinLog(COMFY_LOG_WIN));
        cm.Items.Add("GUI Log",     null, (s, e) => ShowGuiLog());
        cm.Items.Add("-");
        cm.Items.Add("Exit", null, (s, e) => { exitForReal = true; tray.Visible = false; Close(); });
        tray.ContextMenuStrip = cm;
        tray.DoubleClick += (s, e) => ShowFront();
    }

    [STAThread]
    public static void Main()
    {
        bool created;
        using (var mtx = new Mutex(true, "LocalAI_Control_SingleInstance", out created))
        {
            if (!created)
            {
                MessageBox.Show("Local AI Control is already running.\nCheck the system tray.",
                    "Local AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AIControlForm());
        }
    }
}
