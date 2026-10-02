using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Web.Script.Serialization;

public class RecentOpenCodeEntry
{
    public string Path { get; set; }
    public long LastOpenedUtcTicks { get; set; }
}

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
    Label lblSwap, lblWeb, lblComfy, lblMcp, lblOpenCode;
    Label statusSwap, statusWeb, statusComfy, statusMcp;
    Button btnSwap, btnWeb, btnComfy, btnMcp;
    Button btnOpenSwap, btnOpenWeb, btnOpenComfy, btnOpenMcp;
    Button btnLogsSwap, btnLogsWeb, btnLogsComfy, btnLogsMcp, btnLogsGui;
    Button btnAll, btnStop, btnReloadAll, btnLaunchOpenCode;
    ReloadButton btnReloadSwap, btnReloadWeb, btnReloadComfy, btnReloadMcp;
    System.Windows.Forms.Timer timer;
    NotifyIcon tray;
    Icon trayIcon;
    Icon windowIcon;
    Color trayIconColor = Color.Empty;
    Process swapProc, webProc, comfyProc, mcpProc, mcpConfigProc;
    bool exitForReal = false;
    bool reloadingSwap, reloadingWeb, reloadingComfy, reloadingMcp;
    bool checkingPorts;
    System.Windows.Forms.Timer spinTimer;
    List<string> guiLog = new List<string>();

    static Color BG     = Color.FromArgb(25, 25, 25);
    static Color FG     = Color.White;
    static Color GREEN  = Color.FromArgb(50, 205, 50);
    static Color RED    = Color.FromArgb(220, 60, 60);
    static Color YELLOW = Color.FromArgb(255, 215, 0);
    static Color ORANGE = Color.FromArgb(255, 165, 0);
    static Color BTNBG  = Color.FromArgb(55, 55, 55);

    // Log file paths (Windows paths for native, WSL paths for WSL services)
    const string SWAP_LOG_WSL  = "/tmp/llama-swap.log";
    const string WEB_LOG_WSL   = "/tmp/owui.log";
    const string COMFY_LOG_WIN = @"C:\Users\Natural\AppData\Local\Temp\comfyui.log";
    const string SWAP_LOG_WIN  = @"C:\Users\Natural\AppData\Local\Temp\llama-swap.log";
    const string WEB_LOG_WIN   = @"C:\Users\Natural\AppData\Local\Temp\owui.log";
    const string MCP_LOG_WSL   = "/tmp/ai-workspace-mcp.log";
    const string MCP_LOG_WIN   = @"C:\Users\Natural\AppData\Local\Temp\ai-workspace-mcp.log";

    public AIControlForm()
    {
        Text = "Local AI";
        windowIcon = CreateExcavatorIcon(GREEN);
        Icon = windowIcon;
        ClientSize = new Size(320, 276);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        var wa = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(wa.Right - 340, wa.Bottom - 284);
        BackColor = BG;
        ForeColor = FG;

        Font font    = new Font("Segoe UI", 9.5f);
        Font fontSt  = new Font("Segoe UI", 8f);
        Font fontBtn = new Font("Segoe UI", 9f);
        Color OPENBG = Color.FromArgb(35, 70, 140);

        // Row 1 — llama-swap (y=10)
        // layout: ↺ | Open | Start/Stop | ☰
        lblSwap       = MakeLabel("llama-swap", font, FG, 14, 10);
        statusSwap    = MakeStatusLabel(14, 27, fontSt);
        btnReloadSwap = MakeReloadBtn(110, 10);
        btnOpenSwap   = MakeBtn("Open",  fontBtn, 48, 26, 136, 10, OPENBG);
        btnSwap       = MakeBtn("Start", fontBtn, 68, 26, 188, 10, BTNBG);
        btnLogsSwap   = MakeLogsBtn(282, 10);

        // Row 2 — Open WebUI (y=54)
        lblWeb       = MakeLabel("Open WebUI", font, FG, 14, 54);
        statusWeb    = MakeStatusLabel(14, 71, fontSt);
        btnReloadWeb = MakeReloadBtn(110, 54);
        btnOpenWeb   = MakeBtn("Open",  fontBtn, 48, 26, 136, 54, OPENBG);
        btnWeb       = MakeBtn("Start", fontBtn, 68, 26, 188, 54, BTNBG);
        btnLogsWeb   = MakeLogsBtn(282, 54);

        // Row 3 — ComfyUI (y=98)
        lblComfy       = MakeLabel("ComfyUI", font, FG, 14, 98);
        statusComfy    = MakeStatusLabel(14, 115, fontSt);
        btnReloadComfy = MakeReloadBtn(110, 98);
        btnOpenComfy   = MakeBtn("Open",  fontBtn, 48, 26, 136, 98, OPENBG);
        btnComfy       = MakeBtn("Start", fontBtn, 68, 26, 188, 98, BTNBG);
        btnLogsComfy   = MakeLogsBtn(282, 98);

        // Row 4 — official filesystem bridge (y=142)
        lblMcp       = MakeLabel("MCP", font, FG, 14, 142);
        lblMcp.Cursor = Cursors.Hand;
        statusMcp    = MakeStatusLabel(14, 159, fontSt);
        btnReloadMcp = MakeReloadBtn(110, 142);
        btnOpenMcp   = MakeBtn("Open",  fontBtn, 48, 26, 136, 142, OPENBG);
        btnMcp       = MakeBtn("Start", fontBtn, 68, 26, 188, 142, BTNBG);
        var wrapTip = new ToolTip();
        wrapTip.SetToolTip(lblMcp, "Click for MCP options");
        btnLogsMcp   = MakeLogsBtn(282, 142);

        lblOpenCode = MakeLabel("OpenCode", font, FG, 14, 186);
        var openCodeStatus = MakeStatusLabel(14, 203, fontSt);
        openCodeStatus.Text = "WSL terminal";
        openCodeStatus.ForeColor = Color.FromArgb(170, 170, 170);
        btnLaunchOpenCode = MakeBtn("Launch", fontBtn, 120, 26, 136, 186, OPENBG);

        var sep = new Label { BorderStyle = BorderStyle.Fixed3D, Size = new Size(296, 2), Location = new Point(12, 224) };

        // Bottom row: Start All | Stop All | Reload All | ☰
        btnAll       = MakeBtn("Start All",  fontBtn, 80, 28, 12,  232, Color.FromArgb(35, 110, 35));
        btnStop      = MakeBtn("Stop All",   fontBtn, 80, 28, 102, 232, Color.FromArgb(140, 35, 35));
        btnReloadAll = MakeBtn("Reload All", fontBtn, 86, 28, 192, 232, Color.FromArgb(130, 90, 0));
        btnLogsGui   = MakeLogsBtn(282, 232, 28);

        Controls.AddRange(new Control[] {
            lblSwap, statusSwap, btnReloadSwap, btnLogsSwap, btnOpenSwap, btnSwap,
            lblWeb, statusWeb, btnReloadWeb, btnLogsWeb, btnOpenWeb, btnWeb,
            lblComfy, statusComfy, btnReloadComfy, btnLogsComfy, btnOpenComfy, btnComfy,
            lblMcp, statusMcp, btnReloadMcp, btnLogsMcp, btnOpenMcp, btnMcp,
            lblOpenCode, openCodeStatus, btnLaunchOpenCode,
            sep, btnAll, btnStop, btnLogsGui, btnReloadAll
        });

        btnSwap.Click  += (s, e) => { reloadingSwap = false;  if (btnSwap.Text == "Start") DoStart("swap");   else DoStop("swap"); };
        btnWeb.Click   += (s, e) => { reloadingWeb = false;   if (btnWeb.Text == "Start") DoStart("web");     else DoStop("web"); };
        btnComfy.Click += (s, e) => { reloadingComfy = false; if (btnComfy.Text == "Start") DoStart("comfy"); else DoStop("comfy"); };
        btnMcp.Click   += (s, e) => { if (btnMcp.Text == "Start") DoStart("mcp"); else DoStop("mcp"); };
        lblMcp.Click += (s, e) => HandleMcpLabelClick();
        btnOpenSwap.Click  += (s, e) => Process.Start("http://localhost:8080/ui/#/playground");
        btnOpenWeb.Click   += (s, e) => Process.Start("http://localhost:3000");
        btnOpenComfy.Click += (s, e) => Process.Start("http://localhost:8188");
        btnOpenMcp.Click += (s, e) => Process.Start("http://127.0.0.1:8790/");
        btnLaunchOpenCode.Click += (s, e) => LaunchOpenCode();
        btnReloadSwap.Click  += (s, e) => { reloadingSwap = true;  DoStop("swap");  DoStart("swap"); };
        btnReloadWeb.Click   += (s, e) => { reloadingWeb = true;   DoStop("web");   DoStart("web"); };
        btnReloadComfy.Click += (s, e) => { reloadingComfy = true; DoStop("comfy"); DoStart("comfy"); };
        btnReloadMcp.Click += (s, e) => { DoStop("mcp"); DoStart("mcp"); };
        btnAll.Click       += (s, e) => { DoStart("swap"); DoStart("web"); DoStart("comfy"); DoStart("mcp"); };
        btnStop.Click      += (s, e) => StopAllServices();
        var stopMenu = new ContextMenuStrip();
        stopMenu.Items.Add("Stop -> Exit", null, (s, e) => StopAndExit());
        btnStop.ContextMenuStrip = stopMenu;
        btnReloadAll.Click += (s, e) => {
            reloadingSwap = true;  DoStop("swap");  DoStart("swap");
            reloadingWeb = true;   DoStop("web");   DoStart("web");
            reloadingComfy = true; DoStop("comfy"); DoStart("comfy");
            DoStop("mcp"); DoStart("mcp");
        };
        btnLogsSwap.Click  += (s, e) => OpenWslLog(SWAP_LOG_WSL, SWAP_LOG_WIN);
        btnLogsWeb.Click   += (s, e) => OpenWslLog(WEB_LOG_WSL,  WEB_LOG_WIN);
        btnLogsComfy.Click += (s, e) => OpenWinLog(COMFY_LOG_WIN);
        btnLogsMcp.Click   += (s, e) => OpenWslLog(MCP_LOG_WSL, MCP_LOG_WIN);
        btnLogsGui.Click   += (s, e) => ShowGuiLog();

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 3000;
        timer.Tick += (s, e) => CheckServicesAsync();
        timer.Start();

        spinTimer = new System.Windows.Forms.Timer();
        spinTimer.Interval = 50;
        spinTimer.Tick += (s, e) => SpinTick();
        spinTimer.Start();

        SetupTray();
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) { Hide(); ShowInTaskbar = false; } };
        FormClosing += (s, e) => { if (!exitForReal) { e.Cancel = true; Hide(); ShowInTaskbar = false; } };
        FormClosed  += (s, e) => { timer.Stop(); spinTimer.Stop(); tray.Visible = false; tray.Dispose(); if (trayIcon != null) trayIcon.Dispose(); if (windowIcon != null) windowIcon.Dispose(); };

        GUILog("GUI started");
        CheckServicesAsync();
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

    void WrapAllModels()
    {
        string apiKey;
        using (var prompt = new Form())
        {
            prompt.Text = "Open WebUI authentication";
            prompt.Size = new Size(470, 180);
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
            prompt.MaximizeBox = false;
            prompt.MinimizeBox = false;
            prompt.BackColor = BG;
            prompt.ForeColor = FG;
            var label = new Label { Text = "API key (optional)", Location = new Point(12, 12),
                Size = new Size(420, 20), ForeColor = FG };
            var keyBox = new TextBox { Location = new Point(12, 34), Size = new Size(430, 24),
                UseSystemPasswordChar = true };
            var skipKey = new CheckBox { Text = "Continue without a key using local sign-in",
                Location = new Point(12, 66), Size = new Size(420, 24), ForeColor = FG,
                BackColor = BG, AutoSize = false };
            var submit = MakeBtn("Submit", new Font("Segoe UI", 9f), 86, 28, 356, 104,
                Color.FromArgb(35, 110, 35));
            var cancel = MakeBtn("Cancel", new Font("Segoe UI", 9f), 80, 28, 268, 104, BTNBG);
            submit.DialogResult = DialogResult.OK;
            cancel.DialogResult = DialogResult.Cancel;
            Action updateSubmit = () => { submit.Enabled = keyBox.Text.Trim().Length > 0 || skipKey.Checked; };
            keyBox.TextChanged += (s, e) => updateSubmit();
            skipKey.CheckedChanged += (s, e) => updateSubmit();
            updateSubmit();
            prompt.Controls.AddRange(new Control[] { label, keyBox, skipKey, submit, cancel });
            prompt.AcceptButton = submit;
            prompt.CancelButton = cancel;
            if (prompt.ShowDialog(this) != DialogResult.OK) return;
            apiKey = keyBox.Text.Trim();
            keyBox.Clear();
        }

        lblMcp.Enabled = false;
        GUILog("Wrapping Open WebUI model entries");
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string output = "";
            string error = "";
            int exitCode = -1;
            try
            {
                var psi = new ProcessStartInfo {
                    FileName = "wsl.exe",
                    Arguments = "-e bash -lc \"cd /mnt/c/Users/Natural/llama.cpp && ./owui-venv/bin/python wrap_owui_models.py\"",
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
                };
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                using (var p = new Process { StartInfo = psi })
                {
                    p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data); };
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.StandardInput.WriteLine(apiKey);
                    p.StandardInput.Close();
                    apiKey = null;
                    p.WaitForExit();
                    p.WaitForExit();
                    exitCode = p.ExitCode;
                    lock (stdout) output = stdout.ToString();
                    lock (stderr) error = stderr.ToString();
                }
            }
            catch (Exception ex) { error = ex.Message; }

            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    lblMcp.Enabled = true;
                    GUILog(exitCode == 0 ? "Model wrapping completed" : "Model wrapping failed");
                    string result = (output + (String.IsNullOrWhiteSpace(error) ? "" : "\r\n" + error)).Trim();
                    if (result.Length > 5000) result = result.Substring(0, 5000);
                    MessageBox.Show(this, result.Length == 0 ? "The wrapping helper returned no details." : result,
                        exitCode == 0 ? "Model wrapping complete" : "Model wrapping failed",
                        MessageBoxButtons.OK, exitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                });
            }
            catch { }
        });
    }

    void HandleMcpLabelClick()
    {
        if (!TestPort(8787))
        {
            MessageBox.Show(this, "MCP is off. Start MCP, then click its label here to wrap the models.",
                "MCP is off", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            "Wrap all Open WebUI models with MCP and Prefill Injection?",
            "Confirm model wrapping", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer == DialogResult.Yes) WrapAllModels();
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

    bool EnsureGitHubShareMounted()
    {
        var p = RunWSL("-u", "root", "-e", "bash", "-lc",
            "mkdir -p /mnt/r; grep -Fq ' on /mnt/r ' /proc/mounts || mount -t drvfs '\\\\TOWER\\addonfiles' /mnt/r; test -d /mnt/r/github");
        if (p == null) return false;
        try { return p.WaitForExit(15000) && p.ExitCode == 0; }
        catch { return false; }
    }

    void DoStart(string svc)
    {
        if (svc == "swap")
        {
            GUILog("llama-swap starting");
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
        else if (svc == "mcp")
        {
            GUILog("MCP starting");
            statusMcp.Text = "Starting"; statusMcp.ForeColor = ORANGE;
            btnMcp.Text = "..."; btnMcp.Enabled = false;
            if (!EnsureGitHubShareMounted())
            {
            GUILog("MCP could not mount \\\\TOWER\\addonfiles at /mnt/r");
                statusMcp.Text = "Mount failed"; statusMcp.ForeColor = RED;
                btnMcp.Text = "Start"; btnMcp.Enabled = true;
                return;
            }
            mcpConfigProc = RunWSL("bash", "-lc",
                "cd /mnt/c/Users/Natural/llama.cpp && ./owui-venv/bin/uvicorn mcp_configurator:app --host 127.0.0.1 --port 8790 >> /tmp/mcp-configurator.log 2>&1");
            if (!WaitForPort(8790, 12000))
            {
                GUILog("MCP configurator failed to start; see /tmp/mcp-configurator.log");
                statusMcp.Text = "Configurator failed"; statusMcp.ForeColor = RED;
                btnMcp.Text = "Start"; btnMcp.Enabled = true;
                return;
            }
            mcpProc = RunWSL("bash", "-lc",
                "cd /mnt/c/Users/Natural/llama.cpp && ./owui-venv/bin/mcpo --host 127.0.0.1 --port 8787 --config mcp-readonly.json --hot-reload >> " + MCP_LOG_WSL + " 2>&1");
        }
    }

    void DoStop(string svc)
    {
        if (svc == "swap")
        {
            GUILog("llama-swap stopping");
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
        else if (svc == "mcp")
        {
            GUILog("MCP stopping");
            if (IsAlive(mcpProc)) { try { mcpProc.Kill(); } catch {} }
            mcpProc = null;
            RunWSL("pkill", "-x", "mcpo");
            if (IsAlive(mcpConfigProc)) { try { mcpConfigProc.Kill(); } catch {} }
            mcpConfigProc = null;
            RunWSL("bash", "-lc", "fuser -k 8790/tcp 2>/dev/null || true");
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

    bool WaitForPort(int port, int timeoutMs)
    {
        var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < until)
        {
            if (TestPort(port)) return true;
            Thread.Sleep(250);
        }
        return TestPort(port);
    }

    void CheckServicesAsync()
    {
        if (checkingPorts || IsDisposed) return;
        checkingPorts = true;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            bool s = TestPort(8080), w = TestPort(3000), c = TestPort(8188), m = TestPort(8787);
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (!IsDisposed) UpdateUI(s, w, c, m);
                    checkingPorts = false;
                });
            }
            catch { checkingPorts = false; }
        });
    }

    void UpdateUI(bool s, bool w, bool c, bool m)
    {
        UpdateSvc(s, swapProc, statusSwap, btnSwap, btnOpenSwap, ref reloadingSwap, "llama-swap");
        UpdateSvc(w, webProc, statusWeb, btnWeb, btnOpenWeb, ref reloadingWeb, "Open WebUI");
        UpdateSvc(c, comfyProc, statusComfy, btnComfy, btnOpenComfy, ref reloadingComfy, "ComfyUI");
        UpdateSvc(m, mcpProc, statusMcp, btnMcp, btnOpenMcp, ref reloadingMcp, "MCP");
        if (tray != null)
        {
            tray.Text = "Local AI | swap " + (s ? "on" : "off") + " web " + (w ? "on" : "off") +
                " comfy " + (c ? "on" : "off") + " MCP " + (m ? "on" : "off");
            bool crashed = IsErrorStatus(statusSwap) || IsErrorStatus(statusWeb) ||
                IsErrorStatus(statusComfy) || IsErrorStatus(statusMcp);
            bool starting = IsStartingStatus(statusSwap) || IsStartingStatus(statusWeb) ||
                IsStartingStatus(statusComfy) || IsStartingStatus(statusMcp);
            RefreshTrayIcon(crashed ? RED : starting ? YELLOW : GREEN);
        }
    }

    bool IsErrorStatus(Label label)
    {
        return label.Text == "Crashed" || label.Text.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    bool IsStartingStatus(Label label)
    {
        return label.Text == "Starting" || label.Text == "Restarting";
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

    void LaunchOpenCode()
    {
        string selectedPath = null;
        using (var picker = new Form())
        {
            picker.Text = "OpenCode project";
            picker.ClientSize = new Size(640, 390);
            picker.FormBorderStyle = FormBorderStyle.FixedDialog;
            picker.MaximizeBox = false;
            picker.MinimizeBox = false;
            picker.StartPosition = FormStartPosition.CenterParent;
            picker.BackColor = BG;
            picker.ForeColor = FG;
            var label = new Label { Text = "Search recent locations", Location = new Point(12, 12),
                Size = new Size(300, 20), ForeColor = FG };
            var search = new TextBox { Location = new Point(12, 36), Size = new Size(616, 24),
                Font = new Font("Segoe UI", 9f) };
            var searchTip = new ToolTip();
            searchTip.SetToolTip(search, "Search any part of a recent path");
            var recent = new DataGridView { Location = new Point(12, 68), Size = new Size(616, 268),
                BackgroundColor = BTNBG, ForeColor = FG, GridColor = Color.FromArgb(70, 70, 70),
                BorderStyle = BorderStyle.FixedSingle, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true, RowHeadersVisible = false, ColumnHeadersVisible = true,
                MultiSelect = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ScrollBars = ScrollBars.Both,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 28 } };
            recent.DefaultCellStyle.BackColor = BTNBG;
            recent.DefaultCellStyle.ForeColor = FG;
            recent.DefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 70, 140);
            recent.DefaultCellStyle.SelectionForeColor = FG;
            recent.ColumnHeadersDefaultCellStyle.BackColor = BG;
            recent.ColumnHeadersDefaultCellStyle.ForeColor = FG;
            recent.EnableHeadersVisualStyles = false;
            recent.Columns.Add(new DataGridViewTextBoxColumn { Name = "Path", MinimumWidth = 280,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
            recent.Columns.Add(new DataGridViewTextBoxColumn { Name = "Last opened", Width = 150,
                MinimumWidth = 150, SortMode = DataGridViewColumnSortMode.NotSortable });
            recent.Columns.Add(new DataGridViewButtonColumn { Name = "Remove", HeaderText = "", Width = 36,
                ToolTipText = "Remove from Recent", SortMode = DataGridViewColumnSortMode.NotSortable,
                Text = "-", UseColumnTextForButtonValue = true });
            var open = MakeBtn("Open", new Font("Segoe UI", 9f), 128, 28, 306, 350, Color.FromArgb(35, 70, 140));
            var browse = MakeBtn("Browse...", new Font("Segoe UI", 9f), 90, 28, 442, 350, BTNBG);
            var cancel = MakeBtn("Cancel", new Font("Segoe UI", 9f), 88, 28, 540, 350, BTNBG);
            var paths = LoadRecentOpenCodeFolders();
            Action updateAction = () => {
                open.Text = recent.SelectedRows.Count > 1 ? "Remove Multiple" : "Open";
                open.Enabled = recent.SelectedRows.Count > 0;
            };
            Action refresh = () => {
                recent.Rows.Clear();
                foreach (var entry in paths)
                    if (entry.Path.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        recent.Rows.Add(entry.Path, entry.LastOpenedUtcTicks > 0
                            ? new DateTime(entry.LastOpenedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("g")
                            : "Unknown");
                recent.ClearSelection();
                if (recent.Rows.Count > 0) recent.Rows[0].Selected = true;
                updateAction();
            };
            Action<List<string>> removePaths = remove => {
                if (remove.Count == 0) return;
                string message = remove.Count == 1
                    ? "Remove this location from Recent?\n\n" + remove[0]
                    : "Remove " + remove.Count + " locations from Recent?";
                if (MessageBox.Show(picker, message + "\n\nFolders on disk will not be deleted.",
                    "Confirm removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var remaining = paths.FindAll(p => !remove.Exists(item =>
                    string.Equals(item, p.Path, StringComparison.OrdinalIgnoreCase)));
                if (WriteRecentOpenCodeFolders(remaining)) { paths = remaining; refresh(); }
            };
            search.TextChanged += (s, e) => refresh();
            recent.SelectionChanged += (s, e) => updateAction();
            recent.CellContentClick += (s, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex == 2)
                    removePaths(new List<string> { (string)recent.Rows[e.RowIndex].Cells[0].Value });
            };
            recent.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && recent.SelectedRows.Count == 1)
                    open.PerformClick();
            };
            open.Click += (s, e) => {
                if (recent.SelectedRows.Count > 1)
                {
                    var remove = new List<string>();
                    foreach (DataGridViewRow row in recent.SelectedRows)
                        remove.Add((string)row.Cells[0].Value);
                    removePaths(remove);
                }
                else if (recent.SelectedRows.Count == 1)
                {
                    selectedPath = (string)recent.SelectedRows[0].Cells[0].Value;
                    picker.DialogResult = DialogResult.OK;
                }
            };
            browse.Click += (s, e) => {
                using (var folder = new FolderBrowserDialog())
                {
                    folder.Description = "Select a project for OpenCode";
                    folder.ShowNewFolderButton = true;
                    if (recent.SelectedRows.Count == 1 &&
                        Directory.Exists((string)recent.SelectedRows[0].Cells[0].Value))
                        folder.SelectedPath = (string)recent.SelectedRows[0].Cells[0].Value;
                    if (folder.ShowDialog(picker) == DialogResult.OK)
                    {
                        selectedPath = folder.SelectedPath;
                        picker.DialogResult = DialogResult.OK;
                    }
                }
            };
            cancel.DialogResult = DialogResult.Cancel;
            picker.AcceptButton = open;
            picker.CancelButton = cancel;
            picker.Controls.AddRange(new Control[] { label, search, recent, open, browse, cancel });
            refresh();
            if (picker.ShowDialog(this) != DialogResult.OK) return;
        }
        if (!Directory.Exists(selectedPath))
        {
            MessageBox.Show(this, "That folder is no longer available: " + selectedPath,
                "Local AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/k wsl.exe -d Ubuntu --cd \"" + selectedPath +
                    "\" -e /home/natural/.opencode/bin/opencode",
                UseShellExecute = true
            });
            SaveRecentOpenCodeFolder(selectedPath);
            GUILog("OpenCode launched for " + selectedPath);
        }
        catch (Exception ex)
        {
            GUILog("OpenCode launch failed: " + ex.Message);
            MessageBox.Show(this, "Could not open OpenCode: " + ex.Message,
                "Local AI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    string RecentOpenCodePath
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalAI", "opencode-recent.json"); }
    }

    List<RecentOpenCodeEntry> LoadRecentOpenCodeFolders()
    {
        try
        {
            List<RecentOpenCodeEntry> source;
            if (File.Exists(RecentOpenCodePath))
                source = new JavaScriptSerializer().Deserialize<List<RecentOpenCodeEntry>>(File.ReadAllText(RecentOpenCodePath));
            else
            {
                var legacyPath = Path.ChangeExtension(RecentOpenCodePath, ".txt");
                source = new List<RecentOpenCodeEntry>();
                if (File.Exists(legacyPath))
                    foreach (var path in File.ReadAllLines(legacyPath))
                        source.Add(new RecentOpenCodeEntry { Path = path });
            }
            var paths = new List<RecentOpenCodeEntry>();
            if (source == null) return paths;
            foreach (var entry in source)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Path) || paths.Exists(p =>
                    string.Equals(p.Path, entry.Path, StringComparison.OrdinalIgnoreCase))) continue;
                paths.Add(entry);
            }
            return paths;
        }
        catch (Exception ex) { GUILog("Could not read recent OpenCode folders: " + ex.Message); return new List<RecentOpenCodeEntry>(); }
    }

    void SaveRecentOpenCodeFolder(string path)
    {
        try
        {
            var paths = LoadRecentOpenCodeFolders();
            paths.RemoveAll(p => string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));
            paths.Insert(0, new RecentOpenCodeEntry { Path = path, LastOpenedUtcTicks = DateTime.UtcNow.Ticks });
            WriteRecentOpenCodeFolders(paths);
        }
        catch (Exception ex) { GUILog("Could not save recent OpenCode folders: " + ex.Message); }
    }

    bool WriteRecentOpenCodeFolders(List<RecentOpenCodeEntry> paths)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecentOpenCodePath));
            File.WriteAllText(RecentOpenCodePath, new JavaScriptSerializer().Serialize(paths));
            return true;
        }
        catch (Exception ex)
        {
            GUILog("Could not save recent OpenCode folders: " + ex.Message);
            MessageBox.Show(this, "Could not update recent locations: " + ex.Message,
                "Local AI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    void StopAllServices()
    {
        DoStop("swap"); DoStop("web"); DoStop("comfy"); DoStop("mcp");
        CheckServicesAsync();
    }

    void StopAndExit()
    {
        btnStop.Enabled = false;
        StopAllServices();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            var until = DateTime.UtcNow.AddSeconds(12);
            while (DateTime.UtcNow < until &&
                (TestPort(8080) || TestPort(3000) || TestPort(8188) || TestPort(8787) || TestPort(8790)))
                Thread.Sleep(300);

            bool stillRunning = TestPort(8080) || TestPort(3000) || TestPort(8188) ||
                TestPort(8787) || TestPort(8790);
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    btnStop.Enabled = true;
                    if (stillRunning && MessageBox.Show(this,
                        "Some services are still responding. Exit the manager anyway?",
                        "Local AI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    exitForReal = true;
                    Close();
                });
            }
            catch { }
        });
    }

    void SetupTray()
    {
        tray = new NotifyIcon();
        trayIcon = CreateExcavatorIcon(GREEN);
        trayIconColor = GREEN;
        tray.Icon = trayIcon;
        tray.Visible = true;
        tray.Text = "Local AI";

        var cm = new ContextMenuStrip();
        cm.Items.Add("Show",        null, (s, e) => ShowFront());
        cm.Items.Add("Start All",   null, (s, e) => { DoStart("swap"); DoStart("web"); DoStart("comfy"); DoStart("mcp"); });
        cm.Items.Add("Stop All",    null, (s, e) => StopAllServices());
        cm.Items.Add("Exit", null, (s, e) => { exitForReal = true; tray.Visible = false; Close(); });
        tray.ContextMenuStrip = cm;

        tray.DoubleClick += (s, e) => ShowFront();
    }

    void RefreshTrayIcon(Color color)
    {
        if (tray == null || trayIconColor == color) return;
        var next = CreateExcavatorIcon(color);
        var nextWindow = CreateExcavatorIcon(color);
        var previous = trayIcon;
        var previousWindow = windowIcon;
        tray.Icon = next;
        trayIcon = next;
        Icon = nextWindow;
        windowIcon = nextWindow;
        trayIconColor = color;
        if (previous != null) previous.Dispose();
        if (previousWindow != null) previousWindow.Dispose();
    }

    static Icon CreateExcavatorIcon(Color color, string imagePath = null)
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        using (var source = Image.FromFile(imagePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "excavator.png")))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(color);
            g.DrawImage(source, new Rectangle(0, 0, 32, 32));
        }

        IntPtr handle = bmp.GetHicon();
        Icon icon;
        try { icon = (Icon)Icon.FromHandle(handle).Clone(); }
        finally { DestroyIcon(handle); bmp.Dispose(); }
        return icon;
    }

    public static void ExportExcavatorIcon(string path, string imagePath)
    {
        using (var icon = CreateExcavatorIcon(GREEN, imagePath))
        using (var stream = File.Create(path))
            icon.Save(stream);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr handle);

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--export-icon")
        {
            AIControlForm.ExportExcavatorIcon(args[1], args[2]);
            return;
        }
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
