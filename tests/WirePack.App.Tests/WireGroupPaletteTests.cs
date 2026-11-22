using Avalonia.Media;
using WirePack.App.Models;
using Xunit;

namespace WirePack.App.Tests;

public class WireGroupPaletteTests
{
    [Fact]
    public void GetNextColor_SequentialAdditions_ProducesUniqueColors()
    {
        var used = new List<Color>();

        for (int i = 0; i < 20; i++)
        {
            Color color = WireGroupPalette.GetNextColor(used);

            Assert.DoesNotContain(color, used);
            used.Add(color);
        }
    }

    [Fact]
    public void GetNextColor_SkipsColorsAlreadyInUse()
    {
        var used = new List<Color>
        {
            WireGroupPalette.GetColor(0),
            WireGroupPalette.GetColor(1)
        };

        Color next = WireGroupPalette.GetNextColor(used);

        Assert.NotEqual(WireGroupPalette.GetColor(0), next);
        Assert.NotEqual(WireGroupPalette.GetColor(1), next);
    }

    [Fact]
    public void GetNextColor_SameUsedSet_ReturnsSameColor()
    {
        var used = new List<Color> { WireGroupPalette.GetColor(0) };

        Assert.Equal(WireGroupPalette.GetNextColor(used), WireGroupPalette.GetNextColor(used));
    }
}
