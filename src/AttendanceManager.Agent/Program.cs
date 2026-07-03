using AttendanceManager.Agent.Forms;
using AttendanceManager.Agent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AttendanceManager.Agent;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AttendanceManager", "logs", "agent-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        try
        {
            Log.Information("AttendanceManager Agent starting");

            var services = new ServiceCollection();
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            if (OperatingSystem.IsWindows())
            {
                var logger = provider.GetRequiredService<ILogger<StartupManager>>();
                StartupManager.EnableAutoStart(logger);
            }

            var agent = provider.GetRequiredService<AttendanceAgent>();

            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                ApplicationConfiguration.Initialize();
                var trayApp = new TrayApplicationContext(agent, provider.GetRequiredService<ILogger<TrayApplicationContext>>());
                Application.Run(trayApp);
            }
            else
            {
                // fallback: run as console
                agent.InitializeAsync().GetAwaiter().GetResult();
                Console.WriteLine("AttendanceManager Agent running. Press Enter to exit.");
                Console.ReadLine();
                agent.RecordLogoutAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Agent terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddSerilog());

        var serverUrl = LoadServerUrl();
        services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(serverUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IdleDetector>();
        services.AddSingleton<AttendanceAgent>();
    }

    private static string LoadServerUrl()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AttendanceManager", "server_url.txt");

        if (File.Exists(configPath))
            return File.ReadAllText(configPath).Trim();

        return "https://localhost:5001";
    }
}

internal class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly AttendanceAgent _agent;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public TrayApplicationContext(AttendanceAgent agent, Microsoft.Extensions.Logging.ILogger<TrayApplicationContext> logger)
    {
        _agent = agent;
        _logger = logger;

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "AttendanceManager Agent",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _agent.StatusChanged += (_, status) =>
        {
            _trayIcon.Text = $"AttendanceManager - {status}";
        };

        _agent.NotificationsReceived += (_, notifications) =>
        {
            foreach (var n in notifications)
            {
                _trayIcon.ShowBalloonTip(5000, n.Title, n.Message, ToolTipIcon.Info);
            }
        };

        // handle system shutdown/logoff
        SystemEvents.SessionEnding += async (_, args) =>
        {
            _logger.LogInformation("System session ending: {Reason}", args.Reason);
            await _agent.RecordLogoutAsync();
        };

        // start agent
        _ = Task.Run(async () =>
        {
            try
            {
                await _agent.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing agent");
            }
        });
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Status: Starting...");
        statusItem.Enabled = false;
        menu.Items.Add(statusItem);

        _agent.StatusChanged += (_, status) =>
        {
            if (menu.InvokeRequired)
                menu.Invoke(() => statusItem.Text = $"Status: {status}");
            else
                statusItem.Text = $"Status: {status}";
        };

        menu.Items.Add(new ToolStripSeparator());

        var clockOutItem = new ToolStripMenuItem("Clock Out");
        clockOutItem.Click += async (_, _) => await HandleClockOutAsync();
        menu.Items.Add(clockOutItem);

        var leaveItem = new ToolStripMenuItem("Request Leave...");
        leaveItem.Click += async (_, _) => await HandleLeaveRequestAsync();
        menu.Items.Add(leaveItem);

        var correctionItem = new ToolStripMenuItem("Correct Attendance...");
        correctionItem.Click += async (_, _) => await HandleCorrectionAsync();
        menu.Items.Add(correctionItem);

        menu.Items.Add(new ToolStripSeparator());

        var configItem = new ToolStripMenuItem("Configure...");
        configItem.Click += (_, _) => ShowConfigDialog();
        menu.Items.Add(configItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += async (_, _) =>
        {
            await _agent.RecordLogoutAsync();
            _trayIcon.Visible = false;
            Application.Exit();
        };
        menu.Items.Add(exitItem);

        return menu;
    }

    private async Task HandleClockOutAsync()
    {
        if (!_agent.IsAuthenticated)
        {
            MessageBox.Show("Not configured. Please set up your credentials first.", "Clock Out", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (success, error) = await _agent.TryClockOutAsync();
        if (success)
        {
            _trayIcon.ShowBalloonTip(3000, "Clocked Out", "Your logout has been recorded.", ToolTipIcon.Info);
            return;
        }

        // work summary required — show the form then retry
        if (error != null && error.Contains("work summary", StringComparison.OrdinalIgnoreCase))
        {
            var form = new WorkSummaryForm();
            if (form.ShowDialog() != DialogResult.OK) return;

            var (summaryOk, summaryError) = await _agent.SubmitWorkSummaryAsync(form.Entries);
            if (!summaryOk)
            {
                MessageBox.Show($"Failed to submit work summary: {summaryError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var (retryOk, retryError) = await _agent.TryClockOutAsync();
            if (retryOk)
                _trayIcon.ShowBalloonTip(3000, "Clocked Out", "Work summary submitted and logout recorded.", ToolTipIcon.Info);
            else
                MessageBox.Show($"Clock out failed: {retryError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            MessageBox.Show($"Clock out failed: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task HandleLeaveRequestAsync()
    {
        if (!_agent.IsAuthenticated)
        {
            MessageBox.Show("Not configured. Please set up your credentials first.", "Request Leave", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var leaveTypes = await _agent.GetLeaveTypesAsync();
        if (leaveTypes.Count == 0)
        {
            MessageBox.Show("Could not load leave types from server. Please check your connection.", "Request Leave", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var form = new LeaveRequestForm(leaveTypes);
        if (form.ShowDialog() != DialogResult.OK) return;

        var (ok, error) = await _agent.SubmitLeaveRequestAsync(form.SelectedLeaveTypeId, form.StartDate, form.EndDate, form.Reason);
        if (ok)
            _trayIcon.ShowBalloonTip(4000, "Leave Request Submitted", "Your leave request has been sent for approval.", ToolTipIcon.Info);
        else
            MessageBox.Show($"Failed to submit leave request: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async Task HandleCorrectionAsync()
    {
        if (!_agent.IsAuthenticated)
        {
            MessageBox.Show("Not configured. Please set up your credentials first.", "Correct Attendance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var form = new CorrectionForm();
        if (form.ShowDialog() != DialogResult.OK) return;

        var (ok, error) = await _agent.SubmitCorrectionAsync(
            form.SelectedDate, form.ClaimedStatus,
            form.ClaimedLoginTime, form.ClaimedLogoutTime,
            form.Reason);

        if (ok)
            _trayIcon.ShowBalloonTip(4000, "Correction Submitted", "Your attendance correction request has been sent for approval.", ToolTipIcon.Info);
        else
            MessageBox.Show($"Failed to submit correction: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ShowConfigDialog()
    {
        var form = new Form
        {
            Text = "AttendanceManager Agent - Configuration",
            Width = 420,
            Height = 340,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblServer = new Label { Text = "Server URL:", Left = 20, Top = 20, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        var txtServer = new TextBox { Left = 130, Top = 18, Width = 240, Text = "https://localhost:5001" };

        var lblEmail = new Label { Text = "Email:", Left = 20, Top = 55, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        var txtEmail = new TextBox { Left = 130, Top = 53, Width = 240 };

        var rbPassword = new RadioButton { Text = "Password", Left = 130, Top = 88, Width = 100, Checked = true };
        var rbPin = new RadioButton { Text = "PIN", Left = 240, Top = 88, Width = 80 };

        var lblPassword = new Label { Text = "Password:", Left = 20, Top = 120, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        var txtPassword = new TextBox { Left = 130, Top = 118, Width = 240, PasswordChar = '*' };

        var lblPin = new Label { Text = "PIN:", Left = 20, Top = 120, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Visible = false };
        var txtPin = new TextBox { Left = 130, Top = 118, Width = 240, PasswordChar = '*', Visible = false };

        rbPassword.CheckedChanged += (_, _) =>
        {
            lblPassword.Visible = rbPassword.Checked;
            txtPassword.Visible = rbPassword.Checked;
            lblPin.Visible = !rbPassword.Checked;
            txtPin.Visible = !rbPassword.Checked;
        };

        rbPin.CheckedChanged += (_, _) =>
        {
            lblPin.Visible = rbPin.Checked;
            txtPin.Visible = rbPin.Checked;
            lblPassword.Visible = !rbPin.Checked;
            txtPassword.Visible = !rbPin.Checked;
        };

        var lblStatus = new Label { Text = "", Left = 20, Top = 160, Width = 350, ForeColor = System.Drawing.Color.Red };
        var btnSave = new Button { Text = "Connect", Left = 130, Top = 200, Width = 120, Height = 35 };

        btnSave.Click += async (_, _) =>
        {
            lblStatus.Text = "Connecting...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            btnSave.Enabled = false;

            bool success;
            if (rbPin.Checked)
                success = await _agent.ConfigureByPinAsync(txtEmail.Text, txtPin.Text, txtServer.Text);
            else
                success = await _agent.ConfigureAsync(txtEmail.Text, txtPassword.Text, txtServer.Text);

            if (success)
            {
                lblStatus.Text = "Connected successfully!";
                lblStatus.ForeColor = System.Drawing.Color.Green;

                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AttendanceManager");
                Directory.CreateDirectory(configDir);
                File.WriteAllText(Path.Combine(configDir, "server_url.txt"), txtServer.Text);

                await Task.Delay(1500);
                form.Close();
            }
            else
            {
                lblStatus.Text = "Connection failed. Check credentials and server URL.";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                btnSave.Enabled = true;
            }
        };

        form.Controls.AddRange(new Control[]
        {
            lblServer, txtServer, lblEmail, txtEmail,
            rbPassword, rbPin,
            lblPassword, txtPassword, lblPin, txtPin,
            lblStatus, btnSave
        });
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _agent.Shutdown();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
