using System.Net.Http;
using System.Windows;
using AttendanceManager.AdminApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceManager.AdminApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static ApiService Api => Services.GetRequiredService<ApiService>();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<HttpClient>(sp =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        });

        services.AddSingleton<ApiService>();

        Services = services.BuildServiceProvider();
    }
}
