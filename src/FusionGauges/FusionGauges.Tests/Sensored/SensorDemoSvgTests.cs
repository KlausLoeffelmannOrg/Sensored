using WarpToolkit.WinForms.Drawing.Symbols;
using WarpToolkit.WinForms.Drawing.Symbols.Rendering;
using WarpToolkit.WinForms.Drawing.Symbols.Svg;

namespace Sensored.Tests;

/// <summary>Verifies the Sensored symbol inventory, metadata, and small-to-large rendering.</summary>
public sealed class SensorDemoSvgTests
{
    [Fact]
    public void Corpus_is_accessible_valid_and_unclipped()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "TestData", "Symbols", "SensorDemo");
        string[] paths = Directory.GetFiles(directory, "*.svg");
        Assert.Equal(28, paths.Length);
        GdiSymbolRenderer renderer = new();

        foreach (string path in paths)
        {
            string markup = File.ReadAllText(path);
            Assert.Empty(SvgSymbolValidator.Validate(markup));
            SvgSymbol symbol = SvgSymbol.FromMarkup(markup);
            Assert.False(string.IsNullOrWhiteSpace(symbol.AccessibilityName));
            Assert.False(string.IsNullOrWhiteSpace(symbol.AccessibilityDescription));

            foreach (int size in new[] { 16, 20, 24, 32, 48, 64, 96 })
            {
                foreach (Color foreground in new[] { Color.Black, Color.White })
                {
                    using Bitmap bitmap = new(size, size);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    renderer.Render(
                        symbol,
                        graphics,
                        new Rectangle(Point.Empty, bitmap.Size),
                        SymbolRenderContext.CreateDefault(bitmap.Size, foreground));
                    bool hasInk = false;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            int alpha = bitmap.GetPixel(x, y).A;
                            hasInk |= alpha > 0;

                            if (size >= 32 && (x == 0 || y == 0 || x == size - 1 || y == size - 1))
                            {
                                Assert.True(alpha == 0, $"{Path.GetFileName(path)} clips at {size}px ({x},{y}).");
                            }
                        }
                    }

                    Assert.True(hasInk, $"{Path.GetFileName(path)} is blank at {size}px.");
                }
            }
        }
    }
}
