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

    private void ShowConfigDialog()
    {
        var form = new Form
        {
            Text = "AttendanceManager Agent - Configuration",
            Width = 420,
            Height = 280,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblServer = new Label { Text = "Server URL:", Left = 20, Top = 20, Width = 100 };
        var txtServer = new TextBox { Left = 130, Top = 18, Width = 240, Text = "https://localhost:5001" };
        var lblEmail = new Label { Text = "Email:", Left = 20, Top = 55, Width = 100 };
        var txtEmail = new TextBox { Left = 130, Top = 53, Width = 240 };
        var lblPassword = new Label { Text = "Password:", Left = 20, Top = 90, Width = 100 };
        var txtPassword = new TextBox { Left = 130, Top = 88, Width = 240, PasswordChar = '*' };
        var lblStatus = new Label { Text = "", Left = 20, Top = 130, Width = 350, ForeColor = System.Drawing.Color.Red };
        var btnSave = new Button { Text = "Connect", Left = 130, Top = 165, Width = 120, Height = 35 };

        btnSave.Click += async (_, _) =>
        {
            lblStatus.Text = "Connecting...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            btnSave.Enabled = false;

            var success = await _agent.ConfigureAsync(txtEmail.Text, txtPassword.Text, txtServer.Text);
            if (success)
            {
                lblStatus.Text = "Connected successfully!";
                lblStatus.ForeColor = System.Drawing.Color.Green;

                // save server URL
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

        form.Controls.AddRange(new Control[] { lblServer, txtServer, lblEmail, txtEmail, lblPassword, txtPassword, lblStatus, btnSave });
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
