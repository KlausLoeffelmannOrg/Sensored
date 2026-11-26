using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WinForms;
using WarpToolkit.WinForms.AppServices.ServiceExtensions;
using WarpToolkit.Sensors;
using WarpToolkit.ComponentModel;

namespace Sensored;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CrashGuard crashGuard = CrashGuard.Install();

        try
        {
            string? automationDirectory = AppAutomation.ParseDirectory(args);
            crashGuard.SetStage("configuring-host");
            WinFormsApplicationBuilder builder = WinFormsApplication.CreateBuilder();

            builder.Services.AddWinFormsUserSettingsService();
            builder.Services.AddWinFormsExceptionService();
            builder.Services.AddSingleton<DemoSensorSourceFactory>();
            builder.Services.AddSingleton<ISensorSourceFactory>(
                services => services.GetRequiredService<DemoSensorSourceFactory>());
            builder.Services.AddSingleton<ISensorService, SensorService>();
            builder.Services.AddScoped<MainForm>(services =>
            {
                MainForm form = new(services.GetRequiredService<ISensorService>(),
                    services.GetRequiredService<IUserSettingsService>(),
                    services.GetRequiredService<DemoSensorSourceFactory>());
                if (automationDirectory is not null)
                {
                    form.ConfigureAutomation(automationDirectory);
                }

                return form;
            });

            builder.UseStartupForm<MainForm>()
                .UseHighDpiMode(HighDpiMode.SystemAware)
                .UseColorMode(SystemColorMode.System)
                .UseTextRenderingV2()
                .UseLegacyVisualStyles()
                .UseFormRevealMode(FormRevealMode.Deferred)
                .UseVisualStylesMode(VisualStylesMode.Net11);

            crashGuard.SetStage("building-host");
            WinFormsApplication app = builder.Build();

            crashGuard.SetStage("running-message-loop");
            app.Run();
            crashGuard.SetStage("message-loop-exited");
        }
        catch (Exception exception)
        {
            crashGuard.ReportFatal(exception);
            throw;
        }
        finally
        {
            crashGuard.Uninstall();
        }
    }
}
