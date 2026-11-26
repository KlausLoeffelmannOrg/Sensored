using System.Text.Json;
using Sensored;

namespace FusionGauges.Tests;

public sealed class AppAutomationTests
{
    [Fact]
    public void AutomationRequiresExplicitFlagAndNewRunDirectory()
    {
        Assert.Null(AppAutomation.ParseDirectory([]));
        Assert.Throws<ArgumentException>(() => AppAutomation.ParseDirectory(["--automation"]));
        Assert.Throws<ArgumentException>(() => AppAutomation.ParseDirectory(["--other", "run"]));
        string directory = Path.Combine(Path.GetTempPath(), $"sensored-automation-{Guid.NewGuid():N}");
        Assert.Equal(directory, AppAutomation.ParseDirectory(["--automation", directory]));
        try
        {
            using AppAutomation automation = new(directory, _ => Task.CompletedTask, () => new { }, () => [],
                (_, exception) => throw exception);
            Assert.StartsWith(directory, automation.LabelsPath);
            Assert.Throws<IOException>(() => new AppAutomation(directory, _ => Task.CompletedTask,
                () => new { }, () => [], (_, exception) => throw exception));
        }
        finally
        {
            if (Directory.Exists(directory)) { Directory.Delete(directory, recursive: true); }
        }
    }

    [Fact]
    public async Task CommandsAreAcknowledgedOnceAndFailuresAreReported()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"sensored-automation-{Guid.NewGuid():N}");
        List<AutomationCommand> executed = [];
        List<Exception> errors = [];
        try
        {
            using (AppAutomation automation = new(directory, command =>
            {
                if (command.Action == "bad") { throw new InvalidOperationException("Rejected action."); }
                executed.Add(command);
                return Task.CompletedTask;
            }, () => new { Executed = executed.Count }, () => [], (_, exception) => errors.Add(exception)))
            {
                AutomationCommand command = new(Guid.NewGuid(), "start-session");
                File.WriteAllText(Path.Combine(directory, "command.json"), JsonSerializer.Serialize(command));
                await automation.TickAsync();
                Assert.Single(executed);
                using (JsonDocument result = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "result.json"))))
                {
                    Assert.True(result.RootElement.GetProperty("Success").GetBoolean());
                    Assert.Equal(command.Id, result.RootElement.GetProperty("Id").GetGuid());
                }

                File.WriteAllText(Path.Combine(directory, "command.json"), JsonSerializer.Serialize(command));
                await automation.TickAsync();
                Assert.Single(executed);
                Assert.Single(errors);
                File.WriteAllText(Path.Combine(directory, "command.json"),
                    JsonSerializer.Serialize(new AutomationCommand(Guid.NewGuid(), "bad")));
                await automation.TickAsync();
                using JsonDocument failure = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "result.json")));
                Assert.False(failure.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Rejected action.", failure.RootElement.GetProperty("Error").GetString());
                Assert.Equal(2, errors.Count);
                Assert.False(File.Exists(Path.Combine(directory, "processing.json")));
            }

            using JsonDocument status = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "status.json")));
            Assert.Equal("closed", status.RootElement.GetProperty("Phase").GetString());
        }
        finally
        {
            if (Directory.Exists(directory)) { Directory.Delete(directory, recursive: true); }
        }
    }
}
