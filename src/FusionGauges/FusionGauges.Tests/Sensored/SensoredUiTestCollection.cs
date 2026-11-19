namespace Sensored.Tests;

/// <summary>Serializes hosted WinForms and Direct2D tests against other application tests.</summary>
[CollectionDefinition(nameof(SensoredUiTestCollection), DisableParallelization = true)]
public sealed class SensoredUiTestCollection;
