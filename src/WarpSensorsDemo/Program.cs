using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WinForms;
using WarpToolkit.WinForms.AppServices.ServiceExtensions;
using WarpToolkit.Sensors;

namespace WarpSensorsDemo;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        CrashGuard crashGuard = CrashGuard.Install();

        try
        {
            crashGuard.SetStage("configuring-host");
            WinFormsApplicationBuilder builder = WinFormsApplication.CreateBuilder();

            builder.Services.AddWinFormsUserSettingsService();
            builder.Services.AddWinFormsExceptionService();
            builder.Services.AddSingleton<DemoSensorSourceFactory>();
            builder.Services.AddSingleton<ISensorSourceFactory>(
                services => services.GetRequiredService<DemoSensorSourceFactory>());
            builder.Services.AddSingleton<ISensorService, SensorService>();
            builder.Services.AddScoped<MainForm>();

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
